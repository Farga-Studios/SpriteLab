using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SpriteLab.Infrastructure;
using SpriteLab.Core;

namespace SpriteLab.App.Views;

public partial class MainWindow : Window
{
    private Bitmap? _bitmap;
    private SheetValidationResult? _validationResult;
    private ValidationReport? _validationReport;
    private SheetFinding[] _allFindings = [];
    private string _diagnosticSummary = "Validación no ejecutada.";
    private readonly List<ViewerSheet> _sheets = new();
    private readonly List<ViewerClip> _clips = new();
    private bool _refreshingClips;
    private bool _refreshingTracks;
    private PlaybackCursor? _playback;
    private ViewerTrack? _playbackTrack;
    private readonly DispatcherTimer _playTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private PlaybackClock? _previewClock;
    private bool _syncingScrubber;
    private bool _playIconActive;
    private Guid _projectId = Guid.NewGuid();
    private string _projectName = "Proyecto";
    private string? _folder;
    private string? _manifestHash;
    private bool _dirty;
    private bool _refreshing;
    private int _active = -1;
    private bool _allowClose;
    private int _zoom = 1;
    private CellGrid? _grid;
    private int? _selected;
    private bool _showGrid = true;
    private bool _importing;
    private readonly CancellationTokenSource _lifetime = new();

    public MainWindow()
    {
        InitializeComponent();
        InitializeToolbarIcons();
        FramePreview.OffsetCommitted += ApplyFrameOffset;
        FramePreview.OffsetPreviewChanged += (x, y) => { OffsetX.Text = x.ToString(); OffsetY.Text = y.ToString(); };
        TimeScrubber.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) OnScrub(); };
        DiagnosticFilterBox.SelectionChanged += (_, _) => RefreshDiagnosticFilter();
        DiagnosticOverlay.IsCheckedChanged += (_, _) => RefreshDiagnosticFilter();
        Preview.CellSelected += OnManualCell;
        CellStrip.CellSelected += OnManualCell;
        CellStrip.OccurrenceSelected += OnOccurrenceSelected;
        CellStrip.OccurrenceMoved += OnOccurrenceMoved;
        _playTimer.Tick += (_, _) => PlaybackTick();
        UpdatePlaybackControls();
        OnionPrevious.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        OnionNext.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        OnionTint.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        OnionOpacity.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) UpdateOnion(); };
        LoopCheck.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        Preview.PointerWheelChanged += OnPreviewWheel;
        AddHandler(KeyDownEvent, OnAnimationKey, Avalonia.Interactivity.RoutingStrategies.Bubble);
        FramePreviewZoom.Text = "Ajustar";
        VersionText.Text = $"SpriteLab {GetVersion()} · SL4 editor";
        Closing += OnClosing;
        Closed += (_, _) => { _playTimer.Stop(); _lifetime.Cancel(); Preview.SetImage(null); FramePreview.SetFrame(null, default); CellStrip.SetSheet(null, null); _bitmap?.Dispose(); _bitmap = null; };
    }

    private void InitializeToolbarIcons()
    {
        ToolbarIcons.Set(PreviousButton, "previous", "Fotograma anterior (←)");
        ToolbarIcons.Set(PlayButton, "play", "Reproducir (Espacio)");
        ToolbarIcons.Set(StopButton, "stop", "Detener");
        ToolbarIcons.Set(NextButton, "next", "Fotograma siguiente (→)");
        ToolbarIcons.Set(AddFrameButton, "plus", "Añadir fotograma");
        ToolbarIcons.Set(DuplicateFrameButton, "copy", "Duplicar fotograma");
        ToolbarIcons.Set(RemoveFrameButton, "trash", "Quitar fotograma");
        ToolbarIcons.Set(MoveFrameLeftButton, "left", "Mover fotograma a la izquierda");
        ToolbarIcons.Set(MoveFrameRightButton, "right", "Mover fotograma a la derecha");
        ToolbarIcons.Set(EditSequenceButton, "edit", "Editar secuencia");
        ToolbarIcons.Set(AssignTrackButton, "plus", "Asignar fila a dirección");
        ToolbarIcons.Set(RemoveTrackButton, "trash", "Quitar dirección");
        ToolbarIcons.Set(ApplyTimingButton, "check", "Aplicar tiempo");
        ToolbarIcons.Set(ValidateButton, "check", "Validar hoja");
        ToolbarIcons.Set(ExportReportButton, "export", "Exportar informe");
        ToolbarIcons.Set(ZoomOutButton, "zoom-out", "Reducir zoom");
        ToolbarIcons.Set(ZoomInButton, "zoom-in", "Ampliar zoom");
        ToolbarIcons.Set(ZoomResetButton, "reset", "Zoom 100%");
        ToolbarIcons.Set(FitButton, "fit", "Ajustar vista");
        ToolbarIcons.Set(ToggleGridButton, "grid", "Mostrar u ocultar cuadrícula");
        ToolbarIcons.Set(FrameZoomOutButton, "zoom-out", "Reducir vista de fotograma");
        ToolbarIcons.Set(FrameZoomInButton, "zoom-in", "Ampliar vista de fotograma");
        ToolbarIcons.Set(FrameZoomResetButton, "fit", "Centrar fotograma");
    }

    private void OnScrub()
    {
        if (_syncingScrubber || _importing || _previewClock is not { } timing) return;
        var target = TimeScrubber.Value;
        PausePlayback();
        timing.Seek(target);
        ShowPlaybackCell();
    }

    private void UpdateScrubber()
    {
        _syncingScrubber = true;
        try
        {
            var timing = _previewClock;
            TimeScrubber.IsEnabled = !_importing && timing is not null;
            TimeScrubber.Maximum = timing?.Duration ?? 1;
            TimeScrubber.Value = timing?.PositionMs ?? 0;
            ScrubberTime.Text = timing is null ? "— / —" :
                $"{timing.PositionMs / 1000:0.000} s / {timing.Duration / 1000:0.000} s";
        }
        finally { _syncingScrubber = false; }
    }

    private static string GetVersion() =>
        typeof(MainWindow).Assembly.GetName().Version is { } version
            ? version.ToString(version.Revision > 0 ? 4 : 3) : "0.0.0";

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        _importing = true;
        SetBusy(true);
        var token = _lifetime.Token;
        Bitmap? candidate = null;
        byte[]? imported = null;
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
                if (_sheets.Count >= 64 || _sheets.Sum(s => (long)s.Png.Length) + bytes.Length > 256L * 1024 * 1024)
                    throw new InvalidDataException("Límite de recursos del proyecto alcanzado.");
                imported = bytes;
                candidate = await Task.Run(() =>
                {
                    using var memory = new MemoryStream(bytes, writable: false);
                    return new Bitmap(memory);
                }, token);
            }
            if (token.IsCancellationRequested) return;
            _sheets.Add(new ViewerSheet(Guid.NewGuid(), files[0].Name, imported!, null, null));
            ShowSheet(_sheets.Count - 1, candidate!);
            candidate = null;
            RefreshResources();
            RefreshClips();
            MarkDirty();
            InspectorTabs.SelectedIndex = 0;
            StatusText.Text = "PNG cargado · Configura la cuadrícula en Hoja";
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
                SetBusy(false);
        }
    }

    private void ResetGrid()
    {
        ClearDiagnostics();
        ClearPlayback();
        _grid = null;
        _selected = null;
        RefreshFrameEdit();
        Preview.SetGrid(null);
        GridInfo.Text = "Sin cuadrícula";
        FrameInfo.Text = "Sin selección";
        RectInfo.Text = "Rectángulo: —";
        DurationInfo.Text = "Duración: —";
        AnchorInfo.Text = "Anclaje: —";
        TimelineInfo.Text = "Sin fotogramas";
        FramePreview.SetFrame(null, default);
        CellStrip.SetSheet(null, null);
        UpdatePlaybackControls();
    }

    private async void OnSuggestGrid(object? sender, RoutedEventArgs e)
    {
        if (_importing || _bitmap is null) return;
        SetBusy(true);
        GridSuggestion? chosen = null;
        try
        {
            var bitmap = _bitmap;
            var image = new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            var pixels = ValidationPixels.Read(bitmap);
            var suggestions = await Task.Run(() => GridSuggestions.Analyze(image, pixels, _lifetime.Token), _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            if (suggestions.Count == 0) { StatusText.Text = "No hay suficientes píxeles visibles para sugerir una cuadrícula."; return; }
            var list = new ListBox { ItemsSource = suggestions, SelectedIndex = 0 };
            var preview = new SheetPreview { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            preview.SetImage(bitmap);
            var scroll = new ScrollViewer { Content = preview, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            var dialog = new Window { Title = "Asistente de cuadrícula", Width = 940, Height = 620,
                MinWidth = 700, MinHeight = 440, Background = UiPalette.Window, Foreground = UiPalette.Text,
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var detail = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = UiPalette.Accent };
            var zoomLabel = new TextBlock { Text = "100 %", VerticalAlignment = VerticalAlignment.Center };
            var zoom = 1;
            void SetZoom(int value)
            {
                zoom = Math.Clamp(value, 1, 8);
                preview.SetZoom(zoom);
                zoomLabel.Text = $"{zoom * 100} %";
            }
            void UpdatePreview()
            {
                if (list.SelectedItem is GridSuggestion s)
                {
                    preview.SetGrid(new CellGrid(image, new PixelSize(s.Width, s.Height)));
                    detail.Text = $"Celda: {s.Width} × {s.Height} px · {s.Columns * s.Rows} celdas";
                }
            }
            list.SelectionChanged += (_, _) => UpdatePreview();
            var minus = ToolbarIcons.Create("zoom-out", "Reducir zoom");
            var plus = ToolbarIcons.Create("zoom-in", "Ampliar zoom");
            var reset = ToolbarIcons.Create("reset", "Zoom 100 %");
            minus.Click += (_, _) => SetZoom(zoom - 1);
            plus.Click += (_, _) => SetZoom(zoom + 1);
            reset.Click += (_, _) => SetZoom(1);
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                Children = { minus, zoomLabel, plus, reset } };
            var viewer = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 8 };
            Grid.SetRow(scroll, 1);
            viewer.Children.Add(toolbar); viewer.Children.Add(scroll);
            var choices = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 8 };
            var label = new TextBlock { Text = "CUADRÍCULAS SUGERIDAS", FontWeight = FontWeight.SemiBold, Foreground = UiPalette.Muted };
            Grid.SetRow(list, 1); Grid.SetRow(detail, 2);
            choices.Children.Add(label); choices.Children.Add(list); choices.Children.Add(detail);
            var body = new Grid { ColumnDefinitions = new ColumnDefinitions("*,280"), ColumnSpacing = 16 };
            var viewerPanel = new Border { Background = UiPalette.Viewer, BorderBrush = UiPalette.Border,
                BorderThickness = new Avalonia.Thickness(1), CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(12), Child = viewer };
            var choicesPanel = new Border { Background = UiPalette.Panel, BorderBrush = UiPalette.Border,
                BorderThickness = new Avalonia.Thickness(1), CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(12), Child = choices };
            list.Background = UiPalette.Panel;
            Grid.SetColumn(choicesPanel, 1); body.Children.Add(viewerPanel); body.Children.Add(choicesPanel);
            var apply = new Button { Content = "Aplicar cuadrícula" };
            var cancel = new Button { Content = "Cancelar" };
            apply.Click += (_, _) => dialog.Close(list.SelectedItem as GridSuggestion);
            cancel.Click += (_, _) => dialog.Close(null);
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right, Children = { cancel, apply } };
            var hint = new TextBlock { Text = $"Imagen: {image.Width} × {image.Height} px · Selecciona una propuesta y comprueba la vista previa.",
                TextWrapping = TextWrapping.Wrap, Foreground = UiPalette.Muted };
            var root = new Grid { Margin = new Avalonia.Thickness(16), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 12 };
            Grid.SetRow(body, 1); Grid.SetRow(footer, 2);
            root.Children.Add(hint); root.Children.Add(body); root.Children.Add(footer);
            dialog.Content = root;
            UpdatePreview();
            chosen = await dialog.ShowDialog<GridSuggestion?>(this);
            preview.SetImage(null);
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = $"No se pudo sugerir cuadrícula: {ex.Message}"; }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
        if (chosen is null || _lifetime.IsCancellationRequested) return;
        CellWidth.Text = chosen.Width.ToString();
        CellHeight.Text = chosen.Height.ToString();
        OnApplyGrid(null, new RoutedEventArgs());
    }

    private void OnApplyGrid(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        PausePlayback();
        if (_bitmap is null || _importing) return;
        if (!int.TryParse(CellWidth.Text, out var width) ||
            !int.TryParse(CellHeight.Text, out var height) || width <= 0 || height <= 0)
        {
            RestoreGridFields();
            StatusText.Text = "Introduce ancho y alto positivos.";
            return;
        }
        try
        {
            if (_active >= 0 && (_clips.Any(c => c.SheetId == _sheets[_active].Id) || _sheets[_active].Offsets is { Length: > 0 }) &&
                (_sheets[_active].CellWidth != width || _sheets[_active].CellHeight != height))
            {
                RestoreGridFields();
                StatusText.Text = "Quita los clips y restablece los desplazamientos antes de cambiar la cuadrícula.";
                return;
            }
            if (_grid is not null && _grid.Cell.Width == width && _grid.Cell.Height == height)
            {
                StatusText.Text = "La cuadrícula ya tiene esas dimensiones.";
                return;
            }
            var candidate = new CellGrid(
                new PixelSize(_bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new PixelSize(width, height));
            ClearDiagnostics();
            _grid = candidate;
            if (_active >= 0)
            {
                _sheets[_active] = _sheets[_active] with { CellWidth = width, CellHeight = height };
                MarkDirty();
            }
            Preview.SetGrid(candidate);
            CellStrip.SetSheet(_bitmap, candidate);
            RefreshClips();
            GridInfo.Text = $"{candidate.Columns} columnas × {candidate.Rows} filas · {candidate.Count} celdas";
            SelectCell(0);
            ValidateMenu.IsEnabled = ValidateButton.IsEnabled = true;
            StatusText.Text = "Cuadrícula aplicada · Sin modificar el PNG";
        }
        catch (ArgumentException)
        {
            RestoreGridFields();
            StatusText.Text = "La imagen debe dividirse exactamente en celdas completas.";
        }
    }

    private void SelectCell(int index)
    {
        if (_grid is null || index < 0 || index >= _grid.Count) return;
        _selected = index;
        Preview.SetSelection(index);
        var cell = _grid.GetCell(index);
        FramePreview.SetFrame(_bitmap, cell);
        CellStrip.SetSelection(index);
        FrameInfo.Text = $"Celda {index + 1} · fila {index / _grid.Columns + 1}, columna {index % _grid.Columns + 1}";
        RectInfo.Text = $"Rectángulo: ({cell.X}, {cell.Y}) · {cell.Width} × {cell.Height}";
        UpdateVisibleBounds();
        DurationInfo.Text = "Duración: sin asignar";
        AnchorInfo.Text = "Anclaje: sin asignar";
        RefreshFrameEdit();
        TimelineInfo.Text = $"Celda {index + 1} / {_grid.Count}";
        UpdatePlaybackControls();
    }

    private FrameOffset GetOffset(int cell) =>
        _active >= 0 ? _sheets[_active].Offsets?.FirstOrDefault(o => o.Cell == cell) ?? FrameOffset.Zero(cell)
            : FrameOffset.Zero(cell);

    private void RefreshFrameEdit()
    {
        var offset = GetOffset(_selected ?? 0);
        OffsetX.Text = offset.X.ToString();
        OffsetY.Text = offset.Y.ToString();
        FramePreview.SetOffset(offset.X, offset.Y);
    }

    private void ApplyFrameOffset(int x, int y)
    {
        if (_importing || _playback?.IsPlaying == true || _grid is null || _active < 0 || _selected is not int cell) return;
        try
        {
            var sheet = _sheets[_active];
            var offset = new FrameOffset(cell, x, y);
            if (offset == GetOffset(cell)) { RefreshFrameEdit(); return; }
            _sheets[_active] = sheet with { Offsets = FrameOffset.Set(sheet.Offsets ?? [], _grid, offset) };
            MarkDirty();
            RefreshFrameEdit();
            UpdateOnion();
            StatusText.Text = $"Desplazamiento de celda {cell + 1}: {x}, {y} px · PNG intacto";
        }
        catch (ArgumentException ex) { RefreshFrameEdit(); StatusText.Text = ex.Message; }
    }

    private void OnApplyOffset(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(OffsetX.Text, out var x) && int.TryParse(OffsetY.Text, out var y)) ApplyFrameOffset(x, y);
        else { RefreshFrameEdit(); StatusText.Text = "Introduce coordenadas enteras en píxeles."; }
    }

    private void OnResetOffset(object? sender, RoutedEventArgs e) => ApplyFrameOffset(0, 0);

    private void OnMoveModeChanged(object? sender, RoutedEventArgs e) => UpdatePlaybackControls();

    private void OnApplyGuides(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CustomGuideX.Text, out var x) || !int.TryParse(CustomGuideY.Text, out var y) ||
            Math.Abs((long)x) > 8192 || Math.Abs((long)y) > 8192)
        { StatusText.Text = "Introduce coordenadas de guía entre -8192 y 8192."; return; }
        FramePreview.SetGuides(new EditorGuides(CenterGuide.IsChecked == true, GroundGuide.IsChecked == true,
            BoundsGuide.IsChecked == true, CustomGuide.IsChecked == true, x, y));
    }

    private void OnPrevious(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        if (_playback is not null) { PausePlayback(); _playback.Step(-1); _previewClock?.Reset(); ShowPlaybackCell(); }
        else if (_selected is int index) SelectCell(index - 1);
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        if (_playback is not null) { PausePlayback(); _playback.Step(1); _previewClock?.Reset(); ShowPlaybackCell(); }
        else if (_selected is int index) SelectCell(index + 1);
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

    private void OnPreviewWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || _bitmap is null) return;
        ChangeZoom(_zoom + Math.Sign(e.Delta.Y));
        e.Handled = true;
    }

    private void OnFit(object? sender, RoutedEventArgs e)
    {
        if (_bitmap is null) return;
        var ratio = Math.Min(SheetScroll.Viewport.Width / _bitmap.PixelSize.Width,
            SheetScroll.Viewport.Height / _bitmap.PixelSize.Height);
        if (ratio > 0) ChangeZoom((int)Math.Clamp(Math.Floor(ratio), 1, 8));
    }

    private void OnZoomIn(object? sender, RoutedEventArgs e) => ChangeZoom(_zoom + 1);
    private void OnZoomOut(object? sender, RoutedEventArgs e) => ChangeZoom(_zoom - 1);
    private void OnZoomReset(object? sender, RoutedEventArgs e) => ChangeZoom(1);

    private void SetBusy(bool busy)
    {
        if (busy) PausePlayback();
        _importing = busy;
        ImportButton.IsEnabled = ImportMenu.IsEnabled = OpenProjectMenu.IsEnabled =
            SaveProjectMenu.IsEnabled = SaveAsMenu.IsEnabled = RenameProjectMenu.IsEnabled = ResourceList.IsEnabled = ClipList.IsEnabled = !busy;
        ApplyGridButton.IsEnabled = RestoreGridButton.IsEnabled = SuggestGridButton.IsEnabled = !busy && _bitmap is not null;
        DeselectClipButton.IsEnabled = !busy && ClipList.SelectedIndex >= 0;
        TrackList.IsEnabled = !busy;
        ValidateMenu.IsEnabled = ValidateButton.IsEnabled = !busy && _grid is not null;
        FindingList.IsEnabled = DiagnosticFilterBox.IsEnabled = DiagnosticOverlay.IsEnabled = !busy;
        ExportReportButton.IsEnabled = !busy && _validationReport is not null;
        UpdatePlaybackControls();
        RenameResourceButton.IsEnabled = RemoveResourceButton.IsEnabled = !busy && _active >= 0;
    }

    private void MarkDirty()
    {
        _dirty = true;
        if (_validationResult is not null) ClearDiagnostics();
        ProjectText.Text = _projectName + " *";
        SaveIndicator.Text = "● Cambios sin guardar";
        SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#E5B760"));
    }

    private void RefreshResources()
    {
        _refreshing = true;
        try
        {
            ResourceList.ItemsSource = _sheets.Select(s => s.Name).ToArray();
            ResourceList.SelectedIndex = _active;
            ResourceText.Text = $"{_sheets.Count} recursos";
            RenameResourceButton.IsEnabled = RemoveResourceButton.IsEnabled = !_importing && _active >= 0;
        }
        finally { _refreshing = false; }
    }

    private void ShowSheet(int index, Bitmap bitmap)
    {
        var previous = _bitmap;
        Preview.SetImage(bitmap);
        _bitmap = bitmap;
        FramePreview.SetFrame(null, default);
        CellStrip.SetSheet(null, null);
        previous?.Dispose();
        _active = index;
        _zoom = 1;
        ZoomText.Text = "100 %";
        ResetGrid();
        EmptyViewer.IsVisible = false;
        ImageViewer.IsVisible = true;
        var sheet = _sheets[index];
        ResourceText.Text = $"{_sheets.Count} recursos";
        ResolutionText.Text = $"Resolución: {bitmap.PixelSize.Width} × {bitmap.PixelSize.Height}";
        ViewerInfo.Text = $"{bitmap.PixelSize.Width} × {bitmap.PixelSize.Height} px";
        ApplyGridButton.IsEnabled = GridMenu.IsEnabled = true;
        if (sheet.CellWidth is int w && sheet.CellHeight is int h)
        {
            CellWidth.Text = w.ToString();
            CellHeight.Text = h.ToString();
            _grid = new CellGrid(new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height), new PixelSize(w, h));
            Preview.SetGrid(_grid);
            CellStrip.SetSheet(_bitmap, _grid);
            GridInfo.Text = $"{_grid.Columns} columnas × {_grid.Rows} filas · {_grid.Count} celdas";
            SelectCell(0);
        }
        else { CellWidth.Text = CellHeight.Text = "48"; InspectorTabs.SelectedIndex = 0; }
    }

    private async void OnRenameResource(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0) return;
        var selectedClip = ClipList.SelectedItem as ViewerClip;
        var selectedTrack = TrackList.SelectedIndex;
        var selectedOccurrence = _playback?.Position ?? 0;
        SetBusy(true);
        try
        {
            var input = new TextBox { Text = _sheets[_active].Name, MaxLength = 200 };
            var dialog = new Window { Title = "Renombrar recurso", Width = 420, Height = 170,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Content = "Aceptar" };
            var cancel = new Button { Content = "Cancelar" };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = "Nombre visible del recurso" }, input,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, ok } } } };
            var name = await dialog.ShowDialog<string?>(this);
            if (string.IsNullOrWhiteSpace(name) || name == _sheets[_active].Name) return;
            _sheets[_active] = _sheets[_active] with { Name = name };
            RefreshResources();
            MarkDirty();
            if (selectedClip is not null && selectedClip.SheetId == _sheets[_active].Id && selectedTrack >= 0 && selectedTrack < selectedClip.EffectiveTracks.Count)
            {
                _refreshingClips = true;
                try { ClipList.SelectedIndex = _clips.FindIndex(c => c.Id == selectedClip.Id); }
                finally { _refreshingClips = false; }
                RefreshTracks(selectedClip, selectedTrack);
                SetPlayback(selectedClip.EffectiveTracks[selectedTrack]);
                _playback?.Step(selectedOccurrence);
                ShowPlaybackCell();
            }
            StatusText.Text = "Recurso renombrado · Archivo fuente intacto";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async void OnRemoveResource(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0) return;
        if (_clips.Any(c => c.SheetId == _sheets[_active].Id))
        {
            StatusText.Text = "Quita primero los clips asociados al recurso.";
            return;
        }
        SetBusy(true);
        Bitmap? candidate = null;
        try
        {
            var dialog = new Window { Title = "Quitar recurso", Width = 440, Height = 180,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var remove = new Button { Content = "Quitar" };
            var cancel = new Button { Content = "Cancelar" };
            remove.Click += (_, _) => dialog.Close(true);
            cancel.Click += (_, _) => dialog.Close(false);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = "¿Quitar el recurso del proyecto? El PNG original y las copias guardadas no se borrarán.",
                    TextWrapping = TextWrapping.Wrap },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, remove } } } };
            if (!await dialog.ShowDialog<bool>(this)) return;
            var removed = _active;
            var replacement = removed + 1 < _sheets.Count ? removed + 1 : removed - 1;
            if (replacement >= 0)
                candidate = await Task.Run(() => Decode(_sheets[replacement].Png), _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            _sheets.RemoveAt(removed);
            if (candidate is not null)
            {
                ShowSheet(Math.Min(removed, _sheets.Count - 1), candidate);
                candidate = null;
            }
            else
            {
                Preview.SetImage(null);
                ResetGrid();
                _bitmap?.Dispose();
                _bitmap = null;
                _active = -1;
                EmptyViewer.IsVisible = true;
                ImageViewer.IsVisible = false;
                GridMenu.IsEnabled = false;
                ResolutionText.Text = "Resolución: —";
                ViewerInfo.Text = "Sin imagen";
            }
            RefreshResources();
            MarkDirty();
            StatusText.Text = "Recurso quitado · Archivos fuente intactos";
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = $"No se pudo quitar: {ex.Message}"; }
        finally { candidate?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void RefreshClips()
    {
        _refreshingClips = true;
        try { ClipList.ItemsSource = _clips.ToArray(); ClipList.SelectedIndex = -1; RefreshTracks(null); }
        finally { _refreshingClips = false; }
    }

    private async void OnCreateClip(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0 || _grid is null || _selected is not int selected)
        {
            StatusText.Text = "Selecciona una celda de una cuadrícula.";
            return;
        }
        if (_clips.Count >= 256 || _grid.Columns > 4096)
        {
            StatusText.Text = "Límite de clips o fotogramas alcanzado.";
            return;
        }
        SetBusy(true);
        try
        {
            var row = selected / _grid.Columns;
            var input = new TextBox { Text = $"Clip {_clips.Count + 1}", MaxLength = 200 };
            var dialog = new Window { Title = "Crear clip", Width = 440, Height = 190,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Content = "Crear" };
            var cancel = new Button { Content = "Cancelar" };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = $"Fila {row + 1}: {_grid.Columns} fotogramas" }, input,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, ok } } } };
            var name = await dialog.ShowDialog<string?>(this);
            if (string.IsNullOrWhiteSpace(name)) return;
            _clips.Add(new ViewerClip(Guid.NewGuid(), name, _sheets[_active].Id,
                Enumerable.Range(row * _grid.Columns, _grid.Columns).ToArray()));
            RefreshClips();
            MarkDirty();
            StatusText.Text = "Clip creado · Guarda el proyecto para conservarlo";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnRemoveClip(object? sender, RoutedEventArgs e)
    {
        if (_importing || ClipList.SelectedItem is not ViewerClip clip) return;
        _clips.Remove(clip);
        RefreshClips();
        MarkDirty();
        StatusText.Text = "Clip quitado · PNG intacto";
    }

    private async void OnClipSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingClips || _importing) return;
        if (ClipList.SelectedItem is not ViewerClip clip) { RefreshTracks(null); return; }
        var index = _sheets.FindIndex(s => s.Id == clip.SheetId);
        if (index < 0) return;
        SetBusy(true);
        Bitmap? candidate = null;
        try
        {
            if (index != _active)
            {
                candidate = await Task.Run(() => Decode(_sheets[index].Png), _lifetime.Token);
                if (_lifetime.IsCancellationRequested) return;
                ShowSheet(index, candidate);
                candidate = null;
                RefreshResources();
            }
            RefreshTracks(clip);
            var track = clip.EffectiveTracks[0];
            SetPlayback(track);
            StatusText.Text = $"{clip.Name} · {track} · {track.Cells.Length} fotogramas";
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = ex.Message; }
        finally { candidate?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void RefreshTracks(ViewerClip? clip, int index = 0)
    {
        ClearPlayback();
        _refreshingTracks = true;
        try
        {
            TrackList.ItemsSource = clip?.EffectiveTracks.ToArray() ?? [];
            TrackList.SelectedIndex = clip is null ? -1 : index;
        }
        finally { _refreshingTracks = false; }
    }

    private void ReplaceClip(ViewerClip clip, ViewerTrack[] tracks, int index)
    {
        var position = _clips.FindIndex(c => c.Id == clip.Id);
        if (position < 0) return;
        var updated = clip with { Cells = tracks[0].Cells, Tracks = tracks };
        _clips[position] = updated;
        _refreshingClips = true;
        try { ClipList.ItemsSource = _clips.ToArray(); ClipList.SelectedIndex = position; }
        finally { _refreshingClips = false; }
        RefreshTracks(updated, index);
        SetPlayback(tracks[index]);
        MarkDirty();
    }

    private async void OnAssignTrack(object? sender, RoutedEventArgs e)
    {
        if (_importing || ClipList.SelectedItem is not ViewerClip clip ||
            _active < 0 || _sheets[_active].Id != clip.SheetId ||
            _grid is null || _selected is not int selected)
        {
            StatusText.Text = "Selecciona un clip y una celda de su hoja.";
            return;
        }
        if (_grid.Columns > 4096) { StatusText.Text = "Demasiados fotogramas."; return; }
        var columns = _grid.Columns;
        var row = selected / columns;
        SetBusy(true);
        try
        {
            var input = new TextBox { Text = "front", MaxLength = 80 };
            var dialog = new Window { Title = "Asignar dirección", Width = 460, Height = 230,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Content = "Asignar" };
            var cancel = new Button { Content = "Cancelar" };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = $"Fila {row + 1} · {columns} fotogramas" },
                    new TextBlock { Text = "Etiqueta libre (front, left, right, back...). Una etiqueta existente sustituye su pista.", TextWrapping = TextWrapping.Wrap },
                    input, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, ok } } } };
            var name = await dialog.ShowDialog<string?>(this);
            if (string.IsNullOrWhiteSpace(name)) return;
            var tracks = clip.EffectiveTracks.ToList();
            var track = new ViewerTrack(name, Enumerable.Range(row * columns, columns).ToArray());
            var index = tracks.FindIndex(t => t.Direction == name);
            if (index < 0 && clip.Tracks is null) { tracks[0] = track; index = 0; }
            else if (index < 0)
            {
                if (tracks.Count >= 64) { StatusText.Text = "Límite de direcciones alcanzado."; return; }
                index = tracks.Count;
                tracks.Add(track);
            }
            else tracks[index] = track;
            ReplaceClip(clip, tracks.ToArray(), index);
            StatusText.Text = $"{clip.Name} · Dirección {name} asignada";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnRemoveTrack(object? sender, RoutedEventArgs e)
    {
        if (_importing || ClipList.SelectedItem is not ViewerClip clip ||
            _active < 0 || _sheets[_active].Id != clip.SheetId) return;
        var tracks = clip.EffectiveTracks.ToList();
        var index = TrackList.SelectedIndex;
        if (index < 0 || index >= tracks.Count) return;
        if (tracks.Count == 1) { StatusText.Text = "El clip debe conservar una dirección."; return; }
        tracks.RemoveAt(index);
        ReplaceClip(clip, tracks.ToArray(), Math.Min(index, tracks.Count - 1));
        StatusText.Text = "Dirección quitada · PNG intacto";
    }

    private void OnTrackSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingTracks || _refreshingClips || _importing ||
            ClipList.SelectedItem is not ViewerClip clip || TrackList.SelectedItem is not ViewerTrack track ||
            _active < 0 || _sheets[_active].Id != clip.SheetId) return;
        SetPlayback(track);
        StatusText.Text = $"{clip.Name} · {track} · {track.Cells.Length} fotogramas";
    }

    private void EditTimeline(TimelineOperation operation, int position, int argument = 0)
    {
        if (_importing || _playback is not { IsPlaying: false } ||
            _playbackTrack is not { } current || _grid is null ||
            ClipList.SelectedItem is not ViewerClip clip || _active < 0 ||
            _sheets[_active].Id != clip.SheetId) return;
        if (operation == TimelineOperation.Insert && (argument < 0 || argument >= _grid.Count)) return;
        try
        {
            var result = TimelineEdit.Apply(current.Cells, current.DurationsMs, position, operation, argument);
            var tracks = clip.EffectiveTracks.ToArray();
            var index = Array.FindIndex(tracks, t => t.Direction == current.Direction);
            if (index < 0) return;
            tracks[index] = current with { Cells = result.Cells, DurationsMs = result.Durations };
            ReplaceClip(clip, tracks, index);
            _playback?.Step(result.Selected);
            ShowPlaybackCell();
            StatusText.Text = "Secuencia actualizada · Cambios sin guardar";
        }
        catch (ArgumentException ex) { StatusText.Text = ex.Message; }
    }

    private void OnOccurrenceMoved(int source, int destination) =>
        EditTimeline(TimelineOperation.Move, source, destination);

    private void OnDuplicateFrame(object? sender, RoutedEventArgs e)
    {
        if (_playback is { } cursor) EditTimeline(TimelineOperation.Duplicate, cursor.Position);
    }

    private void OnRemoveFrame(object? sender, RoutedEventArgs e)
    {
        if (_playback is { } cursor) EditTimeline(TimelineOperation.Remove, cursor.Position);
    }

    private void OnMoveFrameLeft(object? sender, RoutedEventArgs e)
    {
        if (_playback is { } cursor) EditTimeline(TimelineOperation.Move, cursor.Position, cursor.Position - 1);
    }

    private void OnMoveFrameRight(object? sender, RoutedEventArgs e)
    {
        if (_playback is { } cursor) EditTimeline(TimelineOperation.Move, cursor.Position, cursor.Position + 1);
    }

    private async void OnAddFrame(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { IsPlaying: false } cursor || _grid is null) return;
        var count = _grid.Count;
        var position = cursor.Position;
        var input = new TextBox { Text = (cursor.Cell + 1).ToString() };
        var hint = new TextBlock { Text = $"Celda de la hoja (1–{count}). Se inserta después del fotograma seleccionado.", TextWrapping = TextWrapping.Wrap };
        var dialog = new Window { Title = "Añadir fotograma", Width = 420, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var ok = new Button { Content = "Añadir" };
        var cancel = new Button { Content = "Cancelar" };
        ok.Click += (_, _) =>
        {
            if (int.TryParse(input.Text, out var cell) && cell >= 1 && cell <= count) dialog.Close((int?)(cell - 1));
            else hint.Text = $"Introduce una celda entre 1 y {count}.";
        };
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
            Children = { hint, input, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, ok } } } };
        SetBusy(true);
        int? selected = null;
        try { selected = await dialog.ShowDialog<int?>(this); }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
        if (!_lifetime.IsCancellationRequested && selected is int source)
            EditTimeline(TimelineOperation.Insert, position, source);
    }

    private void OnOccurrenceSelected(int position)
    {
        if (_importing || _playback is not { } cursor) return;
        PausePlayback();
        cursor.Step(position - cursor.Position);
        _previewClock?.Reset();
        ShowPlaybackCell();
    }

    private void OnManualCell(int index)
    {
        if (_importing || _grid is null || index < 0 || index >= _grid.Count) return;
        if (_playback is { } cursor && _playbackTrack is { } track)
        {
            var occurrence = SequenceSelection.Find(track.Cells, cursor.Position, index);
            if (occurrence is int position) { OnOccurrenceSelected(position); return; }
        }
        DeselectClip();
        SelectCell(index);
    }

    private void DeselectClip()
    {
        _refreshingClips = true;
        try { ClipList.SelectedIndex = -1; RefreshTracks(null); }
        finally { _refreshingClips = false; }
        if (_selected is int cell) SelectCell(cell);
    }

    private void OnDeselectClip(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        DeselectClip();
        StatusText.Text = "Modo hoja · Selecciona un clip para reproducir";
    }

    private void RestoreGridFields()
    {
        if (_active < 0) return;
        CellWidth.Text = (_sheets[_active].CellWidth ?? 48).ToString();
        CellHeight.Text = (_sheets[_active].CellHeight ?? 48).ToString();
    }

    private void OnRestoreGrid(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        RestoreGridFields();
        StatusText.Text = "Dimensiones restablecidas · Cuadrícula sin cambios";
    }

    private void OnResetLayout(object? sender, RoutedEventArgs e)
    {
        WorkspaceGrid.ColumnDefinitions[0].Width = new Avalonia.Controls.GridLength(240);
        WorkspaceGrid.ColumnDefinitions[2].Width = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star);
        WorkspaceGrid.ColumnDefinitions[4].Width = new Avalonia.Controls.GridLength(300);
        WorkspaceGrid.RowDefinitions[0].Height = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star);
        WorkspaceGrid.RowDefinitions[2].Height = new Avalonia.Controls.GridLength(320);
        WorkspaceGrid.RowDefinitions[4].Height = new Avalonia.Controls.GridLength(210);
        StatusText.Text = "Distribución restablecida";
    }

    private void SetPlayback(ViewerTrack track)
    {
        ClearPlayback();
        _playback = new PlaybackCursor(track.Cells);
        _playbackTrack = track;
        InspectorTabs.SelectedIndex = 1;
        AnimationHeading.Text = $"ANIMACIÓN Y LÍNEA DE TIEMPO · {(ClipList.SelectedItem as ViewerClip)?.Name ?? track.ToString()}";
        _previewClock = new PlaybackClock(_playback, Enumerable.Range(0, track.Cells.Length).Select(track.DurationAt));
        CellStrip.SetSequence(track.Cells, Enumerable.Range(0, track.Cells.Length).Select(track.DurationAt).ToArray());
        FpsInput.Text = (track.Fps ?? PreviewTiming.DefaultFps).ToString();
        ShowPlaybackCell();
    }

    private void ClearPlayback()
    {
        _playTimer.Stop();
        _clock.Reset();
        _previewClock = null;
        _playback = null;
        _playbackTrack = null;
        AnimationHeading.Text = "ANIMACIÓN Y LÍNEA DE TIEMPO · Sin clip";
        CellStrip.SetSequence(null);
        if (_selected is int cell) CellStrip.SetSelection(cell);
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void PausePlayback()
    {
        _playTimer.Stop();
        _clock.Reset();
        _playback?.Pause();
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void UpdatePlaybackControls()
    {
        DeselectClipButton.IsEnabled = !_importing && ClipList.SelectedIndex >= 0;
        var available = !_importing && _playback is not null;
        var editing = available && _playback?.IsPlaying != true;
        EditSequenceButton.IsEnabled = AddFrameButton.IsEnabled = DuplicateFrameButton.IsEnabled = editing;
        RemoveFrameButton.IsEnabled = editing && _playback!.Count > 1;
        MoveFrameLeftButton.IsEnabled = editing && _playback!.Position > 0;
        MoveFrameRightButton.IsEnabled = editing && _playback!.Position < _playback.Count - 1;
        CellStrip.CanEdit = editing;
        PlayButton.IsEnabled = PlayMenu.IsEnabled = StopButton.IsEnabled = LoopCheck.IsEnabled = available;
        FpsInput.IsEnabled = DurationInput.IsEnabled = ApplyTimingButton.IsEnabled = available && _playback?.IsPlaying != true;
        OnionPrevious.IsEnabled = OnionNext.IsEnabled = OnionOpacity.IsEnabled = OnionTint.IsEnabled = available && _playback?.IsPlaying != true;
        var playing = _playback?.IsPlaying == true;
        FrameEditPanel.IsEnabled = !_importing && !playing && _grid is not null && _selected.HasValue;
        FramePreview.CanMoveSprite = FrameEditPanel.IsEnabled && MoveSpriteCheck.IsChecked == true;
        if (_playIconActive != playing)
        {
            _playIconActive = playing;
            ToolbarIcons.Set(PlayButton, playing ? "pause" : "play", playing ? "Pausa (Espacio)" : "Reproducir (Espacio)");
        }
        UpdateScrubber();
        PlayMenu.Header = _playback?.IsPlaying == true ? "Pausa" : "Reproducir";
        if (_playback is { } cursor)
        {
            PreviousButton.IsEnabled = available && cursor.Position > 0;
            NextButton.IsEnabled = available && cursor.Position < cursor.Count - 1;
        }
        else
        {
            PreviousButton.IsEnabled = !_importing && _selected is int previous && previous > 0;
            NextButton.IsEnabled = !_importing && _grid is not null && _selected is int next && next < _grid.Count - 1;
        }
        AssignTrackButton.IsEnabled = RemoveTrackButton.IsEnabled = !_importing && ClipList.SelectedItem is ViewerClip;
        CreateClipButton.IsEnabled = !_importing && _grid is not null && _selected.HasValue;
        RemoveClipButton.IsEnabled = !_importing && ClipList.SelectedItem is ViewerClip;
    }

    private void ShowPlaybackCell()
    {
        if (_playback is not { } cursor) return;
        SelectCell(cursor.Cell);
        CellStrip.SetSelection(cursor.Position);
        TimelineInfo.Text = $"Fotograma {cursor.Position + 1} / {cursor.Count} · Celda {cursor.Cell + 1}";
        if (_playbackTrack is { } track)
        {
            var duration = track.DurationAt(cursor.Position);
            DurationInfo.Text = $"Duración: {duration:0.##} ms";
            TimelineInfo.Text += $" · {duration:0.##} ms";
            DurationInput.Text = track.DurationsMs?[cursor.Position]?.ToString() ?? "";

        }
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void UpdateOnion()
    {
        OnionOpacityText.Text = $"{OnionOpacity.Value:0}%";
        if (_playback is not { } cursor || cursor.IsPlaying || _grid is null)
        {
            FramePreview.SetOnion(null, null, 0);
            OnionFeedback.Text = "";
            return;
        }
        var loop = LoopCheck.IsChecked == true;
        var previous = OnionPrevious.IsChecked == true ? cursor.Neighbor(-1, loop) : null;
        var next = OnionNext.IsChecked == true ? cursor.Neighbor(1, loop) : null;
        FramePreview.SetOnion(previous is int a ? _grid.GetCell(a) : null,
            next is int b ? _grid.GetCell(b) : null, OnionOpacity.Value / 100.0, OnionTint.IsChecked == true,
            previous is int pc ? GetOffset(pc) : null, next is int nc ? GetOffset(nc) : null);
        OnionFeedback.Text = FramePreview.TintFailed ? "No se pudieron aplicar los tintes; se muestran los colores originales." : "";
    }

    private void OnPlay(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { } cursor) return;
        if (cursor.IsPlaying) { PausePlayback(); return; }
        if (cursor.HasEnded) { cursor.Stop(); _previewClock?.Reset(); }
        cursor.Play();
        ShowPlaybackCell();
        _clock.Restart();
        _playTimer.Start();
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is null) return;
        _playTimer.Stop();
        _clock.Reset();
        _previewClock?.Reset();
        _playback.Stop();
        ShowPlaybackCell();
    }

    private void OnAnimationKey(object? sender, KeyEventArgs e)
    {
        if (_importing || _playback is null || e.KeyModifiers != KeyModifiers.None ||
            e.Source is TextBox || e.Source is Button || e.Source is CheckBox ||
            e.Source is ComboBox || e.Source is ListBoxItem || e.Source is Slider) return;
        switch (e.Key)
        {
            case Key.Space: OnPlay(this, e); break;
            case Key.Left: OnPrevious(this, e); break;
            case Key.Right: OnNext(this, e); break;
            default: return;
        }
        e.Handled = true;
    }

    private void OnFrameZoomIn(object? sender, RoutedEventArgs e)
    {
        FramePreview.ChangeZoom(1);
        FramePreviewZoom.Text = FramePreview.ZoomLabel;
    }

    private void OnFrameZoomOut(object? sender, RoutedEventArgs e)
    {
        FramePreview.ChangeZoom(-1);
        FramePreviewZoom.Text = FramePreview.ZoomLabel;
    }

    private void OnFrameZoomReset(object? sender, RoutedEventArgs e)
    {
        FramePreview.ResetView();
        FramePreviewZoom.Text = FramePreview.ZoomLabel;
    }

    private async void OnEditSequence(object? sender, RoutedEventArgs e)
    {
        if (_importing || _grid is null || _playbackTrack is not { } current ||
            ClipList.SelectedItem is not ViewerClip clip || _active < 0 ||
            _sheets[_active].Id != clip.SheetId) return;
        SetBusy(true);
        try
        {
            var input = new TextBox { Text = string.Join(", ", current.Cells.Select(c => c + 1)),
                AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 90 };
            var dialog = new Window { Title = "Editar secuencia", Width = 500, Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Content = "Aplicar" };
            var cancel = new Button { Content = "Cancelar" };
            var hint = new TextBlock { Text = "Números de celda (desde 1), separados por comas. Puedes repetir, quitar o cambiar el orden. Los tiempos siguen a cada aparición conservada.",
                TextWrapping = TextWrapping.Wrap };
            ok.Click += (_, _) =>
            {
                try { dialog.Close(SequenceEdit.Parse(input.Text ?? "", _grid.Count)); }
                catch (ArgumentException ex) { hint.Text = ex.Message; }
            };
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { hint, input, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                    Children = { cancel, ok } } } };
            var cells = await dialog.ShowDialog<int[]?>(this);
            if (cells is null) return;
            var tracks = clip.EffectiveTracks.ToArray();
            var index = Array.FindIndex(tracks, t => t.Direction == current.Direction);
            if (index < 0) return;
            var timings = SequenceEdit.Remap(current.Cells, current.DurationsMs, cells);
            tracks[index] = current with { Cells = cells, DurationsMs = timings };
            ReplaceClip(clip, tracks, index);
            StatusText.Text = "Secuencia actualizada";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnApplyTiming(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { } cursor || cursor.IsPlaying ||
            ClipList.SelectedItem is not ViewerClip clip || _playbackTrack is null ||
            _active < 0 || _sheets[_active].Id != clip.SheetId) return;
        if (!int.TryParse(FpsInput.Text, out var fps) || fps is < 1 or > 60)
        { StatusText.Text = "Introduce FPS entre 1 y 60."; return; }
        int? duration = null;
        if (!string.IsNullOrWhiteSpace(DurationInput.Text))
        {
            if (!int.TryParse(DurationInput.Text, out var ms) || ms is < 1 or > PreviewTiming.MaxDurationMs)
            { StatusText.Text = "Duración entre 1 y 60000 ms, o vacía para usar FPS."; return; }
            duration = ms;
        }
        var tracks = clip.EffectiveTracks.ToArray();
        var index = Array.FindIndex(tracks, t => t.Direction == _playbackTrack.Direction);
        if (index < 0) return;
        var position = cursor.Position;
        var overrides = tracks[index].DurationsMs?.ToArray() ?? new int?[cursor.Count];
        overrides[position] = duration;
        tracks[index] = tracks[index] with { Fps = fps, DurationsMs = overrides.Any(d => d.HasValue) ? overrides : null };
        ReplaceClip(clip, tracks, index);
        _playback?.Step(position);
        ShowPlaybackCell();
        StatusText.Text = "Tiempo de previsualización actualizado";
    }

    private void PlaybackTick()
    {
        if (_importing || _playback is not { } cursor || _previewClock is not { } timing)
        { PausePlayback(); return; }
        var elapsed = _clock.Elapsed.TotalMilliseconds;
        _clock.Restart();
        if (timing.Advance(elapsed, LoopCheck.IsChecked == true)) ShowPlaybackCell();
        else UpdateScrubber();
        if (!cursor.IsPlaying) PausePlayback();
    }

    private void UpdateVisibleBounds()
    {
        if (_validationResult is null || _selected is not int cell || _grid is null)
        { VisibleBoundsInfo.Text = "Área visible: valida la hoja"; return; }
        if (!_validationResult.Bounds.TryGetValue(cell, out var b))
        { VisibleBoundsInfo.Text = "Área visible: celda vacía"; return; }
        VisibleBoundsInfo.Text = $"Área visible: {b.Width} × {b.Height} px · ({b.X}, {b.Y})\nMárgenes I/S/D/B: {b.X}/{b.Y}/{_grid.Cell.Width - b.X - b.Width}/{_grid.Cell.Height - b.Y - b.Height} px";
    }

    private void ClearDiagnostics()
    {
        _validationResult = null;
        _validationReport = null;
        ExportReportButton.IsEnabled = false;
        VisibleBoundsInfo.Text = "Área visible: valida la hoja";
        _allFindings = [];
        _diagnosticSummary = "Validación no ejecutada.";
        FindingList.ItemsSource = null;
        Preview.SetDiagnosticMarkers(null);
        DiagnosticText.Text = _diagnosticSummary;
    }

    private void RefreshDiagnosticFilter()
    {
        if (_validationResult is null) return;
        var category = (DiagnosticCategory)Math.Clamp(DiagnosticFilterBox.SelectedIndex, 0, 5);
        var filtered = DiagnosticFilter.Apply(_allFindings, category);
        FindingList.ItemsSource = filtered;
        Preview.SetDiagnosticMarkers(DiagnosticOverlay.IsChecked == true ? DiagnosticFilter.Markers(filtered) : null);
        var visibleCount = filtered.Count(f => f.Code != "LIMIT");
        DiagnosticText.Text = _diagnosticSummary + $" · {visibleCount} avisos visibles";
    }

    private async void OnValidate(object? sender, RoutedEventArgs e)
    {
        if (_importing || _bitmap is null || _grid is null || _active < 0) return;
        var grid = _grid;
        SetBusy(true);
        ClearDiagnostics();
        DiagnosticText.Text = "Analizando hoja y animaciones...";
        try
        {
            SheetValidation.CheckGridLimit(grid);
            var sheetId = _sheets[_active].Id;
            var clips = _clips.Where(c => c.SheetId == sheetId)
                .Select(c => new ValidationClip(c.Name, c.EffectiveTracks.Select(t =>
                    new ValidationTrack(t.Direction, t.Cells.ToArray(), t.Fps, t.DurationsMs?.ToArray())).ToArray())).ToArray();
            var tracks = clips.SelectMany(c => c.Tracks.Select(t => new AlignmentTrack(c.Name,
                t.Direction == "none" ? "Sin dirección" : t.Direction, t.Cells))).ToArray();
            var pixels = ValidationPixels.Read(_bitmap);
            var analysis = await Task.Run(() =>
            {
                var sheet = SheetValidation.Analyze(grid, pixels, _lifetime.Token);
                var baseFindings = sheet.Findings.Where(f => f.Code != "LIMIT").ToArray();
                var alignment = AlignmentValidation.Analyze(grid, sheet.Bounds, tracks,
                    SheetValidation.FindingLimit - baseFindings.Length, _lifetime.Token);
                var list = baseFindings.Concat(alignment.Findings).ToList();
                var clipResult = ClipValidation.Analyze(grid, sheet.Bounds, clips,
                    SheetValidation.FindingLimit - list.Count, _lifetime.Token);
                list.AddRange(clipResult.Findings);
                var omitted = sheet.OmittedFindings + alignment.OmittedFindings + clipResult.OmittedFindings;
                if (omitted > 0) list.Add(new SheetFinding("LIMIT", null, $"Se omiten {omitted} avisos; recuento completo."));
                return (Sheet: sheet, Alignment: alignment, Clips: clipResult, Findings: list, Omitted: omitted);
            }, _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            _validationResult = analysis.Sheet;
            UpdateVisibleBounds();
            _allFindings = analysis.Findings.ToArray();
            _validationReport = ValidationReport.Create(GetVersion(), DateTimeOffset.UtcNow, _projectName,
                _sheets[_active].Name, grid, analysis.Sheet, analysis.Alignment.TotalFindings,
                analysis.Clips.TotalFindings, analysis.Omitted, _allFindings);
            var result = analysis.Sheet;
            _diagnosticSummary = $"{result.TotalCells} celdas · {result.EmptyCells} vacías · {result.EdgeCells} con borde · {result.DuplicateCells} duplicadas · {analysis.Alignment.TotalFindings} avisos de tamaño/alineación · {analysis.Clips.TotalFindings} de animaciones" +
                (analysis.Omitted > 0 ? $" · {analysis.Omitted} avisos no listados" : "");
            RefreshDiagnosticFilter();
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                ClearDiagnostics();
                DiagnosticText.Text = $"Validación incompleta: {ex.Message}";
            }
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async void OnExportReport(object? sender, RoutedEventArgs e)
    {
        if (_importing || _validationReport is not { } report) return;
        SetBusy(true);
        try
        {
            var json = new FilePickerFileType("Informe JSON") { Patterns = ["*.json"] };
            var txt = new FilePickerFileType("Informe de texto") { Patterns = ["*.txt"] };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Exportar informe de validación",
                SuggestedFileName = "validacion",
                DefaultExtension = "json",
                FileTypeChoices = [json, txt],
                ShowOverwritePrompt = true
            });
            if (file is null || _lifetime.IsCancellationRequested) return;
            var extension = System.IO.Path.GetExtension(file.Name);
            string content;
            if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase)) content = report.ToJson();
            else if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) content = report.ToPlainText();
            else { StatusText.Text = "Selecciona una extensión .json o .txt."; return; }
            // Never allow a report to overwrite the project's persistence manifest.
            if (string.Equals(file.Name, ProjectStore.ManifestName, StringComparison.OrdinalIgnoreCase))
            { StatusText.Text = "No se puede sobrescribir el manifiesto del proyecto."; return; }
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes, _lifetime.Token);
            await stream.FlushAsync(_lifetime.Token);
            StatusText.Text = $"Informe exportado · {file.Name}";
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested) StatusText.Text = $"No se pudo exportar el informe: {ex.Message}";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnFindingSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_importing || FindingList.SelectedItem is not SheetFinding { Cell: int cell }) return;
        OnManualCell(cell);
        StatusText.Text = $"Revisando celda {cell + 1} · El aviso requiere revisión manual";
    }

    private static Bitmap Decode(byte[] bytes)
    {
        using var memory = new MemoryStream(bytes, writable: false);
        return new Bitmap(memory);
    }

    private async void OnResourceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || _importing) return;
        var index = ResourceList.SelectedIndex;
        if (index < 0 || index >= _sheets.Count || index == _active) return;
        SetBusy(true);
        Bitmap? candidate = null;
        try
        {
            candidate = await Task.Run(() => Decode(_sheets[index].Png), _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            ShowSheet(index, candidate);
            candidate = null;
            RefreshClips();
            StatusText.Text = "Recurso seleccionado";
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                RefreshResources();
                StatusText.Text = $"No se pudo abrir el recurso: {ex.Message}";
            }
        }
        finally { candidate?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async Task<string?> PickFolder(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        { Title = title, AllowMultiple = false });
        if (folders.Count == 0) return null;
        return folders[0].TryGetLocalPath() ?? throw new IOException("Se requiere una carpeta local.");
    }

    private async void OnSaveProject(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        SetBusy(true);
        try { await SaveProjectAsync(); }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async void OnSaveAs(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        SetBusy(true);
        try { await SaveProjectAsync(saveAs: true); }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async Task<(string Folder, string Name)?> PickSaveDestination()
    {
        var name = new TextBox { Text = _projectName, MaxLength = 100 };
        var parent = new TextBox { Text = _folder is null ? "" : Path.GetDirectoryName(_folder), IsReadOnly = true, Watermark = "Selecciona la ubicación" };
        var preview = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var browse = new Button { Content = "Elegir carpeta…" };
        var save = new Button { Content = "Guardar" };
        var cancel = new Button { Content = "Cancelar" };
        var dialog = new Window { Title = "Guardar proyecto", Width = 520, Height = 320, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        void Refresh()
        {
            try
            {
                var folder = ProjectDestination.Resolve(parent.Text ?? "", name.Text ?? "");
                preview.Text = "Carpeta del proyecto: " + folder;
                save.IsEnabled = true;
            }
            catch (ArgumentException ex) { preview.Text = ex.Message; save.IsEnabled = false; }
        }
        name.TextChanged += (_, _) => Refresh();
        browse.Click += async (_, _) =>
        {
            var selected = await dialog.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                { Title = "Selecciona la ubicación del proyecto", AllowMultiple = false });
            if (selected.Count > 0 && selected[0].TryGetLocalPath() is string path)
            { parent.Text = path; Refresh(); }
        };
        save.Click += (_, _) => dialog.Close((ProjectDestination.Resolve(parent.Text ?? "", name.Text ?? ""), name.Text!.Trim()));
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 10,
            Children = { new TextBlock { Text = "Nombre del proyecto" }, name,
                new TextBlock { Text = "Ubicación (carpeta contenedora)" }, parent, browse, preview,
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Children = { cancel, save } } } };
        Refresh();
        return await dialog.ShowDialog<(string Folder, string Name)?>(this);
    }

    private async Task<bool> ConfirmOverwrite(string folder)
    {
        var dialog = new Window { Title = "Confirmar sobrescritura", Width = 520, Height = 230,
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var overwrite = new Button { Content = "Sobrescribir proyecto" };
        var cancel = new Button { Content = "Cancelar" };
        overwrite.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 14,
            Children = { new TextBlock { Text = "Ya existe un proyecto en esta ubicación:", FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = folder, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = "Se reemplazará el manifiesto y se conservará su copia .bak. No se eliminarán los PNG anteriores ni otros archivos.", TextWrapping = TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Children = { cancel, overwrite } } } };
        return await dialog.ShowDialog<bool>(this);
    }

    private async void OnRenameProject(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        SetBusy(true);
        try
        {
            var input = new TextBox { Text = _projectName, MaxLength = 100 };
            var dialog = new Window { Title = "Renombrar proyecto", Width = 460, Height = 210, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Content = "Renombrar" };
            var cancel = new Button { Content = "Cancelar" };
            var hint = new TextBlock { Text = "Cambia el nombre visible; la carpeta actual no se mueve.", TextWrapping = TextWrapping.Wrap };
            ok.Click += (_, _) =>
            {
                try { dialog.Close(ProjectDestination.ValidateName(input.Text ?? "")); }
                catch (ArgumentException ex) { hint.Text = ex.Message; }
            };
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12,
                Children = { hint, input, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, ok } } } };
            var value = await dialog.ShowDialog<string?>(this);
            if (value is null || value == _projectName) return;
            _projectName = value;
            MarkDirty();
            StatusText.Text = "Proyecto renombrado · Guarda para confirmar";
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async Task<bool> SaveProjectAsync(bool saveAs = false)
    {
        try
        {
            var folder = _folder;
            var name = _projectName;
            if (saveAs || folder is null)
            {
                var destination = await PickSaveDestination();
                if (destination is null || _lifetime.IsCancellationRequested) return false;
                (folder, name) = destination.Value;
            }
            if (folder is null || _lifetime.IsCancellationRequested) return false;
            var newDestination = _folder is null || !ViewerWorkspace.SameFolder(_folder, folder);
            var id = newDestination && _folder is not null ? Guid.NewGuid() : _projectId;
            var revision = newDestination ? null : _manifestHash;
            if (newDestination && Directory.Exists(folder))
            {
                if (File.Exists(Path.Combine(folder, ProjectStore.ManifestName)))
                {
                    var existing = await ProjectStore.LoadAsync(folder, _lifetime.Token);
                    if (!await ConfirmOverwrite(folder)) return false;
                    revision = existing.Revision;
                }
                else if (Directory.EnumerateFileSystemEntries(folder).Any())
                    throw new IOException("La carpeta contiene archivos pero no es un proyecto SpriteLab. Elige otra ubicación o nombre.");
            }
            var project = new ViewerProject(id, name, _sheets.ToArray(), _clips.ToArray());
            SaveIndicator.Text = "Guardando…";
            SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#AEBCCA"));
            var hash = await Task.Run(() => ViewerWorkspace.SaveAsync(folder, project, revision, _lifetime.Token));
            if (_lifetime.IsCancellationRequested) return false;
            _folder = folder;
            _projectId = id;
            _projectName = name;
            _manifestHash = hash;
            _dirty = false;
            ProjectText.Text = name;
            SaveIndicator.Text = $"✓ Guardado a las {DateTime.Now:HH:mm:ss}";
            SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#79C9A0"));
            StatusText.Text = $"Proyecto guardado · {folder}";
            return true;
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                StatusText.Text = $"No se pudo guardar: {ex.Message}";
                SaveIndicator.Text = "Error al guardar";
                SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#F08B8B"));
            }
            return false;
        }
    }

    private async void OnOpenProject(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        SetBusy(true);
        Bitmap? first = null;
        try
        {
            if (_dirty && !await ConfirmDiscard()) return;
            var folder = await PickFolder("Selecciona la carpeta del proyecto");
            if (folder is null || _lifetime.IsCancellationRequested) return;
            var loaded = await Task.Run(() => ViewerWorkspace.LoadAsync(folder, _lifetime.Token));
            // Decode all sources before replacing current workspace; keep only the first bitmap.
            first = await Task.Run(() =>
            {
                Bitmap? result = null;
                try
                {
                    foreach (var sheet in loaded.Project.Sheets)
                    {
                        _lifetime.Token.ThrowIfCancellationRequested();
                        var bitmap = Decode(sheet.Png);
                        if (result is null) result = bitmap; else bitmap.Dispose();
                    }
                    return result;
                }
                catch { result?.Dispose(); throw; }
            });
            if (_lifetime.IsCancellationRequested) return;
            _sheets.Clear();
            _sheets.AddRange(loaded.Project.Sheets);
            _clips.Clear();
            _clips.AddRange(loaded.Project.Clips ?? []);
            RefreshClips();
            _projectId = loaded.Project.Id;
            _projectName = loaded.Project.Name;
            _folder = folder;
            _manifestHash = loaded.ManifestHash;
            _dirty = false;
            ProjectText.Text = _projectName;
            SaveIndicator.Text = "✓ Proyecto abierto";
            SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#79C9A0"));
            if (first is not null) { ShowSheet(0, first); first = null; }
            else
            {
                Preview.SetImage(null);
                FramePreview.SetFrame(null, default);
                CellStrip.SetSheet(null, null);
                _bitmap?.Dispose();
                _bitmap = null;
                _active = -1;
                ResetGrid();
                EmptyViewer.IsVisible = true;
                ImageViewer.IsVisible = false;
                GridMenu.IsEnabled = false;
                ResourceText.Text = "Todavía no hay recursos.";
                ResolutionText.Text = "Resolución: —";
                ViewerInfo.Text = "Sin imagen";
            }
            RefreshResources();
            StatusText.Text = "Proyecto abierto";
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = $"No se pudo abrir: {ex.Message}"; }
        finally { first?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async Task<bool> ConfirmDiscard()
    {
        var dialog = new Window { Title = "Cambios sin guardar", Width = 480, Height = 210,
            Background = new SolidColorBrush(Color.Parse("#202630")),
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var save = new Button { Content = "Guardar" };
        var discard = new Button { Content = "No guardar" };
        var cancel = new Button { Content = "Cancelar" };
        save.Click += (_, _) => dialog.Close(1);
        discard.Click += (_, _) => dialog.Close(2);
        cancel.Click += (_, _) => dialog.Close(0);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8,
            Children = { cancel, discard, save } };
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16,
            Children = { new TextBlock { Text = "¿Quieres guardar los cambios del proyecto?",
                FontSize = 16, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = "Si continúas sin guardar, perderás los cambios pendientes.",
                    TextWrapping = TextWrapping.Wrap }, buttons } };
        var result = await dialog.ShowDialog<int>(this);
        return result == 2 || (result == 1 && await SaveProjectAsync());
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose) return;
        if (_importing) { e.Cancel = true; return; }
        if (!_dirty) return;
        e.Cancel = true;
        SetBusy(true);
        try
        {
            if (await ConfirmDiscard()) { _allowClose = true; Close(); }
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

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
                new TextBlock { Text = "SL2 · Clips, direcciones, reproducción y comparación de poses.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
