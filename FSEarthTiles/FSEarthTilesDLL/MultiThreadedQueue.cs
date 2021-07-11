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
    class MultiThreadedQueue
    {
        BlockingCollection<string> _jobs = new BlockingCollection<string>();

        public MultiThreadedQueue(int numThreads)
        {
            for (int i = 0; i < numThreads; i++)
            {
                var thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
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
        }

        public delegate bool JobHandler(string job);

        public JobHandler jobHandler;

        private void OnHandlerStart()
        {
            foreach (var job in _jobs.GetConsumingEnumerable(CancellationToken.None))
            {
                jobHandler(job);
            }
        }
    }
}
