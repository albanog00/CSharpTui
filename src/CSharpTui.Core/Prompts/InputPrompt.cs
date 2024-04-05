using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class InputPrompt : Prompt<string>
{
    private int PromptHeight { get; set; }
    private int HelpHeight { get; set; }

    private Keymap SendInput { get; set; } = new();
    private Keymap Delete { get; set; } = new();
    private Keymap ResetInput { get; set; } = new();
    private Keymap Exit { get; set; } = new();

    public InputPrompt(Tui tui) : base(tui)
    {
        PromptHeight = Tui.Height - 5;
        HelpHeight = Tui.Height - 3;
        InitializeKeymaps();
    }

    public InputPrompt(string title) : this(new Tui()) { }
    public InputPrompt() : this(string.Empty) { }

    public void Draw()
    {
        Tui.Draw();
        DrawHelp();
    }

    public void DrawHelp()
    {
        string help = Keymap.GetHelpString([
                SendInput,
                Delete,
                ResetInput
        ]);

        int startIndex = Constants.PosXStartIndex;
        int endIndex = help.Length + startIndex;
        Tui.UpdateLineRange(HelpHeight, help, Constants.PosXStartIndex);
    }

    public void InitializeKeymaps()
    {
        SendInput = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Send Input");
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
        ResetInput = Keymap.Bind([ConsoleKey.R]).SetIsControl(true).SetHelp("Ctrl-R", "Reset");
        Exit = Keymap.Bind([ConsoleKey.Q]).SetIsShift(true).SetHelp("Q", "Exit");
    }

    public override string? Show(string prompt)
    {
        return this.ShowAsync(prompt, new()).GetAwaiter().GetResult();
    }

    public void DrawAnswer(string answer)
    {
        answer = "Your answer is: " + answer;
        Tui.UpdateLineRange(2, answer, Constants.PosXStartIndex);
        Console.SetCursorPosition(0, Tui.Height);
    }

    public override async Task<string?> ShowAsync(string prompt, CancellationTokenSource tokenSource)
    {
        await Task.Run(() => this.Draw());

        string answer = string.Empty;
        int posStartX = Constants.PosXStartIndex;
        int posEndX = posStartX + prompt.Length;

        Tui.UpdateLineRange(PromptHeight, prompt, posStartX);
        int posX = posEndX + 1;

        Console.SetCursorPosition(posX, PromptHeight);
        bool loop = true;
        while (loop)
        {
            Console.SetCursorPosition(posX, PromptHeight);
            var key = Console.ReadKey(true);

            if (Keymap.Matches([SendInput, Exit], key))
            {
                loop = false;
                continue;
            }

            if (Keymap.Matches(Delete, key))
            {
                if (posX > posEndX && answer.Length > 0)
                {
                    answer = answer[0..(answer.Length - 1)];
                    Tui.UpdateCell(PromptHeight, --posX, Constants.EmptyChar);
                }
                continue;
            }

            if (Keymap.Matches(ResetInput, key))
            {
                posX = posEndX + 1;
                Tui.UpdateLineRange(PromptHeight,
                    new string(Constants.EmptyChar, answer.Length + 1),
                    posEndX + 1);
                answer = string.Empty;

                continue;
            }

            if (key.Modifiers != ConsoleModifiers.Control)
            {
                answer += key.KeyChar;
                Tui.UpdateCell(PromptHeight, posX++, key.KeyChar);
            }
        }
        tokenSource.Cancel();
        Tui.Clear();

        return answer;
    }
}
