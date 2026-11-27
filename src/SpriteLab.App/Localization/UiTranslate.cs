using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Automation;

namespace SpriteLab.App.Views;
internal static class UiTranslate
{
    public static void StaticControls(Control root)
    {
        if(!UiText.IsEnglish)return;
        foreach(var node in root.GetLogicalDescendants().OfType<Control>().ToArray())
        {
            if(ToolTip.GetTip(node) is string tip) ToolTip.SetTip(node,UiText.T(tip));
            var name=AutomationProperties.GetName(node);
            if(!string.IsNullOrEmpty(name)) AutomationProperties.SetName(node,UiText.T(name));
            switch(node)
            {
                case MenuItem menu when menu.Header is string h:menu.Header=UiText.T(h);break;
                case TabItem tab when tab.Header is string h:tab.Header=UiText.T(h);break;
                case ContentControl content when content.Content is string s:content.Content=UiText.T(s);break;
                case TextBlock text when text.Text is string s:text.Text=UiText.T(s);break;
                case TextBox input when input.Watermark is string s:input.Watermark=UiText.T(s);break;
            }
        }
    }
}
