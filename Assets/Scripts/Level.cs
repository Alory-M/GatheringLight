using System;
using System.Collections.Generic;
using System.Linq;

namespace PixelMoments
{
    // One pixel source supplies live paint, color-run clues, and the completed print.
    public sealed class Level
    {
        public const int Size = 15;
        private readonly string[] art;
        public string Id { get; }
        public string Title { get; }
        public string Difficulty { get; }
        public string Description { get; }
        public string Accent { get; }
        public int FilledCount => art.Sum(row => row.Count(c => c != '.'));
        public char PixelKey(int row,int col) => art[row][col];
        public int Segments => Enumerable.Range(0, Size).Sum(i => RowClues(i).Length + ColumnClues(i).Length);
        public Level(string id, string title, string difficulty, string description, string accent, string[] pixels)
        {
            if (pixels == null || pixels.Length != Size || pixels.Any(row => row == null || row.Length != Size))
                throw new ArgumentException("Levels must contain exactly 15 × 15 pixels.");
            const string palette = ".YOGBRCWDT";
            if (pixels.Any(row => row.Any(c => !palette.Contains(c)))) throw new ArgumentException("Unknown palette key.");
            Id=id; Title=title; Difficulty=difficulty; Description=description; Accent=accent;
            art=(string[])pixels.Clone();
        }
        public bool Answer(int row, int col) => art[row][col] != '.';
        public string PixelColor(int row,int col) => PaletteColor(art[row][col]);
        public static string PaletteColor(char key)
        {
            switch (key) {
                case 'Y': return "F7C542"; case 'O': return "EF9144"; case 'G': return "69A85D";
                case 'B': return "64ACCE"; case 'R': return "E67762"; case 'C': return "A1D9CF";
                case 'W': return "FFF1C6"; case 'D': return "4B5763"; case 'T': return "AD754F";
                default: return "FFF8E8";
            }
        }
        public ColorClue[] RowColorClues(int row) => ColorClues(Enumerable.Range(0,Size).Select(c => art[row][c]));
        public ColorClue[] ColumnColorClues(int col) => ColorClues(Enumerable.Range(0,Size).Select(r => art[r][col]));
        public static ColorClue[] ColorClues(IEnumerable<char> pixels)
        {
            var result=new List<ColorClue>();char previous='.';int count=0;
            foreach(char key in pixels) {
                if(key==previous) {count++;continue;}
                if(previous!='.')result.Add(new ColorClue(previous,count));
                previous=key;count=1;
            }
            if(previous!='.')result.Add(new ColorClue(previous,count));
            return result.ToArray();
        }
        // Binary silhouette clues remain available for offline difficulty diagnostics.
        public int[] RowClues(int row) => Puzzle.Clues(Enumerable.Range(0,Size).Select(c => Answer(row,c)));
        public int[] ColumnClues(int col) => Puzzle.Clues(Enumerable.Range(0,Size).Select(r => Answer(r,col)));
    }

    public struct ColorClue
    {
        public char Key { get; }
        public int Length { get; }
        public string Color => Level.PaletteColor(Key);
        public ColorClue(char key,int length) {Key=key;Length=length;}
    }

    public static class Levels
    {
        public static readonly IReadOnlyList<Level> All = Array.AsReadOnly(new[] {
            new Level("sunflower", "向阳花开", "入门", "从饱满的花盘开始，寻找长线索的重叠。", "F7C542", new[] {
                ".....YYYYY.....",
                "...YYYYYYYYY...",
                "..YYYYOOOYYYY..",
                ".YYYOOOOOOOYYY.",
                "YYYOOOTOTOOOYYY",
                "YYYOOOTOTOOOYYY",
                ".YYYOOOOOOOYYY.",
                "..YYYYOOOYYYY..",
                "...YYYYYYYYY...",
                ".....YYGYY.....",
                ".......G.......",
                "...GGGGGGGGG...",
                "....GGGGGGG....",
                ".....TTTTT.....",
                "......TTT......"
            }),
            new Level("cottage", "晴日小屋", "进阶", "屋檐与窗格交错，留意分段和空白间隔。", "EF9144", new[] {
                ".........TT....",
                "......R..TT....",
                ".....RRR.TT....",
                "....RRRRRRR....",
                "...RRRRRRRRR...",
                "..RRRRRRRRRRR..",
                ".RRRRRRRRRRRRR.",
                "...WWWWWWWWW...",
                "...W..WYW..W...",
                "...W..WYW..W...",
                "...WWWWYWWWW...",
                "...WWWTTTWWW...",
                "...WWWTTTWWW...",
                ".GGWWWTTTWWWGG.",
                "GGGGGGTTTGGGGGG"
            }),
            new Level("butterfly", "花间来信", "挑战", "对称翅纹里藏着短线索，交叉排除每一格。", "64ACCE", new[] {
                ".BB.........BB.",
                ".BCC..D.D..CCB.",
                ".BCOC..D..COCB.",
                ".BCOOC.D.COOCB.",
                "..BCOOCDCOOCB..",
                "..BCOCCDCCOCB..",
                "...BCCDDDCCB...",
                "....BBBDBBB....",
                "...RROODOORR...",
                "..RROOODOOORR..",
                "..RYOOWDWOOYR..",
                "..RYOW.D.WOYR..",
                "...RY..D..YR...",
                "....R.....R....",
                "..............."
            })
        });
    }
}
