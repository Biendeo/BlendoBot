using System;
using System.Threading.Tasks;

namespace AutoCorrect
{
    public interface IAutoCorrectProvider : IDisposable
    {
        Task<string> CorrectAsync(string input);
    }
}
