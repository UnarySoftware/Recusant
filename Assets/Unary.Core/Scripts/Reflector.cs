using System;
using System.Collections.Generic;

namespace Unary.Core
{
    public class Reflector : CoreSystem<Reflector>
    {
        private readonly Dictionary<string, Type> _fullNameToType = new();

        public Type GetTypeByName(string name)
        {
            if (_fullNameToType.TryGetValue(name, out Type type))
            {
                return type;
            }
            return null;
        }

        public override bool Initialize()
        {
            foreach (var assembly in ContentLoader.Instance.GetAllAssemblies())
            {
                Type[] types = assembly.GetTypes();

                foreach (var type in types)
                {
                    object[] attributes = type.GetCustomAttributes(false);

                    foreach (var attribute in attributes)
                    {
                        if (attribute is FormerlyDeclaredAsAttribute knownAs)
                        {
                            _fullNameToType[knownAs.FullName] = type;
                        }
                    }

                    _fullNameToType[type.FullName] = type;
                }
            }

            return true;
        }

        public override void PostInitialize()
        {

        }

        public override void Deinitialize()
        {

        }
    }
}
