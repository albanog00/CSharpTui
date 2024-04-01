namespace CSharpTui.Core.Prompts;

public abstract class Prompt<T> : IPrompt<T>
    where T : notnull
{
    protected Tui Tui { get; set; }

    public Prompt(Tui tui)
    {
        Tui = tui;
    }

    public abstract T Show(string prompt);
}
