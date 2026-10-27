namespace Archeo.Core;

/// <summary>A single connected search from the player's tile; unreachable destinations never receive arrows.</summary>
public sealed class FieldRoute
{
    private readonly Dictionary<GridPoint, GridPoint> previous = new();
    public GridPoint Start { get; }
    public IEnumerable<GridPoint> Reachable => previous.Keys;
    public bool Contains(GridPoint point) => previous.ContainsKey(point);
    public FieldRoute(GridPoint start, Func<GridPoint, bool> walkable, int limit = 40000)
    {
        Start = start;
        if (limit < 1) return;
        var queue = new Queue<GridPoint>();
        previous[start] = start; queue.Enqueue(start);
        var offsets = new[] { new GridPoint(0, -1), new GridPoint(1, 0), new GridPoint(0, 1), new GridPoint(-1, 0) };
        while (queue.Count > 0)
        {
            var at = queue.Dequeue();
            foreach (var offset in offsets)
            {
                var next = new GridPoint(at.X + offset.X, at.Y + offset.Y);
                if (previous.Count >= limit || previous.ContainsKey(next) || !walkable(next)) continue;
                previous[next] = at; queue.Enqueue(next);
            }
        }
    }
    public List<GridPoint> PathTo(GridPoint goal)
    {
        if (!Contains(goal)) return new();
        var result = new List<GridPoint> { goal };
        while (goal != Start) { goal = previous[goal]; result.Add(goal); }
        result.Reverse(); return result;
    }
    public static GridPoint Waypoint(IReadOnlyList<GridPoint> path)
    {
        if (path.Count == 0) throw new ArgumentException("A route is required.");
        if (path.Count < 3) return path[^1];
        var delta = new GridPoint(path[1].X - path[0].X, path[1].Y - path[0].Y);
        for (int i = 2; i < Math.Min(path.Count, 7); i++)
            if (path[i].X - path[i - 1].X != delta.X || path[i].Y - path[i - 1].Y != delta.Y) return path[i - 1];
        return path[Math.Min(6, path.Count - 1)];
    }
}
