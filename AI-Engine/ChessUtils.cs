using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    public enum PinDirection
    {
        None,        
        Horizontal, 
        Vertical,   
        Diagonal     
    }
    /// <summary>
    /// Static class, which handles all pin-logic. 
    /// </summary>
    public class ChessUtils
    {
        public static ulong fileAmask = 0x0101010101010101;
        public static ulong rank1mask = 0xFF;

        public static ulong GetPinnedPieces(Board board, bool isWhite)
        {
            ulong pinned = 0;
            ulong kingMask = isWhite ? board.WhiteKings : board.BlackKings;
            if (kingMask == 0) return 0;

            int kingSquare = BitOperations.TrailingZeroCount(kingMask);
            ulong ownPieces = isWhite ? board.WhitePieces : board.BlackPieces;
            ulong opponentPieces = isWhite ? board.BlackPieces : board.WhitePieces;

            var directions = new (int dx, int dy, ulong rookQueens, ulong bishopQueens)[]
            {
            (1, 0, isWhite ? board.BlackRooks | board.BlackQueens : board.WhiteRooks | board.WhiteQueens, 0), 
            (-1, 0, isWhite ? board.BlackRooks | board.BlackQueens : board.WhiteRooks | board.WhiteQueens, 0), 
            (0, 1, isWhite ? board.BlackRooks | board.BlackQueens : board.WhiteRooks | board.WhiteQueens, 0),  
            (0, -1, isWhite ? board.BlackRooks | board.BlackQueens : board.WhiteRooks | board.WhiteQueens, 0), 
            (1, 1, 0, isWhite ? board.BlackBishops | board.BlackQueens : board.WhiteBishops | board.WhiteQueens), 
            (1, -1, 0, isWhite ? board.BlackBishops | board.BlackQueens : board.WhiteBishops | board.WhiteQueens),
            (-1, 1, 0, isWhite ? board.BlackBishops | board.BlackQueens : board.WhiteBishops | board.WhiteQueens), 
            (-1, -1, 0, isWhite ? board.BlackBishops | board.BlackQueens : board.WhiteBishops | board.WhiteQueens)  
            };

            foreach (var dir in directions)
            {

                int x = kingSquare % 8 + dir.dx;
                int y = kingSquare / 8 + dir.dy;
                int pinnedSquare = -1;

                while (x >= 0 && x < 8 && y >= 0 && y < 8)
                {
                    int currentSquare = y * 8 + x;
                    ulong currentMask = 1UL << currentSquare;

                    if ((ownPieces & currentMask) != 0)
                    {
                        if (pinnedSquare == -1)
                        {
                            pinnedSquare = currentSquare;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if ((opponentPieces & currentMask) != 0)
                    {
                        bool isPinner = (dir.rookQueens & currentMask) != 0 ||
                                       (dir.bishopQueens & currentMask) != 0;

                        if (isPinner && pinnedSquare != -1)
                        {
                            pinned |= 1UL << pinnedSquare;
                        }
                        break;
                    }

                    x += dir.dx;
                    y += dir.dy;
                }
            }

            return pinned;
        }

        public static ulong PossibleTargets(int square, int friendlyKing)
        {
            return GetPinRay(square, friendlyKing);
        }
        public static PinDirection GetPinDirection(int kingSquare, int pinnerSquare)
        {
            int kingRank = kingSquare / 8;   
            int kingFile = kingSquare % 8;
            int pinnerRank = pinnerSquare / 8; 
            int pinnerFile = pinnerSquare % 8; 

            if (kingRank == pinnerRank) return PinDirection.Horizontal;
            if (kingFile == pinnerFile) return PinDirection.Vertical;
            if (Math.Abs(kingRank - pinnerRank) == Math.Abs(kingFile - pinnerFile)) return PinDirection.Diagonal;
            return PinDirection.None;
        }
        public static ulong GetPinRay(int kingSquare, int pinnerSquare)
        {
            PinDirection direction = GetPinDirection(kingSquare, pinnerSquare);
            switch (direction)
            {
                case PinDirection.Horizontal:
                    return GetRankBetween(kingSquare, pinnerSquare);
                case PinDirection.Vertical:
                    return GetFileBetween(kingSquare, pinnerSquare);
                case PinDirection.Diagonal:
                    return GetDiagonalBetweenSquares(kingSquare, pinnerSquare); 
                default:
                    return 0;
            }
        }
        public static ulong GetPinRayWhole(int square1, int square2)
        {
            PinDirection direction = GetPinDirection(square1, square2);
            if (direction == PinDirection.None)
                return 0;

            int x1 = square1 % 8;
            int y1 = square1 / 8;
            int x2 = square2 % 8;
            int y2 = square2 / 8;

            int dx = Math.Sign(x2 - x1);
            int dy = Math.Sign(y2 - y1);

            ulong mask = 0;

            int x = x1;
            int y = y1;

            while (x - dx >= 0 && x - dx < 8 && y - dy >= 0 && y - dy < 8)
            {
                x -= dx;
                y -= dy;
            }
            while (x >= 0 && x < 8 && y >= 0 && y < 8)
            {
                int sq = y * 8 + x;
                mask |= 1UL << sq;

                x += dx;
                y += dy;
            }

            return mask;
        }

        public static ulong GetRankBetween(int sq1, int sq2)
        {
            int rank1 = sq1 / 8;
            int rank2 = sq2 / 8;
            if (rank1 != rank2) return 0;

            int file1 = sq1 % 8;
            int file2 = sq2 % 8;
            int startFile = Math.Min(file1, file2);
            int endFile = Math.Max(file1, file2);

            ulong mask = 0;
            for (int f = startFile; f <= endFile; f++)
            {
                mask |= 1UL << (rank1 * 8 + f);
            }
            return mask;
        }

        public static ulong GetFileBetween(int sq1, int sq2)
        {
            int file1 = sq1 % 8;
            int file2 = sq2 % 8;
            if (file1 != file2) return 0;

            int rank1 = sq1 / 8;
            int rank2 = sq2 / 8;
            int startRank = Math.Min(rank1, rank2);
            int endRank = Math.Max(rank1, rank2);

            ulong mask = 0;
            for (int r = startRank; r <= endRank; r++)
            {
                mask |= 1UL << (r * 8 + file1);
            }
            return mask;
        }

        public static ulong GetDiagonalBetweenSquares(int square1, int square2)
        {
            int x1 = square1 % 8;
            int y1 = square1 / 8;
            int x2 = square2 % 8;
            int y2 = square2 / 8;

            if (Math.Abs(x1 - x2) != Math.Abs(y1 - y2))
                return 0;

            int dx = x2 > x1 ? 1 : -1;
            int dy = y2 > y1 ? 1 : -1;

            ulong mask = 0;
            int steps = Math.Abs(x1 - x2);
            for (int i = 0; i <= steps; i++)
            {
                int x = x1 + i * dx;
                int y = y1 + i * dy;
                mask |= 1UL << (y * 8 + x);
            }
            return mask;
        }
    }
}
