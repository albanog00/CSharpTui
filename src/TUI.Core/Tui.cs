namespace TUI.Core;

public class Tui
{
    private char[][] Buffer;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Title { get; private set; }

    public int HelpHeight { get; set; }
    public int PromptHeight { get; set; }

    const char HorizontalChar = '-';
    const char VerticalChar = '|';
    const char EmptyChar = ' ';

    const int InputPromptStartIndex = 3;

    public Tui(string title, int width = 80, int height = 24)
    {
        Title = title;
        Width = width;
        Height = height;

        Buffer = new char[Height][];
        for (int i = 0; i < Height; ++i)
        {
            Buffer[i] = new char[Width];
        }

        HelpHeight = Height - 3;
        PromptHeight = Height - 5;
    }

    public Tui(string title)
        : this(title, Console.WindowWidth, Console.WindowHeight - 2) { }

    public Tui Draw()
    {
        Console.Clear();
        DrawBorders();

        for (int i = 0; i < Height; ++i)
        {
            Console.WriteLine(Buffer[i]);
        }
        return this;
    }

    private void DrawBorders()
    {
        for (int i = 0; i < Height; ++i)
        {
            for (int j = 0; j < Width; ++j)
            {
                if (i == 0 || i == Height - 1)
                {
                    Buffer[i][j] = HorizontalChar;
                }
                else if (j == 0 || j == Width - 1)
                {
                    Buffer[i][j] = VerticalChar;
                }
                else
                {
                    Buffer[i][j] = EmptyChar;
                }
            }
        }
        DrawTitle();
    }

    private void DrawTitle()
    {
        int middleTop = Width / 2;
        int startIndex = middleTop - (Title.Length / 2);
        int endIndex = startIndex + Title.Length;

        for (int i = startIndex; i < endIndex; ++i)
        {
            Buffer[0][i] = Title[i - startIndex];
        }
    }

    private void DrawHelp()
    {

    }

    private void UpdateLine(int line)
    {
        Console.SetCursorPosition(0, line);
        Console.WriteLine(Buffer[line]);
    }

    private void UpdateCell(int y, int x)
    {
        Console.SetCursorPosition(x, y);
        Console.Write(Buffer[y][x]);
    }

    public string InputPrompt(string prompt)
    {
        string answer = string.Empty;

        int promptXStartIndex = InputPromptStartIndex;
        int promptXLastIndex = promptXStartIndex + prompt.Length;
        for (int i = promptXStartIndex; i < promptXLastIndex; ++i)
        {
            Buffer[PromptHeight][i] = prompt[i - promptXStartIndex];
        }

        this.UpdateLine(PromptHeight);
        int posX = promptXLastIndex + 1;

        bool loop = true;
        while (loop)
        {
            Console.SetCursorPosition(posX, PromptHeight);
            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.Enter or ConsoleKey.Escape:
                    loop = false;
                    break;
                case ConsoleKey.Backspace:
                    if (posX - 1 > promptXLastIndex)
                    {
                        answer = answer[0..(answer.Length - 1)];
                        Buffer[PromptHeight][--posX] = ' ';
                        this.UpdateCell(PromptHeight, posX);
                    }
                    break;
                default:
                    answer += key.KeyChar;
                    Buffer[PromptHeight][posX++] = key.KeyChar;
                    this.UpdateCell(PromptHeight, posX);
                    break;
            };
        }

        Console.Clear();
        return new(answer.ToArray());
    }
}
