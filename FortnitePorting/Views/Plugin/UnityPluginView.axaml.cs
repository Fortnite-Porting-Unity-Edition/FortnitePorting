using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Plugin;

namespace FortnitePorting.Views.Plugin;

public partial class UnityPluginView : ViewBase<UnityPluginViewModel>
{
    public UnityPluginView() : base(AppSettings.Current.Plugin.Unity)
    {
        InitializeComponent();
    }
}