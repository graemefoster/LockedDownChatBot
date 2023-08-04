using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

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
        OpenAIClient client = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AzureOpenAISecret"))
            ? new OpenAIClient(openAiHost, new DefaultAzureCredential())
            : new OpenAIClient(openAiHost,
                new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureOpenAISecret")!));

        using var document = PdfDocument.Open(inputBlob);

        foreach (var page in document.GetPages())
        {
            string pageText = page.Text;

            //look for new line characters. We'll index each paragraph
            var lineNumber = 0;
            foreach (var contentPiece in pageText.Split('\n'))
            {
                lineNumber++;
                var fileName = $"{Path.GetFileNameWithoutExtension(name)}-page-{page.Number}-line-{lineNumber}.json";
                
                var embeddings = await client.GetEmbeddingsAsync(
                    embeddingModelName,
                    new EmbeddingsOptions(contentPiece));
                
                var document = new InputBlob()
                {
                    Content = contentPiece,
                    ContentVector = embeddings.Value.Data.SelectMany(x => x.Embedding).ToArray()
                }

                await outputClient.UploadBlobAsync(fileName, new BinaryData(contentPiece));
            }
        }
    }
}

public class InputBlob
{
    public string Content { get; set; }
    public float[] ContentVector  { get; set; }
}