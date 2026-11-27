using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;
public sealed class LocalizationTests
{
    [Fact] public void DiagnosticsKeepCodesAndTranslateMessages()
    {
        var grid=new CellGrid(new PixelSize(1,1),new PixelSize(1,1));
        var es=SheetValidation.Analyze(grid,new byte[4]);
        var en=SheetValidation.Analyze(grid,new byte[4],english:true);
        Assert.Equal(es.Findings[0].Code,en.Findings[0].Code);
        Assert.Contains("transparent",en.Findings[0].Message);
        Assert.Contains("Finding",en.Findings[0].ToString());
        var report=ValidationReport.Create("0.6.3",DateTimeOffset.UtcNow,"Mi proyecto","sprite",grid,en,0,0,0,en.Findings);
        Assert.Contains("VALIDATION REPORT",report.ToPlainText(true));
        Assert.Contains("Mi proyecto",report.ToPlainText(true));
    }
}
