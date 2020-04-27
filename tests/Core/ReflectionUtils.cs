using System;
using System.Reflection;

namespace Cythral.CloudFormation.Monitoring.Tests
{
    public class ReflectionUtils
    {
        public static void SetPrivateProperty<T, U>(T target, string name, U value) 
        {
            var prop = target?.GetType()?.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            prop?.SetValue(target, value);
        }

        public static void SetPrivateField<T, U>(T target, string name, U value) 
        {
            var prop = target?.GetType()?.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            prop?.SetValue(target, value);
        }

        public static void SetPrivateStaticField<T>(Type target, string name, T value) 
        {
            var prop = target?.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            prop?.SetValue(target, value);
        }

        public static void SetReadonlyField<T, U>(T target, string name, U value) 
        {
            var field = target?.GetType()?.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            field?.SetValue(target, value);
        }
    }
}