using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
public class Operation
{
    public string ResourceId { get; }
    public string ThreadId { get; }
    public DateTime Timestamp { get; }
    public Operation(string resourceId, string threadId)
    {
        ResourceId = resourceId;
        ThreadId = threadId;
        Timestamp = DateTime.UtcNow;
    }
    public override string ToString() =>
        $"[{Timestamp:HH:mm:ss.fff}] Потік: {ThreadId}, Ресурс: {ResourceId}";
}
public class ConflictManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, Operation> _resourceLocks = new();

    public bool TryLogOperation(Operation operation)
    {
        lock (_lock)
        {
            if (_resourceLocks.ContainsKey(operation.ResourceId))
            {
                Console.WriteLine($"Конфлікт: Ресурс {operation.ResourceId} вже зайнятий {operation.ThreadId}.");
                return false;
            }
            _resourceLocks[operation.ResourceId] = operation;
            Console.WriteLine($"Операція записана: {operation}");
            return true;
        }
    }
    public void ReleaseResource(string resourceId, string threadId)
    {
        lock (_lock)
        {
            if (_resourceLocks.TryGetValue(resourceId, out var operation) && operation.ThreadId == threadId)
            {
                _resourceLocks.Remove(resourceId);
                Console.WriteLine($"Ресурс {resourceId} звільнений {threadId}.");
            }
        }
    }
}

public class LiveLog
{
    private readonly ConcurrentQueue<Operation> _operationLog = new();
    private readonly ConflictManager _conflictManager = new();

    public async Task PerformOperationAsync(string resourceId, string threadId)
    {
        var operation = new Operation(resourceId, threadId);
        while (!_conflictManager.TryLogOperation(operation))
        {
            await Task.Delay(100); // Очікування перед повторною спробою
        }

        _operationLog.Enqueue(operation);
        await Task.Delay(500); // Симуляція роботи з ресурсом
        _conflictManager.ReleaseResource(resourceId, threadId);
    }
    public void DisplayLog()
    {
        foreach (var operation in _operationLog)
        {
            Console.WriteLine(operation);
        }
    }
}
class Program
{
    static async Task Main(string[] args)
    {
        var liveLog = new LiveLog();
        var resources = new[] { "CPU", "RAM", "Disk" };
        var tasks = new List<Task>();
        for (int i = 1; i <= 5; i++)
        {
            var threadId = $"Потік-{i}";
            tasks.Add(Task.Run(async () =>
            {
                var random = new Random();
                for (int j = 0; j < 5; j++)
                {
                    var resource = resources[random.Next(resources.Length)];
                    await liveLog.PerformOperationAsync(resource, threadId);
                }
            }));
        }
        await Task.WhenAll(tasks);
        Console.WriteLine("\nЖурнал операцій:");
        liveLog.DisplayLog();
    }
}