namespace CSharpTui.Core;

public class Tui
{
    private char[][] Buffer;
    public int Width;
    public int Height;

    public Tui(int width = 80, int height = 24)
    {
        Width = width;
        Height = height;

        Buffer = new char[Height][];
        for (int i = 0; i < Height; ++i)
            Buffer[i] = new char[Width];
    }

    public Tui() : this(Console.WindowWidth, Console.WindowHeight - 1) { }

    public Tui Draw()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        for (int i = 0; i < Height; ++i)
            Console.WriteLine(Buffer[i]);
        return this;
    }

    public Tui ResetRange(int startHeight, int endHeight)
    {
        for (int i = startHeight; i < endHeight; ++i)
        {
            Buffer[i] = new char[Width];
            DrawLine(i);
        }
        return this;
    }

    public Tui Clear()
    {
        Buffer = new char[Height][];
        for (int i = 0; i < Height; ++i)
            Buffer[i] = new char[Width];

        Console.SetCursorPosition(0, 0);
        Console.Clear();
        return this;
    }

    public Tui DrawLine(int line)
    {
        Console.SetCursorPosition(0, line);
        Console.Write(new string(' ', Width));

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

    public Tui UpdateLineRange(int height, char[] value, int padding = 0)
    {
        for (int i = 0; i < padding; ++i)
            UpdateCell(height, i, Constants.EmptyChar);

        for (int j = 0; j < value.Length && j <= Width; ++j)
            UpdateCell(height, padding + j, value[j]);
        return DrawLine(height);
    }

    public Tui UpdateLineRange(int height, string value, int padding = 0) =>
        UpdateLineRange(height, value.ToCharArray(), padding);

    public Tui UpdateLine(int height, char[] value, int padding = 0)
    {
        char[] line = new char[value.Length + padding];

        for (int i = 0; i < padding; ++i)
            line[i] = ' ';

        for (int i = 0; i < value.Length; ++i)
            line[padding + i] = value[i];

        Buffer[height] = line;
        DrawLine(height);
        return this;
    }

    public Tui UpdateLine(int height, string value, int padding = 0) =>
        UpdateLine(height, value.ToCharArray(), padding);
}
