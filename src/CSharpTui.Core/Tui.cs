namespace CSharpTui.Core;

public class Tui
{
    private char[][] Buffer;
    public int Width;
    public int Height;
    public string Title;

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

    public Tui()
        : this(string.Empty, Console.WindowWidth, Console.WindowHeight - 2) { }

    public Tui Draw()
    {
        Console.Clear();
        UpdateBorders();
        UpdateCorners();
        UpdateTitle();

        for (int i = 0; i < Height; ++i)
        {
            Console.WriteLine(Buffer[i]);
        }
        return this;
    }

    public void UpdateBorders()
    {
        for (int i = 0; i < Height; ++i)
        {
            for (int j = 0; j < Width; ++j)
            {
                if (i == 0 || i == Height - 1)
                {
                    Buffer[i][j] = Constants.HorizontalChar;
                }
                else if (j == 0 || j == Width - 1)
                {
                    Buffer[i][j] = Constants.VerticalChar;
                }
                else
                {
                    Buffer[i][j] = Constants.EmptyChar;
                }
            }
        }
    }

    public void UpdateCorners()
    {
        Buffer[0][0] = Constants.TopLeft;
        Buffer[0][Width - 1] = Constants.TopRight;
        Buffer[Height - 1][0] = Constants.BottomLeft;
        Buffer[Height - 1][Width - 1] = Constants.BottomRight;
    }

    public void UpdateTitle()
    {
        int middleTop = Width / 2;
        int startIndex = middleTop - (Title.Length / 2);
        int endIndex = startIndex + Title.Length;

        for (int i = startIndex; i < endIndex; ++i)
        {
            Buffer[0][i] = Title[i - startIndex];
        }
    }

    public void DrawLine(int line)
    {
        Console.SetCursorPosition(0, line);
        Console.WriteLine(Buffer[line]);
    }

    public void DrawCell(int height, int x)
    {
        Console.SetCursorPosition(x, height);
        Console.Write(Buffer[height][x]);
    }

    public void UpdateCell(int height, int x, char value)
    {
        Buffer[height][x] = value;
        DrawCell(height, x);
    }

    public void UpdateLine(int height, char[] value)
    {
        Buffer[height] = value;
        DrawLine(height);
    }
}
