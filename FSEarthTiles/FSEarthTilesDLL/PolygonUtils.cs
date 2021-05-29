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

    class Edge<T> : Way<T> where T : FSEarthTilesDLL.Point
    {
        const int MAX_DISSIMILAR_POINTS = 5;
        public Way<T> parentWay = null;

        public Edge(Way<T> parentWay)
        {
            this.parentWay = parentWay;
        }

        public Tuple<T, T> getPointsOrdered()
        {
            T firstPoint = this[0];
            T secondPoint = this[this.Count - 1];
            T _firstPoint = null;
            T _secondPoint = null;

            if (firstPoint.Y == secondPoint.Y)
            {
                _firstPoint = firstPoint.X < secondPoint.X ? firstPoint : secondPoint;
            }
            else
            {
                _firstPoint = firstPoint.Y < secondPoint.Y ? firstPoint : secondPoint;
            }
            _secondPoint = _firstPoint == firstPoint ? secondPoint : firstPoint;

            return new Tuple<T, T>(_firstPoint, _secondPoint);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge<T>))
            {
                return false;
            }
            Edge<T> objEdge = (Edge<T>)obj;
            Tuple<T, T> thisPoints = getPointsOrdered();
            Tuple<T, T> objPoints = objEdge.getPointsOrdered();

            return obj is Edge<T> edge && thisPoints.Item1 == objPoints.Item1 && thisPoints.Item2 == objPoints.Item2;
        }

        public bool edgesSimilarEnough(Edge<T> e)
        {
            if (Math.Abs(this.Count - e.Count) > MAX_DISSIMILAR_POINTS)
            {
                return false;
            }
            HashSet<T> ePoints = new HashSet<T>(e);

            int numDissimilar = 0;
            foreach (T p in this)
            {
                if (ePoints.Contains(p))
                {
                    numDissimilar++;
                }
            }

            return numDissimilar <= MAX_DISSIMILAR_POINTS;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {
            Tuple<T, T> points = getPointsOrdered();
            T firstPoint = points.Item1;
            T secondPoint = points.Item2;

            return firstPoint.ToString() + secondPoint.ToString();
        }
    }

    class PolygonPartsBuilder<T> where T : FSEarthTilesDLL.Point
    {
        public HashSet<Way<T>> parts = new HashSet<Way<T>>();
        // this is built lazily when needed
        public Dictionary<Edge<T>, Edge<T>> edges = null;

        private Dictionary<Edge<T>, List<Edge<T>>> buildEdgesForWay(Way<T> way, HashSet<T> excludedPoints)
        {
            int startIdx = 0;
            Console.WriteLine("this is a closed way " + way.isClosedWay() + " with count of " + way.Count);

            bool inSharedEdge = false;
            Dictionary<Edge<T>, List<Edge<T>>> toReturn = new Dictionary<Edge<T>, List<Edge<T>>>();
            Edge<T> curEdge = new Edge<T>(way);
            T firstEdgePoint = null;
            int lastIdx = way.isClosedWay() ? way.Count - 1 : way.Count;
            while (startIdx < lastIdx)
            {
                // move along this edge
                if (excludedPoints.Contains(way[startIdx]))
                {
                    curEdge.Add(way[startIdx]);
                    inSharedEdge = !inSharedEdge;
                    if (inSharedEdge)
                    {
                        Console.WriteLine("starting with " + way[startIdx] + " at idx " + startIdx);
                        if (firstEdgePoint == null)
                        {
                            firstEdgePoint = way[startIdx];
                        }
                    }
                    else
                    {
                        Console.WriteLine("ending with " + way[startIdx] + " at idx " + startIdx);
                        Console.WriteLine("wayID " + way.wayID + " " + curEdge.ToString());
                        if (!toReturn.ContainsKey(curEdge))
                        {
                            List<Edge<T>> potentiallySimilarEdges = new List<Edge<T>>();
                            potentiallySimilarEdges.Add(curEdge);
                            toReturn.Add(curEdge, potentiallySimilarEdges);
                        }
                        else
                        {
                            List<Edge<T>> potentiallySimilarEdges = toReturn[curEdge];
                            potentiallySimilarEdges.Add(curEdge);
                        }
                        curEdge = new Edge<T>(way);
                        startIdx--; // make sure we get this point as the first point of the next possible edge
                    }
                }
                else if (inSharedEdge)
                {
                    curEdge.Add(way[startIdx]);
                }
                startIdx++;
            }
            if (inSharedEdge)
            {
                Console.WriteLine("we are still in a shared edge at " + (startIdx - 1));
                // keep adding until we reach the firstEdgePoint we added
                int tika = -1;
                for (int i = 0; !way[i].Equals(firstEdgePoint); i++)
                {
                    //Console.WriteLine("so added " + way[i] + " at idx " + i);
                    curEdge.Add(way[i]);
                    tika = i;
                }
                tika++;
                curEdge.Add(firstEdgePoint);
                Console.WriteLine("and the final one - the firstEdgePoint " + firstEdgePoint + " which is found at idx " + tika);
                if (!toReturn.ContainsKey(curEdge))
                {
                    List<Edge<T>> potentiallySimilarEdges = new List<Edge<T>>();
                    potentiallySimilarEdges.Add(curEdge);
                    toReturn.Add(curEdge, potentiallySimilarEdges);
                }
                else
                {
                    List<Edge<T>> potentiallySimilarEdges = toReturn[curEdge];
                    potentiallySimilarEdges.Add(curEdge);
                }
                Console.WriteLine("wayID " + way.wayID + " " + curEdge.ToString());
            }

            return toReturn;
        }

        private double HaversineDistance(Point pos1, Point pos2)
        {
            const double R = 6371;
            const double TO_RADS = Math.PI / 180.0;
            double lat = (pos2.Y - pos1.Y) * TO_RADS;
            double lng = (pos2.X - pos1.X) * TO_RADS;
            double h1 = Math.Sin(lat / 2) * Math.Sin(lat / 2) +
                          Math.Cos(pos1.Y * TO_RADS) * Math.Cos(pos2.Y * TO_RADS) *
                          Math.Sin(lng / 2) * Math.Sin(lng / 2);
            double h2 = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h1)));

            return R * h2;
        }

        private Point getMidPoint(Edge<T> way)
        {
            if (way.Count <= 2)
            {
                return new Point((way[0].X + way[way.Count - 1].X) / 2, (way[0].Y + way[way.Count - 1].Y) / 2);
            }

            // we assume the distance between each consecutive point is somewhat constant
            return way[way.Count / 2];
        }

        private double distanceBetweenCenters(Edge<T> way1, Edge<T> way2)
        {
            Point p1 = getMidPoint(way1);
            Point p2 = getMidPoint(way2);

            return HaversineDistance(p1, p2);
        }

        public void buildEdges(Way<T> way, Way<T> otherWay, HashSet<T> excludedPoints)
        {
            this.edges = new Dictionary<Edge<T>, Edge<T>>();
            Dictionary<Edge<T>, List<Edge<T>>> wayEdges = this.buildEdgesForWay(way, excludedPoints);
            Dictionary<Edge<T>, List<Edge<T>>> otherWayEdges = this.buildEdgesForWay(otherWay, excludedPoints);

            foreach (KeyValuePair<Edge<T>, List<Edge<T>>> kv in wayEdges)
            {
                Edge<T> e = kv.Key;
                Console.WriteLine(e);
                List<Edge<T>> wayEdgesList = kv.Value;
                List<Edge<T>> otherWayEdgesList = otherWayEdges[e];
                foreach (Edge<T> wayEdge in wayEdgesList)
                {
                    foreach (Edge<T> otherWayEdge in otherWayEdgesList)
                    {
                        const double MAX_DISTANCE = 0.4;
                        double curDistance = distanceBetweenCenters(wayEdge, otherWayEdge);
                        if (curDistance < MAX_DISTANCE)
                        {
                            if (!this.edges.ContainsKey(wayEdge))
                            {
                                this.edges.Add(wayEdge, otherWayEdge);
                            }
                            else
                            {
                                Edge<T> curEdgeThere = this.edges[wayEdge];
                                double oldDistance = distanceBetweenCenters(wayEdge, curEdgeThere);
                                if (oldDistance < curDistance)
                                {
                                    this.edges[wayEdge] = otherWayEdge;
                                }
                            }
                        }
                        Console.WriteLine(wayEdge + "       " + otherWayEdge);
                        Console.WriteLine("------------------------------------------");
                    }
                }
            }
            Console.WriteLine("done");
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


        private bool pointOnSharedEdge(Way<T> wayToTraverse, T point, Way<T> otherWay, HashSet<T> excludedPoints)
        {
            // build edges lazily for performance reasons
            if (this.edges == null)
            {
                Console.WriteLine("THIS IS NULLLLLLLLLL THISHKSDFSDFS I SNULLLLLL");
                this.buildEdges(wayToTraverse, otherWay, excludedPoints);
            }

            foreach (KeyValuePair<Edge<T>, Edge<T>> kv in this.edges)
            {
                Edge<T> toTraverseEdge = null;
                // throw exception instead of one liner to help in users finding bugs (they'll report crashes)
                if (kv.Key.parentWay.Equals(wayToTraverse))
                {
                    Console.WriteLine("KEYYYYYYYYYY");
                    toTraverseEdge = kv.Key;
                }
                else if (kv.Value.parentWay.Equals(wayToTraverse))
                {
                    Console.WriteLine("VALUEEEEEEEEEE");
                    toTraverseEdge = kv.Value;
                }
                else
                {
                    throw new Exception("None of those parent way's match wayToTraverse");
                }

                foreach (T p in toTraverseEdge)
                {
                    if (point.Equals(p))
                    {
                        return true;
                    }
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
                        if (pointOnSharedEdge(wayToTraverse, wayToTraverse[startIdx], otherWay, excludedPoints))
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
