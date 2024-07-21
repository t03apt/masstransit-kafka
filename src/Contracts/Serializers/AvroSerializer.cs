using Avro.IO;
using Avro.Specific;

namespace Contracts.Serializers;

public static class AvroSerializer
{
    public static byte[] Serialize<T>(T objectToSerialize)
        where T : ISpecificRecord
    {
        var writer = new SpecificDefaultWriter(objectToSerialize.Schema); // Schema comes from pre-compiled, code-gen phase
        using var ms = new MemoryStream();
        writer.Write(objectToSerialize, new BinaryEncoder(ms));
        return ms.ToArray();
    }

    public static T Deserialize<T>(byte[] serialized)
        where T : ISpecificRecord, new()
    {
        var regenObj = new T();
        var reader = new SpecificDefaultReader(regenObj.Schema, regenObj.Schema);
        using var ms = new MemoryStream(serialized);
        reader.Read(regenObj, new BinaryDecoder(ms));
        return regenObj;
    }
}