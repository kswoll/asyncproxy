using System.Threading.Tasks;

namespace AsyncProxy.Tests
{
    public interface IHandWritten
    {
        Task<string> GetStringAsync();
        Task DoSomethingAsync();
        void DoSomething();
        string GetString();
        int Sum(int first, int second);
        Task<int> SumAsync(int first, int second);
    }
}