using System;
using System.Collections.Generic;
using System.Reflection;

namespace ValheimAdminTool.Utils
{
    public static class ReflectionExtensions
    {
        private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
        private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        // Lookups happen on hot paths (camera, minimap overlay, Harmony patches), so resolve each
        // member once per type and reuse it.
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> s_fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> s_methods = new Dictionary<Type, Dictionary<string, MethodInfo>>();
        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>> s_staticMethods = new Dictionary<Type, Dictionary<string, MethodInfo>>();

        public static T GetFieldValue<T>(this object obj, string name)
        {
            FieldInfo field = Field(obj.GetType(), name);
            return (T)field?.GetValue(obj);
        }

        public static void SetFieldValue<T>(this object obj, string name, T value)
        {
            Field(obj.GetType(), name)?.SetValue(obj, value);
        }

        public static object CallMethod(this object obj, string methodName, params object[] args)
        {
            MethodInfo method = Method(s_methods, obj.GetType(), methodName, InstanceFlags);
            return method != null ? method.Invoke(obj, args) : null;
        }

        public static object CallStaticMethod<T>(string methodName, params object[] args) where T : new()
        {
            MethodInfo method = Method(s_staticMethods, typeof(T), methodName, StaticFlags);
            return method != null ? method.Invoke(null, args) : null;
        }

        private static FieldInfo Field(Type type, string name)
        {
            Dictionary<string, FieldInfo> byName;
            if (!s_fields.TryGetValue(type, out byName))
            {
                byName = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                s_fields.Add(type, byName);
            }

            FieldInfo field;
            if (!byName.TryGetValue(name, out field))
            {
                field = type.GetField(name, InstanceFlags);
                byName.Add(name, field);
            }

            return field;
        }

        private static MethodInfo Method(Dictionary<Type, Dictionary<string, MethodInfo>> cache, Type type, string name, BindingFlags flags)
        {
            Dictionary<string, MethodInfo> byName;
            if (!cache.TryGetValue(type, out byName))
            {
                byName = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
                cache.Add(type, byName);
            }

            MethodInfo method;
            if (!byName.TryGetValue(name, out method))
            {
                method = type.GetMethod(name, flags);
                byName.Add(name, method);
            }

            return method;
        }
    }
}
