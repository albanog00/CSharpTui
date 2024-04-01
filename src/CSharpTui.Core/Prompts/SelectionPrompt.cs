using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : notnull
{
    private IList<T> Choices { get; set; } = [];
    private Func<T, string> StringConverter { get; set; } = x => x.ToString()!;
    private IList<KeyValuePair<int, string>> ConvertedChoice = [];
    private string SearchString { get; set; } = string.Empty;

    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();
    private Keymap Search { get; set; } = new();
    private Keymap StopSearch { get; set; } = new();
    private Keymap Delete { get; set; } = new();

    private int HeaderIndex { get; set; }
    private int HelpIndex { get; set; }
    private int ListStartIndex { get; set; }
    private int ListMaxIndex { get; set; }
    private int SearchIndex { get; set; }

    public SelectionPrompt(Tui tui) : base(tui)
    {
        HeaderIndex = Constants.PosYStartIndex;
        HelpIndex = Tui.Height - 4;

        ListStartIndex = HeaderIndex + 2;
        ListMaxIndex = HelpIndex - 2;

        SearchIndex = HelpIndex + 2;

        InitializeKeymaps();
    }

    public SelectionPrompt(string title) : this(new Tui()) { }
    public SelectionPrompt() : this(new Tui()) { }

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
                Search,
                StopSearch,
        ]);
        Tui.UpdateLineRange(HelpIndex, help, Constants.PosXStartIndex);
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

    public SelectionPrompt<T> SetSearchString(string search)
    {
        SearchString = search;
        return this;
    }

    public void InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K]).SetHelp("Up/k", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J]).SetHelp("Down/j", "Go down");
        Search = Keymap.Bind([ConsoleKey.Q]).SetIsControl(true).SetHelp("Ctrl-q", "Start Search");
        StopSearch = Keymap.Bind([ConsoleKey.Escape]).SetHelp("Esc", "Stop Search").SetDisabled(true);
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
    }

    public IList<KeyValuePair<int, string>> UpdateSearch(int currentlyShowedItems = 0)
    {
        int maxIndex = Choices.Count + ListStartIndex > ListMaxIndex
            ? ListMaxIndex
            : ListStartIndex + currentlyShowedItems;

        Tui.ResetRange(ListStartIndex, maxIndex);

        int height = ListStartIndex;
        int posX = Constants.PosXStartIndex + 2;

        IList<KeyValuePair<int, string>> items = [];
        foreach (var choice in ConvertedChoice)
        {
            if (choice.Value.Contains(SearchString))
            {
                if (height <= ListMaxIndex)
                {
                    Tui.UpdateLineRange(height++, choice.Value, posX);
                }
                items.Add(choice);
            }
        }
        return items;
    }

    public override T Show(string prompt)
    {
        object selected = new();

        for (int i = 0; i < Choices.Count; ++i)
        {
            ConvertedChoice.Add(new(i, StringConverter(Choices[i])));
        }
        Console.CursorVisible = false;
        Tui.UpdateLineRange(Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);
        Draw();

        var showedItems = UpdateSearch();

        int bufferIndex = ListStartIndex;
        int index = 0;
        bool loop = true;
        while (loop)
        {
            Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');

            int minIndex = ListStartIndex;
            int maxIndex = minIndex + showedItems.Count - 1;
            bool higherOrEqualThanMin = bufferIndex > minIndex;
            bool lowerThanMax = bufferIndex < maxIndex;

            var key = Console.ReadKey(true);

            if (Keymap.Matches(SelectKey, key))
            {
                loop = false;
                selected = Choices[showedItems[index].Key];
                continue;
            }

            if (Keymap.Matches(UpKey, key))
            {
                if (higherOrEqualThanMin)
                {
                    --index;
                    Tui.UpdateCell(bufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
                }
                else
                {
                    Tui.UpdateCell(minIndex, Constants.PosXStartIndex, Constants.EmptyChar);
                    bufferIndex = maxIndex;
                    index = showedItems.Count - 1;
                }
                continue;
            }

            if (Keymap.Matches(DownKey, key))
            {
                if (lowerThanMax)
                {
                    ++index;
                    Tui.UpdateCell(bufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
                }
                else
                {
                    Tui.UpdateCell(maxIndex, Constants.PosXStartIndex, Constants.EmptyChar);
                    bufferIndex = minIndex;
                    index = 0;
                }
                continue;
            }

            if (Keymap.Matches(Search, key))
            {
                Console.CursorVisible = true;

                UpKey.SetDisabled(true);
                DownKey.SetDisabled(true);
                SelectKey.SetDisabled(true);
                Search.SetDisabled(true);
                StopSearch.SetDisabled(false);
                DrawHelp();

                int searchWidth = SearchString.Length + 1;
                bool search = true;
                while (search)
                {
                    showedItems = UpdateSearch(showedItems.Count);
                    Console.SetCursorPosition(searchWidth, SearchIndex);

                    var searchKey = Console.ReadKey(true);

                    if (Keymap.Matches(Delete, searchKey))
                    {
                        if (searchWidth > 1 && SearchString.Length > 0)
                        {
                            SearchString = SearchString[0..(SearchString.Length - 1)];
                            Tui.UpdateCell(SearchIndex, --searchWidth, Constants.EmptyChar);
                        }
                        continue;
                    }

                    if (searchKey.Key == ConsoleKey.Escape)
                    {
                        Console.CursorVisible = false;

                        UpKey.SetDisabled(false);
                        DownKey.SetDisabled(false);
                        SelectKey.SetDisabled(false);
                        Search.SetDisabled(false);
                        StopSearch.SetDisabled(true);
                        DrawHelp();

                        search = false;
                        break;
                    }

                    if (searchKey.Modifiers != ConsoleModifiers.Control)
                    {
                        SearchString += searchKey.KeyChar;
                        Tui.UpdateCell(SearchIndex, searchWidth++, searchKey.KeyChar);
                    }
                }
                index = 0;
                bufferIndex = ListStartIndex;
            }
        }
        Tui.Clear();
        return (T)selected;
    }

    public void HandleKey(ConsoleKeyInfo key)
    {

    }
}
