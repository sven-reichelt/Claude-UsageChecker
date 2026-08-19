using System.Text.Json;
using System.Text.Json.Serialization;
using ClaudeUsageChecker.Core.Authentication;
using ClaudeUsageChecker.Core.Models.Api;

namespace ClaudeUsageChecker.Core;

/// <summary>
/// Source-generated JSON context. Avoids reflection at runtime and keeps the
/// application fit for trimming and AOT.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
[JsonSerializable(typeof(UsageResponseDto))]
[JsonSerializable(typeof(ClaudeCliCredentials))]
internal sealed partial class ClaudeUsageJsonContext : JsonSerializerContext;
