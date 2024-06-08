using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Material.Icons;
using NAudio.Wave;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using FeaturedControl = FortnitePorting.Controls.Home.FeaturedControl;
using NewsControl = FortnitePorting.Controls.Home.NewsControl;

namespace FortnitePorting.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    [ObservableProperty] private ReadOnlyObservableCollection<MusicPackItem> _activeCollection;
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    [ObservableProperty] private MusicPackItem? _activeItem;
    [ObservableProperty] private RadioPlaylist _activePlaylist;
    [ObservableProperty] private ObservableCollection<RadioPlaylist> _playlists = [RadioPlaylist.Default];
    public RadioPlaylist[] CustomPlaylists => Playlists.Where(playlist => !playlist.IsDefault).ToArray();
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))] private bool _isPlaying;
    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumeIconKind))]
    private float volume = 1.0f;
    public MaterialIconKind VolumeIconKind => Volume switch
    {
        0.0f => MaterialIconKind.VolumeMute,
        < 0.3f => MaterialIconKind.VolumeLow,
        < 0.66f => MaterialIconKind.VolumeMedium,
        <= 1.0f => MaterialIconKind.VolumeHigh
    };
    
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    
    [ObservableProperty] private bool _isLooping;
    [ObservableProperty] private bool _isShuffling;

    [ObservableProperty] private int _selectedDeviceIndex = 0;
    public DirectSoundDeviceInfo[] Devices => DirectSoundOut.Devices.ToArray()[1..];
    
    public WaveFileReader? AudioReader;
    public WaveOutEvent OutputDevice = new();
    
    public readonly ReadOnlyObservableCollection<MusicPackItem> Filtered;
    public readonly ReadOnlyObservableCollection<MusicPackItem> PlaylistMusicPacks;
    public SourceList<MusicPackItem> Source = new();
    
    private readonly IObservable<Func<MusicPackItem, bool>> RadioSearchFilter;
    private readonly IObservable<Func<MusicPackItem, bool>> RadioPlaylistFilter;

    private readonly string[] IgnoreFilters = ["Random", "TBD", "MusicPack_000_Default"];
    private const string CLASS_NAME = "AthenaMusicPackItemDefinition";
    
    private readonly DispatcherTimer UpdateTimer = new();

    public RadioViewModel()
    {
        UpdateTimer.Tick += OnUpdateTimerTick;
        UpdateTimer.Interval = TimeSpan.FromSeconds(0.1f);
        UpdateTimer.Start();
        
        RadioSearchFilter = this.WhenAnyValue(radio => radio.SearchFilter).Select(CreateSearchFilter);
        RadioPlaylistFilter = this.WhenAnyValue(radio => radio.ActivePlaylist).Select(CreatePlaylistFilter);
        
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            
            .Filter(RadioPlaylistFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(item => item.Id))
            .Bind(out PlaylistMusicPacks)
            
            .Filter(RadioSearchFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(item => item.Id))
            .Bind(out Filtered)
            .Subscribe();

        ActiveCollection = Filtered;
    }

    public override async Task Initialize()
    {
        SelectedDeviceIndex = AppSettings.Current.AudioDeviceIndex;
        Volume = AppSettings.Current.Volume;
        foreach (var serializeData in AppSettings.Current.Playlists)
        {
            Playlists.Add(await RadioPlaylist.FromSerializeData(serializeData));
        }
        
        var assets = CUE4ParseVM.AssetRegistry
            .Where(data => data.AssetClass.Text.Equals(CLASS_NAME))
            .Where(data => !IgnoreFilters.Any(filter => data.AssetName.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        foreach (var asset in assets)
        {
            try
            {
                var musicPack = await CUE4ParseVM.Provider.LoadObjectAsync(asset.ObjectPath);
                Source.Add(new MusicPackItem(musicPack));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
        }
    }

    public override void OnApplicationExit()
    {
        base.OnApplicationExit();
        
        AppSettings.Current.Playlists = CustomPlaylists.Select(RadioPlaylistSerializeData.FromPlaylist).ToArray();
        AppSettings.Current.AudioDeviceIndex = SelectedDeviceIndex;
        AppSettings.Current.Volume = Volume;
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (AudioReader is null) return;
        
        TotalTime = AudioReader.TotalTime;
        CurrentTime = AudioReader.CurrentTime;
        
        if (CurrentTime >= TotalTime)
        {
            if (IsLooping)
                Restart();
            else
                Next();
        }
    }
    
    public void Play(MusicPackItem musicPackItem)
    { 
        if (!SoundExtensions.TrySaveSoundToAssets(musicPackItem.GetSound(), AppSettings.Current.Application.AssetPath, out Stream stream)) return;
        
        Stop();

        ActiveItem = musicPackItem;
        AudioReader = new WaveFileReader(stream);
        
        TaskService.Run(() =>
        {
            OutputDevice.Init(AudioReader);
            Play();
            
            while (OutputDevice.PlaybackState != PlaybackState.Stopped) { }

            Stop();
        });
    }

    public void UpdateOutputDevice()
    {
        Stop();
        
        OutputDevice = new WaveOutEvent { DeviceNumber = SelectedDeviceIndex };
        
        if (AudioReader is not null)
        {
            OutputDevice.Init(AudioReader);
            Play();
        }
    }
    

    public void Scrub(TimeSpan time)
    {
        if (AudioReader is null) return;
        AudioReader.CurrentTime = time;
    }
    
    public void Restart()
    {
        if (AudioReader is null) return;
        AudioReader.CurrentTime = TimeSpan.Zero;
        OutputDevice.Play();
    }
    
    public void TogglePlayPause()
    {
        if (ActiveItem is null) return;
        
        if (IsPlaying)
            Pause();
        else
            Play();
    }
    
    public void Stop()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Stop();
        ActiveItem.IsPlaying = false;
    }

    public void Pause()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Pause();
        IsPlaying = false;
        ActiveItem.IsPlaying = false;
    }

    public void Play()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Play();
        IsPlaying = true;
        ActiveItem.IsPlaying = true;
    }
    
    public void Previous()
    {
        if (ActiveItem is null) return;

        var previousSongIndex = PlaylistMusicPacks.IndexOf(ActiveItem) - 1;
        if (previousSongIndex < 0) previousSongIndex = PlaylistMusicPacks.Count - 1;
        if (AudioReader?.CurrentTime.TotalSeconds > 5)
        {
            Restart();
            return;
        }
        
        Play(PlaylistMusicPacks[previousSongIndex]);
    }

    public void Next()
    {
        if (ActiveItem is null) return;
        
        var nextSongIndex = IsShuffling ? Random.Shared.Next(0, PlaylistMusicPacks.Count) : PlaylistMusicPacks.IndexOf(ActiveItem) + 1;
        if (nextSongIndex >= PlaylistMusicPacks.Count)
        {
            nextSongIndex = 0;
        }
        
        Play(PlaylistMusicPacks[nextSongIndex]);
    }
    
    public void SetVolume(float value)
    {
        OutputDevice.Volume = value;
    }

    [RelayCommand]
    public async Task SaveAll()
    {
        if (await BrowseFolderDialog() is not { } exportPath) return;

        var directory = new DirectoryInfo(exportPath);
        foreach (var item in Source.Items)
        {
            await item.SaveAudio(directory);
        }
    }
    
    [RelayCommand]
    public async Task AddPlaylist()
    {
        Playlists.Add(new RadioPlaylist(isDefault: false));
    }

    [RelayCommand]
    public async Task RemovePlaylist()
    {
        if (ActivePlaylist.IsDefault) return;
        
        Playlists.Remove(ActivePlaylist);
        ActivePlaylist = Playlists.Last();
    }
    
    [RelayCommand]
    public async Task ExportPlaylist()
    {
        if (ActivePlaylist.IsDefault) return;
        if (await SaveFileDialog(suggestedFileName: ActivePlaylist.PlaylistName, fileTypes: Globals.PlaylistFileType) is not { } path) return;

        path = path.SubstringBeforeLast(".").SubstringBeforeLast("."); // scuffed fix for avalonia bug
        var serializeData = RadioPlaylistSerializeData.FromPlaylist(ActivePlaylist);
        await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(serializeData));
    }
    
    [RelayCommand]
    public async Task ImportPlaylist()
    {
        if (await BrowseFileDialog(fileTypes: Globals.PlaylistFileType) is not { } path) return;

        var serializeData = JsonConvert.DeserializeObject<RadioPlaylistSerializeData>(await File.ReadAllTextAsync(path));
        if (serializeData is null) return;

        var playlist = await RadioPlaylist.FromSerializeData(serializeData);
        Playlists.Add(playlist);
    }
    
    private static Func<MusicPackItem, bool> CreateSearchFilter(string searchFilter)
    {
        return item => item.Match(searchFilter);
    }
    
    private static Func<MusicPackItem, bool> CreatePlaylistFilter(RadioPlaylist playlist)
    {
        if (playlist is null) return _ => true;
        
        return item => playlist.ContainsID(item.Id);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(SelectedDeviceIndex):
            {
                UpdateOutputDevice();
                break;
            }
        }
    }
}