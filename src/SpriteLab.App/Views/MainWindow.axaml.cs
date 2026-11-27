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
using SpriteLab.Core.Processing;

namespace SpriteLab.App.Views;

public partial class MainWindow : Window
{
    private sealed record EditorState(Guid SheetId, FrameOffset[]? Offsets, FrameAnchor[]? Anchors,
        ViewerClip[] Clips, Guid? ClipId, string? Direction, int Position, int? Cell);
    private readonly EditHistory<EditorState> _editHistory = new(64);
    private bool _editorReady;
    private Bitmap? _bitmap;
    private SheetValidationResult? _validationResult;
    private ValidationReport? _validationReport;
    private SheetFinding[] _allFindings = [];
    private string _diagnosticSummary = UiText.T("Validación no ejecutada.");
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
    private string _projectName = UiText.T("Proyecto");
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
        UiTranslate.StaticControls(this);
        InitializeToolbarIcons();
        InitializeCaption();
        _editorReady = true;
        OnApplyGuides(this, new RoutedEventArgs());
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
        FramePreviewZoom.Text = UiText.T("Ajustar");
        VersionText.Text = (UiText.IsEnglish ? $"SpriteLab {GetVersion()} · SL7 polish" : $"SpriteLab {GetVersion()} · SL7 acabado");
        Opened += async (_, _) => { try { var update=await UpdateWindow.CheckAsync(_lifetime.Token); if(update is not null&&!_lifetime.IsCancellationRequested&&!_importing) StatusText.Text=UiText.T("Nueva versión: ")+update.Version+" · "+UiText.T("Actualizaciones"); } catch(Exception) { /* Offline or invalid metadata never blocks startup. */ } };
        Closing += OnClosing;
        Closed += (_, _) => { _playTimer.Stop(); _lifetime.Cancel(); Preview.SetImage(null); FramePreview.SetFrame(null, default); CellStrip.SetSheet(null, null); _bitmap?.Dispose(); _bitmap = null; };
    }

    private async void OnProcessImages(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0 || _bitmap is null || _grid is null || _playback?.IsPlaying == true) return;
        var sheet = _sheets[_active];
        var grid = _grid;
        var selected = _selected ?? 0;
        var bitmap = _bitmap;
        SetBusy(true);
        Bitmap? candidate = null;
        try
        {
            if (_sheets.Count >= 64) throw new InvalidDataException(UiText.T("Límite de recursos alcanzado."));
            var pixels = ValidationPixels.Read(bitmap);
            var result = await ProcessingWindow.RunAsync(this, grid, pixels, selected, sheet.Name, _lifetime.Token);
            if (result is null || _lifetime.IsCancellationRequested) return;
            var bytes = result.Png;
            if (_sheets.Sum(s => (long)s.Png.Length) + bytes.Length > 256L * 1024 * 1024)
                throw new InvalidDataException(UiText.T("Límite de recursos del proyecto alcanzado."));
            using (var stream = new MemoryStream(bytes, writable: false))
                bytes = await PngSource.ReadAsync(stream, _lifetime.Token);
            candidate = await Task.Run(() => Decode(bytes), _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            var stem = Path.GetFileNameWithoutExtension(sheet.Name);
            if (stem.Length > 140) stem = stem[..140];
            var name = stem + "_procesado.png";
            for (var n = 2; _sheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)); n++)
                name = stem + $"_procesado_{n}.png";
            _sheets.Add(new ViewerSheet(Guid.NewGuid(), name, bytes, grid.Cell.Width, grid.Cell.Height));
            ShowSheet(_sheets.Count - 1, candidate);
            candidate = null;
            RefreshResources(); RefreshClips(); MarkDirty();
            StatusText.Text = (UiText.IsEnglish ? $"New resource · {result.ChangedPixels} pixels changed · Original intact" : $"Recurso nuevo · {result.ChangedPixels} píxeles modificados · Original intacto");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Processing failed: {ex.Message}" : $"No se pudo procesar: {ex.Message}"); }
        finally { candidate?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async void OnExportSprites(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0 || _grid is null || _bitmap is null) return;
        var grid=_grid;var sheet=_sheets[_active];var bitmap=_bitmap;var selected=_selected??0;var clips=_clips.ToArray();
        SetBusy(true);
        try
        {
            var rgba=ValidationPixels.Read(bitmap);
            var choice=await ExportOptionsWindow.ShowAsync(this,sheet,grid,rgba,selected,clips,_lifetime.Token);
            if(choice is null||_lifetime.IsCancellationRequested)return;
            var folder=await PickFolder(UiText.T("Seleccionar carpeta de exportación"));
            if(folder is null||_lifetime.IsCancellationRequested)return;
            using var job=CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            var progress=new ProgressBar{Minimum=0,Maximum=100,IsIndeterminate=!choice.Profile.AllSheets,Height=12};
            var status=new TextBlock{Text=UiText.T("Exportando…"),TextWrapping=TextWrapping.Wrap};
            var cancel=new Button{Content=UiText.T("Cancelar")};cancel.Click+=(_,_)=>{job.Cancel();cancel.IsEnabled=false;status.Text=UiText.T("Cancelando…");};
            var dialog=new SuiteDialog {Title=UiText.T("Exportación"),Width=460,Height=180,CanResize=false,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            dialog.Content=new StackPanel{Margin=new Avalonia.Thickness(18),Spacing=12,Children={status,progress,cancel}};
            bool finished=false;dialog.Closing+=(_,args)=>{if(!finished){args.Cancel=true;job.Cancel();}};
            var sheets=_sheets.Where(s=>s.CellWidth.HasValue&&s.CellHeight.HasValue&&
                (choice.Profile.Kind is not (SpriteExportKind.Gif or SpriteExportKind.Animations)||clips.Any(c=>c.SheetId==s.Id))).ToArray();
            ExportOutcome? outcome=null;Exception? error=null;
            dialog.Opened+=async(_,_)=>{
                try{
                    var reporter=new Progress<int>(v=>progress.Value=v);
                    outcome=await Task.Run(async()=>{
                        if(!choice.Profile.AllSheets)return await SpriteExport.WriteAsync(folder,sheet,grid,rgba,clips,choice.Profile.Kind,selected,job.Token,choice.Profile.Gif,choice.Clip,choice.Direction);
                        return await BatchExport.WriteAsync(folder,sheets,s=>{
                            job.Token.ThrowIfCancellationRequested();
                            if(s.Id==sheet.Id)return new ExportInput(s,grid,rgba,selected);
                            using var image=Decode(s.Png);
                            var g=new CellGrid(new PixelSize(image.PixelSize.Width,image.PixelSize.Height),new PixelSize(s.CellWidth!.Value,s.CellHeight!.Value));
                            return new ExportInput(s,g,ValidationPixels.Read(image));
                        },clips,choice.Profile,reporter,job.Token);
                    },job.Token);
                }catch(Exception ex){error=ex;}finally{finished=true;dialog.Close();}
            };
            await dialog.ShowDialog(this);
            if(error is OperationCanceledException){StatusText.Text=UiText.T("Exportación cancelada.");return;}
            if(error is not null)throw error;
            if(outcome is null||_lifetime.IsCancellationRequested)return;
            StatusText.Text=(UiText.IsEnglish ? $"Exported {outcome.Files} files · {outcome.ClippedVisiblePixels} clipped pixels · {outcome.Folder}" : $"Exportados {outcome.Files} archivos · {outcome.ClippedVisiblePixels} píxeles recortados · {outcome.Folder}");
            var done=new SuiteDialog {Title=UiText.T("Exportación completada"),Width=570,Height=210,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            var close=new Button{Content=UiText.T("Aceptar")};close.Click+=(_,_)=>done.Close();
            done.Content=new StackPanel{Margin=new Avalonia.Thickness(18),Spacing=12,Children={
                new TextBlock{Text=(UiText.IsEnglish ? $"{outcome.Files} files · {outcome.ClippedVisiblePixels} visible pixels clipped." : $"{outcome.Files} archivos · {outcome.ClippedVisiblePixels} píxeles visibles recortados."),TextWrapping=TextWrapping.Wrap},
                new TextBlock{Text=outcome.Folder,TextWrapping=TextWrapping.Wrap},close}};
            await done.ShowDialog(this);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!_lifetime.IsCancellationRequested)StatusText.Text=(UiText.IsEnglish ? $"Export failed: {ex.Message}" : $"No se pudo exportar: {ex.Message}");}
        finally{if(!_lifetime.IsCancellationRequested)SetBusy(false);}
    }

    private async void OnVerifyExport(object? sender, RoutedEventArgs e)
    {
        if(_importing)return;
        SetBusy(true);
        try{
            var folder=await PickFolder(UiText.T("Selecciona una exportación con integrity.json"));
            if(folder is null)return;
            await Task.Run(()=>ExportIntegrity.VerifyAsync(folder,_lifetime.Token),_lifetime.Token);
            StatusText.Text=UiText.T("Integridad verificada: todos los archivos coinciden.");
        }catch(OperationCanceledException){}
        catch(Exception ex){if(!_lifetime.IsCancellationRequested)StatusText.Text=UiText.T("Verificación fallida: ")+ex.Message;}
        finally{if(!_lifetime.IsCancellationRequested)SetBusy(false);}
    }

    private async void OnRecoverExports(object? sender, RoutedEventArgs e)
    {
        if(_importing)return;
        SetBusy(true);
        try{
            var folder=await PickFolder(UiText.T("Carpeta padre de exportaciones"));
            if(folder is null)return;
            var candidates=ExportRecovery.Find(folder);
            if(candidates.Length==0){StatusText.Text=UiText.T("No hay exportaciones incompletas reconocidas.");return;}
            var dialog=new SuiteDialog {Title=UiText.T("Recuperación"),Width=560,Height=230,WindowStartupLocation=WindowStartupLocation.CenterOwner};
            var yes=new Button{Content=UiText.T("Apartar incompletas")};var no=new Button{Content=UiText.T("Cancelar")};
            yes.Click+=(_,_)=>dialog.Close(true);no.Click+=(_,_)=>dialog.Close(false);
            dialog.Content=new StackPanel{Margin=new Avalonia.Thickness(18),Spacing=12,Children={
                new TextBlock{Text=(UiText.IsEnglish ? $"Found {candidates.Length} incomplete folders. Inactive folders will be renamed for manual review; no files will be deleted." : $"Se detectaron {candidates.Length} carpetas incompletas. Se renombrarán las inactivas para revisión manual; no se borrará ningún archivo."),TextWrapping=TextWrapping.Wrap},
                new WrapPanel{Orientation=Orientation.Horizontal,Children={yes,no}}}};
            if(!await dialog.ShowDialog<bool>(this))return;
            var count=await Task.Run(()=>candidates.Count(c=>ExportRecovery.TryQuarantine(folder,c)),_lifetime.Token);
            StatusText.Text=(UiText.IsEnglish ? $"Recovery: {count} folders quarantined; {candidates.Length-count} unchanged." : $"Recuperación: {count} carpetas apartadas; {candidates.Length-count} sin cambios.");
        }catch(OperationCanceledException){}
        catch(Exception ex){if(!_lifetime.IsCancellationRequested)StatusText.Text=UiText.T("Recuperación fallida: ")+ex.Message;}
        finally{if(!_lifetime.IsCancellationRequested)SetBusy(false);}
    }

    private async void OnPreferences(object? sender,RoutedEventArgs e)
    {
        if(_importing)return;
        SetBusy(true);
        try
        {
            var options=new[]{UiText.T("Idioma del sistema"),UiText.T("Español"),UiText.T("English")};
            var language=new ComboBox{ItemsSource=options,SelectedIndex=UiText.Preference=="es"?1:UiText.Preference=="en"?2:0};
            var dialog=new SuiteDialog{Title=UiText.T("Preferencias"),Width=480,Height=210,CanResize=false};
            var save=new Button{Content=UiText.T("Guardar"),Classes={"primary"}};
            var cancel=new Button{Content=UiText.T("Cancelar")};
            save.Click+=(_,_)=>dialog.Close(language.SelectedIndex);cancel.Click+=(_,_)=>dialog.Close(-1);
            dialog.Content=new StackPanel{Margin=new Avalonia.Thickness(20),Spacing=12,Children={
                new TextBlock{Text=UiText.T("Idioma"),FontWeight=FontWeight.SemiBold},language,
                new TextBlock{Text=UiText.T("Selecciona el idioma. Se aplicará al reiniciar SpriteLab."),TextWrapping=TextWrapping.Wrap},
                new WrapPanel{Orientation=Orientation.Horizontal,Children={save,cancel}}}};
            var selected=await dialog.ShowDialog<int?>(this);
            if(selected is >=0 and <=2){UiText.Save(new[]{"system","es","en"}[selected.Value]);StatusText.Text=UiText.T("Preferencias guardadas. Reinicia SpriteLab para aplicar el idioma.");}
        }
        catch(Exception ex){StatusText.Text=UiText.T("No se pudo guardar la preferencia.")+" "+ex.Message;}
        finally{if(!_lifetime.IsCancellationRequested)SetBusy(false);}
    }

    private void InitializeCaption()
    {
        ToolbarIcons.Set(CaptionMinimize, "minus", UiText.T("Minimizar"));
        ToolbarIcons.Set(CaptionClose, "close", UiText.T("Cerrar"));
        UpdateCaption();
        PropertyChanged += (_, e) => { if (e.Property == WindowStateProperty) UpdateCaption(); };
    }

    private void UpdateCaption()
    {
        var maximized = WindowState == WindowState.Maximized;
        ToolbarIcons.Set(CaptionMaximize, maximized ? "restore-window" : "maximize-window",
            maximized ? UiText.T("Restaurar ventana") : UiText.T("Maximizar"));
    }

    private void OnCaptionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2) ToggleMaximized();
        else BeginMoveDrag(e);
        e.Handled = true;
    }

    private void ToggleMaximized() => WindowState =
        WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnCaptionMinimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnCaptionMaximize(object? sender, RoutedEventArgs e) => ToggleMaximized();
    private void OnCaptionClose(object? sender, RoutedEventArgs e) => Close();

    private void InitializeToolbarIcons()
    {
        ToolbarIcons.Set(PreviousButton, "previous", UiText.T("Fotograma anterior (←)"));
        ToolbarIcons.Set(PlayButton, "play", UiText.T("Reproducir (Espacio)"));
        ToolbarIcons.Set(StopButton, "stop", UiText.T("Detener"));
        ToolbarIcons.Set(NextButton, "next", UiText.T("Fotograma siguiente (→)"));
        ToolbarIcons.Set(UndoButton, "undo", UiText.T("Deshacer (Ctrl+Z)"));
        ToolbarIcons.Set(RedoButton, "redo", UiText.T("Rehacer (Ctrl+Y)"));
        ToolbarIcons.Set(ReplaceFrameButton, "replace", UiText.T("Reemplazar fotograma"));
        ToolbarIcons.Set(AddFrameButton, "plus", UiText.T("Añadir fotograma"));
        ToolbarIcons.Set(DuplicateFrameButton, "copy", UiText.T("Duplicar fotograma"));
        ToolbarIcons.Set(RemoveFrameButton, "trash", UiText.T("Quitar fotograma"));
        ToolbarIcons.Set(MoveFrameLeftButton, "left", UiText.T("Mover fotograma a la izquierda"));
        ToolbarIcons.Set(MoveFrameRightButton, "right", UiText.T("Mover fotograma a la derecha"));
        ToolbarIcons.Set(EditSequenceButton, "edit", UiText.T("Editar secuencia"));
        ToolbarIcons.Set(AssignTrackButton, "plus", UiText.T("Asignar fila a dirección"));
        ToolbarIcons.Set(RemoveTrackButton, "trash", UiText.T("Quitar dirección"));
        ToolbarIcons.Set(ApplyTimingButton, "check", UiText.T("Aplicar tiempo"));
        ToolbarIcons.Set(ValidateButton, "check", UiText.T("Validar hoja"));
        ToolbarIcons.Set(ExportReportButton, "export", UiText.T("Exportar informe"));
        ToolbarIcons.Set(ZoomOutButton, "zoom-out", UiText.T("Reducir zoom"));
        ToolbarIcons.Set(ZoomInButton, "zoom-in", UiText.T("Ampliar zoom"));
        ToolbarIcons.Set(ZoomResetButton, "reset", "Zoom 100%");
        ToolbarIcons.Set(FitButton, "fit", "Ajustar vista");
        ToolbarIcons.Set(ToggleGridButton, "grid", UiText.T("Mostrar u ocultar cuadrícula"));
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
                Title = UiText.T("Importar hoja PNG"),
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }]
            });
            if (files.Count == 0 || token.IsCancellationRequested) return;
            StatusText.Text = UiText.T("Importando...");
            using (var stream = await files[0].OpenReadAsync())
            {
                var bytes = await PngSource.ReadAsync(stream, token);
                if (_sheets.Count >= 64 || _sheets.Sum(s => (long)s.Png.Length) + bytes.Length > 256L * 1024 * 1024)
                    throw new InvalidDataException(UiText.T("Límite de recursos del proyecto alcanzado."));
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
            StatusText.Text = UiText.T("PNG cargado · Configura la cuadrícula en Hoja");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                StatusText.Text = (UiText.IsEnglish ? $"PNG import failed: {ex.Message}" : $"No se pudo importar el PNG: {ex.Message}");
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
        ClearEditorHistory();
        ClearDiagnostics();
        ClearPlayback();
        _grid = null;
        _selected = null;
        BatchMode.IsChecked = false;
        BatchCells.Text = "";
        RefreshFrameEdit();
        Preview.SetGrid(null);
        GridInfo.Text = UiText.T("Sin cuadrícula");
        FrameInfo.Text = UiText.T("Sin selección");
        RectInfo.Text = UiText.T("Rectángulo: —");
        DurationInfo.Text = UiText.T("Duración: —");
        AnchorInfo.Text = UiText.T("Anclaje: —");
        TimelineInfo.Text = UiText.T("Sin fotogramas");
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
            if (suggestions.Count == 0) { StatusText.Text = UiText.T("No hay suficientes píxeles visibles para sugerir una cuadrícula."); return; }
            var list = new ListBox { ItemsSource = suggestions, SelectedIndex = 0 };
            var preview = new SheetPreview { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            preview.SetImage(bitmap);
            var scroll = new ScrollViewer { Content = preview, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
            var dialog = new SuiteDialog { Title = UiText.T("Asistente de cuadrícula"), Width = 940, Height = 620,
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
                    detail.Text = (UiText.IsEnglish ? $"Cell: {s.Width} × {s.Height} px · {s.Columns * s.Rows} cells" : $"Celda: {s.Width} × {s.Height} px · {s.Columns * s.Rows} celdas");
                }
            }
            list.SelectionChanged += (_, _) => UpdatePreview();
            var minus = ToolbarIcons.Create("zoom-out", UiText.T("Reducir zoom"));
            var plus = ToolbarIcons.Create("zoom-in", UiText.T("Ampliar zoom"));
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
            var label = new TextBlock { Text = UiText.T("CUADRÍCULAS SUGERIDAS"), FontWeight = FontWeight.SemiBold, Foreground = UiPalette.Muted };
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
            var apply = new Button { Classes = { "primary" }, Content = UiText.T("Aplicar cuadrícula") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
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
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Grid suggestion failed: {ex.Message}" : $"No se pudo sugerir cuadrícula: {ex.Message}"); }
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
            StatusText.Text = UiText.T("Introduce ancho y alto positivos.");
            return;
        }
        try
        {
            if (_active >= 0 && (_clips.Any(c => c.SheetId == _sheets[_active].Id) || _sheets[_active].Offsets is { Length: > 0 } || _sheets[_active].Anchors is { Length: > 0 }) &&
                (_sheets[_active].CellWidth != width || _sheets[_active].CellHeight != height))
            {
                RestoreGridFields();
                StatusText.Text = UiText.T("Quita los clips y restablece los desplazamientos y anclajes antes de cambiar la cuadrícula.");
                return;
            }
            if (_grid is not null && _grid.Cell.Width == width && _grid.Cell.Height == height)
            {
                StatusText.Text = UiText.T("La cuadrícula ya tiene esas dimensiones.");
                return;
            }
            var candidate = new CellGrid(
                new PixelSize(_bitmap.PixelSize.Width, _bitmap.PixelSize.Height),
                new PixelSize(width, height));
            ClearEditorHistory();
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
            GridInfo.Text = (UiText.IsEnglish ? $"{candidate.Columns} columns × {candidate.Rows} rows · {candidate.Count} cells" : $"{candidate.Columns} columnas × {candidate.Rows} filas · {candidate.Count} celdas");
            SelectCell(0);
            ValidateMenu.IsEnabled = ValidateButton.IsEnabled = true;
            StatusText.Text = UiText.T("Cuadrícula aplicada · Sin modificar el PNG");
        }
        catch (ArgumentException)
        {
            RestoreGridFields();
            StatusText.Text = UiText.T("La imagen debe dividirse exactamente en celdas completas.");
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
        FrameInfo.Text = (UiText.IsEnglish ? $"Cell {index + 1} · row {index / _grid.Columns + 1}, column {index % _grid.Columns + 1}" : $"Celda {index + 1} · fila {index / _grid.Columns + 1}, columna {index % _grid.Columns + 1}");
        RectInfo.Text = (UiText.IsEnglish ? $"Rectangle: ({cell.X}, {cell.Y}) · {cell.Width} × {cell.Height}" : $"Rectángulo: ({cell.X}, {cell.Y}) · {cell.Width} × {cell.Height}");
        UpdateVisibleBounds();
        DurationInfo.Text = UiText.T("Duración: sin asignar");
        AnchorInfo.Text = UiText.T("Anclaje: sin asignar");
        RefreshFrameEdit();
        TimelineInfo.Text = (UiText.IsEnglish ? $"Cell {index + 1} / {_grid.Count}" : $"Celda {index + 1} / {_grid.Count}");
        UpdatePlaybackControls();
    }

    private EditorState CaptureEditorState()
    {
        var sheet = _sheets[_active];
        return new EditorState(sheet.Id, sheet.Offsets, sheet.Anchors, _clips.ToArray(),
            (ClipList.SelectedItem as ViewerClip)?.Id, _playbackTrack?.Direction,
            _playback?.Position ?? 0, _selected);
    }

    private void RecordEditorChange(string label)
    {
        if (_active >= 0 && _grid is not null) _editHistory.Record(CaptureEditorState(), label);
    }

    private void ClearEditorHistory()
    {
        _editHistory.Clear();
        UpdateHistoryControls();
    }

    private void UpdateHistoryControls()
    {
        var enabled = !_importing && _active >= 0 && _grid is not null && _playback?.IsPlaying != true;
        UndoMenu.IsEnabled = UndoButton.IsEnabled = enabled && _editHistory.CanUndo;
        RedoMenu.IsEnabled = RedoButton.IsEnabled = enabled && _editHistory.CanRedo;
        ToolTip.SetTip(UndoButton, _editHistory.UndoLabel is { } u ? $"Deshacer: {u} (Ctrl+Z)" : "Deshacer (Ctrl+Z)");
        ToolTip.SetTip(RedoButton, _editHistory.RedoLabel is { } r ? $"Rehacer: {r} (Ctrl+Y)" : "Rehacer (Ctrl+Y)");
    }

    private void RestoreEditorState(EditorState state)
    {
        if (_active < 0 || _sheets[_active].Id != state.SheetId)
        { ClearEditorHistory(); return; }
        PausePlayback();
        _sheets[_active] = _sheets[_active] with { Offsets = state.Offsets, Anchors = state.Anchors };
        _clips.Clear(); _clips.AddRange(state.Clips);
        RefreshClips();
        var index = _clips.FindIndex(c => c.Id == state.ClipId && c.SheetId == state.SheetId);
        if (index >= 0)
        {
            var clip = _clips[index];
            var tracks = clip.EffectiveTracks;
            var trackIndex = Math.Max(0, Array.FindIndex(tracks.ToArray(), t => t.Direction == state.Direction));
            _refreshingClips = true;
            try { ClipList.SelectedIndex = index; }
            finally { _refreshingClips = false; }
            RefreshTracks(clip, trackIndex);
            SetPlayback(tracks[trackIndex]);
            _playback?.Step(state.Position);
            ShowPlaybackCell();
        }
        else if (state.Cell is int cell) SelectCell(cell);
        MarkDirty();
        RefreshFrameEdit(); UpdateOnion(); UpdateHistoryControls();
    }

    private void OnUndo(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0 || _grid is null || _playback?.IsPlaying == true || !_editHistory.CanUndo) return;
        var label = _editHistory.UndoLabel;
        RestoreEditorState(_editHistory.Undo(CaptureEditorState()));
        StatusText.Text = (UiText.IsEnglish ? $"Undone: {label}" : $"Deshecho: {label}");
    }

    private void OnRedo(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0 || _grid is null || _playback?.IsPlaying == true || !_editHistory.CanRedo) return;
        var label = _editHistory.RedoLabel;
        RestoreEditorState(_editHistory.Redo(CaptureEditorState()));
        StatusText.Text = (UiText.IsEnglish ? $"Redone: {label}" : $"Rehecho: {label}");
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
        OffsetInfo.Text = _selected.HasValue ? $"Desplazamiento: {offset.X}, {offset.Y} px" : UiText.T("Desplazamiento: —");
        var anchor = _selected is int c ? GetAnchor(c) : null;
        AnchorX.Text = (anchor?.X ?? 0).ToString();
        AnchorY.Text = (anchor?.Y ?? 0).ToString();
        AnchorInfo.Text = anchor is null ? UiText.T("Anclaje: sin asignar") : $"Anclaje: {anchor.X}, {anchor.Y} px";
        FramePreview.SetAnchor(anchor);
    }

    private void ApplyFrameOffset(int x, int y)
    {
        if (_importing || _playback?.IsPlaying == true || _grid is null || _active < 0 || _selected is not int cell) return;
        try
        {
            var sheet = _sheets[_active];
            var offset = new FrameOffset(cell, x, y);
            if (offset == GetOffset(cell)) { RefreshFrameEdit(); return; }
            var result = FrameOffset.Set(sheet.Offsets ?? [], _grid, offset);
            RecordEditorChange("Mover sprite");
            _sheets[_active] = sheet with { Offsets = result };
            MarkDirty();
            RefreshFrameEdit();
            UpdateOnion();
            StatusText.Text = (UiText.IsEnglish ? $"Cell {cell + 1} offset: {x}, {y} px · PNG intact" : $"Desplazamiento de celda {cell + 1}: {x}, {y} px · PNG intacto");
        }
        catch (ArgumentException ex) { RefreshFrameEdit(); StatusText.Text = ex.Message; }
    }

    private FrameAnchor? GetAnchor(int cell) =>
        _active >= 0 ? _sheets[_active].Anchors?.FirstOrDefault(a => a.Cell == cell) : null;

    private bool CanEditFrame() => !_importing && _playback?.IsPlaying != true && _grid is not null && _active >= 0 && _selected.HasValue;

    private int[] EditCells()
    {
        if (BatchMode.IsChecked != true) return [_selected!.Value];
        return FrameAlignment.ParseSelection(BatchCells.Text ?? "", _grid!.Count);
    }

    private void OnSelectTrackCells(object? sender, RoutedEventArgs e)
    {
        if (!CanEditFrame() || _playbackTrack is null) return;
        BatchCells.Text = string.Join(",", _playbackTrack.Cells.Distinct().Order().Select(c => c + 1));
        BatchMode.IsChecked = true;
    }

    private void OnSelectRowCells(object? sender, RoutedEventArgs e)
    {
        if (!CanEditFrame()) return;
        var first = _selected!.Value / _grid!.Columns * _grid.Columns + 1;
        BatchCells.Text = $"{first}-{first + _grid.Columns - 1}";
        BatchMode.IsChecked = true;
    }

    private void OnAnchorPreset(object? sender, RoutedEventArgs e)
    {
        if (!_editorReady || !CanEditFrame() || AnchorPreset.SelectedIndex < 0) return;
        var center = AnchorPreset.SelectedIndex == 1;
        AnchorX.Text = (_grid!.Cell.Width / 2).ToString();
        AnchorY.Text = (center ? _grid.Cell.Height / 2 : _grid.Cell.Height).ToString();
    }

    private void ChangeAnchors(bool clear)
    {
        if (!CanEditFrame()) return;
        try
        {
            int? x = null, y = null;
            if (!clear)
            {
                if (!int.TryParse(AnchorX.Text, out var ax) || !int.TryParse(AnchorY.Text, out var ay))
                    throw new ArgumentException("Introduce coordenadas enteras.");
                x = ax; y = ay;
            }
            var cells = EditCells();
            var sheet = _sheets[_active];
            var result = FrameAnchor.SetMany(_grid!, sheet.Anchors ?? [], cells, x, y);
            if (result.SequenceEqual(sheet.Anchors ?? [])) return;
            RecordEditorChange(clear ? "Quitar anclajes" : "Asignar anclajes");
            _sheets[_active] = sheet with { Anchors = result };
            MarkDirty(); RefreshFrameEdit(); UpdateOnion();
            StatusText.Text = (UiText.IsEnglish ? $"Anchors updated in {cells.Length} cells." : $"Anclajes actualizados en {cells.Length} celdas.");
        }
        catch (ArgumentException ex) { StatusText.Text = ex.Message; }
    }

    private void OnApplyAnchor(object? sender, RoutedEventArgs e) => ChangeAnchors(false);
    private void OnClearAnchor(object? sender, RoutedEventArgs e) => ChangeAnchors(true);
    private void OnSnapChanged(object? sender, RoutedEventArgs e) { if (_editorReady) FramePreview.SnapEnabled = SnapCheck.IsChecked == true; }

    private void EditOffsets(Func<int, FrameOffset, FrameOffset> change)
    {
        if (!CanEditFrame()) return;
        try
        {
            var cells = EditCells();
            var sheet = _sheets[_active];
            var result = FrameAlignment.EditMany(_grid!, sheet.Offsets ?? [], cells, change);
            if (result.SequenceEqual(sheet.Offsets ?? [])) { RefreshFrameEdit(); return; }
            RecordEditorChange(UiText.T("Desplazamientos de selección"));
            _sheets[_active] = sheet with { Offsets = result };
            MarkDirty(); RefreshFrameEdit(); UpdateOnion();
            StatusText.Text = (UiText.IsEnglish ? $"Offsets updated in {cells.Length} cells." : $"Desplazamientos actualizados en {cells.Length} celdas.");
        }
        catch (ArgumentException ex) { RefreshFrameEdit(); StatusText.Text = ex.Message; }
        catch (OverflowException) { RefreshFrameEdit(); StatusText.Text = UiText.T("Desplazamiento fuera de rango."); }
    }

    private void OnBatchDelta(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DeltaX.Text, out var x) || !int.TryParse(DeltaY.Text, out var y))
        { StatusText.Text = UiText.T("Introduce incrementos enteros."); return; }
        EditOffsets((c, old) => new FrameOffset(c, checked(old.X + x), checked(old.Y + y)));
    }

    private async void OnAlignFrames(object? sender, RoutedEventArgs e)
    {
        if (!CanEditFrame() || _bitmap is null) return;
        var mode = AlignMode.SelectedIndex;
        var grid = _grid!;
        int[] cells;
        try { cells = EditCells(); }
        catch (ArgumentException ex) { StatusText.Text = ex.Message; return; }
        var sheet = _sheets[_active];
        var sheetId = sheet.Id;
        FrameOffset[]? result = null;
        var skipped = 0;
        SetBusy(true);
        try
        {
            IReadOnlyDictionary<int, VisibleBounds> bounds = new Dictionary<int, VisibleBounds>();
            if (mode != 2)
            {
                SheetValidation.CheckGridLimit(grid);
                var pixels = ValidationPixels.Read(_bitmap);
                bounds = (await Task.Run(() => SheetValidation.Analyze(grid, pixels, _lifetime.Token), _lifetime.Token)).Bounds;
            }
            if (_lifetime.IsCancellationRequested) return;
            result = FrameAlignment.EditMany(grid, sheet.Offsets ?? [], cells, (c, old) =>
            {
                if (mode == 2)
                {
                    var anchor = sheet.Anchors?.FirstOrDefault(a => a.Cell == c);
                    if (anchor is null) { skipped++; return old; }
                    return new FrameOffset(c, grid.Cell.Width / 2 - anchor.X, grid.Cell.Height - anchor.Y);
                }
                if (!bounds.TryGetValue(c, out var b)) { skipped++; return old; }
                var x = (int)Math.Round(grid.Cell.Width / 2.0 - (b.X + b.Width / 2.0));
                var y = grid.Cell.Height - (b.Y + b.Height);
                return mode switch
                {
                    0 => old with { X = x },
                    1 => old with { Y = y },
                    3 => new FrameOffset(c, x, y),
                    _ => old
                };
            });
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Alignment failed: {ex.Message}" : $"No se pudo alinear: {ex.Message}"); }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
        if (result is null || _lifetime.IsCancellationRequested || _active < 0 || _sheets[_active].Id != sheetId) return;
        if (!result.SequenceEqual(sheet.Offsets ?? []))
        {
            RecordEditorChange(UiText.T("Alineación automática"));
            _sheets[_active] = sheet with { Offsets = result };
            MarkDirty(); RefreshFrameEdit(); UpdateOnion();
        }
        StatusText.Text = (UiText.IsEnglish ? $"Alignment: {cells.Length - skipped} cells processed, {skipped} skipped." : $"Alineación: {cells.Length - skipped} celdas procesadas, {skipped} omitidas.");
    }

    private void OnApplyOffset(object? sender, RoutedEventArgs e)
    {
        if (int.TryParse(OffsetX.Text, out var x) && int.TryParse(OffsetY.Text, out var y)) EditOffsets((c, _) => new FrameOffset(c, x, y));
        else { RefreshFrameEdit(); StatusText.Text = UiText.T("Introduce coordenadas enteras en píxeles."); }
    }

    private void OnResetOffset(object? sender, RoutedEventArgs e) => EditOffsets((c, _) => FrameOffset.Zero(c));

    private void OnMoveModeChanged(object? sender, RoutedEventArgs e) { if (_editorReady) UpdatePlaybackControls(); }

    private void OnApplyGuides(object? sender, RoutedEventArgs e)
    {
        if (!int.TryParse(CustomGuideX.Text, out var x) || !int.TryParse(CustomGuideY.Text, out var y) ||
            Math.Abs((long)x) > 8192 || Math.Abs((long)y) > 8192)
        { StatusText.Text = UiText.T("Introduce coordenadas de guía entre -8192 y 8192."); return; }
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
        UpdateHistoryControls();
        if (_validationResult is not null) ClearDiagnostics();
        ProjectText.Text = _projectName + " *";
        SaveIndicator.Text = UiText.T("● Cambios sin guardar");
        SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#E5B760"));
    }

    private void RefreshResources()
    {
        _refreshing = true;
        try
        {
            ResourceList.ItemsSource = _sheets.Select(s => s.Name).ToArray();
            ResourceList.SelectedIndex = _active;
            ResourceText.Text = (UiText.IsEnglish ? $"{_sheets.Count} resources" : $"{_sheets.Count} recursos");
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
        ResourceText.Text = (UiText.IsEnglish ? $"{_sheets.Count} resources" : $"{_sheets.Count} recursos");
        ResolutionText.Text = (UiText.IsEnglish ? $"Resolution: {bitmap.PixelSize.Width} × {bitmap.PixelSize.Height}" : $"Resolución: {bitmap.PixelSize.Width} × {bitmap.PixelSize.Height}");
        ViewerInfo.Text = $"{bitmap.PixelSize.Width} × {bitmap.PixelSize.Height} px";
        ApplyGridButton.IsEnabled = GridMenu.IsEnabled = true;
        if (sheet.CellWidth is int w && sheet.CellHeight is int h)
        {
            CellWidth.Text = w.ToString();
            CellHeight.Text = h.ToString();
            _grid = new CellGrid(new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height), new PixelSize(w, h));
            Preview.SetGrid(_grid);
            CellStrip.SetSheet(_bitmap, _grid);
            GridInfo.Text = (UiText.IsEnglish ? $"{_grid.Columns} columns × {_grid.Rows} rows · {_grid.Count} cells" : $"{_grid.Columns} columnas × {_grid.Rows} filas · {_grid.Count} celdas");
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
            var dialog = new SuiteDialog { Title = UiText.T("Renombrar recurso"), Width = 420, Height = 170,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Classes = { "primary" }, Content = UiText.T("Aceptar") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = UiText.T("Nombre visible del recurso") }, input,
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
            StatusText.Text = UiText.T("Recurso renombrado · Archivo fuente intacto");
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async void OnRemoveResource(object? sender, RoutedEventArgs e)
    {
        if (_importing || _active < 0) return;
        if (_clips.Any(c => c.SheetId == _sheets[_active].Id))
        {
            StatusText.Text = UiText.T("Quita primero los clips asociados al recurso.");
            return;
        }
        SetBusy(true);
        Bitmap? candidate = null;
        try
        {
            var dialog = new SuiteDialog { Title = UiText.T("Quitar recurso"), Width = 440, Height = 180,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var remove = new Button { Classes = { "danger" }, Content = UiText.T("Quitar") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            remove.Click += (_, _) => dialog.Close(true);
            cancel.Click += (_, _) => dialog.Close(false);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = UiText.T("¿Quitar el recurso del proyecto? El PNG original y las copias guardadas no se borrarán."),
                    TextWrapping = TextWrapping.Wrap },
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, remove } } } };
            if (!await dialog.ShowDialog<bool>(this)) return;
            var removed = _active;
            var replacement = removed + 1 < _sheets.Count ? removed + 1 : removed - 1;
            if (replacement >= 0)
                candidate = await Task.Run(() => Decode(_sheets[replacement].Png), _lifetime.Token);
            if (_lifetime.IsCancellationRequested) return;
            ClearEditorHistory();
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
                ResolutionText.Text = UiText.T("Resolución: —");
                ViewerInfo.Text = UiText.T("Sin imagen");
            }
            RefreshResources();
            MarkDirty();
            StatusText.Text = UiText.T("Recurso quitado · Archivos fuente intactos");
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Remove failed: {ex.Message}" : $"No se pudo quitar: {ex.Message}"); }
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
            StatusText.Text = UiText.T("Selecciona una celda de una cuadrícula.");
            return;
        }
        if (_clips.Count >= 256 || _grid.Columns > 4096)
        {
            StatusText.Text = UiText.T("Límite de clips o fotogramas alcanzado.");
            return;
        }
        SetBusy(true);
        try
        {
            var row = selected / _grid.Columns;
            var input = new TextBox { Text = $"Clip {_clips.Count + 1}", MaxLength = 200 };
            var dialog = new SuiteDialog { Title = UiText.T("Crear clip"), Width = 440, Height = 190,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Classes = { "primary" }, Content = UiText.T("Crear") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = (UiText.IsEnglish ? $"Row {row + 1}: {_grid.Columns} frames" : $"Fila {row + 1}: {_grid.Columns} fotogramas") }, input,
                    new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8,
                        Children = { cancel, ok } } } };
            var name = await dialog.ShowDialog<string?>(this);
            if (string.IsNullOrWhiteSpace(name)) return;
            RecordEditorChange(UiText.T("Crear clip"));
            _clips.Add(new ViewerClip(Guid.NewGuid(), name, _sheets[_active].Id,
                Enumerable.Range(row * _grid.Columns, _grid.Columns).ToArray()));
            RefreshClips();
            MarkDirty();
            StatusText.Text = UiText.T("Clip creado · Guarda el proyecto para conservarlo");
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnRemoveClip(object? sender, RoutedEventArgs e)
    {
        if (_importing || ClipList.SelectedItem is not ViewerClip clip) return;
        RecordEditorChange(UiText.T("Quitar clip"));
        _clips.Remove(clip);
        RefreshClips();
        MarkDirty();
        StatusText.Text = UiText.T("Clip quitado · PNG intacto");
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
            StatusText.Text = (UiText.IsEnglish ? $"{clip.Name} · {track} · {track.Cells.Length} frames" : $"{clip.Name} · {track} · {track.Cells.Length} fotogramas");
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
        RecordEditorChange(UiText.T("Editar animación"));
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
            StatusText.Text = UiText.T("Selecciona un clip y una celda de su hoja.");
            return;
        }
        if (_grid.Columns > 4096) { StatusText.Text = UiText.T("Demasiados fotogramas."); return; }
        var columns = _grid.Columns;
        var row = selected / columns;
        SetBusy(true);
        try
        {
            var input = new TextBox { Text = "front", MaxLength = 80 };
            var dialog = new SuiteDialog { Title = UiText.T("Asignar dirección"), Width = 460, Height = 230,
                CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Classes = { "primary" }, Content = UiText.T("Asignar") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            ok.Click += (_, _) => dialog.Close(input.Text?.Trim());
            cancel.Click += (_, _) => dialog.Close(null);
            dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
                Children = { new TextBlock { Text = (UiText.IsEnglish ? $"Row {row + 1} · {columns} frames" : $"Fila {row + 1} · {columns} fotogramas") },
                    new TextBlock { Text = UiText.T("Etiqueta libre (front, left, right, back...). Una etiqueta existente sustituye su pista."), TextWrapping = TextWrapping.Wrap },
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
                if (tracks.Count >= 64) { StatusText.Text = UiText.T("Límite de direcciones alcanzado."); return; }
                index = tracks.Count;
                tracks.Add(track);
            }
            else tracks[index] = track;
            ReplaceClip(clip, tracks.ToArray(), index);
            StatusText.Text = (UiText.IsEnglish ? $"{clip.Name} · Direction {name} assigned" : $"{clip.Name} · Dirección {name} asignada");
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
        if (tracks.Count == 1) { StatusText.Text = UiText.T("El clip debe conservar una dirección."); return; }
        tracks.RemoveAt(index);
        ReplaceClip(clip, tracks.ToArray(), Math.Min(index, tracks.Count - 1));
        StatusText.Text = UiText.T("Dirección quitada · PNG intacto");
    }

    private void OnTrackSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingTracks || _refreshingClips || _importing ||
            ClipList.SelectedItem is not ViewerClip clip || TrackList.SelectedItem is not ViewerTrack track ||
            _active < 0 || _sheets[_active].Id != clip.SheetId) return;
        SetPlayback(track);
        StatusText.Text = (UiText.IsEnglish ? $"{clip.Name} · {track} · {track.Cells.Length} frames" : $"{clip.Name} · {track} · {track.Cells.Length} fotogramas");
    }

    private void EditTimeline(TimelineOperation operation, int position, int argument = 0)
    {
        if (_importing || _playback is not { IsPlaying: false } ||
            _playbackTrack is not { } current || _grid is null ||
            ClipList.SelectedItem is not ViewerClip clip || _active < 0 ||
            _sheets[_active].Id != clip.SheetId) return;
        if ((operation is TimelineOperation.Insert or TimelineOperation.Replace) && (argument < 0 || argument >= _grid.Count)) return;
        if (position < 0 || position >= current.Cells.Length) return;
        if (operation == TimelineOperation.Replace && current.Cells[position] == argument) return;
        if (operation == TimelineOperation.Move && position == argument) return;
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
            StatusText.Text = UiText.T("Secuencia actualizada · Cambios sin guardar");
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

    private async void OnReplaceFrame(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { IsPlaying: false } cursor || _grid is null) return;
        var position = cursor.Position;
        var count = _grid.Count;
        var input = new TextBox { Text = (cursor.Cell + 1).ToString() };
        var hint = new TextBlock { Text = (UiText.IsEnglish ? $"Replace occurrence {position + 1} with a cell from the same sheet (1–{count}). Keeps duration and sequence position." : $"Sustituir aparición {position + 1} por una celda de la misma hoja (1–{count}). Conserva duración y posición en la secuencia."), TextWrapping = TextWrapping.Wrap };
        var dialog = new SuiteDialog { Title = UiText.T("Reemplazar fotograma"), Width = 460, Height = 230,
            Background = UiPalette.Window, Foreground = UiPalette.Text, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var ok = new Button { Classes = { "primary" }, Content = UiText.T("Reemplazar") };
        var cancel = new Button { Content = UiText.T("Cancelar") };
        ok.Click += (_, _) =>
        {
            if (int.TryParse(input.Text, out var cell) && cell >= 1 && cell <= count) dialog.Close((int?)(cell - 1));
            else hint.Text = (UiText.IsEnglish ? $"Enter a cell between 1 and {count}." : $"Introduce una celda entre 1 y {count}.");
        };
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(18), Spacing = 12,
            Children = { hint, input, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { cancel, ok } } } };
        SetBusy(true);
        int? selected = null;
        try { selected = await dialog.ShowDialog<int?>(this); }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
        if (!_lifetime.IsCancellationRequested && selected is int source)
            EditTimeline(TimelineOperation.Replace, position, source);
    }

    private async void OnAddFrame(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { IsPlaying: false } cursor || _grid is null) return;
        var count = _grid.Count;
        var position = cursor.Position;
        var input = new TextBox { Text = (cursor.Cell + 1).ToString() };
        var hint = new TextBlock { Text = (UiText.IsEnglish ? $"Sheet cell (1–{count}). Inserted after the selected frame." : $"Celda de la hoja (1–{count}). Se inserta después del fotograma seleccionado."), TextWrapping = TextWrapping.Wrap };
        var dialog = new SuiteDialog { Title = UiText.T("Añadir fotograma"), Width = 420, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var ok = new Button { Classes = { "primary" }, Content = UiText.T("Añadir") };
        var cancel = new Button { Content = UiText.T("Cancelar") };
        ok.Click += (_, _) =>
        {
            if (int.TryParse(input.Text, out var cell) && cell >= 1 && cell <= count) dialog.Close((int?)(cell - 1));
            else hint.Text = (UiText.IsEnglish ? $"Enter a cell between 1 and {count}." : $"Introduce una celda entre 1 y {count}.");
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
        StatusText.Text = UiText.T("Modo hoja · Selecciona un clip para reproducir");
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
        StatusText.Text = UiText.T("Dimensiones restablecidas · Cuadrícula sin cambios");
    }

    private void OnResetLayout(object? sender, RoutedEventArgs e)
    {
        WorkspaceGrid.ColumnDefinitions[0].Width = new Avalonia.Controls.GridLength(240);
        WorkspaceGrid.ColumnDefinitions[2].Width = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star);
        WorkspaceGrid.ColumnDefinitions[4].Width = new Avalonia.Controls.GridLength(300);
        WorkspaceGrid.RowDefinitions[0].Height = new Avalonia.Controls.GridLength(1, Avalonia.Controls.GridUnitType.Star);
        WorkspaceGrid.RowDefinitions[2].Height = new Avalonia.Controls.GridLength(320);
        WorkspaceGrid.RowDefinitions[4].Height = new Avalonia.Controls.GridLength(210);
        StatusText.Text = UiText.T("Distribución restablecida");
    }

    private void SetPlayback(ViewerTrack track)
    {
        ClearPlayback();
        _playback = new PlaybackCursor(track.Cells);
        _playbackTrack = track;
        InspectorTabs.SelectedIndex = 1;
        AnimationHeading.Text = (UiText.IsEnglish ? $"ANIMATION AND TIMELINE · {(ClipList.SelectedItem as ViewerClip)?.Name ?? track.ToString()}" : $"ANIMACIÓN Y LÍNEA DE TIEMPO · {(ClipList.SelectedItem as ViewerClip)?.Name ?? track.ToString()}");
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
        AnimationHeading.Text = UiText.T("ANIMACIÓN Y LÍNEA DE TIEMPO · Sin clip");
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
        UpdateHistoryControls();
        ReplaceFrameButton.IsEnabled = editing;
        ProcessingMenu.IsEnabled = !_importing && _grid is not null && _bitmap is not null && _playback?.IsPlaying != true;
        ExportSpritesMenu.IsEnabled = ProcessingMenu.IsEnabled;
        var playing = _playback?.IsPlaying == true;
        FrameEditPanel.IsEnabled = !_importing && !playing && _grid is not null && _selected.HasValue;
        FramePreview.CanMoveSprite = FrameEditPanel.IsEnabled && MoveSpriteCheck.IsChecked == true;
        FramePreview.SnapEnabled = SnapCheck.IsChecked == true;
        if (_playIconActive != playing)
        {
            _playIconActive = playing;
            ToolbarIcons.Set(PlayButton, playing ? "pause" : "play", playing ? "Pausa (Espacio)" : UiText.T("Reproducir (Espacio)"));
        }
        UpdateScrubber();
        PlayMenu.Header = _playback?.IsPlaying == true ? UiText.T("Pausa") : UiText.T("Reproducir");
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
        TimelineInfo.Text = (UiText.IsEnglish ? $"Frame {cursor.Position + 1} / {cursor.Count} · Cell {cursor.Cell + 1}" : $"Fotograma {cursor.Position + 1} / {cursor.Count} · Celda {cursor.Cell + 1}");
        if (_playbackTrack is { } track)
        {
            var duration = track.DurationAt(cursor.Position);
            DurationInfo.Text = (UiText.IsEnglish ? $"Duration: {duration:0.##} ms" : $"Duración: {duration:0.##} ms");
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
            var dialog = new SuiteDialog { Title = UiText.T("Editar secuencia"), Width = 500, Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Classes = { "primary" }, Content = UiText.T("Aplicar") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            var hint = new TextBlock { Text = UiText.T("Números de celda (desde 1), separados por comas. Puedes repetir, quitar o cambiar el orden. Los tiempos siguen a cada aparición conservada."),
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
            StatusText.Text = UiText.T("Secuencia actualizada");
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnApplyTiming(object? sender, RoutedEventArgs e)
    {
        if (_importing || _playback is not { } cursor || cursor.IsPlaying ||
            ClipList.SelectedItem is not ViewerClip clip || _playbackTrack is null ||
            _active < 0 || _sheets[_active].Id != clip.SheetId) return;
        if (!int.TryParse(FpsInput.Text, out var fps) || fps is < 1 or > 60)
        { StatusText.Text = UiText.T("Introduce FPS entre 1 y 60."); return; }
        int? duration = null;
        if (!string.IsNullOrWhiteSpace(DurationInput.Text))
        {
            if (!int.TryParse(DurationInput.Text, out var ms) || ms is < 1 or > PreviewTiming.MaxDurationMs)
            { StatusText.Text = UiText.T("Duración entre 1 y 60000 ms, o vacía para usar FPS."); return; }
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
        StatusText.Text = UiText.T("Tiempo de previsualización actualizado");
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
        { VisibleBoundsInfo.Text = UiText.T("Área visible: valida la hoja"); return; }
        if (!_validationResult.Bounds.TryGetValue(cell, out var b))
        { VisibleBoundsInfo.Text = UiText.T("Área visible: celda vacía"); return; }
        VisibleBoundsInfo.Text = (UiText.IsEnglish ? $"Visible area: {b.Width} × {b.Height} px · ({b.X}, {b.Y})\nMargins L/T/R/B: {b.X}/{b.Y}/{_grid.Cell.Width - b.X - b.Width}/{_grid.Cell.Height - b.Y - b.Height} px" : $"Área visible: {b.Width} × {b.Height} px · ({b.X}, {b.Y})\nMárgenes I/S/D/B: {b.X}/{b.Y}/{_grid.Cell.Width - b.X - b.Width}/{_grid.Cell.Height - b.Y - b.Height} px");
    }

    private void ClearDiagnostics()
    {
        _validationResult = null;
        _validationReport = null;
        ExportReportButton.IsEnabled = false;
        VisibleBoundsInfo.Text = UiText.T("Área visible: valida la hoja");
        _allFindings = [];
        _diagnosticSummary = UiText.T("Validación no ejecutada.");
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
        DiagnosticText.Text = _diagnosticSummary + (UiText.IsEnglish ? $" · {visibleCount} visible findings" : $" · {visibleCount} avisos visibles");
    }

    private async void OnValidate(object? sender, RoutedEventArgs e)
    {
        if (_importing || _bitmap is null || _grid is null || _active < 0) return;
        var grid = _grid;
        SetBusy(true);
        ClearDiagnostics();
        DiagnosticText.Text = UiText.T("Analizando hoja y animaciones...");
        try
        {
            SheetValidation.CheckGridLimit(grid);
            var sheetId = _sheets[_active].Id;
            var clips = _clips.Where(c => c.SheetId == sheetId)
                .Select(c => new ValidationClip(c.Name, c.EffectiveTracks.Select(t =>
                    new ValidationTrack(t.Direction, t.Cells.ToArray(), t.Fps, t.DurationsMs?.ToArray())).ToArray())).ToArray();
            var tracks = clips.SelectMany(c => c.Tracks.Select(t => new AlignmentTrack(c.Name,
                t.Direction == "none" ? UiText.T("Sin dirección") : t.Direction, t.Cells))).ToArray();
            var pixels = ValidationPixels.Read(_bitmap);
            var analysis = await Task.Run(() =>
            {
                var sheet = SheetValidation.Analyze(grid, pixels, _lifetime.Token, UiText.IsEnglish);
                var baseFindings = sheet.Findings.Where(f => f.Code != "LIMIT").ToArray();
                var alignment = AlignmentValidation.Analyze(grid, sheet.Bounds, tracks,
                    SheetValidation.FindingLimit - baseFindings.Length, _lifetime.Token, UiText.IsEnglish);
                var list = baseFindings.Concat(alignment.Findings).ToList();
                var clipResult = ClipValidation.Analyze(grid, sheet.Bounds, clips,
                    SheetValidation.FindingLimit - list.Count, _lifetime.Token, UiText.IsEnglish);
                list.AddRange(clipResult.Findings);
                var omitted = sheet.OmittedFindings + alignment.OmittedFindings + clipResult.OmittedFindings;
                if (omitted > 0) list.Add(new SheetFinding("LIMIT", null, (UiText.IsEnglish ? $"{omitted} findings omitted; counts are complete." : $"Se omiten {omitted} avisos; recuento completo.")) { English = UiText.IsEnglish });
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
            _diagnosticSummary = (UiText.IsEnglish ? $"{result.TotalCells} cells · {result.EmptyCells} empty · {result.EdgeCells} edge · {result.DuplicateCells} duplicates · {analysis.Alignment.TotalFindings} size/alignment findings · {analysis.Clips.TotalFindings} animation findings" : $"{result.TotalCells} celdas · {result.EmptyCells} vacías · {result.EdgeCells} con borde · {result.DuplicateCells} duplicadas · {analysis.Alignment.TotalFindings} avisos de tamaño/alineación · {analysis.Clips.TotalFindings} de animaciones") +
                (analysis.Omitted > 0 ? (UiText.IsEnglish ? $" · {analysis.Omitted} unlisted findings" : $" · {analysis.Omitted} avisos no listados") : "");
            RefreshDiagnosticFilter();
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                ClearDiagnostics();
                DiagnosticText.Text = (UiText.IsEnglish ? $"Validation incomplete: {ex.Message}" : $"Validación incompleta: {ex.Message}");
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
            var json = new FilePickerFileType(UiText.T("Informe JSON")) { Patterns = ["*.json"] };
            var txt = new FilePickerFileType(UiText.T("Informe de texto")) { Patterns = ["*.txt"] };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = UiText.T("Exportar informe de validación"),
                SuggestedFileName = "validacion",
                DefaultExtension = "json",
                FileTypeChoices = [json, txt],
                ShowOverwritePrompt = true
            });
            if (file is null || _lifetime.IsCancellationRequested) return;
            var extension = System.IO.Path.GetExtension(file.Name);
            string content;
            if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase)) content = report.ToJson();
            else if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)) content = report.ToPlainText(UiText.IsEnglish);
            else { StatusText.Text = UiText.T("Selecciona una extensión .json o .txt."); return; }
            // Never allow a report to overwrite the project's persistence manifest.
            if (string.Equals(file.Name, ProjectStore.ManifestName, StringComparison.OrdinalIgnoreCase))
            { StatusText.Text = UiText.T("No se puede sobrescribir el manifiesto del proyecto."); return; }
            await using var stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes, _lifetime.Token);
            await stream.FlushAsync(_lifetime.Token);
            StatusText.Text = (UiText.IsEnglish ? $"Report exported · {file.Name}" : $"Informe exportado · {file.Name}");
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Report export failed: {ex.Message}" : $"No se pudo exportar el informe: {ex.Message}");
        }
        finally { if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private void OnFindingSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_importing || FindingList.SelectedItem is not SheetFinding { Cell: int cell }) return;
        OnManualCell(cell);
        StatusText.Text = (UiText.IsEnglish ? $"Reviewing cell {cell + 1} · Finding requires manual review" : $"Revisando celda {cell + 1} · El aviso requiere revisión manual");
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
            StatusText.Text = UiText.T("Recurso seleccionado");
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                RefreshResources();
                StatusText.Text = (UiText.IsEnglish ? $"Resource open failed: {ex.Message}" : $"No se pudo abrir el recurso: {ex.Message}");
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
        var parent = new TextBox { Text = _folder is null ? "" : Path.GetDirectoryName(_folder), IsReadOnly = true, Watermark = UiText.T("Selecciona la ubicación") };
        var preview = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var browse = new Button { Content = UiText.T("Elegir carpeta…") };
        var save = new Button { Classes = { "primary" }, Content = UiText.T("Guardar") };
        var cancel = new Button { Content = UiText.T("Cancelar") };
        var dialog = new SuiteDialog { Title = UiText.T("Guardar proyecto"), Width = 520, Height = 320, CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner };
        void Refresh()
        {
            try
            {
                var folder = ProjectDestination.Resolve(parent.Text ?? "", name.Text ?? "");
                preview.Text = UiText.T("Carpeta del proyecto: ") + folder;
                save.IsEnabled = true;
            }
            catch (ArgumentException ex) { preview.Text = ex.Message; save.IsEnabled = false; }
        }
        name.TextChanged += (_, _) => Refresh();
        browse.Click += async (_, _) =>
        {
            var selected = await dialog.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
                { Title = UiText.T("Selecciona la ubicación del proyecto"), AllowMultiple = false });
            if (selected.Count > 0 && selected[0].TryGetLocalPath() is string path)
            { parent.Text = path; Refresh(); }
        };
        save.Click += (_, _) => dialog.Close((ProjectDestination.Resolve(parent.Text ?? "", name.Text ?? ""), name.Text!.Trim()));
        cancel.Click += (_, _) => dialog.Close(null);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 10,
            Children = { new TextBlock { Text = UiText.T("Nombre del proyecto") }, name,
                new TextBlock { Text = UiText.T("Ubicación (carpeta contenedora)") }, parent, browse, preview,
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8, Children = { cancel, save } } } };
        Refresh();
        return await dialog.ShowDialog<(string Folder, string Name)?>(this);
    }

    private async Task<bool> ConfirmOverwrite(string folder)
    {
        var dialog = new SuiteDialog { Title = UiText.T("Confirmar sobrescritura"), Width = 520, Height = 230,
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var overwrite = new Button { Classes = { "danger" }, Content = UiText.T("Sobrescribir proyecto") };
        var cancel = new Button { Content = UiText.T("Cancelar") };
        overwrite.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 14,
            Children = { new TextBlock { Text = UiText.T("Ya existe un proyecto en esta ubicación:"), FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = folder, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = UiText.T("Se reemplazará el manifiesto y se conservará su copia .bak. No se eliminarán los PNG anteriores ni otros archivos."), TextWrapping = TextWrapping.Wrap },
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
            var dialog = new SuiteDialog { Title = UiText.T("Renombrar proyecto"), Width = 460, Height = 210, CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var ok = new Button { Classes = { "primary" }, Content = UiText.T("Renombrar") };
            var cancel = new Button { Content = UiText.T("Cancelar") };
            var hint = new TextBlock { Text = UiText.T("Cambia el nombre visible; la carpeta actual no se mueve."), TextWrapping = TextWrapping.Wrap };
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
            StatusText.Text = UiText.T("Proyecto renombrado · Guarda para confirmar");
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
                    throw new IOException(UiText.T("La carpeta contiene archivos pero no es un proyecto SpriteLab. Elige otra ubicación o nombre."));
            }
            var project = new ViewerProject(id, name, _sheets.ToArray(), _clips.ToArray());
            SaveIndicator.Text = UiText.T("Guardando…");
            SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#AEBCCA"));
            var hash = await Task.Run(() => ViewerWorkspace.SaveAsync(folder, project, revision, _lifetime.Token));
            if (_lifetime.IsCancellationRequested) return false;
            _folder = folder;
            _projectId = id;
            _projectName = name;
            _manifestHash = hash;
            _dirty = false;
            ProjectText.Text = name;
            SaveIndicator.Text = (UiText.IsEnglish ? $"✓ Saved at {DateTime.Now:HH:mm:ss}" : $"✓ Guardado a las {DateTime.Now:HH:mm:ss}");
            SaveIndicator.Foreground = new SolidColorBrush(Color.Parse("#79C9A0"));
            StatusText.Text = (UiText.IsEnglish ? $"Project saved · {folder}" : $"Proyecto guardado · {folder}");
            return true;
        }
        catch (Exception ex)
        {
            if (!_lifetime.IsCancellationRequested)
            {
                StatusText.Text = (UiText.IsEnglish ? $"Save failed: {ex.Message}" : $"No se pudo guardar: {ex.Message}");
                SaveIndicator.Text = UiText.T("Error al guardar");
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
            ClearEditorHistory();
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
            SaveIndicator.Text = UiText.T("✓ Proyecto abierto");
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
                ResourceText.Text = UiText.T("Todavía no hay recursos.");
                ResolutionText.Text = UiText.T("Resolución: —");
                ViewerInfo.Text = UiText.T("Sin imagen");
            }
            RefreshResources();
            StatusText.Text = UiText.T("Proyecto abierto");
        }
        catch (Exception ex) { if (!_lifetime.IsCancellationRequested) StatusText.Text = (UiText.IsEnglish ? $"Open failed: {ex.Message}" : $"No se pudo abrir: {ex.Message}"); }
        finally { first?.Dispose(); if (!_lifetime.IsCancellationRequested) SetBusy(false); }
    }

    private async Task<bool> ConfirmDiscard()
    {
        var dialog = new SuiteDialog { Title = UiText.T("Cambios sin guardar"), Width = 480, Height = 210,
            Background = new SolidColorBrush(Color.Parse("#202630")),
            CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var save = new Button { Classes = { "primary" }, Content = UiText.T("Guardar") };
        var discard = new Button { Classes = { "danger" }, Content = UiText.T("No guardar") };
        var cancel = new Button { Content = UiText.T("Cancelar") };
        save.Click += (_, _) => dialog.Close(1);
        discard.Click += (_, _) => dialog.Close(2);
        cancel.Click += (_, _) => dialog.Close(0);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8,
            Children = { cancel, discard, save } };
        dialog.Content = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 16,
            Children = { new TextBlock { Text = UiText.T("¿Quieres guardar los cambios del proyecto?"),
                FontSize = 16, TextWrapping = TextWrapping.Wrap },
                new TextBlock { Text = UiText.T("Si continúas sin guardar, perderás los cambios pendientes."),
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

    private async void OnShortcuts(object? sender,RoutedEventArgs e)
    {
        var dialog=new SuiteDialog{Title=UiText.T("Atajos de teclado"),Width=560,Height=440,CanResize=true};
        var rows=new StackPanel{Spacing=8,Margin=new Avalonia.Thickness(20)};
        foreach(var (key,label) in new (string,string)[]{
            ("Ctrl+I","Importar PNG"),("Ctrl+O","Abrir proyecto"),("Ctrl+S","Guardar"),("Ctrl+Shift+S","Guardar como"),
            ("Ctrl+E","Exportar sprites y animaciones"),("F8","Validar hoja"),("Ctrl+Z","Deshacer"),("Ctrl+Y","Rehacer"),
            ("Space","Reproducir / pausar"),("← / →","Fotograma anterior / siguiente"),("Tab / Shift+Tab","Navegar por los controles"),("Esc","Cerrar diálogo")})
        {
            var row=new Grid{ColumnDefinitions=new ColumnDefinitions("150,*"),ColumnSpacing=12};
            row.Children.Add(new TextBlock{Text=key,Foreground=UiPalette.Accent,FontWeight=FontWeight.SemiBold});
            var text=new TextBlock{Text=UiText.T(label),TextWrapping=TextWrapping.Wrap};Grid.SetColumn(text,1);row.Children.Add(text);rows.Children.Add(row);
        }
        var close=new Button{Content=UiText.T("Cerrar"),Classes={"primary"},HorizontalAlignment=HorizontalAlignment.Right};
        close.Click+=(_,_)=>dialog.Close();rows.Children.Add(close);dialog.Content=new ScrollViewer{Content=rows};
        await dialog.ShowDialog(this);
    }

    private async void OnUpdates(object? sender,RoutedEventArgs e)
    {
        if(_importing)return;
        SetBusy(true);
        try
        {
            var update=await UpdateWindow.ShowAsync(this,_lifetime.Token);
            if(update is null||_lifetime.IsCancellationRequested)return;
            if(!UpdateWindow.IsInstalled)throw new InvalidOperationException("Portable installation.");
            if(_dirty&&!await ConfirmDiscard())return;
            await UpdateWindow.LaunchTrackedAsync(update,_lifetime.Token);
            _allowClose=true;
            Close();
        }
        catch(OperationCanceledException){}
        catch(Exception ex){if(!_lifetime.IsCancellationRequested)StatusText.Text=UiText.T("No se pudo iniciar la actualización: ")+ex.Message;}
        finally{if(!_lifetime.IsCancellationRequested)SetBusy(false);}
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var dialog = new SuiteDialog {
            Title = UiText.T("Acerca de SpriteLab"),
            Width = 420,
            Height = 220,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var close = new Button { Content = UiText.T("Cerrar"), HorizontalAlignment = HorizontalAlignment.Right };
        close.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(24),
            Spacing = 14,
            Children =
            {
                new TextBlock { Text = $"SpriteLab {GetVersion()}", FontSize = 22 },
                new TextBlock { Text = UiText.T("Herramienta independiente para sprites 2D."), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBlock { Text = UiText.T("Editor de sprites, animaciones, procesamiento y exportación PNG, JSON y GIF."), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                close
            }
        };
        await dialog.ShowDialog(this);
    }
}
