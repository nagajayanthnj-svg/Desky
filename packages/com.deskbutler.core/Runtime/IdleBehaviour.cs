using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    /// <summary>A render-only offset on top of an item's saved pose.</summary>
    public struct VisualOffset
    {
        public Vec3 Position;
        public float YawDegrees;

        public VisualOffset(Vec3 position, float yawDegrees) { Position = position; YawDegrees = yawDegrees; }
    }

    /// <summary>
    /// Idle personality behaviour is cosmetic (D-006): it returns visual offsets the renderer applies
    /// on top of saved poses. It never mutates the Desk, never touches locked or hidden items,
    /// and offsets are discarded on restart. Alden's "straightening" = clearing Pip's offsets.
    /// </summary>
    public static class IdleBehaviour
    {
        public const float PipMaxOffsetMetres = 0.015f;
        public const float PipMaxYawDegrees = 5f;

        /// <summary>Pip nudges one random eligible item. Returns the full offset map to render.</summary>
        public static Dictionary<string, VisualOffset> PipNudge(
            IReadOnlyList<DeskItem> items, Dictionary<string, VisualOffset> current, Random rng)
        {
            var next = current.Where(kv => Eligible(items, kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            var candidates = items.Where(i => !i.Locked && !i.HiddenByFocus).ToList();
            if (candidates.Count == 0) return next;

            var pick = candidates[rng.Next(candidates.Count)];
            var dx = Range(rng, PipMaxOffsetMetres);
            var dz = Range(rng, PipMaxOffsetMetres);
            next[pick.Id] = new VisualOffset(new Vec3(dx, 0, dz), Range(rng, PipMaxYawDegrees));
            return next;
        }

        /// <summary>Alden tidies: every crooked item animates back to its saved pose.</summary>
        public static Dictionary<string, VisualOffset> AldenStraighten()
        {
            return new Dictionary<string, VisualOffset>();
        }

        /// <summary>Ids Alden should walk to and "straighten" (for the animation), in order.</summary>
        public static List<string> AldenTargets(Dictionary<string, VisualOffset> current)
        {
            return current.Keys.OrderBy(k => k).ToList();
        }

        static bool Eligible(IReadOnlyList<DeskItem> items, string id)
        {
            var item = items.FirstOrDefault(i => i.Id == id);
            return item != null && !item.Locked && !item.HiddenByFocus;
        }

        static float Range(Random rng, float max) { return (float)(rng.NextDouble() * 2 - 1) * max; }
    }
}
