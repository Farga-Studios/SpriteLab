using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;
public sealed class SequenceSelectionTests
{
    [Fact] public void KeepsRepeatedOccurrence() => Assert.Equal((int?)2, SequenceSelection.Find([4, 5, 4], 2, 4));
    [Fact] public void SelectsMatchingOccurrence() => Assert.Equal((int?)1, SequenceSelection.Find([4, 5, 4], 0, 5));
    [Fact] public void OutsideTrackReturnsNull() => Assert.Null(SequenceSelection.Find([4, 5, 4], 0, 9));
}
