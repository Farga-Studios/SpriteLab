using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Automation;
using IconPath = Avalonia.Controls.Shapes.Path;

namespace SpriteLab.App.Views;

internal static class ToolbarIcons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["play"] = "M7,4 L20,12 7,20 Z", ["pause"] = "M7,4 L7,20 M17,4 L17,20",
        ["stop"] = "M5,5 L19,5 19,19 5,19 Z",
        ["previous"] = "M15,5 L8,12 15,19 M4,5 L4,19",
        ["next"] = "M9,5 L16,12 9,19 M20,5 L20,19",
        ["plus"] = "M12,4 L12,20 M4,12 L20,12", ["minus"] = "M4,12 L20,12",
        ["zoom-in"] = "M10,3 A7,7 0 1 0 10,17 A7,7 0 1 0 10,3 M15,15 L21,21 M10,6 L10,14 M6,10 L14,10",
        ["zoom-out"] = "M10,3 A7,7 0 1 0 10,17 A7,7 0 1 0 10,3 M15,15 L21,21 M6,10 L14,10",
        ["fit"] = "M3,9 L3,3 9,3 M15,3 L21,3 21,9 M21,15 L21,21 15,21 M9,21 L3,21 3,15",
        ["grid"] = "M3,3 L21,3 21,21 3,21 Z M9,3 L9,21 M15,3 L15,21 M3,9 L21,9 M3,15 L21,15",
        ["check"] = "M4,12 L9,17 20,6", ["copy"] = "M8,8 L21,8 21,21 8,21 Z M16,8 L16,3 3,3 3,16 8,16",
        ["trash"] = "M4,6 L20,6 M9,6 L9,3 15,3 15,6 M6,6 L7,21 17,21 18,6 M10,10 L10,17 M14,10 L14,17",
        ["edit"] = "M4,20 L5,15 16,4 20,8 9,19 Z M14,6 L18,10",
        ["left"] = "M20,12 L4,12 M10,6 L4,12 10,18", ["right"] = "M4,12 L20,12 M14,6 L20,12 14,18",
        ["export"] = "M4,16 L4,21 20,21 20,16 M12,3 L12,16 M7,11 L12,16 17,11",
        ["reset"] = "M4,4 L4,10 10,10 M4,10 A8,8 0 1 1 6,18",
        ["scan"] = "M3,9 L3,3 9,3 M15,3 L21,3 21,9 M21,15 L21,21 15,21 M9,21 L3,21 3,15 M7,7 L17,7 17,17 7,17 Z"
    };

    public static void Set(Button button, string name, string label)
    {
        button.Content = new IconPath { Data = Geometry.Parse(Paths[name]), Stroke = Brushes.White,
            StrokeThickness = 1.7, Width = 18, Height = 18, Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        button.MinWidth = 34;
        ToolTip.SetTip(button, label);
        AutomationProperties.SetName(button, label);
    }

    public static Button Create(string name, string label)
    {
        var button = new Button();
        Set(button, name, label);
        return button;
    }
}
