using TUI.Core;

var tui = new Tui("FileFinder");
var value = tui.Draw()
    .InputPrompt("Prompt");

Console.WriteLine(value);
