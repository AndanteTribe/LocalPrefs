#nullable enable

using System.Text.Json.Serialization;

namespace AndanteTribe.IO.Tests
{
    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Default,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false)]
    [JsonSerializable(typeof(CustomData))]
    internal partial class LocalPrefsTestJsonSerializerContext : JsonSerializerContext
    {
    }
}