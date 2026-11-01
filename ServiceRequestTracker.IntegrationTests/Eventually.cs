namespace ServiceRequestTracker.IntegrationTests;

public static class Eventually
{
    public static async Task<T> WaitAsync<T>(
        Func<Task<T>> query,
        Func<T, bool> isDone,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        var delay = interval ?? TimeSpan.FromMilliseconds(200);

        while (true)
        {
            var result = await query();
            if (isDone(result) || DateTime.UtcNow >= deadline)
            {
                return result;
            }

            await Task.Delay(delay);
        }
    }
}