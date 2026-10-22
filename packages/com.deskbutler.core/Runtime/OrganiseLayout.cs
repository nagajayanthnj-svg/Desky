using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    /// <summary>
    /// Pure layout planner for "Clean up the desk". Returns target poses; the agent applies them.
    /// Groups by primary tag (one column per group, untagged last), High priority first within a group.
    /// Locked items never move, and no target lands on top of one.
    /// </summary>
    public static class OrganiseLayout
    {
        public const float ColumnSpacing = 0.12f;  // metres between group columns
        public const float RowSpacing = 0.09f;     // metres between items in a column
        public const float Margin = 0.05f;
        public const float LockedClearance = 0.07f;

        public static Dictionary<string, Pose> Plan(IReadOnlyList<DeskItem> items, DeskBounds bounds)
        {
            var result = new Dictionary<string, Pose>();
            var lockedSpots = items.Where(i => i.Locked).Select(i => i.LocalPose.Position).ToList();

            var groups = items.Where(i => !i.Locked)
                .GroupBy(i => i.PrimaryTag == null ? null : i.PrimaryTag.ToLowerInvariant())
                .OrderBy(g => g.Key == null ? 1 : 0)
                .ThenBy(g => g.Key)
                .ToList();

            var x = bounds.MinX + Margin;
            var zStart = bounds.MinZ + Margin;
            var zEnd = bounds.MaxZ - Margin;

            foreach (var group in groups)
            {
                var ordered = group.OrderByDescending(i => i.Priority).ThenBy(i => i.CreatedAtMs).ToList();
                var z = zStart;
                foreach (var item in ordered)
                {
                    Vec3 spot;
                    while (true)
                    {
                        if (z > zEnd) { x += ColumnSpacing; z = zStart; }   // wrap to a new column
                        spot = new Vec3(x, 0f, z);
                        z += RowSpacing;
                        var s = spot;
                        if (!lockedSpots.Any(p => Flat(p, s) < LockedClearance)) break;
                    }
                    result[item.Id] = new Pose(spot, Quat.Identity, item.LocalPose.Scale);
                }
                x += ColumnSpacing;
            }
            return result;
        }

        static float Flat(Vec3 a, Vec3 b)
        {
            return Vec3.Distance(new Vec3(a.X, 0, a.Z), new Vec3(b.X, 0, b.Z));
        }
    }
}
