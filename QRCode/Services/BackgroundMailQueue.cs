using System.Collections.Concurrent;

namespace QRCode.Services;

public class BackgroundMailQueue<T> : IBackgroundQueue<T> where T : class
{
    private readonly ConcurrentQueue<T> _items = new();

    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        _items.Enqueue(item);
    }

    public T Dequeue()
    {
        var succcess = _items.TryDequeue(out var workItem);
        return succcess ? workItem! : throw new InvalidOperationException("Queue is empty");
    }
}
