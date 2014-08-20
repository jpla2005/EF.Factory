using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ef.Factory
{
    public class GenericFactory<T, TKey> :  IGenericFactoryAsync<T, TKey> where T : class
    {
        #region Properties

        private bool AlwaysCommit { get; set; }

        private DbContext Context { get; set; }

        public bool IsDisposed { get; private set; }

        #endregion

        #region Constructors

        public GenericFactory(DbContext context, bool commit = false)
        {
            Context = context;
            AlwaysCommit = commit;
        }

        public GenericFactory(bool commit = true)
            : this(null, commit)
        {
            AlwaysCommit = commit;
        }

        ~GenericFactory()
        {
            Dispose(false);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!IsDisposed && disposing)
            {
                if (Context != null)
                {
                    Context.Dispose();
                }
            }

            IsDisposed = true;
        }

        #endregion

        #region Core

        protected virtual void SaveCore(T entity)
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            dbset.Add(entity);
        }

        protected virtual async void UpdateCore(T entity, bool async)
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            dbset.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;

            if (AlwaysCommit)
            {
                if (async)
                {
                    await CommitAsync();
                }
                else
                {
                    Commit();
                }
            }
        }

        protected virtual async void DeleteCore(T entity, bool async)
        {
            var db = GetContext();
            try
            {
                var dbset = db.Set<T>();
                dbset.Remove(entity);

                if (AlwaysCommit)
                {
                    if (async)
                    {
                        await CommitAsync();
                    }
                    else
                    {
                        Commit();
                    }
                }
            }
            catch (Exception)
            {
                if (db.ChangeTracker.Entries().Any(q => q.Entity.Equals(entity) && q.State == EntityState.Deleted))
                {
                    db.Entry(entity).State = EntityState.Unchanged;
                }

                throw;
            }
        }

        protected virtual IQueryable<TR> QueryCore<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties,
            params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            var source = dbset.OfType<TR>();
            if (includeProperties != null)
            {
                var expressions = includeProperties as Expression<Func<TR, object>>[] ?? includeProperties.ToArray();
                if (!CollectionUtils.IsNullOrEmpty(expressions))
                {
                    source = PerformInclusions(expressions, source);
                }
            }

            if (!CollectionUtils.IsNullOrEmpty(filters))
            {
                source = filters.Aggregate(source, (current, expression) => current.Where(expression));
            }

            return source;
        }

        #endregion

        #region Helper

        private static IQueryable<TR> PerformInclusions<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties,
            IQueryable<TR> query) where TR : class, T
        {
            return includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        }

        #endregion

        #region Context Management

        #region Sync

        protected virtual DbContext GetContext()
        {
            return GetContext("DefaultConnection");
        }

        protected virtual DbContext GetContext(string connection)
        {
            return Context;
        }

        public virtual int Commit()
        {
            return GetContext().SaveChanges();
        }

        #endregion

        #region Async

        public async virtual Task<int> CommitAsync()
        {
            return await GetContext().SaveChangesAsync();
        }

        #endregion

        #endregion

        #region CRUD Operations

        #region Sync

        public void Save(T entity)
        {
            SaveCore(entity);
            if (AlwaysCommit)
            {
                Commit();
            }
        }

        public void Update(T entity)
        {
            UpdateCore(entity, false);
        }

        public void Delete(T entity)
        {
            DeleteCore(entity, false);
        }

        public void Delete(params Expression<Func<T, bool>>[] filters)
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            var objects = !CollectionUtils.IsNullOrEmpty(filters)
                ? filters.Aggregate(dbset.OfType<T>(), (current, expression) => current.Where(expression))
                : dbset.AsQueryable();

            foreach (var obj in objects)
            {
                DeleteCore(obj, true);
            }
        }

        #endregion

        #region Async

        public async Task SaveAsync(T entity)
        {
            SaveCore(entity);
            if (AlwaysCommit)
            {
                await CommitAsync();
            }
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Run(() => UpdateCore(entity, true));
        }

        public async Task DeleteAsync(T entity)
        {
            await Task.Run(() => DeleteCore(entity, true));
        }

        #endregion

        #endregion

        #region Query Operations

        #region Sync

        public T GetById(TKey id)
        {
            var db = GetContext();
            return db.Set<T>().Find(id);
        }

   

        public TR First<TR>(params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return QueryCore(null, filters).FirstOrDefault();
        }

        public T First(params Expression<Func<T, bool>>[] filters)
        {
            return First<T>(filters);
        }

        public IQueryable<TR> GetAll<TR>(params Expression<Func<TR, object>>[] includeProperties) where TR : class, T
        {
            return QueryCore(includeProperties);
        }

        public IQueryable<T> GetAll(params Expression<Func<T, object>>[] includeProperties)
        {
            return GetAll<T>(includeProperties);
        }

        public IQueryable<TR> Find<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties,
            params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return QueryCore(includeProperties, filters);
        }

        public IQueryable<TR> Find<TR>(params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return Find(null, filters);
        }

        public IQueryable<T> Find(IEnumerable<Expression<Func<T, object>>> includeProperties,
            params Expression<Func<T, bool>>[] filters)
        {
            return Find<T>(includeProperties, filters);
        }

        public IQueryable<T> Find(params Expression<Func<T, bool>>[] filters)
        {
            return Find<T>(filters);
        }

        public int Count(params Expression<Func<T, bool>>[] filters)
        {
            return Find(filters).Count();
        }

        public virtual IQueryable<T> AllIncluding(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> source = GetContext().Set<T>();
            return includeProperties.Aggregate(source, (current, path) => current.Include(path));
        }

        #endregion

        #region Async

        public async Task<T> GetByIdAsync(TKey id)
        {
            var db = GetContext();
            return await db.Set<T>().FindAsync(id);
        }

        public async Task<TR> FirstAsync<TR>(params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return await QueryCore(null, filters).FirstOrDefaultAsync();
        }

        public async Task<T> FirstAsync(params Expression<Func<T, bool>>[] filters)
        {
            return await FirstAsync<T>(filters);
        }

        public async Task<int> CountAsync(params Expression<Func<T, bool>>[] filters)
        {
            return await QueryCore(null, filters).CountAsync();
        }

        #endregion

        #endregion
    }
}