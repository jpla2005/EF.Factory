using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace Ef.Factory
{
    public class UnitWork<T> : IUnitOfWork<T> where T : DbContext, new()
    {
        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        protected T Context { get; set; }

        protected IDictionary<Type, ICommitable> CreatedFactories { get; set; }

        public bool AutoCommit { get; set; }

        #endregion

        #region Constructors

        public UnitWork()
        {
            Context = new T();
            CreatedFactories = new Dictionary<Type, ICommitable>();
        }

        ~UnitWork()
        {
            Dispose(false);
        }

        #endregion

        #region Factory Operations

        private GenericFactory<TEntity, TKey> CreateFactoryCore<TEntity, TKey>() where TEntity : class
        {
            var disposable = CreatedFactories.ContainsKey(typeof(TEntity))
                ? CreatedFactories[typeof(TEntity)]
                : null;

            if (disposable != null)
            {
                if (!disposable.IsDisposed)
                {
                    return (GenericFactory<TEntity, TKey>)disposable;
                }

                CreatedFactories.Remove(typeof(TEntity));
            }

            var factoryType = GetRepositoryType().MakeGenericType(typeof(TEntity), typeof(TKey));
            try
            {
                var factoryObj = Activator.CreateInstance(factoryType, new object[] { Context, AutoCommit });
                var factory = (GenericFactory<TEntity, TKey>)factoryObj;
                if (factory != null)
                {
                    CreatedFactories.Add(typeof(TEntity), (ICommitable)factoryObj);

                    return factory;
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // just return null
            }

            return null;
        }

        protected virtual Type GetRepositoryType()
        {
            return typeof(GenericFactory<,>);
        }

        public IGenericFactoryAsync<TEntity, TKey> CreateFactory<TEntity, TKey>() where TEntity : class
        {
            return CreateFactoryCore<TEntity, TKey>();
        }

        #endregion

        #region UnitOfWork Operations

        public T GetContext
        {
            get { return Context; }
        }

        public void SetContext(T cont)
        {
            Context = cont;
        }

        public virtual int Commit(bool autoRollbackOnError = true)
        {
            try
            {
                return CreatedFactories.Values.Sum(factory => factory.Commit());
            }
            catch (Exception)
            {
                if (autoRollbackOnError)
                {
                    RollBack();
                }

                throw;
            }
        }

        public async virtual Task<int> CommitAsync(bool autoRollbackOnError = true)
        {
            try
            {
                var result = 0;
                foreach (var factory in CreatedFactories.Values)
                {
                    result += await factory.CommitAsync();
                }

                return result;
            }
            catch (Exception)
            {
                if (autoRollbackOnError)
                {
                    RollBack();
                }

                throw;
            }
        }

        public void RollBack()
        {
            var ctx = ((IObjectContextAdapter)Context).ObjectContext;
            ctx.AcceptAllChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                if (!CollectionUtils.IsNullOrEmpty(CreatedFactories))
                {
                    foreach (var disposable in CreatedFactories.Values)
                    {
                        disposable.Dispose();
                    }

                    CreatedFactories.Clear();
                }

                Context.Dispose();
                _isDisposed = true;
            }
        }

        #endregion
    }
}