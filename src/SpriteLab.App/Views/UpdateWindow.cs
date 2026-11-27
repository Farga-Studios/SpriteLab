using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Suite.Updater;

namespace SpriteLab.App.Views;
internal static class UpdateWindow
{
    private static readonly HttpClient Http=new(){Timeout=TimeSpan.FromSeconds(30)};
    // Public signing key embedded in release builds. Empty means fail closed.
    private static string PublicKey
    {
        get
        {
            using var stream=typeof(UpdateWindow).Assembly.GetManifestResourceStream("SpriteLab.UpdatePublicKey.pem");
            if(stream is null)return "";
            using var reader=new StreamReader(stream);return reader.ReadToEnd();
        }
    }
    private static UpdateClient Client()=>new(Http,new Uri("https://raw.githubusercontent.com/Sealsworn/DevelopmentSuite-Downloads/main/channels/spritelab/stable/update.json"),PublicKey,"SpriteLab","stable");
    private static Version Installed
    {
        get{var v=typeof(UpdateWindow).Assembly.GetName().Version??new Version(0,0,0);return new Version(v.Major,v.Minor,Math.Max(0,v.Build));}
    }
    public static async Task<UpdateManifest?> CheckAsync(CancellationToken token)=>await Client().CheckAsync(Installed,token);
    public static async Task ShowAsync(Window owner,CancellationToken token,UpdateManifest? known=null)
    {
        using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(token);
        var dialog=new SuiteDialog{Title=UiText.T("Actualizaciones"),Width=620,Height=430,CanResize=true};
        var status=new TextBlock{Text=UiText.T("Buscando actualizaciones…"),TextWrapping=TextWrapping.Wrap};
        var notes=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var progress=new ProgressBar{Minimum=0,Maximum=100,Height=10};
        var download=new Button{Content=UiText.T("Descargar instalador"),Classes={"primary"},IsEnabled=false};
        var close=new Button{Content=UiText.T("Cerrar")};
        var client=Client();UpdateManifest? manifest=known;bool closed=false;
        close.Click+=(_,_)=>dialog.Close();
        dialog.Closed+=(_,_)=>{closed=true;lifetime.Cancel();};
        dialog.Opened+=async(_,_)=>{
            try{
                if(!client.Configured){status.Text=UiText.T("Actualizador pendiente de configurar la clave pública.");return;}
                manifest??=await client.CheckAsync(Installed,lifetime.Token);
                if(closed)return;
                if(manifest is null){status.Text=UiText.T("No hay actualizaciones disponibles.");return;}
                status.Text=UiText.T("Nueva versión: ")+manifest.Version;notes.Text=manifest.Notes;download.IsEnabled=true;
            }catch(OperationCanceledException){}catch(Exception ex){if(!closed)status.Text=UiText.T("No se pudo comprobar la actualización: ")+ex.Message;}
        };
        download.Click+=async(_,_)=>{
            if(manifest is null)return;download.IsEnabled=false;
            try{
                status.Text=UiText.T("Descargando actualización…");
                var path=await client.DownloadAsync(manifest,Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SpriteLab","Updates"),new Progress<int>(v=>progress.Value=v),lifetime.Token);
                if(closed)return;
                status.Text=UiText.T("Instalador verificado. Guarda tus proyectos, cierra SpriteLab y ejecuta el archivo: ")+path;
                // Manual launch until close/restart and rollback have been validated.
            }catch(OperationCanceledException){}catch(Exception ex){if(!closed)status.Text=UiText.T("No se pudo descargar la actualización: ")+ex.Message;}
            finally{if(!closed)download.IsEnabled=true;}
        };
        var body=new StackPanel{Spacing=12,Margin=new Avalonia.Thickness(20),Children={status,notes,progress,new WrapPanel{Orientation=Orientation.Horizontal,Children={download,close}}}};
        dialog.Content=new ScrollViewer{Content=body};
        await dialog.ShowDialog(owner);
    }
}
