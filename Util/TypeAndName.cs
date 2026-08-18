using System;

namespace Mochi
{
    public struct TypeAndName
    {
        public Type Type;
        public string Name;

        public TypeAndName(Type type, string name = "")
        {
            Type = type;
            Name = name;
        }

        public override string ToString()
        {
            return $"TypeAndName({Type.Name},{Name})";
        }
    }
}
