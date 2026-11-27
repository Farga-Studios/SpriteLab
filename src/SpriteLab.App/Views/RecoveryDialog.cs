using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Suite.Updater;

namespace SpriteLab.App.Views;

internal enum RecoveryAction { Later, CancelPrepared, ConfirmUpdate, ConfirmRollback, Rollback }

internal static class RecoveryDialog
{
    private static string T(string es, string en) => UiText.IsEnglish ? en : es;

    public static Task<RecoveryAction> ShowAsync(Window owner, RecoveryAssessment assessment)
    {
        var dialog = new SuiteDialog { Title = T("Recuperación de actualizaciones", "Update recovery"),
            Width = 580, Height = 420, CanResize = true };
        var panel = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 12 };
        var buttons = new WrapPanel();
        var later = new Button { Content = T("Más tarde", "Later") };
        later.Click += (_, _) => dialog.Close(RecoveryAction.Later);
        buttons.Children.Add(later);
        void Add(string es, string en, RecoveryAction action, CheckBox? condition = null)
        {
            var button = new Button { Content = T(es, en), IsEnabled = condition is null };
            if (condition is not null)
                condition.PropertyChanged += (_, _) => button.IsEnabled = condition.IsChecked == true;
            button.Click += (_, _) => dialog.Close(action);
            buttons.Children.Add(button);
        }
        void Explain(string es, string en) => panel.Children.Add(new TextBlock { Text = T(es, en), TextWrapping = TextWrapping.Wrap });
        CheckBox Attest(string es, string en)
        {
            var check = new CheckBox { Content = new TextBlock { Text = T(es, en), TextWrapping = TextWrapping.Wrap } };
            panel.Children.Add(check);
            return check;
        }
        if (assessment.Transaction is { } t)
        {
            Explain($"Versión anterior: {t.PreviousVersion} · Nueva: {t.TargetVersion}", $"Previous: {t.PreviousVersion} · Target: {t.TargetVersion}");
            if (assessment.Status == RecoveryStatus.Interrupted && t.Phase == UpdatePhase.Prepared)
                Add("Cancelar actualización preparada", "Cancel prepared update", RecoveryAction.CancelPrepared);
            else if (assessment.Status is RecoveryStatus.HealthCheckRequired or RecoveryStatus.RollbackHealthCheckRequired)
            {
                Explain("Prueba la apertura, edición y exportación de un proyecto de ejemplo antes de confirmar.", "Test opening, editing and exporting a sample project before confirming.");
                var check = Attest("El instalador ha terminado y las pruebas funcionan.", "The installer has finished and the tests pass.");
                Add("Confirmar funcionamiento", "Confirm working version",
                    assessment.Status == RecoveryStatus.HealthCheckRequired ? RecoveryAction.ConfirmUpdate : RecoveryAction.ConfirmRollback, check);
            }
            if (assessment.Status is RecoveryStatus.HealthCheckRequired or RecoveryStatus.RecoveryRequired or RecoveryStatus.RollbackInterrupted)
            {
                Explain("Solo se restaurará un instalador anterior firmado y conservado. Si no existe, será necesaria una recuperación manual.", "Only a retained signed previous installer can be restored. If unavailable, manual recovery is required.");
                var check = Attest("El instalador ha terminado; he respaldado mis proyectos y comprobado su compatibilidad con la versión anterior.", "The installer has finished; I backed up my projects and checked compatibility with the previous version.");
                Add("Restaurar versión anterior", "Restore previous version", RecoveryAction.Rollback, check);
            }
        }
        Explain("Si no puedes confirmar el estado o no hay un paquete verificable, cierra el instalador y consulta la documentación de recuperación. No borres el registro.", "If you cannot confirm the state or no verified package exists, close the installer and consult the recovery documentation. Do not delete the journal.");
        panel.Children.Add(buttons);
        dialog.Content = new ScrollViewer { Content = panel };
        return dialog.ShowDialog<RecoveryAction>(owner);
    }
}
