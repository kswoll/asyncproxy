using System.Threading.Tasks;

namespace AsyncProxy.Tests
{
    public class HandWritten : IHandWritten
    {
        public const string GetStringAsyncReturnValue = "Some async string";
        public const string GetStringReturnValue = "Some non async string";

        public bool DoSomethingAsyncCalled { get; set; }
        public bool DoSomethingCalled { get; set; }

        public async Task<string> GetStringAsync()
        {
            await Task.Delay(1);
            return GetStringAsyncReturnValue;
        }

        public async Task DoSomethingAsync()
        {
            await Task.Delay(1);
            DoSomethingAsyncCalled = true;
        }

        public string GetString()
        {
            return GetStringReturnValue;
        }

        public void DoSomething()
        {
            DoSomethingCalled = true;
        }

        public int Sum(int first, int second)
        {
            return first + second;
        }

        public async Task<int> SumAsync(int first, int second)
        {
            await Task.Delay(1);
            return first + second;
        }
    }
}