using System;
using System.Linq;
using EasyCpu.Backend.Local;

namespace EasyCpu.Backend.Serializers;

public class LegacyAsSerializer : ISourceSerializer
{
    public bool CanWrite => false;

    public (string[] code, string[] data) Load(string path)
    {
        Storage.Apri(path, out var code, out var data);
        return (code.ToArray(), data.ToArray());
    }

    public void Save(string path, string[] code, string[] data)
        => throw new InvalidOperationException("Il formato legacy (.as) è di sola lettura.");
}
