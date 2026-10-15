using System.Text.Json;
using System.Text.Json.Serialization;

namespace Enigma.FileExplorer.Core.Settings;

/// <summary>
/// The settings file's JSON contract, generated at build time. Enums are written by name so the file reads
/// well and survives reordered members; comments and trailing commas are tolerated in a hand-edited file.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
