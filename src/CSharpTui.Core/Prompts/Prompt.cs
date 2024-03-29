namespace CSharpTui.Core.Prompts;

public abstract class Prompt<T> : IPrompt<T>
    where T : notnull
{
    public Tui Tui { get; set; }

    public Prompt(Tui tui)
    {
        Tui = tui;
        InitializeKeymaps();
    }

    public abstract T? Show(string prompt);
    public abstract void InitializeKeymaps();

    public Prompt<T> SetTitle(string title)
    {
        Tui.ResetTitle();

        Tui.Title = title;
        Tui.DrawTitle();
        Tui.DrawLine(0);
        return this;
    }
}
