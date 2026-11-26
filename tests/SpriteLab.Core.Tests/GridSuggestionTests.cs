using SpriteLab.Core;
using Xunit;
namespace SpriteLab.Core.Tests;

public sealed class GridSuggestionTests
{
    [Theory]
    [InlineData(48, 48)]
    [InlineData(48, 64)]
    public void RepeatedSeparatedSpritesSuggestExactGrid(int w, int h)
    {
        var image = new PixelSize(w * 5, h * 3);
        var rgba = new byte[image.Width * image.Height * 4];
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < 5; col++)
                for (var y = 8; y < h - 8; y++)
                    for (var x = 8; x < w - 8; x++)
                        rgba[((row * h + y) * image.Width + col * w + x) * 4 + 3] = 255;
        var candidates = GridSuggestions.Analyze(image, rgba);
        Assert.Contains(candidates, c => c.Width == w && c.Height == h);
        Assert.All(candidates, c => { Assert.Equal(0, image.Width % c.Width); Assert.Equal(0, image.Height % c.Height); });
    }

    [Fact]
    public void FullyTransparentSheetHasNoSuggestion()
    {
        Assert.Empty(GridSuggestions.Analyze(new PixelSize(48, 48), new byte[48 * 48 * 4]));
    }
}
