namespace subtitle_ocr_console.Utils;

public class BoundedTaskScheduler
{
    private SemaphoreSlim _pool;

    public BoundedTaskScheduler(int maxTasks)
    {
        _pool = new(maxTasks);
    }

    public async Task Schedule<T>(Task<T> task)
    {
        var newTask = task.ContinueWith(_ => _pool.Release());

        // TODO: Check result of WaitAsync
        await _pool.WaitAsync();

        task.Start();
    }
}