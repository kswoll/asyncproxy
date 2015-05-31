﻿using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using AsyncProxy.Extensions;

namespace AsyncProxy
{
    public static class DefaultInterfaceImplementationFactory
    {
        private static readonly MethodInfo TaskFromResult = typeof(Task).GetMethod("FromResult");

        public static void CreateDefaultMethodImplementation(MethodInfo methodInfo, ILGenerator il)
        {
            if (typeof (Task).IsAssignableFrom(methodInfo.ReturnType))
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
                    var fromResult = TaskFromResult.MakeGenericMethod(typeof (object));
                    il.Emit(OpCodes.Call, fromResult);
                }
            }
            else if (methodInfo.ReturnType != typeof (void))
            {
                il.EmitDefaultValue(methodInfo.ReturnType);
            }

            // Return
            il.Emit(OpCodes.Ret);
        }
    }
}