using CSharpTui.Core.Prompts;

var inputPrompt = new InputPrompt();
var value = inputPrompt
    .SetTitle("FileFinder")
    .Show("Does this works?");
