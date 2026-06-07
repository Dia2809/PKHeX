using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PKHeX.Core;

namespace PKHeX.Avalonia.ViewModels;

public partial class PokemonEditorViewModel : ViewModelBase
{
    // ── backing PKM state ────────────────────────────────────────────────────
    private PKM? _pk;
    private int _box;
    private int _slot;
    private SaveFile? _sav;

    // ── observable fields ─────────────────────────────────────────────────────
    [ObservableProperty] private string _species = string.Empty;
    [ObservableProperty] private string _nickname = string.Empty;
    [ObservableProperty] private int _level;
    [ObservableProperty] private string _otName = string.Empty;
    [ObservableProperty] private bool _isShiny;
    [ObservableProperty] private bool _isNicknamed;

    [ObservableProperty] private int _selectedNatureIndex;
    [ObservableProperty] private int _selectedAbilityIndex;
    [ObservableProperty] private int _selectedGenderIndex;

    [ObservableProperty] private int _selectedMove1Index;
    [ObservableProperty] private int _selectedMove2Index;
    [ObservableProperty] private int _selectedMove3Index;
    [ObservableProperty] private int _selectedMove4Index;

    [ObservableProperty] private int _selectedItemIndex;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivHp;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivAtk;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivDef;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivSpa;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivSpd;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IvTotal))] private int _ivSpe;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evHp;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evAtk;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evDef;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evSpa;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evSpd;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(EvTotal))] [NotifyPropertyChangedFor(nameof(EvRemaining))] private int _evSpe;

    [ObservableProperty] private string _legalityReport = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(LegalityStatus))] private bool _isLegal;
    [ObservableProperty] private bool _hasPokemon;
    [ObservableProperty] private Bitmap? _sprite;

    // ── computed display helpers ──────────────────────────────────────────────
    public int IvTotal => IvHp + IvAtk + IvDef + IvSpa + IvSpd + IvSpe;
    public int EvTotal => EvHp + EvAtk + EvDef + EvSpa + EvSpd + EvSpe;
    public int EvRemaining => 510 - EvTotal;
    public string LegalityStatus => IsLegal ? "✓ Legal" : "✗ Illegal";

    /// <summary>Raised after <see cref="ApplyChanges"/> writes back to the save,
    /// so the owner can refresh the box grid (e.g. shiny colouring).</summary>
    public Action? Applied { get; set; }

    // ── dropdown option lists ─────────────────────────────────────────────────
    public ObservableCollection<string> NatureOptions { get; } = [];
    public ObservableCollection<string> AbilityOptions { get; } = [];
    public string[] GenderOptions { get; } = ["♂ Male", "♀ Female", "― Genderless"];
    public ObservableCollection<string> MoveOptions { get; } = [];
    public ObservableCollection<string> ItemOptions { get; } = [];

    // ── constructor ───────────────────────────────────────────────────────────

    public PokemonEditorViewModel()
    {
        var strings = GameInfo.GetStrings("en");

        // Natures 0–24 (Hardy → Quirky).
        for (int i = 0; i <= 24; i++)
            NatureOptions.Add(i < strings.natures.Length ? strings.natures[i] : $"Nature {i}");

        // Moves — include all, index 0 is "(None)".
        foreach (var move in strings.movelist)
            MoveOptions.Add(move);

        // Items — index 0 is "(None)".
        foreach (var item in strings.itemlist)
            ItemOptions.Add(item);

        // Abilities — populated per-Pokémon in LoadPokemon; pre-fill from master list.
        foreach (var ability in strings.abilitylist)
            AbilityOptions.Add(ability);
    }

    // ── public load entry point ───────────────────────────────────────────────

    public void LoadPokemon(PKM pk, int box, int slot, SaveFile sav)
    {
        _pk = pk;
        _box = box;
        _slot = slot;
        _sav = sav;

        var strings = GameInfo.GetStrings("en");

        // Basic info.
        Species = pk.Species < strings.specieslist.Length
            ? strings.specieslist[pk.Species]
            : $"#{pk.Species}";
        Nickname = pk.Nickname;
        Level = pk.CurrentLevel;
        OtName = pk.OriginalTrainerName;
        IsShiny = pk.IsShiny;
        IsNicknamed = pk.IsNicknamed;

        // Gender (0=M, 1=F, 2=genderless).
        SelectedGenderIndex = pk.Gender <= 2 ? pk.Gender : 2;

        // Nature (use StatNature for the displayed/effective nature).
        SelectedNatureIndex = (int)pk.StatNature;

        // Ability — find index in the master ability list.
        SelectedAbilityIndex = Math.Max(0, Array.IndexOf(strings.abilitylist, strings.abilitylist
            .ElementAtOrDefault(pk.Ability) ?? string.Empty));
        // pk.Ability is the ability ID, so use it directly as index if in range.
        SelectedAbilityIndex = pk.Ability >= 0 && pk.Ability < AbilityOptions.Count
            ? pk.Ability
            : 0;

        // Held item.
        SelectedItemIndex = pk.HeldItem >= 0 && pk.HeldItem < ItemOptions.Count
            ? pk.HeldItem
            : 0;

        // Moves — match move ID to position in the movelist array.
        SelectedMove1Index = IndexOfMove(strings, pk.Move1);
        SelectedMove2Index = IndexOfMove(strings, pk.Move2);
        SelectedMove3Index = IndexOfMove(strings, pk.Move3);
        SelectedMove4Index = IndexOfMove(strings, pk.Move4);

        // IVs.
        IvHp  = pk.IV_HP;
        IvAtk = pk.IV_ATK;
        IvDef = pk.IV_DEF;
        IvSpa = pk.IV_SPA;
        IvSpd = pk.IV_SPD;
        IvSpe = pk.IV_SPE;

        // EVs.
        EvHp  = pk.EV_HP;
        EvAtk = pk.EV_ATK;
        EvDef = pk.EV_DEF;
        EvSpa = pk.EV_SPA;
        EvSpd = pk.EV_SPD;
        EvSpe = pk.EV_SPE;

        // Sprite.
        UpdateSprite();

        // Legality.
        RunLegalityCheck();

        HasPokemon = true;
    }

    private void UpdateSprite()
    {
        if (_pk is null)
        {
            Sprite = null;
            return;
        }
        Sprite = SpriteLoader.GetSprite(_pk.Species, _pk.Form, _pk.Gender, _pk.IsShiny);
    }

    // ── commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void ApplyChanges()
    {
        if (_pk is null || _sav is null)
            return;

        // Write all editable fields back to the PKM.
        _pk.Nickname = Nickname;
        _pk.CurrentLevel = (byte)Level;
        _pk.OriginalTrainerName = OtName;
        _pk.IsNicknamed = IsNicknamed;

        if (SelectedGenderIndex >= 0 && SelectedGenderIndex <= 2)
            _pk.Gender = (byte)SelectedGenderIndex;

        if (SelectedNatureIndex >= 0 && SelectedNatureIndex <= 24)
        {
            _pk.Nature = (Nature)SelectedNatureIndex;
            _pk.StatNature = (Nature)SelectedNatureIndex;
        }

        // Ability — store the ability ID (same as index in abilitylist).
        if (SelectedAbilityIndex >= 0 && SelectedAbilityIndex < AbilityOptions.Count)
            _pk.Ability = SelectedAbilityIndex;

        // Held item.
        if (SelectedItemIndex >= 0 && SelectedItemIndex < ItemOptions.Count)
            _pk.HeldItem = SelectedItemIndex;

        // Moves — the index in MoveOptions equals the move ID.
        _pk.Move1 = (ushort)Math.Max(0, SelectedMove1Index);
        _pk.Move2 = (ushort)Math.Max(0, SelectedMove2Index);
        _pk.Move3 = (ushort)Math.Max(0, SelectedMove3Index);
        _pk.Move4 = (ushort)Math.Max(0, SelectedMove4Index);

        // IVs.
        _pk.IV_HP  = IvHp;
        _pk.IV_ATK = IvAtk;
        _pk.IV_DEF = IvDef;
        _pk.IV_SPA = IvSpa;
        _pk.IV_SPD = IvSpd;
        _pk.IV_SPE = IvSpe;

        // EVs.
        _pk.EV_HP  = EvHp;
        _pk.EV_ATK = EvAtk;
        _pk.EV_DEF = EvDef;
        _pk.EV_SPA = EvSpa;
        _pk.EV_SPD = EvSpd;
        _pk.EV_SPE = EvSpe;

        // Persist to save.
        _sav.SetBoxSlotAtIndex(_pk, _box, _slot);

        // Re-run legality.
        RunLegalityCheck();

        // Refresh sprite (form/item/shiny may have changed).
        UpdateSprite();

        // Let the owner refresh the box grid (shiny colour, species, etc.).
        Applied?.Invoke();
    }

    [RelayCommand]
    private void MaxIVs()
    {
        IvHp  = 31;
        IvAtk = 31;
        IvDef = 31;
        IvSpa = 31;
        IvSpd = 31;
        IvSpe = 31;
    }

    [RelayCommand]
    private void ZeroEVs()
    {
        EvHp  = 0;
        EvAtk = 0;
        EvDef = 0;
        EvSpa = 0;
        EvSpd = 0;
        EvSpe = 0;
    }

    [RelayCommand]
    private void MaxEVs()
    {
        // Distribute 510 EVs evenly across 6 stats, capping each at 252.
        const int totalBudget = 510;
        const int statCap = 252;
        int[] evs = new int[6];

        // Assign each stat the smaller of (budget / remaining stats) and the cap.
        int remaining = totalBudget;
        for (int i = 0; i < 6; i++)
        {
            int statsLeft = 6 - i;
            int share = remaining / statsLeft;
            evs[i] = Math.Min(share, statCap);
            remaining -= evs[i];
        }

        EvHp  = evs[0];
        EvAtk = evs[1];
        EvDef = evs[2];
        EvSpa = evs[3];
        EvSpd = evs[4];
        EvSpe = evs[5];
    }

    // ── private helpers ───────────────────────────────────────────────────────

    /// <summary>Returns the index of <paramref name="moveId"/> inside the movelist array,
    /// or 0 (None) when not found.</summary>
    private static int IndexOfMove(GameStrings strings, ushort moveId)
    {
        // The movelist array is indexed by move ID, so use direct indexing.
        if (moveId < strings.movelist.Length)
            return moveId;
        return 0;
    }

    private void RunLegalityCheck()
    {
        if (_pk is null)
        {
            LegalityReport = string.Empty;
            IsLegal = false;
            return;
        }

        var analysis = new LegalityAnalysis(_pk);
        IsLegal = analysis.Valid;
        LegalityReport = analysis.Report();
    }
}
