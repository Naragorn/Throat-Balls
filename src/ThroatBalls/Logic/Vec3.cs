using System;

namespace ThroatBalls.Logic
{
    /// <summary>
    /// Minimal 3D vector so the decision logic stays free of UnityEngine and can be
    /// unit-tested outside the game. Y is up, as in Unity.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);
        public static readonly Vec3 Up = new Vec3(0f, 1f, 0f);

        private const float Epsilon = 1e-6f;

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Magnitude => (float)Math.Sqrt(Dot(this, this));

        /// <summary>The vector projected onto the horizontal plane.</summary>
        public Vec3 Flattened => new Vec3(X, 0f, Z);

        /// <summary>Unit vector in the same direction, or zero for a (near-)zero vector.</summary>
        public Vec3 Normalized
        {
            get
            {
                var length = Magnitude;
                return length < Epsilon ? Zero : this * (1f / length);
            }
        }

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static float Distance(Vec3 a, Vec3 b) => (a - b).Magnitude;

        /// <summary>Angle in degrees between two directions; NaN if either is (near-)zero.</summary>
        public static float AngleDegrees(Vec3 from, Vec3 to)
        {
            var a = from.Normalized;
            var b = to.Normalized;
            if (a.Equals(Zero) || b.Equals(Zero))
            {
                return float.NaN;
            }

            var cos = Math.Max(-1f, Math.Min(1f, Dot(a, b)));
            return (float)(Math.Acos(cos) * 180.0 / Math.PI);
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);

        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is Vec3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                return (hash * 397) ^ Z.GetHashCode();
            }
        }

        public override string ToString() => $"({X:0.000}, {Y:0.000}, {Z:0.000})";
    }
}
