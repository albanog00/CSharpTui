namespace TUI.Core;

public class Tui
{
    private char[][] Buffer;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Title { get; private set; }

    const char HORIZONTAL_CHAR = '-';
    const char VERTICAL_CHAR = '|';
    const char EMPTY_CHAR = ' ';

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
                    Buffer[i][j] = HORIZONTAL_CHAR;
                }
                else if (j == 0 || j == Width - 1)
                {
                    Buffer[i][j] = VERTICAL_CHAR;
                }
                else
                {
                    Buffer[i][j] = EMPTY_CHAR;
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

    private void DrawLine(int line)
    {
        Console.SetCursorPosition(0, line);
        Console.WriteLine(Buffer[line]);
    }

    public string InputPrompt(string prompt)
    {
        string answer = string.Empty;
        int promptHeight = Height - 2;
        int promptWidthStartIndex = 3;
        int promptWidthEndIndex = promptWidthStartIndex + prompt.Length;

        for (int i = promptWidthStartIndex; i < promptWidthEndIndex; ++i)
        {
            Buffer[promptHeight][i] = prompt[i - promptWidthStartIndex];
        }

        this.DrawLine(promptHeight);

        int currentPosition = promptWidthEndIndex + 1;
        bool loop = true;
        while (loop)
        {
            Console.SetCursorPosition(currentPosition, promptHeight);
            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    loop = false;
                    break;
                default:
                    answer += key.KeyChar;
                    Buffer[promptHeight][currentPosition++] = key.KeyChar;
                    this.DrawLine(promptHeight);
                    break;
            };
        }

        Console.Clear();
        return answer;
    }
}
