using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SpriteLab.Core;
using SpriteLab.Core.Processing;
using System.Runtime.InteropServices;

namespace SpriteLab.App.Views;

internal sealed record ProcessingImport(byte[] Png, int ChangedPixels);

internal static class ProcessingWindow
{
    private static WriteableBitmap CreateBitmap(CellGrid grid, byte[] rgba)
    {
        var bitmap = new WriteableBitmap(new Avalonia.PixelSize(grid.Image.Width, grid.Image.Height),
            new Vector(96, 96), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        try
        {
            using var buffer = bitmap.Lock();
            var stride = checked(grid.Image.Width * 4);
            for (var y = 0; y < grid.Image.Height; y++)
                Marshal.Copy(rgba, y * stride, IntPtr.Add(buffer.Address, y * buffer.RowBytes), stride);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    public static async Task<ProcessingImport?> RunAsync(Window owner, CellGrid grid, byte[] source,
        int selected, string name, CancellationToken token)
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        using var original = CreateBitmap(grid, source);
        WriteableBitmap? processed = null;
        ProcessingResult? result = null;
        CancellationTokenSource? running = null;
        var dialog = new SuiteDialog { Title = UiText.T("Procesamiento de imágenes"), Width = 900, Height = 650,
            MinWidth = 700, MinHeight = 540, Background = UiPalette.Window, Foreground = UiPalette.Text,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var operation = new ComboBox { SelectedIndex = 0, ItemsSource = new[]
            { UiText.T("Limpiar RGB transparente"), UiText.T("Umbral alfa"), UiText.T("Voltear horizontal"), UiText.T("Voltear vertical") } };
        var scope = new ComboBox { SelectedIndex = 0, ItemsSource = new[]
            { UiText.T("Celda seleccionada"), UiText.T("Fila seleccionada"), UiText.T("Toda la hoja"), UiText.T("Rango personalizado") } };
        var range = new TextBox { Text = (selected + 1).ToString(), Watermark = "1,3-5,8" };
        var threshold = new TextBox { Text = "128", Watermark = "0–255" };
        var progress = new ProgressBar { Minimum = 0, Maximum = 100, Height = 8 };
        var status = new TextBlock { Text = UiText.T("Selecciona una operación y genera una vista previa."), TextWrapping = TextWrapping.Wrap };
        var run = new Button { Content = UiText.T("Generar vista previa") };
        var stop = new Button { Content = UiText.T("Cancelar proceso"), IsEnabled = false };
        var add = new Button { Content = UiText.T("Añadir como recurso nuevo"), IsEnabled = false };
        var close = new Button { Content = UiText.T("Cerrar") };
        var before = new SheetPreview { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var after = new SheetPreview { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        before.SetImage(original); before.SetGrid(grid); before.SetSelection(selected);
        int zoom = 1;
        void SetZoom(int z)
        {
            zoom = Math.Clamp(z, 1, 8);
            before.SetZoom(zoom); after.SetZoom(zoom);
        }
        var zoomOut = ToolbarIcons.Create("zoom-out", UiText.T("Reducir zoom"));
        var zoomIn = ToolbarIcons.Create("zoom-in", UiText.T("Ampliar zoom"));
        zoomOut.Click += (_, _) => SetZoom(zoom - 1);
        zoomIn.Click += (_, _) => SetZoom(zoom + 1);
        void InvalidateResult()
        {
            result = null; add.IsEnabled = false;
            after.SetImage(null); processed?.Dispose(); processed = null;
            progress.Value = 0;
            status.Text = UiText.T("Genera una nueva vista previa.");
        }
        void SetWorking(bool busy)
        {
            operation.IsEnabled = scope.IsEnabled = run.IsEnabled = !busy;
            range.IsEnabled = !busy && scope.SelectedIndex == 3;
            threshold.IsEnabled = !busy && operation.SelectedIndex == 1;
            stop.IsEnabled = busy;
            add.IsEnabled = !busy && result is { ChangedPixels: > 0 };
        }
        operation.SelectionChanged += (_, _) => { InvalidateResult(); SetWorking(false); };
        scope.SelectionChanged += (_, _) => { InvalidateResult(); SetWorking(false); };
        SetWorking(false);
        range.TextChanged += (_, _) => InvalidateResult();
        threshold.TextChanged += (_, _) => InvalidateResult();
        run.Click += async (_, _) =>
        {
            if (running is not null) return;
            InvalidateResult();
            int[] cells;
            int alpha = 128;
            try
            {
                if (operation.SelectedIndex == 1 &&
                    (!int.TryParse(threshold.Text, out alpha) || alpha is < 0 or > 255))
                    throw new ArgumentException("Umbral alfa entre 0 y 255.");
                cells = scope.SelectedIndex switch
                {
                    0 => [selected],
                    1 => Enumerable.Range(selected / grid.Columns * grid.Columns, grid.Columns).ToArray(),
                    2 when grid.Count <= 65536 => Enumerable.Range(0, grid.Count).ToArray(),
                    2 => throw new ArgumentException("Máximo 65536 celdas por operación."),
                    3 => FrameAlignment.ParseSelection(range.Text ?? "", grid.Count),
                    _ => throw new ArgumentException("Selecciona un alcance.")
                };
                if (operation.SelectedIndex < 0) throw new ArgumentException("Selecciona una operación.");
            }
            catch (ArgumentException ex) { status.Text = ex.Message; return; }
            var request = new ProcessingRequest((SpriteOperation)operation.SelectedIndex, cells, alpha);
            using var job = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            running = job;
            SetWorking(true); progress.Value = 0; status.Text = UiText.T("Procesando…");
            try
            {
                var reporter = new Progress<int>(value =>
                {
                    if (!lifetime.IsCancellationRequested && ReferenceEquals(running, job))
                        progress.Value = value;
                });
                var completed = await Task.Run(() => SpriteProcessor.Run(grid, source, request, reporter, job.Token), job.Token);
                job.Token.ThrowIfCancellationRequested();
                processed = CreateBitmap(grid, completed.Rgba);
                after.SetImage(processed); after.SetGrid(grid); after.SetZoom(zoom);
                result = completed; progress.Value = 100;
                status.Text = $"{completed.ProcessedCells} celdas · {completed.ChangedPixels} píxeles modificados." +
                    (completed.ChangedPixels == 0 ? " Sin cambios que incorporar." : " Puedes incorporar una copia nueva.");
            }
            catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) status.Text = UiText.T("Proceso cancelado. Original intacto."); }
            catch (Exception ex) { if (!lifetime.IsCancellationRequested) status.Text = $"Error: {ex.Message}"; }
            finally
            {
                running = null;
                if (!lifetime.IsCancellationRequested) SetWorking(false);
            }
        };
        stop.Click += (_, _) => running?.Cancel();
        add.Click += async (_, _) =>
        {
            if (running is not null || processed is null || result is not { ChangedPixels: > 0 }) return;
            var snapshot = result;
            using var job = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            running = job; SetWorking(true);
            status.Text = UiText.T("Codificando PNG sin pérdida…"); progress.IsIndeterminate = true;
            try
            {
                var png = await Task.Run(() => RgbaPng.Encode(grid.Image.Width, grid.Image.Height, snapshot.Rgba, job.Token), job.Token);
                job.Token.ThrowIfCancellationRequested();
                dialog.Close(new ProcessingImport(png, snapshot.ChangedPixels));
            }
            catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) status.Text = UiText.T("Codificación cancelada. Original intacto."); }
            catch (Exception ex) { if (!lifetime.IsCancellationRequested) status.Text = $"No se pudo codificar PNG: {ex.Message}"; }
            finally { running = null; if (!lifetime.IsCancellationRequested) { progress.IsIndeterminate = false; SetWorking(false); } }
        };
        close.Click += (_, _) => dialog.Close(null);
        dialog.Closed += (_, _) => lifetime.Cancel();
        Border Panel(string title, SheetPreview preview)
        {
            var scroll = new ScrollViewer { Content = preview,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            var g = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 8 };
            g.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.SemiBold });
            Grid.SetRow(scroll, 1); g.Children.Add(scroll);
            return new Border { Background = UiPalette.Viewer, BorderBrush = UiPalette.Border,
                BorderThickness = new Thickness(1), Padding = new Thickness(8), Child = g };
        }
        var comparison = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 12 };
        var left = Panel(UiText.T("Original"), before); var right = Panel(UiText.T("Resultado"), after);
        Grid.SetColumn(right, 1); comparison.Children.Add(left); comparison.Children.Add(right);
        var header = new StackPanel { Spacing = 8, Children =
        {
            new TextBlock { Text = name + " · " + grid.Cell.Width + "×" + grid.Cell.Height + " px por celda", TextWrapping = TextWrapping.Wrap },
            new WrapPanel { Orientation = Orientation.Horizontal, Children = { operation, scope, range, threshold, zoomOut, zoomIn } },
            new TextBlock { Text = "Rango: 1,3-5,8 · Umbral: 0–255 (solo alfa). Se procesa la imagen fuente sin offsets ni anclajes.", TextWrapping = TextWrapping.Wrap, Foreground = UiPalette.Muted }
        }};
        var footer = new StackPanel { Spacing = 8, Children = { progress, status,
            new WrapPanel { Orientation = Orientation.Horizontal, Children = { run, stop, add, close } } } };
        var root = new Grid { Margin = new Thickness(16), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 12 };
        root.Children.Add(header); Grid.SetRow(comparison, 1); root.Children.Add(comparison);
        Grid.SetRow(footer, 2); root.Children.Add(footer); dialog.Content = root;
        try { return await dialog.ShowDialog<ProcessingImport?>(owner); }
        finally
        {
            lifetime.Cancel();
            before.SetImage(null); after.SetImage(null); processed?.Dispose();
        }
    }
}
