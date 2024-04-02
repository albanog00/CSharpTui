using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : notnull
{
    private IList<T> Choices { get; set; } = [];
    private Func<T, string> StringConverter { get; set; } = x => x.ToString()!;
    private IList<KeyValuePair<int, string>> ConvertedChoices = [];
    private IList<KeyValuePair<int, string>> SearchResultChoices = [];
    private string SearchString { get; set; } = string.Empty;

    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();
    private Keymap SearchKey { get; set; } = new();
    private Keymap StopSearch { get; set; } = new();
    private Keymap Delete { get; set; } = new();

    private int HeaderIndex { get; set; }
    private int HelpIndex { get; set; }
    private int ChoicesFirstIndex { get; set; }
    private int ChoicesLastIndex { get; set; }
    private int SearchInputIndex { get; set; }
    private int SearchResultIndex { get; set; } = 0;
    private int ItemsOnScreen { get; set; } = 5;

    public SelectionPrompt(Tui tui) : base(tui)
    {
        HeaderIndex = Constants.PosYStartIndex;

        ChoicesFirstIndex = HeaderIndex + 2;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;

        HelpIndex = ChoicesLastIndex + 2;
        SearchInputIndex = HelpIndex + 2;
    }

    public SelectionPrompt(string title) : this(new Tui()) { }
    public SelectionPrompt() : this(new Tui()) { }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        foreach (var choice in choices)
        {
            this.AddChoice(choice);
        }
        return this;
    }

    private SelectionPrompt<T> Draw()
    {
        Tui.Draw();
        return this
            .InitializeKeymaps()
            .DrawHelp()
            .ConvertChoices()
            .Search()
            .RenderSearch();
    }

    private SelectionPrompt<T> DrawHelp()
    {
        string help = Keymap.GetHelpString([
                SelectKey,
                UpKey,
                DownKey,
                SearchKey,
                StopSearch,
        ]);
        Tui.UpdateLine(HelpIndex, help, Constants.PosXStartIndex);
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

    public SelectionPrompt<T> SetSearchString(string search)
    {
        SearchString = search;
        return this;
    }

    private SelectionPrompt<T> InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K]).SetHelp("Up/k", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J]).SetHelp("Down/j", "Go down");
        SearchKey = Keymap.Bind([ConsoleKey.Q]).SetIsControl(true).SetHelp("Ctrl-q", "Start Search");
        StopSearch = Keymap.Bind([ConsoleKey.Escape]).SetHelp("Esc", "Stop Search").SetDisabled(true);
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
        return this;
    }

    // Cache the converter results
    private SelectionPrompt<T> ConvertChoices()
    {
        for (int i = 0; i < Choices.Count; ++i)
        {
            var convertedString = StringConverter(Choices[i]);
            if (i + ChoicesFirstIndex < ChoicesLastIndex)
            {
                Tui.UpdateLine(i + ChoicesFirstIndex,
                        convertedString, Constants.PosXStartIndex + 2);
            }
            ConvertedChoices.Add(new(i, convertedString));
        }
        return this;
    }

    // Searchs through the cache
    private SelectionPrompt<T> Search()
    {
        int height = ChoicesFirstIndex;
        int currentSize = SearchResultChoices.Count;
        int maxIndex =
            Math.Min(currentSize + height, ChoicesLastIndex);

        SearchResultChoices = [];

        // TODO: Optimize this
        foreach (var choice in ConvertedChoices)
        {
            if (choice.Value.Contains(SearchString))
            {
                SearchResultChoices.Add(choice);
            }
        }
        return this;
    }

    private SelectionPrompt<T> RenderSearch()
    {
        int posX = Constants.PosXStartIndex + 2;
        int firstIndex = Math.Max(0, SearchResultIndex - ItemsOnScreen + 1);
        int height = ChoicesFirstIndex;

        Tui.ResetRange(height, ChoicesLastIndex);

        for (int i = firstIndex; i < SearchResultChoices.Count && height < ChoicesLastIndex; ++i)
        {
            Tui.UpdateLine(height++, SearchResultChoices[i].Value, posX);
        }

        return this;
    }

    public override T Show(string prompt)
    {
        Console.CursorVisible = false;
        int bufferIndex = ChoicesFirstIndex;

        this.Draw();
        Tui.UpdateLine(Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);
        Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');

        object selected = new();
        bool loop = true;
        while (loop)
        {
            Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');
            var key = Console.ReadKey(true);
            if (Keymap.Matches(SelectKey, key))
            {
                if (SearchResultChoices.Count <= 0)
                {
                    continue;
                }

                selected = Choices[SearchResultChoices[SearchResultIndex].Key];
                loop = false;
                continue;
            }

            if (Keymap.Matches(UpKey, key))
            {
                // Should not become negative
                if (SearchResultIndex > 0)
                {
                    --SearchResultIndex;

                    // Should not become less than first index
                    if (bufferIndex > ChoicesFirstIndex)
                    {
                        Tui.UpdateCell(bufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
                    }
                    else
                    {
                        this.RenderSearch();
                    }
                }
                continue;
            }

            if (Keymap.Matches(DownKey, key))
            {
                // Should not become higher than count
                if (SearchResultIndex + 1 < SearchResultChoices.Count)
                {
                    ++SearchResultIndex;

                    // Should not become higher than last index
                    if (bufferIndex + 1 < ChoicesLastIndex)
                    {
                        Tui.UpdateCell(bufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
                    }
                    else
                    {
                        this.RenderSearch();
                    }
                }
                continue;
            }

            if (Keymap.Matches(SearchKey, key))
            {
                UpKey.SetDisabled(true);
                DownKey.SetDisabled(true);
                SelectKey.SetDisabled(true);
                SearchKey.SetDisabled(true);
                StopSearch.SetDisabled(false);
                this.DrawHelp();

                int searchWidth = SearchString.Length + 1;
                bool search = true;

                Console.CursorVisible = true;
                while (search)
                {
                    this.Search();
                    Console.SetCursorPosition(searchWidth, SearchInputIndex);

                    var searchKey = Console.ReadKey(true);

                    if (Keymap.Matches(Delete, searchKey))
                    {
                        if (searchWidth > 1 && SearchString.Length > 0)
                        {
                            SearchString = SearchString[0..(SearchString.Length - 1)];
                            Tui.UpdateCell(SearchInputIndex, --searchWidth, Constants.EmptyChar);
                        }
                        continue;
                    }

                    if (searchKey.Key == ConsoleKey.Escape)
                    {
                        Console.CursorVisible = false;

                        UpKey.SetDisabled(false);
                        DownKey.SetDisabled(false);
                        SelectKey.SetDisabled(false);
                        SearchKey.SetDisabled(false);
                        StopSearch.SetDisabled(true);
                        this.DrawHelp();

                        search = false;
                        continue;
                    }

                    if (searchKey.Modifiers != ConsoleModifiers.Control)
                    {
                        SearchString += searchKey.KeyChar;
                        Tui.UpdateCell(SearchInputIndex, searchWidth++, searchKey.KeyChar);
                    }
                }
                SearchInputIndex = 0;
                bufferIndex = ChoicesFirstIndex;
            }
        }
        Tui.Clear();
        return (T)selected;
    }
}
