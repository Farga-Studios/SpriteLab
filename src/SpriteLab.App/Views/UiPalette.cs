using Avalonia.Media;

namespace SpriteLab.App.Views;

// Shared tool-window palette aligned with MainWindow.
internal static class UiPalette
{
    public static readonly IBrush Window = Brush("#171B22");
    public static readonly IBrush Panel = Brush("#202630");
    public static readonly IBrush Viewer = Brush("#11151C");
    public static readonly IBrush Border = Brush("#353E4C");
    public static readonly IBrush Text = Brush("#E3E9F0");
    public static readonly IBrush Muted = Brush("#AEBCCA");
    public static readonly IBrush Icon = Brush("#DCE5EF");
    public static readonly IBrush Accent = Brush("#79C9A0");

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
}
