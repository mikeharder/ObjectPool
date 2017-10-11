using System;
using System.Collections.Concurrent;

namespace ObjectPool
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T> _objectGenerator;

        public ObjectPool(Func<T> objectGenerator)
        {
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T GetObject()
        {
            if (_objects.TryTake(out var item))
            {
                return item;
            }
            else
            {
                return _objectGenerator();
            }
        }

        public void PutObject(T item)
        {
            _objects.Add(item);
        }
    }
}
