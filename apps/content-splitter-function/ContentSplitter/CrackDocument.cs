using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

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

        var embeddingModelName = Environment.GetEnvironmentVariable("AzureOpenAIEmbeddingModel")!;
        var openAiHost = new Uri(Environment.GetEnvironmentVariable("AzureOpenAIHost")!);
        var accessOpenAiUsingManagedIdentity =
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureOpenAISecret"));

        OpenAIClient client = accessOpenAiUsingManagedIdentity
            ? new OpenAIClient(openAiHost,
                new ManagedIdentityCredential(Environment.GetEnvironmentVariable("AzureOpenAIIdentityClientId")!))
            : new OpenAIClient(openAiHost,
                new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureOpenAISecret")!));

        var cracker = Path.GetExtension(name) switch
        {
            ".pdf" => (ICrack)new CrackPdf(),
            ".docx" => new CrackWord(),
            _ => new CrackText()
        };

        var chunkNumber = 0;
        var wordList = new List<string>();
        await foreach (var phrase in cracker.Crack(inputBlob))
        {
            wordList.Add(phrase);
            if (wordList.Count > 200)
            {
                await LockChunk(outputClient, client, embeddingModelName, name, ++chunkNumber,
                    string.Join(' ', wordList));
                wordList = wordList.TakeLast(20).ToList();
            }
        }

        await LockChunk(outputClient, client, embeddingModelName, name, ++chunkNumber, string.Join(' ', wordList));
    }

    private static async Task LockChunk(
        BlobContainerClient blobContainerClient,
        OpenAIClient client,
        string embeddingModelName,
        string documentName,
        int chunkNumber,
        string chunk)
    {
        var fileName = $"{Path.GetFileNameWithoutExtension(documentName)}-chunk-{chunkNumber}.json";
        var titleBits = documentName.Split(' ');
        var embeddingChunk = chunk;
        foreach (var titleBit in titleBits)
        {
            //Skewed searches when words which are implicit (such as the title) appear multiple times in the content. Remove and just add the title once to each embedded...
            embeddingChunk = embeddingChunk.Replace(titleBit, "");
        }

        var embeddings = await client.GetEmbeddingsAsync(
            embeddingModelName,
            new EmbeddingsOptions(documentName + " " + embeddingChunk.ReplaceLineEndings(" ")));

        var documentWithEmbeddings = new InputBlob()
        {
            Content = chunk,
            ContentVector = embeddings.Value.Data.First().Embedding.ToArray()
        };

        await blobContainerClient.DeleteBlobIfExistsAsync(fileName);
        await blobContainerClient.UploadBlobAsync(fileName, new BinaryData(documentWithEmbeddings));
    }
}

