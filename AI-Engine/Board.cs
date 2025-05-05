using ChessAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace ChessAIProject
{
    /// <summary>
    /// Represents the chessboard. 
    /// Uses bitboards
    /// </summary>
    public partial class Board
    {
        public Stack<BoardState> history = new Stack<BoardState>(); // This stores all states of the board during a chess game.
        public bool IsWhiteTurn = true;
        public byte CastlingRights;
        public int EnPassantSquare = -1;
        public ulong WhitePawns;
        public ulong WhiteKnights;
        public ulong WhiteBishops;
        public ulong WhiteRooks;
        public ulong WhiteQueens;
        public ulong WhiteKings;
        public ulong BlackPawns;
        public ulong BlackKnights;
        public ulong BlackBishops;
        public ulong BlackRooks;
        public ulong BlackQueens;
        public ulong BlackKings;
        public int HalfmoveClock;
        public ulong ZobristHash { get; private set; } //Zobrist hashing - Not implemented.

        public ulong OccupiedSquares => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings | BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;

        private static readonly ulong[,] ZobristPieceKeys = new ulong[12, 64];
        private static readonly ulong[] ZobristCastlingKeys = new ulong[16];
        private static readonly ulong[] ZobristEnPassantKeys = new ulong[8];
        private static readonly ulong ZobristSideKey;

        public void SetUpBoard(string fen)
        {
            string[] parts = fen.Split(' ');
            string[] ranks = parts[0].Split('/');

            int square = 56;
            foreach (string rank in ranks)
            {
                foreach (char symbol in rank)
                {
                    if (char.IsDigit(symbol))
                    {
                        square += symbol - '0';
                    }
                    else
                    {
                        PlacePiece(symbol, square);
                        square++;
                    }
                }
                square -= 16;
            }

            IsWhiteTurn = parts[1] == "w";
            CastlingRights = 0;
            if (parts[2].Contains('K')) CastlingRights |= 0b00000001;
            if (parts[2].Contains('Q')) CastlingRights |= 0b00000010;
            if (parts[2].Contains('k')) CastlingRights |= 0b00000100;
            if (parts[2].Contains('q')) CastlingRights |= 0b00001000;
            EnPassantSquare = parts[3] == "-" ? -1 : AlgebraicToSquare(parts[3]);
            HalfmoveClock = 0;

        }
        private void PlacePiece(char symbol, int square)
        {
            ulong mask = 1UL << square;
            switch (symbol)
            {
                case 'P': WhitePawns |= mask; break;
                case 'N': WhiteKnights |= mask; break;
                case 'B': WhiteBishops |= mask; break;
                case 'R': WhiteRooks |= mask; break;
                case 'Q': WhiteQueens |= mask; break;
                case 'K': WhiteKings |= mask; break;
                case 'p': BlackPawns |= mask; break;
                case 'n': BlackKnights |= mask; break;
                case 'b': BlackBishops |= mask; break;
                case 'r': BlackRooks |= mask; break;
                case 'q': BlackQueens |= mask; break;
                case 'k': BlackKings |= mask; break;
            }
        }
        public override string ToString()
        {
            StringBuilder boardDisplay = new StringBuilder();
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    char piece = GetPieceAt(square);
                    boardDisplay.Append(piece != '\0' ? piece + " " : ". ");
                }
                boardDisplay.AppendLine();
            }
            return boardDisplay.ToString();
        }
        public char GetPieceAt(int square)
        {
            ulong mask = 1UL << square;
            if ((WhitePawns & mask) != 0) return 'P';
            if ((WhiteKnights & mask) != 0) return 'N';
            if ((WhiteBishops & mask) != 0) return 'B';
            if ((WhiteRooks & mask) != 0) return 'R';
            if ((WhiteQueens & mask) != 0) return 'Q';
            if ((WhiteKings & mask) != 0) return 'K';
            if ((BlackPawns & mask) != 0) return 'p';
            if ((BlackKnights & mask) != 0) return 'n';
            if ((BlackBishops & mask) != 0) return 'b';
            if ((BlackRooks & mask) != 0) return 'r';
            if ((BlackQueens & mask) != 0) return 'q';
            if ((BlackKings & mask) != 0) return 'k';
            return '\0';
        }
        public PieceType GetPieceTypeAt(int square)
        {
            ulong mask = 1UL << square;
            if ((WhitePawns & mask) != 0) return PieceType.Pawn;
            if ((WhiteKnights & mask) != 0) return PieceType.Knight;
            if ((WhiteBishops & mask) != 0) return PieceType.Bishop;
            if ((WhiteRooks & mask) != 0) return PieceType.Rook;
            if ((WhiteQueens & mask) != 0) return PieceType.Queen;
            if ((WhiteKings & mask) != 0) return PieceType.King;
            if ((BlackPawns & mask) != 0) return PieceType.Pawn;
            if ((BlackKnights & mask) != 0) return PieceType.Knight;
            if ((BlackBishops & mask) != 0) return PieceType.Bishop;
            if ((BlackRooks & mask) != 0) return PieceType.Rook;
            if ((BlackQueens & mask) != 0) return PieceType.Queen;
            if ((BlackKings & mask) != 0) return PieceType.King;
            return PieceType.None;
        }
        public int AlgebraicToSquare(string algebraic)
        {
            int file = algebraic[0] - 'a';
            int rank = algebraic[1] - '1';
            return rank * 8 + file;
        }

        private void MovePiece(ulong fromMask, ulong toMask, ref ulong pieceBitboard)
        {
            ClearSquare(toMask);
            pieceBitboard &= ~fromMask;
            pieceBitboard |= toMask;
        }

        private void MovePiece(int fromSquare, int toSquare, ref ulong pieceBitboard)
        {
            ulong fromMask = 1UL << fromSquare;
            ulong toMask = 1UL << toSquare;
            ClearSquare(toMask);
            pieceBitboard &= ~fromMask;
            pieceBitboard |= toMask;
        }

        private void ClearSquare(ulong mask)
        {
            WhitePawns &= ~mask;
            WhiteKnights &= ~mask;
            WhiteBishops &= ~mask;
            WhiteRooks &= ~mask;
            WhiteQueens &= ~mask;
            WhiteKings &= ~mask;
            BlackPawns &= ~mask;
            BlackKnights &= ~mask;
            BlackBishops &= ~mask;
            BlackRooks &= ~mask;
            BlackQueens &= ~mask;
            BlackKings &= ~mask;
        }

        private void ClearSquare(ulong mask, ref ulong bitboard)
        {
            bitboard &= ~mask;
        }

        private void KingSideCastle(bool isWhiteTurn)
        {
            if (isWhiteTurn)
            {
                MovePiece(4, 6, ref WhiteKings);
                MovePiece(7, 5, ref WhiteRooks);
            }
            else
            {
                MovePiece(60, 62, ref BlackKings);
                MovePiece(63, 61, ref BlackRooks);
            }
        }

        private void QueenSideCastle(bool isWhiteTurn)
        {
            if (isWhiteTurn)
            {
                MovePiece(4, 2, ref WhiteKings);
                MovePiece(0, 3, ref WhiteRooks);
            }
            else
            {
                MovePiece(60, 58, ref BlackKings);
                MovePiece(56, 59, ref BlackRooks);
            }
        }

        private void DisableKingCastlingRights(bool white)
        {
            if (white)
                CastlingRights &= 0b11111110;
            else
                CastlingRights &= 0b11111011;
        }

        private void DisableQueenCastlingRights(bool white)
        {
            if (white)
                CastlingRights &= 0b11111101;
            else
                CastlingRights &= 0b11110111;
        }

        public void MakeMove(Move move)
        {
            byte oldCastlingRights = CastlingRights;
            int oldEnPassantSquare = EnPassantSquare;
            bool wasWhiteTurn = IsWhiteTurn;

            if (GetPieceTypeAt(move.ToSquare) != PieceType.None || move.PieceType == PieceType.Pawn)
                HalfmoveClock = 0;
            else
                HalfmoveClock++;

            history.Push(new BoardState(this));
            EnPassantSquare = -1;

            ZobristHash ^= ZobristSideKey; 
            ZobristHash ^= ZobristCastlingKeys[oldCastlingRights]; 
            if (oldEnPassantSquare != -1)
            {
                int file = BoardHelper.GetFile(oldEnPassantSquare);
                ZobristHash ^= ZobristEnPassantKeys[file]; 
            }

            if (move.Flag == MoveFlag.CastlingQueenSide)
            {
                QueenSideCastle(IsWhiteTurn);
                DisableQueenCastlingRights(IsWhiteTurn);
                DisableKingCastlingRights(IsWhiteTurn);
                IsWhiteTurn = !IsWhiteTurn;
                ZobristHash ^= ZobristCastlingKeys[CastlingRights];
                return;
            }
            else if (move.Flag == MoveFlag.CastlingKingSide)
            {
                KingSideCastle(IsWhiteTurn);
                DisableQueenCastlingRights(IsWhiteTurn);
                DisableKingCastlingRights(IsWhiteTurn);
                IsWhiteTurn = !IsWhiteTurn;
                ZobristHash ^= ZobristCastlingKeys[CastlingRights];
                return;
            }

            ulong fromMask = 1UL << move.FromSquare;
            ulong toMask = 1UL << move.ToSquare;

            if (move.Flag == MoveFlag.DoublePawnPush)
            {
                if (IsWhiteTurn)
                    EnPassantSquare = move.ToSquare - 8;
                else
                    EnPassantSquare = move.ToSquare + 8;
                ZobristHash ^= ZobristEnPassantKeys[BoardHelper.GetFile(EnPassantSquare)];
            }

            if (move.Flag == MoveFlag.EnPassant)
            {
                if (IsWhiteTurn)
                {
                    int capturedSquare = move.ToSquare - 8;
                    ZobristHash ^= ZobristPieceKeys[6, capturedSquare];
                    ClearSquare(1UL << capturedSquare, ref BlackPawns);
                }
                else
                {
                    int capturedSquare = move.ToSquare + 8;
                    ZobristHash ^= ZobristPieceKeys[0, capturedSquare];
                    ClearSquare(1UL << capturedSquare, ref WhitePawns);
                }
            }

            if (IsWhiteTurn)
            {
                if ((WhiteKings & fromMask) != 0)
                {
                    DisableKingCastlingRights(true);
                    DisableQueenCastlingRights(true);
                }
                else if ((WhiteRooks & fromMask) != 0)
                {
                    if (move.FromSquare == 0) DisableQueenCastlingRights(true);
                    else if (move.FromSquare == 7) DisableKingCastlingRights(true);
                }
                if (move.ToSquare == 56 && (BlackRooks & toMask) != 0) DisableQueenCastlingRights(false);
                else if (move.ToSquare == 63 && (BlackRooks & toMask) != 0) DisableKingCastlingRights(false);
            }
            else
            {
                if ((BlackKings & fromMask) != 0)
                {
                    DisableKingCastlingRights(false);
                    DisableQueenCastlingRights(false);
                }
                else if ((BlackRooks & fromMask) != 0)
                {
                    if (move.FromSquare == 56) DisableQueenCastlingRights(false);
                    else if (move.FromSquare == 63) DisableKingCastlingRights(false);
                }
                if (move.ToSquare == 0 && (WhiteRooks & toMask) != 0) DisableQueenCastlingRights(true);
                else if (move.ToSquare == 7 && (WhiteRooks & toMask) != 0) DisableKingCastlingRights(true);
            }

            if (move.Flag == MoveFlag.PromotionToQueen ||
                move.Flag == MoveFlag.PromotionToRook ||
                move.Flag == MoveFlag.PromotionToBishop ||
                move.Flag == MoveFlag.PromotionToKnight)
            {
                ClearSquare(toMask);
                if (IsWhiteTurn)
                {
                    ZobristHash ^= ZobristPieceKeys[0, move.FromSquare];
                    WhitePawns &= ~fromMask;
                    switch (move.Flag)
                    {
                        case MoveFlag.PromotionToQueen: WhiteQueens |= toMask; ZobristHash ^= ZobristPieceKeys[4, move.ToSquare]; break;
                        case MoveFlag.PromotionToRook: WhiteRooks |= toMask; ZobristHash ^= ZobristPieceKeys[3, move.ToSquare]; break;
                        case MoveFlag.PromotionToBishop: WhiteBishops |= toMask; ZobristHash ^= ZobristPieceKeys[2, move.ToSquare]; break;
                        case MoveFlag.PromotionToKnight: WhiteKnights |= toMask; ZobristHash ^= ZobristPieceKeys[1, move.ToSquare]; break;
                    }
                }
                else
                {
                    ZobristHash ^= ZobristPieceKeys[6, move.FromSquare];
                    BlackPawns &= ~fromMask;
                    switch (move.Flag)
                    {
                        case MoveFlag.PromotionToQueen: BlackQueens |= toMask; ZobristHash ^= ZobristPieceKeys[10, move.ToSquare]; break;
                        case MoveFlag.PromotionToRook: BlackRooks |= toMask; ZobristHash ^= ZobristPieceKeys[9, move.ToSquare]; break;
                        case MoveFlag.PromotionToBishop: BlackBishops |= toMask; ZobristHash ^= ZobristPieceKeys[8, move.ToSquare]; break;
                        case MoveFlag.PromotionToKnight: BlackKnights |= toMask; ZobristHash ^= ZobristPieceKeys[7, move.ToSquare]; break;
                    }
                }
            }
            else
            {
                int pieceIndex = -1;
                if ((WhitePawns & fromMask) != 0) { pieceIndex = 0; MovePiece(fromMask, toMask, ref WhitePawns); }
                else if ((WhiteKnights & fromMask) != 0) { pieceIndex = 1; MovePiece(fromMask, toMask, ref WhiteKnights); }
                else if ((WhiteBishops & fromMask) != 0) { pieceIndex = 2; MovePiece(fromMask, toMask, ref WhiteBishops); }
                else if ((WhiteRooks & fromMask) != 0) { pieceIndex = 3; MovePiece(fromMask, toMask, ref WhiteRooks); }
                else if ((WhiteQueens & fromMask) != 0) { pieceIndex = 4; MovePiece(fromMask, toMask, ref WhiteQueens); }
                else if ((WhiteKings & fromMask) != 0) { pieceIndex = 5; MovePiece(fromMask, toMask, ref WhiteKings); }
                else if ((BlackPawns & fromMask) != 0) { pieceIndex = 6; MovePiece(fromMask, toMask, ref BlackPawns); }
                else if ((BlackKnights & fromMask) != 0) { pieceIndex = 7; MovePiece(fromMask, toMask, ref BlackKnights); }
                else if ((BlackBishops & fromMask) != 0) { pieceIndex = 8; MovePiece(fromMask, toMask, ref BlackBishops); }
                else if ((BlackRooks & fromMask) != 0) { pieceIndex = 9; MovePiece(fromMask, toMask, ref BlackRooks); }
                else if ((BlackQueens & fromMask) != 0) { pieceIndex = 10; MovePiece(fromMask, toMask, ref BlackQueens); }
                else if ((BlackKings & fromMask) != 0) { pieceIndex = 11; MovePiece(fromMask, toMask, ref BlackKings); }

                ZobristHash ^= ZobristPieceKeys[pieceIndex, move.FromSquare];
                ZobristHash ^= ZobristPieceKeys[pieceIndex, move.ToSquare];

                if (GetPieceTypeAt(move.ToSquare) != PieceType.None)
                {
                    int capturedPieceIndex = -1;
                    if ((WhitePawns & toMask) != 0) capturedPieceIndex = 0;
                    else if ((WhiteKnights & toMask) != 0) capturedPieceIndex = 1;
                    else if ((WhiteBishops & toMask) != 0) capturedPieceIndex = 2;
                    else if ((WhiteRooks & toMask) != 0) capturedPieceIndex = 3;
                    else if ((WhiteQueens & toMask) != 0) capturedPieceIndex = 4;
                    else if ((WhiteKings & toMask) != 0) capturedPieceIndex = 5;
                    else if ((BlackPawns & toMask) != 0) capturedPieceIndex = 6;
                    else if ((BlackKnights & toMask) != 0) capturedPieceIndex = 7;
                    else if ((BlackBishops & toMask) != 0) capturedPieceIndex = 8;
                    else if ((BlackRooks & toMask) != 0) capturedPieceIndex = 9;
                    else if ((BlackQueens & toMask) != 0) capturedPieceIndex = 10;
                    else if ((BlackKings & toMask) != 0) capturedPieceIndex = 11;
                    if (capturedPieceIndex != -1)
                        ZobristHash ^= ZobristPieceKeys[capturedPieceIndex, move.ToSquare];
                }
            }

            IsWhiteTurn = !IsWhiteTurn;
            ZobristHash ^= ZobristCastlingKeys[CastlingRights];
        }

        public void UnMakeMove()
        {
            if (history.Count > 0)
            {
                BoardState previousState = history.Pop();
                previousState.Restore(this);
            }
            else
            {
                throw new InvalidOperationException("No move to undo");
            }
        }
        public bool IsDrawingMaterial()
        {
            if (this.WhitePawns != 0 || this.BlackPawns != 0 ||
                this.WhiteRooks != 0 || this.BlackRooks != 0 ||
                this.WhiteQueens != 0 || this.BlackQueens != 0)
            {
                return false;
            }

            int whiteMinors = BitOperations.PopCount(this.WhiteKnights) +
                             BitOperations.PopCount(this.WhiteBishops);
            int blackMinors = BitOperations.PopCount(this.BlackKnights) +
                             BitOperations.PopCount(this.BlackBishops);

            int totalMinors = whiteMinors + blackMinors;

            return totalMinors <= 1;
        }
    }
}