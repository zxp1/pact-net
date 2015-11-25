using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PactNet.Comparers
{
    public class PropertyUtils
    {
        public static Type GetValueType(Type type)
        {
            Type valueType = type;
            if (type.IsGenericType && type.GetGenericArguments().Length > 1)
                valueType = type.GetGenericArguments()[1];
            else if (type.IsGenericType && type.GetGenericArguments().Length > 0)
                valueType = type.GetGenericArguments()[0];
            else if (type.HasElementType)
                valueType = type.GetElementType();
            return valueType;
        }

        public static bool IsCustomType(Type type)
        {
            if (type == null) return false;
            if (string.IsNullOrEmpty(type.Namespace)) return false;
            return type.Namespace.StartsWith("Leica");
        }
    }
}
