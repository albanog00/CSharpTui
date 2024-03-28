namespace CSharpTui.Core.Keymaps;

public class Keymap
{
    private ConsoleKey[] Keys { get; init; }
    private string Help { get; set; } = string.Empty;
    private bool Disabled { get; set; }

    private Keymap(ConsoleKey[] keys, bool disabled)
    {
        Keys = keys;
        Disabled = disabled;
    }

    public static Keymap Bind(ConsoleKey[] keys, bool disabled) =>
        new(keys, disabled);

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

    public static bool Matches(IList<Keymap> keymaps, ConsoleKey key) =>
        keymaps.Any(x => x.Keys.Any(k => k == key));

    public static string GetHelpString(IList<Keymap> keymaps)
    {
        string help = string.Empty;
        foreach (var keymap in keymaps)
        {
            if (!string.IsNullOrEmpty(keymap.Help) && !keymap.Disabled)
            {
                help += keymap.Help;
                help += ' ';
            }
        }
        return help;
    }
}
