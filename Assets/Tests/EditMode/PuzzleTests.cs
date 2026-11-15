using System.Linq;
using NUnit.Framework;

namespace PixelMoments.Tests
{
    public class PuzzleTests
    {
        [Test] public void CluesMatchStarAndSolutionIsUnique()
        {
            var p = new Puzzle();
            CollectionAssert.AreEqual(new[] {1},p.RowClues(0));
            CollectionAssert.AreEqual(new[] {5},p.RowClues(1));
            CollectionAssert.AreEqual(new[] {2,2},p.RowClues(3));
            CollectionAssert.AreEqual(new int[0],Puzzle.Clues(new[]{false,false}));
            Assert.AreEqual(1,Puzzle.CountSolutions());
        }
        [Test] public void CorrectMarksAndErasingCostNothing()
        {
            var p=new Puzzle();
            p.Submit(0,0,Mark.Empty,out bool error); Assert.IsFalse(error);
            p.Submit(0,0,Mark.Unknown,out error); Assert.IsFalse(error);
            p.Submit(0,2,Mark.Filled,out error); Assert.IsFalse(error);
            Assert.AreEqual(5,p.Reasoning);
        }
        [Test] public void ErrorsLockAndOnlyResetRestoresBoard()
        {
            var p=new Puzzle();
            for(int c=0;c<5;c++) p.Submit(0,c,p.Answer(0,c)?Mark.Empty:Mark.Filled,out _);
            Assert.AreEqual(Phase.Locked,p.State); Assert.AreEqual(0,p.Reasoning);
            Assert.IsFalse(p.Submit(1,0,Mark.Filled,out _));
            Assert.IsTrue(p.Reset()); Assert.AreEqual(5,p.Reasoning);
            Assert.AreEqual(0,p.MarkedCount); Assert.AreEqual(Phase.Playing,p.State);
        }
        [Test] public void CompletionRequiresEveryCellAndCannotBeReset()
        {
            var p=new Puzzle();
            for(int i=0;i<24;i++) p.Submit(i/5,i%5,p.Answer(i/5,i%5)?Mark.Filled:Mark.Empty,out _);
            Assert.AreEqual(Phase.Playing,p.State);
            p.Submit(4,4,Mark.Filled,out _); Assert.AreEqual(Phase.Completed,p.State);
            Assert.IsFalse(p.Reset()); Assert.IsFalse(p.Submit(0,0,Mark.Filled,out _));
        }
        [Test] public void RepeatedIdenticalSubmissionDoesNotSpendAnotherPoint()
        {
            var p=new Puzzle();p.Submit(0,0,Mark.Filled,out _);
            for(int i=0;i<100;i++) Assert.IsFalse(p.Submit(0,0,Mark.Filled,out _));
            Assert.AreEqual(4,p.Reasoning);
            for(int i=0;i<100;i++) p.Reset();
            Assert.AreEqual(5,p.Reasoning);Assert.AreEqual(0,p.MarkedCount);
        }
    }
}
