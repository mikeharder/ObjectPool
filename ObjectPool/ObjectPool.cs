using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ObjectPool
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _availableObjects;
        private readonly ConcurrentDictionary<T, object> _leasedObjects;
        private readonly Func<T> _objectGenerator;

        public int AvailableObjectsCount
        {
            get
            {
                return _availableObjects.Count;
            }
        }

        public int LeasedObjectsCount
        {
            get
            {
                return _leasedObjects.Count;
            }
        }

        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator;

            _availableObjects = new ConcurrentBag<T>();
            _leasedObjects = new ConcurrentDictionary<T, object>();
        }

        public T GetObject(object owner)
        {
            T item;
            if (!_availableObjects.TryTake(out item))
            {
                item = _objectGenerator();
            }

            if (!_leasedObjects.TryAdd(item, owner))
            {
                Console.WriteLine("Failed add");
            }

            return item;
        }

        public bool PutObject(T item, object owner)
        {
            if (_leasedObjects.TryGetValue(item, out var leasedOwner) &&
                Object.Equals(leasedOwner, owner))
            {
                while (!_leasedObjects.TryRemove(item, out var leasedOwner2)) { }
            }
            else
            {
                // Object is not leased, or leased by a different owner
                return false;
            }

            _availableObjects.Add(item);
            return true;
        }
    }
}
