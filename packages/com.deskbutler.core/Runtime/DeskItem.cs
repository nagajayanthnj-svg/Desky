using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    public enum ItemType { Sticky = 0, Panel = 1, Image = 2 }

    public enum Priority { Normal = 0, High = 1 }

    /// <summary>
    /// One thing pinned on the desk. Public fields (not properties) so Unity's JsonUtility can serialise it.
    /// Pose is relative to the single desk anchor (DECISIONS D-002), never to a per-item anchor.
    /// </summary>
    [Serializable]
    public sealed class DeskItem
    {
        public string Id;
        public ItemType Type;
        public string Title;            // panels only; null for stickies
        public string Content;          // note text, or local file path for images
        public List<string> Tags = new List<string>();
        public Priority Priority;
        public bool Locked;             // agent may not move, hide-for-focus, or delete
        public bool HiddenByFocus;      // set only by Focus Mode
        public Pose LocalPose = Pose.Default;
        public long CreatedAtMs;
        public long UpdatedAtMs;

        public string PrimaryTag { get { return Tags.Count > 0 ? Tags[0] : null; } }

        public bool HasTag(string tag)
        {
            return Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        }

        public DeskItem Clone()
        {
            var c = (DeskItem)MemberwiseClone();
            c.Tags = Tags == null ? new List<string>() : new List<string>(Tags);
            return c;
        }

        /// <summary>Overwrite this instance's state, keeping object identity (views may hold references).</summary>
        public void CopyStateFrom(DeskItem o)
        {
            Type = o.Type; Title = o.Title; Content = o.Content;
            Tags = new List<string>(o.Tags);
            Priority = o.Priority; Locked = o.Locked; HiddenByFocus = o.HiddenByFocus;
            LocalPose = o.LocalPose; CreatedAtMs = o.CreatedAtMs; UpdatedAtMs = o.UpdatedAtMs;
        }

        /// <summary>Equality of user-visible state; timestamps are ignored.</summary>
        public bool SameStateAs(DeskItem o)
        {
            return o != null
                && Id == o.Id && Type == o.Type && Title == o.Title && Content == o.Content
                && Priority == o.Priority && Locked == o.Locked && HiddenByFocus == o.HiddenByFocus
                && LocalPose.ApproxEquals(o.LocalPose)
                && Tags.SequenceEqual(o.Tags);
        }

        public override string ToString() { return string.Format("{0}:{1}:'{2}'", Type, Id, Title ?? Content); }
    }
}
