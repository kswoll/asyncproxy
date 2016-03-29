# Note: This project has been deprecated in favor of [sexy-proxy](https://github.com/kswoll/sexy-proxy).  All new development
will be happening there.

---

# AsyncProxy
A .NET library for dynamically creating proxies that fully support async/await

This library is similar in purpose to [Castle's DynamicProxy](https://github.com/castleproject/Core/blob/master/docs/dynamicproxy-introduction.md) 
but with support for `async/await` semantics.  Consider DynamicProxy's example (on the linked to page):

    [Serializable]
    public class Interceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine("Before target call");
            try
            {
               invocation.Proceed();
            }
            catch(Exception)
            {
               Console.WriteLine("Target threw an exception!");
               throw;
            }
            finally
            {
               Console.WriteLine("After target call");
            }
        }
    }
    
It's unstated what the contract of the intercepted method is, but let's say for the sake of argument it's:

    Task<string> GetHelloWorldAsync();
    
Lets say the target on which you're wrapping has implemented this method like so:

    public async Task<string> GetHelloWorldAsync()
    {
        await Task.Delay(1000);
        return "Hello World";
    }

Now, how will the interceptor handle this?  The code in `finally` that logs "After target call" will happen *immediately* -- 
before the delay has finished.**  This is not really how you want the interceptor to work.  (It's true that you can use 
`.ContinueWith` on the returned task and then reassign the return value to that new task.  But this is ugly and error prone) Instead, it would be great if interceptors were *designed* to be `Task` based.  That way, it's still trivial to intercept 
non-async methods, but would allow you to use `await` when appropriate.

** For more discussion on these issues, see [Intercept the call to an async method using DynamicProxy](http://stackoverflow.com/questions/14288075/intercept-the-call-to-an-async-method-using-dynamicproxy) and [Intercept async method that returns generic Task<> via DynamicProxy](http://stackoverflow.com/questions/28099669/intercept-async-method-that-returns-generic-task-via-dynamicproxy).

## AsyncProxy to the Rescue

Now look at AsyncProxy's interceptor contract: 

    public static T CreateProxy(T target, Func<Invocation, Task<object>> invocationHandler) { ... }
    
Specifically, a function that is passed an `Invocation` and returns a `Task<object>` representing the result.  This means that 
your interceptor can itself be an `async` method, and you can use `await` to your heart's content:

    var proxy = Proxy.CreateProxy<IHandWritten>(target, async invocation =>
    {
        await Task.Delay(1);
        var returnValue = await invocation.Proceed();
        return (string)returnValue + " Test";
    });

Now, if we invoke our async method:

    var result = await proxy.GetHelloWorldAsync();
    
The interceptor will delay `1` millisecond, the `target` implementation will wait `1000` milliseconds, then subsequently return 
"Hello World". Finally, the interceptor casts the return value as a `string` concatenates `" Test"`, and returns the result.  
Consequently, the value of `result` will be `Hello World Test`, after ~1001 milliseconds of awaiting.

If you happen to know that your contract is not returning any tasks that need to be awaited, you can use an overload when creating the proxy that allows you to pass in a lambda that doesn't return a `Task`, avoiding `async` semantics altogether:

    public static T CreateProxy<T>(T target, Func<Invocation, object> invocationHandler)
