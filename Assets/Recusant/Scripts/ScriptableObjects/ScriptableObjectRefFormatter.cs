using System;
using Utf8Json;

namespace Recusant
{
    public class ScriptableObjectRefFormatter<T> : IJsonFormatter<AssetRef<T>> where T : BaseScriptableObject
    {
        public void Serialize(ref JsonWriter writer, AssetRef<T> value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var formatter = formatterResolver.GetFormatterWithVerify<Guid>();
            formatter.Serialize(ref writer, value.AssetId.Value, formatterResolver);
        }

        public AssetRef<T> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull())
            {
                return null;
            }

            Guid value = formatterResolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, formatterResolver);
            return new AssetRef<T>(value);
        }
    }
}
