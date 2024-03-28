using TUI.Core;

var inputPrompt = new InputPrompt();
var value = inputPrompt
    .SetTitle("FileFinder")
    .Show("Does this works?");

Console.WriteLine(value);
