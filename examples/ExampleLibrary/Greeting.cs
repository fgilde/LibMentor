namespace ExampleLibrary;

/// <summary>Creates a greeting for a library consumer.</summary>
public static class Greeting
{
    /// <summary>Returns a greeting for the supplied name.</summary>
    public static string For(string name) => $"Hello, {name}!";
}
