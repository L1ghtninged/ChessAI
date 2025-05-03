using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    /// <summary>
    /// Static class, which contains some helpful methods for board logic.
    /// </summary>
    public static class BoardHelper
    {
        public static bool IsInBounds(int square)
        {
            return square >= 0 && square < 64;
        }
        public static ulong SetBit(ulong bitboard, int square)
        {
            return bitboard | (1UL << square);
        }

        public static ulong ClearBit(ulong bitboard, int square)
        {
            return bitboard & ~(1UL << square);
        }
        public static bool IsBitOne(ulong bitboard, int square)
        {
            return (bitboard & (1UL << square)) != 0;
        }
        public static int GetRank(int index)
        {
            return index / 8;
        }
        public static int GetFile(int index)
        {
            return index - 8 * GetRank(index);
        }
        public static int GetIndex(int file, int rank)
        {
            return 8 * rank + file;
        }
        public static int CountBits(ulong bitboard)
        {
            return BitOperations.PopCount(bitboard);
        }

        public static bool WhiteKingSideCastle(int flag)
        {
            return (flag & (1 << 0)) != 0;
        }
        public static bool WhiteQueenSideCastle(int flag)
        {
            return (flag & (1 << 1)) != 0;
        }
        public static bool BlackKingSideCastle(int flag)
        {
            return (flag & (1 << 2)) != 0;
        }
        public static bool BlackQueenSideCastle(int flag)
        {
            return (flag & (1 << 3)) != 0;
        }
        public static bool IsSquareOccupied(Board board, int square)
        {
            return (board.OccupiedSquares & (1UL << square)) != 0;
        }
        



    }
}
