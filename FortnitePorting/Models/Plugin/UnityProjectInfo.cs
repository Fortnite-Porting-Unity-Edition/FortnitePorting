using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Utils;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Plugin;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Plugin;

public partial class UnityProjectInfo : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))] private string _projectFilePath;
    [ObservableProperty] private Version? _version;
    [ObservableProperty, JsonIgnore] private Bitmap _image = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/UnityLogo.png");

    public string Name => ProjectFilePath.SubstringAfterLast("/").SubstringBeforeLast(".");

    public string PluginsFolder => Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), "Engine");
    public string FortnitePortingFolder => Path.Combine(PluginsFolder, "FortnitePorting");
    public string UEFormatFolder => Path.Combine(PluginsFolder, "UEFormat");
    public string PluginPath => Path.Combine(FortnitePortingFolder, "FortnitePorting.cs"); // .unitypackage?

    public UnityProjectInfo(string projectFilePath)
    {
        ProjectFilePath = projectFilePath;

        Update();
    }

    public void Update()
    {
        if (File.Exists(PluginPath))
        {
            var pluginInfo = JsonConvert.DeserializeObject<UPlugin>(File.ReadAllText(PluginPath));
            Version = new Version(pluginInfo!.VersionName);
        }

        var imageFilePath = Path.Combine(ProjectFilePath.SubstringBeforeLast("/"), $"{Name}.png");
        if (File.Exists(imageFilePath))
        {
            Image = new Bitmap(imageFilePath);
        }
    }
}