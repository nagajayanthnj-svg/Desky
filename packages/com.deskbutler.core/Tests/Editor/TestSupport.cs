using DeskButler.Core;

namespace DeskButler.Core.Tests
{
    public sealed class FakeClock : IClock
    {
        public long NowMs { get; set; }
        public FakeClock(long start = 1000) { NowMs = start; }
        public void Advance(long ms) { NowMs += ms; }
    }

    public sealed class SequentialIds : IIdGenerator
    {
        int _n;
        public string NewId() { _n++; return "item" + _n; }
    }

    public static class Make
    {
        public static Desk Desk(FakeClock clock = null)
        {
            return new Desk(clock ?? new FakeClock(), new SequentialIds());
        }

        public static DeskItem Note(Desk desk, string content, float x = 0.1f, float z = 0.1f,
            Priority priority = Priority.Normal, bool locked = false, params string[] tags)
        {
            var item = desk.Add(ItemType.Sticky, content, Pose.At(new Vec3(x, 0, z)));
            item.Priority = priority;
            item.Locked = locked;
            item.Tags.AddRange(tags);
            return item;
        }

        public static Command Cmd(string utterance) { return CommandParser.Parse(utterance); }
    }
}
