using System.Numerics;

namespace Archeo.Core;

/// <summary>Walks the owner's actual trail, keeping a short following distance.</summary>
public sealed class Trail
{
    public Vector2 Position { get; private set; }
    public bool Moving { get; private set; }
    private readonly Queue<Vector2> points = new();
    private Vector2 previous;
    private bool initialized;

    public void Reset(Vector2 owner)
    {
        Position = previous = owner;
        points.Clear();
        Moving = false;
        initialized = true;
    }

    public void Update(Vector2 owner, float seconds, Func<Vector2, bool> canStand)
    {
        if (!initialized || Vector2.Distance(owner, previous) > 768 || points.Count > 128) Reset(owner);
        if (Vector2.Distance(owner, previous) >= 12) { points.Enqueue(owner); previous = owner; }
        Moving = false;
        if (points.Count == 0) return;
        float length = 0;
        Vector2 last = Position;
        foreach (var point in points) { length += Vector2.Distance(last, point); last = point; }
        if (length <= 68) return;
        float step = Math.Min(200 * Math.Clamp(seconds, 0, .05f), length - 68);
        while (step > .01f && points.Count > 0)
        {
            Vector2 target = points.Peek();
            float distance = Vector2.Distance(Position, target);
            if (distance < .1f) { points.Dequeue(); continue; }
            Vector2 next = Vector2.Lerp(Position, target, Math.Min(1, step / distance));
            if (!canStand(next)) { Reset(owner); return; }
            Position = next;
            Moving = true;
            if (step >= distance) { points.Dequeue(); step -= distance; } else break;
        }
    }
}
