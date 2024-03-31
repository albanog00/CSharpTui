using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : notnull
{
    private Func<T, string> StringConverter { get; set; } = x => x.ToString()!;
    private IList<T> Choices { get; set; } = [];
    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();
    private int HelpHeight { get; set; }

    public SelectionPrompt(Tui tui) : base(tui)
    {
        HelpHeight = Tui.Height - 3;
        InitializeKeymaps();
        Draw();
    }

    public SelectionPrompt(string title) : this(new Tui(title)) { }
    public SelectionPrompt() : this(new Tui(string.Empty)) { }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        foreach (var choice in choices)
        {
            AddChoice(choice);
        }
        return this;
    }

    public void Draw()
    {
        Tui.Draw();
        DrawHelp();
    }

    public void DrawHelp()
    {
        string help = Keymap.GetHelpString([
                SelectKey,
                UpKey,
                DownKey,
        ]);

        int startIndex = Constants.PosXStartIndex;
        int endIndex = help.Length + startIndex;

        Tui.UpdateRange(HelpHeight, Constants.PosXStartIndex, help);
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

    public void InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K]).SetHelp("Up/k", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J]).SetHelp("Down/j", "Go down");
    }

    public override T Show(string prompt)
    {
        object selected = new();
        int bufferIndex = Constants.PosYStartIndex + 2;
        int height = bufferIndex;
        int posX = Constants.PosXStartIndex + 2;

        foreach (var choice in Choices)
        {
            Tui.UpdateRange(height++, posX, StringConverter(choice));
        }
        Tui.UpdateRange(Constants.PosYStartIndex, Constants.PosXStartIndex, prompt);

        int index = 0;
        bool loop = true;

        Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
        while (loop)
        {
            Console.SetCursorPosition(0, Tui.Height);
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
                    Tui.UpdateCell(bufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
                }
                else
                {
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
                    bufferIndex += Choices.Count - 1;
                    index = Choices.Count - 1;
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
                }
            }

            if (Keymap.Matches(DownKey, key))
            {
                if (index < Choices.Count - 1)
                {
                    ++index;
                    Tui.UpdateCell(bufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
                }
                else
                {
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
                    bufferIndex -= index;
                    index = 0;
                    Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
                }
            }
        }
        Tui.Clear();

        return (T)selected;
    }
}
