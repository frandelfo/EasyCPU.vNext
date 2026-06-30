using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyCpu.Backend.Serializers;

public class EasyFileSerializer : ISourceSerializer
{
    public bool CanWrite => true;

    public (string[] code, string[] data) Load(string path)
    {
        var text = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<FileDoc>(text);
        return (doc?.code ?? [], doc?.data ?? []);
    }

    public void Save(string path, string[] code, string[] data)
    {
        var doc = new FileDoc(1, code, data);
        File.WriteAllText(path, JsonSerializer.Serialize(doc,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private record FileDoc(
        [property: JsonPropertyName("version")] int version,
        [property: JsonPropertyName("code")]    string[] code,
        [property: JsonPropertyName("data")]    string[] data);
}
