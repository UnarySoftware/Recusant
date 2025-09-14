using System;
using Utf8Json;

namespace Recusant
{
    public class ScriptableObjectRefFormatter<T> : IJsonFormatter<ScriptableObjectRef<T>> where T : BaseScriptableObject
    {
        public void Serialize(ref JsonWriter writer, ScriptableObjectRef<T> value, IJsonFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var formatter = formatterResolver.GetFormatterWithVerify<Guid>();
            formatter.Serialize(ref writer, value.UniqueId.Value, formatterResolver);
        }

        public ScriptableObjectRef<T> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull())
            {
                return null;
            }

            Guid value = formatterResolver.GetFormatterWithVerify<Guid>().Deserialize(ref reader, formatterResolver);
            return new ScriptableObjectRef<T>(value);
        }
    }
}
