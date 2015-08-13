using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ef.Factory
{
    public class GenericFactory<T, TKey> : IGenericFactoryAsync<T, TKey> where T : class
    {
        #region Properties

        private bool AlwaysCommit { get; set; }

        private DbContext Context { get; set; }

        public bool IsDisposed { get; private set; }

        #endregion

        #region Events

        public event EventHandler<FactoryNonQueryEventArgs<T>> Saving; 
        public event EventHandler<FactoryNonQueryEventArgs<T>> Updating; 
        public event EventHandler<FactoryNonQueryEventArgs<T>> Deleting; 
        public event EventHandler<FactoryNonQueryEventArgs<T>> Committing; 
        public event EventHandler<FactoryQueryEventArgs<T>> Querying; 

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

        #region Sync

        private void SaveCore(T entity)
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            if (Saving != null)
            {
                Saving(this, new FactoryNonQueryEventArgs<T>(entity));
            }

            dbset.Add(entity);
            if (AlwaysCommit)
            {
                Commit();
            }
        }

        private void UpdateCore(T entity)
        {
            var db = GetContext();
            db.Entry(entity).State = EntityState.Modified;

            if (Updating != null)
            {
                Updating(this, new FactoryNonQueryEventArgs<T>(entity));
            }

            if (AlwaysCommit)
            {
                Commit();
            }
        }

        private void DeleteCore(T entity)
        {
            var db = GetContext();
            try
            {
                var dbset = db.Set<T>();

                if (Deleting != null)
                {
                    Deleting(this, new FactoryNonQueryEventArgs<T>(entity));
                }

                var logicalDelete = entity as ILogicalDelete;

                if (logicalDelete != null)
                {
                    logicalDelete.IsDeleted = true;
                }
                else
                {
                    dbset.Remove(entity);
                }

                if (AlwaysCommit)
                {
                    Commit();
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

        private IQueryable<TR> QueryCore<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties,
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

            if (Querying != null)
            {
                Querying(this, new FactoryQueryEventArgs<T>(source));
            }

            return source;
        }

        #endregion

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
            return Context;
        }

        public virtual int Commit()
        {
            if (Committing != null)
            {
                Committing(this, new FactoryNonQueryEventArgs<T>());
            }

            return GetContext().SaveChanges();
        }

        #endregion

        #region Async

        public async virtual Task<int> CommitAsync()
        {
            if (Committing != null)
            {
                Committing(this, new FactoryNonQueryEventArgs<T>());
            }

            return await GetContext().SaveChangesAsync();
        }

        #endregion

        #endregion

        #region CRUD Operations

        #region Sync

        public void Save(T entity)
        {
            SaveCore(entity);
        }

        public void Update(T entity)
        {
            UpdateCore(entity);
        }

        public void Delete(T entity)
        {
            DeleteCore(entity);
        }

        public void Delete(params Expression<Func<T, bool>>[] filters)
        {
            var db = GetContext();
            var dbset = db.Set<T>();

            var objects = !CollectionUtils.IsNullOrEmpty(filters)
                ? filters.Aggregate(dbset.OfType<T>(), (current, expression) => current.Where(expression))
                : dbset.AsQueryable();

            if (typeof(ILogicalDelete).IsAssignableFrom(typeof(T)))
            {
                foreach (var obj in objects)
                {
                    var logicalDelete = obj as ILogicalDelete;
                    if (logicalDelete != null)
                    {
                        logicalDelete.IsDeleted = true;
                    }
                }
            }
            else
            {
                dbset.RemoveRange(objects);
            }

            if (AlwaysCommit)
            {
                Commit();
            }
        }

        #endregion

        #region Async

        public async Task SaveAsync(T entity)
        {
            await Task.Run(() => SaveCore(entity));
        }

        public async Task UpdateAsync(T entity)
        {
            await Task.Run(() => UpdateCore(entity));
        }

        public async Task DeleteAsync(T entity)
        {
            await Task.Run(() => DeleteCore(entity));
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

        public T First(IEnumerable<Expression<Func<T, object>>> includeProperties, params Expression<Func<T, bool>>[] filters)
        {
            return QueryCore(includeProperties, filters).FirstOrDefault();
        }

        public TR First<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties, params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return QueryCore(includeProperties, filters).FirstOrDefault();
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

        public async Task<T> FirstAsync(IEnumerable<Expression<Func<T, object>>> includeProperties, params Expression<Func<T, bool>>[] filters)
        {
            return await FirstAsync<T>(includeProperties, filters);
        }

        public async Task<TR> FirstAsync<TR>(IEnumerable<Expression<Func<TR, object>>> includeProperties, params Expression<Func<TR, bool>>[] filters) where TR : class, T
        {
            return await QueryCore(includeProperties, filters).FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync(params Expression<Func<T, bool>>[] filters)
        {
            return await QueryCore(null, filters).CountAsync();
        }

        #endregion

        #endregion
    }
}