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
    private static UpdateClient Client()=>new(Http,new Uri("https://raw.githubusercontent.com/Farga-Studios/SpriteLab/main/channels/stable/update.json"),PublicKey,"SpriteLab","stable");
    private static Version Installed
    {
        get{var v=typeof(UpdateWindow).Assembly.GetName().Version??new Version(0,0,0);return new Version(v.Major,v.Minor,Math.Max(0,v.Build));}
    }
    public static async Task<UpdateManifest?> CheckAsync(CancellationToken token)=>await Client().CheckAsync(Installed,token);
    public static string StagingRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SpriteLab","Updates");
    public static RecoveryAssessment AssessStartup()
    {
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        return service.Assess(Installed);
    }

    public static void CancelPrepared(Guid transactionId)
    {
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        service.CancelPrepared(transactionId, Installed);
    }

    public static void ConfirmRollbackHealth(Guid transactionId)
    {
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        service.ConfirmRollbackHealthy(transactionId, Installed);
    }

    public static async Task RollbackAsync(Guid transactionId, bool dataCompatible, CancellationToken token)
    {
        if (!IsInstalled) throw new InvalidOperationException("Portable installation.");
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        await service.RollbackAsync(transactionId, Installed, StagingRoot, dataCompatible, token);
    }

    public static void ConfirmStartupHealth(Guid transactionId)
    {
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        service.ConfirmHealthy(transactionId, Installed);
    }

    public static async Task LaunchTrackedAsync(PreparedUpdate update, CancellationToken token)
    {
        if (!IsInstalled) throw new InvalidOperationException("Portable installation.");
        using var service = new SpriteLab.App.Updates.UpdateRecoveryService(StagingRoot, PublicKey);
        var assessment = service.Assess(Installed);
        if (assessment.Status is not (RecoveryStatus.NoTransaction or RecoveryStatus.Healthy or RecoveryStatus.Aborted or RecoveryStatus.RolledBack))
            throw new InvalidOperationException("An unresolved update requires recovery.");
        await service.RetainAsync(Client(), update, token);
        token.ThrowIfCancellationRequested();
        var transaction = service.Begin(Installed, update);
        service.MarkHandoff(transaction.Id);
        await InstallerHandoff.LaunchAsync(update, StagingRoot, token, transactionId: transaction.Id);
    }

    public static bool IsInstalled=>InstallerHandoff.IsInstalled(AppContext.BaseDirectory,"{B637E3EE-3A57-474F-9F87-C70E3F12AE3B}");
    public static async Task<PreparedUpdate?> ShowAsync(Window owner,CancellationToken token,UpdateManifest? known=null)
    {
        using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(token);
        var dialog=new SuiteDialog{Title=UiText.T("Actualizaciones"),Width=620,Height=430,CanResize=true};
        var status=new TextBlock{Text=UiText.T("Buscando actualizaciones…"),TextWrapping=TextWrapping.Wrap};
        var notes=new TextBlock{TextWrapping=TextWrapping.Wrap};
        var progress=new ProgressBar{Minimum=0,Maximum=100,Height=10};
        var download=new Button{Content=UiText.T("Descargar instalador"),Classes={"primary"},IsEnabled=false};
        var close=new Button{Content=UiText.T("Cerrar")};
        var install=new Button{Content=UiText.T("Instalar actualización"),IsEnabled=false,Classes={"primary"}};
        PreparedUpdate? prepared=null;
        install.Click+=(_,_)=>{if(prepared is not null)dialog.Close(prepared);};
        var client=Client();UpdateManifest? manifest=known;bool closed=false;
        close.Click+=(_,_)=>dialog.Close();
        dialog.Closed+=(_,_)=>{closed=true;lifetime.Cancel();};
        dialog.Opened+=async(_,_)=>{
            try{
                if(!client.Configured){status.Text=UiText.T("Actualizador pendiente de configurar la clave pública.");return;}
                manifest??=await client.CheckAsync(Installed,lifetime.Token);
                if(closed)return;
                if(manifest is null){status.Text=UiText.T("No hay actualizaciones disponibles.");return;}
                status.Text=UiText.T("Nueva versión: ")+manifest.Version;notes.Text=manifest.Notes;download.IsEnabled=IsInstalled;
                if(!IsInstalled)status.Text+=" · "+UiText.T("Versión portátil: descarga el ZIP de la nueva versión desde el repositorio oficial. No se instalará automáticamente.");
            }catch(OperationCanceledException){}catch(Exception ex){if(!closed)status.Text=UiText.T("No se pudo comprobar la actualización: ")+ex.Message;}
        };
        download.Click+=async(_,_)=>{
            if(manifest is null)return;download.IsEnabled=false;install.IsEnabled=false;prepared=null;
            try{
                status.Text=UiText.T("Descargando actualización…");
                var path=await client.DownloadAsync(manifest,StagingRoot,new Progress<int>(v=>progress.Value=v),lifetime.Token);
                if(closed)return;
                prepared=new PreparedUpdate(manifest,path);
                install.IsEnabled=IsInstalled;
                status.Text=IsInstalled?UiText.T("Instalador verificado. Instalar cerrará SpriteLab después de confirmar los cambios pendientes."):
                    UiText.T("Versión portátil: descarga el ZIP de la nueva versión desde el repositorio oficial. No se instalará automáticamente.");
            }catch(OperationCanceledException){}catch(Exception ex){if(!closed)status.Text=UiText.T("No se pudo descargar la actualización: ")+ex.Message;}
            finally{if(!closed)download.IsEnabled=true;}
        };
        var body=new StackPanel{Spacing=12,Margin=new Avalonia.Thickness(20),Children={status,notes,progress,new WrapPanel{Orientation=Orientation.Horizontal,Children={download,install,close}}}};
        dialog.Content=new ScrollViewer{Content=body};
        return await dialog.ShowDialog<PreparedUpdate?>(owner);
    }
}
