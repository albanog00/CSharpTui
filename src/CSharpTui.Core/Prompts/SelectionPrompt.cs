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
    private int ChoicesListStartIndex { get; set; }
    private int ChoicesListMaxIndex { get; set; }
    private int SearchIndex { get; set; }
    private int PerPage { get; set; } = 20;

    public SelectionPrompt(Tui tui) : base(tui)
    {
        HeaderIndex = Constants.PosYStartIndex;

        ChoicesListStartIndex = HeaderIndex + 2;
        ChoicesListMaxIndex = ChoicesListStartIndex + PerPage;

        HelpIndex = ChoicesListMaxIndex + 2;
        SearchIndex = HelpIndex + 2;
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
            .Search();
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
            if (i + ChoicesListStartIndex < ChoicesListMaxIndex)
            {
                Tui.UpdateLine(i + ChoicesListStartIndex,
                        convertedString, Constants.PosXStartIndex + 2);
            }
            ConvertedChoices.Add(new(i, convertedString));
        }
        return this;
    }

    // Searchs through the cache
    private SelectionPrompt<T> Search()
    {
        int currentSize = SearchResultChoices.Count;
        int height = ChoicesListStartIndex;
        int maxIndex = Math.Min(currentSize, ChoicesListMaxIndex) + height;
        int posX = Constants.PosXStartIndex + 2;

        SearchResultChoices = [];
        Tui.ResetRange(height, maxIndex);

        // TODO: Optimize this
        foreach (var choice in ConvertedChoices)
        {
            if (choice.Value.Contains(SearchString))
            {
                if (height < ChoicesListMaxIndex)
                {
                    Tui.UpdateLine(height++, choice.Value, posX);
                }
                SearchResultChoices.Add(choice);
            }
        }
        return this;
    }

    public override T Show(string prompt)
    {
        object selected = new();

        Console.CursorVisible = false;
        this.Draw();
        Tui.UpdateLine(
                Constants.PosYStartIndex, prompt, Constants.PosXStartIndex);

        int bufferIndex = ChoicesListStartIndex;
        int index = 0;
        bool loop = true;
        while (loop)
        {
            Tui.UpdateCell(bufferIndex, Constants.PosXStartIndex, '>');

            int minIndex = ChoicesListStartIndex;
            int maxIndex = minIndex + SearchResultChoices.Count - 1;
            bool higherThanMin = bufferIndex > minIndex;
            bool lowerThanMax = bufferIndex < maxIndex;

            var key = Console.ReadKey(true);

            if (Keymap.Matches(SelectKey, key))
            {
                if (SearchResultChoices.Count <= 0)
                {
                    continue;
                }

                loop = false;
                selected = Choices[SearchResultChoices[index].Key];
                continue;
            }

            if (Keymap.Matches(UpKey, key))
            {
                if (higherThanMin)
                {
                    --index;
                    Tui.UpdateCell(bufferIndex--, Constants.PosXStartIndex, Constants.EmptyChar);
                }
                else
                {
                    Tui.UpdateCell(minIndex, Constants.PosXStartIndex, Constants.EmptyChar);
                    bufferIndex = maxIndex;
                    index = SearchResultChoices.Count - 1;
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

            if (Keymap.Matches(SearchKey, key))
            {
                Console.CursorVisible = true;

                UpKey.SetDisabled(true);
                DownKey.SetDisabled(true);
                SelectKey.SetDisabled(true);
                SearchKey.SetDisabled(true);
                StopSearch.SetDisabled(false);
                this.DrawHelp();

                int searchWidth = SearchString.Length + 1;
                bool search = true;
                while (search)
                {
                    this.Search();
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
                        SearchKey.SetDisabled(false);
                        StopSearch.SetDisabled(true);
                        this.DrawHelp();

                        search = false;
                        continue;
                    }

                    if (searchKey.Modifiers != ConsoleModifiers.Control)
                    {
                        SearchString += searchKey.KeyChar;
                        Tui.UpdateCell(SearchIndex, searchWidth++, searchKey.KeyChar);
                    }
                }
                index = 0;
                bufferIndex = ChoicesListStartIndex;
            }
        }
        Tui.Clear();
        return (T)selected;
    }
}
