using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SpriteLab.Core;
using SpriteLab.Core.Export;
using SpriteLab.Core.Processing;
using SpriteLab.Infrastructure;

namespace SpriteLab.App.Views;
internal sealed record ExportChoice(ExportProfile Profile,Guid? Clip,string? Direction);
internal static class ExportOptionsWindow
{
    public static async Task<ExportChoice?> ShowAsync(Window owner,ViewerSheet sheet,CellGrid grid,byte[] rgba,int selected,
        IReadOnlyList<ViewerClip> clips,CancellationToken token)
    {
        var local=clips.Where(c=>c.SheetId==sheet.Id).ToArray();
        var profiles=Array.Empty<ExportProfile>();
        string initial=UiText.T("Configura la exportación y revisa la vista previa.");
        try{profiles=await ExportProfiles.LoadAsync(ExportProfiles.DefaultPath,token);}
        catch(Exception ex) when(ex is not OperationCanceledException){initial=UiText.T("No se pudieron cargar perfiles: ")+ex.Message;}
        var window=new SuiteDialog {Title=UiText.T("Exportar sprites"),Width=850,Height=720,MinWidth=650,MinHeight=550,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var kind=new ComboBox{ItemsSource=new[]{UiText.T("Fotograma + JSON"),UiText.T("Sprites individuales + JSON"),UiText.T("Hoja + JSON"),UiText.T("Hoja y animaciones + JSON"),UiText.T("GIF animado")},SelectedIndex=0};
        var batch=new CheckBox{Content=UiText.T("Todas las hojas con cuadrícula (lote)")};
        var profileList=new ComboBox{ItemsSource=profiles.Select(p=>p.Name).ToArray(),PlaceholderText=UiText.T("Perfil guardado")};
        var profileName=new TextBox{Watermark=UiText.T("Nombre del perfil"),MaxLength=80};
        var save=new Button{Content=UiText.T("Guardar perfil")};var remove=new Button{Content=UiText.T("Eliminar perfil")};
        var clip=new ComboBox{ItemsSource=local.Select(c=>c.Name).ToArray(),SelectedIndex=local.Length>0?0:-1};
        var direction=new ComboBox{SelectedIndex=0};
        var scale=new ComboBox{ItemsSource=new[]{"×1","×2","×3","×4","×8"},SelectedIndex=0};
        var speed=new TextBox{Text="100",Watermark=UiText.T("Velocidad %")};
        var repeats=new TextBox{Text="0",Watermark=UiText.T("Repeticiones")};
        var transparent=new CheckBox{Content=UiText.T("Fondo transparente"),IsChecked=true};
        var bg=new TextBox{Text="202630",Watermark=UiText.T("RGB hexadecimal")};
        var status=new TextBlock{Text=initial,TextWrapping=TextWrapping.Wrap};
        var preview=new Image{Stretch=Stretch.Uniform,MaxHeight=260};
        RenderOptions.SetBitmapInterpolationMode(preview,BitmapInterpolationMode.None);
        var previewButton=new Button{Content=UiText.T("Vista previa / reproducir")};
        var ok=new Button{Content=UiText.T("Continuar")};var cancel=new Button{Content=UiText.T("Cancelar")};
        var timer=new DispatcherTimer();Bitmap? shown=null;int frame=0;ViewerTrack? playing=null;bool closed=false;
        var offsets=(sheet.Offsets??[]).ToDictionary(o=>o.Cell);
        var scales=new[]{1,2,3,4,8};
        ExportProfile Read()
        {
            if(kind.SelectedIndex>=0&&kind.SelectedIndex!=4)return new ExportProfile(string.IsNullOrWhiteSpace(profileName.Text)?"Temporal":profileName.Text.Trim(),(SpriteExportKind)kind.SelectedIndex,new GifOptions(),batch.IsChecked==true);
            if(kind.SelectedIndex<0||scale.SelectedIndex<0||!int.TryParse(speed.Text,out var s)||
                !int.TryParse(repeats.Text,out var r)||!int.TryParse(bg.Text,System.Globalization.NumberStyles.HexNumber,null,out var b))
                throw new ArgumentException(UiText.T("Opciones GIF inválidas."));
            var p=new ExportProfile(string.IsNullOrWhiteSpace(profileName.Text)?"Temporal":profileName.Text.Trim(),
                (SpriteExportKind)kind.SelectedIndex,new GifOptions(scales[scale.SelectedIndex],s,r,transparent.IsChecked==true,b),batch.IsChecked==true);
            p.Validate();return p;
        }
        void Stop(){timer.Stop();playing=null;preview.Source=null;shown?.Dispose();shown=null;}
        void Directions()
        {
            direction.ItemsSource=clip.SelectedIndex>=0?new[]{UiText.T("Todas las direcciones")}.Concat(local[clip.SelectedIndex].EffectiveTracks.Select(t=>t.Direction)).ToArray():new[]{UiText.T("Todas las direcciones")};
            direction.SelectedIndex=0;
        }
        void Refresh()
        {
            Stop();bool gif=kind.SelectedIndex==4;bool single=batch.IsChecked!=true;
            clip.IsEnabled=direction.IsEnabled=gif&&single;scale.IsEnabled=speed.IsEnabled=repeats.IsEnabled=transparent.IsEnabled=gif;
            bg.IsEnabled=gif&&transparent.IsChecked!=true;
        }
        void ShowRaster(RasterFrame raster)
        {
            token.ThrowIfCancellationRequested();
            var next=new WriteableBitmap(new Avalonia.PixelSize(raster.Width,raster.Height),new Vector(96,96),Avalonia.Platform.PixelFormat.Rgba8888,Avalonia.Platform.AlphaFormat.Unpremul);
            try
            {
                using var buffer=next.Lock();var stride=checked(raster.Width*4);
                for(int y=0;y<raster.Height;y++)
                    System.Runtime.InteropServices.Marshal.Copy(raster.Rgba,y*stride,IntPtr.Add(buffer.Address,y*buffer.RowBytes),stride);
            }
            catch{next.Dispose();throw;}
            preview.Source=next;shown?.Dispose();shown=next;
        }
        void Tick()
        {
            if(playing is null)return;
            try
            {
                var p=Read();var c=playing.Cells[frame];
                ShowRaster(FrameRaster.Render(grid,rgba,c,offsets.GetValueOrDefault(c)??FrameOffset.Zero(c),token));
                timer.Interval=TimeSpan.FromMilliseconds(Math.Max(10,playing.DurationAt(frame)*100/p.Gif.SpeedPercent));
                frame=(frame+1)%playing.Cells.Length;
            }catch(Exception ex){Stop();status.Text=ex.Message;}
        }
        timer.Tick+=(_,_)=>Tick();
        clip.SelectionChanged+=(_,_)=>{Directions();Refresh();};direction.SelectionChanged+=(_,_)=>Stop();
        kind.SelectionChanged+=(_,_)=>Refresh();batch.IsCheckedChanged+=(_,_)=>Refresh();transparent.IsCheckedChanged+=(_,_)=>Refresh();
        Directions();Refresh();
        profileList.SelectionChanged+=(_,_)=>{
            if(profileList.SelectedIndex<0)return;var p=profiles[profileList.SelectedIndex];profileName.Text=p.Name;kind.SelectedIndex=(int)p.Kind;
            batch.IsChecked=p.AllSheets;scale.SelectedIndex=Array.IndexOf(scales,p.Gif.Scale);speed.Text=p.Gif.SpeedPercent.ToString();
            repeats.Text=p.Gif.Repetitions.ToString();transparent.IsChecked=p.Gif.Transparent;bg.Text=p.Gif.BackgroundRgb.ToString("X6");Refresh();
        };
        save.Click+=async(_,_)=>{save.IsEnabled=remove.IsEnabled=false;try{var p=Read();if(string.IsNullOrWhiteSpace(profileName.Text))throw new ArgumentException(UiText.T("Escribe un nombre."));
            var next=profiles.Where(x=>!string.Equals(x.Name,p.Name,StringComparison.OrdinalIgnoreCase)).Append(p).ToArray();
            await ExportProfiles.SaveAsync(ExportProfiles.DefaultPath,next,token);if(closed)return;profiles=next;profileList.ItemsSource=profiles.Select(x=>x.Name).ToArray();status.Text=UiText.T("Perfil guardado.");
        }catch(Exception ex){if(!closed)status.Text=ex.Message;}finally{if(!closed)save.IsEnabled=remove.IsEnabled=true;}};
        remove.Click+=async(_,_)=>{if(profileList.SelectedIndex<0)return;save.IsEnabled=remove.IsEnabled=false;try{var next=profiles.Where((_,i)=>i!=profileList.SelectedIndex).ToArray();
            await ExportProfiles.SaveAsync(ExportProfiles.DefaultPath,next,token);if(closed)return;profiles=next;profileList.ItemsSource=profiles.Select(x=>x.Name).ToArray();status.Text=UiText.T("Perfil eliminado.");
        }catch(Exception ex){if(!closed)status.Text=ex.Message;}finally{if(!closed)save.IsEnabled=remove.IsEnabled=true;}};
        previewButton.Click+=async(_,_)=>{
            Stop();previewButton.IsEnabled=ok.IsEnabled=false;
            try{
                var p=Read();
                if(p.Kind==SpriteExportKind.Gif){if(clip.SelectedIndex<0)throw new ArgumentException(UiText.T("La hoja activa no tiene clips."));
                    var tracks=local[clip.SelectedIndex].EffectiveTracks;playing=tracks[Math.Max(0,direction.SelectedIndex-1)];frame=0;Tick();if(playing is not null)timer.Start();
                    status.Text=UiText.T("Vista previa de la dirección seleccionada (RGBA; GIF cuantiza color y transparencia).");
                }else{
                    var raster=await Task.Run(()=>p.Kind==SpriteExportKind.SelectedFrame?FrameRaster.Render(grid,rgba,selected,offsets.GetValueOrDefault(selected)??FrameOffset.Zero(selected),token):
                        FrameRaster.RenderSheet(grid,rgba,sheet.Offsets??[],token),token);
                    if(closed)return;ShowRaster(raster);status.Text=(UiText.IsEnglish ? $"Preview · {raster.ClippedVisiblePixels} visible pixels clipped." : $"Vista previa · {raster.ClippedVisiblePixels} píxeles visibles recortados.");
                }
            }catch(Exception ex){if(!closed)status.Text=ex.Message;}finally{if(!closed)previewButton.IsEnabled=ok.IsEnabled=true;}
        };
        ok.Click+=(_,_)=>{try{var p=Read();p.Validate();Guid? id=null;string? dir=null;
            if(p.Kind==SpriteExportKind.Gif&&!p.AllSheets){if(clip.SelectedIndex<0)throw new ArgumentException(UiText.T("Selecciona clip."));id=local[clip.SelectedIndex].Id;
                if(direction.SelectedIndex>0)dir=local[clip.SelectedIndex].EffectiveTracks[direction.SelectedIndex-1].Direction;}
            window.Close(new ExportChoice(p,id,dir));
        }catch(Exception ex){status.Text=ex.Message;}};
        cancel.Click+=(_,_)=>window.Close(null);
        window.Closed+=(_,_)=>{closed=true;Stop();};
        StackPanel Row(params Control[] controls){var row=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8};foreach(var c in controls)row.Children.Add(c);return row;}
        var form=new StackPanel{Margin=new Thickness(16),Spacing=10,Children={
            new TextBlock{Text=sheet.Name,FontWeight=FontWeight.SemiBold},kind,batch,
            new TextBlock{Text=UiText.T("Perfiles")},profileList,profileName,Row(save,remove),
            new TextBlock{Text=UiText.T("GIF: clip / dirección / escala")},clip,direction,scale,
            new TextBlock{Text=UiText.T("Velocidad (%) · Repeticiones (0 = infinito) · Fondo RGB")},Row(speed,repeats,bg),transparent,
            previewButton,preview,status,Row(ok,cancel)}};
        window.Content=new ScrollViewer{Content=form};
        try{return await window.ShowDialog<ExportChoice?>(owner);}finally{closed=true;Stop();}
    }
}
