using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class EditHistoryTests
{
    [Fact]
    public void UndoRedoAndBranch()
    {
        var history = new EditHistory<int>();
        history.Record(0,"first");
        history.Record(1,"second");
        Assert.Equal("second",history.UndoLabel);
        Assert.Equal(1,history.Undo(2));
        Assert.Equal(0,history.Undo(1));
        Assert.Equal(1,history.Redo(0));
        history.Record(1,"new");
        Assert.False(history.CanRedo);
        Assert.Equal(1,history.Undo(3));
    }

    [Fact]
    public void CapacityAndClear()
    {
        var history = new EditHistory<int>(2);
        history.Record(0,"a"); history.Record(1,"b"); history.Record(2,"c");
        Assert.Equal(2,history.Undo(3));
        Assert.Equal(1,history.Undo(2));
        Assert.False(history.CanUndo);
        history.Clear();
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void ReplacementPreservesOccurrenceTimingAndOriginal()
    {
        int[] original=[0,1,0,2];
        int?[] times=[null,250,80,null];
        var result=TimelineEdit.Apply(original,times,2,TimelineOperation.Replace,3);
        Assert.Equal(new[]{0,1,3,2},result.Cells);
        Assert.Equal(times,result.Durations);
        Assert.Equal(2,result.Selected);
        Assert.Equal(0,original[2]);
        Assert.Throws<ArgumentException>(()=>TimelineEdit.Apply(original,times,2,TimelineOperation.Replace,-1));
    }
}
