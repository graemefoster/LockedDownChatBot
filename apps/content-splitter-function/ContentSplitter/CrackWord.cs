using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ContentSplitter;

public class CrackWord : ICrack
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<string> Crack(Stream documentStream)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        using var reader = WordprocessingDocument.Open(documentStream, false);
        foreach (var paragraph in reader.MainDocumentPart?.Document.Body?.Elements<Paragraph>() ??
                                  Enumerable.Empty<Paragraph>())
        {
            foreach (var word in paragraph.InnerText.Split(' '))
            {
                yield return word;
            }
        }
    }
}