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

            var formatter = formatterResolver.GetFormatterWithVerify<string>();
            formatter.Serialize(ref writer, value.Path, formatterResolver);
        }

        public ScriptableObjectRef<T> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (reader.ReadIsNull())
            {
                return null;
            }

            string value = formatterResolver.GetFormatterWithVerify<string>().Deserialize(ref reader, formatterResolver);
            return new ScriptableObjectRef<T>(value);
        }
    }
}
