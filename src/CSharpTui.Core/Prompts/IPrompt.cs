namespace CSharpTui.Core.Prompts;

public interface IPrompt<T> where T : class
{
    public T? Show(string prompt);
    public Task<T?> ShowAsync(string prompt, CancellationTokenSource tokenSource);
}

