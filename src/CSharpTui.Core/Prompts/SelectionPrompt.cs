using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : notnull
{
    private IList<T> Choices { get; set; } = [];
    private Func<T, string> StringConverter { get; set; } = x => x.ToString()!;
    private IList<KeyValuePair<int, string>> ConvertedChoices = [];
    private IList<KeyValuePair<int, string>> SearchResultChoices = [];

    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();
    private Keymap SearchKey { get; set; } = new();
    private Keymap StopSearch { get; set; } = new();
    private Keymap Delete { get; set; } = new();

    private int HeaderIndex { get; set; }
    private int HelpIndex { get; set; }
    private int CountIndex { get; set; }
    private int ChoicesFirstIndex { get; set; }
    private int ChoicesLastIndex { get; set; }
    private int BufferIndex { get; set; }
    private int SearchInputIndex { get; set; }

    private string SearchString { get; set; } = string.Empty;
    private int LastSearchStringLength { get; set; } = 0;
    private int SearchResultIndex { get; set; } = 0;

    private int ItemsOnScreen { get; set; } = 20;

    public SelectionPrompt(Tui tui) : base(tui)
    {
        HeaderIndex = Constants.PosYStartIndex;

        ChoicesFirstIndex = HeaderIndex + 2;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;

        BufferIndex = ChoicesFirstIndex;

        HelpIndex = ChoicesLastIndex + 2;
        CountIndex = HelpIndex - 1;
        SearchInputIndex = HelpIndex + 2;

        InitializeKeymaps();
    }

    public SelectionPrompt() : this(new Tui()) { }

    private SelectionPrompt<T> Draw()
    {
        return this.DrawHelp().Search().RenderSearch();
    }

    private SelectionPrompt<T> DrawCount()
    {
        Tui.UpdateLine(CountIndex,
                $"{SearchResultChoices.Count}/{ConvertedChoices.Count}",
                Constants.PosXStartIndex);
        return this;
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

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        lock (Choices)
            foreach (var choice in choices)
                this.AddChoice(choice);

        if (SearchResultChoices.Count < ItemsOnScreen)
            this.Search().RenderSearch();
        else this.Search().DrawCount();

        return this;
    }

    public SelectionPrompt<T> AddChoice(T choice, bool search = false)
    {
        lock (Choices)
        {
            Choices.Add(choice);
            AddConvertedChoice(choice, Choices.Count);
        }

        if (search)
        {
            if (SearchResultChoices.Count < ItemsOnScreen)
                this.Search().RenderSearch();
            else this.Search().DrawCount();
        }

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

    public SelectionPrompt<T> SetItemsOnScreen(int items)
    {
        if (items <= 0)
            throw new ArgumentException("argument should be greater than 0");

        ItemsOnScreen = items;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;
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

    private SelectionPrompt<T> AddConvertedChoice(T choice, int count)
    {
        lock (ConvertedChoices)
            ConvertedChoices.Add(new(count, StringConverter(choice)));
        return this;
    }

    // Searchs through the cache
    private SelectionPrompt<T> Search()
    {
        SearchResultIndex = 0;

        lock (SearchResultChoices)
        {
            if (LastSearchStringLength < SearchString.Length)
            {
                IList<KeyValuePair<int, string>> newSearchResult = [];
                foreach (var choice in SearchResultChoices)
                    if (Find(SearchString, choice.Value))
                        newSearchResult.Add(choice);
                SearchResultChoices = newSearchResult;
            }
            else
            {
                SearchResultChoices.Clear();
                foreach (var choice in ConvertedChoices)
                    if (Find(SearchString, choice.Value))
                        SearchResultChoices.Add(choice);
            }
        }
        return this;
    }

    // Fuzzy Finder
    private bool Find(string find, string value)
    {
        if (value.Length < find.Length)
            return false;

        int j = 0;
        for (int i = 0; i < value.Length && j < find.Length; ++i)
            if (value[i] == find[j])
                ++j;
        return j == find.Length;
    }

    private SelectionPrompt<T> RenderSearch()
    {
        int posX = Constants.PosXStartIndex + 2;
        int firstIndex = Math.Max(0, SearchResultIndex + 1 - ItemsOnScreen);
        int height = ChoicesFirstIndex;

        Tui.ResetRange(height, ChoicesLastIndex);

        for (int i = firstIndex; i < SearchResultChoices.Count && height < ChoicesLastIndex; ++i)
            Tui.UpdateLine(height++, SearchResultChoices[i].Value, posX);
        return this.DrawCount();
    }

    public override T Show(string prompt)
    {
        Console.CursorVisible = false;

        this.Draw();
        Tui.UpdateLine(Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, '>');

        object selected = new();
        bool loop = true;
        while (loop)
        {
            Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, '>');
            var key = Console.ReadKey(true);
            if (Keymap.Matches(SelectKey, key))
            {
                if (SearchResultChoices.Count <= 0)
                    continue;

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
                    if (BufferIndex > ChoicesFirstIndex)
                        Tui.UpdateCell(BufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
                    else this.RenderSearch();
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
                    if (BufferIndex + 1 < ChoicesLastIndex)
                        Tui.UpdateCell(BufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
                    else this.RenderSearch();
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
                    LastSearchStringLength = SearchString.Length;

                    Console.SetCursorPosition(searchWidth, SearchInputIndex);
                    Console.CursorVisible = true;
                    var searchKey = Console.ReadKey(true);
                    Console.CursorVisible = false;

                    if (Keymap.Matches(Delete, searchKey))
                    {
                        if (searchWidth > 1 && SearchString.Length > 0)
                        {
                            SearchString = SearchString[0..(SearchString.Length - 1)];
                            Tui.UpdateCell(SearchInputIndex, --searchWidth, Constants.EmptyChar);
                            this.Search().RenderSearch();
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
                        this.Search().RenderSearch();
                    }
                }
                BufferIndex = ChoicesFirstIndex;
                continue;
            }
        }

        Tui.Clear();
        return (T)selected;
    }
}

