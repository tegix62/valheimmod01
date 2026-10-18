using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace LightsaberMod.Util
{
    /// <summary>
    /// Reflection helpers for game members whose signatures have shifted between Valheim
    /// patches (extra optional parameters, renamed fields). Calling through these keeps the
    /// mod loading on game versions where a direct call would throw MissingMethodException.
    /// </summary>
    internal static class Compat
    {
        private static readonly Dictionary<string, MethodInfo> MethodCache = new Dictionary<string, MethodInfo>();
        private static readonly Dictionary<string, FieldInfo> FieldCache = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// Invokes the first overload of <paramref name="name"/> whose leading parameters accept
        /// <paramref name="args"/> and whose remaining parameters are optional.
        /// </summary>
        public static object Call(object target, string name, params object[] args)
        {
            if (target == null) return null;
            Type type = target.GetType();
            string key = type.FullName + "::" + name + "/" + string.Join(",", args.Select(a => a?.GetType().Name ?? "null"));

            if (!MethodCache.TryGetValue(key, out MethodInfo method))
            {
                method = AccessTools.GetDeclaredMethods(type)
                    .Concat(AllBaseMethods(type))
                    .Where(m => m.Name == name)
                    .FirstOrDefault(m => Accepts(m.GetParameters(), args));
                MethodCache[key] = method;
                if (method == null)
                    Jotunn.Logger.LogWarning($"Compat: no usable overload of {type.Name}.{name}");
            }

            if (method == null) return null;

            ParameterInfo[] ps = method.GetParameters();
            object[] full = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                full[i] = i < args.Length ? args[i] : DefaultFor(ps[i]);

            return method.Invoke(target, full);
        }

        /// <summary>Calls a static method on a game type looked up by name, e.g. ZInput.GetButtonDown.</summary>
        public static object CallStatic(string typeName, string name, params object[] args)
        {
            string key = "static:" + typeName + "::" + name + "/" + args.Length;
            if (!MethodCache.TryGetValue(key, out MethodInfo method))
            {
                Type type = AccessTools.TypeByName(typeName);
                method = type == null ? null : AccessTools.GetDeclaredMethods(type)
                    .FirstOrDefault(m => m.IsStatic && m.Name == name && Accepts(m.GetParameters(), args));
                MethodCache[key] = method;
                if (method == null)
                    Jotunn.Logger.LogWarning($"Compat: no usable static {typeName}.{name}");
            }

            if (method == null) return null;
            ParameterInfo[] ps = method.GetParameters();
            object[] full = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                full[i] = i < args.Length ? args[i] : DefaultFor(ps[i]);
            return method.Invoke(null, full);
        }

        public static T GetField<T>(object target, string name, T fallback = default)
        {
            FieldInfo field = FindField(target?.GetType(), name);
            if (field == null || !typeof(T).IsAssignableFrom(field.FieldType)) return fallback;
            return (T)field.GetValue(target);
        }

        public static bool SetField(object target, string name, object value)
        {
            FieldInfo field = FindField(target?.GetType(), name);
            if (field == null || (value != null && !field.FieldType.IsInstanceOfType(value))) return false;
            field.SetValue(target, value);
            return true;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            if (type == null) return null;
            string key = type.FullName + "::" + name;
            if (!FieldCache.TryGetValue(key, out FieldInfo field))
            {
                field = AccessTools.Field(type, name);
                FieldCache[key] = field;
            }
            return field;
        }

        private static IEnumerable<MethodInfo> AllBaseMethods(Type type)
        {
            for (Type t = type.BaseType; t != null && t != typeof(object); t = t.BaseType)
                foreach (MethodInfo m in AccessTools.GetDeclaredMethods(t))
                    yield return m;
        }

        private static bool Accepts(ParameterInfo[] ps, object[] args)
        {
            if (ps.Length < args.Length) return false;
            for (int i = 0; i < ps.Length; i++)
            {
                if (i < args.Length)
                {
                    Type pt = ps[i].ParameterType;
                    if (args[i] == null ? pt.IsValueType : !pt.IsInstanceOfType(args[i])) return false;
                }
                else if (!ps[i].IsOptional && !ps[i].ParameterType.IsValueType)
                {
                    // Non-optional reference parameter we can't sensibly fill.
                    return false;
                }
            }
            return true;
        }

        private static object DefaultFor(ParameterInfo p)
        {
            if (p.HasDefaultValue) return p.DefaultValue;
            return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
        }
    }
}
