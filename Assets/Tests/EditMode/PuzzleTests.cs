using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PixelMoments.Tests
{
    public class PuzzleTests
    {
        private static void Complete(Puzzle p,int count=225)
        {for(int i=0;i<count;i++)p.Submit(i/15,i%15,p.Answer(i/15,i%15)?Mark.Filled:Mark.Empty,out _);}
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
        public void CompletionRequires225CorrectCellsAndCannotReset(int index)
        {
            var p=new Puzzle(Levels.All[index]);Complete(p,224);Assert.AreEqual(Phase.Playing,p.State);
            p.Submit(14,14,p.Answer(14,14)?Mark.Filled:Mark.Empty,out _);
            Assert.AreEqual(Phase.Completed,p.State);Assert.IsFalse(p.Reset());
            Assert.IsFalse(p.Submit(0,0,Mark.Unknown,out _));
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
            stroke.Visit(3,0,add);stroke.Visit(3,14,add);stroke.Visit(7,3,add);stroke.Visit(2,14,add);
            CollectionAssert.AreEqual(Enumerable.Range(0,15),seen);
        }
        [Test] public void StrokeInterpolatesVerticalAndClampsOutsideBoard()
        {
            var seen=new List<int>();var stroke=new LineStroke(14,6,Mark.Empty);
            stroke.Visit(14,6,(r,c,m)=>seen.Add(r));
            stroke.Visit(-20,8,(r,c,m)=> {Assert.AreEqual(6,c);Assert.AreEqual(Mark.Empty,m);seen.Add(r);});
            CollectionAssert.AreEqual(Enumerable.Range(0,15).Reverse(),seen);
        }
        [Test] public void StrokeStopsChangingPuzzleWhenReasoningRunsOut()
        {
            var p=new Puzzle();new LineStroke(0,0,Mark.Filled).Visit(0,14,(r,c,m)=>p.Submit(r,c,m,out _));
            Assert.AreEqual(Phase.Locked,p.State);Assert.AreEqual(5,p.MarkedCount);
        }
        [Test] public void DifficultyIncreasesByFewerSeedsAndMoreSegments()
        {
            var levels=Levels.All;
            for(int i=1;i<levels.Count;i++) {
                Assert.Greater(NonogramSolver.Analyze(levels[i-1]).InitialForcedCells,NonogramSolver.Analyze(levels[i]).InitialForcedCells);
                Assert.Less(levels[i-1].Segments,levels[i].Segments);
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
        [Test] public void InvalidLevelsAndIndicesAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new Level("x","x","x","x","FFFFFF",new[]{"."}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new Campaign().Start(3));
        }
    }
}
