using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models.Api;

namespace ClaudeUsageChecker.Core;

/// <summary>
/// Quellcode-generierter JSON-Kontext. Vermeidet Reflexion zur Laufzeit und
/// haelt die Anwendung fuer Trimming/AOT tauglich.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(UsageResponseDto))]
[JsonSerializable(typeof(ClaudeCliCredentials))]
internal sealed partial class ClaudeUsageJsonContext : JsonSerializerContext;
