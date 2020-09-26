# KnockKnock
Super-simplified, event-based network broadcast discovery for C#/.NET.

## Usage
```csharp
using KnockKnock;

static void Main()
{
    WhosThere wt = new WhosThere("jeffery", 1234);
    
    // Listen for others
    wt.SomeonesHere += (sender, event) =>
    {
        Console.WriteLine($"{event.Name} exists at {event.RemoteAddress}");
    }
    
    // Broadcast that you're here
    wt.ImHere();
    
    // Stop listening for others (does not broadcast anything)
    wt.ImGone();
}
```

## License
MIT License.
