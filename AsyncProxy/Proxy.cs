﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using AsyncProxy.Extensions;

namespace AsyncProxy
{
    /// <summary>
    /// Generates proxies for the following usage scenarios:<ul>
    /// 
    /// <li>Create a proxy on a base class where the proxy class should override virtual methods
    ///     in the base class, making those methods available to the proxy.  In this context, 
    ///     Invocation.Proceed invokes the base method implementation.</li>
    /// 
    /// <li>Create a proxy on an interface while supplying a target implementation of that 
    ///     interface.  In this context, Invocation.Proceed invokes the method on the provided
    ///     target.</li>
    /// 
    /// <li>Create a proxy on an interface, not providing any target.  In this context, 
    ///     Invocation.Proceed does nothing.</li>
    /// 
    /// </ul>
    /// 
    /// <b>Note:</b> Generated implementations are stored in a static field of the generic
    /// Proxy&lt;T&gt; class.  These are instantiated upon first access of a particular
    /// variant of that class (variant on the type argument), which solves any thread
    /// contention issues.
    /// </summary>
    public class Proxy
    {
        /// <summary>
        /// Creates a proxy for a given type.  This method supports two discrete usage scenarios.<p/>
        /// If T is an interface, the target should be an implementation of that interface. In 
        /// this scenario, T should be <i>explicitly</i> specified unless the type of <i>target</i>
        /// at the calling site is of that interface.  In other words, if the calling site has the
        /// <i>target</i> declared as the concrete implementation, the proxy will be generated
        /// for the implementation, rather than for the interface.
        /// 
        /// If T is a class, the target should be an instance of that class, and a subclassing 
        /// proxy will be created for it.  However, because target is specified in this case, 
        /// the base class behavior will be ignored (it will all be delegated to the target).
        /// </summary>
        /// <typeparam name="T">The type to create the proxy for.  May be an interface or a 
        /// concrete base class.</typeparam>
        /// <param name="target">The instance of T that should be the recipient of all invocations
        /// on the proxy via Invocation.Proceed.</param>
        /// <param name="invocationHandler">This is where you get to inject your logic.</param>
        /// <returns>The new instance of the proxy that is an instance of T</returns>
        public static T CreateProxy<T>(T target, Func<Invocation, Task<object>> invocationHandler)
        {
            return Proxy<T>.CreateProxy(target, invocationHandler);
        }

        /// <summary>
        /// Creates a proxy for a given type.  This method supports two discrete usage scenarios.<p/>
        /// If T is an interface, the target should be an implementation of that interface. In 
        /// this scenario, T should be <i>explicitly</i> specified unless the type of <i>target</i>
        /// at the calling site is of that interface.  In other words, if the calling site has the
        /// <i>target</i> declared as the concrete implementation, the proxy will be generated
        /// for the implementation, rather than for the interface.
        /// 
        /// If T is a class, the target should be an instance of that class, and a subclassing 
        /// proxy will be created for it.  However, because target is specified in this case, 
        /// the base class behavior will be ignored (it will all be delegated to the target).
        /// </summary>
        /// <typeparam name="T">The type to create the proxy for.  May be an interface or a 
        /// concrete base class.</typeparam>
        /// <param name="target">The instance of T that should be the recipient of all invocations
        /// on the proxy via Invocation.Proceed.</param>
        /// <param name="invocationHandler">This is where you get to inject your logic.</param>
        /// <returns>The new instance of the proxy that is an instance of T</returns>
        public static T CreateProxy<T>(T target, Func<Invocation, object> invocationHandler)
        {
            return CreateProxy(target, invocation => Task.FromResult(invocationHandler(invocation)));
        }

        /// <summary>
        /// Creates a proxy for a given type.  This method supports two discrete usage scenarios.<p/>
        /// If T is an interface, the target should be an implementation of that interface. In 
        /// this scenario, T should be <i>explicitly</i> specified unless the type of <i>target</i>
        /// at the calling site is of that interface.  In other words, if the calling site has the
        /// <i>target</i> declared as the concrete implementation, the proxy will be generated
        /// for the implementation, rather than for the interface.
        /// 
        /// If T is a class, the target should be an instance of that class, and a subclassing 
        /// proxy will be created for it.  However, because target is specified in this case, 
        /// the base class behavior will be ignored (it will all be delegated to the target).
        /// </summary>
        /// <typeparam name="T">The type to create the proxy for.  May be an interface or a 
        /// concrete base class.</typeparam>
        /// <param name="invocationHandler">This is where you get to inject your logic.</param>
        /// <returns>The new instance of the proxy that is an instance of T</returns>
        public static T CreateProxy<T>(Func<Invocation, Task<object>> invocationHandler)
        {
            return CreateProxy(default(T), invocationHandler);
        }

        /// <summary>
        /// Creates a proxy for a given type.  This method supports two discrete usage scenarios.<p/>
        /// If T is an interface, the target should be an implementation of that interface. In 
        /// this scenario, T should be <i>explicitly</i> specified unless the type of <i>target</i>
        /// at the calling site is of that interface.  In other words, if the calling site has the
        /// <i>target</i> declared as the concrete implementation, the proxy will be generated
        /// for the implementation, rather than for the interface.
        /// 
        /// If T is a class, the target should be an instance of that class, and a subclassing 
        /// proxy will be created for it.  However, because target is specified in this case, 
        /// the base class behavior will be ignored (it will all be delegated to the target).
        /// </summary>
        /// <typeparam name="T">The type to create the proxy for.  May be an interface or a 
        /// concrete base class.</typeparam>
        /// <param name="invocationHandler">This is where you get to inject your logic.</param>
        /// <returns>The new instance of the proxy that is an instance of T</returns>
        public static T CreateProxy<T>(Func<Invocation, object> invocationHandler)
        {
            return CreateProxy(default(T), invocation => Task.FromResult(invocationHandler(invocation)));
        }
    }

    public class Proxy<T>
    {
        private static ConstructorInfo voidInvocationConstructor = typeof(VoidInvocation).GetConstructors()[0];
        private static ConstructorInfo voidAsyncInvocationConstructor = typeof(VoidAsyncInvocation).GetConstructors()[0];
        private static MethodInfo voidInvokeMethod = typeof(InvocationHandler).GetMethod("VoidInvoke");
        private static MethodInfo asyncVoidInvokeMethod = typeof(InvocationHandler).GetMethod("VoidAsyncInvoke");
        private static MethodInfo invokeTMethod = typeof(InvocationHandler).GetMethod("InvokeT");
        private static MethodInfo asyncInvokeTMethod = typeof(InvocationHandler).GetMethod("AsyncInvokeT");

        private static Type proxyType = CreateProxyType();

        public static T CreateProxy(T target, Func<Invocation, Task<object>> invocationHandler)
        {
            return (T)Activator.CreateInstance(proxyType, target, new InvocationHandler(invocationHandler));
        }

        private static Type CreateProxyType()
        {
            string assemblyName = typeof(T).FullName + "__Proxy";

            bool isIntf = typeof(T).IsInterface;
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName);

            var baseType = isIntf ? typeof(object) : typeof(T);
            var intfs = isIntf ? new[] { typeof(T) } : Type.EmptyTypes;

            var type = module.DefineType(assemblyName, TypeAttributes.Public, baseType, intfs);

            // Create target field
            var target = type.DefineField("__target", typeof(T), FieldAttributes.Private);
            var invocationHandler = type.DefineField("__invocationHandler", typeof(InvocationHandler), FieldAttributes.Private);

            // Create constructor 
            var constructorWithTarget = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(T), typeof(InvocationHandler) });
            var constructorWithTargetIl = constructorWithTarget.GetILGenerator();
            constructorWithTargetIl.EmitDefaultBaseConstructorCall(typeof(T));

            constructorWithTargetIl.Emit(OpCodes.Ldarg_0);  // Put "this" on the stack for the subsequent stfld instruction way below
            constructorWithTargetIl.Emit(OpCodes.Ldarg_1);  // Put "target" argument on the stack

            // If target is null, we will make the target ourself
            if (!isIntf)
            {
                var targetNotNull = constructorWithTargetIl.DefineLabel();
                constructorWithTargetIl.Emit(OpCodes.Dup);                      // Duplicate "target" since it will be consumed by the following branch instruction
                constructorWithTargetIl.Emit(OpCodes.Brtrue, targetNotNull);    // If target is not null, jump below
                constructorWithTargetIl.Emit(OpCodes.Pop);                      // Pop the null target off the stack
                constructorWithTargetIl.Emit(OpCodes.Ldarg_0);                  // Place "this" onto the stack (our new target)
                constructorWithTargetIl.MarkLabel(targetNotNull);               // Mark where the previous branch instruction should jump to
            }

            // Store whatever is on the stack inside the "target" field.  The value is either: 
            // * The "target" argument passed in -- if not null.
            // * If null and T is an interface type, then it is a struct that implements that interface and returns default values for each method
            // * If null and T is not an interface type, then it is "this", where "proceed" will invoke the base implementation.
            constructorWithTargetIl.Emit(OpCodes.Stfld, target);                

            constructorWithTargetIl.Emit(OpCodes.Ldarg_0);                      // Load "this" for subsequent call to stfld
            constructorWithTargetIl.Emit(OpCodes.Ldarg_2);                      // Load the 2nd argument, which is the invocation handler
            constructorWithTargetIl.Emit(OpCodes.Stfld, invocationHandler);     // Store it in the invocationHandler field
            constructorWithTargetIl.Emit(OpCodes.Ret);                          // Return from the constructor (a ret call is always required, even for void methods and constructors)

            // We use a static constructor to store all the method infos in static fiellds for fast access
            var staticConstructor = type.DefineConstructor(MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
            var staticIl = staticConstructor.GetILGenerator();

            // Now implement/override all methods
            var methods = typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).AsEnumerable();

            // If T is an interface type, we want to implement *all* the methods defined by the interface and its parent interfaces.
            if (isIntf)
                methods = methods.Concat(typeof(T).GetInterfaces().SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)));

            // Now create an implementation for each method
            foreach (var methodInfo in methods)
            {
                var parameterInfos = methodInfo.GetParameters();

                // Finalize doesn't work if we try to proxy it and really, who cares?
                if (methodInfo.Name == "Finalize" && parameterInfos.Length == 0 && methodInfo.DeclaringType == typeof(object))
                    continue;

                // If we're not an interface and the method is not virtual, it's not possible to intercept
                if (!isIntf && methodInfo.IsFinal)
                    continue;

                MethodAttributes methodAttributes;
                if (isIntf)
                {
                    // The attributes required for the normal implementation of an interface method
                    methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
                }
                else
                {
                    // If we're overriding a method, these attributes are required
                    methodAttributes = methodInfo.IsPublic ? MethodAttributes.Public : MethodAttributes.Family;
                    methodAttributes |= MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
                }

                // Define the actual method
                var method = type.DefineMethod(methodInfo.Name, methodAttributes, methodInfo.ReturnType, parameterInfos.Select(x => x.ParameterType).ToArray());

                // Initialize method info in static constructor
                var methodInfoField = type.DefineField(methodInfo.Name + "__Info", typeof(MethodInfo), FieldAttributes.Private | FieldAttributes.Static);
                staticIl.StoreMethodInfo(methodInfoField, methodInfo);

                // Create proceed method (four different types).  The proceed method is what you may call in your invocation handler
                // in order to invoke the behavior that would have happened without the proxy.  The actual behavior depends on the value
                // of "target".  If it's not null, it calls the equivalent method on "target".  If it *is* null, then:
                //
                // * If it's an interface, it provides a default value
                // * If it's not an interface, and the method is not abstract, it calls the base implementation of that class.
                // * If it's abstract, then it provides a default value
                //
                // The actual implementation of proceed varies based on whether (where T represents the method's return type):
                //
                // * The method's return type is void               (Represented by Action)
                // * The method's return type is Task               (Represented by Func<Task>)
                // * The method's return type is Task<T>            (Represented by Func<Task<T>>)
                // * The method's return type is anything else      (Represented by Func<T>)
                Type proceedDelegateType;
                Type proceedReturnType;
                OpCode proceedCall = isIntf ? OpCodes.Callvirt : OpCodes.Call;
                ConstructorInfo invocationConstructor;
                MethodInfo invokeMethod;
                if (methodInfo.ReturnType == typeof(void))
                {
                    proceedDelegateType = typeof(Action<object[]>);
                    proceedReturnType = typeof(void);
                    invocationConstructor = voidInvocationConstructor;
                    invokeMethod = voidInvokeMethod;
                }
                else
                {
                    proceedDelegateType = typeof(Func<,>).MakeGenericType(typeof(object[]), methodInfo.ReturnType);
                    proceedReturnType = methodInfo.ReturnType;
                    if (!typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                    {
                        invocationConstructor = typeof(InvocationT<>).MakeGenericType(methodInfo.ReturnType).GetConstructors()[0];
                        invokeMethod = invokeTMethod.MakeGenericMethod(methodInfo.ReturnType);
                    }
                    else if (methodInfo.ReturnType.IsTaskT())
                    {
                        var taskType = methodInfo.ReturnType.GetTaskType();
                        invocationConstructor = typeof(AsyncInvocationT<>).MakeGenericType(taskType).GetConstructors()[0];
                        invokeMethod = asyncInvokeTMethod.MakeGenericMethod(taskType);
                    }
                    else
                    {
                        invocationConstructor = voidAsyncInvocationConstructor;
                        invokeMethod = asyncVoidInvokeMethod;
                    }
                }
                var proceed = type.DefineMethod(methodInfo.Name + "__Proceed", MethodAttributes.Private, proceedReturnType, new[] { typeof(object[]) });
                var proceedIl = proceed.GetILGenerator();

                if (!methodInfo.IsAbstract || isIntf)
                {
                    // If T is an interface, then we want to check if target is null; if so, we want to just return the default value
                    if (isIntf)
                    {
                        var targetNotNull = proceedIl.DefineLabel();
                        proceedIl.Emit(OpCodes.Ldarg_0);                    // Load "this"
                        proceedIl.Emit(OpCodes.Ldfld, target);              // Load "target" from "this"
                        proceedIl.Emit(OpCodes.Brtrue, targetNotNull);      // If target is not null, jump below
                        DefaultInterfaceImplementationFactory.CreateDefaultMethodImplementation(methodInfo, proceedIl);
                        proceedIl.MarkLabel(targetNotNull);                 // Mark where the previous branch instruction should jump to                        
                    }

                    // Load target for subsequent call
                    proceedIl.Emit(OpCodes.Ldarg_0);
                    proceedIl.Emit(OpCodes.Ldfld, target);

                    // Decompose array into arguments
                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        proceedIl.Emit(OpCodes.Ldarg, (short)1);            // Push array 
                        proceedIl.Emit(OpCodes.Ldc_I4, i);                  // Push element index
                        proceedIl.Emit(OpCodes.Ldelem, typeof(object));     // Get element
                        if (parameterInfos[i].ParameterType.IsValueType)
                            proceedIl.Emit(OpCodes.Unbox_Any, parameterInfos[i].ParameterType);
                    }

                    proceedIl.Emit(proceedCall, methodInfo);
                    proceedIl.Emit(OpCodes.Ret);                    
                }
                else
                {
                    DefaultInterfaceImplementationFactory.CreateDefaultMethodImplementation(methodInfo, proceedIl);
                }

                // Implement method
                var il = method.GetILGenerator();

                // Load handler
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, invocationHandler);

                // Load method info
                il.Emit(OpCodes.Ldsfld, methodInfoField);

                // Create arguments array
                il.Emit(OpCodes.Ldc_I4, parameterInfos.Length);         // Array length
                il.Emit(OpCodes.Newarr, typeof(object));                // Instantiate array
                for (var i = 0; i < parameterInfos.Length; i++)
                {
                    il.Emit(OpCodes.Dup);                               // Duplicate array
                    il.Emit(OpCodes.Ldc_I4, i);                         // Array index
                    il.Emit(OpCodes.Ldarg, (short)(i + 1));             // Element value

                    if (parameterInfos[i].ParameterType.IsValueType)
                        il.Emit(OpCodes.Box, parameterInfos[i].ParameterType);

                    il.Emit(OpCodes.Stelem, typeof(object));            // Set array at index to element value
                }

                // Load function pointer to proceed method
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldftn, proceed);
                il.Emit(OpCodes.Newobj, proceedDelegateType.GetConstructors()[0]);

                // Instantiate Invocation
                il.Emit(OpCodes.Newobj, invocationConstructor);

                // Invoke handler
                il.Emit(OpCodes.Callvirt, invokeMethod);

                // Return
                il.Emit(OpCodes.Ret);
            }

            staticIl.Emit(OpCodes.Ret);

            Type proxyType = type.CreateType();
            return proxyType;
        }
    }
}