using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Suite.Updater;

namespace SpriteLab.App.Views;

internal enum RecoveryAction { Later, CancelPrepared, ConfirmUpdate, ConfirmRollback, Rollback, ResolveCancelled }

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
            else if (assessment.Status == RecoveryStatus.Interrupted && t.Phase is (UpdatePhase.HandoffStarted or UpdatePhase.AwaitingHealthCheck))
            {
                dialog.Title = T("Actualización interrumpida", "Update interrupted");
                Explain("La última actualización no se ha completado. Si cancelaste antes de que se copiaran archivos, puedes seguir utilizando tu versión actual. Si la instalación llegó a copiar archivos o no estás seguro, consulta la ayuda.", "The last update did not finish. If you cancelled before any files were copied, you can keep using your current version. If files were copied or you are unsure, see the help documentation.");
                var check = Attest("Cancelé antes de copiar archivos, el instalador está cerrado y he probado la versión anterior correctamente.", "I cancelled before file copying, the installer has exited, and I tested the previous version successfully.");
                Add("Continuar con mi versión", "Keep my current version", RecoveryAction.ResolveCancelled, check);
            }
            else if (assessment.Status is RecoveryStatus.HealthCheckRequired or RecoveryStatus.RollbackHealthCheckRequired)
            {
                Explain("Comprueba que puedes abrir, editar y exportar un proyecto. Si todo funciona, confirma para terminar.", "Check that you can open, edit and export a project. If everything works, confirm to finish.");
                var check = Attest("El instalador ha terminado y las pruebas funcionan.", "The installer has finished and the tests pass.");
                Add("Confirmar funcionamiento", "Confirm working version",
                    assessment.Status == RecoveryStatus.HealthCheckRequired ? RecoveryAction.ConfirmUpdate : RecoveryAction.ConfirmRollback, check);
            }
            if (assessment.Status is RecoveryStatus.HealthCheckRequired or RecoveryStatus.RecoveryRequired or RecoveryStatus.RollbackInterrupted)
            {
                Explain("Si la nueva versión da problemas, puedes volver a la anterior cuando haya una copia disponible.", "If the new version has problems, you can return to the previous one when a copy is available.");
                var check = Attest("El instalador ha terminado; he respaldado mis proyectos y comprobado su compatibilidad con la versión anterior.", "The installer has finished; I backed up my projects and checked compatibility with the previous version.");
                Add("Restaurar versión anterior", "Restore previous version", RecoveryAction.Rollback, check);
            }
        }
        Explain("Si no sabes qué opción elegir, pulsa Más tarde y consulta la ayuda.", "If you are unsure which option to choose, select Later and see the help documentation.");
        panel.Children.Add(buttons);
        dialog.Content = new ScrollViewer { Content = panel };
        return dialog.ShowDialog<RecoveryAction>(owner);
    }
}
