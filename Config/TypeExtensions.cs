#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Text;

namespace PassivePicasso.ThunderKit.Config
{
    public static class TypeExtensions
    {
        public static string GetParameterFriendlyName(this Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            bool isNullable = nullableType != null;
            if (isNullable)
                type = nullableType;

            string friendlyName = type.FullName;
            if (friendlyName.StartsWith("System.Nullable"))
            {
                var index = friendlyName.IndexOf("[[") + 2;
                friendlyName = friendlyName.Substring(index);

                index = friendlyName.IndexOf(",");
                friendlyName = friendlyName.Substring(0, index);
                friendlyName = friendlyName.Substring(friendlyName.LastIndexOf(".") + 1);
            }
            else if (type.IsGenericType)
            {
                friendlyName = GetTypeString(type);
            }

            string cleaned = friendlyName.Replace('+', '.');
            if (isNullable)
                return $"{cleaned}?";
            else
                return cleaned;
        }

        public static string GetFriendlyName(this Type type)
        {
            string friendlyName = type.FullName;
            if (type.IsGenericType)
            {
                friendlyName = GetTypeString(type);
            }

            return friendlyName.Replace('+', '.');
        }

        private static string GetTypeString(Type type)
        {
            var t = type.AssemblyQualifiedName;

            var output = new StringBuilder();
            List<string> typeStrings = new List<string>();

            output.Append(t.Substring(0, t.IndexOf('`')).Replace("[", string.Empty));
            var genericTypes = type.GetGenericArguments();

            foreach (var genType in genericTypes)
            {
                typeStrings.Add(genType.IsGenericType ? GetTypeString(genType) : genType.ToString());
            }

            output.Append($"<{string.Join(",", typeStrings)}>");
            return output.ToString();
        }
    }
}

#endif