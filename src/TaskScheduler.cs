using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class JobStealingTaskScheduler : TaskScheduler
{
    private readonly List<ConcurrentQueue<Task>> _taskQueues;
    private readonly List<Thread> _threads;
    private readonly int _maxDegreeOfParallelism;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public JobStealingTaskScheduler(int maxDegreeOfParallelism)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _taskQueues = Enumerable.Range(0, _maxDegreeOfParallelism).Select(_ => new ConcurrentQueue<Task>()).ToList();
        _threads = new List<Thread>(_maxDegreeOfParallelism);
        _cancellationTokenSource = new CancellationTokenSource();

        for (int i = 0; i < _maxDegreeOfParallelism; i++)
        {
            var localQueue = _taskQueues[i];
            var thread = new Thread(() => WorkerThread(localQueue, i));
            thread.Start();
            _threads.Add(thread);
        }
    }

    private void WorkerThread(ConcurrentQueue<Task> localQueue, int index)
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (localQueue.TryDequeue(out var task) || StealTask(index, out task))
            {
                base.TryExecuteTask(task);
            }
            else
            {
                Thread.Sleep(1); // Adjust sleep for better performance
            }
        }
    }

    private bool StealTask(int currentIndex, out Task task)
    {
        for (int i = 0; i < _maxDegreeOfParallelism; i++)
        {
            if (i == currentIndex) continue;

            if (_taskQueues[i].TryDequeue(out task))
            {
                return true;
            }
        }

        task = null;
        return false;
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return _taskQueues.SelectMany(queue => queue.ToArray());
    }

    protected override void QueueTask(Task task)
    {
        var index = Thread.CurrentThread.ManagedThreadId % _maxDegreeOfParallelism;
        _taskQueues[index].Enqueue(task);
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (taskWasPreviouslyQueued)
        {
            return false;
        }
        return base.TryExecuteTask(task);
    }

    public override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }
}
