using System;
using System.Runtime.InteropServices;

namespace DataStructuresFsConsoleApp.Common
{
    public static class BufferUtil
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] xBytes, byte[] yBytes, long count);

        public static bool ReadBool(byte[] bytes, int position)
        {
            return (bytes[position] != 0);
        }
        public static byte ReadByte(byte[] bytes, int position)
        {
            return bytes[position];
        }
        public static int ReadInt(byte[] bytes, int position)
        {
            return BitConverter.ToInt32(bytes, position);
        }
        public static long ReadLong(byte[] bytes, int position)
        {
            return BitConverter.ToInt64(bytes, position);
        }

        public static void Write(byte[] bytes, int position, bool value)
        {
            bytes[position] = (byte)(value ? 1 : 0);
        }
        public static void Write(byte[] bytes, int position, byte value)
        {
            bytes[position] = value;
        }
        public static void Write(byte[] bytes, int position, int value)
        {
            bytes[position++] = (byte)value;
            bytes[position++] = (byte)(value >> 8);
            bytes[position++] = (byte)(value >> 16);
            bytes[position] = (byte)(value >> 24);
        }
        public static void Write(byte[] bytes, int position, long value)
        {
            bytes[position++] = (byte)value;
            bytes[position++] = (byte)(value >> 8);
            bytes[position++] = (byte)(value >> 16);
            bytes[position++] = (byte)(value >> 24);
            bytes[position++] = (byte)(value >> 32);
            bytes[position++] = (byte)(value >> 40);
            bytes[position++] = (byte)(value >> 48);
            bytes[position] = (byte)(value >> 56);
        }

        public static int ByteArrayCompare(byte[] x, byte[] y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            var xLen = GetLength(x);
            var yLen = GetLength(y);

            var order = xLen.CompareTo(yLen);
            if (order != 0)
                return order;

            return memcmp(x, y, xLen);
        }

        public static bool ByteArrayEquals(byte[] x, byte[] y)
        {
            return ByteArrayCompare(x, y) == 0;
        }

        public static int GetLength(byte[] bytes)
        {
            if (bytes == null)
                return -1;

            return bytes.Length;
        }
    }
}
