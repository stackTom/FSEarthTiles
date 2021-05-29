using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSEarthTilesDLL
{
    public class Point
    {
        public double X;
        public double Y;
        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is Point point &&
                   X == point.X &&
                   Y == point.Y;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            return this.X.ToString() + "," + this.Y.ToString();
        }
    }
}

