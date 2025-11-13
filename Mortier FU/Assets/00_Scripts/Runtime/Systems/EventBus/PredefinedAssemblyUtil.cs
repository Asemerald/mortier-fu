using System.Collections.Generic;
using System.Reflection;
using System;

// Code from git-amend "Learn to Build an Advanced Event Bus | Unity Architecture".

namespace MortierFU
{
    public static class PredefinedAssemblyUtil
    {
        public static void AddTypesFromAssembly(Type[] assembly, ICollection<Type> types, Type interfaceType)
        {
            if (assembly == null) return;
            for (int i = 0; i < assembly.Length; i++)
            {
                Type type = assembly[i];
                if (type != interfaceType && interfaceType.IsAssignableFrom(type))
                {
                    types.Add(type);
                }
            }
        }

        public static List<Type> GetTypes(Type interfaceType)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            List<Type> types = new List<Type>();
            for(int i = 0; i < assemblies.Length; i++)
            {
                AddTypesFromAssembly(assemblies[i].GetTypes(), types, interfaceType);
            }

            return types;
        }
    }
}