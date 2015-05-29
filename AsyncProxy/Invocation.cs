using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AsyncProxy
{
    public abstract class Invocation
    {
        public MethodInfo Method { get; private set; }
        public object[] Arguments { get; set; }        

        public abstract Task<object> Proceed();

        protected Invocation(MethodInfo method, object[] arguments)
        {
            Method = method;
            Arguments = arguments;
        }
    }

    public class VoidAsyncInvocation : Invocation
    {
        private Func<object[], Task> implementation;

        public VoidAsyncInvocation(MethodInfo method, object[] arguments, Func<object[], Task> implementation) : base(method, arguments)
        {
            this.implementation = implementation;
        }

        public override async Task<object> Proceed()
        {
            await implementation(Arguments);
            return null;
        }
    }

    public class AsyncInvocationT<T> : Invocation
    {
        private Func<object[], Task<T>> implementation;

        public AsyncInvocationT(MethodInfo method, object[] arguments, Func<object[], Task<T>> implementation) : base(method, arguments)
        {
            this.implementation = implementation;
        }

        public override async Task<object> Proceed()
        {
            return await implementation(Arguments);
        }
    }

    public class VoidInvocation : Invocation
    {
        private Action<object[]> implementation;

        public VoidInvocation(MethodInfo method, object[] arguments, Action<object[]> implementation) : base(method, arguments)
        {
            this.implementation = implementation;
        }

        public override Task<object> Proceed()
        {
            implementation(Arguments);
            return Task.FromResult<object>(null);
        }
    }

    public class InvocationT<T> : Invocation
    {
        private Func<object[], T> implementation;

        public InvocationT(MethodInfo method, object[] arguments, Func<object[], T> implementation) : base(method, arguments)
        {
            this.implementation = implementation;
        }

        public override Task<object> Proceed()
        {
            return Task.FromResult<object>(implementation(Arguments));
        }
    }
}