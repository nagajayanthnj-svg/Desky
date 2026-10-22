using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    /// <summary>
    /// One-level undo for any desk-changing command (DECISIONS D-005).
    /// Begin() snapshots the desk, Commit() keeps only the items the command actually changed,
    /// so manual hand edits to *other* items after the command survive an undo.
    /// </summary>
    public sealed class UndoBuffer
    {
        Dictionary<string, DeskItem> _pendingBefore;
        Dictionary<string, DeskItem> _before;   // id -> state before (null = item did not exist)
        public string Label { get; private set; }

        public bool CanUndo { get { return _before != null && _before.Count > 0; } }

        public void Begin(Desk desk, string label)
        {
            _pendingBefore = desk.Items.ToDictionary(i => i.Id, i => i.Clone());
            _pendingLabel = label;
        }

        string _pendingLabel;

        /// <summary>Returns true if the command changed anything (and is therefore undoable).</summary>
        public bool Commit(Desk desk)
        {
            var changed = new Dictionary<string, DeskItem>();
            foreach (var kv in _pendingBefore)
            {
                var now = desk.Get(kv.Key);
                if (now == null || !now.SameStateAs(kv.Value)) changed[kv.Key] = kv.Value;   // changed or deleted
            }
            foreach (var item in desk.Items)
                if (!_pendingBefore.ContainsKey(item.Id)) changed[item.Id] = null;          // created

            _pendingBefore = null;
            if (changed.Count == 0) return false;   // no-op commands do not overwrite the previous undo
            _before = changed;
            Label = _pendingLabel;
            return true;
        }

        /// <summary>Restores the changed items. Returns false if there was nothing to undo.</summary>
        public bool Undo(Desk desk)
        {
            if (!CanUndo) return false;
            foreach (var kv in _before)
            {
                if (kv.Value == null) desk.Remove(kv.Key);
                else desk.Put(kv.Value.Clone());
            }
            Clear();
            return true;
        }

        public void Clear()
        {
            _before = null;
            _pendingBefore = null;
            Label = null;
        }
    }
}
