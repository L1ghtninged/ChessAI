using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    /// <summary>
    /// The AI uses this as an evaluation function in the mini-max algorithm.
    /// It takes into account piece positioning, material, phase of the game, and getting the enemy king into a corner.
    /// </summary>
    internal class Evaluation
    {
        public static int pawnValue = 100;
        public static int knightValue = 300;
        public static int bishopValue = 310;
        public static int rookValue = 500;
        public static int queenValue = 920;

        public static float gamePhaseConst = 0.6f;
        public static float pieceSquareTableConst = 1f;
        public static float weightMultiplierConstHigher = 3f;
        public static float weightMultiplierConstLower = 1f;
        public static int kingCornerEvalConst = 20;
        public static int Evaluate(Board board)
        {
            bool isWhite = board.IsWhiteTurn;
            int whiteMaterial = Evaluation.GetWhiteMaterial(board);
            int blackMaterial = Evaluation.GetBlackMaterial(board);

            double gamePhase = Evaluation.GetGamePhase(board); // 0 = opening, 1 = endgame
            int whitePositioningEval = (int)(Evaluation.EvaluatePieceSquareTables(board, true) * pieceSquareTableConst);
            int blackPositionEval = (int)(Evaluation.EvaluatePieceSquareTables(board, false) * pieceSquareTableConst);

            int whiteEval = whiteMaterial + whitePositioningEval;
            int blackEval = blackMaterial + blackPositionEval;
            int kingCornerEval = 0;

            int whitePawnStructure = EvaluatePawnStructure(board, true);
            int blackPawnStructure = EvaluatePawnStructure(board, false);
            
            whiteEval += whitePawnStructure;
            blackEval += blackPawnStructure;
            if (gamePhase >= gamePhaseConst)
            {
                int friendlyKingIndex = BitOperations.TrailingZeroCount(isWhite ? board.WhiteKings : board.BlackKings);
                int opponentKingIndex = BitOperations.TrailingZeroCount(isWhite ? board.BlackKings : board.WhiteKings);
                int opponentMinorPieces = BitOperations.PopCount(isWhite ? board.BlackKnights | board.BlackBishops :
                                                                         board.WhiteKnights | board.WhiteBishops);
                int opponentMajorPieces = BitOperations.PopCount(isWhite ? board.BlackRooks | board.BlackQueens :
                                                                         board.WhiteRooks | board.WhiteQueens);

                float weightMultiplier = (opponentMinorPieces <= 1 && opponentMajorPieces == 0) ? weightMultiplierConstHigher : weightMultiplierConstLower;
                float endGameWeight = (float)gamePhase * weightMultiplier;
                int[] whitePassedPawns = FindPassedPawns(board.WhitePawns, board.BlackPawns, true);
                int[] blackPassedPawns = FindPassedPawns(board.BlackPawns, board.WhitePawns, false);
                kingCornerEval = ForceKingIntoCornerEval(
           friendlyKingIndex,
           opponentKingIndex,
           isWhite ? whitePassedPawns : blackPassedPawns,
           endGameWeight
       );

            }
            int eval = whiteEval - blackEval;
            int perspective = isWhite ? 1 : -1;
            return eval * perspective + kingCornerEval;
        }
        public static int GetWhiteMaterial(Board board)
        {
            var whitePawnsEval = BitOperations.PopCount(board.WhitePawns) * Evaluation.pawnValue;
            var whiteKnightsEval = BitOperations.PopCount(board.WhiteKnights) * Evaluation.knightValue;
            var whiteRooksEval = BitOperations.PopCount(board.WhiteRooks) * Evaluation.rookValue;
            var whiteBishopsEval = BitOperations.PopCount(board.WhiteBishops) * Evaluation.bishopValue;
            var whiteQueensEval = BitOperations.PopCount(board.WhiteQueens) * Evaluation.queenValue;

            return whitePawnsEval + whiteKnightsEval + whiteBishopsEval + whiteRooksEval + whiteQueensEval;
        }
        public static int GetBlackMaterial(Board board)
        {
            var blackPawnsEval = BitOperations.PopCount(board.BlackPawns) * Evaluation.pawnValue;
            var blackKnightsEval = BitOperations.PopCount(board.BlackKnights) * Evaluation.knightValue;
            var blackRooksEval = BitOperations.PopCount(board.BlackRooks) * Evaluation.rookValue;
            var blackBishopsEval = BitOperations.PopCount(board.BlackBishops) * Evaluation.bishopValue;
            var blackQueensEval = BitOperations.PopCount(board.BlackQueens) * Evaluation.queenValue;
            return blackPawnsEval + blackKnightsEval + blackBishopsEval + blackRooksEval + blackQueensEval;
        }

        public static int ForceKingIntoCornerEval(int friendlyKingIndex, int opponentKingIndex, int[] passedPawns, float endGameWeight)
        {
            int evaluation = 0;

            int opponentCornerScore = Math.Abs(3 - opponentKingIndex % 8) + Math.Abs(3 - opponentKingIndex / 8);
            evaluation += opponentCornerScore * 2;

            if (passedPawns.Length > 0)
            {
                int minKingDistance = passedPawns.Min(p =>
                    Math.Abs(p % 8 - friendlyKingIndex % 8) +
                    Math.Abs(p / 8 - friendlyKingIndex / 8));
                evaluation += (7 - minKingDistance) * 3;
            }

            int kingDistance = Math.Abs(friendlyKingIndex % 8 - opponentKingIndex % 8)
                             + Math.Abs(friendlyKingIndex / 8 - opponentKingIndex / 8);
            evaluation += (14 - kingDistance) * 2;

            return (int)(evaluation * endGameWeight);
        }

        public static int EvaluatePieceSquareTables(Board board, bool isWhite)
        {
            int score = 0;

            ulong pawns = isWhite ? board.WhitePawns : board.BlackPawns;
            ulong knights = isWhite ? board.WhiteKnights : board.BlackKnights;
            ulong bishops = isWhite ? board.WhiteBishops : board.BlackBishops;
            ulong rooks = isWhite ? board.WhiteRooks : board.BlackRooks;
            ulong queens = isWhite ? board.WhiteQueens : board.BlackQueens;
            ulong king = isWhite ? board.WhiteKings : board.BlackKings;

            while (pawns != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawns);
                score += PieceSquareTable.Read(PieceSquareTable.pawns, square, isWhite);
                pawns &= pawns - 1;
            }

            while (knights != 0)
            {
                int square = BitOperations.TrailingZeroCount(knights);
                score += PieceSquareTable.Read(PieceSquareTable.knights, square, isWhite);
                knights &= knights - 1;
            }

            while (bishops != 0)
            {
                int square = BitOperations.TrailingZeroCount(bishops);
                score += PieceSquareTable.Read(PieceSquareTable.bishops, square, isWhite);
                bishops &= bishops - 1;
            }

            while (rooks != 0)
            {
                int square = BitOperations.TrailingZeroCount(rooks);
                score += PieceSquareTable.Read(PieceSquareTable.rooks, square, isWhite);
                rooks &= rooks - 1;
            }

            while (queens != 0)
            {
                int square = BitOperations.TrailingZeroCount(queens);
                score += PieceSquareTable.Read(PieceSquareTable.queens, square, isWhite);
                queens &= queens - 1;
            }

            int kingSquare = BitOperations.TrailingZeroCount(king);
            double gamePhase = GetGamePhase(board); // 0 = opening, 1 = endgame
            int kingMiddleScore = PieceSquareTable.Read(PieceSquareTable.kingMiddle, kingSquare, isWhite);
            int kingEndScore = PieceSquareTable.Read(PieceSquareTable.kingEnd, kingSquare, isWhite);
            int kingScore = (int)((1 - gamePhase) * kingMiddleScore + gamePhase * kingEndScore);
            score += kingScore;

            return score;
        }
        public static double GetGamePhase(Board board)
        {
            int whitePieces = BitOperations.PopCount(board.WhiteKnights) +
                              BitOperations.PopCount(board.WhiteBishops) +
                              BitOperations.PopCount(board.WhiteRooks) +
                              BitOperations.PopCount(board.WhiteQueens);
            int blackPieces = BitOperations.PopCount(board.BlackKnights) +
                              BitOperations.PopCount(board.BlackBishops) +
                              BitOperations.PopCount(board.BlackRooks) +
                              BitOperations.PopCount(board.BlackQueens);
            int totalPieces = whitePieces + blackPieces;

            const int maxPieces = 14;
            double phase = 1.0 - (double)totalPieces / maxPieces;
            return Math.Clamp(phase, 0, 1);
        }
        public static int EvaluatePawnStructure(Board board, bool isWhite)
        {
            int score = 0;
            ulong pawns = isWhite ? board.WhitePawns : board.BlackPawns;

            while (pawns != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawns);
                int rank = BoardHelper.GetRank(square);

                if ((PawnAttacks(!isWhite, square) & pawns) == 0)
                    score += 15;

                if ((PawnAttacks(isWhite, square) & pawns) != 0)
                    score += 20;

                int promotionBonus = isWhite ? rank : 7 - rank;
                score += promotionBonus * 10;

                pawns &= pawns - 1;
            }
            return score;
        }

        private static ulong PawnAttacks(bool isWhite, int square)
        {
            return isWhite ? MoveGenerator.PawnAttacksWhite[square] : MoveGenerator.PawnAttacksBlack[square];
        }
        public static int[] FindPassedPawns(ulong pawns, ulong enemyPawns, bool isWhite)
        {
            List<int> passedPawns = new List<int>();
            

            while (pawns != 0)
            {
                int square = BitOperations.TrailingZeroCount(pawns);
                
                if ((PassedPawnMask(square, isWhite) & enemyPawns) == 0)
                    passedPawns.Add(square);

                pawns &= pawns - 1;
            }
            return passedPawns.ToArray();
        }
        public static ulong PassedPawnMask(int squareIndex, bool isWhite)
        {
            ulong fileAMask = 0x0101010101010101;
            int rank = BoardHelper.GetRank(squareIndex);
            int file = BoardHelper.GetFile(squareIndex);

            ulong rankMask = isWhite ? ulong.MaxValue << 8 * (rank+1) : ulong.MaxValue >> 8 * (rank - 1);
            ulong fileMask = fileAMask << file;
            ulong fileMaskLeft = fileAMask <<Math.Max(0,file-1);
            ulong fileMaskRight = fileAMask <<Math.Min(7,file+1);

            return (fileMaskLeft | fileMaskRight | fileMask) & rankMask;
        }


    }
}
