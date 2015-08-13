using System;
using System.Linq;

namespace Ef.Factory
{
    public class FactoryQueryEventArgs<T> : EventArgs where T : class
    {
        public IQueryable<T> Items { get; set; }

        public FactoryQueryEventArgs(IQueryable<T> items)
        {
            Items = items;
        }
    }
}