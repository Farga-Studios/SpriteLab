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
    private readonly List<ViewerSheet> _sheets = new();
    private readonly List<ViewerClip> _clips = new();
    private bool _refreshingClips;
    private bool _refreshingTracks;
    private PlaybackCursor? _playback;
    private ViewerTrack? _playbackTrack;
    private readonly DispatcherTimer _playTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private double _elapsedMs;
    private double _cycleMs;
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
        Preview.CellSelected += OnManualCell;
        CellStrip.CellSelected += OnManualCell;
        CellStrip.OccurrenceSelected += OnOccurrenceSelected;
        _playTimer.Tick += (_, _) => PlaybackTick();
        UpdatePlaybackControls();
        OnionPrevious.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        OnionNext.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        OnionOpacity.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) UpdateOnion(); };
        LoopCheck.PropertyChanged += (_, e) => { if (e.Property == CheckBox.IsCheckedProperty) UpdateOnion(); };
        Preview.PointerWheelChanged += OnPreviewWheel;
        VersionText.Text = $"SpriteLab {GetVersion()} · SL2";
        Closing += OnClosing;
        Closed += (_, _) => { _playTimer.Stop(); _lifetime.Cancel(); Preview.SetImage(null); FramePreview.SetFrame(null, default); CellStrip.SetSheet(null, null); _bitmap?.Dispose(); _bitmap = null; };
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
            var previous = _bitmap;
            Preview.SetImage(candidate);
            _bitmap = candidate;
            candidate = null;
            FramePreview.SetFrame(null, default);
        CellStrip.SetSheet(null, null);
        previous?.Dispose();
            _zoom = 1;
            ZoomText.Text = "100 %";
            EmptyViewer.IsVisible = false;
            ImageViewer.IsVisible = true;
            RefreshClips();
            _sheets.Add(new ViewerSheet(Guid.NewGuid(), files[0].Name, imported!, null, null));
            _active = _sheets.Count - 1;
            RefreshResources();
            MarkDirty();
            ResourceText.Text = $"{_sheets.Count} recursos";
            ResolutionText.Text = $"Resolución: {_bitmap.PixelSize.Width} × {_bitmap.PixelSize.Height}";
            ViewerInfo.Text = $"{_bitmap.PixelSize.Width} × {_bitmap.PixelSize.Height} px";
            ResetGrid();
            ApplyGridButton.IsEnabled = GridMenu.IsEnabled = true;
            StatusText.Text = "PNG cargado · Configura la cuadrícula";
            CellWidth.Text = CellHeight.Text = "48";
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
        ClearPlayback();
        _grid = null;
        _selected = null;
        Preview.SetGrid(null);
        GridInfo.Text = "Sin cuadrícula";
        FrameInfo.Text = "Sin selección";
        RectInfo.Text = "Rectángulo: —";
        DurationInfo.Text = "Duración: —";
        AnchorInfo.Text = "Anclaje: —";
        TimelineInfo.Text = "Sin fotogramas";
        FramePreview.SetFrame(null, default);
        CellStrip.SetSheet(null, null);
        PreviousButton.IsEnabled = NextButton.IsEnabled = false;
    }

    private void OnApplyGrid(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        PausePlayback();
        if (_bitmap is null || _importing) return;
        if (!int.TryParse(CellWidth.Text, out var width) ||
            !int.TryParse(CellHeight.Text, out var height) || width <= 0 || height <= 0)
        {
            StatusText.Text = "Introduce ancho y alto positivos.";
            return;
        }
        try
        {
            if (_active >= 0 && _clips.Any(c => c.SheetId == _sheets[_active].Id) &&
                (_sheets[_active].CellWidth != width || _sheets[_active].CellHeight != height))
            {
                StatusText.Text = "Quita los clips asociados antes de cambiar la cuadrícula.";
                return;
            }
            var candidate = new CellGrid(
                new PixelSize(_bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new PixelSize(width, height));
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
        FramePreview.SetFrame(_bitmap, cell);
        CellStrip.SetSelection(index);
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
        if (_importing) return;
        if (_playback is not null) { PausePlayback(); _playback.Step(-1); ShowPlaybackCell(); }
        else if (_selected is int index) SelectCell(index - 1);
    }

    private void OnNext(object? sender, RoutedEventArgs e)
    {
        if (_importing) return;
        if (_playback is not null) { PausePlayback(); _playback.Step(1); ShowPlaybackCell(); }
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
            SaveProjectMenu.IsEnabled = ResourceList.IsEnabled = ClipList.IsEnabled = !busy;
        ApplyGridButton.IsEnabled = !busy && _bitmap is not null;
        TrackList.IsEnabled = !busy;
        UpdatePlaybackControls();
        RenameResourceButton.IsEnabled = RemoveResourceButton.IsEnabled = !busy && _active >= 0;
    }

    private void MarkDirty()
    {
        _dirty = true;
        ProjectText.Text = _projectName + " *";
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
        else { CellWidth.Text = CellHeight.Text = "48"; }
    }

    private async void OnRenameResource(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0) return;
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
        if (_refreshingClips || _importing || ClipList.SelectedItem is not ViewerClip clip) return;
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
            StatusText.Text = $"{clip.Name} · {track.Direction} · {track.Cells.Length} fotogramas";
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
        StatusText.Text = $"{clip.Name} · {track.Direction} · {track.Cells.Length} fotogramas";
    }

    private void OnOccurrenceSelected(int position)
    {
        if (_importing || _playback is not { } cursor) return;
        PausePlayback();
        cursor.Step(position - cursor.Position);
        ShowPlaybackCell();
    }

    private void OnManualCell(int index)
    {
        if (_importing) return;
        ClearPlayback();
        SelectCell(index);
    }

    private void SetPlayback(ViewerTrack track)
    {
        ClearPlayback();
        _playback = new PlaybackCursor(track.Cells);
        _playbackTrack = track;
        _elapsedMs = 0;
        _cycleMs = Enumerable.Range(0, track.Cells.Length).Sum(track.DurationAt);
        CellStrip.SetSequence(track.Cells);
        FpsInput.Text = (track.Fps ?? PreviewTiming.DefaultFps).ToString();
        ShowPlaybackCell();
    }

    private void ClearPlayback()
    {
        _playTimer.Stop();
        _clock.Reset();
        _elapsedMs = 0;
        _playback = null;
        _playbackTrack = null;
        CellStrip.SetSequence(null);
        if (_selected is int cell) CellStrip.SetSelection(cell);
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void PausePlayback()
    {
        _playTimer.Stop();
        _clock.Reset();
        _elapsedMs = 0;
        _playback?.Pause();
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void UpdatePlaybackControls()
    {
        var available = !_importing && _playback is not null;
        PlayButton.IsEnabled = PlayMenu.IsEnabled = StopButton.IsEnabled = LoopCheck.IsEnabled = available;
        FpsInput.IsEnabled = DurationInput.IsEnabled = ApplyTimingButton.IsEnabled = available && _playback?.IsPlaying != true;
        OnionPrevious.IsEnabled = OnionNext.IsEnabled = OnionOpacity.IsEnabled = available && _playback?.IsPlaying != true;
        PlayButton.Content = _playback?.IsPlaying == true ? "Pausa" : "Reproducir";
        PlayMenu.Header = _playback?.IsPlaying == true ? "Pausa" : "Reproducir";
        if (_playback is { } cursor)
        {
            PreviousButton.IsEnabled = available && cursor.Position > 0;
            NextButton.IsEnabled = available && cursor.Position < cursor.Count - 1;
        }
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
            DurationInput.Text = track.DurationsMs?[cursor.Position]?.ToString() ?? "";

        }
        UpdatePlaybackControls();
        UpdateOnion();
    }

    private void UpdateOnion()
    {
        if (_playback is not { } cursor || cursor.IsPlaying || _grid is null)
        {
            FramePreview.SetOnion(null, null, 0);
            return;
        }
        var loop = LoopCheck.IsChecked == true;
        var previous = OnionPrevious.IsChecked == true ? cursor.Neighbor(-1, loop) : null;
        var next = OnionNext.IsChecked == true ? cursor.Neighbor(1, loop) : null;
        FramePreview.SetOnion(previous is int a ? _grid.GetCell(a) : null,
            next is int b ? _grid.GetCell(b) : null, OnionOpacity.Value / 100.0);
    }

    private void OnPlay(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { } cursor) return;
        if (cursor.IsPlaying) { PausePlayback(); return; }
        if (cursor.Position == cursor.Count - 1) cursor.Stop();
        cursor.Play();
        ShowPlaybackCell();
        _elapsedMs = 0;
        _clock.Restart();
        _playTimer.Start();
    }

    private void OnStop(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is null) return;
        _playTimer.Stop();
        _clock.Reset();
        _elapsedMs = 0;
        _playback.Stop();
        ShowPlaybackCell();
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
        if (_importing || _playback is not { } cursor || _playbackTrack is not { } track) { PausePlayback(); return; }
        _elapsedMs += _clock.Elapsed.TotalMilliseconds;
        _clock.Restart();
        var loop = LoopCheck.IsChecked == true;
        if (loop && _cycleMs > 0 && _elapsedMs >= _cycleMs) _elapsedMs %= _cycleMs;
        var changed = false;
        while (_elapsedMs >= track.DurationAt(cursor.Position))
        {
            _elapsedMs -= track.DurationAt(cursor.Position);
            if (!cursor.Tick(loop)) { PausePlayback(); break; }
            changed = true;
        }
        if (changed) ShowPlaybackCell();
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

    private async Task<bool> SaveProjectAsync()
    {
        try
        {
            var folder = _folder ?? await PickFolder("Selecciona una carpeta para guardar el proyecto");
            if (folder is null || _lifetime.IsCancellationRequested) return false;
            var name = _folder is null ? new DirectoryInfo(folder).Name : _projectName;
            var project = new ViewerProject(_projectId, name, _sheets.ToArray(), _clips.ToArray());
            var hash = await Task.Run(() => ViewerWorkspace.SaveAsync(folder, project, _manifestHash, _lifetime.Token));
            if (_lifetime.IsCancellationRequested) return false;
            _folder = folder;
            _projectName = name;
            _manifestHash = hash;
            _dirty = false;
            ProjectText.Text = name;
            StatusText.Text = "Proyecto guardado";
            return true;
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = $"No se pudo guardar: {ex.Message}"; return false; }
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
