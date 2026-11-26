using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class DiagnosticFilterTests
{
    [Fact]
    public void FilterKeepsLimitNoticeAndGroupsAnimationCodes()
    {
        SheetFinding[] findings = [new("EDGE", 0, ""), new("TRACK_LABEL", 1, ""), new("TRACK_TIMING", null, ""), new("LIMIT", null, "")];
        Assert.Equal(new[] { "TRACK_LABEL", "TRACK_TIMING", "LIMIT" },
            DiagnosticFilter.Apply(findings, DiagnosticCategory.Animation).Select(f => f.Code).ToArray());
        Assert.Equal(4, DiagnosticFilter.Apply(findings, DiagnosticCategory.All).Length);
    }

    [Fact]
    public void MarkersCombineCategoriesAndIgnoreNonNavigableFindings()
    {
        SheetFinding[] findings = [new("EDGE", 3, ""), new("DUPLICATE", 3, ""), new("EDGE", 3, ""), new("TRACK_EMPTY", null, ""), new("LIMIT", null, "")];
        var marker = Assert.Single(DiagnosticFilter.Markers(findings));
        Assert.Equal(3, marker.Key);
        Assert.Equal(new[] { DiagnosticCategory.Edge, DiagnosticCategory.Duplicate }, marker.Value);
    }

    [Fact]
    public void NoMatchesProduceNoMarkers()
    {
        Assert.Empty(DiagnosticFilter.Markers(DiagnosticFilter.Apply([new("EDGE", 2, "")], DiagnosticCategory.Empty)));
    }
}
