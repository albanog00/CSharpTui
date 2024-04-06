using CSharpTui.Core.Keymaps;
using System.Reflection.Metadata.Ecma335;

namespace CSharpTui.Core.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : class
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
    private Keymap Exit { get; set; } = new();

    private int HeaderIndex { get; set; }
    private int HelpIndex { get; set; }
    private int CountIndex { get; set; }
    private int ChoicesFirstIndex { get; set; }
    private int ChoicesLastIndex { get; set; }
    private int BufferIndex { get; set; }
    private int SearchInputIndex { get; set; }

    private CancellationTokenSource CancellationTokenSearch { get; set; } = new();
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
        Tui.Draw();
        this.DrawHelp().Search();
        return this.RenderSearch();
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
                Exit
        ]);
        Tui.UpdateLine(HelpIndex, help, Constants.PosXStartIndex);
        return this;
    }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        foreach (var choice in choices)
            this.AddChoice(choice);

        if (SearchResultChoices.Count < ItemsOnScreen)
            this.Search().RenderSearch();
        else this.Search().DrawCount();

        return this;
    }

    private SelectionPrompt<T> AddChoice(T choice, bool search = false)
    {
        int index;
        lock (Choices)
        {
            Choices.Add(choice);
            index = Choices.Count - 1;
        }
        AddConvertedChoice(choice, index);

        if (search)
        {
            if (SearchResultChoices.Count < ItemsOnScreen)
                this.Search().RenderSearch();
            else this.Search().DrawCount();
        }

        return this;
    }

    public SelectionPrompt<T> AddChoice(T choice) =>
        this.AddChoice(choice, false);

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

        Tui.ResetRange(ChoicesFirstIndex, SearchInputIndex);

        ItemsOnScreen = items;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;

        BufferIndex = ChoicesFirstIndex;

        HelpIndex = ChoicesLastIndex + 2;
        CountIndex = HelpIndex - 1;
        SearchInputIndex = HelpIndex + 2;

        return this.Draw();
    }

    private SelectionPrompt<T> InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K]).SetHelp("Up/k", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J]).SetHelp("Down/j", "Go down");
        SearchKey = Keymap.Bind([ConsoleKey.Q]).SetIsControl(true).SetHelp("Ctrl-q", "Start Search");
        StopSearch = Keymap.Bind([ConsoleKey.Escape]).SetHelp("Esc", "Stop Search").SetDisabled(true);
        Exit = Keymap.Bind([ConsoleKey.Q]).SetIsShift(true).SetHelp("Q", "Exit");
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
        return this;
    }

    private SelectionPrompt<T> KeymapsSearchToggle()
    {
        UpKey.SetDisabled(!UpKey.Disabled);
        DownKey.SetDisabled(!DownKey.Disabled);
        SelectKey.SetDisabled(!SelectKey.Disabled);
        SearchKey.SetDisabled(!SearchKey.Disabled);
        StopSearch.SetDisabled(!StopSearch.Disabled);
        Exit.SetDisabled(!Exit.Disabled);
        return this;
    }

    private SelectionPrompt<T> AddConvertedChoice(T choice, int count)
    {
        lock (ConvertedChoices)
            ConvertedChoices.Add(new(count, StringConverter(choice)));
        return this;
    }

    private SelectionPrompt<T> SearchAsync(
        CancellationToken cancellationToken = default)
    {
        IList<KeyValuePair<int, string>> newSearchResult = [];

        if (LastSearchStringLength > 0 &&
            LastSearchStringLength < SearchString.Length)
        {
            // if `SearchString` gets appended a new character
            // it searches in through cached results in `SearchResultChoices`.
            newSearchResult = SearchResultChoices
                .AsParallel()
                .Where(x => x.Value.Contains(
                    SearchString, StringComparison.OrdinalIgnoreCase))
                .Select(x => x)
                .ToArray();
        }
        else
        {
            // else if last character of search string is deleted 
            // it searches in `ConvertedChoices`.
            newSearchResult = ConvertedChoices
                .AsParallel()
                .Where(x => x.Value.Contains(
                    SearchString, StringComparison.OrdinalIgnoreCase))
                .Select(x => x)
                .ToArray();
        }

        if (!cancellationToken.IsCancellationRequested)
            lock (SearchResultChoices)
                SearchResultChoices = newSearchResult;

        return this;
    }

    private SelectionPrompt<T> Search()
    {
        if (!CancellationTokenSearch.IsCancellationRequested)
            CancellationTokenSearch.Cancel();

        CancellationTokenSearch = new();
        return Task.Run(() => SearchAsync(CancellationTokenSearch.Token)).Result;
    }

    private SelectionPrompt<T> RenderSearch(int index = 0)
    {
        int posX = Constants.PosXStartIndex + 2;
        int height = ChoicesFirstIndex;

        Tui.ResetRange(height, ChoicesLastIndex + 1);

        for (int i = index; i < SearchResultChoices.Count && height < ChoicesLastIndex; ++i)
            Tui.UpdateLine(height++, SearchResultChoices[i].Value, posX);
        return this.DrawCount();
    }

    private void RenderNext() =>
        this.RenderSearch(Math.Max(0, SearchResultIndex + 1 - ItemsOnScreen));

    private void RenderPrev() =>
        this.RenderSearch(SearchResultIndex);

    public override T? Show(string prompt)
    {
        return ShowAsync(prompt, new()).GetAwaiter().GetResult();
    }

    public override async Task<T?> ShowAsync(string prompt, CancellationTokenSource tokenSource)
    {
        Tui.UpdateLine(Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);
        Console.CursorVisible = false;
        await Task.Run(() => this.Draw());

        var result = HandleInput();

        tokenSource.Cancel();
        Tui.Clear();

        return result;
    }

    private T? HandleInput()
    {
        object? selected = null;
        bool loop = true;
        while (loop)
        {
            Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, '>');
            var key = Console.ReadKey(true);
            if (Keymap.Matches(Exit, key))
            {
                loop = false;
                continue;
            }

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
                HandleUpKey();
                continue;
            }

            if (Keymap.Matches(DownKey, key))
            {
                HandleDownKey();
                continue;
            }

            if (Keymap.Matches(SearchKey, key))
            {
                HandleSearch();
            }
        }
        return selected as T;
    }

    private void HandleUpKey()
    {
        // Should not become negative
        if (SearchResultIndex > 0)
        {
            --SearchResultIndex;
            // Should not become less than first index
            if (BufferIndex > ChoicesFirstIndex)
                Tui.UpdateCell(BufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
            else RenderPrev();
        }
    }

    private void HandleDownKey()
    {
        // Should not become higher than count
        if (SearchResultIndex + 1 < SearchResultChoices.Count)
        {
            ++SearchResultIndex;
            // Should not become higher than last index
            if (BufferIndex + 1 < ChoicesLastIndex)
                Tui.UpdateCell(BufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
            else RenderNext();
        }
    }

    private void HandleSearch()
    {
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        this.KeymapsSearchToggle().DrawHelp();
        int searchWidth = SearchString.Length + 1;
        bool search = true;

        while (search)
        {
            RenderSearch();
            LastSearchStringLength = SearchString.Length;

            Console.SetCursorPosition(searchWidth, SearchInputIndex);
            var searchKey = Console.ReadKey(true);

            if (Keymap.Matches(Delete, searchKey))
            {
                if (searchWidth > 1 && SearchString.Length > 0)
                {
                    SearchString = SearchString[0..(SearchString.Length - 1)];
                    Tui.UpdateCell(SearchInputIndex, --searchWidth, Constants.EmptyChar);
                    Search();
                }
                continue;
            }

            if (searchKey.Key == ConsoleKey.Escape)
            {
                KeymapsSearchToggle().DrawHelp();
                search = false;
                continue;
            }

            if (searchKey.Modifiers != ConsoleModifiers.Control)
            {
                SearchString += searchKey.KeyChar;
                Tui.UpdateCell(SearchInputIndex, searchWidth++, searchKey.KeyChar);
                Search();
            }
        }
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = ChoicesFirstIndex;
        SearchResultIndex = 0;
    }
}
