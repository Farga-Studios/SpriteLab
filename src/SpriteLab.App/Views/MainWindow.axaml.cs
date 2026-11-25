using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SpriteLab.Infrastructure;

namespace SpriteLab.App.Views;

public partial class MainWindow : Window
{
    private Bitmap? _bitmap;
    private int _zoom = 1;
    private bool _importing;
    private readonly CancellationTokenSource _lifetime = new();

    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = $"SpriteLab {GetVersion()} · SL1";
        Closed += (_, _) => { _lifetime.Cancel(); Preview.SetImage(null); _bitmap?.Dispose(); _bitmap = null; };
    }

    private static string GetVersion() =>
        typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        _importing = true;
        ImportButton.IsEnabled = ImportMenu.IsEnabled = false;
        var token = _lifetime.Token;
        Bitmap? candidate = null;
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Importar hoja PNG",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }]
            });
            if (files.Count == 0 || token.IsCancellationRequested) return;
            StatusText.Text = "Importando...";
            using (var stream = await files[0].OpenReadAsync())
            {
                var bytes = await PngSource.ReadAsync(stream, token);
                candidate = await Task.Run(() =>
                {
                    using var memory = new MemoryStream(bytes, writable: false);
                    return new Bitmap(memory);
                }, token);
            }
            if (token.IsCancellationRequested) return;
            var previous = _bitmap;
            Preview.SetImage(candidate);
            _bitmap = candidate;
            candidate = null;
            previous?.Dispose();
            _zoom = 1;
            ZoomText.Text = "100 %";
            EmptyViewer.IsVisible = false;
            ImageViewer.IsVisible = true;
            ResourceText.Text = files[0].Name;
            ResolutionText.Text = $"Resolución: {_bitmap.PixelSize.Width} × {_bitmap.PixelSize.Height}";
            ViewerInfo.Text = $"{_bitmap.PixelSize.Width} × {_bitmap.PixelSize.Height} px";
            StatusText.Text = "PNG cargado · Vista temporal sin proyecto";
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                StatusText.Text = $"No se pudo importar el PNG: {ex.Message}";
        }
        finally
        {
            candidate?.Dispose();
            _importing = false;
            if (!token.IsCancellationRequested)
                ImportButton.IsEnabled = ImportMenu.IsEnabled = true;
        }
    }

    private void ChangeZoom(int value)
    {
        if (_bitmap is null) return;
        _zoom = Math.Clamp(value, 1, 8);
        Preview.SetZoom(_zoom);
        ZoomText.Text = $"{_zoom * 100} %";
    }

    private void OnZoomIn(object? sender, RoutedEventArgs e) => ChangeZoom(_zoom + 1);
    private void OnZoomOut(object? sender, RoutedEventArgs e) => ChangeZoom(_zoom - 1);
    private void OnZoomReset(object? sender, RoutedEventArgs e) => ChangeZoom(1);

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Acerca de SpriteLab",
            Width = 420,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var close = new Button { Content = "Cerrar", HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = $"SpriteLab {GetVersion()}", FontSize = 22 },
                new TextBlock { Text = "Herramienta independiente para sprites 2D.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = "SL1 · Visualizador PNG inicial. Cuadrícula y proyectos pendientes.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
