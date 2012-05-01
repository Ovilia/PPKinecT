using System;

namespace PPKinecT
{
    /// <summary>
    /// 2 dimension vector of float
    /// </summary>
    public struct Vector2f
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2f(float x, float y) : this()
        {
            this.X = x;
            this.Y = y;
        }

        public float Modulus()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y));
        }

        public Vector2f Normalize()
        {
            float modulus = Modulus();
            if (modulus == 0)
            {
                return new Vector2f(0.0f, 0.0f);
            }
            else
            {
                return new Vector2f(X / modulus, Y / modulus);
            }
        }
    }

    /// <summary>
    /// 3 dimension vector of float
    /// </summary>
    public struct Vector3f
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3f(float x, float y, float z) : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public float Modulus()
        {
            return (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }

        public Vector3f Normalize()
        {
            float modulus = Modulus();
            if (modulus == 0)
            {
                return new Vector3f(0.0f, 0.0f, 0.0f);
            }
            else
            {
                return new Vector3f(X / modulus, Y / modulus, Z / modulus);
            }
        }
    }

    /// <summary>
    /// 3 dimension vector of double
    /// </summary>
    public struct Vector3d
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3d(double x, double y, double z) : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double Modulus()
        {
            return Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        }

        public Vector3d Normalize()
        {
            double modulus = Modulus();
            if (modulus == 0)
            {
                return new Vector3d(0.0f, 0.0f, 0.0f);
            }
            else
            {
                return new Vector3d(X / modulus, Y / modulus, Z / modulus);
            }
        }
    }
}
