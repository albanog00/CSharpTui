using CSharpTui.Core;

var inputPrompt = new InputPrompt();
var value = inputPrompt
    .SetTitle("FileFinder")
    .Show("Does this works?");
