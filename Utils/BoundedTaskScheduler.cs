namespace subtitle_ocr_console.Utils;

// Provides a task scheduler that ensures a maximum concurrency level while
// running on top of the thread pool.
// Based on source: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler?view=net-6.0
public class BoundedTaskScheduler : TaskScheduler
{
    // Used to asynchronously block thread that calls QueueTask if queue is full
    private readonly SemaphoreSlim _queuePool;

    // Indicates whether the current thread is processing work items.
    [ThreadStatic]
    private static bool _currentThreadIsProcessingItems;

    // The list of tasks to be executed
    private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)

    // The maximum concurrency level allowed by this scheduler.
    private readonly int _maxDegreeOfParallelism;

    // Indicates whether the scheduler is currently processing work items.
    private int _delegatesQueuedOrRunning = 0;

    // Creates a new instance with the specified degree of parallelism.
    public BoundedTaskScheduler(int maxQueueSize, int maxDegreeOfParallelism)
    {
        if (maxQueueSize < 1) throw new ArgumentOutOfRangeException("maxQueueSize");
        if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
        _queuePool = new(maxQueueSize);
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    // Queues a task to the scheduler.
    protected sealed override void QueueTask(Task task)
    {
        // Wait for queue to be non-full
        // TODO: Check result of WaitAsync
        _queuePool.Wait();

        // Add the task to the list of tasks to be processed.  If there aren't enough
        // delegates currently queued or running to process tasks, schedule another.
        lock (_tasks)
        {
            _tasks.AddLast(task);
            if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
        }
    }

    // Inform the ThreadPool that there's work to be executed for this scheduler.
    private void NotifyThreadPoolOfPendingWork()
    {
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            // Note that the current thread is now processing work items.
            // This is necessary to enable inlining of tasks into this thread.
            _currentThreadIsProcessingItems = true;
            try
            {
                // Process all available items in the queue.
                while (true)
                {
                    Task item;
                    lock (_tasks)
                    {
                        // When there are no more items to be processed,
                        // note that we're done processing, and get out.
                        if (_tasks.Count == 0)
                        {
                            --_delegatesQueuedOrRunning;
                            break;
                        }

                        // Get the next item from the queue
                        // Ignore warning because we checked _tasks.Count just above and
                        // we have a lock
#pragma warning disable CS8602
                        item = _tasks.First.Value;
#pragma warning restore CS8602
                        _tasks.RemoveFirst();
                        // Release a queue spot back to the pool
                        _queuePool.Release();
                    }

                    // Execute the task we pulled out of the queue
                    base.TryExecuteTask(item);
                }
            }
            // We're done processing items on the current thread
            finally { _currentThreadIsProcessingItems = false; }
        }, null);
    }

    // Attempts to execute the specified task on the current thread.
    protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        // If this thread isn't already processing a task, we don't support inlining
        if (!_currentThreadIsProcessingItems) return false;

        // If the task was previously queued, remove it from the queue
        if (taskWasPreviouslyQueued)
            // Try to run the task.
            if (TryDequeue(task))
            {
                // Release a queue spot back to the pool
                _queuePool.Release();

                return base.TryExecuteTask(task);
            }
            else
                return false;
        else
            return base.TryExecuteTask(task);
    }

    // Attempt to remove a previously scheduled task from the scheduler.
    protected sealed override bool TryDequeue(Task task)
    {
        lock (_tasks) return _tasks.Remove(task);
    }

    // Gets the maximum concurrency level supported by this scheduler.
    public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

    // Gets an enumerable of the tasks currently scheduled on this scheduler.
    protected sealed override IEnumerable<Task> GetScheduledTasks()
    {
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_tasks, ref lockTaken);
            if (lockTaken) return _tasks;
            else throw new NotSupportedException();
        }
        finally
        {
            if (lockTaken) Monitor.Exit(_tasks);
        }
    }
}