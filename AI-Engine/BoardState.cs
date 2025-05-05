using ChessAIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    /// <summary>
    /// Stores all information about a chessboard.
    /// </summary>
    public class BoardState
    {
        public ulong ZobristHash;
        public int HalfmoveClock;
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
        public int EnPassantSquare;
        public bool IsWhiteTurn;
        public byte CastlingRights;

        public ulong OccupiedSquares => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings | BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;
        public ulong WhitePieces => WhitePawns | WhiteKnights | WhiteBishops | WhiteRooks | WhiteQueens | WhiteKings;
        public ulong BlackPieces => BlackPawns | BlackKnights | BlackBishops | BlackRooks | BlackQueens | BlackKings;

        public BoardState(Board board)
        {
            HalfmoveClock = board.HalfmoveClock;
            WhitePawns = board.WhitePawns;
            WhiteKnights = board.WhiteKnights;
            WhiteBishops = board.WhiteBishops;
            WhiteRooks = board.WhiteRooks;
            WhiteQueens = board.WhiteQueens;
            WhiteKings = board.WhiteKings;
            BlackPawns = board.BlackPawns;
            BlackKnights = board.BlackKnights;
            BlackBishops = board.BlackBishops;
            BlackRooks = board.BlackRooks;
            BlackQueens = board.BlackQueens;
            BlackKings = board.BlackKings;
            EnPassantSquare = board.EnPassantSquare;
            IsWhiteTurn = board.IsWhiteTurn;
            CastlingRights = board.CastlingRights;
        }

        public void Restore(Board board)
        {
            board.WhitePawns = WhitePawns;
            board.WhiteKnights = WhiteKnights;
            board.WhiteBishops = WhiteBishops;
            board.WhiteRooks = WhiteRooks;
            board.WhiteQueens = WhiteQueens;
            board.WhiteKings = WhiteKings;
            board.BlackPawns = BlackPawns;
            board.BlackKnights = BlackKnights;
            board.BlackBishops = BlackBishops;
            board.BlackRooks = BlackRooks;
            board.BlackQueens = BlackQueens;
            board.BlackKings = BlackKings;
            board.EnPassantSquare = EnPassantSquare;
            board.IsWhiteTurn = IsWhiteTurn;
            board.CastlingRights = CastlingRights;
            board.HalfmoveClock = HalfmoveClock;
        }
    }
}
