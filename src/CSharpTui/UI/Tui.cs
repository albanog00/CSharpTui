namespace CSharpTui.UI;

public class Tui
{
    private MutexBuffer Buffer;
    public int Cols;
    public int Rows;

    private object _locker = new object();

    public Tui(int cols = 80, int rows = 24)
    {
        Cols = cols;
        Rows = rows;
        Buffer = new(rows, cols);
    }

    public Tui()
        : this(Console.WindowWidth, Console.WindowHeight - 1) { }

    public void Draw()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);
        var slice = Buffer.Slice(0, Buffer.Rows);

        lock (_locker)
        {
            foreach (var line in slice)
            {
                Console.WriteLine(line);
            }
        }
    }

    public void ResetRange(int startRow, int endRow)
    {
        Buffer.ClearLines(startRow, endRow);
        lock (_locker)
        {
            DrawLinesRange(startRow, endRow);
        }
    }

    public void Clear()
    {
        Buffer.Clear();
        lock (_locker)
        {
            Console.SetCursorPosition(0, 0);
            Console.Clear();
        }
    }

    public void DrawLinesRange(int startRow, int endRow)
    {
        lock (_locker)
        {
            Console.SetCursorPosition(0, startRow);
            var slice = Buffer.Slice(startRow, endRow);

            foreach (var line in slice)
            {
                Console.WriteLine(line);
            }
        }
    }

    public void DrawLine(int row)
    {
        lock (_locker)
        {
            Console.SetCursorPosition(0, row);
            Console.WriteLine();

            Console.SetCursorPosition(0, row);
            Console.WriteLine(Buffer.Row(row).ToArray());
        }
    }

    public void DrawCell(int row, int col)
    {
        lock (_locker)
        {
            Console.SetCursorPosition(col, row);
            Console.Write(Buffer.Cell(row, col));
        }
    }

    public void UpdateCell(int row, int col, char value)
    {
        Buffer.UpdateCell(row, col, value);
        DrawCell(row, col);
    }

    public void UpdateLineRange(int height, ReadOnlySpan<char> value, int padding = 0)
    {
        for (int i = 0; i < padding && i < Cols; ++i)
        {
            UpdateCell(height, i, Constants.EmptyChar);
        }

        for (int j = 0; j < value.Length && j < Cols - padding; ++j)
        {
            UpdateCell(height, padding + j, value[j]);
        }

        DrawLine(height);
    }

    public void UpdateLineRange(int height, string value, int padding = 0) =>
        UpdateLineRange(height, value.ToCharArray(), padding);

    public void UpdateLine(int row, ReadOnlySpan<char> value, int padding = 0)
    {
        Buffer.UpdateLine(row, value, padding);
        DrawLine(row);
    }

    public void UpdateLine(int height, string value, int padding = 0)
    {
        UpdateLine(height, value.AsSpan(), padding);
    }
}
