using System;
using System.Collections.Generic;
using System.Linq;

namespace DeskButler.Core
{
    public sealed class StatusReport
    {
        public int Total;
        public int Important;
        public int Locked;
        public int Hidden;
        public bool FocusActive;
        public List<string> TopTags = new List<string>();
    }

    /// <summary>Read-only questions about the desk. Answers come only from what is on it (PRD 10).</summary>
    public static class DeskQueries
    {
        static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "the", "my", "a", "an", "i", "did", "put", "is", "are", "of", "on", "about", "for", "to",
            "note", "notes", "item", "items", "panel", "panels", "sticky", "thing", "stuff", "that", "this"
        };

        public static List<string> Terms(string query)
        {
            return CommandParser.Normalise(query).Split(' ')
                .Where(t => t.Length > 1 && !StopWords.Contains(t))
                .Select(Stem)
                .Distinct()
                .ToList();
        }

        static string Stem(string t)
        {
            if (t.EndsWith("ies") && t.Length > 4) return t.Substring(0, t.Length - 3) + "y";
            if (t.EndsWith("s") && !t.EndsWith("ss") && t.Length > 3) return t.Substring(0, t.Length - 1);
            return t;
        }

        /// <summary>Items matching any term, best first. Empty list = nothing found; never a guess.</summary>
        public static List<DeskItem> Find(Desk desk, string query)
        {
            var terms = Terms(query);
            if (terms.Count == 0) return new List<DeskItem>();

            return desk.Items
                .Select(i => new { Item = i, Score = Score(i, terms) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Item.UpdatedAtMs)
                .Select(x => x.Item)
                .ToList();
        }

        static int Score(DeskItem item, List<string> terms)
        {
            var hay = ((item.Title ?? "") + " " + (item.Content ?? "")).ToLowerInvariant();
            var score = 0;
            foreach (var t in terms)
            {
                if (item.Tags.Any(tag => tag.ToLowerInvariant().StartsWith(t))) score += 3;
                if (item.Title != null && item.Title.ToLowerInvariant().Contains(t)) score += 2;
                if (hay.Contains(t)) score += 1;
            }
            return score;
        }

        public static List<DeskItem> Important(Desk desk)
        {
            return desk.Items.Where(i => i.Priority == Priority.High).OrderBy(i => i.CreatedAtMs).ToList();
        }

        public static StatusReport Status(Desk desk)
        {
            return new StatusReport
            {
                Total = desk.Items.Count,
                Important = desk.Items.Count(i => i.Priority == Priority.High),
                Locked = desk.Items.Count(i => i.Locked),
                Hidden = desk.Items.Count(i => i.HiddenByFocus),
                FocusActive = desk.FocusActive,
                TopTags = desk.Items.SelectMany(i => i.Tags)
                    .GroupBy(t => t.ToLowerInvariant())
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
                    .Take(3).Select(g => g.Key).ToList()
            };
        }
    }
}
