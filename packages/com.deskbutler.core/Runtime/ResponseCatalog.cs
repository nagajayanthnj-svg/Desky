using System.Collections.Generic;

namespace DeskButler.Core
{
    /// <summary>
    /// Deterministic spoken/subtitled lines. This is the baseline the butler always has;
    /// an LLM may only re-word it (D-004). Every ResponseKey must have a line for both personalities
    /// (enforced by ResponseCatalogTests). Placeholders: {arg} {count} {total} {important} {hidden} {tags}.
    /// </summary>
    public static class ResponseCatalog
    {
        static readonly Dictionary<ResponseKey, string> Alden = new Dictionary<ResponseKey, string>
        {
            { ResponseKey.Organised, "The desk is in order. {count} items set straight." },
            { ResponseKey.NothingToOrganise, "Everything is already precisely where it ought to be." },
            { ResponseKey.Grouped, "Your {arg} notes are gathered together." },
            { ResponseKey.Found, "Here. I've highlighted it for you." },
            { ResponseKey.NothingFound, "I'm afraid there's nothing like that on the desk." },
            { ResponseKey.WhatToFind, "Certainly. What shall I look for?" },
            { ResponseKey.Status, "{total} items on the desk, {important} marked important{hiddenClause}." },
            { ResponseKey.StatusEmpty, "The desk is entirely clear. Shall we pin something?" },
            { ResponseKey.ImportantList, "{count} items need your attention. I've raised them." },
            { ResponseKey.NothingImportant, "Nothing is marked important. Point at a note and say 'mark this important'." },
            { ResponseKey.FocusOn, "Focus mode. I've tucked away the rest." },
            { ResponseKey.FocusNeedsImportant, "Nothing is marked important yet, so I wouldn't know what to keep. Mark a note first." },
            { ResponseKey.FocusOff, "Everything is back on the desk." },
            { ResponseKey.NotInFocus, "We're not in focus mode at present." },
            { ResponseKey.Locked, "Locked. I shan't touch it." },
            { ResponseKey.Unlocked, "Unlocked." },
            { ResponseKey.ItemIsLocked, "That one is locked. Unlock it first if you truly wish." },
            { ResponseKey.MarkedImportant, "Marked as important." },
            { ResponseKey.ClearedImportant, "No longer marked important." },
            { ResponseKey.Tagged, "Filed under {arg}." },
            { ResponseKey.ConfirmDelete, "Remove this note? Say yes to confirm." },
            { ResponseKey.Deleted, "Removed. Say 'undo' should you change your mind." },
            { ResponseKey.DeleteCancelled, "Very good, it stays." },
            { ResponseKey.Undone, "Undone." },
            { ResponseKey.NothingToUndo, "There's nothing for me to undo." },
            { ResponseKey.Switched, "Alden, at your service." },
            { ResponseKey.Pinned, "Pinned." },
            { ResponseKey.WhichItem, "Which one? Point at it, if you would." },
            { ResponseKey.ClarifyPartial, "I didn't quite get all of that. Could you finish the request?" },
            { ResponseKey.NothingPending, "There's nothing awaiting confirmation." },
            { ResponseKey.Cancelled, "As you wish." },
            { ResponseKey.Listening, "I'm listening." },
            { ResponseKey.Help, "Try 'new note', 'clean up the desk', 'where is', 'focus mode', or 'undo'." },
            { ResponseKey.NotUnderstood, "I'm not sure I follow. Say 'help' for what I can do." },
        };

        static readonly Dictionary<ResponseKey, string> Pip = new Dictionary<ResponseKey, string>
        {
            { ResponseKey.Organised, "Ta-da! {count} things all neat and tidy!" },
            { ResponseKey.NothingToOrganise, "It's already super tidy!" },
            { ResponseKey.Grouped, "All your {arg} stuff is together now!" },
            { ResponseKey.Found, "Found it! Right here!" },
            { ResponseKey.NothingFound, "Hmm, I looked everywhere. It's not on the desk!" },
            { ResponseKey.WhatToFind, "Ooh, a hunt! What am I finding?" },
            { ResponseKey.Status, "{total} things on the desk, {important} important ones{hiddenClause}!" },
            { ResponseKey.StatusEmpty, "The desk is empty! Let's make a note!" },
            { ResponseKey.ImportantList, "{count} important things! I made them glow!" },
            { ResponseKey.NothingImportant, "Nothing's important yet! Point at one and say 'mark this important'!" },
            { ResponseKey.FocusOn, "Shhh, focus time! I hid the other stuff." },
            { ResponseKey.FocusNeedsImportant, "Which ones do I keep? Mark something important first!" },
            { ResponseKey.FocusOff, "Everything's back!" },
            { ResponseKey.NotInFocus, "We're not in focus mode, silly!" },
            { ResponseKey.Locked, "Locked! Paws off, promise!" },
            { ResponseKey.Unlocked, "Unlocked!" },
            { ResponseKey.ItemIsLocked, "That one's locked! Unlock it first." },
            { ResponseKey.MarkedImportant, "Ooh, important! Got it!" },
            { ResponseKey.ClearedImportant, "Okay, not important anymore." },
            { ResponseKey.Tagged, "Tagged it {arg}!" },
            { ResponseKey.ConfirmDelete, "Throw this one away? Say yes!" },
            { ResponseKey.Deleted, "Gone! Say 'undo' if that was oops." },
            { ResponseKey.DeleteCancelled, "Phew, keeping it!" },
            { ResponseKey.Undone, "Whoops, put it all back!" },
            { ResponseKey.NothingToUndo, "Nothing to undo!" },
            { ResponseKey.Switched, "Pip's here!" },
            { ResponseKey.Pinned, "Pinned it!" },
            { ResponseKey.WhichItem, "Which one? Point at it!" },
            { ResponseKey.ClarifyPartial, "Huh? Say the whole thing!" },
            { ResponseKey.NothingPending, "Yes to what? I wasn't asking anything!" },
            { ResponseKey.Cancelled, "Okay, never mind!" },
            { ResponseKey.Listening, "I'm all ears!" },
            { ResponseKey.Help, "Say 'new note', 'clean up the desk', 'where is', 'focus mode', or 'undo'!" },
            { ResponseKey.NotUnderstood, "I didn't get that! Say 'help' to see my tricks." },
        };

        public static bool Has(Personality p, ResponseKey key)
        {
            return (p == Personality.Pip ? Pip : Alden).ContainsKey(key);
        }

        public static string Render(Personality p, AgentResult result)
        {
            var template = (p == Personality.Pip ? Pip : Alden)[result.Key];
            var s = result.Status;
            var hiddenClause = s != null && s.Hidden > 0 ? ", " + s.Hidden + " hidden for focus" : "";
            return template
                .Replace("{arg}", result.Argument ?? "")
                .Replace("{count}", result.ItemIds.Count.ToString())
                .Replace("{total}", s != null ? s.Total.ToString() : "0")
                .Replace("{important}", s != null ? s.Important.ToString() : "0")
                .Replace("{hiddenClause}", hiddenClause)
                .Replace("{tags}", s != null ? string.Join(", ", s.TopTags) : "");
        }
    }
}
