public class TaskProgram : IDisposable
{
    private readonly TaskRunner _taskRunner;

    public TaskProgram(int maxDegreeOfParallelism)
    {
        _taskRunner = new TaskRunner(maxDegreeOfParallelism);
    }

    public async Task Run()
    {
        // Example tasks
        var tasks = new List<Task>();
        for (int i = 1; i <= 100; i++)
        {
            int taskNumber = i; // To avoid closure problem in loop
            tasks.Add(_taskRunner.RunAsync(() => HeavyComputation(taskNumber)));
        }
        await Task.WhenAll(tasks);
    }

    private long HeavyComputation(int taskNumber)
    {
        long result = 0;
        for (int i = 0; i < 1000000000; i++)
        {
            result += i * taskNumber;
        }
        return result;
    }

    public void Dispose()
    {
        _taskRunner.Dispose();
    }
}

public static class Program
{
    public static async Task Main()
    {
        using (var taskProgram = new TaskProgram(Environment.ProcessorCount))
        {
            await taskProgram.Run();
        }
    }
}