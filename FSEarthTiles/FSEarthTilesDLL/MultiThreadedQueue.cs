using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FSEarthTilesDLL
{
    // credit to: https://michaelscodingspot.com/c-job-queues/
    // I didn't feel like writing my own producer/consumer :)
    // I've made some modifications to gracefully stop the consumer processes
    class MultiThreadedQueue
    {
        public BlockingCollection<string> _jobs = new BlockingCollection<string>();
        private List<Thread> threads;
        private CancellationTokenSource stopFlag;

        public MultiThreadedQueue(int numThreads)
        {
            stopFlag = new CancellationTokenSource();
            threads = new List<Thread>(numThreads);
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                threads.Add(thread);
                thread.Start();
            }
        }

        public void Enqueue(string job)
        {
            if (!_jobs.IsAddingCompleted)
            {
                _jobs.Add(job);
            }
        }

        public void Stop()
        {
            //This will cause '_jobs.GetConsumingEnumerable' to stop blocking and exit when it's empty
            _jobs.CompleteAdding();
            stopFlag.Cancel();
            _jobs = new BlockingCollection<string>();
            foreach (Thread t in threads)
            {
                t.Abort();
            }
            threads = new List<Thread>();
        }

        public delegate bool JobHandler(string job);

        public JobHandler jobHandler;

        private void OnHandlerStart()
        {
            try
            {
                foreach (var job in _jobs.GetConsumingEnumerable(stopFlag.Token))
                {
                    jobHandler(job);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}

