using CSharpTui.Core.Prompts;

// var inputPrompt = new InputPrompt();
// var value = inputPrompt
//     .SetTitle("FileFinder")
//     .Show("Does this works?");
//

var choices = new string[] {
    "A",
    "B",
    "C"
};

var selectionPrompt = new SelectionPrompt<string>();
var value = selectionPrompt
    .SetTitle("Selection")
    .AddChoices(choices)
    .Show("Pick a choice");

Console.WriteLine(value);
