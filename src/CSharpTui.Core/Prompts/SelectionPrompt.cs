using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : notnull
{
    private Func<T, string> StringConverter { get; set; } = x => x.ToString();
    private IList<T> Choices { get; set; } = [];
    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();

    public SelectionPrompt(Tui tui)
        : base(tui)
    {
        Tui.Draw();
    }

    public SelectionPrompt() : this(new Tui(string.Empty)) { }

    public new SelectionPrompt<T> SetTitle(string title)
    {
        base.SetTitle(title);
        return this;
    }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        foreach (var choice in choices)
        {
            AddChoice(choice);
        }
        return this;
    }

    public SelectionPrompt<T> AddChoice(T choice)
    {
        Choices.Add(choice);
        return this;
    }

    public SelectionPrompt<T> SetConverter(Func<T, string> converter)
    {
        StringConverter = converter;
        return this;
    }

    public override void InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow]).SetHelp("Up", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow]).SetHelp("Down", "Go down");
    }

    public override T Show(string prompt)
    {
        object selected = new();

        int bufferIndex = Constants.PosYStartIndex + 2;
        int height = bufferIndex;
        foreach (var choice in Choices)
        {
            Tui.UpdateRange(height++, Constants.PosXStartIndex, StringConverter(choice));
        }

        Tui.UpdateRange(Constants.PosYStartIndex, Constants.PosXStartIndex, prompt);

        int index = 0;
        bool loop = true;

        Tui.UpdateCell(bufferIndex, 1, '>');
        Console.SetCursorPosition(0, Tui.Height);
        while (loop)
        {
            var key = Console.ReadKey(true);

            if (Keymap.Matches(SelectKey, key))
            {
                loop = false;
                selected = Choices[index];
            }

            if (Keymap.Matches(UpKey, key))
            {
                if (index > 0)
                {
                    --index;
                    Tui.UpdateCell(bufferIndex--, 1, Constants.EmptyChar);
                    Tui.UpdateCell(bufferIndex, 1, '>');
                }
            }

            if (Keymap.Matches(DownKey, key))
            {
                if (index < Choices.Count)
                {
                    ++index;
                    Tui.UpdateCell(bufferIndex++, 1, Constants.EmptyChar);
                    Tui.UpdateCell(bufferIndex, 1, '>');
                }
            }
        }
        Tui.Clear();

        return (T)selected;
    }
}
