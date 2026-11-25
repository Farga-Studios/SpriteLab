using Avalonia;

namespace SpriteLab.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--smoke-test", StringComparer.Ordinal))
        {
            Console.WriteLine($"SpriteLab {typeof(Program).Assembly.GetName().Version} — Avalonia {typeof(Application).Assembly.GetName().Version}");
            return 0;
        }

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
