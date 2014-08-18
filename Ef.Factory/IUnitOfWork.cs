using System;
using System.Threading.Tasks;

namespace Ef.Factory
{
    public interface IUnitOfWork<T> : IDisposable
    {
        IGenericFactoryAsync<TEntity, TKey> CreateFactory<TEntity, TKey>() where TEntity : class;

        int Commit(bool autoRollbackOnError = true);
        Task<int> CommitAsync(bool autoRollbackOnError = true);
        void RollBack();
    }
}