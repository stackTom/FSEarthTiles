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

    class Edge<T> : System.Collections.Generic.List<T> where T : FSEarthTilesDLL.Point
    {
        public T firstPoint;
        public T secondPoint;

        // these two ensure Edge(A, B) == Edge(B, A)
        private Point _firstPoint;
        private Point _secondPoint;
        public int length;

        public Edge(T firstPoint, T secondPoint, int length)
        {
            this.firstPoint = firstPoint;
            this.secondPoint = secondPoint;
            this.length = length;

            if (firstPoint.Y == secondPoint.Y)
            {
                _firstPoint = firstPoint.X < secondPoint.X ? firstPoint : secondPoint;
            }
            else
            {
                _firstPoint = firstPoint.Y < secondPoint.Y ? firstPoint : secondPoint;
            }
            _secondPoint = _firstPoint == firstPoint ? secondPoint : firstPoint;
        }

        public override bool Equals(object obj)
        {
            return obj is Edge<T> edge &&
                   EqualityComparer<Point>.Default.Equals(firstPoint, edge.firstPoint) &&
                   EqualityComparer<Point>.Default.Equals(secondPoint, edge.secondPoint);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            return _firstPoint.ToString() + secondPoint.ToString();
        }
    }

    class PolygonPartsBuilder<T> : System.Collections.Generic.List<T> where T : FSEarthTilesDLL.Point
    {
        public HashSet<Way<T>> parts = new HashSet<Way<T>>();
        // this is built lazily when needed
        public HashSet<Edge<T>> edges = null;

        private HashSet<Edge<T>> buildEdgesForWay(Way<T> way, HashSet<T> excludedPoints)
        {
            int startIdx = 0;

            bool inSharedEdge = false;
            T firstPoint = null;
            T secondPoint = null;
            int length = 0;
            HashSet<Edge<T>> toReturn = new HashSet<Edge<T>>();
            while (startIdx < way.Count)
            {
                // move along this edge
                if (excludedPoints.Contains(way[startIdx]))
                {
                    inSharedEdge = !inSharedEdge;
                    if (inSharedEdge)
                    {
                        firstPoint = way[startIdx];
                    }
                    else
                    {
                        secondPoint = way[startIdx];
                        Edge<T> e = new Edge<T>(firstPoint, secondPoint, length);
                        if (!toReturn.Contains(e))
                        {
                            toReturn.Add(e);
                        }
                        firstPoint = null;
                        secondPoint = null;
                        length = 0;
                        startIdx--; // make sure we get this point as the first point of the next possible edge
                    }
                }
                startIdx++;
                if (inSharedEdge)
                {
                    length++;
                }
            }

            return toReturn;
        }

        public void buildEdges(Way<T> way, Way<T> otherWay, HashSet<T> excludedPoints)
        {
            this.edges = new HashSet<Edge<T>>();
            HashSet<Edge<T>> wayEdges = this.buildEdgesForWay(way, excludedPoints);
            HashSet<Edge<T>> otherWayEdges = this.buildEdgesForWay(otherWay, excludedPoints);

            foreach (Edge<T> e in wayEdges)
            {
                if (otherWayEdges.Contains(e))
                {
                    this.edges.Add(e);
                }
            }
        }


        // the below code is thanks to Rod Stephens (http://csharphelper.com/blog/2014/07/determine-whether-a-point-is-inside-a-polygon-in-c/)
        // and (http://csharphelper.com/blog/2014/07/determine-whether-a-polygon-is-convex-in-c/)
        // I've modified it slightly for this use case, but the fundamental idea is the same
        // ------------------------------------------------------------------------------------------------------
        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        private static double CrossProductLength(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the Z coordinate of the cross product.
            return (BAx * BCy - BAy * BCx);
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        private static double DotProduct(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the vectors' coordinates.
            double BAx = Ax - Bx;
            double BAy = Ay - By;
            double BCx = Cx - Bx;
            double BCy = Cy - By;

            // Calculate the dot product.
            return (BAx * BCx + BAy * BCy);
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        private static double GetAngle(double Ax, double Ay,
            double Bx, double By, double Cx, double Cy)
        {
            // Get the dot product.
            double dot_product = DotProduct(Ax, Ay, Bx, By, Cx, Cy);

            // Get the cross product.
            double cross_product = CrossProductLength(Ax, Ay, Bx, By, Cx, Cy);

            // Calculate the angle.
            return Math.Atan2(cross_product, dot_product);
        }

        // Return True if the point is in the polygon.
        public static bool PointInPolygon(Way<T> Points, double X, double Y)
        {
            // Get the angle between the point and the
            // first and last vertices.
            int max_point = Points.Count - 1;
            double total_angle = GetAngle(
                Points[max_point].X, Points[max_point].Y,
                X, Y,
                Points[0].X, Points[0].Y);

            // Add the angles from the point
            // to each other pair of vertices.
            for (int i = 0; i < max_point; i++)
            {
                total_angle += GetAngle(
                    Points[i].X, Points[i].Y,
                    X, Y,
                    Points[i + 1].X, Points[i + 1].Y);
            }

            // The total angle should be 2 * PI or -2 * PI if
            // the point is in the polygon and close to zero
            // if the point is outside the polygon.
            // The following statement was changed. See the comments.
            //return (Math.Abs(total_angle) > 0.000001);
            return (Math.Abs(total_angle) > 1);
        }
        // ------------------------------------------------------------------------------------------------------

        private bool pointOnLine(Point toCheck, Point lineP1, Point lineP2)
        {
            Point minP = lineP1.X < lineP2.X ? lineP1 : lineP2;
            Point maxP = minP == lineP1 ? lineP2 : lineP1;
            Decimal dy1 = (Decimal)(maxP.Y - minP.Y);
            Decimal dx1 = (Decimal)(maxP.X - minP.X);
            // here, toCheck is to the right X wise from minP
            Decimal dy2 = (Decimal)(toCheck.Y - minP.Y);
            Decimal dx2 = (Decimal)(toCheck.X - minP.X);

            if (dx1 == Decimal.Zero || dx2 == Decimal.Zero)
            {
                if (dx1 == dx2)
                {
                    minP = lineP1.Y < lineP2.Y ? lineP1 : lineP2;
                    maxP = minP == lineP1 ? lineP2 : lineP1;

                    return toCheck.Y >= minP.Y && toCheck.Y <= maxP.Y;
                }

                return false;
            }

            Decimal lineSlope = dy1 / dx1;
            Decimal toCheckSlope = dy2 / dx2;

            Decimal EPSILON = Decimal.Parse("0.1");
            if (toCheck.X < minP.X || Math.Abs(lineSlope - toCheckSlope) > EPSILON || toCheck.X > maxP.X)
            {
                return false;
            }

            return true;
        }


        private bool pointOnSharedEdge(Way<T> wayToTraverse, int idx, T nextPoint, Way<T> otherWay, HashSet<T> excludedPoints)
        {
            T point = wayToTraverse[idx];

            if (excludedPoints.Contains(point))
            {
                return true;
            }

            // build edges lazily for performance reasons
            if (this.edges == null)
            {
                this.buildEdges(wayToTraverse, otherWay, excludedPoints);
            }

            foreach (Edge<T> e in this.edges)
            {
                T p1 = e.firstPoint;
                T p2 = e.secondPoint;

                if (pointOnLine(point, p1, p2))
                {
                    return true;
                }
                bool lines_intersect, segments_intersect;
                Point poi, close1, close2;
                AreaKMLFromOSMDataCreator.FindIntersection(p1, p2, point, nextPoint,
                    out lines_intersect, out segments_intersect,
                    out poi, out close1, out close2);

                // make sure segments intersect before checking if pointinpolygon
                // otherwise, we can have cases like in Torry Island where it has a body of water
                // fully inside it. so all points in this body of water are in the Torry Island polygon...
                if (segments_intersect && PointInPolygon(otherWay, point.X, point.Y))
                {
                    return true;
                }
            }

            return false;
        }

        public void appendParts(Way<T> wayToTraverse, Way<T> otherWay, List<T> excludedPointsList, HashSet<T> excludedPoints)
        {
            int startIdx = 0;

            Way<T> temp = new Way<T>();
            bool inSharedEdge = false;
            while (startIdx < wayToTraverse.Count)
            {
                // move along this edge
                while (startIdx < wayToTraverse.Count)
                {
                    if (excludedPoints.Contains(wayToTraverse[startIdx]))
                    {
                        inSharedEdge = !inSharedEdge;
                        do
                        {
                            startIdx++;
                        } while (startIdx < wayToTraverse.Count && excludedPoints.Contains(wayToTraverse[startIdx]));
                    }
                    else
                    {
                        T nextPoint = startIdx == wayToTraverse.Count - 1 ? wayToTraverse[0] : wayToTraverse[startIdx];
                        if (pointOnSharedEdge(wayToTraverse, startIdx, nextPoint, otherWay, excludedPoints))
                        {
                            // handle case when we begin iterating in a shared edge, but not in excludedPoints (one of the screwed up points)
                            if (startIdx == 0)
                            {
                                startIdx++;
                                inSharedEdge = true;
                            }
                            else if (inSharedEdge)
                            {
                                // here, we don't begin in a shared edge, but we are in an shared edge, so walk along it
                                startIdx++;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (!inSharedEdge)
                    {
                        break;
                    }
                }
                inSharedEdge = false;

                temp = new Way<T>();

                if (startIdx > 0)
                {
                    T p = wayToTraverse[startIdx - 1];
                    temp.Add(p);
                }

                for (int i = startIdx; i < wayToTraverse.Count; i++)
                {
                    T p = wayToTraverse[i];
                    temp.Add(p);
                    startIdx++;
                    // reached another shared edge, or the end
                    if (excludedPoints.Contains(wayToTraverse[i]) || i == wayToTraverse.Count - 1)
                    {
                        temp.wayID = i.ToString();
                        parts.Add(temp);
                        if (i != wayToTraverse.Count - 1)
                        {
                            startIdx--;
                        }
                        break;
                    }
                }
            }
        }
    }
}
