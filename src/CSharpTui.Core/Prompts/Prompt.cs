namespace CSharpTui.Core.Prompts;

public abstract class Prompt<T> : IPrompt<T>
    where T : class
{
    protected Tui Tui { get; set; }

    public Prompt(Tui tui)
    {
        Tui = tui;
    }

    public abstract T? Show(string prompt);
    public abstract Task<T?> ShowAsync(string prompt, CancellationTokenSource tokenSource);
}
