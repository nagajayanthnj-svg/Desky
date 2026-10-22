using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    public enum Personality { Alden = 0, Pip = 1 }

    public enum Outcome
    {
        Done,
        StartCapture,         // UI opens dictation (Argument may hold inline text)
        NeedsConfirmation,    // agent is waiting for Confirm / Deny
        NeedsClarification,   // missing target or partial command
        Refused,              // rule violation, e.g. item is locked
        NothingFound,
        NotUnderstood         // not in the grammar; LLM may add small talk, never actions
    }

    public enum ResponseKey
    {
        Organised, NothingToOrganise, Grouped,
        Found, NothingFound, WhatToFind,
        Status, StatusEmpty,
        ImportantList, NothingImportant,
        FocusOn, FocusNeedsImportant, FocusOff, NotInFocus,
        Locked, Unlocked, ItemIsLocked,
        MarkedImportant, ClearedImportant, Tagged,
        ConfirmDelete, Deleted, DeleteCancelled,
        Undone, NothingToUndo,
        Switched,
        Pinned,
        WhichItem, ClarifyPartial, NothingPending, Cancelled,
        Listening, Help, NotUnderstood
    }

    public sealed class AgentResult
    {
        public Outcome Outcome;
        public ResponseKey Key;
        public string Argument;
        public List<string> ItemIds = new List<string>();
        public bool DeskChanged;
        public StatusReport Status;

        public override string ToString() { return Outcome + "/" + Key + (Argument != null ? "(" + Argument + ")" : ""); }
    }

    /// <summary>
    /// The productive agent. Takes a parsed command plus the item the user is pointing/gazing at,
    /// applies PRD 5.2 rules, and returns what happened. Never speaks or renders; the app does that.
    /// Every desk-changing command is wrapped in the one-level undo (D-005).
    /// </summary>
    public sealed class DeskAgent
    {
        readonly Desk _desk;
        readonly UndoBuffer _undo = new UndoBuffer();
        Command _pending;
        string _pendingTarget;

        public Personality Personality { get; private set; }
        public Desk Desk { get { return _desk; } }
        public bool AwaitingConfirmation { get { return _pending != null; } }
        public bool CanUndo { get { return _undo.CanUndo; } }

        public DeskAgent(Desk desk, Personality personality = Personality.Alden)
        {
            _desk = desk;
            Personality = personality;
        }

        public AgentResult Handle(Command cmd, string targetItemId = null)
        {
            if (_pending != null)
            {
                var target = _pendingTarget;
                _pending = null; _pendingTarget = null;
                if (cmd.Kind == CommandKind.Confirm) return ExecuteDelete(target);
                if (cmd.Kind == CommandKind.Deny || cmd.Kind == CommandKind.Cancel) return R(Outcome.Done, ResponseKey.DeleteCancelled);
                // anything else: drop the pending delete and handle the new command
            }

            if (cmd.IsPartial) return R(Outcome.NeedsClarification, ResponseKey.ClarifyPartial, cmd.Kind.ToString());

            switch (cmd.Kind)
            {
                case CommandKind.NewNote: return new AgentResult { Outcome = Outcome.StartCapture, Key = ResponseKey.Listening, Argument = cmd.Argument };
                case CommandKind.Organise: return Organise(cmd.Argument);
                case CommandKind.Find: return Find(cmd.Argument);
                case CommandKind.Status: return Status();
                case CommandKind.Important: return Important();
                case CommandKind.FocusOn: return FocusOn();
                case CommandKind.FocusOff: return FocusOff();
                case CommandKind.Lock: return OnTarget(targetItemId, i => { i.Locked = true; }, ResponseKey.Locked, false);
                case CommandKind.Unlock: return OnTarget(targetItemId, i => { i.Locked = false; }, ResponseKey.Unlocked, false);
                case CommandKind.MarkImportant: return OnTarget(targetItemId, i => { i.Priority = Priority.High; }, ResponseKey.MarkedImportant, false);
                case CommandKind.ClearImportant: return OnTarget(targetItemId, i => { i.Priority = Priority.Normal; }, ResponseKey.ClearedImportant, false);
                case CommandKind.Tag: return Tag(targetItemId, cmd.Argument);
                case CommandKind.Delete: return RequestDelete(targetItemId);
                case CommandKind.Undo: return Undo();
                case CommandKind.SwitchPersonality:
                    Personality = cmd.Argument == "pip" ? Personality.Pip : Personality.Alden;
                    return R(Outcome.Done, ResponseKey.Switched, Personality.ToString());
                case CommandKind.Confirm:
                case CommandKind.Deny:
                    return R(Outcome.Done, ResponseKey.NothingPending);
                case CommandKind.Cancel: return R(Outcome.Done, ResponseKey.Cancelled);
                case CommandKind.Help: return R(Outcome.Done, ResponseKey.Help);
                default: return R(Outcome.NotUnderstood, ResponseKey.NotUnderstood, cmd.Raw);
            }
        }

        /// <summary>Called when a capture session completes. Creating a note is undoable.</summary>
        public AgentResult PinNote(string text, Pose pose, ItemType type = ItemType.Sticky, string title = null)
        {
            DeskItem item = null;
            var res = Mutate("pin note", () => { item = _desk.Add(type, text, pose, title); });
            res.Key = ResponseKey.Pinned;
            res.ItemIds.Add(item.Id);
            return res;
        }

        /// <summary>Hand edits (grab/move/scale) go through here so timestamps stay right. Not undoable.</summary>
        public void ApplyManualPose(string itemId, Pose pose)
        {
            var item = _desk.Get(itemId);
            if (item == null) return;
            item.LocalPose = pose;
            _desk.Touch(item);
        }

        // ---------- commands ----------

        AgentResult Organise(string groupTag)
        {
            if (groupTag != null && DeskQueries.Find(_desk, groupTag).Count == 0)
                return R(Outcome.NothingFound, ResponseKey.NothingFound, groupTag);

            var plan = OrganiseLayout.Plan(_desk.Items, _desk.Bounds);
            var moved = new List<string>();
            var res = Mutate("organise", () =>
            {
                foreach (var kv in plan)
                {
                    var item = _desk.Get(kv.Key);
                    if (item.LocalPose.ApproxEquals(kv.Value)) continue;
                    item.LocalPose = kv.Value;
                    _desk.Touch(item);
                    moved.Add(item.Id);
                }
            });
            if (!res.DeskChanged) return R(Outcome.Done, ResponseKey.NothingToOrganise);
            res.Key = groupTag != null ? ResponseKey.Grouped : ResponseKey.Organised;
            res.Argument = groupTag;
            res.ItemIds = moved;
            return res;
        }

        AgentResult Find(string query)
        {
            if (DeskQueries.Terms(query).Count == 0) return R(Outcome.NeedsClarification, ResponseKey.WhatToFind);
            var hits = DeskQueries.Find(_desk, query);
            if (hits.Count == 0) return R(Outcome.NothingFound, ResponseKey.NothingFound, query);
            var res = R(Outcome.Done, ResponseKey.Found, hits[0].Title ?? hits[0].Content);
            res.ItemIds = hits.Select(i => i.Id).ToList();
            return res;
        }

        AgentResult Status()
        {
            var s = DeskQueries.Status(_desk);
            var res = R(Outcome.Done, s.Total == 0 ? ResponseKey.StatusEmpty : ResponseKey.Status);
            res.Status = s;
            return res;
        }

        AgentResult Important()
        {
            var items = DeskQueries.Important(_desk);
            if (items.Count == 0) return R(Outcome.Done, ResponseKey.NothingImportant);
            var res = R(Outcome.Done, ResponseKey.ImportantList);
            res.ItemIds = items.Select(i => i.Id).ToList();
            return res;
        }

        AgentResult FocusOn()
        {
            if (!_desk.Items.Any(i => i.Priority == Priority.High))
                return R(Outcome.NeedsClarification, ResponseKey.FocusNeedsImportant);

            var res = Mutate("focus mode", () =>
            {
                foreach (var i in _desk.Items)
                {
                    var hide = i.Priority != Priority.High && !i.Locked;   // locked items stay visible (D-008)
                    if (i.HiddenByFocus == hide) continue;
                    i.HiddenByFocus = hide;
                    _desk.Touch(i);
                }
            });
            res.Key = ResponseKey.FocusOn;
            res.ItemIds = _desk.Items.Where(i => !i.HiddenByFocus).Select(i => i.Id).ToList();
            return res;
        }

        AgentResult FocusOff()
        {
            if (!_desk.FocusActive) return R(Outcome.Done, ResponseKey.NotInFocus);
            var res = Mutate("exit focus", () =>
            {
                foreach (var i in _desk.Items.Where(i => i.HiddenByFocus)) { i.HiddenByFocus = false; _desk.Touch(i); }
            });
            res.Key = ResponseKey.FocusOff;
            return res;
        }

        AgentResult Tag(string targetId, string tag)
        {
            var normalised = tag.Trim().ToLowerInvariant();
            var res = OnTarget(targetId, i =>
            {
                if (i.HasTag(normalised)) return;
                i.Tags.Insert(0, normalised);   // most recent tag becomes the grouping tag
            }, ResponseKey.Tagged, false);
            if (res.Outcome == Outcome.Done) res.Argument = normalised;
            return res;
        }

        AgentResult RequestDelete(string targetId)
        {
            var item = _desk.Get(targetId);
            if (targetId == null) return R(Outcome.NeedsClarification, ResponseKey.WhichItem);
            if (item == null) return R(Outcome.NothingFound, ResponseKey.NothingFound);
            if (item.Locked) return Refused(item);

            _pending = new Command { Kind = CommandKind.Delete };
            _pendingTarget = targetId;
            var res = R(Outcome.NeedsConfirmation, ResponseKey.ConfirmDelete, item.Title ?? item.Content);
            res.ItemIds.Add(item.Id);
            return res;
        }

        AgentResult ExecuteDelete(string targetId)
        {
            var item = _desk.Get(targetId);
            if (item == null) return R(Outcome.NothingFound, ResponseKey.NothingFound);
            if (item.Locked) return Refused(item);   // locked between request and confirm
            var res = Mutate("delete", () => _desk.Remove(targetId));
            res.Key = ResponseKey.Deleted;
            res.ItemIds.Add(targetId);
            return res;
        }

        AgentResult Undo()
        {
            var label = _undo.Label;
            if (!_undo.Undo(_desk)) return R(Outcome.Done, ResponseKey.NothingToUndo);
            var res = R(Outcome.Done, ResponseKey.Undone, label);
            res.DeskChanged = true;
            return res;
        }

        // ---------- helpers ----------

        AgentResult OnTarget(string targetId, System.Action<DeskItem> change, ResponseKey key, bool respectLock)
        {
            if (targetId == null) return R(Outcome.NeedsClarification, ResponseKey.WhichItem);
            var item = _desk.Get(targetId);
            if (item == null) return R(Outcome.NothingFound, ResponseKey.NothingFound);
            if (respectLock && item.Locked) return Refused(item);

            var res = Mutate(key.ToString(), () => { change(item); _desk.Touch(item); });
            res.Key = key;
            res.ItemIds.Add(item.Id);
            return res;
        }

        AgentResult Mutate(string label, System.Action action)
        {
            _undo.Begin(_desk, label);
            action();
            var changed = _undo.Commit(_desk);
            return new AgentResult { Outcome = Outcome.Done, DeskChanged = changed };
        }

        static AgentResult Refused(DeskItem item)
        {
            var res = R(Outcome.Refused, ResponseKey.ItemIsLocked, item.Title ?? item.Content);
            res.ItemIds.Add(item.Id);
            return res;
        }

        static AgentResult R(Outcome o, ResponseKey k, string arg = null)
        {
            return new AgentResult { Outcome = o, Key = k, Argument = arg };
        }
    }
}
