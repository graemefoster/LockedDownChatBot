using System.Collections.Generic;
using System.IO;
using UglyToad.PdfPig;

namespace ContentSplitter;

public class CrackPdf : ICrack
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async IAsyncEnumerable<string> Crack(Stream documentStream)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        using var document = PdfDocument.Open(documentStream);
        foreach (var page in document.GetPages())
        {
            foreach (var word in page.GetWords())
            {
                yield return word.Text;
            }
        }
    }
}