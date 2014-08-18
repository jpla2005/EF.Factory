using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Ef.Factory
{
    public interface IConmited : IDisposable
    {
        bool IsDisposed { get; }
    }

    public interface IGenericFactory<T> : IGenericFactory<T, int> where T : class
    {
    }

    public interface IGenericFactory<T, in TKey> : IConmited where T : class
    {
        void Save(T entity);
        void Update(T entity);
        void Delete(T entity);
        void Delete(params Expression<Func<T, bool>>[] filters);
        int Commit();
     
        T GetById(TKey id);

        T First(params Expression<Func<T, bool>>[] filters);
        TR First<TR>(params Expression<Func<TR, bool>>[] filters) where TR : class, T;

        IQueryable<T> GetAll(params Expression<Func<T, object>>[] includeProperties);
        IQueryable<TR> GetAll<TR>(params Expression<Func<TR, object>>[] includeProperties) where TR : class, T;

        IQueryable<TR> Find<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties = null, params Expression<Func<TR, bool>>[] filters) where TR : class, T;

        IQueryable<TR> Find<TR>(params Expression<Func<TR, bool>>[] filters) where TR : class, T;

        IQueryable<T> Find(IEnumerable<Expression<Func<T, object>>> includeProperties = null, params Expression<Func<T, bool>>[] filters);

        IQueryable<T> Find(params Expression<Func<T, bool>>[] filters);

        int Count(params Expression<Func<T, bool>>[] filters);

        IQueryable<T> AllIncluding(params Expression<Func<T, object>>[] includeProperties);
    }
}