using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PassivePicasso.RainOfStages.Configurators
{
    public static class Extension
    {
        public static bool IsPointInside(this Mesh aMesh, Vector3 aLocalPoint)
        {
            var verts = aMesh.vertices;
            var tris = aMesh.triangles;
            int triangleCount = tris.Length / 3;
            for (int i = 0; i < triangleCount; i++)
            {
                var V1 = verts[tris[i * 3]];
                var V2 = verts[tris[i * 3 + 1]];
                var V3 = verts[tris[i * 3 + 2]];
                var P = new Plane(V1, V2, V3);
                if (P.GetSide(aLocalPoint))
                    return false;
            }
            return true;
        }
        public static void SetValueDirect(this SerializedProperty property, object value)
        {
            if (property == null)
                throw new System.NullReferenceException("SerializedProperty is null");

            object obj = property.serializedObject.targetObject;
            string propertyPath = property.propertyPath;
            var flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var paths = propertyPath.Split('.');
            FieldInfo field = null;

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (obj == null)
                    throw new System.NullReferenceException("Can't set a value on a null instance");

                var type = obj.GetType();
                if (path == "Array")
                {
                    path = paths[++i];
                    var iter = (obj as System.Collections.IEnumerable);
                    if (iter == null)
                        //Property path thinks this property was an enumerable, but isn't. property path can't be parsed
                        throw new System.ArgumentException("SerializedProperty.PropertyPath [" + propertyPath + "] thinks that [" + paths[i - 2] + "] is Enumerable.");

                    var sind = path.Split('[', ']');
                    int index = -1;

                    if (sind == null || sind.Length < 2)
                        // the array string index is malformed. the property path can't be parsed
                        throw new System.FormatException("PropertyPath [" + propertyPath + "] is malformed");

                    if (!Int32.TryParse(sind[1], out index))
                        //the array string index in the property path couldn't be parsed,
                        throw new System.FormatException("PropertyPath [" + propertyPath + "] is malformed");

                    obj = iter.ElementAtOrDefault(index);
                    continue;
                }

                field = type.GetField(path, flag);
                if (field == null)
                    //field wasn't found
                    throw new System.MissingFieldException("The field [" + path + "] in [" + propertyPath + "] could not be found");

                if (i < paths.Length - 1)
                    obj = field.GetValue(obj);

            }

            var valueType = value.GetType();
            if (!valueType.Is(field.FieldType))
                // can't set value into field, types are incompatible
                throw new System.InvalidCastException("Cannot cast [" + valueType + "] into Field type [" + field.FieldType + "]");

            field.SetValue(obj, value);
        }
        public static System.Object ElementAtOrDefault(this System.Collections.IEnumerable collection, int index)
        {
            var enumerator = collection.GetEnumerator();
            int j = 0;
            for (; enumerator.MoveNext(); j++)
            {
                if (j == index) break;
            }

            System.Object element = (j == index)
                ? enumerator.Current
                : default(System.Object);

            var disposable = enumerator as System.IDisposable;
            if (disposable != null) disposable.Dispose();

            return element;
        }
        public static bool Is(this Type type, Type baseType)
        {
            if (type == null) return false;
            if (baseType == null) return false;

            return baseType.IsAssignableFrom(type);
        }

        public static bool Is<T>(this Type type)
        {
            if (type == null) return false;
            Type baseType = typeof(T);

            return baseType.IsAssignableFrom(type);
        }
    }
}
