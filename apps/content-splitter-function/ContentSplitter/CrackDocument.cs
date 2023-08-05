using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.DeepDev;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace ContentSplitter;

public static class CrackDocument
{
    [FunctionName("CrackDocument")]
    public static async Task RunAsync(
        [BlobTrigger("documents-in/{name}", Connection = "BlobStorageAccount")]
        Stream inputBlob,
        string name, ILogger log)
    {
        log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {inputBlob.Length} Bytes");
        var outputClient =
            new BlobContainerClient(Environment.GetEnvironmentVariable("BlobStorageAccount"), "sample-documents");

        var tokenCounter = await TokenizerBuilder.CreateByModelNameAsync("gpt-3.5-turbo");

        var embeddingModelName = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingModel")!;
        var openAiHost = new Uri(Environment.GetEnvironmentVariable("AzureOpenAIHost")!);
        var accessOpenAiUsingManagedIdentity =
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureOpenAISecret"));

        OpenAIClient client = accessOpenAiUsingManagedIdentity
            ? new OpenAIClient(openAiHost, new ManagedIdentityCredential(Environment.GetEnvironmentVariable("AzureOpenAIIdentityClientId")!))
            : new OpenAIClient(openAiHost,
                new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureOpenAISecret")!));

        using var document = PdfDocument.Open(inputBlob);

        var chunkNumber = 0;
        var content = string.Empty;
        foreach (var page in document.GetPages())
        {
            string pageText = page.Text;

            foreach (var contentPiece in pageText.Split('\n'))
            {
                var potentialChunk = content + Environment.NewLine + contentPiece;
                var tokens = tokenCounter.Encode(potentialChunk, new string[] { });
                if (tokens.Count > 600)
                {
                    //tipped over the limit. Lock the chunk without the additional line
                    await LockChunk(outputClient, client, embeddingModelName, name, chunkNumber, content);
                    chunkNumber++;
                    content = contentPiece; //didn't fit into this chunk
                }
                else
                {
                    content = potentialChunk;
                }
            }
        }

        if (content != string.Empty)
        {
            await LockChunk(outputClient, client, embeddingModelName, name, chunkNumber, content);
        }
    }

    private static async Task LockChunk(
        BlobContainerClient blobContainerClient,
        OpenAIClient client,
        string embeddingModelName,
        string documentName,
        int chunkNumber,
        string potentialChunk)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(documentName)}-chunk-{chunkNumber}.json";

        var embeddings = await client.GetEmbeddingsAsync(
            embeddingModelName,
            new EmbeddingsOptions(potentialChunk.ReplaceLineEndings(" ")));

        var documentWithEmbeddings = new InputBlob()
        {
            Content = potentialChunk,
            ContentVector = embeddings.Value.Data.First().Embedding.ToArray()
        };

        await blobContainerClient.UploadBlobAsync(fileName, new BinaryData(documentWithEmbeddings));
    }
}