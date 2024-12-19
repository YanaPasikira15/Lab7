using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Event
{
    public string EventId { get; }
    public string SenderId { get; }
    public int LamportTimestamp { get; }
    public Event(string eventId, string senderId, int lamportTimestamp)
    {
        EventId = eventId;
        SenderId = senderId;
        LamportTimestamp = lamportTimestamp;
    }
    public override string ToString() =>
        $"[Подія: {EventId}] Відправник: {SenderId}, Час: {LamportTimestamp}";
}

public class Node
{
    private readonly string _nodeId;
    private int _lamportClock;
    private readonly List<Node> _connectedNodes = new();
    private readonly ConcurrentQueue<Event> _eventLog = new();
    public Node(string nodeId)
    {
        _nodeId = nodeId;
        _lamportClock = 0;
    }
    public string NodeId => _nodeId;
    public void Connect(Node node)
    {
        if (!_connectedNodes.Contains(node))
        {
            _connectedNodes.Add(node);
            node._connectedNodes.Add(this);
        }
    }
    public void Disconnect(Node node)
    {
        _connectedNodes.Remove(node);
        node._connectedNodes.Remove(this);
    }
    public void TriggerEvent(string eventId)
    {
        _lamportClock++;
        var evt = new Event(eventId, _nodeId, _lamportClock);
        _eventLog.Enqueue(evt);
        foreach (var node in _connectedNodes)
        {
            node.ReceiveEvent(evt);
        }
        Console.WriteLine($"{_nodeId} створив {evt}");
    }
    public void ReceiveEvent(Event evt)
    {
        _lamportClock = Math.Max(_lamportClock, evt.LamportTimestamp) + 1;
        _eventLog.Enqueue(evt);
        Console.WriteLine($"{_nodeId} отримав {evt}");
    }
    public void DisplayEventLog()
    {
        var sortedLog = _eventLog.OrderBy(e => e.LamportTimestamp).ToList();
        Console.WriteLine($"Журнал подій для {_nodeId}:");
        foreach (var evt in sortedLog)
        {
            Console.WriteLine(evt);
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var nodeA = new Node("NodeA");
        var nodeB = new Node("NodeB");
        var nodeC = new Node("NodeC");
        nodeA.Connect(nodeB);
        nodeB.Connect(nodeC);
        var tasks = new List<Task>
        {
            Task.Run(() => nodeA.TriggerEvent("Event1")),
            Task.Run(() => nodeB.TriggerEvent("Event2")),
            Task.Run(() => nodeC.TriggerEvent("Event3")),
            Task.Delay(500).ContinueWith(_ => nodeA.TriggerEvent("Event4"))
        };
        await Task.WhenAll(tasks);
        Console.WriteLine("\nЖурнал подій:");
        nodeA.DisplayEventLog();
        nodeB.DisplayEventLog();
        nodeC.DisplayEventLog();
    }
}