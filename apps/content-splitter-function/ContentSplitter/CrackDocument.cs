using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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

        using var document = PdfDocument.Open(inputBlob);
        foreach (var page in document.GetPages())
        {
            string pageText = page.Text;

            //look for new line characters. We'll index each paragraph
            var lineNumber = 0;
            foreach (var line in pageText.Split('\n'))
            {
                lineNumber++;
                var fileName = $"{Path.GetFileNameWithoutExtension(name)}-page-{page.Number}-line-{lineNumber}.json";
                await outputClient.UploadBlobAsync(fileName, new BinaryData(line));
            }
        }
    }
}