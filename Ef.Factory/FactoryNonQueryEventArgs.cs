using System;

namespace Ef.Factory
{
    public class FactoryNonQueryEventArgs<T> : EventArgs where T : class
    {
        public T Entity { get; set; }

        public FactoryNonQueryEventArgs()
            : this(null)
        {
        }

        public FactoryNonQueryEventArgs(T entity)
        {
            Entity = entity;
        }
    }
}