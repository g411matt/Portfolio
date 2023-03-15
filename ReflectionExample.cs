using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Snippet of using reflection to locate methods in an assembly that have been flagged by a specific attribute
/// By locating and storing valid methods other classes can then access the list and invoke methods
/// For this snippet methods are valid if they have no parameters or only value type parameters
/// Additionally we will assume that every method will have a unique name so that we only have to store the method name
/// Author: Matt Gall
/// </summary>
public static class ReflectionSnippet
{
    private static Dictionary<string, MethodInfo> _storedMethods = new Dictionary<string, MethodInfo>();

    /// <summary>
    /// Build the Dictionary of available methods from a specified assembly
    /// </summary>
    /// <param name="assembly"></param>
    public static void LoadValidMethodsFromAssembly(Assembly assembly)
    {
        // iterate over the assembly's types
        foreach (var type in assembly.GetTypes()) 
        {
            // if the type has the desired attribute then check its methods
            if (type.GetCustomAttribute<SampleAttribute>(false) != null)
            {
                // get all public static methods
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach(var method in methods)
                {
                    // if the method also has the attribute then it is possibly valid so check its parameters 
                    if (method.GetCustomAttribute<SampleAttribute>(false) != null)
                    {
                        // check the parameters to make sure they are value type
                        var parameters = method.GetParameters();
                        bool valid = true;
                        if (parameters.Length > 0)
                        {
                            foreach(var para in parameters)
                            {
                                if (!para.ParameterType.IsValueType)
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                        if (valid)
                        {
                            _storedMethods.Add(method.Name, method);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Invoke a saved method with the name as reference
    /// </summary>
    public static bool InvokeMethod(string methodName, params object[] parameters)
    {
        if (_storedMethods.TryGetValue(methodName, out MethodInfo method))
        {
            // check to make sure that the number and types of parameters match before invoking
            var parameterInfo = method.GetParameters();
            if (parameterInfo.Length != parameters.Length)
            {
                return false;
            }
            if (parameterInfo.Length > 0)
            {
                for(int i = 0; i < parameterInfo.Length; i++)
                {
                    if (parameterInfo[i].ParameterType != parameters[i].GetType())
                    {
                        return false;
                    }
                }
            }
            // invoke the method
            method.Invoke(null, parameters);
            return true;
        }
        return false;
    }
}

public class SampleAttribute : Attribute { }
