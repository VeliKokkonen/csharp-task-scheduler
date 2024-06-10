public class TaskRunner : IDisposable
{
    private readonly JobStealingTaskScheduler _scheduler;
    private readonly TaskFactory _taskFactory;

    public TaskRunner(int maxDegreeOfParallelism)
    {
        _scheduler = new JobStealingTaskScheduler(maxDegreeOfParallelism);
        _taskFactory = new TaskFactory(_scheduler);
    }

    public Task RunAsync(Action action)
    {
        return _taskFactory.StartNew(action);
    }

    public Task<TResult> RunAsync<TResult>(Func<TResult> function)
    {
        return _taskFactory.StartNew(function);
    }

    public void Dispose()
    {
        _scheduler.Dispose();
    }
}
