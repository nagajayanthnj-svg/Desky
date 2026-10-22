using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    /// <summary>Serialised form of the whole desk. One file, one desk anchor.</summary>
    [Serializable]
    public sealed class DeskSnapshot
    {
        public const int CurrentSchema = 1;

        public int SchemaVersion = CurrentSchema;
        public string DeskAnchorId;      // persistent spatial anchor UUID for the desk
        public DeskBounds Bounds;
        public List<DeskItem> Items = new List<DeskItem>();
    }

    /// <summary>
    /// The aggregate: every item on the desk, in insertion order.
    /// Does not know about speech, personality or Unity.
    /// </summary>
    public sealed class Desk
    {
        public static readonly DeskBounds DefaultBounds = new DeskBounds(-0.5f, 0.5f, 0f, 0.6f);

        readonly List<DeskItem> _items = new List<DeskItem>();
        readonly IClock _clock;
        readonly IIdGenerator _ids;

        public string AnchorId;
        public DeskBounds Bounds = DefaultBounds;

        public Desk(IClock clock, IIdGenerator ids)
        {
            _clock = clock;
            _ids = ids;
        }

        public IReadOnlyList<DeskItem> Items { get { return _items; } }

        public bool FocusActive { get { return _items.Any(i => i.HiddenByFocus); } }

        public DeskItem Get(string id) { return id == null ? null : _items.FirstOrDefault(i => i.Id == id); }

        public DeskItem Add(ItemType type, string content, Pose pose, string title = null)
        {
            var now = _clock.NowMs;
            var item = new DeskItem
            {
                Id = _ids.NewId(),
                Type = type,
                Content = content,
                Title = title,
                LocalPose = pose,
                CreatedAtMs = now,
                UpdatedAtMs = now
            };
            _items.Add(item);
            return item;
        }

        public bool Remove(string id) { return _items.RemoveAll(i => i.Id == id) > 0; }

        /// <summary>Call after mutating an item so UpdatedAt is correct.</summary>
        public void Touch(DeskItem item) { item.UpdatedAtMs = _clock.NowMs; }

        /// <summary>Insert, or overwrite the existing item's state in place (same object). Used by undo.</summary>
        public void Put(DeskItem item)
        {
            var existing = Get(item.Id);
            if (existing != null) existing.CopyStateFrom(item); else _items.Add(item);
        }

        public DeskSnapshot ToSnapshot()
        {
            return new DeskSnapshot
            {
                DeskAnchorId = AnchorId,
                Bounds = Bounds,
                Items = _items.Select(i => i.Clone()).ToList()
            };
        }

        /// <summary>App start: saved desk if there is one, else an empty desk (onboarding will set the anchor).</summary>
        public static Desk LoadOrCreate(IDeskStore store, IClock clock, IIdGenerator ids)
        {
            var snap = store.Load();
            return snap == null ? new Desk(clock, ids) : FromSnapshot(snap, clock, ids);
        }

        public bool HasAnchor { get { return !string.IsNullOrEmpty(AnchorId); } }

        public static Desk FromSnapshot(DeskSnapshot snap, IClock clock, IIdGenerator ids)
        {
            if (snap.SchemaVersion > DeskSnapshot.CurrentSchema)
                throw new NotSupportedException("Desk saved by a newer app version: schema " + snap.SchemaVersion);

            var desk = new Desk(clock, ids) { AnchorId = snap.DeskAnchorId, Bounds = snap.Bounds };
            foreach (var i in snap.Items)
            {
                var c = i.Clone();
                if (c.Tags == null) c.Tags = new List<string>();   // JsonUtility may give null lists
                desk._items.Add(c);
            }
            return desk;
        }
    }
}
