using System.Collections.Generic;
using System.IO;

namespace ContentSplitter;

public interface ICrack
{
    IAsyncEnumerable<string> Crack(Stream documentStream);
}