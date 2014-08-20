using System;
using System.Threading.Tasks;

namespace Ef.Factory
{
    public interface ICommitable : IDisposable
    {
        bool IsDisposed { get; }

        int Commit();
        Task<int> CommitAsync();
    }
}