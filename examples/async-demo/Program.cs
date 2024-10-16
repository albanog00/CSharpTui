using CSharpTui.Prompts;

Func<Func<int>> countFunction = () =>
{
    int i = 0;
    return () => ++i;
};
Func<int> displayChoiceNumber = countFunction();

var choices = new string[] { "A", "B", "C", "D", "E", "F" };
var selectionPrompt = new SelectionPrompt<string>();
for (int i = 0; i < choices.Length; ++i)
{
    var val = i;
    Thread thread =
        new(() =>
        {
            Thread.Sleep(TimeSpan.FromSeconds(val + 1));
            selectionPrompt.AddChoice(choices[val]);
        });

    thread.Start();
}

var value = selectionPrompt
    .SetConverter(x => $"{displayChoiceNumber()}. {x.ToLower()}")
    .SetItemsOnScreen(5)
    .Show("Pick a choice");

Console.WriteLine(value);
