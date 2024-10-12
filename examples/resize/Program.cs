using System.Reactive.Linq;

var observable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(1000));

int width = Console.WindowWidth;
int height = Console.WindowHeight;

var subscriber = observable.Subscribe(tick =>
{
    if (width != Console.WindowWidth)
    {
        width = Console.WindowWidth;
        Console.WriteLine($"Width changed: {Console.WindowWidth}");
    }

    if (height != Console.WindowHeight)
    {
        height = Console.WindowHeight;
        Console.WriteLine($"Height changed: {Console.WindowHeight}");
    }
});

Console.ReadLine();
