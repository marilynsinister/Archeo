namespace Archeo.Core;

public readonly record struct JournalControl(string Id, int X, int Y, int Width, int Height);

/// <summary>Spatial focus navigation shared by every journal page, including disabled-control exclusion by the caller.</summary>
public static class JournalNavigation
{
    public static string Move(IReadOnlyList<JournalControl> controls, string current, int dx, int dy)
    {
        if (controls.Count == 0) return "";
        int index = controls.ToList().FindIndex(p => p.Id == current);
        if (index < 0) return controls[0].Id;
        var origin = controls[index];
        double Score(JournalControl candidate)
        {
            double x = candidate.X + candidate.Width / 2d - origin.X - origin.Width / 2d;
            double y = candidate.Y + candidate.Height / 2d - origin.Y - origin.Height / 2d;
            double forward = x * dx + y * dy, sideways = Math.Abs(x * dy - y * dx);
            return forward <= 0 ? double.MaxValue : forward + sideways * 3;
        }
        var next = controls.Where(p => p.Id != current && Score(p) < double.MaxValue).OrderBy(Score).FirstOrDefault();
        return next.Id ?? current;
    }
}
