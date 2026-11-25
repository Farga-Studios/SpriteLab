using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SpriteLab.Infrastructure;
using SpriteLab.Core;

namespace SpriteLab.App.Views;

public partial class MainWindow : Window
{
    private Bitmap? _bitmap;
    private int _zoom = 1;
    private CellGrid? _grid;
    private int? _selected;
    private bool _showGrid = true;
    private bool _importing;
    private readonly CancellationTokenSource _lifetime = new();

    public MainWindow()
    {
        InitializeComponent();
        Preview.CellSelected += SelectCell;
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
            ResetGrid();
            ApplyGridButton.IsEnabled = GridMenu.IsEnabled = true;
            StatusText.Text = "PNG cargado · Configura la cuadrícula";
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

    private void ResetGrid()
    {
        _grid = null;
        _selected = null;
        Preview.SetGrid(null);
        GridInfo.Text = "Sin cuadrícula";
        FrameInfo.Text = "Sin selección";
        RectInfo.Text = "Rectángulo: —";
        DurationInfo.Text = "Duración: —";
        AnchorInfo.Text = "Anclaje: —";
        TimelineInfo.Text = "Sin fotogramas";
        PreviousButton.IsEnabled = NextButton.IsEnabled = false;
    }

    private void OnApplyGrid(object? sender, RoutedEventArgs e)
    {
        if (_bitmap is null) return;
        if (!int.TryParse(CellWidth.Text, out var width) ||
            !int.TryParse(CellHeight.Text, out var height) || width <= 0 || height <= 0)
        {
            StatusText.Text = "Introduce ancho y alto positivos.";
            return;
        }
        try
        {
            var candidate = new CellGrid(
                new PixelSize(_bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new PixelSize(width, height));
            _grid = candidate;
            Preview.SetGrid(candidate);
            GridInfo.Text = $"{candidate.Columns} columnas × {candidate.Rows} filas · {candidate.Count} celdas";
            SelectCell(0);
            StatusText.Text = "Cuadrícula aplicada · Sin modificar el PNG";
        }
        catch (ArgumentException)
        {
            StatusText.Text = "La imagen debe dividirse exactamente en celdas completas.";
        }
    }

    private void SelectCell(int index)
    {
        if (_grid is null || index < 0 || index >= _grid.Count) return;
        _selected = index;
        Preview.SetSelection(index);
        var cell = _grid.GetCell(index);
        FrameInfo.Text = $"Celda {index + 1} · fila {index / _grid.Columns + 1}, columna {index % _grid.Columns + 1}";
        RectInfo.Text = $"Rectángulo: ({cell.X}, {cell.Y}) · {cell.Width} × {cell.Height}";
        DurationInfo.Text = "Duración: sin asignar";
        AnchorInfo.Text = "Anclaje: sin asignar";
        TimelineInfo.Text = $"Celda {index + 1} / {_grid.Count}";
        PreviousButton.IsEnabled = index > 0;
        NextButton.IsEnabled = index < _grid.Count - 1;
    }

    private void OnPrevious(object? sender, RoutedEventArgs e)
    {
        if (_selected is int index) SelectCell(index - 1);
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (_selected is int index) SelectCell(index + 1);
    }

    private void OnToggleGrid(object? sender, RoutedEventArgs e)
    {
        _showGrid = !_showGrid;
        Preview.SetGridVisible(_showGrid);
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
                new TextBlock { Text = "SL1 · Visualizador PNG inicial. Cuadrícula y selección iniciales; proyectos pendientes.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
