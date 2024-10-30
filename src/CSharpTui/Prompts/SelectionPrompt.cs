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
    private Keymap PageUp { get; set; } = new();
    private Keymap PageDown { get; set; } = new();
    private Keymap Home { get; set; } = new();
    private Keymap End { get; set; } = new();
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
        Tui.ResetRange(HelpIndex, HelpIndex);
        Tui.UpdateLine(HelpIndex, help, Constants.PosXStartIndex);
    }

    public SelectionPrompt<T> AddChoices(IList<T> choices)
    {
        lock (Choices)
        {
            foreach (var choice in choices)
            {
                int index;
                Choices.Add(choice);
                index = Choices.Count - 1;

                AddConvertedChoice(choice, index);
            }
        }
        Search();
        return this;
    }

    public SelectionPrompt<T> AddChoice(T choice)
    {
        int index;
        lock (Choices)
        {
            Choices.Add(choice);
            index = Choices.Count - 1;
        }
        AddConvertedChoice(choice, index);
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
        SelectKey = Keymap.Bind([ConsoleKey.Enter])/* .SetHelp("Enter", "Select") */;
        UpKey = Keymap.Bind([ConsoleKey.UpArrow, ConsoleKey.K])/* .SetHelp("Up/k", "Go up") */;
        DownKey = Keymap.Bind([ConsoleKey.DownArrow, ConsoleKey.J])/* .SetHelp("Down/j", "Go down") */;
        PageUp = Keymap.Bind([ConsoleKey.PageUp]);
        PageDown = Keymap.Bind([ConsoleKey.PageDown]);
        Home = Keymap.Bind([ConsoleKey.Home]);
        End = Keymap.Bind([ConsoleKey.End]);
        SearchKey = Keymap.Bind([ConsoleKey.F]).SetIsControl(true).SetHelp("Ctrl-f", "Start Search");
        StopSearch = Keymap.Bind([ConsoleKey.Escape]).SetDisabled(true).SetHelp("Esc", "Stop Search");
        Exit = Keymap.Bind([ConsoleKey.Q]).SetIsShift(true).SetHelp("Q", "Exit");
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
    }

    private void KeymapsSearchToggle()
    {
        UpKey.SetDisabled(!UpKey.Disabled);
        DownKey.SetDisabled(!DownKey.Disabled);
        PageUp.SetDisabled(!PageUp.Disabled);
        PageDown.SetDisabled(!PageDown.Disabled);
        Home.SetDisabled(!End.Disabled);
        End.SetDisabled(!Home.Disabled);
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

            if (fromSource || SearchResultChoices.Count - SearchResultIndex < ItemsOnScreen)
            {
                if (StopSearch.Disabled) RenderSearch();
                else RenderSearch(0);
            }
        }

        DrawCount();
    }

    private void RenderSearch(int? index = null)
    {
        int start = index ?? SearchResultIndex;
        int posX = Constants.PosXStartIndex + 2;
        int height = ChoicesFirstIndex;

        Tui.ResetRange(height, ChoicesLastIndex + 1);
        while (start < SearchResultChoices.Count && height < ChoicesLastIndex)
        {
            Tui.UpdateLine(height++, SearchResultChoices[start++].Value, posX);
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
        while (true)
        {
            Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, '>');
            var key = Console.ReadKey(true);
            if (Keymap.Matches(Exit, key))
                break;

            else if (Keymap.Matches(SelectKey, key))
            {
                if (SearchResultChoices.Count <= 0)
                    continue;

                selected = Choices[SearchResultChoices[SearchResultIndex].Key];
                break;
            }

            else if (Keymap.Matches(UpKey, key))
                HandleUpKey();

            else if (Keymap.Matches(DownKey, key))
                HandleDownKey();

            else if (Keymap.Matches(PageUp, key))
                HandlePageUp();

            else if (Keymap.Matches(PageDown, key))
                HandlePageDown();

            else if (Keymap.Matches(Home, key))
                HandleHome();

            else if (Keymap.Matches(End, key))
                HandleEnd();

            else if (Keymap.Matches(SearchKey, key))
                HandleSearch();
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

    private void HandlePageUp()
    {
        SearchResultIndex = Math.Max(0, SearchResultIndex - ItemsOnScreen);
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = ChoicesFirstIndex;
        RenderSearch(SearchResultIndex);
    }

    private void HandlePageDown()
    {
        SearchResultIndex = Math.Min(SearchResultChoices.Count - 1, SearchResultIndex + ItemsOnScreen);
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = Math.Min(ChoicesFirstIndex + SearchResultChoices.Count, ChoicesLastIndex - 1);
        RenderSearch(SearchResultIndex - ItemsOnScreen + 1);
    }

    private void HandleHome()
    {
        SearchResultIndex = 0;
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = ChoicesFirstIndex;
        RenderSearch(SearchResultIndex);
    }

    private void HandleEnd()
    {
        SearchResultIndex = SearchResultChoices.Count - 1;
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        BufferIndex = Math.Min(ChoicesFirstIndex + SearchResultChoices.Count, ChoicesLastIndex - 1);
        RenderSearch(SearchResultIndex - ItemsOnScreen + 1);
    }

    private void HandleSearch()
    {
        Tui.UpdateCell(BufferIndex, Constants.PosXStartIndex, Constants.EmptyChar);
        KeymapsSearchToggle();
        DrawHelp();
        int searchWidth = SearchString.Length + 1;
        while (true)
        {
            RenderSearch();
            Console.SetCursorPosition(searchWidth, SearchInputIndex);
            var searchKey = Console.ReadKey(true);
            if (searchKey.Key == ConsoleKey.Escape)
            {
                KeymapsSearchToggle();
                DrawHelp();
                break;
            }

            else if (Keymap.Matches(Delete, searchKey))
            {
                if (SearchString.Length > 0)
                {
                    SearchString.Remove(SearchString.Length - 1, 1);
                    Tui.UpdateCell(SearchInputIndex, --searchWidth, Constants.EmptyChar);
                    Search();
                }
            }

            else if (searchKey.Modifiers != ConsoleModifiers.Control)
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
