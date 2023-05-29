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
        public BlockingCollection<MasksResampleWorker> _jobs = new BlockingCollection<MasksResampleWorker>();
        private List<Thread> threads;
        private int threadsRunning = 0;
        private readonly object threadsRunningLock = new object();
        private readonly object totalJobsLock = new object();
        private CancellationTokenSource stopFlag;
        private bool doneAdding = false;
        private long totalJobsRan = 0;

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

        public void Enqueue(MasksResampleWorker job)
        {
            if (!_jobs.IsAddingCompleted)
            {
                _jobs.Add(job);
            }
        }

        // can't use CompleteAdding of BlockingCollection
        // because then jobs don't get called.
        // TODO: I must be misunderstanding it. Try it again and use
        // BlockingCollection's CompleteAdding
        public void CompleteAdding()
        {
            doneAdding = true;
        }

        public void UncompleteAdding()
        {
            doneAdding = false;
        }

        public int GetNumRunningThreads()
        {
            int running = 0;
            lock (threadsRunningLock)
            {
                running = threadsRunning;
            }

            return running;
        }

        public long GetNumJobsWaiting()
        {
            // no point locking this
            return _jobs.Count;
        }

        public long GetTotalJobsDone()
        {
            long done = 0;
            lock (totalJobsLock)
            {
                done = totalJobsRan;
            }

            return done;
        }

        public void SetTotalJobsDone(long done)
        {
            lock (totalJobsLock)
            {
                totalJobsRan = done;
            }
        }

        public void IncremenntTotalJobsDoneBy(long increment)
        {
            lock (totalJobsLock)
            {
                totalJobsRan += increment;
            }
        }

        public bool AllThreadsDone()
        {
            return GetNumRunningThreads() == 0;
        }

        public bool AllDone()
        {
            return AllThreadsDone() && doneAdding;
        }

        public void Stop()
        {
            //This will cause '_jobs.GetConsumingEnumerable' to stop blocking and exit when it's empty
            _jobs.CompleteAdding();
            stopFlag.Cancel();
            _jobs = new BlockingCollection<MasksResampleWorker>();
            foreach (Thread t in threads)
            {
                t.Abort();
            }
            threads = new List<Thread>();
            lock (threadsRunningLock)
            {
                threadsRunning = 0;
            }
            SetTotalJobsDone(0);
            CompleteAdding();
        }

        public delegate void JobHandler(MasksResampleWorker job);

        public JobHandler jobHandler;

        private void OnHandlerStart()
        {
            try
            {
                foreach (var job in _jobs.GetConsumingEnumerable(stopFlag.Token))
                {
                    lock (threadsRunningLock)
                    {
                        threadsRunning++;
                    }
                    jobHandler(job);
                    lock (threadsRunningLock)
                    {
                        threadsRunning--;
                    }
                    lock (totalJobsLock)
                    {
                        totalJobsRan++;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                lock (threadsRunningLock)
                {
                    threadsRunning = 0;
                }
                SetTotalJobsDone(0);
            }
        }
    }
}

