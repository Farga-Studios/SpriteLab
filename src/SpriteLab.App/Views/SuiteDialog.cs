using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SpriteLab.App.Views;

internal sealed class SuiteDialog : Window
{
    private bool _prepared;
    private void Prepare()
    {
        if(_prepared)return;
        _prepared=true;
        SystemDecorations=SystemDecorations.BorderOnly;
        Background=UiPalette.Window;Foreground=UiPalette.Text;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var body=Content as Control;
        Content=null;
        var close=new Button{Classes={"captionButton","captionClose"}};
        ToolbarIcons.Set(close,"close",UiText.T("Cerrar"));close.MinHeight=36;close.MinWidth=46;close.Click+=(_,_)=>Close();
        var caption=new Border{Background=Brushes.Transparent,Padding=new Thickness(12,0),Child=new TextBlock{
            Text=Title,FontWeight=FontWeight.SemiBold,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis}};
        caption.PointerPressed+=(_,e)=>{if(e.GetCurrentPoint(this).Properties.IsLeftButtonPressed){BeginMoveDrag(e);e.Handled=true;}};
        var header=new Grid{Height=36,Background=UiPalette.Panel,ColumnDefinitions=new ColumnDefinitions("*,Auto")};
        header.Children.Add(caption);Grid.SetColumn(close,1);header.Children.Add(close);
        var root=new Grid{RowDefinitions=new RowDefinitions("Auto,*")};root.Children.Add(header);
        if(body is not null){Grid.SetRow(body,1);root.Children.Add(body);}
        Content=root;
        // Keep old content area sizes while adding the integrated caption.
        if(double.IsFinite(Height))Height+=36;
        if(MinHeight>0)MinHeight+=36;
        KeyDown+=(_,e)=>{if(e.Key==Key.Escape&&!e.Handled){e.Handled=true;Close();}};
    }
    public new Task<T> ShowDialog<T>(Window owner){Prepare();return base.ShowDialog<T>(owner);}
    public new Task ShowDialog(Window owner){Prepare();return base.ShowDialog(owner);}
}
