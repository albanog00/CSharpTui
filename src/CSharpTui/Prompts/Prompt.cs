using CSharpTui.UI;

namespace CSharpTui.Prompts;

public abstract class Prompt<T>
    where T : class
{
    protected Tui Tui { get; set; }

    public Prompt(Tui tui)
    {
        Tui = tui;
    }

    public abstract T? Show(string prompt);
}
