using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Canvas;
using FortnitePorting.Models.Voting;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels.Settings;
using SixLabors.ImageSharp.PixelFormats;
using Color = Avalonia.Media.Color;
using Poll = FortnitePorting.Models.Voting.Poll;

namespace FortnitePorting.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    [ObservableProperty] private bool _showPixelAuthors = true;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private Color _color;
    public SolidColorBrush Brush => new(Color);
    
    [ObservableProperty] private ushort _width;
    [ObservableProperty] private ushort _height;
    
    [ObservableProperty] private DateTime _nextPlacementTime;
    [ObservableProperty] private TimeSpan _timeUntilNextPlacement = TimeSpan.Zero;
    [ObservableProperty] private bool _readyToPlace;
    
    [ObservableProperty] private string _popupName;

    [ObservableProperty] private WriteableBitmap _bitmapSource;
    [ObservableProperty] private Image _bitmapImage;

    public ConcurrentDictionary<Point, PixelMetadata> PixelMetadata = [];
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;

    public override async Task Initialize()
    {
        var placementUpdateTimer = new DispatcherTimer();
        placementUpdateTimer.Interval = TimeSpan.FromMilliseconds(10);
        placementUpdateTimer.Tick += (sender, args) =>
        {
            TimeUntilNextPlacement = NextPlacementTime - DateTime.UtcNow;
            ReadyToPlace = TimeUntilNextPlacement < TimeSpan.Zero;
        };
        
        placementUpdateTimer.Start();
    }

    public override async Task OnViewOpened()
    {
        await OnlineService.Send([], EPacketType.RequestCanvasInfo);
    }
}