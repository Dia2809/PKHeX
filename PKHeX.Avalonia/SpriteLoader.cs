using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace PKHeX.Avalonia;

/// <summary>
/// Loads PKHeX's embedded PNG sprites as Avalonia <see cref="Bitmap"/>s, fully
/// cross-platform (no System.Drawing). Sprites are embedded via AvaloniaResource
/// and addressed by the same filename convention PKHeX uses on disk:
/// <c>b_{species}[-{form}][f][s].png</c> (f = female-gendered, s = shiny).
/// </summary>
public static class SpriteLoader
{
    private const string SpriteRoot = "avares://PKHeX.Avalonia/Assets/sprites/";
    private const string ItemRoot = "avares://PKHeX.Avalonia/Assets/items/";

    // Cache decoded bitmaps (and misses) by URI so each image is decoded once.
    private static readonly Dictionary<string, Bitmap?> Cache = new();

    /// <summary>Egg / empty placeholder sprite.</summary>
    public static Bitmap? Egg => Load(SpriteRoot + "b_0.png");

    /// <summary>
    /// Resolves the best-matching sprite for the given entity attributes,
    /// preferring shiny/form/gender variants and falling back gracefully to the
    /// base species sprite, then the egg placeholder.
    /// </summary>
    public static Bitmap? GetSprite(ushort species, byte form, byte gender, bool shiny)
    {
        if (species == 0)
            return null;

        string b = $"b_{species}";
        string f = form > 0 ? $"-{form}" : string.Empty;
        string g = gender == 1 ? "f" : string.Empty; // female-gendered sprites only exist for a few species
        string s = shiny ? "s" : string.Empty;

        foreach (var name in Candidates(b, f, g, s))
        {
            var bmp = Load(SpriteRoot + name + ".png");
            if (bmp is not null)
                return bmp;
        }

        return Load(SpriteRoot + "b_0.png");
    }

    /// <summary>Held-item icon, or null when there is no item / no icon.</summary>
    public static Bitmap? GetItem(int item)
    {
        if (item <= 0)
            return null;
        return Load(ItemRoot + $"bitem_{item}.png");
    }

    // Ordered candidate filenames; AssetLoader.Exists gates each so missing
    // variants (e.g. a species without a female sprite) are skipped cleanly.
    private static IEnumerable<string> Candidates(string b, string f, string g, string s)
    {
        var seen = new HashSet<string>();
        foreach (var n in new[]
                 {
                     b + f + g + s, // form + female + shiny
                     b + f + s,     // form + shiny
                     b + g + s,     // female + shiny
                     b + s,         // shiny
                     b + f + g,     // form + female
                     b + f,         // form
                     b + g,         // female
                     b,             // base
                 })
        {
            if (seen.Add(n))
                yield return n;
        }
    }

    private static Bitmap? Load(string uri)
    {
        if (Cache.TryGetValue(uri, out var cached))
            return cached;

        Bitmap? bmp = null;
        try
        {
            var u = new Uri(uri);
            if (AssetLoader.Exists(u))
            {
                using var stream = AssetLoader.Open(u);
                bmp = new Bitmap(stream);
            }
        }
        catch
        {
            bmp = null;
        }

        Cache[uri] = bmp;
        return bmp;
    }
}
