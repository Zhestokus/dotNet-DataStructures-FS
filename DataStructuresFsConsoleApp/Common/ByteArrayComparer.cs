using System.Collections.Generic;

namespace DataStructuresFsConsoleApp.Common
{
    public class ByteArrayComparer : IComparer<byte[]>
    {
        public int Compare(byte[] x, byte[] y)
        {
            return BufferUtil.ByteArrayCompare(x, y);
        }
    }
}
