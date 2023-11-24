using System.Collections.Generic;
using System.IO;

namespace ContentSplitter;

public class CrackText : ICrack
{
    public async IAsyncEnumerable<string> Crack(Stream documentStream)
    {
        //presume a stream of text:
        using var reader = new StreamReader(documentStream);
        while (!reader.EndOfStream)
        {
            var next = await reader.ReadLineAsync();
            foreach (var word in (next ?? string.Empty).Split(' '))
            {
                yield return word;
            }
        }
    }
}