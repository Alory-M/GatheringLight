using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PixelMoments.Tests
{
    public class PuzzleTests
    {
        private static void Complete(Puzzle p)
        {for(int r=0;r<15;r++)for(int c=0;c<15;c++)if(p.Answer(r,c))p.Submit(r,c,Mark.Filled,out _);}
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void LevelsHaveUniqueSolutionsAndGeneratedClues(int index)
        {
            var l=Levels.All[index];var result=NonogramSolver.Analyze(l);
            Console.WriteLine($"{l.Title}: solutions={result.Solutions}, search nodes={result.SearchNodes}, initial forced={result.InitialForcedCells}, segments={l.Segments}");
            Assert.AreEqual(1,result.Solutions);
            for(int r=0;r<15;r++) {
                Assert.Contains(Enumerable.Range(0,15).Aggregate(0,(m,c)=>m|(l.Answer(r,c)?1<<c:0)),NonogramSolver.Patterns(l.RowClues(r)));
                Assert.Contains(Enumerable.Range(0,15).Aggregate(0,(m,c)=>m|(l.Answer(c,r)?1<<c:0)),NonogramSolver.Patterns(l.ColumnClues(r)));
            }
        }
        [Test] public void EmptyAndSegmentedLinesGenerateValidPatterns()
        {
            CollectionAssert.AreEqual(new int[0],Puzzle.Clues(new[]{false,false}));
            CollectionAssert.AreEqual(new[]{1,2},Puzzle.Clues(new[]{true,false,true,true}));
            CollectionAssert.AreEqual(new[]{0},NonogramSolver.Patterns(new int[0]));
            CollectionAssert.AreEqual(new[]{32767},NonogramSolver.Patterns(new[]{15}));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CorrectMarksAndErasureCostNothing(int index)
        {
            var p=new Puzzle(Levels.All[index]);
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                p.Submit(r,c,p.Answer(r,c)?Mark.Filled:Mark.Empty,out bool error);Assert.IsFalse(error);
                p.Submit(r,c,Mark.Unknown,out error);Assert.IsFalse(error);
            }
            Assert.AreEqual(5,p.Reasoning);Assert.AreEqual(0,p.MarkedCount);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FiveErrorsLockAndResetRestoresAll225Cells(int index)
        {
            var p=new Puzzle(Levels.All[index]);
            for(int c=0;c<5;c++) {p.Submit(0,c,p.Answer(0,c)?Mark.Empty:Mark.Filled,out bool error);Assert.IsTrue(error);Assert.AreEqual(4-c,p.Reasoning);}
            Assert.AreEqual(Phase.Locked,p.State);
            Assert.IsFalse(p.Submit(1,0,Mark.Filled,out _));
            Assert.IsFalse(p.Submit(0,0,Mark.Unknown,out _));
            Assert.IsTrue(p.Reset());Assert.AreEqual(5,p.Reasoning);Assert.AreEqual(0,p.MarkedCount);
            Assert.AreEqual(Phase.Playing,p.State);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CompletionRequiresOnlyArtPixelsAndCannotReset(int index)
        {
            var p=new Puzzle(Levels.All[index]);
            var filled=Enumerable.Range(0,225).Where(i=>p.Answer(i/15,i%15)).ToArray();
            foreach(int i in filled.Take(filled.Length-1))p.Submit(i/15,i%15,Mark.Filled,out _);
            Assert.AreEqual(Phase.Playing,p.State);
            int last=filled.Last();p.Submit(last/15,last%15,Mark.Filled,out _);
            Assert.AreEqual(Phase.Completed,p.State);Assert.IsFalse(p.Reset());
            Assert.AreEqual(p.Level.FilledCount,p.FilledCount);
            Assert.AreEqual(Mark.Unknown,p.Cell(0,0)); // No crosses are needed.
            Assert.IsFalse(p.Submit(0,0,Mark.Empty,out _));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void WrongBlankFillPreventsCompletionUntilErased(int index)
        {
            var p=new Puzzle(Levels.All[index]);p.Submit(0,0,Mark.Filled,out _);Complete(p);
            Assert.AreEqual(Phase.Playing,p.State);Assert.AreEqual(p.Level.FilledCount,p.FilledCount);
            p.Submit(0,0,Mark.Unknown,out _);Assert.AreEqual(Phase.Completed,p.State);
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void OptionalCrossesDoNotChangeCompletionRule(int index)
        {
            var p=new Puzzle(Levels.All[index]);p.Submit(0,0,Mark.Empty,out _);Complete(p);
            Assert.AreEqual(Phase.Completed,p.State);Assert.AreEqual(5,p.Reasoning);
        }
        [Test] public void RepeatedSameSubmissionAndResetsAreStable()
        {
            var p=new Puzzle();var mark=p.Answer(0,0)?Mark.Empty:Mark.Filled;
            p.Submit(0,0,mark,out _);
            for(int i=0;i<100;i++)Assert.IsFalse(p.Submit(0,0,mark,out _));
            Assert.AreEqual(4,p.Reasoning);
            for(int i=0;i<100;i++)p.Reset();Assert.AreEqual(5,p.Reasoning);Assert.AreEqual(0,p.MarkedCount);
        }
        [Test] public void CampaignRunsThreeLevelsThenReturnsHome()
        {
            var flow=new Campaign();Assert.IsTrue(flow.IsHome);Assert.IsFalse(flow.Advance());
            flow.Start();Assert.IsFalse(flow.Advance());
            for(int i=0;i<3;i++) {
                Assert.AreEqual(i,flow.Index);Assert.AreEqual(i==2,flow.IsLast);
                Complete(flow.Current);Assert.IsTrue(flow.Advance());
                if(i<2) {Assert.AreEqual(5,flow.Current.Reasoning);Assert.AreEqual(0,flow.Current.MarkedCount);}
            }
            Assert.IsTrue(flow.IsHome);flow.Start();Assert.AreEqual(0,flow.Index);
        }
        [Test] public void StrokeInterpolatesHorizontalAndNeverRepeatsCells()
        {
            var seen=new List<int>();var stroke=new LineStroke(3,0,Mark.Filled);
            Action<int,int,Mark> add=(r,c,m)=> {Assert.AreEqual(3,r);Assert.AreEqual(Mark.Filled,m);seen.Add(c);};
            stroke.Visit(3,0,add);stroke.Visit(3,14,add);stroke.Visit(3,3,add);stroke.Visit(3,14,add);
            CollectionAssert.AreEqual(Enumerable.Range(0,15),seen);
        }
        [Test] public void StrokeInterpolatesVerticalToBoardEdge()
        {
            var seen=new List<int>();var stroke=new LineStroke(14,6,Mark.Empty);
            stroke.Visit(14,6,(r,c,m)=>seen.Add(r));
            stroke.Visit(-20,6,(r,c,m)=> {Assert.AreEqual(6,c);Assert.AreEqual(Mark.Empty,m);seen.Add(r);});
            CollectionAssert.AreEqual(Enumerable.Range(0,15).Reverse(),seen);
        }
        [Test] public void DiagonalStrokePaintsDiagonalWithoutPaintingCornerNeighbours()
        {
            var seen=new List<int>();var stroke=new LineStroke(0,0,Mark.Filled);
            stroke.Visit(14,14,(r,c,m)=> {Assert.AreEqual(r,c);seen.Add(r);});
            CollectionAssert.AreEqual(Enumerable.Range(0,15),seen);
        }
        [Test] public void StrokeCanTurnFreelyAndBacktrackWithoutDuplicates()
        {
            var seen=new List<int>();var stroke=new LineStroke(0,0,Mark.Empty);
            Action<int,int,Mark> add=(r,c,m)=>seen.Add(r*15+c);
            stroke.Visit(0,2,add);stroke.Visit(2,2,add);stroke.Visit(0,0,add);
            CollectionAssert.AreEquivalent(new[]{0,1,2,17,32,16},seen);
            Assert.AreEqual(seen.Count,seen.Distinct().Count());
        }
        [Test] public void FractionalPathIncludesEveryActuallyCrossedCell()
        {
            var seen=new List<int>();var stroke=new LineStroke(.1,.9,Mark.Filled);
            stroke.Visit(2.9,1.1,(r,c,m)=>seen.Add(r*15+c));
            CollectionAssert.AreEqual(new[]{0,15,16,31},seen);
        }
        [Test] public void MovingOutsideDoesNotPaintAnImaginaryBoardEdge()
        {
            var seen=new List<int>();var stroke=new LineStroke(.5,.5,Mark.Filled);
            Action<int,int,Mark> add=(r,c,m)=>seen.Add(r*15+c);
            stroke.Visit(-3.0,-3.0,add);stroke.Visit(-3.0,20.0,add);
            CollectionAssert.AreEqual(new[]{0},seen);
        }
        [Test] public void StrokeStopsChangingPuzzleWhenReasoningRunsOut()
        {
            var p=new Puzzle();new LineStroke(0,0,Mark.Filled).Visit(0,14,(r,c,m)=>p.Submit(r,c,m,out _));
            Assert.AreEqual(Phase.Locked,p.State);Assert.AreEqual(5,p.MarkedCount);
        }
        [Test] public void DifficultyIncreasesByFewerForcedColorSeeds()
        {
            var levels=Levels.All;
            for(int i=1;i<levels.Count;i++) {
                Assert.Greater(ColorNonogramSolver.Analyze(levels[i-1]).InitialForcedCells,ColorNonogramSolver.Analyze(levels[i]).InitialForcedCells);
            }
        }
        [Test] public void TapAndBacktrackingStrokeSubmitOnlyOnce()
        {
            var p=new Puzzle();var stroke=new LineStroke(0,0,Mark.Filled);
            stroke.Visit(0,0,(r,c,m)=>p.Submit(r,c,m,out _));
            stroke.Visit(0,0,(r,c,m)=>p.Submit(r,c,m,out _));
            Assert.AreEqual(4,p.Reasoning);Assert.AreEqual(1,p.MarkedCount);
            stroke.Visit(0,1,(r,c,m)=>p.Submit(r,c,m,out _));
            stroke.Visit(0,0,(r,c,m)=>p.Submit(r,c,m,out _));
            Assert.AreEqual(3,p.Reasoning);Assert.AreEqual(2,p.MarkedCount);
        }
        [Test] public void EveryRevealPixelHasAValidColorAndArtHasMultipleColors()
        {
            foreach(var level in Levels.All) {
                var colors=new HashSet<string>();
                for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                    string color=level.PixelColor(r,c);
                    Assert.IsTrue(color.Length==6 && color.All(x=>Uri.IsHexDigit(x)));
                    if(level.Answer(r,c))colors.Add(color);
                }
                Assert.GreaterOrEqual(colors.Count,4);
            }
        }
        [Test] public void ColorCluesSplitAdjacentDifferentColorsAndBlankGaps()
        {
            var clues=Level.ColorClues("YYO.OYY".ToCharArray());
            CollectionAssert.AreEqual(new[]{'Y','O','O','Y'},clues.Select(x=>x.Key));
            CollectionAssert.AreEqual(new[]{2,1,1,2},clues.Select(x=>x.Length));
            Assert.IsEmpty(Level.ColorClues("...".ToCharArray()));
            var candidates=ColorNonogramSolver.Patterns(new[]{new ColorClue('Y',1),new ColorClue('O',1)});
            Assert.IsTrue(candidates.Any(x=>x.StartsWith("YO")));
            Assert.IsFalse(ColorNonogramSolver.Patterns(new[]{new ColorClue('Y',1),new ColorClue('Y',1)}).Any(x=>x.StartsWith("YY")));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void BothAxesHaveMatchingColorSequencesAndUniqueColorSolution(int index)
        {
            var level=Levels.All[index];var solved=ColorNonogramSolver.Analyze(level);
            Console.WriteLine($"Color puzzle {level.Title}: solutions={solved.Solutions}, search nodes={solved.SearchNodes}, forced={solved.InitialForcedCells}");
            Assert.AreEqual(1,solved.Solutions);
            for(int i=0;i<15;i++) {
                string row=new string(Enumerable.Range(0,15).Select(c=>level.PixelKey(i,c)).ToArray());
                string col=new string(Enumerable.Range(0,15).Select(r=>level.PixelKey(r,i)).ToArray());
                CollectionAssert.Contains(ColorNonogramSolver.Patterns(level.RowColorClues(i)),row);
                CollectionAssert.Contains(ColorNonogramSolver.Patterns(level.ColumnColorClues(i)),col);
            }
        }
        [TestCase(0,"8605cd16d479d29bde587437018d302a39c465f03c7dc474c2c90b88854cb836")]
        [TestCase(1,"fa4939bb41746ddb9d30405f7a9e39f5263b9cc182c665c7253f377fd15f949f")]
        [TestCase(2,"d3160e1517a63d7deca499120f15b03a7d8a8674ed7ef1c03547b4ef401fd983")]
        public void PixelColorsMatchApprovedReferenceImage(int index,string expected)
        {
            var level=Levels.All[index];var rgb=new List<byte>();
            for(int r=0;r<15;r++)for(int c=0;c<15;c++) {
                string hex=level.PixelColor(r,c);
                for(int channel=0;channel<3;channel++)rgb.Add(Convert.ToByte(hex.Substring(channel*2,2),16));
            }
            using(var hash=System.Security.Cryptography.SHA256.Create())
                Assert.AreEqual(expected,BitConverter.ToString(hash.ComputeHash(rgb.ToArray())).Replace("-","").ToLowerInvariant());
        }
        [Test] public void InvalidLevelsAndIndicesAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new Level("x","x","x","x","FFFFFF",new[]{"."}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new Campaign().Start(3));
        }
    }
}
