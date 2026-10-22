using DeskButler.Core;
using NUnit.Framework;

namespace DeskButler.Core.Tests
{
    [TestFixture]
    public class CommandParserTests
    {
        [TestCase("New note", CommandKind.NewNote, null)]
        [TestCase("Pin this idea", CommandKind.NewNote, null)]
        [TestCase("Remember this", CommandKind.NewNote, null)]
        [TestCase("Pin this idea: call the vendor", CommandKind.NewNote, "call the vendor")]
        [TestCase("Clean up the desk", CommandKind.Organise, null)]
        [TestCase("Organise the desk", CommandKind.Organise, null)]
        [TestCase("organize my desk", CommandKind.Organise, null)]
        [TestCase("Group research notes", CommandKind.Organise, "research")]
        [TestCase("Where did I put the pricing notes?", CommandKind.Find, "pricing notes")]
        [TestCase("Where is the launch plan", CommandKind.Find, "launch plan")]
        [TestCase("Find budget", CommandKind.Find, "budget")]
        [TestCase("What's important today?", CommandKind.Important, null)]
        [TestCase("What’s important", CommandKind.Important, null)]
        [TestCase("Give me a status", CommandKind.Status, null)]
        [TestCase("Give me a quick overview", CommandKind.Status, null)]
        [TestCase("Overview", CommandKind.Status, null)]
        [TestCase("Focus mode", CommandKind.FocusOn, null)]
        [TestCase("Clear distractions", CommandKind.FocusOn, null)]
        [TestCase("Exit focus", CommandKind.FocusOff, null)]
        [TestCase("Lock this", CommandKind.Lock, null)]
        [TestCase("Unlock this", CommandKind.Unlock, null)]
        [TestCase("Mark this important", CommandKind.MarkImportant, null)]
        [TestCase("This is important", CommandKind.MarkImportant, null)]
        [TestCase("Mark this as not important", CommandKind.ClearImportant, null)]
        [TestCase("Tag this as research", CommandKind.Tag, "research")]
        [TestCase("Delete this", CommandKind.Delete, null)]
        [TestCase("Undo", CommandKind.Undo, null)]
        [TestCase("Switch to Alden", CommandKind.SwitchPersonality, "alden")]
        [TestCase("Switch to Pip", CommandKind.SwitchPersonality, "pip")]
        [TestCase("Yes", CommandKind.Confirm, null)]
        [TestCase("ok", CommandKind.Confirm, null)]
        [TestCase("No", CommandKind.Deny, null)]
        [TestCase("Never mind", CommandKind.Cancel, null)]
        [TestCase("No, I said buy oat milk", CommandKind.Correction, "buy oat milk")]
        [TestCase("Change that to call Ravi at 5", CommandKind.Correction, "call Ravi at 5")]
        [TestCase("Help", CommandKind.Help, null)]
        public void Recognises_core_vocabulary(string utterance, CommandKind kind, string arg)
        {
            var cmd = CommandParser.Parse(utterance);
            Assert.AreEqual(kind, cmd.Kind, cmd.ToString());
            Assert.AreEqual(arg, cmd.Argument);
            Assert.IsFalse(cmd.IsPartial);
        }

        [TestCase("Alden, clean up the desk", CommandKind.Organise)]
        [TestCase("hey pip could you clean up the desk please", CommandKind.Organise)]
        [TestCase("Okay, focus mode", CommandKind.FocusOn)]
        public void Ignores_wake_words_and_politeness(string utterance, CommandKind kind)
        {
            Assert.AreEqual(kind, CommandParser.Parse(utterance).Kind);
        }

        [TestCase("Where is", CommandKind.Find)]
        [TestCase("Find", CommandKind.Find)]
        [TestCase("Switch to", CommandKind.SwitchPersonality)]
        [TestCase("Tag this as", CommandKind.Tag)]
        [TestCase("Group", CommandKind.Organise)]
        public void Flags_partial_commands_for_clarification(string utterance, CommandKind kind)
        {
            var cmd = CommandParser.Parse(utterance);
            Assert.AreEqual(kind, cmd.Kind);
            Assert.IsTrue(cmd.IsPartial, cmd.ToString());
        }

        [TestCase("What a lovely day")]
        [TestCase("Tell me a joke")]
        [TestCase("")]
        [TestCase(null)]
        public void Anything_else_is_conversational_filler(string utterance)
        {
            Assert.AreEqual(CommandKind.None, CommandParser.Parse(utterance).Kind);
        }

        [Test]
        public void Dictated_text_keeps_case_and_punctuation()
        {
            Assert.AreEqual("Call Ravi at 5:30, re: GST invoice.",
                CommandParser.Parse("No, I said Call Ravi at 5:30, re: GST invoice.").Argument);
            Assert.AreEqual("Q3 pricing: ₹499/month",
                CommandParser.Parse("Alden, pin this idea: Q3 pricing: ₹499/month").Argument);
        }

        [Test]
        public void Change_to_personality_is_not_a_correction()
        {
            Assert.AreEqual(CommandKind.SwitchPersonality, CommandParser.Parse("change to pip").Kind);
        }
    }
}
