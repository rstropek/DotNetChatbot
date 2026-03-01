using System.Reflection;

namespace ChatBot;

/// <summary>
/// Singleton service that loads and caches the developer (system) message from the embedded resource.
/// </summary>
public class DeveloperMessageProvider
{
    private static readonly Lazy<Task<string>> _message = new(LoadAsync);

    public Task<string> GetAsync() => _message.Value;

    private static async Task<string> LoadAsync()
    {
        const string resourceName = "ChatBot.developer-message.md";
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Resource {resourceName} not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
