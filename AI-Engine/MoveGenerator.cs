using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    /// <summary>
    /// Main class for move generation. 
    /// It generates legal moves for a given position.
    /// </summary>
    public static class MoveGenerator
    {
        public static readonly ulong[] KnightMoves = new ulong[64];
        public static readonly ulong[] KingMoves = new ulong[64];
        public static readonly ulong[] PawnAttacksWhite = new ulong[64];
        public static readonly ulong[] PawnAttacksBlack = new ulong[64];
        public static readonly ulong[] RookMoves = new ulong[64];
        public static readonly ulong[] BishopMoves = new ulong[64];
        public static readonly ulong[] QueenMoves = new ulong[64];

        public static readonly ulong NotAFile = 0xFEFEFEFEFEFEFEFEUL;
        public static readonly ulong NotHFile = 0x7F7F7F7F7F7F7F7FUL;

        static MoveGenerator()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong mask = 1UL << square;

                int x = square % 8;
                int y = square / 8;
                ulong moves = 0;

                int[] dx = { 2, 1, -1, -2, -2, -1, 1, 2 };
                int[] dy = { 1, 2, 2, 1, -1, -2, -2, -1 };

                for (int i = 0; i < 8; i++)
                {
                    int newX = x + dx[i];
                    int newY = y + dy[i];

                    if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
                    {
                        moves |= 1UL << (newY * 8 + newX);
                    }
                }

                KnightMoves[square] = moves;
                moves = 0;
                int[]kingdx = { -1, 0, 1, -1, 1, -1, 0, 1 };
                int[]kingdy = { -1, -1, -1, 0, 0, 1, 1, 1 };

                for (int i = 0; i < 8; i++)
                {
                    int newX = x + kingdx[i];
                    int newY = y + kingdy[i];

                    if (newX >= 0 && newX < 8 && newY >= 0 && newY < 8)
                    {
                        moves |= 1UL << (newY * 8 + newX);
                    }
                }

                KingMoves[square] = moves;

                PawnAttacksWhite[square] = ((mask & NotAFile) << 7) | ((mask & NotHFile) << 9);
                PawnAttacksBlack[square] = ((mask & NotAFile) >> 9) | ((mask & NotHFile) >> 7);

                ulong rook = 0;
                for (int i = 1; i < 8; i++)
                    if (square + i * 8 < 64) rook |= 1UL << (square + i * 8);
                for (int i = 1; i < 8; i++)
                    if (square - i * 8 >= 0) rook |= 1UL << (square - i * 8);
                for (int i = 1; i < 8 && (square % 8) + i < 8; i++)
                    rook |= 1UL << (square + i);
                for (int i = 1; i < 8 && (square % 8) - i >= 0; i++)
                    rook |= 1UL << (square - i);
                RookMoves[square] = rook;

                ulong bishop = 0;
                for (int i = 1; i < 8 && square + i * 9 < 64 && (square % 8) + i < 8; i++)
                    bishop |= 1UL << (square + i * 9);
                for (int i = 1; i < 8 && square + i * 7 < 64 && (square % 8) - i >= 0; i++)
                    bishop |= 1UL << (square + i * 7);
                for (int i = 1; i < 8 && square - i * 7 >= 0 && (square % 8) + i < 8; i++)
                    bishop |= 1UL << (square - i * 7);
                for (int i = 1; i < 8 && square - i * 9 >= 0 && (square % 8) - i >= 0; i++)
                    bishop |= 1UL << (square - i * 9);
                BishopMoves[square] = bishop;

                QueenMoves[square] = rook | bishop;
            }
            
        }
        public static List<Move> GenerateLegalMoves(Board board, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            bool isWhite = board.IsWhiteTurn;
            ulong kingMask = isWhite ? board.WhiteKings : board.BlackKings;
            int kingSquare = BitOperations.TrailingZeroCount(kingMask);

            ulong attackers = GetAttackers(board, kingSquare, !isWhite);
            int checkCount = BitOperations.PopCount(attackers);

            ulong pinnedPieces = ChessUtils.GetPinnedPieces(board, isWhite);

            ulong attacked = GetAttackedSquares(board, !isWhite);

            if (checkCount == 0)
            {
                if (!onlyCaptures)
                {
                    moves.AddRange(GenerateCastlingMoves(board, attacked));
                }
                moves.AddRange(GenerateKingMoves(board, attacked, onlyCaptures));
                moves.AddRange(GeneratePawnMoves(board, pinnedPieces, ulong.MaxValue, false, onlyCaptures));
                moves.AddRange(GenerateKnightMoves(board, pinnedPieces, ulong.MaxValue, false, onlyCaptures));
                moves.AddRange(GenerateBishopMoves(board, pinnedPieces, ulong.MaxValue, false, onlyCaptures));
                moves.AddRange(GenerateRookMoves(board, pinnedPieces, ulong.MaxValue, false, onlyCaptures));
                moves.AddRange(GenerateQueenMoves(board, pinnedPieces, ulong.MaxValue, false, onlyCaptures));
            }
            else if (checkCount == 1)
            {
                ulong evasionMask = GetEvasionMask(kingSquare, attackers);
                moves.AddRange(GenerateKingMoves(board, attacked, onlyCaptures));
                moves.AddRange(GeneratePawnMoves(board, pinnedPieces, evasionMask, true, onlyCaptures));
                moves.AddRange(GenerateKnightMoves(board, pinnedPieces, evasionMask, true, onlyCaptures));
                moves.AddRange(GenerateBishopMoves(board, pinnedPieces, evasionMask, true, onlyCaptures));
                moves.AddRange(GenerateRookMoves(board, pinnedPieces, evasionMask, true, onlyCaptures));
                moves.AddRange(GenerateQueenMoves(board, pinnedPieces, evasionMask, true, onlyCaptures));
            }
            else
            {
                moves.AddRange(GenerateKingMoves(board, attacked, onlyCaptures));
            }

            return moves;
        }

        private static ulong GetEvasionMask(int kingSquare, ulong attackers)
        {
            int attackerSquare = BitOperations.TrailingZeroCount(attackers);
            PinDirection dir = ChessUtils.GetPinDirection(kingSquare, attackerSquare);

            if (dir != PinDirection.None)
                return ChessUtils.GetPinRay(kingSquare, attackerSquare) | attackers;
            else
                return attackers;
        }


        public static List<Move> GenerateKingMoves(Board board, ulong attacked, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();

            int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? board.WhiteKings : board.BlackKings);
            ulong ownPieces = board.IsWhiteTurn ? board.WhitePieces : board.BlackPieces;
            ulong targets = KingMoves[kingSquare] & ~ownPieces & ~attacked;
            if (onlyCaptures)
            {
                ulong opponentPieces = board.IsWhiteTurn ? board.BlackPieces : board.WhitePieces;
                targets &= opponentPieces;
            }
            while (targets != 0)
            {
                int toSquare = BitOperations.TrailingZeroCount(targets);
                moves.Add(new Move(kingSquare, toSquare));
                targets &= targets - 1;
            }

            return moves;
        }
        public static List<Move> GenerateKnightMoves(Board board, ulong pinned, ulong validSquares = ulong.MaxValue, bool isIncheck = false, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            ulong knights = board.IsWhiteTurn ? board.WhiteKnights : board.BlackKnights;
            ulong ownPieces = board.IsWhiteTurn ? board.WhitePieces : board.BlackPieces;

            while (knights != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(knights);
                ulong fromMask = 1UL << fromSquare;
                if ((pinned & fromMask) != 0)
                {
                    knights &= knights - 1;
                    continue;
                }
                ulong targets = KnightMoves[fromSquare] & ~ownPieces & validSquares;
                if (onlyCaptures)
                {
                    ulong opponentPieces = board.IsWhiteTurn ? board.BlackPieces : board.WhitePieces;
                    targets &= opponentPieces;
                }
                while (targets != 0)
                {
                    int toSquare = BitOperations.TrailingZeroCount(targets);
                    moves.Add(new Move(fromSquare, toSquare, MoveFlag.None, PieceType.Knight));
                    targets &= targets - 1;
                }
                knights &= knights - 1;


            }

            return moves;
        }
        public static List<Move> GeneratePawnMoves(Board board, ulong pinned, ulong validSquares = ulong.MaxValue, bool isInCheck = false, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            ulong pawns = board.IsWhiteTurn ? board.WhitePawns : board.BlackPawns;
            ulong opponentPieces = board.IsWhiteTurn ? board.BlackPieces : board.WhitePieces;

            while (pawns != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(pawns);
                ulong fromMask = 1UL << fromSquare;

                if ((pinned & fromMask) != 0)
                {
                    if(isInCheck)
                    {
                        pawns &= pawns - 1;
                        continue;
                    }
                    int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? board.WhiteKings : board.BlackKings);
                    PinDirection direction = ChessUtils.GetPinDirection(kingSquare, fromSquare);

                    switch (direction)
                    {
                        case PinDirection.Vertical:
                            if (onlyCaptures)
                            {
                                pawns &= pawns - 1;
                                continue;
                            }
                            int oneStep = fromSquare + (board.IsWhiteTurn ? 8 : -8);
                            if (oneStep >= 0 && oneStep < 64 && (board.OccupiedSquares & (1UL << oneStep)) == 0)
                            {
                                if (BoardHelper.IsBitOne(validSquares, oneStep))
                                    moves.AddRange(AddPawnMove(fromSquare, oneStep));

                                int fromRank = fromSquare / 8;
                                if ((board.IsWhiteTurn && fromRank == 1) || (!board.IsWhiteTurn && fromRank == 6))
                                {
                                    int twoStep = fromSquare + (board.IsWhiteTurn ? 16 : -16);
                                    if ((board.OccupiedSquares & (1UL << twoStep)) == 0 && BoardHelper.IsBitOne(validSquares, twoStep))
                                        moves.Add(new Move(fromSquare, twoStep, MoveFlag.DoublePawnPush, PieceType.Pawn));
                                }
                            }
                            break;

                        case PinDirection.Diagonal:
                            ulong attacks = board.IsWhiteTurn ? PawnAttacksWhite[fromSquare] : PawnAttacksBlack[fromSquare];
                            ulong allowedCaptures = attacks & opponentPieces & ChessUtils.GetPinRayWhole(fromSquare, kingSquare);

                            while (allowedCaptures != 0)
                            {
                                int captureSquare = BitOperations.TrailingZeroCount(allowedCaptures);
                                moves.AddRange(AddPawnMove(fromSquare, captureSquare));
                                allowedCaptures &= allowedCaptures - 1;
                            }
                            break;

                        default:
                            break;
                    }

                    pawns &= pawns - 1;
                    continue;
                }

                int rank = fromSquare / 8;
                int oneStepNormal = fromSquare + (board.IsWhiteTurn ? 8 : -8);
                if (!onlyCaptures)
                {
                    if (oneStepNormal >= 0 && oneStepNormal < 64 && (board.OccupiedSquares & (1UL << oneStepNormal)) == 0)
                    {
                        if (BoardHelper.IsBitOne(validSquares, oneStepNormal))
                            moves.AddRange(AddPawnMove(fromSquare, oneStepNormal));

                        if ((board.IsWhiteTurn && rank == 1) || (!board.IsWhiteTurn && rank == 6))
                        {
                            int twoStepNormal = fromSquare + (board.IsWhiteTurn ? 16 : -16);
                            if ((board.OccupiedSquares & (1UL << twoStepNormal)) == 0 && BoardHelper.IsBitOne(validSquares, twoStepNormal))
                                moves.Add(new Move(fromSquare, twoStepNormal, MoveFlag.DoublePawnPush, PieceType.Pawn));
                        }
                    }

                }
                
                
                ulong pawnAttacks = board.IsWhiteTurn ? PawnAttacksWhite[fromSquare] : PawnAttacksBlack[fromSquare];
                ulong captures = pawnAttacks & opponentPieces & validSquares;
                while (captures != 0)
                {
                    int targetSquare = BitOperations.TrailingZeroCount(captures);
                    moves.AddRange(AddPawnMove(fromSquare, targetSquare));
                    captures &= captures - 1;
                }

                if (board.EnPassantSquare != -1)
                {
                    ulong enPassantMask = 1UL << board.EnPassantSquare;
                    if ((pawnAttacks & enPassantMask) != 0 && IsEnPassantSafe(board, fromSquare, board.EnPassantSquare))
                    {
                        moves.Add(new Move(fromSquare, board.EnPassantSquare, MoveFlag.EnPassant, PieceType.Pawn));
                    }
                }

                pawns &= pawns - 1;
            }

            return moves;
        }

        private static List<Move> AddPawnMove(int fromSquare, int toSquare)
        {
            List<Move> moves = new List<Move>();
            if (toSquare >= 56 || toSquare <= 7)
            {
                moves.Add(new Move(fromSquare, toSquare, MoveFlag.PromotionToQueen, PieceType.Pawn));
                moves.Add(new Move(fromSquare, toSquare, MoveFlag.PromotionToRook, PieceType.Pawn));
                moves.Add(new Move(fromSquare, toSquare, MoveFlag.PromotionToBishop, PieceType.Pawn));
                moves.Add(new Move(fromSquare, toSquare, MoveFlag.PromotionToKnight, PieceType.Pawn));
            }
            else
            {
                moves.Add(new Move(fromSquare, toSquare, MoveFlag.None, PieceType.Pawn));
            }
            return moves;
        }

        private static bool IsEnPassantSafe(Board board, int fromSquare, int epSquare)
        {
            var tempBoard = new Board
            {
                WhitePawns = board.WhitePawns,
                BlackPawns = board.BlackPawns,
                WhiteKnights = board.WhiteKnights,
                BlackKnights = board.BlackKnights,
                WhiteBishops = board.WhiteBishops,
                BlackBishops = board.BlackBishops,
                WhiteRooks = board.WhiteRooks,
                BlackRooks = board.BlackRooks,
                WhiteQueens = board.WhiteQueens,
                BlackQueens = board.BlackQueens,
                WhiteKings = board.WhiteKings,
                BlackKings = board.BlackKings,
                IsWhiteTurn = board.IsWhiteTurn
            };

            if (board.IsWhiteTurn)
            {
                tempBoard.WhitePawns ^= 1UL << fromSquare;
                tempBoard.WhitePawns |= 1UL << epSquare;      
                tempBoard.BlackPawns ^= 1UL << (epSquare - 8);   
            }
            else
            {
                tempBoard.BlackPawns ^= 1UL << fromSquare;
                tempBoard.BlackPawns |= 1UL << epSquare;
                tempBoard.WhitePawns ^= 1UL << (epSquare + 8);
            }

            int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? tempBoard.WhiteKings : tempBoard.BlackKings);

            return !IsSquareAttacked(tempBoard, !board.IsWhiteTurn, kingSquare);
        }

        public static List<Move> GenerateBishopMoves(Board board, ulong pinnedPieces, ulong validSquares = ulong.MaxValue, bool isInCheck = false, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            ulong bishops = board.IsWhiteTurn ? board.WhiteBishops : board.BlackBishops;
            ulong ownPieces = board.IsWhiteTurn ? board.WhitePieces : board.BlackPieces;
            ulong occupied = board.OccupiedSquares;
            ulong pinRay;
            int[] directions = { 9, -9, 7, -7 };
            if (onlyCaptures)
            {
                validSquares &= (occupied & ~ownPieces);
            }
            while (bishops != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(bishops);
                ulong fromMask = 1UL << fromSquare;
                pinRay = ulong.MaxValue;
                if ((pinnedPieces & fromMask) != 0)
                {
                    if (isInCheck)
                    {
                        bishops &= bishops - 1;
                        continue;
                    }
                    
                    int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? board.WhiteKings : board.BlackKings);
                    pinRay = ChessUtils.GetPinRayWhole(kingSquare, fromSquare);
                    
                }
                
                    int fromRank = fromSquare / 8;
                    int fromFile = fromSquare % 8; 

                    foreach (int dir in directions)
                    {
                        int currentSquare = fromSquare;
                        while (true)
                        {
                            currentSquare += dir;
                            if (currentSquare < 0 || currentSquare >= 64) break;

                            int toRank = currentSquare / 8;
                            int toFile = currentSquare % 8;

                            if (dir == 9 || dir == -9)
                            {
                                if (toRank - toFile != fromRank - fromFile) break;
                            }
                            else if (dir == 7 || dir == -7)
                            {
                                if (toRank + toFile != fromRank + fromFile) break;
                            }
                            
                            ulong toMask = 1UL << currentSquare;
                            if ((ownPieces & toMask) != 0) break;
                            if ((validSquares & toMask & pinRay) != 0)
                            {
                                moves.Add(new Move(fromSquare, currentSquare, MoveFlag.None, PieceType.Bishop));
                            }
                            if ((occupied & toMask) != 0) break;
                        }
                    }
                

                bishops &= bishops - 1;
            }

            return moves;
        }
        public static List<Move> GenerateRookMoves(Board board, ulong pinnedPieces, ulong validSquares = ulong.MaxValue, bool isInCheck = false, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            ulong rooks = board.IsWhiteTurn ? board.WhiteRooks : board.BlackRooks;
            ulong ownPieces = board.IsWhiteTurn ? board.WhitePieces : board.BlackPieces;
            ulong occupied = board.OccupiedSquares;
            ulong pinRay;
            int[] directions = { 8, -8, 1, -1 };
            if (onlyCaptures)
            {
                validSquares &= (occupied & ~ownPieces);
            }
            while (rooks != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(rooks);
                ulong fromMask = 1UL << fromSquare;
                pinRay = ulong.MaxValue;
                if ((pinnedPieces & fromMask) != 0)
                {
                    if (isInCheck)
                    {
                        rooks &= rooks - 1;
                        continue;
                    }
                    int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? board.WhiteKings : board.BlackKings);
                    pinRay = ChessUtils.GetPinRayWhole(kingSquare, fromSquare);

                }
                
                    foreach (int dir in directions)
                    {
                        int currentSquare = fromSquare;
                        while (true)
                        {
                            currentSquare += dir;
                            if (currentSquare < 0 || currentSquare >= 64) break;
                            if (dir == 1 && currentSquare % 8 == 0) break;
                            if (dir == -1 && currentSquare % 8 == 7) break;

                            ulong toMask = 1UL << currentSquare;
                            if ((ownPieces & toMask) != 0) break;
                            
                        if ((validSquares & toMask & pinRay) != 0) 
                            {
                                moves.Add(new Move(fromSquare, currentSquare, MoveFlag.None, PieceType.Rook));
                            }
                            if ((occupied & toMask) != 0) break;
                        }
                    }

                rooks &= rooks - 1;
                }

            
            

            return moves;
        }
        public static List<Move> GenerateQueenMoves(Board board, ulong pinnedPieces, ulong validSquares = ulong.MaxValue, bool isInCheck = false, bool onlyCaptures = false)
        {
            List<Move> moves = new List<Move>();
            ulong queens = board.IsWhiteTurn ? board.WhiteQueens : board.BlackQueens;
            ulong ownPieces = board.IsWhiteTurn ? board.WhitePieces : board.BlackPieces;
            ulong occupied = board.OccupiedSquares;
            ulong pinRay;
            int[] directions = { 8, -8, 1, -1, 9, -9, 7, -7 };
            if (onlyCaptures)
            {
                validSquares &= (occupied & ~ownPieces);
            }
            while (queens != 0)
            {
                int fromSquare = BitOperations.TrailingZeroCount(queens);
                ulong fromMask = 1UL << fromSquare;
                pinRay = ulong.MaxValue;

                if ((pinnedPieces & fromMask) != 0)
                {
                    if (isInCheck)
                    {
                        queens &= queens - 1;
                        continue;
                    }
                    int kingSquare = BitOperations.TrailingZeroCount(board.IsWhiteTurn ? board.WhiteKings : board.BlackKings);
                    pinRay = ChessUtils.GetPinRayWhole(kingSquare, fromSquare);

                    
                    
                }
                    int fromRank = fromSquare / 8;
                    int fromFile = fromSquare % 8;

                foreach (int dir in directions)
                {
                    int currentSquare = fromSquare;
                    while (true)
                    {
                        currentSquare += dir;
                        if (currentSquare < 0 || currentSquare >= 64) break;

                        int toRank = currentSquare / 8;
                        int toFile = currentSquare % 8;

                        if (dir == 1 || dir == -1) 
                        {
                            if (toRank != fromRank) break; 
                        }
                        else if (dir == 8 || dir == -8)
                        {
                            if (toFile != fromFile) break;
                        }
                        else if (dir == 9 || dir == -9)
                        {
                            if (toRank - toFile != fromRank - fromFile) break;
                        }
                        else if (dir == 7 || dir == -7)
                        {
                            if (toRank + toFile != fromRank + fromFile) break;
                        }

                        ulong toMask = 1UL << currentSquare;
                        if ((ownPieces & toMask) != 0) break;
                        
                        if ((validSquares & toMask & pinRay) != 0)
                        {
                            moves.Add(new Move(fromSquare, currentSquare, MoveFlag.None, PieceType.Queen));
                        }
                        if ((occupied & toMask) != 0) break; 
                    }
                }
                

                queens &= queens - 1;
            }

            return moves;
        }
        public static ulong GetAttackers(Board board, int square, bool attackerIsWhite)
        {
            ulong attackers = 0;
            ulong occupied = board.OccupiedSquares;

            ulong rookQueens = attackerIsWhite ? board.WhiteRooks | board.WhiteQueens : board.BlackRooks | board.BlackQueens;
            int[] straightDirs = { 8, -8, 1, -1 };

            foreach (int dir in straightDirs)
            {
                int currentSquare = square;
                while (true)
                {
                    currentSquare += dir;
                    if (currentSquare < 0 || currentSquare >= 64) break;
                    if (dir == 1 && currentSquare % 8 == 0) break;
                    if (dir == -1 && currentSquare % 8 == 7) break;

                    ulong mask = 1UL << currentSquare;
                    if ((occupied & mask) != 0)
                    {
                        if ((rookQueens & mask) != 0) attackers |= mask;
                        break;
                    }
                }
            }

            ulong bishopQueens = attackerIsWhite ? board.WhiteBishops | board.WhiteQueens : board.BlackBishops | board.BlackQueens;
            int[] diagonalDirs = { 9, -9, 7, -7 };

            foreach (int dir in diagonalDirs)
            {
                int currentSquare = square;
                while (true)
                {
                    currentSquare += dir;
                    if (currentSquare < 0 || currentSquare >= 64) break;

                    int origRank = square / 8, origFile = square % 8;
                    int currRank = currentSquare / 8, currFile = currentSquare % 8;

                    if (Math.Abs(origRank - currRank) != Math.Abs(origFile - currFile)) break;

                    ulong mask = 1UL << currentSquare;
                    if ((occupied & mask) != 0)
                    {
                        if ((bishopQueens & mask) != 0) attackers |= mask;
                        break;
                    }
                }
            }

            attackers |= (attackerIsWhite ? board.WhiteKnights : board.BlackKnights) & KnightMoves[square];
            attackers |= (attackerIsWhite
                ? board.WhitePawns & PawnAttacksBlack[square]
                : board.BlackPawns & PawnAttacksWhite[square]);

            attackers |= (attackerIsWhite ? board.WhiteKings : board.BlackKings) & KingMoves[square];

            return attackers;
        }
        public static ulong GetAttackedSquares(Board board, bool byWhite)
        {
            ulong attacked = 0;

            ulong pawns = byWhite ? board.WhitePawns : board.BlackPawns;
            ulong knights = byWhite ? board.WhiteKnights : board.BlackKnights;
            ulong bishops = byWhite ? board.WhiteBishops : board.BlackBishops;
            ulong rooks = byWhite ? board.WhiteRooks : board.BlackRooks;
            ulong queens = byWhite ? board.WhiteQueens : board.BlackQueens;
            ulong king = byWhite ? board.WhiteKings : board.BlackKings;
            ulong opponentKing = byWhite ? board.BlackKings : board.WhiteKings;
            ulong occupied = board.OccupiedSquares & ~opponentKing;

            while (pawns != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawns);
                attacked |= byWhite ? PawnAttacksWhite[square] : PawnAttacksBlack[square];
                pawns &= pawns - 1;
            }

            while (knights != 0)
            {
                int square = BitOperations.TrailingZeroCount(knights);
                attacked |= KnightMoves[square];
                knights &= knights - 1;
            }

            int kingSquare = BitOperations.TrailingZeroCount(king);
            attacked |= KingMoves[kingSquare];

            ulong bishopQueens = bishops | queens;
            int[] bishopDirections = { 9, -9, 7, -7 };
            while (bishopQueens != 0)
            {
                int square = BitOperations.TrailingZeroCount(bishopQueens);
                foreach (int dir in bishopDirections)
                {
                    int currentSquare = square;
                    while (true)
                    {
                        currentSquare += dir;
                        if (currentSquare < 0 || currentSquare >= 64) break;
                        int toRank = currentSquare / 8;
                        int toFile = currentSquare % 8;
                        int fromRank = square / 8;
                        int fromFile = square % 8;
                        if (dir == 9 && toRank - toFile != fromRank - fromFile) break;
                        if (dir == -9 && toRank - toFile != fromRank - fromFile) break;
                        if (dir == 7 && toRank + toFile != fromRank + fromFile) break;
                        if (dir == -7 && toRank + toFile != fromRank + fromFile) break;

                        ulong toMask = 1UL << currentSquare;
                        attacked |= toMask;
                        if ((occupied & toMask) != 0) break;
                    }
                }
                bishopQueens &= bishopQueens - 1;
            }
            ulong rookQueens = rooks | queens;
            int[] rookDirections = { 8, -8, 1, -1 };
            while (rookQueens != 0)
            {
                int square = BitOperations.TrailingZeroCount(rookQueens);
                foreach (int dir in rookDirections)
                {
                    int currentSquare = square;
                    while (true)
                    {
                        currentSquare += dir;
                        if (currentSquare < 0 || currentSquare >= 64) break;
                        if (dir == 1 && currentSquare % 8 == 0) break;
                        if (dir == -1 && currentSquare % 8 == 7) break; 

                        ulong toMask = 1UL << currentSquare;
                        attacked |= toMask;
                        if ((occupied & toMask) != 0) break; 
                    }
                }
                rookQueens &= rookQueens - 1;
            }
            return attacked;
        }
        public static bool IsSquareAttacked(Board board, bool byWhite, int square)
        {
            return (GetAttackedSquares(board, byWhite) & (1UL << square)) != 0;                
        }

        public static List<Move> GenerateCastlingMoves(Board board, ulong attacked)
        {
            List<Move> moves = new List<Move>();
            bool isWhite = board.IsWhiteTurn;
            int kingSquare = BitOperations.TrailingZeroCount(isWhite ? board.WhiteKings : board.BlackKings);

            if (isWhite && kingSquare == 4)
            {
                if ((board.CastlingRights & 0b0001) != 0 &&                    
                    (board.OccupiedSquares & ((1UL << 5) | (1UL << 6))) == 0 &&
                    (attacked & ((1UL << 5) | (1UL << 6))) == 0)               
                {
                    moves.Add(new Move(4, 6, MoveFlag.CastlingKingSide));
                }

                if ((board.CastlingRights & 0b0010) != 0 &&                          
                    (board.OccupiedSquares & ((1UL << 3) | (1UL << 2) | (1UL << 1))) == 0 && 
                    (attacked & ((1UL << 3) | (1UL << 2))) == 0)                     
                {
                    moves.Add(new Move(4, 2, MoveFlag.CastlingQueenSide));
                }
            }
            else if (!isWhite && kingSquare == 60)
            {
                if ((board.CastlingRights & 0b0100) != 0 &&                       
                    (board.OccupiedSquares & ((1UL << 61) | (1UL << 62))) == 0 && 
                    (attacked & ((1UL << 61) | (1UL << 62))) == 0)                 
                {
                    moves.Add(new Move(60, 62, MoveFlag.CastlingKingSide));
                }

                if ((board.CastlingRights & 0b1000) != 0 &&                           
                    (board.OccupiedSquares & ((1UL << 59) | (1UL << 58) | (1UL << 57))) == 0 && 
                    (attacked & ((1UL << 59) | (1UL << 58))) == 0)
                {
                    moves.Add(new Move(60, 58, MoveFlag.CastlingQueenSide));
                }
            }

            return moves;
        }

    }
}
