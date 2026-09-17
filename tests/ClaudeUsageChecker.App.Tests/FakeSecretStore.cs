using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>A secret store in memory, so that tests leave nothing behind on the system.</summary>
internal sealed class FakeSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _entries = [];

    public bool IsSupported { get; init; } = true;

    public string? Read(string key) => _entries.GetValueOrDefault(key);

    public void Write(string key, string secret) => _entries[key] = secret;

    public void Delete(string key) => _entries.Remove(key);
}
