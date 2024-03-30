using CSharpTui.Core.Keymaps;

namespace CSharpTui.Core.Prompts;

public class InputPrompt : Prompt<string>
{
    public int PromptHeight { get; set; }
    public int HelpHeight { get; set; }

    public Keymap SendInput { get; private set; } = new();
    public Keymap Delete { get; private set; } = new();
    public Keymap ResetInput { get; private set; } = new();

    public InputPrompt(Tui tui) : base(tui)
    {
        PromptHeight = Tui.Height - 5;
        HelpHeight = Tui.Height - 3;
        InitializeKeymaps();
        Draw();
    }

    public InputPrompt(string title) : this(new Tui(title)) { }
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

        Tui.UpdateRange(HelpHeight, Constants.PosXStartIndex, help);
    }

    public void InitializeKeymaps()
    {
        SendInput = Keymap.Bind([ConsoleKey.Enter]).SetHelp("Enter", "Send Input");
        Delete = Keymap.Bind([ConsoleKey.Backspace]);
        ResetInput = Keymap.Bind([ConsoleKey.R]).SetIsControl(true).SetHelp("Ctrl-R", "Reset");
    }

    public override string Show(string prompt)
    {
        string answer = string.Empty;
        int posStartX = Constants.PosXStartIndex;
        int posEndX = posStartX + prompt.Length;

        Tui.UpdateRange(PromptHeight, posStartX, prompt);
        int posX = posEndX + 1;

        Console.SetCursorPosition(posX, PromptHeight);
        bool loop = true;
        while (loop)
        {
            Console.SetCursorPosition(posX, PromptHeight);
            var key = Console.ReadKey(true);

            if (Keymap.Matches(SendInput, key))
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
                Tui.UpdateRange(PromptHeight, posEndX + 1,
                    new string(Constants.EmptyChar, answer.Length + 1));
                answer = string.Empty;

                continue;
            }

            if (key.Modifiers != ConsoleModifiers.Control)
            {
                answer += key.KeyChar;
                Tui.UpdateCell(PromptHeight, posX++, key.KeyChar);
            }
        }
        Tui.Clear();
        DrawAnswer(answer);

        return answer;
    }

    public void DrawAnswer(string answer)
    {
        answer = "Your answer is: " + answer;
        Tui.UpdateRange(2, Constants.PosXStartIndex, answer);
        Console.SetCursorPosition(0, Tui.Height);
    }
}
