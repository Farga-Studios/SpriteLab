using SpriteLab.Infrastructure;
using Xunit;

namespace SpriteLab.Infrastructure.Tests;

public sealed class FrameDocumentTests
{
    private static SheetEntry Sheet() => new(Guid.NewGuid(), "sheet.png", new string('A', 64), 96, 48, 48, 48);

    [Fact]
    public void SharedCellsHaveStableIdsAndOccurrencesKeepTiming()
    {
        var sheet = Sheet();
        var clip = new ViewerClip(Guid.NewGuid(), "Walk", sheet.Id, [1, 0, 1],
            [new("front", [1, 0, 1], 12, [null, 200, 300])]);
        var document = FrameDocument.FromViewer([clip], [sheet]);
        Assert.Equal(2, document.Frames.Length);
        var track = Assert.Single(Assert.Single(document.Clips).Tracks);
        Assert.Equal(track.FrameIds[0], track.FrameIds[2]);
        Assert.Equal(FrameDocument.FrameId(sheet.Id, 1), track.FrameIds[0]);
        var restored = Assert.Single(document.ToViewer([sheet]));
        Assert.Equal(clip.Id, restored.Id);
        Assert.Equal(clip.Cells, restored.Cells);
        Assert.Equal(new int?[] { null, 200, 300 }, restored.EffectiveTracks[0].DurationsMs);
        Assert.Single(document.ToCore(Guid.NewGuid(), "Test", [sheet]).Clips);
    }

    [Fact]
    public void MissingFrameReferenceIsRejected()
    {
        var sheet = Sheet();
        var doc = new FrameDocument([], [new(Guid.NewGuid(), "Walk", sheet.Id,
            [new("front", [Guid.NewGuid()], null, null)])]);
        Assert.Throws<InvalidDataException>(() => doc.ToViewer([sheet]));
    }

    [Fact]
    public void IdenticalCellNumbersOnDifferentSheetsHaveDistinctIds()
    {
        Assert.NotEqual(FrameDocument.FrameId(Guid.NewGuid(), 0), FrameDocument.FrameId(Guid.NewGuid(), 0));
    }
}
