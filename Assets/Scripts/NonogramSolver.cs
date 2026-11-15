using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelMoments
{
    public sealed class SolverResult
    {
        public int Solutions { get; internal set; }
        public int SearchNodes { get; internal set; }
        public int InitialForcedCells { get; internal set; }
    }
    public static class NonogramSolver
    {
        private const int Full=(1<<Level.Size)-1;
        public static List<int> Patterns(int[] clues)
        {
            var result=new List<int>();
            Action<int,int,int> place=null;
            place=(index,start,mask)=> {
                if(index==clues.Length) {result.Add(mask);return;}
                int remaining=clues.Skip(index).Sum()+clues.Length-index-1;
                for(int at=start;at<=Level.Size-remaining;at++)
                    place(index+1,at+clues[index]+1,mask|(((1<<clues[index])-1)<<at));
            };
            place(0,0,0);return result;
        }
        public static SolverResult Analyze(Level level,int limit=2)
        {
            if(limit<1) throw new ArgumentOutOfRangeException(nameof(limit));
            var result=new SolverResult();
            var rows=Enumerable.Range(0,Level.Size).Select(i=>Patterns(level.RowClues(i))).ToArray();
            var cols=Enumerable.Range(0,Level.Size).Select(i=>Patterns(level.ColumnClues(i))).ToArray();
            for(int r=0;r<Level.Size;r++) {
                int all=rows[r].Aggregate(Full,(a,b)=>a&b),any=rows[r].Aggregate(0,(a,b)=>a|b);
                for(int c=0;c<Level.Size;c++) if((all&(1<<c))!=0 || (any&(1<<c))==0) result.InitialForcedCells++;
            }
            Search(rows,cols,result,limit);return result;
        }
        private static void Search(List<int>[] rows,List<int>[] cols,SolverResult result,int limit)
        {
            if(result.Solutions>=limit) return;
            result.SearchNodes++;
            bool changed;
            do {
                changed=false;
                for(int pass=0;pass<2;pass++) {
                    var source=pass==0?rows:cols;var target=pass==0?cols:rows;
                    for(int i=0;i<Level.Size;i++) {
                        if(source[i].Count==0) return;
                        int all=source[i].Aggregate(Full,(a,b)=>a&b),any=source[i].Aggregate(0,(a,b)=>a|b);
                        for(int j=0;j<Level.Size;j++) {
                            if((all&(1<<j))==0 && (any&(1<<j))!=0) continue;
                            bool value=(all&(1<<j))!=0;int bit=1<<i;
                            int removed=target[j].RemoveAll(mask=>((mask&bit)!=0)!=value);
                            if(target[j].Count==0) return;
                            changed|=removed>0;
                        }
                    }
                }
            } while(changed);
            int best=-1,count=int.MaxValue;
            for(int i=0;i<Level.Size*2;i++) {
                int n=(i<Level.Size?rows[i]:cols[i-Level.Size]).Count;
                if(n>1 && n<count) {best=i;count=n;}
            }
            if(best<0) {result.Solutions++;return;}
            var options=best<Level.Size?rows[best]:cols[best-Level.Size];
            foreach(int mask in options) {
                var r=rows.Select(line=>new List<int>(line)).ToArray();
                var c=cols.Select(line=>new List<int>(line)).ToArray();
                if(best<Level.Size) r[best]=new List<int>{mask};else c[best-Level.Size]=new List<int>{mask};
                Search(r,c,result,limit);if(result.Solutions>=limit) return;
            }
        }
    }
}
