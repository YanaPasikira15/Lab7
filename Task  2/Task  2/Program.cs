using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ResourceManager
{
    private readonly SemaphoreSlim _cpuSemaphore;
    private readonly SemaphoreSlim _ramSemaphore;
    private readonly SemaphoreSlim _diskSemaphore;
    private readonly object _priorityLock = new();
    public ResourceManager(int cpuCount, int ramCount, int diskCount)
    {
        _cpuSemaphore = new SemaphoreSlim(cpuCount);
        _ramSemaphore = new SemaphoreSlim(ramCount);
        _diskSemaphore = new SemaphoreSlim(diskCount);
    }

    public async Task<bool> RequestResourcesAsync(int priority, string threadName)
    {
        lock (_priorityLock)
        {
            Console.WriteLine($"{threadName} (Пріоритет {priority}) запитує ресурси...");
        }

        var cpuAcquired = await _cpuSemaphore.WaitAsync(0);
        var ramAcquired = await _ramSemaphore.WaitAsync(0);
        var diskAcquired = await _diskSemaphore.WaitAsync(0);

        if (cpuAcquired && ramAcquired && diskAcquired)
        {
            Console.WriteLine($"{threadName} отримав ресурси.");
            await Task.Delay(1000); // Симуляція використання ресурсів
            ReleaseResources();
            Console.WriteLine($"{threadName} звільнив ресурси.");
            return true;
        }
        else
        {
            if (cpuAcquired) _cpuSemaphore.Release();
            if (ramAcquired) _ramSemaphore.Release();
            if (diskAcquired) _diskSemaphore.Release();
            lock (_priorityLock)
            {
                Console.WriteLine($"{threadName} не отримав ресурси.");
            }
            return false;
        }
    }
    private void ReleaseResources()
    {
        _cpuSemaphore.Release();
        _ramSemaphore.Release();
        _diskSemaphore.Release();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var resourceManager = new ResourceManager(cpuCount: 2, ramCount: 2, diskCount: 1);
        var threads = new List<Task>();
        for (int i = 1; i <= 10; i++)
        {
            int priority = i % 3 + 1; // Пріоритет: 1, 2 або 3
            var threadName = $"Потік-{i}";
            threads.Add(Task.Run(async () =>
            {
                while (!await resourceManager.RequestResourcesAsync(priority, threadName))
                {
                    await Task.Delay(priority * 100); // Затримка залежить від пріоритету
                }
            }));
        }
        await Task.WhenAll(threads);
        Console.WriteLine("Всі потоки завершили роботу.");
    }
}
