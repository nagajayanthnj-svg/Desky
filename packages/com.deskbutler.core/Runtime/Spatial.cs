using System;

namespace DeskButler.Core
{
    // Engine-agnostic math types. Unity adapters convert to/from Vector3/Quaternion.
    // Coordinates are desk-local: origin = desk anchor, Y up from the surface, X right, Z away from the user.

    [Serializable]
    public struct Vec3
    {
        public float X, Y, Z;

        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static readonly Vec3 Zero = new Vec3(0, 0, 0);
        public static readonly Vec3 One = new Vec3(1, 1, 1);

        public float Magnitude { get { return (float)Math.Sqrt(X * X + Y * Y + Z * Z); } }

        public static Vec3 operator +(Vec3 a, Vec3 b) { return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static Vec3 operator -(Vec3 a, Vec3 b) { return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static Vec3 operator *(Vec3 a, float s) { return new Vec3(a.X * s, a.Y * s, a.Z * s); }

        public static float Distance(Vec3 a, Vec3 b) { return (a - b).Magnitude; }

        public bool ApproxEquals(Vec3 o, float eps = 1e-4f)
        {
            return Math.Abs(X - o.X) < eps && Math.Abs(Y - o.Y) < eps && Math.Abs(Z - o.Z) < eps;
        }

        public override string ToString() { return string.Format("({0:0.###}, {1:0.###}, {2:0.###})", X, Y, Z); }
    }

    [Serializable]
    public struct Quat
    {
        public float X, Y, Z, W;

        public Quat(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }

        public static readonly Quat Identity = new Quat(0, 0, 0, 1);

        public static Quat FromYawDegrees(float deg)
        {
            var half = deg * (float)Math.PI / 360f;
            return new Quat(0, (float)Math.Sin(half), 0, (float)Math.Cos(half));
        }

        public bool ApproxEquals(Quat o, float eps = 1e-4f)
        {
            // q and -q are the same rotation.
            var dot = X * o.X + Y * o.Y + Z * o.Z + W * o.W;
            return Math.Abs(Math.Abs(dot) - 1f) < eps;
        }
    }

    [Serializable]
    public struct Pose
    {
        public Vec3 Position;
        public Quat Rotation;
        public Vec3 Scale;

        public Pose(Vec3 position, Quat rotation, Vec3 scale) { Position = position; Rotation = rotation; Scale = scale; }

        public static Pose At(Vec3 position) { return new Pose(position, Quat.Identity, Vec3.One); }
        public static readonly Pose Default = new Pose(Vec3.Zero, Quat.Identity, Vec3.One);

        public bool ApproxEquals(Pose o)
        {
            return Position.ApproxEquals(o.Position) && Rotation.ApproxEquals(o.Rotation) && Scale.ApproxEquals(o.Scale);
        }
    }

    /// <summary>Usable rectangle of the desk surface in desk-local metres.</summary>
    [Serializable]
    public struct DeskBounds
    {
        public float MinX, MaxX, MinZ, MaxZ;

        public DeskBounds(float minX, float maxX, float minZ, float maxZ) { MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; }

        public bool Contains(Vec3 p) { return p.X >= MinX && p.X <= MaxX && p.Z >= MinZ && p.Z <= MaxZ; }
    }
}
