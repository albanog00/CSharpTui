using System.Diagnostics;

namespace CSharpTui.UI;

internal sealed class MutexBuffer
{
    private readonly Mutex _mutex = new();
    private char[][] _internalBuffer;

    public int Rows { get; private set; }
    public int Cols { get; private set; }

    public MutexBuffer(int rows, int cols)
    {
        Debug.Assert(rows >= 0 && rows <= Console.LargestWindowHeight, "Invalid rows provided to `CSharpTui.UI.MutexBuffer()`");
        Debug.Assert(cols >= 0 && cols <= Console.LargestWindowWidth, "Invalid cols provided to `CSharpTui.UI.MutexBuffer()`");

        Rows = rows;
        Cols = cols;

        _internalBuffer = new char[rows][];
        for (int i = 0; i < rows; ++i)
        {
            _internalBuffer[i] = new char[cols];
        }
    }

    public void UpdateCell(int row, int col, char value)
    {
        Debug.Assert(row >= 0 && row <= this.Rows);
        Debug.Assert(col >= 0 && col <= this.Cols);

        _mutex.WaitOne();
        _internalBuffer[row][col] = value;
        _mutex.ReleaseMutex();
    }

    public void UpdateLine(int row, ReadOnlySpan<char> value, int padding = 0)
    {
        Debug.Assert(row >= 0);

        _mutex.WaitOne();

        var maxIndex = Math.Min(this.Cols, padding);
        for (int i = 0; i < maxIndex; ++i)
        {
            _internalBuffer[row][padding] = ' ';
        }

        maxIndex = Math.Min(this.Cols - padding, value.Length);
        for (int i = 0; i < maxIndex; ++i)
        {
            _internalBuffer[row][i + padding] = value[i];
        }

        _mutex.ReleaseMutex();
    }

    public void ClearCell(int row, int col)
    {
        this.UpdateCell(row, col, ' ');
    }

    public void ClearLine(int row, int startCol = 0)
    {
        Debug.Assert(row >= 0 && row <= this.Rows);
        Debug.Assert(startCol >= 0 && startCol <= this.Cols);

        _mutex.WaitOne();
        this.FillLineWith(' ', row, startCol);
        _mutex.ReleaseMutex();
    }

    public void ClearLines(int startRow, int endRow)
    {
        Debug.Assert(startRow >= 0 && startRow <= this.Rows);
        Debug.Assert(endRow >= 0 && endRow <= this.Rows);

        _mutex.WaitOne();

        while (startRow <= endRow)
        {
            this.FillLineWith(' ', startRow++);
        }

        _mutex.ReleaseMutex();
    }

    public void Clear()
    {
        _mutex.WaitOne();

        for (int i = 0; i < this.Rows; ++i)
        {
            for (int j = 0; j < this.Cols; ++j)
            {
                _internalBuffer[i][j] = ' ';
            }
        }

        _mutex.ReleaseMutex();
    }

    public ReadOnlySpan<char[]> Slice(int startRow, int endRow)
    {
        Debug.Assert(startRow >= 0 && startRow <= this.Rows);
        Debug.Assert(endRow >= 0 && endRow <= this.Rows);

        _mutex.WaitOne();
        ReadOnlySpan<char[]> slice = _internalBuffer[startRow..endRow];
        _mutex.ReleaseMutex();

        return slice;
    }

    public ReadOnlySpan<char> Row(int row)
    {
        Debug.Assert(row >= 0 && row <= this.Rows);

        _mutex.WaitOne();
        ReadOnlySpan<char> slice = _internalBuffer[row];
        _mutex.ReleaseMutex();

        return slice;
    }

    public char Cell(int row, int col)
    {
        Debug.Assert(row >= 0 && row <= this.Rows);
        Debug.Assert(col >= 0 && col <= this.Cols);

        _mutex.WaitOne();
        var cell = _internalBuffer[row][col];
        _mutex.ReleaseMutex();

        return cell;
    }


    private void FillLineWith(char ch, int row, int startCol = 0)
    {
        Debug.Assert(row >= 0 && row <= this.Rows);
        Debug.Assert(startCol >= 0 && startCol <= this.Cols);

        for (int i = 0; i + startCol < this.Cols; ++i)
        {
            _internalBuffer[row][i + startCol] = ch;
        }
    }
}

