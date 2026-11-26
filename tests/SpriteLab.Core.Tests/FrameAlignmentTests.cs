using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;
public sealed class FrameAlignmentTests
{
    private static CellGrid Grid() => new(new PixelSize(192, 48), new PixelSize(48, 48));
    [Fact] public void SelectionDeduplicatesRanges() => Assert.Equal(new[] {0,1,2,3}, FrameAlignment.ParseSelection("1,2-4,3",4));
    [Fact] public void SnapOnlyWithinTolerance() { Assert.Equal(10, FrameAlignment.Snap(8,[0,10],3)); Assert.Equal(6,FrameAlignment.Snap(6,[0,10],3)); }
    [Fact] public void BatchPreservesIndividualOffsets()
    {
        var original = new FrameOffset[] { new(0,3,1), new(1,-2,4) };
        var edited = FrameAlignment.EditMany(Grid(),original,[0,1],(c,o)=>o with { X=o.X+2 });
        Assert.Equal(5,edited[0].X); Assert.Equal(0,edited[1].X); Assert.Equal(3,original[0].X);
        Assert.Throws<ArgumentException>(()=>FrameAlignment.EditMany(Grid(),original,[0,1],(c,o)=>o with { X=100 }));
    }
    [Fact] public void AnchorsCanBeSetAndClearedIndependently()
    {
        var a=FrameAnchor.SetMany(Grid(),[],[0,1],24,48);
        Assert.Equal(2,a.Length);
        Assert.Single(FrameAnchor.SetMany(Grid(),a,[0],null,null));
        Assert.Throws<ArgumentException>(()=>FrameAnchor.SetMany(Grid(),a,[1],49,0));
    }
}
