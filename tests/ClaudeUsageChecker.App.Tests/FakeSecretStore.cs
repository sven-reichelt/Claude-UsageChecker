using ClaudeUsageChecker.Core.Platform;

namespace ClaudeUsageChecker.App.Tests;

/// <summary>Secret-Store im Arbeitsspeicher, damit Tests nichts am System hinterlassen.</summary>
internal sealed class FakeSecretStore : ISecretStore
{
    private readonly Dictionary<string, string> _entries = [];

    public bool IsSupported { get; init; } = true;

    public string? Read(string key) => _entries.GetValueOrDefault(key);

    public void Write(string key, string secret) => _entries[key] = secret;

    public void Delete(string key) => _entries.Remove(key);
}
