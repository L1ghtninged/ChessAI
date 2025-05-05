using System.Numerics;

namespace ChessAIProject
{
    /// <summary>
    /// Main class
    /// </summary>
    public class Program
    {
        // Tests the number of nodes for a chess position after some depth.
        static int MoveGenerationTest(int depth, Board board)
        {
            if (depth == 0) return 1;

            List<Move> moves = MoveGenerator.GenerateLegalMoves(board);

            int numPositions = 0;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                numPositions += MoveGenerationTest(depth - 1, board);
                board.UnMakeMove();

            }

            return numPositions;



        }
        static void Main(string[] args)
        {
            //Board board = new Board();
            //board.SetUpBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

            

        }
        public static void PrintBitboardBinary(ulong bitboard)
        {
            for (int rank = 7; rank >= 0; rank--)
            {
                for (int file = 0; file < 8; file++)
                {
                    ulong mask = 1UL << (rank * 8 + file);
                    Console.Write((bitboard & mask) != 0 ? "1 " : ". ");
                }
                Console.WriteLine();
            }
            Console.WriteLine("******************************");
        }
    }
}