using ChessAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    /// <summary>
    /// Main class for AI search.
    /// </summary>
    public class AI
    {
        public static Book book = new Book("Games.txt");
        public const int positiveInfinity = 10000;
        public const int negativeInfinity = -positiveInfinity;
        
        private static MoveOrdering ordering = new();
        public const int timeLimit = 1000;
        private static DateTime searchStartTime;

        private static Move currentBestMove;
        private static int currentBestEval;
        public const int mate_score = positiveInfinity - 1;

        public static bool IsRepetition(Board board)
        { 
            int count = 0;
            foreach (BoardState state in board.history)
            {
                if (state.OccupiedSquares == board.OccupiedSquares)
                {
                    count++;
                    if (count >= 1)
                    return true;
                }
            }
            return false;
        }

        public static int Search(Board board, int alpha, int beta, int depth, bool storeBestMove = false)
        { 

            if (depth == 0)
            {
                return SearchAllCaptures(board, alpha, beta);
            }

            if (IsRepetition(board))
            {
                return 0;
            }
            if (board.IsDrawingMaterial())
            {
                return 0;
            }
            List<Move> moves = MoveGenerator.GenerateLegalMoves(board);
            if (moves.Count == 0)
            {
                int kingIndex = board.IsWhiteTurn ? BitOperations.TrailingZeroCount(board.WhiteKings) : BitOperations.TrailingZeroCount(board.BlackKings);
                if (MoveGenerator.IsSquareAttacked(board, !board.IsWhiteTurn, kingIndex))
                    return -mate_score - depth;
                return 0;
            }

            ordering.OrderMoves(board, moves);

            int bestScore = negativeInfinity;
            Move bestMove = default;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = -Search(board, -beta, -alpha, depth - 1);
                board.UnMakeMove();

                if (eval > bestScore)
                {
                    bestScore = eval;
                    bestMove = move;

                    if (storeBestMove && depth > 1)
                    {
                        currentBestMove = move;
                        currentBestEval = eval;
                    }
                }

                if (eval >= beta)
                {
                    return eval;
                }

                alpha = Math.Max(alpha, eval);
            }
            

            return bestScore;
        }

        public static int SearchAllCaptures(Board board, int alpha, int beta)
        {
            int eval = Evaluation.Evaluate(board);
            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);

            List<Move> moves = MoveGenerator.GenerateLegalMoves(board, true);
            ordering.OrderMoves(board, moves);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = -SearchAllCaptures(board, -beta, -alpha);
                board.UnMakeMove();
                if (eval >= beta)
                    return beta;
                alpha = Math.Max(alpha, eval);
            }

            return alpha;
        }

        public static Move FindBestMove(Board board, int depth)
        {
            List<Move> moves = MoveGenerator.GenerateLegalMoves(board);
            if (moves.Count == 0)
                return null;

            Move bestMove = moves[0];
            int bestEval = negativeInfinity;

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = -Search(board, -9999, 9999, depth - 1);
                board.UnMakeMove();

                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }

            return bestMove;
        }
        public static Move FindBestMoveIterative(Board board, int timeLimitMs = timeLimit, int maxDepth = 20)
        {
            searchStartTime = DateTime.Now;
            currentBestMove = null;
            currentBestEval = negativeInfinity;

            var moves = MoveGenerator.GenerateLegalMoves(board);
            if (moves.Count == 0) return null;

            currentBestMove = moves[0];
            int depth;
            for (depth = 1; depth <= maxDepth; depth++)
            {
                if (TimeExceeded(timeLimitMs)) break;

                try
                {
                    int eval = Search(board, negativeInfinity, positiveInfinity, depth, true);
                    if (eval >= mate_score - 1)
                    {
                        Console.WriteLine("Mate in "+depth/2 +" moves");
                        break;
                    }
                    if (currentBestMove != null)
                    {
                        ordering.moveFromPreviousIteration = currentBestMove;
                    }
                }
                catch (TimeoutException)
                {
                    break;
                }
            }
            Console.WriteLine("Depth reached: "+depth);
            return currentBestMove;
        }
        private static bool TimeExceeded(int time = timeLimit)
        {
            return (DateTime.Now - searchStartTime).TotalMilliseconds > time;
        }

        public static Move? FindBookMove(Board board)
        {
            return book.GetBookMove(board);
        }
        


    }
}