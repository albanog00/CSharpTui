namespace TUI.Core;

public class Keymap
{
    public ConsoleKeyInfo KeyValue { get; init; }
    public Action Callback { get; init; }

    public Keymap(ConsoleKeyInfo keyValue, Action callback)
    {
        KeyValue = keyValue;
        Callback = callback;
    }
}

