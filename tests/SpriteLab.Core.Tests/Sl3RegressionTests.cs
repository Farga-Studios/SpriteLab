using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class Sl3RegressionTests
{
    [Fact]
    public void OversizedGridIsRejectedBeforeAnalysis()
    {
        var grid = new CellGrid(new PixelSize(257, 256), new PixelSize(1, 1));
        Assert.Throws<ArgumentException>(() => SheetValidation.CheckGridLimit(grid));
        Assert.Throws<ArgumentException>(() => SheetValidation.Analyze(grid, new byte[257 * 256 * 4]));
        SheetValidation.CheckGridLimit(new CellGrid(new PixelSize(256, 256), new PixelSize(1, 1)));
    }

    [Fact]
    public void AnalysisFilterAndReportPreserveOriginalPixelsAndGlobalCounts()
    {
        var grid = new CellGrid(new PixelSize(12, 4), new PixelSize(4, 4));
        var pixels = new byte[192];
        pixels[(12 + 1) * 4 + 3] = 255;
        pixels[(12 + 5) * 4 + 3] = 255;
        var original = pixels.ToArray();
        var sheet = SheetValidation.Analyze(grid, pixels);
        var clip = ClipValidation.Analyze(grid, sheet.Bounds, [new("Idle", [new("front", [0, 1, 2])])]);
        var findings = sheet.Findings.Concat(clip.Findings).ToArray();
        var filtered = DiagnosticFilter.Apply(findings, DiagnosticCategory.Duplicate);
        Assert.Single(filtered);
        var report = ValidationReport.Create("0.3.0", DateTimeOffset.UnixEpoch, "Test", "sheet.png",
            grid, sheet, 0, clip.TotalFindings, 0, findings);
        Assert.Equal(3, report.TotalFindings);
        Assert.Equal(1, report.EmptyCells);
        Assert.Equal(1, report.DuplicateCells);
        Assert.Equal(1, report.AnimationFindings);
        Assert.Equal(original, pixels);
    }
}
