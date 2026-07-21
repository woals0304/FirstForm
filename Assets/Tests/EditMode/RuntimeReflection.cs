using System;
using System.Linq;
using System.Reflection;

namespace FirstForm.Tests
{
    internal static class RuntimeReflection
    {
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public static Type Type(string fullName)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name == "Assembly-CSharp")
                .Select(assembly => assembly.GetType(fullName, false))
                .FirstOrDefault(candidate => candidate != null);

            if (type == null)
            {
                throw new InvalidOperationException("Assembly-CSharp에서 런타임 타입을 찾을 수 없습니다: " + fullName);
            }

            return type;
        }

        public static object Create(string fullName)
        {
            return Activator.CreateInstance(Type(fullName));
        }

        public static object GetField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, AnyInstance);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            return field.GetValue(target);
        }

        public static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, AnyInstance);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }

        public static object GetProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, AnyInstance);
            if (property == null)
            {
                throw new MissingMemberException(target.GetType().FullName, propertyName);
            }

            return property.GetValue(target);
        }

        public static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length, AnyInstance);
            return method.Invoke(target, arguments);
        }

        public static object InvokeStatic(string fullTypeName, string methodName, params object[] arguments)
        {
            Type type = Type(fullTypeName);
            MethodInfo method = FindMethod(type, methodName, arguments.Length, AnyStatic);
            return method.Invoke(null, arguments);
        }

        private static MethodInfo FindMethod(Type type, string methodName, int argumentCount, BindingFlags flags)
        {
            MethodInfo[] matches = type.GetMethods(flags)
                .Where(method => method.Name == methodName && method.GetParameters().Length == argumentCount)
                .ToArray();

            if (matches.Length != 1)
            {
                throw new MissingMethodException(type.FullName, methodName + "(" + argumentCount + ")");
            }

            return matches[0];
        }
    }
}
