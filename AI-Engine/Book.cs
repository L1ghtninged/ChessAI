using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChessAIProject
{
    /// <summary>
    /// Reads a file, which contains grandmaster games.
    /// The AI uses this as a book which contains starting moves.
    /// </summary>
    public class Book
    {
        private readonly Dictionary<string, List<MoveEntry>> _openingBook = new Dictionary<string, List<MoveEntry>>();
        private readonly Random _random = new Random();
        public Book(string file) 
        {
            LoadFromFile(file);
        }


        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Opening book file not found", filePath);

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var moves = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var board = new Board();
                board.SetUpBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

                for (int i = 0; i < moves.Length; i++)
                {
                    string moveStr = moves[i];
                    if (moveStr.Contains("1/2-1/2") || moveStr.Contains("1-0") || moveStr.Contains("0-1"))
                        break;

                    var legalMoves = MoveGenerator.GenerateLegalMoves(board);
                    Move move = ParseMove(moveStr, board, legalMoves);

                    if (move == null)
                    {
                        break;
                    }

                    string fen = GetSimplifiedFen(board);
                    if (!_openingBook.ContainsKey(fen))
                    {
                        _openingBook[fen] = new List<MoveEntry>();
                    }

                    if (!_openingBook[fen].Any(x => x.Move.Equals(move)))
                    {
                        _openingBook[fen].Add(new MoveEntry(move, 1));
                    }
                    else
                    {
                        var entry = _openingBook[fen].First(x => x.Move.Equals(move));
                        entry.Weight++;
                    }

                    board.MakeMove(move);
                }
            }
        }

        public Move GetBookMove(Board board)
        {
            string fen = GetSimplifiedFen(board);
            if (!_openingBook.ContainsKey(fen) || _openingBook[fen].Count == 0)
                return null;

            var possibleMoves = _openingBook[fen];

            int totalWeight = possibleMoves.Sum(x => x.Weight);
            int randomValue = _random.Next(totalWeight);
            int cumulativeWeight = 0;

            foreach (var entry in possibleMoves)
            {
                cumulativeWeight += entry.Weight;
                if (randomValue < cumulativeWeight)
                    return entry.Move;
            }

            return possibleMoves[0].Move;
        }

        private string GetSimplifiedFen(Board board)
        {
            var fenParts = new System.Text.StringBuilder();

            for (int rank = 7; rank >= 0; rank--)
            {
                int emptySquares = 0;
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    char piece = board.GetPieceAt(square);
                    if (piece == '\0')
                    {
                        emptySquares++;
                    }
                    else
                    {
                        if (emptySquares > 0)
                        {
                            fenParts.Append(emptySquares);
                            emptySquares = 0;
                        }
                        fenParts.Append(piece);
                    }
                }
                if (emptySquares > 0)
                    fenParts.Append(emptySquares);
                if (rank > 0)
                    fenParts.Append('/');
            }

            fenParts.Append(board.IsWhiteTurn ? " w " : " b ");

            fenParts.Append(board.CastlingRights switch
            {
                0 => "-",
                _ => ((board.CastlingRights & 0b0001) != 0 ? "K" : "") +
                     ((board.CastlingRights & 0b0010) != 0 ? "Q" : "") +
                     ((board.CastlingRights & 0b0100) != 0 ? "k" : "") +
                     ((board.CastlingRights & 0b1000) != 0 ? "q" : "")
            });

            // En passant
            fenParts.Append(board.EnPassantSquare == -1 ? " -" : " " + GetSquareNotation(board.EnPassantSquare));

            return fenParts.ToString();
        }

        private string GetSquareNotation(int square)
        {
            char file = (char)('a' + (square % 8));
            char rank = (char)('1' + (square / 8));
            return $"{file}{rank}";
        }

        private Move ParseMove(string moveStr, Board board, List<Move> legalMoves)
        {
            if (moveStr == "O-O" || moveStr == "0-0")
            {
                return legalMoves.FirstOrDefault(m => m.Flag == MoveFlag.CastlingKingSide);
            }
            if (moveStr == "O-O-O" || moveStr == "0-0-0")
            {
                return legalMoves.FirstOrDefault(m => m.Flag == MoveFlag.CastlingQueenSide);
            }

            if (moveStr.Contains('='))
            {
                string squareStr = moveStr.Substring(0, moveStr.IndexOf('='));
                char promotionChar = moveStr[moveStr.IndexOf('=') + 1];
                int to_square = GetSquareIndex(squareStr);

                var promotionFlag = promotionChar switch
                {
                    'Q' => MoveFlag.PromotionToQueen,
                    'R' => MoveFlag.PromotionToRook,
                    'B' => MoveFlag.PromotionToBishop,
                    'N' => MoveFlag.PromotionToKnight,
                    _ => MoveFlag.None
                };

                return legalMoves.FirstOrDefault(m =>
                    m.ToSquare == to_square &&
                    m.Flag == promotionFlag);
            }

            if (moveStr.EndsWith("e.p."))
            {
                moveStr = moveStr.Substring(0, moveStr.IndexOf(' '));
                int to_square = GetSquareIndex(moveStr.Substring(2, 2));
                return legalMoves.FirstOrDefault(m =>
                    m.ToSquare == to_square &&
                    m.Flag == MoveFlag.EnPassant);
            }

            int toSquare = GetSquareIndex(moveStr.Substring(moveStr.Length - 2));
            char pieceChar = moveStr[0];
            PieceType pieceType = pieceChar switch
            {
                'N' => PieceType.Knight,
                'B' => PieceType.Bishop,
                'R' => PieceType.Rook,
                'Q' => PieceType.Queen,
                'K' => PieceType.King,
                _ => PieceType.Pawn
            };

            if (moveStr.Length > 3 && (moveStr[1] == 'x' || char.IsLower(moveStr[1])))
            {
                char disambiguator = moveStr[1];
                if (disambiguator == 'x') disambiguator = moveStr[2];

                if (char.IsLower(disambiguator))
                {
                    int file = disambiguator - 'a';
                    return legalMoves.FirstOrDefault(m =>
                        m.ToSquare == toSquare &&
                        m.PieceType == pieceType &&
                        (m.FromSquare % 8) == file);
                }
                else if (char.IsDigit(disambiguator))
                {
                    int rank = disambiguator - '1';
                    return legalMoves.FirstOrDefault(m =>
                        m.ToSquare == toSquare &&
                        m.PieceType == pieceType &&
                        (m.FromSquare / 8) == rank);
                }
            }

            return legalMoves.FirstOrDefault(m =>
                m.ToSquare == toSquare &&
                m.PieceType == pieceType);
        }

        private int GetSquareIndex(string squareNotation)
        {
            char file = squareNotation[0];
            char rank = squareNotation[1];
            return (rank - '1') * 8 + (file - 'a');
        }

        private class MoveEntry
        {
            public Move Move { get; }
            public int Weight { get; set; }

            public MoveEntry(Move move, int weight)
            {
                Move = move;
                Weight = weight;
            }
        }
    }
}
