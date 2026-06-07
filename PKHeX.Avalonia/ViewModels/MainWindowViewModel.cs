using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ── observable state ────────────────────────────────────────────────────
    [ObservableProperty] private bool _hasSave;
    [ObservableProperty] private string _trainerName = string.Empty;
    [ObservableProperty] private string _gameVersion = string.Empty;
    [ObservableProperty] private string _trainerID = string.Empty;
    [ObservableProperty] private string _money = string.Empty;
    [ObservableProperty] private string _statusText = "No save loaded.";
    [ObservableProperty] private int _currentBoxIndex;
    [ObservableProperty] private ObservableCollection<string> _boxNames = [];
    [ObservableProperty] private ObservableCollection<SlotViewModel> _currentBoxSlots = [];
    [ObservableProperty] private ObservableCollection<SlotViewModel> _partySlots = [];
    [ObservableProperty] private PokemonEditorViewModel _pokemonEditor = new();

    // SAV tab info
    [ObservableProperty] private string _playTime = string.Empty;
    [ObservableProperty] private string _savLanguage = string.Empty;
    [ObservableProperty] private string _savGameCode = string.Empty;
    [ObservableProperty] private string _savGeneration = string.Empty;

    // ── private backing state ───────────────────────────────────────────────
    private SaveFile? _sav;
    private string[] _speciesNames = [];
    private string _lastSavePath = string.Empty;

    /// <summary>Set by the View so file dialogs can be opened without
    /// the ViewModel holding a hard reference to the Window.</summary>
    public TopLevel? TopLevel { get; set; }

    public MainWindowViewModel()
    {
        // Refresh box grid and party after the editor writes changes back to save.
        PokemonEditor.Applied = () =>
        {
            if (_sav is not null)
            {
                LoadBox(CurrentBoxIndex);
                LoadParty();
            }
        };
    }

    // ── file-type helpers ────────────────────────────────────────────────────
    private static readonly FilePickerFileType SaveFileType = new("Save Files")
    {
        Patterns = ["*.sav", "*.dsv", "*.dat", "*.gci", "*.bin"]
    };

    private static readonly FilePickerFileType AllFiles = new("All Files")
    {
        Patterns = ["*.*"]
    };

    // ── commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenSave()
    {
        if (TopLevel is null)
            return;

        var files = await TopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Save File",
            AllowMultiple = false,
            FileTypeFilter = [SaveFileType, AllFiles]
        });

        if (files.Count == 0)
            return;

        var file = files[0];
        await using var stream = await file.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var data = ms.ToArray();

        var sav = SaveUtil.GetSaveFile(data);
        if (sav is null)
        {
            StatusText = "Failed to load save — unrecognised format.";
            return;
        }

        _lastSavePath = file.Path.LocalPath;
        LoadSave(sav);
    }

    [RelayCommand]
    private void Save()
    {
        if (_sav is null)
            return;

        if (string.IsNullOrEmpty(_lastSavePath))
        {
            // Fall back to SaveAs if there is no known path yet.
            _ = SaveAsAsync();
            return;
        }

        try
        {
            var bytes = _sav.Write().ToArray();
            File.WriteAllBytes(_lastSavePath, bytes);
            StatusText = $"Saved to {Path.GetFileName(_lastSavePath)}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAs()
    {
        await SaveAsAsync();
    }

    private async Task SaveAsAsync()
    {
        if (_sav is null || TopLevel is null)
            return;

        var file = await TopLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File As",
            SuggestedFileName = string.IsNullOrEmpty(_lastSavePath)
                ? "main.sav"
                : Path.GetFileName(_lastSavePath),
            FileTypeChoices = [SaveFileType, AllFiles]
        });

        if (file is null)
            return;

        try
        {
            var bytes = _sav.Write().ToArray();
            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(bytes);
            _lastSavePath = file.Path.LocalPath;
            StatusText = $"Saved to {Path.GetFileName(_lastSavePath)}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Save failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Exit()
    {
        // Request the application to shut down.
        if (global::Avalonia.Application.Current?.ApplicationLifetime is
            global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    [RelayCommand]
    private void SelectSlot(SlotViewModel slot)
    {
        if (_sav is null || slot.IsEmpty)
            return;

        PokemonEditor.LoadPokemon(slot.Entity, slot.Box, slot.Slot, _sav);
    }

    [RelayCommand]
    private void SelectPartySlot(SlotViewModel slot)
    {
        if (_sav is null || slot.IsEmpty)
            return;

        PokemonEditor.LoadPokemon(slot.Entity, -1, slot.Slot, _sav, isPartySlot: true);
    }

    // ── partial property callbacks ───────────────────────────────────────────

    partial void OnCurrentBoxIndexChanged(int value)
    {
        if (_sav is not null && value >= 0 && value < _sav.BoxCount)
            LoadBox(value);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private void LoadSave(SaveFile sav)
    {
        _sav = sav;

        // Cache species names for slot display.
        var strings = GameInfo.GetStrings("en");
        _speciesNames = strings.specieslist;

        // Trainer info.
        TrainerName = sav.OT;
        GameVersion = sav.Version.ToString();
        TrainerID = $"TID: {sav.TID16}  SID: {sav.SID16}";
        Money = $"${sav.Money:N0}";
        HasSave = true;
        StatusText = $"Loaded {Path.GetFileName(_lastSavePath)} — {sav.Version}";

        // Box names.
        BoxNames.Clear();
        if (sav.HasBox)
        {
            var names = BoxUtil.GetBoxNames(sav);
            foreach (var name in names)
                BoxNames.Add(name);
        }

        // SAV tab details.
        PlayTime = sav.PlayTimeString;
        SavLanguage = sav.Language.ToString();
        SavGameCode = sav.Version.ToString();
        SavGeneration = $"Gen {sav.Generation}";

        // Load first box.
        CurrentBoxIndex = 0;
        LoadBox(0);
        LoadParty();
    }

    private void LoadBox(int box)
    {
        if (_sav is null || !_sav.HasBox)
            return;

        CurrentBoxSlots.Clear();

        int slotCount = _sav.BoxSlotCount;
        for (int slot = 0; slot < slotCount; slot++)
        {
            var pk = _sav.GetBoxSlotAtIndex(box, slot);
            CurrentBoxSlots.Add(new SlotViewModel(box, slot, pk, _speciesNames, SelectSlotCommand));
        }
    }

    private void LoadParty()
    {
        if (_sav is null || !_sav.HasParty)
            return;

        PartySlots.Clear();

        for (int i = 0; i < 6; i++)
        {
            var pk = _sav.GetPartySlotAtIndex(i);
            PartySlots.Add(new SlotViewModel(-1, i, pk, _speciesNames, SelectPartySlotCommand));
        }
    }
}
