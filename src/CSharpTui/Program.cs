﻿using CSharpTui.Core.Prompts;

// var inputPrompt = new InputPrompt();
// var value = inputPrompt
//     .SetTitle("FileFinder")
//     .Show("Does this works?");
//

var choices = new string[] {
    "A",
    "B",
    "C",
    "D",
    "E",
    "F"
};

Func<Func<int>> countFunction = () =>
{
    int i = 0;
    return () => ++i;
};
Func<int> displayChoiceNumber = countFunction();

var selectionPrompt = new SelectionPrompt<string>();
var value = selectionPrompt
    .AddChoices(choices)
    .SetConverter(x => $"{displayChoiceNumber()}. {x.ToLower()}")
    .SetItemsOnScreen(5)
    .Show("Pick a choice");

Console.WriteLine(value);
