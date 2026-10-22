using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeskButler.Core
{
    public enum CommandKind
    {
        None,               // not a command: conversational filler
        NewNote,
        Organise,
        Find,
        Status,
        Important,
        FocusOn,
        FocusOff,
        Lock,
        Unlock,
        MarkImportant,
        ClearImportant,
        Tag,
        Delete,
        Undo,
        SwitchPersonality,
        Confirm,
        Deny,
        Cancel,
        Correction,
        Help
    }

    public sealed class Command
    {
        public CommandKind Kind;
        public string Argument;     // query, tag, personality name, inline note text, correction text
        public bool IsPartial;      // recognised start of a command with a missing part -> clarify
        public string Raw;

        public override string ToString()
        {
            return Kind + (Argument != null ? "(" + Argument + ")" : "") + (IsPartial ? "[partial]" : "");
        }
    }

    /// <summary>
    /// Deterministic command grammar (PRD 5.2, DECISIONS D-004). Ordered rules, first match wins.
    /// Adding a phrase = add a test case in CommandParserTests first, then a rule here.
    /// </summary>
    public static class CommandParser
    {
        sealed class Rule
        {
            public Regex Pattern; public CommandKind Kind; public int ArgGroup; public bool Partial;
        }

        static readonly List<Rule> Rules = new List<Rule>();

        static void R(string pattern, CommandKind kind, int argGroup = 0, bool partial = false)
        {
            Rules.Add(new Rule
            {
                Pattern = new Regex("^(?:" + pattern + ")$", RegexOptions.Compiled | RegexOptions.CultureInvariant),
                Kind = kind, ArgGroup = argGroup, Partial = partial
            });
        }

        static CommandParser()
        {
            const string it = "(?:this|it|that)(?: note| one| panel)?";

            R("undo|undo that|take that back|revert that", CommandKind.Undo);
            R("cancel|never mind|nevermind|forget it|stop", CommandKind.Cancel);
            R("(?:no )?i said (.+)|change (?:that|it) to (.+)|correction (.+)", CommandKind.Correction);
            R("yes|yeah|yep|yup|confirm|do it|go ahead|sure|ok|okay|correct|that's right|looks good|save it|pin it", CommandKind.Confirm);
            R("no|nope|don't|do not|wrong", CommandKind.Deny);

            R("(?:new note|pin this(?: idea)?|remember this|take a note|note this|pin an idea)(?: (.+))?", CommandKind.NewNote, 1);

            R("(?:clean up|tidy up|tidy|organi[sz]e|sort out|sort)(?: the| my)?(?: desk)?", CommandKind.Organise);
            R("group(?: the| my)? (.+?)(?: notes| items| stuff)?", CommandKind.Organise, 1);

            R("(?:where is|where's|where are|where did i put|find|locate|show me)(?: the| my)?(?: (.+))?", CommandKind.Find, 1);

            R("(?:give me )?(?:a )?(?:quick )?(?:status|overview|summary|rundown|status update|desk status)", CommandKind.Status);
            R("what's important(?: today)?|what is important(?: today)?|what needs attention|priorities|what's urgent", CommandKind.Important);

            R("exit focus(?: mode)?|focus (?:mode )?off|end focus|stop focus|show everything|show all", CommandKind.FocusOff);
            R("focus|focus mode|focus mode on|enter focus(?: mode)?|clear distractions|hide distractions", CommandKind.FocusOn);

            R("unlock(?: " + it + ")?", CommandKind.Unlock);
            R("lock(?: " + it + ")?", CommandKind.Lock);

            R("(?:mark|set) " + it + " (?:as )?(?:not important|normal|unimportant)|unmark " + it, CommandKind.ClearImportant);
            R("(?:mark|flag|set) " + it + " (?:as )?(?:important|high priority|urgent)|" + it + " is important", CommandKind.MarkImportant);

            R("(?:tag|label) " + it + "(?: as| with)? (?!(?:as|with)$)(.+)", CommandKind.Tag, 1);
            R("(?:delete|remove|bin|trash|throw away|get rid of) " + it, CommandKind.Delete);

            R("(?:switch to|swap to|change to|bring back|i want) (alden|pip)", CommandKind.SwitchPersonality, 1);

            R("help|what can you do|commands|what can i say", CommandKind.Help);

            // Partial phrases: we know the intent but a required part is missing -> butler asks.
            R("switch(?: to)?|swap(?: to)?", CommandKind.SwitchPersonality, 0, true);
            R("tag(?: " + it + ")?(?: as)?|label(?: " + it + ")?", CommandKind.Tag, 0, true);
            R("delete|remove", CommandKind.Delete);   // target comes from pointing; agent asks if none
            R("where", CommandKind.Find, 0, true);
            R("group", CommandKind.Organise, 0, true);
        }

        static readonly Regex Leading = new Regex(
            @"^(?:(?:hey|hi|ok|okay|oi)\s+)?(?:(?:alden|pip)\s+)?(?:(?:please|could you|can you|would you|will you|kindly)\s+)*",
            RegexOptions.Compiled);

        public static string Normalise(string utterance)
        {
            var s = (utterance ?? "").ToLowerInvariant().Replace('’', '\'');
            s = Regex.Replace(s, @"[^a-z0-9' ]", " ");
            s = Regex.Replace(s, @"\s+", " ").Trim();
            // Strip wake/politeness only when something follows ("ok" alone must stay a Confirm).
            var stripped = Leading.Replace(s, "").Trim();
            if (stripped.Length > 0) s = stripped;
            if (s.EndsWith(" please")) s = s.Substring(0, s.Length - 7);
            if (s.EndsWith(" for me")) s = s.Substring(0, s.Length - 7);
            return s.Trim();
        }

        public static Command Parse(string utterance)
        {
            var text = Normalise(utterance);
            foreach (var r in Rules)
            {
                var m = r.Pattern.Match(text);
                if (!m.Success) continue;

                string arg = null;
                if (r.ArgGroup > 0)
                {
                    arg = m.Groups[r.ArgGroup].Value;
                }
                else if (r.Kind == CommandKind.Correction)
                {
                    for (var g = 1; g < m.Groups.Count; g++)
                        if (m.Groups[g].Success) { arg = m.Groups[g].Value; break; }
                }
                if (arg != null) arg = arg.Trim();
                if (arg == "") arg = null;

                // Dictated text (inline notes, corrections) keeps the user's case and punctuation.
                if (arg != null && (r.Kind == CommandKind.NewNote || r.Kind == CommandKind.Correction))
                    arg = FreeText(utterance) ?? arg;

                var partial = r.Partial || (r.Kind == CommandKind.Find && arg == null);
                return new Command { Kind = r.Kind, Argument = arg, IsPartial = partial, Raw = utterance };
            }
            return new Command { Kind = CommandKind.None, Raw = utterance };
        }

        static readonly Regex RawLeading = new Regex(
            @"^(?:(?:hey|hi|ok|okay|oi)[,\s]+)?(?:(?:alden|pip)[,\s]+)?(?:(?:please|could you|can you|would you|will you|kindly)\s+)*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex[] FreeTextRules =
        {
            new Regex(@"^(?:no[,.!]?\s+)?i said[,:]?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^change (?:that|it) to[,:]?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^correction[,:]?\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"^(?:new note|pin this(?: idea)?|remember this|take a note|note this|pin an idea)[,:.]?\s+(.+)$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        static string FreeText(string raw)
        {
            var s = Regex.Replace(raw.Replace('’', '\''), @"\s+", " ").Trim();
            s = RawLeading.Replace(s, "");
            foreach (var rule in FreeTextRules)
            {
                var m = rule.Match(s);
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            return null;
        }
    }
}
