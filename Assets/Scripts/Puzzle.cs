using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelMoments
{
    public enum Mark { Unknown, Filled, Empty }
    public enum Phase { Playing, Locked, Completed }

    public sealed class Puzzle
    {
        public const int Size = Level.Size;
        public const int InitialReasoning = 5;
        public Level Level { get; }
        private readonly Mark[,] cells = new Mark[Size,Size];
        public int Reasoning { get; private set; } = InitialReasoning;
        public Phase State { get; private set; } = Phase.Playing;
        public int MarkedCount { get; private set; }
        public Puzzle(Level level = null) { Level = level ?? Levels.All[0]; }
        public Mark Cell(int row,int col) => cells[row,col];
        public bool Answer(int row,int col) => Level.Answer(row,col);
        public int[] RowClues(int row) => Level.RowClues(row);
        public int[] ColumnClues(int col) => Level.ColumnClues(col);
        public static int[] Clues(IEnumerable<bool> line)
        {
            var values=new List<int>(); int run=0;
            foreach(bool filled in line) { if(filled) run++; else if(run>0) { values.Add(run);run=0; } }
            if(run>0) values.Add(run); return values.ToArray();
        }
        public bool Submit(int row,int col,Mark mark,out bool error)
        {
            error=false;
            if(row<0 || row>=Size || col<0 || col>=Size) throw new ArgumentOutOfRangeException();
            if(!Enum.IsDefined(typeof(Mark),mark)) throw new ArgumentOutOfRangeException(nameof(mark));
            if(State!=Phase.Playing || cells[row,col]==mark) return false;
            if(cells[row,col]!=Mark.Unknown) MarkedCount--;
            cells[row,col]=mark;
            if(mark!=Mark.Unknown) MarkedCount++;
            error=mark!=Mark.Unknown && (mark==Mark.Filled)!=Answer(row,col);
            if(error) Reasoning--;
            if(Reasoning==0) { State=Phase.Locked;return true; }
            if(MarkedCount==Size*Size && Enumerable.Range(0,Size).All(r =>
                Enumerable.Range(0,Size).All(c => (cells[r,c]==Mark.Filled)==Answer(r,c)))) State=Phase.Completed;
            return true;
        }
        public bool Reset()
        {
            if(State==Phase.Completed) return false;
            Array.Clear(cells,0,cells.Length);MarkedCount=0;Reasoning=InitialReasoning;State=Phase.Playing;return true;
        }
    }

    // The flow is shared by the UI and tests. Advancing an unfinished puzzle is rejected.
    public sealed class Campaign
    {
        public int Index { get; private set; } = -1;
        public Puzzle Current { get; private set; }
        public bool IsHome => Current==null;
        public bool IsLast => Index==Levels.All.Count-1;
        public void Start(int index=0)
        {
            if(index<0 || index>=Levels.All.Count) throw new ArgumentOutOfRangeException(nameof(index));
            Index=index;Current=new Puzzle(Levels.All[index]);
        }
        public bool Advance()
        {
            if(Current==null || Current.State!=Phase.Completed) return false;
            if(IsLast) Home();else Start(Index+1);return true;
        }
        public void Home() { Current=null;Index=-1; }
    }
}
