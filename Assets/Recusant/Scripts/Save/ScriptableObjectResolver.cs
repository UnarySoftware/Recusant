using System;
using System.Reflection;
using Utf8Json;

namespace Recusant
{
    public class ScriptableObjectResolver : IJsonFormatterResolver
    {
        public static readonly IJsonFormatterResolver Instance = new ScriptableObjectResolver();

        ScriptableObjectResolver()
        {

        }

        public IJsonFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IJsonFormatter<T> formatter;

            static FormatterCache()
            {
                formatter = (IJsonFormatter<T>)GetFormatter(typeof(T));
            }
        }

        public static object GetFormatter(Type t)
        {
            var ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                var genericType = ti.GetGenericTypeDefinition();

                if (genericType == typeof(ScriptableObjectRef<>))
                {
                    return CreateInstance(typeof(ScriptableObjectRefFormatter<>), ti.GenericTypeArguments);
                }
            }

            return null;
        }

        static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}
