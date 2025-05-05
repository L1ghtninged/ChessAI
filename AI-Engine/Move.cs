using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    public enum PieceType
    {
        None,
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King,

    }
    public enum MoveFlag
    {
        None,
        CastlingKingSide,
        CastlingQueenSide,
        PromotionToQueen,
        PromotionToRook,
        PromotionToBishop,
        PromotionToKnight,
        DoublePawnPush,
        EnPassant
    }
    /// <summary>
    /// Represents a chess move.
    /// </summary>
    public class Move
    {
        public int FromSquare { get; set; }
        public int ToSquare { get; set; }
        public MoveFlag Flag { get; set; }
        public PieceType PieceType { get; set; }

        public Move(int fromSquare, int toSquare, MoveFlag flag = MoveFlag.None, PieceType PieceType = PieceType.None)
        {
            this.ToSquare = toSquare;
            this.FromSquare = fromSquare;
            this.Flag = flag;
            this.PieceType = PieceType;
            if ((!BoardHelper.IsInBounds(fromSquare)) | (!BoardHelper.IsInBounds(toSquare))) throw new Exception("Move is invalid");
        }
        public Move(string fromSquare, string toSquare, MoveFlag flag = MoveFlag.None, PieceType PieceType = PieceType.None)
        {
            FromSquare = GetSquareIndex(fromSquare);
            ToSquare = GetSquareIndex(toSquare);
            this.Flag = flag;
            this.PieceType = PieceType;
            if ((!BoardHelper.IsInBounds(FromSquare)) | (!BoardHelper.IsInBounds(ToSquare))) throw new Exception("Move is invalid");

        }

        public override string ToString()
        {
            string move = $"{GetSquareNotation(FromSquare)}-{GetSquareNotation(ToSquare)}";
            if (Flag == MoveFlag.CastlingKingSide) return "0-0";
            if (Flag == MoveFlag.CastlingQueenSide) return "0-0-0";

            if (Flag == MoveFlag.PromotionToQueen)
            {
                move += $" (promoted to queen)";
            }
            else if (Flag == MoveFlag.PromotionToRook)
            {
                move += $" (promoted to rook)";
            }
            else if (Flag == MoveFlag.PromotionToBishop)
            {
                move += $" (promoted to bishop)";
            }
            else if (Flag == MoveFlag.PromotionToKnight)
            {
                move += $" (promoted to knight)";
            }


            return move;
        }
        private string GetSquareNotation(int square)
        {
            char file = (char)('a' + (square % 8)); // a-h
            char rank = (char)('1' + (square / 8)); // 1-8
            return $"{file}{rank}";
        }
        private int GetSquareIndex(string notation)
        {
            char file = notation[0];
            char rank = notation[1]; 

            int fileIndex = file - 'a';

            int rankIndex = rank - '1';

            return rankIndex * 8 + fileIndex;
        }

        public override bool Equals(object? obj)
        {
            return obj is Move move &&
                   FromSquare == move.FromSquare &&
                   ToSquare == move.ToSquare &&
                   Flag == move.Flag &&
                   PieceType == move.PieceType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FromSquare, ToSquare, Flag, PieceType);
        }
    }
}
