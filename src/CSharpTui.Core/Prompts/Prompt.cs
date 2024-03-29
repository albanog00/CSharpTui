namespace CSharpTui.Core.Prompts;

public abstract class Prompt<T> : IPrompt<T>
    where T : notnull
{
    private Tui _tui { get; set; }

    public Prompt(Tui tui)
    {
        _tui = tui;
        InitializeKeymaps();
    }

    public abstract T? Show(string prompt);
    public abstract void InitializeKeymaps();

    public int GetHeight() => _tui.Height;
    public int GetWidth() => _tui.Width;
    public string GetTitle() => _tui.Title;

    public virtual void Draw() => _tui.Draw();
    public virtual void Clear() => Console.Clear();

    public void UpdateCell(int height, int x, char value) => _tui.UpdateCell(height, x, value);

    public void UpdateRange(int height, int x, char[] value) => _tui.UpdateRange(height, x, value);
    public void UpdateRange(int height, int x, string value) => UpdateRange(height, x, value.ToCharArray());

    public void UpdateLine(int height, char[] value) => _tui.UpdateLine(height, value);
    public void UpdateLine(int height, string value) => UpdateLine(height, value.ToCharArray());

    public Prompt<T> SetTitle(string title)
    {
        _tui.ResetTitle();

        _tui.Title = title;
        _tui.UpdateTitle();
        _tui.DrawLine(0);
        return this;
    }
}
