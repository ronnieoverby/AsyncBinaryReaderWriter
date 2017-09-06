using System;
using System.IO;

namespace Overby.Extensions.AsyncBinaryReaderWriter
{
    internal static class __Error
    {
        internal static void EndOfFile()
        {
            throw new EndOfStreamException();
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, nameof(FileNotOpen));
        }
    }
}