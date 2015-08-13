using System;

namespace Ef.Factory
{
    public class FactoryNonQueryEventArgs<T> : EventArgs where T : class
    {
        private T _entity;

        public FactoryNonQueryEventArgs()
            : this(null)
        {
        }

        public FactoryNonQueryEventArgs(T entity)
        {
            _entity = entity;
        }
    }
}