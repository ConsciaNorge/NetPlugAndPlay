using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace libterminal.JobRunner
{
    public class QueuedJob
    {
        public Job Job { get; set; }
        public AutoResetEvent SignalFinished = new AutoResetEvent(false);
        public bool Success { get; set; }
    }

    public class JobRunner
    {
        private Thread WorkerThread = null;
        private ConcurrentQueue<QueuedJob> JobQueue = new ConcurrentQueue<QueuedJob>();
        private AutoResetEvent Signal = new AutoResetEvent(false);
        private bool EndWorker = false;

        public JobRunner()
        {
            WorkerThread = new Thread(WorkerFunction);
            WorkerThread.Start();
        }

        public void Stop()
        {
            EndWorker = true;
            Signal.Set();
            if(!WorkerThread.Join(1000))
            {
                WorkerThread.Abort();
            }
        }

        private void WorkerFunction(object obj)
        {
            while(true)
            {
                if(JobQueue.IsEmpty)
                    Signal.WaitOne();

                if (EndWorker)
                    break;

                QueuedJob activeJob;
                if(JobQueue.TryDequeue(out activeJob))
                {
                    if (activeJob == null || activeJob.Job == null)
                    {
                        // TODO : Handle bad job
                        continue;
                    }

                    var job = activeJob.Job;

                    System.Diagnostics.Debug.WriteLine("Job dequeued : " + job.Id);

                    var currentTask = job.Tasks.Where(x => x.Name == job.InitialTask).FirstOrDefault();
                    if (currentTask == null)
                        continue;

                    bool jobComplete = false;
                    while (!jobComplete)
                    {
                        var processedDescription = job.Value(currentTask.Description);
                        System.Diagnostics.Debug.WriteLine("Task description - " + processedDescription);

                        var result = currentTask.Execute(job);
                        System.Diagnostics.Debug.WriteLine(result);

                        switch(result)
                        {
                            case "CriticalError":
                                activeJob.Success = false;
                                jobComplete = true;
                                break;
                            case "Done":
                                activeJob.Success = true;
                                jobComplete = true;
                                break;
                            default:
                                currentTask = job.Tasks.Where(x => x.Name == result).FirstOrDefault();
                                if (currentTask == null)
                                {
                                    activeJob.Success = false;
                                    jobComplete = true;
                                }
                                break;
                        }
                    }
                    activeJob.SignalFinished.Set();
                }
            }
        }

        public JobResult ExecuteJob(Job job, int timeoutMs=30000)
        {
            var jobHolder = new QueuedJob { Job = job };
            JobQueue.Enqueue(jobHolder);
            Signal.Set();

            if(!jobHolder.SignalFinished.WaitOne(timeoutMs))
            {
                System.Diagnostics.Debug.WriteLine("FAIL!!!!");
                return new JobResult { Success = false };
            }

            var result = jobHolder.Job.Results;

            return new JobResult
            {
                Success = jobHolder.Success,
                Values = result
            };
        }
    }
}
