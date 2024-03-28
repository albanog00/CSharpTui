using CSharpTui.Core.Keymaps;
using CSharpTui.Core.Prompts;

namespace CSharpTui.Core;

public class InputPrompt : Prompt<string>
{
    public int PromptHeight { get; set; }
    public int HelpHeight { get; set; }

    public InputPrompt()
        : base(new Tui())
    {
        PromptHeight = GetHeight() - 5;
        HelpHeight = GetHeight() - 3;

        Draw();
    }

    public override void Draw()
    {
        base.Draw();
        DrawHelp();
    }

    public override string Show(string prompt)
    {
        string answer = string.Empty;
        int posStartX = Constants.InputPromptStartIndex;
        int posEndX = posStartX + prompt.Length;
        for (int i = posStartX; i < posEndX; ++i)
        {
            UpdateCell(PromptHeight, i, prompt[i - posStartX]);
        }

        int posX = posEndX + 1;

        bool loop = true;
        while (loop)
        {
            Console.SetCursorPosition(posX, PromptHeight);

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    loop = false;
                    break;
                case ConsoleKey.Backspace:
                    if (posX - 1 > posEndX && answer.Length > 0)
                    {
                        answer = answer[0..(answer.Length - 1)];
                        UpdateCell(PromptHeight, --posX, Constants.EmptyChar);
                    }
                    break;
                default:
                    answer += key.KeyChar;
                    UpdateCell(PromptHeight, posX++, key.KeyChar);
                    break;
            };
        }
        Console.Clear();

        return answer;
    }

    public override void InitializeKeymaps() =>
        Keymaps = [
            Keymap.Bind([ConsoleKey.Enter], false).SetHelp("Enter", "Send input"),
            Keymap.Bind([ConsoleKey.Backspace], false)
        ];

    public void DrawHelp()
    {
        string help = Keymap.GetHelpString(Keymaps);
        int startIndex = Constants.InputPromptStartIndex;
        int endIndex = help.Length + startIndex;

        for (int i = startIndex; i < endIndex; ++i)
        {
            if (i >= GetWidth())
            {
                break;
            }
            UpdateCell(HelpHeight, i, help[i - startIndex]);
        }
    }
}
