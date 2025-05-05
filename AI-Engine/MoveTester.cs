using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAIProject
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    /// <summary>
    /// This is used for debugging-only. It compares number of nodes from stockfish with the MoveGenerator logic.
    /// </summary>
    public static class MoveTester
    {
        public static Dictionary<string, int> ReadStockfish(string input)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            var lines = input.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length < 2)
                    continue;

                string moveRaw = parts[0].Trim();
                string valueStr = parts[1].Trim();
                if (!int.TryParse(valueStr, out int value))
                    continue;

                string norm = NormalizeStockfishMove(moveRaw);
                dict[norm] = value;
            }
            return dict;
        }

        public static Dictionary<string, int> ReadOwn(string input)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            var lines = input.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length < 2)
                    continue;

                string moveRaw = parts[0].Trim();
                string valueStr = parts[1].Trim();
                if (!int.TryParse(valueStr, out int value))
                    continue;

                dict[moveRaw] = value;
            }
            return dict;
        }

        private static string NormalizeStockfishMove(string move)
        {
            if (move == "e1g1") return "0-0";
            if (move == "e1c1") return "0-0-0";
            if (move == "e8g8") return "0-0";
            if (move == "e8c8") return "0-0-0";

            if (move.Length == 5)
            {
                string from = move.Substring(0, 2);
                string to = move.Substring(2, 2);
                char prom = move[4];

                string promPiece = prom switch
                {
                    'q' or 'Q' => "queen",
                    'r' or 'R' => "rook",
                    'b' or 'B' => "bishop",
                    'n' or 'N' => "knight",
                    _ => "unknown"
                };

                return $"{from}-{to} (promoted to {promPiece})";
            }

            if (move.Length == 4)
            {
                return move.Substring(0, 2) + "-" + move.Substring(2, 2);
            }

            return move;
        }

        public static string[] GetWrongNumbers(string stockfishInput, string ownInput)
        {
            var stockfishMoves = ReadStockfish(stockfishInput);
            var ownMoves = ReadOwn(ownInput);
            List<string> mismatches = new List<string>();
            int celkovyRozdil = 0;
            foreach (var kvp in stockfishMoves)
            {
                string move = kvp.Key;
                int sfValue = kvp.Value;

                if (ownMoves.TryGetValue(move, out int ownValue))
                {
                    if (sfValue != ownValue)
                    {
                        celkovyRozdil += (ownValue - sfValue);
                        mismatches.Add($"Tah {move}: Stockfish = {sfValue}, Own = {ownValue}, Rozdíl: {ownValue - sfValue}");
                    }
                }
                else
                {
                    mismatches.Add($"Tah {move} chybí v 'own' výsledcích.");
                }
            }

            foreach (var move in ownMoves.Keys)
            {
                if (!stockfishMoves.ContainsKey(move))
                {
                    mismatches.Add($"Tah {move} je navíc ve 'own' výsledcích.");
                }
            }
            mismatches.Add($"Celkový rozdíl: {celkovyRozdil}");
            return mismatches.ToArray();
        }
    }










}
