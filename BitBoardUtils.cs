using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    /// <summary>
    /// This replaces the built-in class BitOperations.
    /// 
    /// </summary>
    public static class BitBoardUtils
    {
        private const ulong deBruijn64 = 0x37E84A99DAE458F;
        private static readonly int[] deBruijnTable =
        {
            0, 1, 17, 2, 18, 50, 3, 57,
            47, 19, 22, 51, 29, 4, 33, 58,
            15, 48, 20, 27, 25, 23, 52, 41,
            54, 30, 38, 5, 43, 34, 59, 8,
            63, 16, 49, 56, 46, 21, 28, 32,
            14, 26, 24, 40, 53, 37, 42, 7,
            62, 55, 45, 31, 13, 39, 36, 6,
            61, 44, 12, 35, 60, 11, 10, 9
        };
        private static readonly byte[] BitCountTable = new byte[256];

        static BitBoardUtils() // Lookup table
        {
            for (int i = 0; i < 256; i++)
                BitCountTable[i] = (byte)((i & 1) + BitCountTable[i >> 1]);
        }

        public static int PopCount(ulong bitboard)
        {
            int count = 0;
            for (int i = 0; i < 8; i++)
            {
                count += BitCountTable[(bitboard >> (i * 8)) & 0xFF];
            }
            return count;
        }
        public static int TrailingZeroCount(ulong x)
        {
            if (x == 0) return 64;
            ulong isolatedLSB = x & (ulong)(-(long)x); // Isolates LSB
            int index = deBruijnTable[(isolatedLSB * deBruijn64) >> 58];
            return index;
        }



    }
}
