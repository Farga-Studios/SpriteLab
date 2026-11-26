using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class ClipValidationTests
{
    private static CellGrid Grid() => new(new PixelSize(12, 4), new PixelSize(4, 4));
    private static Dictionary<int, VisibleBounds> Bounds() => new() { [0] = new(1, 1, 2, 2), [1] = new(1, 1, 2, 2) };

    [Fact]
    public void FreeLabelsDifferentLengthsAndIntentionalRepeatsAreValid()
    {
        var result = ClipValidation.Analyze(Grid(), Bounds(),
            [new("Walk", [new("front", [0, 0, 1], 8), new("back", [1], 12)])]);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void TransparentOccurrencesAreGroupedPerTrack()
    {
        var result = ClipValidation.Analyze(Grid(), Bounds(), [new("Attack", [new("none", [0, 2, 2])])]);
        var warning = Assert.Single(result.Findings);
        Assert.Equal("TRACK_TRANSPARENT", warning.Code);
        Assert.Equal((int?)2, warning.Cell);
    }

    [Fact]
    public void SimilarLabelsAndUnspecifiedMixedTrackAreAdvisory()
    {
        var result = ClipValidation.Analyze(Grid(), Bounds(),
            [new("Walk", [new("front", [0]), new(" FRONT ", [1]), new("none", [0])])]);
        Assert.Equal(new[] { "TRACK_LABEL", "TRACK_UNSPECIFIED" }, result.Findings.Select(f => f.Code).ToArray());
    }

    [Fact]
    public void InvalidReferencesAndTimingRemainCountedWhenListIsCapped()
    {
        var result = ClipValidation.Analyze(Grid(), Bounds(), [new("Bad", [new("front", [9], 61)])], 1);
        Assert.Equal(2, result.TotalFindings);
        Assert.Equal(1, result.OmittedFindings);
        Assert.Equal("TRACK_REFERENCE", Assert.Single(result.Findings).Code);
    }

    [Fact]
    public void CancelledAnalysisStops()
    {
        Assert.Throws<OperationCanceledException>(() => ClipValidation.Analyze(Grid(), Bounds(),
            [new("Walk", [new("front", [0])])], token: new CancellationToken(true)));
    }
}
