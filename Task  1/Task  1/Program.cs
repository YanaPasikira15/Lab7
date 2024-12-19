using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
public class DistributedSystemNode
{
    private readonly string _nodeId;
    private readonly ConcurrentQueue<string> _messageQueue = new();
    private readonly List<DistributedSystemNode> _connectedNodes = new();
    private volatile bool _isActive = true;
    public DistributedSystemNode(string nodeId) => _nodeId = nodeId;
    public string NodeId => _nodeId;
    public bool IsActive => _isActive;
    public void Connect(DistributedSystemNode node)
    {
        if (node != null && !_connectedNodes.Contains(node))
        {
            _connectedNodes.Add(node);
            node._connectedNodes.Add(this);
        }
    }
    public async Task SendMessageAsync(string message, DistributedSystemNode targetNode)
    {
        if (!_isActive) return;

        if (targetNode.IsActive)
        {
            await Task.Run(() => targetNode.ReceiveMessage($"{_nodeId}: {message}"));
        }
    }
    private void ReceiveMessage(string message) => _messageQueue.Enqueue(message);
    public async Task ProcessMessagesAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_messageQueue.TryDequeue(out var message))
            {
                Console.WriteLine($"{_nodeId} отримав повідомлення: {message}");
            }
            await Task.Delay(100);
        }
    }
    public async Task SetStatusAsync(bool isActive)
    {
        _isActive = isActive;
        var status = _isActive ? "активний" : "неактивний";
        var tasks = _connectedNodes.Select(node => node.ReceiveStatusUpdateAsync(_nodeId, status));
        await Task.WhenAll(tasks);
    }
    private async Task ReceiveStatusUpdateAsync(string nodeId, string status)
    {
        Console.WriteLine($"{_nodeId}: Отримав оновлення статусу від {nodeId}: {status}");
        await Task.Delay(50);
    }
}
class Program
{
    static async Task Main(string[] args)
    {
        var nodeA = new DistributedSystemNode("NodeA");
        var nodeB = new DistributedSystemNode("NodeB");
        var nodeC = new DistributedSystemNode("NodeC");
        nodeA.Connect(nodeB);
        nodeB.Connect(nodeC);
        var cancellationTokenSource = new CancellationTokenSource();
        var tasks = new List<Task>
        {
            nodeA.ProcessMessagesAsync(cancellationTokenSource.Token),
            nodeB.ProcessMessagesAsync(cancellationTokenSource.Token),
            nodeC.ProcessMessagesAsync(cancellationTokenSource.Token)
        };

        await nodeA.SendMessageAsync("Привіт, NodeB!", nodeB);
        await nodeB.SendMessageAsync("Привіт, NodeC!", nodeC);
        await nodeC.SendMessageAsync("Привіт, NodeA!", nodeA);
        await nodeB.SetStatusAsync(false);
        await nodeA.SendMessageAsync("Чи ти активний, NodeB?", nodeB);
        await Task.Delay(1000);
        cancellationTokenSource.Cancel();
        await Task.WhenAll(tasks);
    }
}