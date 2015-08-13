using System;
using System.Linq;

namespace Ef.Factory
{
    public class FactoryQueryEventArgs<T> : EventArgs where T : class
    {
        private IQueryable<T> _items;

        public FactoryQueryEventArgs(IQueryable<T> items)
        {
            _items = items;
        }
    }
}