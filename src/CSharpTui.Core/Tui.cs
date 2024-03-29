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
            for (int j = 0; j < Width; ++j)
            {
                Buffer[i][j] = Constants.EmptyChar;
            }
        }
    }

    public Tui(string title)
        : this(title, Console.WindowWidth, Console.WindowHeight - 1) { }

    public Tui Draw()
    {
        this.DrawBorders()
            .DrawTitle();

        Console.SetCursorPosition(0, 0);
        for (int i = 0; i < Height; ++i)
        {
            Console.WriteLine(Buffer[i]);
        }
        return this;
    }

    public Tui Clear()
    {
        for (int i = 0; i < Height; ++i)
        {
            for (int j = 0; j < Width; ++j)
            {
                Buffer[i][j] = Constants.EmptyChar;
            }
        }
        return Draw();
    }

    public Tui DrawBorders()
    {
        for (int i = 0; i < Width; ++i)
        {
            Buffer[0][i] = Constants.HorizontalChar;
        }
        for (int i = 0; i < Width; ++i)
        {
            Buffer[Height - 1][i] = Constants.HorizontalChar;
        }
        for (int i = 0; i < Height; ++i)
        {
            Buffer[i][0] = Constants.VerticalChar;
        }
        for (int i = 0; i < Height; ++i)
        {
            Buffer[i][Width - 1] = Constants.VerticalChar;
        }

        DrawCorners();
        return this;
    }

    public Tui DrawCorners()
    {
        Buffer[0][0] = Constants.TopLeft;
        Buffer[0][Width - 1] = Constants.TopRight;
        Buffer[Height - 1][0] = Constants.BottomLeft;
        Buffer[Height - 1][Width - 1] = Constants.BottomRight;
        return this;
    }

    public Tui DrawTitle()
    {
        int middleTop = Width / 2;
        int startIndex = middleTop - (Title.Length / 2);
        int endIndex = startIndex + Title.Length;

        for (int i = startIndex; i < endIndex; ++i)
        {
            Buffer[0][i] = Title[i - startIndex];
        }
        return this;
    }

    public Tui ResetTitle()
    {
        int middleTop = Width / 2;
        int startIndex = middleTop - (Title.Length / 2);
        int endIndex = startIndex + Title.Length;

        for (int i = startIndex; i < endIndex; ++i)
        {
            Buffer[0][i] = Constants.HorizontalChar;
        }
        return this;
    }

    public Tui DrawLine(int line)
    {
        Console.SetCursorPosition(0, line);
        Console.WriteLine(Buffer[line]);
        return this;
    }

    public Tui DrawCell(int height, int x)
    {
        Console.SetCursorPosition(x, height);
        Console.Write(Buffer[height][x]);
        return this;
    }

    public Tui UpdateCell(int height, int x, char value)
    {
        Buffer[height][x] = value;
        DrawCell(height, x);
        return this;
    }

    public Tui UpdateRange(int height, int x, char[] value)
    {
        for (int i = 0; i < value.Length; ++i)
        {
            UpdateCell(height, x + i, value[i]);
        }
        return this;
    }

    public Tui UpdateRange(int height, int x, string value) =>
        UpdateRange(height, x, value.ToCharArray());

    public Tui UpdateLine(int height, char[] value)
    {
        Buffer[height] = value;
        DrawLine(height);
        return this;
    }

    public Tui UpdateLine(int height, string value) =>
        UpdateLine(height, value.ToCharArray());
}
