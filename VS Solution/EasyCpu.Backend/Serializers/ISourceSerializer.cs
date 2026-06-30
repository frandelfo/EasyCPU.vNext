using System;

namespace EasyCpu.Backend.Serializers;

public interface ISourceSerializer
{
    (string[] code, string[] data) Load(string path);
    void Save(string path, string[] code, string[] data);
    bool CanWrite { get; }

    public static ISourceSerializer ForPath(string path) =>
        path.EndsWith(".asj", StringComparison.OrdinalIgnoreCase)
            ? new EasyFileSerializer()
            : (ISourceSerializer)new LegacyAsSerializer();
}
