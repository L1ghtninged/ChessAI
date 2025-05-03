using System;
using System.Collections.Generic;
using System.Numerics;

namespace ChessAIProject
{
    /// <summary>
    /// This tries to speed up the Search process. 
    /// Without alpha-beta pruning useless.
    /// Decides which moves are more interesting for the Search function and evaluates them first.
    /// </summary>
    public class MoveOrdering
    {
        int[] moveScores;
        const int maxMoveCount = 218;
        const int squareControlledByOpponentPawnPenalty = 350;
        const int capturedPieceValueMultiplier = 10;
        public Move moveFromPreviousIteration = null;
        private readonly int[,] history = new int[64, 64];

        public MoveOrdering()
        {
            this.moveScores = new int[maxMoveCount];
        }

        public ulong GetPawnAttacks(Board board, bool byWhite)
        {
            ulong pawns = byWhite ? board.WhitePawns : board.BlackPawns;
            ulong attacked = 0;

            while (pawns != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawns);
                attacked |= byWhite ? MoveGenerator.PawnAttacksWhite[square] : MoveGenerator.PawnAttacksBlack[square];
                pawns &= pawns - 1;
            }
            return attacked;
        }

        private bool IsSquareAttackedByOpponentPawn(ulong attacked, int square)
        {
            return (attacked & (1UL << square)) != 0;
        }

        public void OrderMoves(Board board, List<Move> moves)
        {
            ulong attackedByPawn = GetPawnAttacks(board, !board.IsWhiteTurn);

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                int moveScoreGuess = 0;
                if (move.Equals(moveFromPreviousIteration))
                {
                    moveScores[i] = int.MaxValue;
                }
                int fromSquare = move.FromSquare;
                int toSquare = move.ToSquare;
                PieceType ownPiece = board.GetPieceTypeAt(fromSquare);
                PieceType opponentPiece = board.GetPieceTypeAt(toSquare);

                int ownPieceValue = GetPieceValue(ownPiece);
                int opponentPieceValue = GetPieceValue(opponentPiece);

                if (opponentPiece != PieceType.None)
                {
                    moveScoreGuess = capturedPieceValueMultiplier * opponentPieceValue - ownPieceValue;
                }
                if (ownPiece == PieceType.Pawn)
                {
                    if (move.Flag == MoveFlag.PromotionToQueen)
                    {
                        moveScoreGuess += Evaluation.queenValue;
                    }
                    else if (move.Flag == MoveFlag.PromotionToKnight)
                    {
                        moveScoreGuess += Evaluation.knightValue;
                    }
                    else if (move.Flag == MoveFlag.PromotionToRook)
                    {
                        moveScoreGuess += Evaluation.rookValue;
                    }
                    else if (move.Flag == MoveFlag.PromotionToBishop)
                    {
                        moveScoreGuess += Evaluation.bishopValue;
                    }
                }
                else
                {
                    if (IsSquareAttackedByOpponentPawn(attackedByPawn, toSquare))
                    {
                        moveScoreGuess -= squareControlledByOpponentPawnPenalty;
                    }
                }
                moveScores[i] = moveScoreGuess;
            }

            Sort(moves);

        }

        public void AddHistory(Move move, int depth)
        {
            history[move.FromSquare, move.ToSquare] += depth * depth; // Bonusy za hlubší úspěchy
        }

        void Sort(List<Move> moves)
        {
            // Sort the moves list based on scores
            for (int i = 0; i < moves.Count - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    if (moveScores[swapIndex] < moveScores[j])
                    {
                        (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                        (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                    }
                }
            }
        }

        public int GetPieceValue(PieceType pieceType)
        {
            switch (pieceType)
            {
                case PieceType.Queen:
                    return Evaluation.queenValue;
                case PieceType.Rook:
                    return Evaluation.rookValue;
                case PieceType.Knight:
                    return Evaluation.knightValue;
                case PieceType.Bishop:
                    return Evaluation.bishopValue;
                case PieceType.Pawn:
                    return Evaluation.pawnValue;
                default:
                    return 0;
            }
        }
    }
}