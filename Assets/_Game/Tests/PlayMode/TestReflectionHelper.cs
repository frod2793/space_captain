using System;
using System.Reflection;
using UnityEngine;

public static class TestReflectionHelper
{
    private static Assembly s_gameAssembly;

    public static Assembly GameAssembly
    {
        get
        {
            if (s_gameAssembly == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "Assembly-CSharp")
                    {
                        s_gameAssembly = assembly;
                        break;
                    }
                }
            }
            return s_gameAssembly;
        }
    }

    public static Type GetGameType(string typeName)
    {
        return GameAssembly?.GetType(typeName);
    }

    public static object GetStaticFieldValue(string typeName, string fieldName)
    {
        var type = GetGameType(typeName);
        var field = type?.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        return field?.GetValue(null);
    }

    public static object InvokeStaticMethod(string typeName, string methodName, params object[] args)
    {
        var type = GetGameType(typeName);
        var method = type?.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        return method?.Invoke(null, args);
    }
}
