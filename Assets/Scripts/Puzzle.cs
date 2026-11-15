using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelMoments
{
    public enum Mark { Unknown, Filled, Empty }
    public enum Phase { Playing, Locked, Completed }

    // Pure C#: no rendering, input or Unity dependency.
    public sealed class Puzzle
    {
        public const int Size = 5;
        public const int InitialReasoning = 5;
        // A small star: narrow tip, full horizontal arms, two lower points.
        private static readonly string[] Star = { "00100", "11111", "01110", "11011", "10001" };
        public readonly Mark[,] Cells = new Mark[Size, Size];
        public int Reasoning { get; private set; } = InitialReasoning;
        public Phase State { get; private set; } = Phase.Playing;
        public int MarkedCount => Cells.Cast<Mark>().Count(x => x != Mark.Unknown);
        public bool Answer(int row, int col) => Star[row][col] == '1';
        public int[] RowClues(int row) => Clues(Enumerable.Range(0, Size).Select(c => Answer(row, c)));
        public int[] ColumnClues(int col) => Clues(Enumerable.Range(0, Size).Select(r => Answer(r, col)));
        public static int[] Clues(IEnumerable<bool> line)
        {
            var values = new List<int>(); int run = 0;
            foreach (bool filled in line) { if (filled) run++; else if (run > 0) { values.Add(run); run = 0; } }
            if (run > 0) values.Add(run);
            return values.ToArray();
        }
        public bool Submit(int row, int col, Mark mark, out bool error)
        {
            error = false;
            if (row < 0 || row >= Size || col < 0 || col >= Size) throw new ArgumentOutOfRangeException();
            if (!Enum.IsDefined(typeof(Mark), mark)) throw new ArgumentOutOfRangeException(nameof(mark));
            if (State != Phase.Playing || Cells[row, col] == mark) return false;
            Cells[row, col] = mark;
            error = mark != Mark.Unknown && (mark == Mark.Filled) != Answer(row, col);
            if (error) Reasoning--;
            if (Reasoning == 0) { State = Phase.Locked; return true; }
            if (MarkedCount == Size * Size && Enumerable.Range(0, Size).All(r =>
                Enumerable.Range(0, Size).All(c => (Cells[r, c] == Mark.Filled) == Answer(r, c))))
                State = Phase.Completed;
            return true;
        }
        public bool Reset()
        {
            if (State == Phase.Completed) return false;
            Array.Clear(Cells, 0, Cells.Length); Reasoning = InitialReasoning; State = Phase.Playing;
            return true;
        }
        public static int CountSolutions(int limit = 2)
        {
            var puzzle = new Puzzle(); var board = new bool[Size, Size]; int count = 0;
            Action<int> search = null;
            search = row => {
                if (count >= limit) return;
                if (row == Size) {
                    if (Enumerable.Range(0, Size).All(c => Clues(Enumerable.Range(0, Size).Select(r => board[r,c])).SequenceEqual(puzzle.ColumnClues(c)))) count++;
                    return;
                }
                for (int mask = 0; mask < (1 << Size); mask++) {
                    if (!Clues(Enumerable.Range(0, Size).Select(c => (mask & (1 << c)) != 0)).SequenceEqual(puzzle.RowClues(row))) continue;
                    for (int c = 0; c < Size; c++) board[row,c] = (mask & (1 << c)) != 0;
                    search(row + 1);
                }
            };
            search(0); return count;
        }
    }
}
