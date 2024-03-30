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

Func<Func<int>> countFunction = () =>
{
    int i = 0;
    return () => ++i;
};
Func<int> displayChoiceNumber = countFunction();

var selectionPrompt = new SelectionPrompt<string>();
var value = selectionPrompt
    .SetTitle("Selection")
    .AddChoices(choices)
    .SetConverter(x => $"{displayChoiceNumber()}. {x.ToLower()}")
    .Show("Pick a choice");

Console.WriteLine(value);
