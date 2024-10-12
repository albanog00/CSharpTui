using System.Diagnostics;
using System.Text;
using CSharpTui.Keymaps;
using CSharpTui.UI;

namespace CSharpTui.Prompts;

public class SelectionPrompt<T> : Prompt<T>
    where T : class
{
    private Func<T, string> StringConverter { get; set; } = x => x.ToString()!;

    // Choices
    private IList<T> Choices { get; set; } = [];
    private IList<KeyValuePair<int, string>> ConvertedChoices = [];
    private IList<KeyValuePair<int, string>> SearchResultChoices = [];

    // Keys
    private Keymap SelectKey { get; set; } = new();
    private Keymap UpKey { get; set; } = new();
    private Keymap DownKey { get; set; } = new();
    private Keymap SearchKey { get; set; } = new();
    private Keymap StopSearch { get; set; } = new();
    private Keymap Delete { get; set; } = new();
    private Keymap Exit { get; set; } = new();

    // UI Indexes
    private int HeaderIndex { get; set; }
    private int HelpIndex { get; set; }
    private int CountIndex { get; set; }
    private int ChoicesFirstIndex { get; set; }
    private int ChoicesLastIndex { get; set; }
    private int BufferIndex { get; set; }
    private int SearchInputIndex { get; set; }

    // Search
    private StringBuilder SearchString { get; set; } = new();
    private int SearchResultIndex { get; set; } = 0;

    // Options
    private int ItemsOnScreen { get; set; } = 20;

    public SelectionPrompt(Tui tui)
        : base(tui)
    {
        HeaderIndex = 2;

        ChoicesFirstIndex = HeaderIndex + 2;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;

        BufferIndex = ChoicesFirstIndex;

        HelpIndex = ChoicesLastIndex + 2;
        CountIndex = HelpIndex - 1;
        SearchInputIndex = HelpIndex + 2;

        InitializeKeymaps();
    }

    public SelectionPrompt()
        : this(new Tui()) { }

    private void Draw()
    {
        Tui.Draw();
        DrawHelp();
        Search();
    }

    private void DrawCount()
    {
        Tui.UpdateLine(
            CountIndex,
            $"{SearchResultChoices.Count}/{ConvertedChoices.Count}",
            Constants.PosXStartIndex
        );
    }

    private void DrawHelp()
    {
        string help = Keymap.GetHelpString(
            [SelectKey, UpKey, DownKey, SearchKey, StopSearch, Exit]
        );
        Tui.UpdateLine(HelpIndex, help, Constants.PosXStartIndex);
    }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        foreach (var choice in choices)
        {
            int index;
            lock (Choices)
            {
                Choices.Add(choice);
                index = Choices.Count - 1;
            }

            AddConvertedChoice(choice, index);
        }

        Search();

        return this;
    }

    public SelectionPrompt<T> SetConverter(Func<T, string> converter)
    {
        StringConverter = converter;
        return this;
    }

    public SelectionPrompt<T> SetItemsOnScreen(int items)
    {
        Debug.Assert(items > 0, "argument should be greater than 0");

        Tui.ResetRange(ChoicesFirstIndex, SearchInputIndex);

        ItemsOnScreen = items;
        ChoicesLastIndex = ChoicesFirstIndex + ItemsOnScreen;

        BufferIndex = ChoicesFirstIndex;

        HelpIndex = ChoicesLastIndex + 2;
        CountIndex = HelpIndex - 1;
        SearchInputIndex = HelpIndex + 2;

        Draw();
        return this;
    }

    private void InitializeKeymaps()
    {
        SelectKey = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Select");
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K]).SetHelp("Up/k", "Go up");
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J]).SetHelp("Down/j", "Go down");
        SearchKey = Keymap
            .Bind([ConsoleKey.Q])
            .SetIsControl(true)
            .SetHelp("Ctrl-q", "Start Search");
        StopSearch = Keymap
            .Bind([ConsoleKey.Escape])
            .SetDisabled(true)
            .SetHelp("Esc", "Stop Search");
        Exit = Keymap.Bind([ConsoleKey.Q]).SetIsShift(true).SetHelp("Q", "Exit");
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
    }

    private void KeymapsSearchToggle()
    {
        UpKey.SetDisabled(!UpKey.Disabled);
        DownKey.SetDisabled(!DownKey.Disabled);
        SelectKey.SetDisabled(!SelectKey.Disabled);
        SearchKey.SetDisabled(!SearchKey.Disabled);
        StopSearch.SetDisabled(!StopSearch.Disabled);
        Exit.SetDisabled(!Exit.Disabled);
    }

    private void AddConvertedChoice(T choice, int count)
    {
        ConvertedChoices.Add(new(count, StringConverter(choice)));
    }

    private void Search(bool fromSource = true)
    {
        lock (SearchResultChoices)
        {
            SearchResultChoices = (
                !fromSource && SearchString.Length > 0 ? SearchResultChoices : ConvertedChoices
            )
                .AsParallel()
                .Where(x =>
                    x.Value.Contains(SearchString.ToString(), StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            if (fromSource || SearchResultChoices.Count < ItemsOnScreen)
            {
                RenderSearch();
            }
        }

        DrawCount();
    }

    private void RenderSearch(int index = 0)
    {
        int posX = Constants.PosXStartIndex + 2;
        int height = ChoicesFirstIndex;

        Tui.ResetRange(height, ChoicesLastIndex + 1);
        for (int i = index; i < SearchResultChoices.Count && height < ChoicesLastIndex; ++i)
        {
            Tui.UpdateLine(height++, SearchResultChoices[i].Value, posX);
        }
    }

    private void RenderNext() => RenderSearch(Math.Max(0, SearchResultIndex + 1 - ItemsOnScreen));

    private void RenderPrev() => RenderSearch(SearchResultIndex);

    public override T? Show(string prompt)
    {
        Tui.UpdateLine(Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);
        Draw();

        var result = HandleInput();

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
        if (SearchResultIndex > 0)
        {
            --SearchResultIndex;
            if (BufferIndex > ChoicesFirstIndex)
            {
                Tui.UpdateCell(BufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
            }
            else
            {
                RenderPrev();
            }
        }
    }

    private void HandleDownKey()
    {
        if (SearchResultIndex + 1 < SearchResultChoices.Count)
        {
            ++SearchResultIndex;
            if (BufferIndex + 1 < ChoicesLastIndex)
            {
                Tui.UpdateCell(BufferIndex++, Constants.PosXStartIndex, Constants.EmptyChar);
            }
            else
            {
                RenderNext();
            }
        }
    }

    private void HandleSearch()
    {
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        KeymapsSearchToggle();
        DrawHelp();
        int searchWidth = SearchString.Length + 1;
        bool search = true;

        while (search)
        {
            RenderSearch();

            Console.SetCursorPosition(searchWidth, SearchInputIndex);
            var searchKey = Console.ReadKey(true);

            if (Keymap.Matches(Delete, searchKey))
            {
                if (SearchString.Length > 0)
                {
                    SearchString.Remove(SearchString.Length - 1, 1);
                    Tui.UpdateCell(SearchInputIndex, --searchWidth, Constants.EmptyChar);
                    Search();
                }
                continue;
            }

            if (searchKey.Key == ConsoleKey.Escape)
            {
                KeymapsSearchToggle();
                DrawHelp();
                search = false;
                continue;
            }

            if (searchKey.Modifiers != ConsoleModifiers.Control)
            {
                SearchString.Append(searchKey.KeyChar);
                Tui.UpdateCell(SearchInputIndex, searchWidth++, searchKey.KeyChar);
                Search(false);
            }
        }
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = ChoicesFirstIndex;
        SearchResultIndex = 0;
    }
}
