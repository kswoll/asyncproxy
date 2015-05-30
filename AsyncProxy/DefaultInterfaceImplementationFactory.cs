using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using AsyncProxy.Extensions;

namespace AsyncProxy
{
    public static class DefaultInterfaceImplementationFactory
    {
        private static readonly MethodInfo TaskFromResult = typeof(Task).GetMethod("FromResult");

        public static ConstructorInfo CreateDefaultInterfaceImplementation<T>(TypeBuilder type)
        {
            var defaultType = type.DefineNestedType("__DefaultImplementation", TypeAttributes.NestedPublic, typeof(ValueType), new[] { typeof(T) });
            var constructor = defaultType.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new Type[0]);
            constructor.GetILGenerator().Emit(OpCodes.Ret);

            // Now implement/override all methods
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).AsEnumerable();
            methods = methods.Concat(typeof(T).GetInterfaces().SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)));
            
            foreach (var methodInfo in methods)
            {
                var parameterInfos = methodInfo.GetParameters();

                // Finalize doesn't work if we try to proxy it and really, who cares?
                if (methodInfo.Name == "Finalize" && parameterInfos.Length == 0 && methodInfo.DeclaringType == typeof(object))
                    continue;

                var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
                var method = defaultType.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.ReturnType, parameterInfos.Select(x => x.ParameterType).ToArray());

                // Implement method
                var il = method.GetILGenerator();

                if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    if (methodInfo.ReturnType.IsTaskT())
                    {
                        var taskType = methodInfo.ReturnType.GetTaskType();
                        il.EmitDefaultValue(taskType);
                        var fromResult = TaskFromResult.MakeGenericMethod(taskType);
                        il.Emit(OpCodes.Call, fromResult);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                        var fromResult = TaskFromResult.MakeGenericMethod(typeof(object));
                        il.Emit(OpCodes.Call, fromResult);
                    }
                }
                else if (methodInfo.ReturnType != typeof(void))
                {
                    il.EmitDefaultValue(methodInfo.ReturnType);
                }

                // Return
                il.Emit(OpCodes.Ret);
            }

            defaultType.CreateType();

            return constructor;
        }
    }
}