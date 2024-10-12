using System.Text;

namespace CSharpTui.Keymaps;

public class Keymap
{
    private ConsoleKey[] Keys { get; init; }
    private string Help { get; set; } = string.Empty;
    public bool Disabled { get; private set; } = false;
    private bool IsControl { get; set; } = false;
    private bool IsShift { get; set; } = false;

    public Keymap()
        : this([]) { }

    private Keymap(ConsoleKey[] keys)
    {
        Keys = keys;
    }

    public static Keymap Bind(ConsoleKey[] keys) => new(keys);

    public Keymap SetHelp(string key, string help)
    {
        Help = $"{key} ({help})";
        return this;
    }

    public Keymap SetDisabled(bool value)
    {
        Disabled = value;
        return this;
    }

    public Keymap SetIsControl(bool value)
    {
        IsControl = value;
        return this;
    }

    public Keymap SetIsShift(bool value)
    {
        IsShift = true;
        return this;
    }

    public static bool Matches(Keymap keymap, ConsoleKeyInfo key) =>
        !keymap.Disabled
        && keymap.Keys.Any(x =>
            x == key.Key
            && key.Modifiers.HasFlag(ConsoleModifiers.Control) == keymap.IsControl
            && key.Modifiers.HasFlag(ConsoleModifiers.Shift) == keymap.IsShift
        );

    public static bool Matches(Keymap[] keymaps, ConsoleKeyInfo key) =>
        keymaps.Any(keymap =>
            !keymap.Disabled
            && keymap.Keys.Any(x =>
                x == key.Key
                && key.Modifiers.HasFlag(ConsoleModifiers.Control) == keymap.IsControl
                && key.Modifiers.HasFlag(ConsoleModifiers.Shift) == keymap.IsShift
            )
        );

    public static string GetHelpString(IList<Keymap> keymaps)
    {
        StringBuilder builder = new();
        builder.Append(" | ");

        foreach (var keymap in keymaps)
        {
            if (!string.IsNullOrEmpty(keymap.Help) && !keymap.Disabled)
            {
                builder.Append(keymap.Help).Append(" | ");
            }
        }
        return builder.ToString();
    }
}
