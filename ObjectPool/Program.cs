using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool
{
    class Program
    {
        private const int _concurrentTasks = 256;
        private static int _iterations;

        static void Main(string[] args)
        {
            Console.WriteLine($"Concurrent Tasks: {_concurrentTasks}{Environment.NewLine}");

            var pool = new ObjectPool<MyObject>(() => new MyObject());

            var printStatusTask = PrintStatus();

            var testTasks = new Task[_concurrentTasks];
            for (var i= 0; i < _concurrentTasks; i++)
            {
                testTasks[i] = Test(pool);
            }
            Task.WaitAll(testTasks);

            printStatusTask.Wait();
        }

        private static async Task PrintStatus()
        {
            while (true)
            {
                Console.Write($"\rIterations: {_iterations.ToString("N0")}");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task Test(ObjectPool<MyObject> pool)
        {
            while (true)
            {
                var obj = pool.GetObject();
                await Task.Yield();
                pool.PutObject(obj);
                Interlocked.Increment(ref _iterations);
            }
        }

        private class MyObject
        {
            public int Value { get; set; }
        }
    }
}
