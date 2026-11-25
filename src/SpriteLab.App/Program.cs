using Avalonia;

namespace SpriteLab.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // SL0: verify the framework can be resolved before introducing UI.
        Console.WriteLine($"SpriteLab 0.0.2 — Avalonia {typeof(Application).Assembly.GetName().Version}");
    }
}
