using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KiteGlance.State;

public enum PinMode
{
    /// <summary>Ordinary window. Falls behind whatever you click next.</summary>
    Normal,

    /// <summary>Floats above every app. Useful while actively trading.</summary>
    AlwaysOnTop,

    /// <summary>
    /// Pinned under every app, over the wallpaper: bottom-most z-order,
    /// out of Alt+Tab. What a desktop widget should be. The default.
    /// </summary>
    Desktop
}

public enum BackdropMode
{
    /// <summary>Dawn, day, dusk, night -- follows the clock. The default.</summary>
    TimeOfDay,

    /// <summary>Cycles through the whole set every few hours.</summary>
    Rotate,

    /// <summary>One fixed backdrop (the day graphite).</summary>
    Static,

    /// <summary>An image of the user's choosing.</summary>
    Custom
}

/// <summary>
/// Where you put the widget, how you left it, and how you like it pinned.
///
/// A widget that forgets its position after every restart is not a widget --
/// it's a window that happens to be small. Nothing else on the desktop behaves
/// that way, and the omission does more damage to the sense of "first-party"
/// than any amount of gradient work can repair.
/// </summary>
public sealed class WidgetState
{
    [JsonIgnore]
    private static readonly string Path_ = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KiteGlance", "state.json");

    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Expanded { get; set; }
    public string Tab { get; set; } = "stocks";
    public PinMode Pin { get; set; } = PinMode.Desktop;
    public BackdropMode Backdrop { get; set; } = BackdropMode.TimeOfDay;

    /// <summary>Absolute path of the user-chosen image (Custom mode only).
    /// Points inside %APPDATA%\KiteGlance, where we copy the picked file, so
    /// the backdrop survives the original being moved or deleted.</summary>
    public string? CustomBackdropPath { get; set; }

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static WidgetState Load()
    {
        try
        {
            if (!File.Exists(Path_)) return new WidgetState();

            var json = File.ReadAllText(Path_);
            return JsonSerializer.Deserialize<WidgetState>(json, Opts) ?? new WidgetState();
        }
        catch
        {
            return new WidgetState();
        }
    }

    public void Save()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path_)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path_, JsonSerializer.Serialize(this, Opts));
        }
        catch
        {
            // Losing window position is not worth crashing over.
        }
    }
}
