using System.Numerics;

namespace GeoAppCore.Geometry
{
    public struct GeoVector3 : IEquatable<GeoVector3>
    {
        private double x;
        private double y;
        private double z;

        private bool isNormalized;
        public double X
        {
            get => x;
            set
            {
                x = value;
                isNormalized = false;
            }
        }

        public double Y
        {
            get => y;
            set
            {
                y = value;
                isNormalized = false;
            }
        }

        public double Z
        {
            get => z;
            set
            {
                z = value;
                isNormalized = false;
            }
        }

        public static GeoVector3 Zero => new(0, 0, 0);

        public GeoVector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            isNormalized = false;
        }

        public bool Equals(GeoVector3 other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override bool Equals(object? obj)
        {
            return obj is GeoVector3 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(GeoVector3 left, GeoVector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeoVector3 left, GeoVector3 right)
        {
            return !left.Equals(right);
        }

        public static GeoVector3 operator +(GeoVector3 u, GeoVector3 v)
        {
            return new GeoVector3(
                u.X + v.X,
                u.Y + v.Y,
                u.Z + v.Z);
        }

        public static GeoVector3 operator -(GeoVector3 u, GeoVector3 v)
        {
            return new GeoVector3(
                u.X - v.X,
                u.Y - v.Y,
                u.Z - v.Z);
        }

        public static double Distance(GeoVector3 u, GeoVector3 v)
        {
            return Math.Sqrt(SquareDistance(u, v));
        }

        public static double SquareDistance(GeoVector3 u, GeoVector3 v)
        {
            return (u.X - v.X) * (u.X - v.X)
                 + (u.Y - v.Y) * (u.Y - v.Y)
                 + (u.Z - v.Z) * (u.Z - v.Z);
        }
    }
}
