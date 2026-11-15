using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelMoments
{
    public static class ColorNonogramSolver
    {
        public static List<string> Patterns(ColorClue[] clues)
        {
            var result=new List<string>();var pixels=Enumerable.Repeat('.',Level.Size).ToArray();
            Action<int,int> place=null;
            place=(index,start)=> {
                if(index==clues.Length) {result.Add(new string(pixels));return;}
                int remaining=0;
                for(int i=index;i<clues.Length;i++) {
                    remaining+=clues[i].Length;
                    if(i+1<clues.Length && clues[i].Key==clues[i+1].Key)remaining++;
                }
                var clue=clues[index];
                for(int at=start;at<=Level.Size-remaining;at++) {
                    for(int p=at;p<at+clue.Length;p++)pixels[p]=clue.Key;
                    int gap=index+1<clues.Length && clue.Key==clues[index+1].Key?1:0;
                    place(index+1,at+clue.Length+gap);
                    for(int p=at;p<at+clue.Length;p++)pixels[p]='.';
                }
            };
            place(0,0);return result;
        }
        public static SolverResult Analyze(Level level,int limit=2)
        {
            if(limit<1)throw new ArgumentOutOfRangeException(nameof(limit));
            var result=new SolverResult();
            var rows=Enumerable.Range(0,Level.Size).Select(i=>Patterns(level.RowColorClues(i))).ToArray();
            var cols=Enumerable.Range(0,Level.Size).Select(i=>Patterns(level.ColumnColorClues(i))).ToArray();
            for(int r=0;r<Level.Size;r++)for(int c=0;c<Level.Size;c++)
                if(rows[r].Select(line=>line[c]).Distinct().Count()==1)result.InitialForcedCells++;
            Search(rows,cols,result,limit);return result;
        }
        private static void Search(List<string>[] rows,List<string>[] cols,SolverResult result,int limit)
        {
            if(result.Solutions>=limit)return;
            result.SearchNodes++;bool changed;
            do {
                changed=false;
                for(int pass=0;pass<2;pass++) {
                    var source=pass==0?rows:cols;var target=pass==0?cols:rows;
                    for(int i=0;i<Level.Size;i++) {
                        if(source[i].Count==0)return;
                        for(int j=0;j<Level.Size;j++) {
                            var possible=new HashSet<char>(source[i].Select(line=>line[j]));
                            int removed=target[j].RemoveAll(line=>!possible.Contains(line[i]));
                            if(target[j].Count==0)return;
                            changed|=removed>0;
                        }
                    }
                }
            }while(changed);
            int best=-1,count=int.MaxValue;
            for(int i=0;i<Level.Size*2;i++) {
                int n=(i<Level.Size?rows[i]:cols[i-Level.Size]).Count;
                if(n>1 && n<count) {best=i;count=n;}
            }
            if(best<0) {result.Solutions++;return;}
            foreach(var line in best<Level.Size?rows[best]:cols[best-Level.Size]) {
                var r=rows.Select(x=>new List<string>(x)).ToArray();var c=cols.Select(x=>new List<string>(x)).ToArray();
                if(best<Level.Size)r[best]=new List<string>{line};else c[best-Level.Size]=new List<string>{line};
                Search(r,c,result,limit);if(result.Solutions>=limit)return;
            }
        }
    }
}
