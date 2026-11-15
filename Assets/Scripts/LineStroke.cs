using System;
using System.Collections.Generic;

namespace PixelMoments
{
    // First movement chooses a row or column. Interpolation covers skipped cells at any frame rate.
    public sealed class LineStroke
    {
        private readonly HashSet<int> visited=new HashSet<int>();
        private readonly int startRow,startCol;
        private int axis,last;
        public Mark Mark { get; }
        public LineStroke(int row,int col,Mark mark)
        { startRow=row;startCol=col;last=col;Mark=mark; }
        public void Visit(int row,int col,Action<int,int,Mark> apply)
        {
            row=Math.Max(0,Math.Min(Puzzle.Size-1,row));col=Math.Max(0,Math.Min(Puzzle.Size-1,col));
            if(axis==0 && (row!=startRow || col!=startCol)) {
                axis=Math.Abs(col-startCol)>=Math.Abs(row-startRow)?1:2;
                last=axis==1?startCol:startRow;
            }
            int target=axis==2?row:col;
            if(axis==0) target=startCol;
            int step=target>=last?1:-1;
            for(int p=last;;p+=step) {
                int r=axis==2?p:startRow,c=axis==1?p:startCol;
                if(visited.Add(r*Puzzle.Size+c)) apply(r,c,Mark);
                if(p==target) break;
            }
            last=target;
        }
    }
}
