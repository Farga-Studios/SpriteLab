using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Automation;
using IconPath = Avalonia.Controls.Shapes.Path;

namespace SpriteLab.App.Views;

// Original, consistent 24-unit outline icon set. Keep geometry in a fixed viewbox
// so narrow glyphs (pause, step) retain the same visual proportions.
internal static class ToolbarIcons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["play"] = "M8,5 L19,12 L8,19 Z",
        ["pause"] = "M9,5 L9,19 M15,5 L15,19",
        ["stop"] = "M6,5 H18 Q19,5 19,6 V18 Q19,19 18,19 H6 Q5,19 5,18 V6 Q5,5 6,5 Z",
        ["previous"] = "M18,5 L9,12 L18,19 Z M5,5 V19",
        ["next"] = "M6,5 L15,12 L6,19 Z M19,5 V19",
        ["plus"] = "M12,5 V19 M5,12 H19",
        ["minus"] = "M5,12 H19",
        ["zoom-in"] = "M10.5,3 A7.5,7.5 0 1 0 10.5,18 A7.5,7.5 0 1 0 10.5,3 M16,16 L21,21 M10.5,7 V14 M7,10.5 H14",
        ["zoom-out"] = "M10.5,3 A7.5,7.5 0 1 0 10.5,18 A7.5,7.5 0 1 0 10.5,3 M16,16 L21,21 M7,10.5 H14",
        ["fit"] = "M8,3 H5 Q3,3 3,5 V8 M16,3 H19 Q21,3 21,5 V8 M21,16 V19 Q21,21 19,21 H16 M8,21 H5 Q3,21 3,19 V16",
        ["grid"] = "M5,3 H19 Q21,3 21,5 V19 Q21,21 19,21 H5 Q3,21 3,19 V5 Q3,3 5,3 Z M9,3 V21 M15,3 V21 M3,9 H21 M3,15 H21",
        ["check"] = "M5,12 L10,17 L19,7",
        ["copy"] = "M10,8 H19 Q21,8 21,10 V19 Q21,21 19,21 H10 Q8,21 8,19 V10 Q8,8 10,8 Z M16,5 Q16,3 14,3 H5 Q3,3 3,5 V14 Q3,16 5,16",
        ["trash"] = "M3,6 H21 M9,6 V4 Q9,3 10,3 H14 Q15,3 15,4 V6 M6,6 L7,19 Q7,21 9,21 H15 Q17,21 17,19 L18,6 M10,10 V17 M14,10 V17",
        ["edit"] = "M4,20 L5,15 L16,4 Q17,3 18,4 L20,6 Q21,7 20,8 L9,19 Z M14,6 L18,10",
        ["left"] = "M20,12 H4 M10,6 L4,12 L10,18",
        ["right"] = "M4,12 H20 M14,6 L20,12 L14,18",
        ["export"] = "M4,16 V19 Q4,21 6,21 H18 Q20,21 20,19 V16 M12,3 V15 M7,10 L12,15 L17,10",
        ["reset"] = "M4,4 V10 H10 M4,10 A8,8 0 1 1 5,17",
        ["scan"] = "M8,3 H5 Q3,3 3,5 V8 M16,3 H19 Q21,3 21,5 V8 M21,16 V19 Q21,21 19,21 H16 M8,21 H5 Q3,21 3,19 V16 M8,8 H16 V16 H8 Z"
    };

    public static void Set(Button button, string name, string label)
    {
        var canvas = new Canvas { Width = 24, Height = 24 };
        canvas.Children.Add(new IconPath { Data = Geometry.Parse(Paths[name]),
            Stroke = UiPalette.Icon, StrokeThickness = 1.8,
            StrokeLineCap = PenLineCap.Round, StrokeJoin = PenLineJoin.Round });
        button.Content = new Viewbox { Width = 20, Height = 20, Child = canvas,
            Stretch = Stretch.Uniform };
        button.MinWidth = 36;
        button.MinHeight = 34;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
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
