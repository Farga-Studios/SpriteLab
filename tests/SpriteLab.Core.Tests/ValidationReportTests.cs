using System.Text.Json;
using SpriteLab.Core;
using Xunit;

namespace SpriteLab.Core.Tests;

public sealed class ValidationReportTests
{
    private static ValidationReport Report(bool truncated)
    {
        var grid = new CellGrid(new PixelSize(8, 4), new PixelSize(4, 4));
        var sheet = new SheetValidationResult(2, 0, 0, [], 0, 1, 1);
        SheetFinding[] findings = truncated
            ? [new("DUPLICATE", 1, "Igual a celda 1") { ReferenceCell = 0 }, new("LIMIT", null, "Omitidos")]
            : [];
        return ValidationReport.Create("0.2.7", DateTimeOffset.Parse("2026-11-26T12:00:00+01:00"),
            "Proyecto", "orco.png", grid, sheet, 0, 0, truncated ? 5 : 0, findings);
    }

    [Fact]
    public void JsonUsesOneBasedCellsAndExplicitTruncation()
    {
        using var json = JsonDocument.Parse(Report(true).ToJson());
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("formatVersion").GetInt32());
        Assert.Equal(6, root.GetProperty("totalFindings").GetInt32());
        Assert.Equal(1, root.GetProperty("listedFindings").GetInt32());
        Assert.True(root.GetProperty("isTruncated").GetBoolean());
        var finding = root.GetProperty("findings")[0];
        Assert.Equal(2, finding.GetProperty("cell").GetInt32());
        Assert.Equal(1, finding.GetProperty("referenceCell").GetInt32());
        Assert.Equal("DUPLICATE", finding.GetProperty("code").GetString());
    }

    [Fact]
    public void TextIncludesCountsAndDoesNotExportLimitAsFinding()
    {
        var text = Report(true).ToPlainText();
        Assert.Contains("Omitidos: 5", text);
        Assert.Contains("Referencia: celda 1", text);
        Assert.DoesNotContain("[LIMIT]", text);
        Assert.Contains("revisión manual", text);
    }

    [Fact]
    public void CleanReportIsExportable()
    {
        var report = Report(false);
        Assert.False(report.IsTruncated);
        Assert.Equal(0, report.TotalFindings);
        Assert.Contains("No se detectaron avisos", report.ToPlainText());
        Assert.Equal(TimeSpan.Zero, report.AnalyzedAtUtc.Offset);
    }
}
