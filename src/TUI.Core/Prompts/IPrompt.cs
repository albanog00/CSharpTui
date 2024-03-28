namespace TUI.Core.Prompts;

public interface IPrompt<T> where T : notnull
{
    public T? Show(string prompt);
}

