using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace ObjectPool
{
    class Program
    {
        private const int _concurrentTasks = 128;

        private static readonly ConcurrentRandom _random = new ConcurrentRandom();

        private static readonly ConcurrentDictionary<string, string> _status = new ConcurrentDictionary<string, string>();

        static void Main(string[] args)
        {
            Console.WriteLine($"Concurrent Tasks: {_concurrentTasks}{Environment.NewLine}");

            var printStatusTask = PrintStatus();

            var testTasks = new List<Task>
            {
                Test("Guid\t\t", () => Guid.NewGuid()),
                Test("ExecutionContext", () => Thread.CurrentThread.ExecutionContext)
            };
            Task.WaitAll(testTasks.ToArray());

            printStatusTask.Wait();
        }

        private static async Task PrintStatus()
        {
            var top = Console.CursorTop;
            while (true)
            {
                Console.SetCursorPosition(0, top);

                foreach (var status in _status.Values.OrderBy(s => s))
                {
                    Console.WriteLine(status);
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task Test(string name, Func<object> generateOwner)
        {
            var iterations = 0;
            var failures = 0;

            var pool = new ObjectPool<MyObject>(() => new MyObject());

            var updateStatusTask = UpdateStatus(name, pool, () => iterations, () => failures);

            var testTasks = new Task[_concurrentTasks];
            for (var i = 0; i < _concurrentTasks; i++)
            {
                testTasks[i] = Test(pool, generateOwner,
                    () => Interlocked.Increment(ref iterations), () => Interlocked.Increment(ref failures));
            }
            await Task.WhenAll(testTasks);

            await updateStatusTask;
        }

        private static async Task UpdateStatus(string name, ObjectPool<MyObject> pool,
            Func<int> iterations, Func<int> failures)
        {
            while (true)
            {
                var status = $"{name}\t" +
                    $"Available Objects: {pool.AvailableObjectsCount}" +
                    $", Leased Objects: {pool.LeasedObjectsCount}" +
                    $", Iterations: {iterations().ToString("N0")}" +
                    $", Failures: {failures().ToString("N0")}";

                _status.AddOrUpdate(name, status, (n, s) => status);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task Test(ObjectPool<MyObject> pool, Func<object> generateOwner,
            Action incrementIterations, Action incrementFailures)
        {
            while (true)
            {
                var owner = generateOwner();

                var rand = _random.Next();
                var obj = pool.GetObject(owner);

                obj.Value = rand;

                await Task.Yield();

                if (obj.Value != rand)
                {
                    incrementFailures();
                }

                pool.PutObject(obj, owner);

                await Task.Yield();

                pool.PutObject(obj, owner);

                incrementIterations();
            }
        }

        private class MyObject
        {
            public int Value { get; set; }
        }
    }
}
