using System;
using System.Threading.Tasks;

namespace AsyncProxy.Tests
{
    public struct HandWrittenDefaultImplementation : IHandWritten
    {
        public Task<string> GetStringAsync()
        {
            return Task.FromResult<string>(null);
        }

        public Task DoSomethingAsync()
        {
            return Task.FromResult<object>(null);
        }

        public void DoSomething()
        {
        }

        public string GetString()
        {
            return default(string);
        }

        public int Sum(int first, int second)
        {
            return default(int);
        }

        public Task<int> SumAsync(int first, int second)
        {
            return Task.FromResult(default(int));
        }

        public DateTime GetDateTime()
        {
            return default(DateTime);
        }
    }
}