using FSEarthTilesInternalDLL;
using FSEarthTilesDLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.Windows;
using TGASharpLib;


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
    }
    class Way<T> : System.Collections.Generic.List<T> where T : FSEarthTilesDLL.Point
    {
        public string relation;
        public bool mergeWithWay(Way<T> way)
        {
            Point w1p1 = this[0];
            Point w1p2 = this[this.Count - 1];
            Point w2p1 = way[0];
            Point w2p2 = way[way.Count - 1];
            if (w1p1 == w2p1)
            {
                this.Reverse();
                for (int w2 = 1; w2 < way.Count; w2++)
                {
                    this.Add(way[w2]);
                }

                return true;
            }
            if (w1p2 == w2p2)
            {
                for (int w2 = way.Count - 2; w2 >= 0; w2--)
                {
                    this.Add(way[w2]);
                }

                return true;
            }
            if (w1p2 == w2p1)
            {
                for (int w2 = 1; w2 < way.Count; w2++)
                {
                    this.Add(way[w2]);
                }

                return true;
            }
            if (w1p1 == w2p2)
            {
                this.Reverse();
                for (int w2 = way.Count - 2; w2 >= 0; w2--)
                {
                    this.Add(way[w2]);
                }

                return true;
            }

            return false;
        }
    }
    class AreaKMLFromOSMDataCreator
    {
        private static Dictionary<string, Point> getNodeIDsToCoords(XmlDocument d)
        {
            Dictionary<string, Point> nodeIDsToCoords = new Dictionary<string, Point>();
            XmlNodeList nodeTags = d.GetElementsByTagName("node");
            foreach (XmlElement node in nodeTags)
            {
                double lat = Convert.ToDouble(node.GetAttribute("lat"));
                double lon = Convert.ToDouble(node.GetAttribute("lon"));
                string id = node.GetAttribute("id");
                Point coords = new Point(lon, lat);
                nodeIDsToCoords.Add(id, coords);
            }

            return nodeIDsToCoords;
        }

        private static Dictionary<string, List<string>> getWayIDsToWayNodes(XmlDocument d)
        {
            Dictionary<string, List<string>> wayIDsToWayNodes = new Dictionary<string, List<string>>();
            XmlNodeList wayTags = d.GetElementsByTagName("way");
            foreach (XmlElement way in wayTags)
            {
                string id = way.GetAttribute("id");

                List<string> nodes = new List<string>();
                XmlNodeList ndTags = way.GetElementsByTagName("nd");
                foreach (XmlElement nd in ndTags)
                {
                    string ndId = nd.GetAttribute("ref");
                    nodes.Add(ndId);
                }
                wayIDsToWayNodes.Add(id, nodes);
            }

            return wayIDsToWayNodes;
        }

        private static Dictionary<string, Way<Point>> getWayIDsToWays(XmlDocument d, Dictionary<string, List<string>> wayIDsToWayNodes, Dictionary<string, Point> nodeIDsToCoords)
        {
            Dictionary<string, Way<Point>> wayIDsToways = new Dictionary<string, Way<Point>>();

            foreach (KeyValuePair<string, List<string>> kv in wayIDsToWayNodes)
            {
                string wayID = kv.Key;
                List<string> nodIDs = kv.Value;
                Way<Point> way = new Way<Point>();
                foreach (string id in nodIDs)
                {
                    Point coords = nodeIDsToCoords[id];
                    way.Add(coords);
                }
                wayIDsToways.Add(wayID, way);
            }

            return wayIDsToways;
        }
        private static List<string> getWaysInThisMultipolygonAndUpdateRelations(XmlElement rel, Dictionary<string, string> wayIDsToRelation)
        {
            List<string> waysInThisMultipolygon = new List<string>();

            foreach (XmlElement tag in rel.GetElementsByTagName("tag"))
            {
                if (tag.GetAttribute("v") == "multipolygon")
                {
                    foreach (XmlElement member in rel.GetElementsByTagName("member"))
                    {
                        string wayID = member.GetAttribute("ref");
                        waysInThisMultipolygon.Add(wayID);
                        string role = member.GetAttribute("role");
                        string curRole = null;
                        wayIDsToRelation.TryGetValue(wayID, out curRole);
                        if (curRole == null)
                        {
                            wayIDsToRelation.Add(wayID, role);
                        }
                        else if (curRole != role)
                        {
                            // should hopefully never get here. if we do, this will help users give me test cases so I can investigate
                            throw new Exception("A way has more than one role type!");
                        }
                    }
                }
            }

            return waysInThisMultipolygon;
        }

        // NOTE: this function modifies WayIDsToWays by removing all individual multipolygon segments that are able to be combined
        // with another segment
        private static void mergeMultipolygonWays(List<string> waysInThisMultipolygon, Dictionary<string, Way<Point>> wayIDsToWays)
        {
            for (int i = 0; i < waysInThisMultipolygon.Count; i++)
            {
                for (int j = 0; j < waysInThisMultipolygon.Count; j++)
                {
                    // i != j makes sure not comparing to the same way
                    if (i != j)
                    {
                        string way1id = waysInThisMultipolygon[i];
                        string way2id = waysInThisMultipolygon[j];
                        // make sure the way hasn't been removed due to being combined previously...
                        if (!wayIDsToWays.ContainsKey(way1id) || !wayIDsToWays.ContainsKey(way2id))
                        {
                            continue;
                        }
                        Way<Point> way1 = wayIDsToWays[waysInThisMultipolygon[i]];
                        Way<Point> way2 = wayIDsToWays[waysInThisMultipolygon[j]];
                        bool ableToMerge = way1.mergeWithWay(way2);
                        if (ableToMerge)
                        {
                            wayIDsToWays[waysInThisMultipolygon[i]] = way1;
                            wayIDsToWays.Remove(waysInThisMultipolygon[j]);
                        }
                    }
                }
            }
        }

        private static List<Way<Point>> GetWays(string OSMKML)
        {
            XmlDocument d = new XmlDocument();
            d.LoadXml(OSMKML);

            Dictionary<string, List<string>> wayIDsToWayNodes = getWayIDsToWayNodes(d);
            Dictionary<string, Point> nodeIDsToCoords = getNodeIDsToCoords(d);
            Dictionary<string, Way<Point>> wayIDsToWays = getWayIDsToWays(d, wayIDsToWayNodes, nodeIDsToCoords);
            // POSSIBLE BUG: can a way every be both inner and outer in a relationship?
            Dictionary<string, string> wayIDsToRelation = new Dictionary<string, string>();

            XmlNodeList relationTags = d.GetElementsByTagName("relation");
            foreach (XmlElement rel in relationTags)
            {
                // unite multipolygon pieces into one big linestring, since fset just looks at line. no point in doing a kml polygon
                // since fset seems to not understand outer vs inner relation. we need this because singular way lines of inner water are problematic
                // it is hard to determine direction the water is in relative to the way because I believe OSM only requires direction
                // for coastal ways. but if we make them a full polygon, then it is easy to determine that the water is inside the polygon
                // here we compare every way to every other way.
                List<string> waysInThisMultipolygon = getWaysInThisMultipolygonAndUpdateRelations(rel, wayIDsToRelation);
                mergeMultipolygonWays(waysInThisMultipolygon, wayIDsToWays);
            }

            // update relations
            foreach (KeyValuePair<string, Way<Point>> kv in wayIDsToWays)
            {
                string wayID = kv.Key;
                Way<Point> way = kv.Value;
                way.relation = null;
                if (wayIDsToRelation.ContainsKey(wayID))
                {
                    way.relation = wayIDsToRelation[wayID];
                }
            }

            return wayIDsToWays.Values.ToList();
        }

        // the below code is thanks to Rod Stephens (http://csharphelper.com/blog/2020/12/enlarge-a-polygon-that-has-colinear-vertices-in-c/)
        // I've modified it slightly for this use case, but the fundamental idea is the same
        // ------------------------------------------------------------------------------------------------------
        private static Way<Point> GetEnlargedPolygon(Way<Point> old_points, double offset)
        {
            Way<Point> enlarged_points = new Way<Point>();
            int num_points = old_points.Count;
            for (int j = 0; j < num_points; j++)
            {
                // Find the new location for point j.
                // Find the points before and after j.
                int i = (j - 1);
                if (i < 0) i += num_points;
                int k = (j + 1) % num_points;

                // Move the points by the offset.
                Vector v1 = new Vector(
                    old_points[j].X - old_points[i].X,
                    old_points[j].Y - old_points[i].Y);
                v1.Normalize();
                v1 *= offset;
                Vector n1 = new Vector(-v1.Y, v1.X);

                Point pij1 = new Point(
                    (old_points[i].X + n1.X),
                    (double)(old_points[i].Y + n1.Y));
                Point pij2 = new Point(
                    (old_points[j].X + n1.X),
                    (old_points[j].Y + n1.Y));

                Vector v2 = new Vector(
                    old_points[k].X - old_points[j].X,
                    old_points[k].Y - old_points[j].Y);
                v2.Normalize();
                v2 *= offset;
                Vector n2 = new Vector(-v2.Y, v2.X);

                Point pjk1 = new Point(
                    (old_points[j].X + n2.X),
                    (old_points[j].Y + n2.Y));
                Point pjk2 = new Point(
                    (old_points[k].X + n2.X),
                    (old_points[k].Y + n2.Y));

                // See where the shifted lines ij and jk intersect.
                bool lines_intersect, segments_intersect;
                Point poi, close1, close2;
                FindIntersection(pij1, pij2, pjk1, pjk2,
                    out lines_intersect, out segments_intersect,
                    out poi, out close1, out close2);
                if (lines_intersect) enlarged_points.Add(poi);
            }

            return enlarged_points;
        }
        // basically uses GetEnlargedPolygon, except changes the first and last points so it forms a line and not a polygon.
        // Code is messy, but it works. TODO: try to refactor and make less messy?
        private static Way<Point> GetEnlargedLine(Way<Point> old_points, double offset)
        {
            Way<Point> enlarged_points = GetEnlargedPolygon(old_points, offset);
            // Move the points by the offset.
            int j = 0;
            int i = old_points.Count - 1;
            int k = 1;
            if (old_points.Count == 2)
            {
                enlarged_points.Add(new Point(0.0, 0.0));
                enlarged_points.Add(new Point(0.0, 0.0));
            }
            Vector v1 = new Vector(
                old_points[j].X - old_points[i].X,
                old_points[j].Y - old_points[i].Y);
            v1.Normalize();
            v1 *= offset;
            Vector n1 = new Vector(-v1.Y, v1.X);

            Point pij1 = new Point(
                (old_points[i].X + n1.X),
                (double)(old_points[i].Y + n1.Y));
            Point pij2 = new Point(
                (old_points[j].X + n1.X),
                (old_points[j].Y + n1.Y));

            Vector v2 = new Vector(
                old_points[k].X - old_points[j].X,
                old_points[k].Y - old_points[j].Y);
            v2.Normalize();
            v2 *= offset;
            Vector n2 = new Vector(-v2.Y, v2.X);

            Point pjk1 = new Point(
                (old_points[j].X + n2.X),
                (old_points[j].Y + n2.Y));
            Point pjk2 = new Point(
                (old_points[k].X + n2.X),
                (old_points[k].Y + n2.Y));

            enlarged_points[0] = pjk1;

            j = enlarged_points.Count - 1;
            i = j - 1;
            k = 0;
            v1 = new Vector(
                old_points[j].X - old_points[i].X,
                old_points[j].Y - old_points[i].Y);
            v1.Normalize();
            v1 *= offset;
            n1 = new Vector(-v1.Y, v1.X);

            pij1 = new Point(
                (old_points[i].X + n1.X),
                (double)(old_points[i].Y + n1.Y));
            pij2 = new Point(
                (old_points[j].X + n1.X),
                (old_points[j].Y + n1.Y));

            v2 = new Vector(
                old_points[k].X - old_points[j].X,
                old_points[k].Y - old_points[j].Y);
            v2.Normalize();
            v2 *= offset;
            n2 = new Vector(-v2.Y, v2.X);

            pjk1 = new Point(
                (old_points[j].X + n2.X),
                (old_points[j].Y + n2.Y));
            pjk2 = new Point(
                (old_points[k].X + n2.X),
                (old_points[k].Y + n2.Y));

            enlarged_points[enlarged_points.Count - 1] = pij2;

            return enlarged_points;
        }


        // Find the point of intersection between
        // the lines p1 --> p2 and p3 --> p4.
        private static void FindIntersection(
            Point p1, Point p2, Point p3, Point p4,
            out bool lines_intersect, out bool segments_intersect,
            out Point intersection,
            out Point close_p1, out Point close_p2)
        {
            // Get the segments' parameters.
            double dx12 = p2.X - p1.X;
            double dy12 = p2.Y - p1.Y;
            double dx34 = p4.X - p3.X;
            double dy34 = p4.Y - p3.Y;

            // Solve for t1 and t2
            double denominator = (dy12 * dx34 - dx12 * dy34);
            bool lines_parallel = (Math.Abs(denominator) < 0.001);

            double t1 =
                ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34)
                    / denominator;
            if (double.IsNaN(t1) || double.IsInfinity(t1))
                lines_parallel = true;

            if (lines_parallel)
            {
                // The lines are parallel (or close enough to it).
                lines_intersect = false;
                segments_intersect = false;
                intersection = new Point(double.NaN, double.NaN);
                close_p1 = new Point(double.NaN, double.NaN);
                close_p2 = new Point(double.NaN, double.NaN);
                return;
            }
            lines_intersect = true;

            double t2 =
                ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12)
                    / -denominator;

            // Find the point of intersection.
            intersection = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);

            // The segments intersect if t1 and t2 are between 0 and 1.
            segments_intersect =
                ((t1 >= 0) && (t1 <= 1) &&
                 (t2 >= 0) && (t2 <= 1));

            // Find the closest points on the segments.
            if (t1 < 0)
            {
                t1 = 0;
            }
            else if (t1 > 1)
            {
                t1 = 1;
            }

            if (t2 < 0)
            {
                t2 = 0;
            }
            else if (t2 > 1)
            {
                t2 = 1;
            }

            close_p1 = new Point(p1.X + dx12 * t1, p1.Y + dy12 * t1);
            close_p2 = new Point(p3.X + dx34 * t2, p3.Y + dy34 * t2);
        }
        // ------------------------------------------------------------------------------------------------------

        private static double getWayArea(Way<Point> way)
        {
            double area = Math.Abs(way.Take(way.Count - 1).Select((p, i) => (way[i + 1].X - p.X) * (way[i + 1].Y + p.Y)).Sum() / 2);

            return area;
        }
        private static bool isClosedWay(Way<Point> way)
        {
            Point firstCoord = way[0];
            Point lastCoord = way[way.Count - 1];

            return firstCoord.X == lastCoord.X && firstCoord.Y == lastCoord.Y;
        }
        private static Way<Point> getShiftedWay(Way<Point> way)
        {
            Way<Point> shiftedWay = new Way<Point>();
            bool closedWay = isClosedWay(way);

            Way<Point> ps = new Way<Point>();
            Way<Point> t = new Way<Point>();
            int mult = 100000;
            foreach (Point coord in way)
            {
                ps.Add(new Point(coord.X * mult, coord.Y * mult));
            }
            if (closedWay)
            {
                ps.RemoveAt(ps.Count - 1);
            }
            Way<Point> deepWaterPoints = null;
            if (!closedWay)
            {
                deepWaterPoints = GetEnlargedLine(ps, 1);
            }
            else
            {
                deepWaterPoints = GetEnlargedPolygon(ps, 1);
            }
            foreach (Point p in deepWaterPoints)
            {
                shiftedWay.Add(new Point(p.X / mult, p.Y / mult));
            }
            if (closedWay)
            {
                Point lastWay = shiftedWay[0];
                shiftedWay.Add(new Point(lastWay.X, lastWay.Y));
            }

            return shiftedWay;
        }
        private static string getLineStringPlacemark(string name, string coordinates)
        {
            return null;
        }

        private static void appendLineStringPlacemark(List<string> kml, string name, Way<Point> way)
        {
            kml.Add("<Placemark>");
            kml.Add("<name>" + name + "</name>");
            kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
            kml.Add("<LineString>");
            kml.Add("<coordinates>");
            string deepWaterCoords = "";
            foreach (Point coord in way)
            {
                deepWaterCoords += coord.X + "," + coord.Y + ",0 ";
            }
            deepWaterCoords = deepWaterCoords.Remove(deepWaterCoords.Length - 1, 1);
            kml.Add(deepWaterCoords);
            kml.Add("</coordinates>");
            kml.Add("</LineString>");
            kml.Add("</Placemark>");
        }

        public static string createWaterKMLFromOSM(string waterOSM, string coastOSM)
        {
            List<Way<Point>> waterWays = GetWays(waterOSM);
            List<Way<Point>> coastWays = GetWays(coastOSM);
            List<string> kml = new List<string>();
            kml.Add("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            kml.Add("<kml xmlns=\"http://earth.google.com/kml/2.2\">");
            kml.Add("<Document>");
            kml.Add("<Style id = \"yellowLineGreenPoly\" >");
            kml.Add("<LineStyle>");
            kml.Add("<color>7f00ffff</color>");
            kml.Add("<width>4</width>");
            kml.Add("</LineStyle>");
            kml.Add("<PolyStyle>");
            kml.Add("<color>7f00ff00</color>");
            kml.Add("</PolyStyle>");
            kml.Add("</Style>");
            kml.Add("<Folder>");
            kml.Add("<name>Test</name>");
            kml.Add("<open>1</open>");
            foreach (Way<Point> way in coastWays)
            {
                // the we take the coast from osm and use that as our DeepWater.
                // why? because polygon buffering algorithm I found online breaks if try to encase original polygon with new,
                // bigger one, but works great if make a new, slightly smaller polygon encased by the original, bigger one
                appendLineStringPlacemark(kml, "DeepWater", way);
                Way<Point> shiftedWay = getShiftedWay(way);
                appendLineStringPlacemark(kml, "Coast", shiftedWay);
            }
            foreach (Way<Point> way in waterWays)
            {
                Way<Point> shiftedWay = getShiftedWay(way);
                Way<Point> coastWay = shiftedWay;
                Way<Point> deepWaterWay = way;
                if (way.relation == "inner")
                {
                    appendLineStringPlacemark(kml, "BlendPool", way);
                }
                else
                {
                    appendLineStringPlacemark(kml, "DeepWaterTwo", deepWaterWay);
                    shiftedWay = getShiftedWay(way);
                    double origArea = getWayArea(way);
                    double shiftedArea = getWayArea(shiftedWay);
                    if (origArea < shiftedArea)
                    {
                        deepWaterWay = way;
                        coastWay = shiftedWay;
                    }
                    else
                    {
                        deepWaterWay = shiftedWay;
                        coastWay = way;
                    }
                    appendLineStringPlacemark(kml, "CoastTwo", coastWay);
                }
            }

            kml.Add("</Folder>");
            kml.Add("</Document>");
            kml.Add("</kml>");

            return String.Join("\n", kml.ToArray());
        }
    }
    public class AutomaticWaterMasking
    {
        private static string[] overPassServers = {
            "http://overpass-api.de/api/interpreter",
            "http://api.openstreetmap.fr/oapi/interpreter",
            "https://overpass.kumi.systems/api/interpreter",
            "http://overpass.osm.rambler.ru/cgi/interpreter",
        };

        private static string downloadOSM(string queryParams, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            bool keepTrying = false;
            string contents = null;
            int sleepTime = 1;
            do
            {
                foreach (string server in overPassServers)
                {
                    using (var wc = new System.Net.WebClient())
                    {
                        try
                        {
                            // make sure to kill any zombie queries...
                            wc.DownloadString("http://overpass-api.de/api/kill_my_queries");

                            contents = wc.DownloadString(server + queryParams);
                            keepTrying = false;
                            break;
                        }
                        catch (System.Net.WebException e)
                        {
                            iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Download failed using " + server + "... trying new overpass server in " + sleepTime + " seconds");
                            keepTrying = true;
                            System.Threading.Thread.Sleep(sleepTime);
                        }
                    }
                }
                if (sleepTime < 32)
                {
                    sleepTime *= 2;
                }
            } while (keepTrying);

            return contents;
        }
        private struct DownloadArea
        {
            public double startLon;
            public double endLon;
            public double startLat;
            public double endLat;

            public DownloadArea(double startLon, double endLon, double startLat, double endLat)
            {
                this.startLon = startLon;
                this.endLon = endLon;
                this.startLat = startLat;
                this.endLat = endLat;
            }
        }

        private static string downloadOsmWaterData(DownloadArea d, string saveLoc, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            string[] waterQueries = { "rel[\"natural\"=\"water\"]", "rel[\"waterway\"=\"riverbank\"]", "way[\"natural\"=\"water\"]", "way[\"waterway\"=\"riverbank\"]", "way[\"waterway\"=\"dock\"]" };
            string waterOSM = null;
            string queryParams = "?data=(";
            string bbox = "(" + d.endLat + ", " + d.startLon + ", " + d.startLat + ", " + d.endLon + ")";
            foreach (string query in waterQueries)
            {
                queryParams += query + bbox + ";";
            }
            queryParams = queryParams.Remove(queryParams.Length - 1, 1);
            queryParams += ";);(._;>>;);out body;";

            waterOSM = downloadOSM(queryParams, iFSEarthTilesInternalInterface);
            File.WriteAllText(saveLoc, waterOSM);

            return waterOSM;
        }
        // http://overpass-api.de/api/interpreter?data=(way["natural"="coastline"](23, -83, 24, -82););(._;>>;);out meta;
        private static string downloadOsmCoastData(DownloadArea d, string saveLoc, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            string[] coastQueries = { "way[\"natural\"=\"coastline\"]" };
            string coastOSM = null;
            string queryParams = "?data=(";
            string bbox = "(" + d.endLat + ", " + d.startLon + ", " + d.startLat + ", " + d.endLon + ")";
            foreach (string query in coastQueries)
            {
                queryParams += query + bbox + ";";
            }
            queryParams = queryParams.Remove(queryParams.Length - 1, 1);
            queryParams += ";);(._;>>;);out body;";

            coastOSM = downloadOSM(queryParams, iFSEarthTilesInternalInterface);
            File.WriteAllText(saveLoc, coastOSM);

            return coastOSM;
        }
        private static string getAreaHash(EarthArea iEarthArea)
        {
            string startLon = iEarthArea.AreaSnapStartLongitude.ToString();
            string stopLon = iEarthArea.AreaSnapStopLongitude.ToString();
            string startLat = iEarthArea.AreaSnapStartLatitude.ToString();
            string stopLat = iEarthArea.AreaSnapStopLatitude.ToString();

            return startLon + stopLon + startLat + stopLat;
        }
        public static void createAreaKMLFromOSMData(EarthArea iEarthArea, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            DownloadArea d = new DownloadArea(iEarthArea.AreaSnapStartLongitude, iEarthArea.AreaSnapStopLongitude, iEarthArea.AreaSnapStartLatitude, iEarthArea.AreaSnapStopLatitude);
            // we need a padding otherwise there seems to be rounding errors and edge cases when creating coasts
            double PADDING = 0.0;
            d.startLat += PADDING;
            d.endLat -= PADDING;
            d.startLon -= PADDING;
            d.endLon += PADDING;
            string coastOSM = null;
            string coastOSMFileLoc = EarthConfig.mWorkFolder + "\\" + getAreaHash(iEarthArea) + "coast.osm";
            if (File.Exists(coastOSMFileLoc))
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Recycling already downloaded OSM coast data");
                coastOSM = File.ReadAllText(coastOSMFileLoc);
            }
            else
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Downloading OSM coast data for water masking...");
                coastOSM = downloadOsmCoastData(d, coastOSMFileLoc, iFSEarthTilesInternalInterface);
            }
            string waterOSM = null;
            string waterOSMFileLoc = EarthConfig.mWorkFolder + "\\" + getAreaHash(iEarthArea) + "water.osm";
            if (File.Exists(waterOSMFileLoc))
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Recycling already downloaded OSM water data");
                waterOSM = File.ReadAllText(waterOSMFileLoc);
            }
            else
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Downloading OSM water data for water masking...");
                waterOSM = downloadOsmWaterData(d, waterOSMFileLoc, iFSEarthTilesInternalInterface);
            }
            string kml = AreaKMLFromOSMDataCreator.createWaterKMLFromOSM(waterOSM, coastOSM);
            File.WriteAllText(EarthConfig.mWorkFolder + "\\AreaKML.kml", kml);
        }
        private static bool BMPAllBlack(Bitmap b)
        {
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    // if even one alpha is not black, then bmp is not all black
                    byte curAlphaVal = b.GetPixel(x, y).A;
                    if (curAlphaVal != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public static void ensureTGAsNotAllBlack(string directoryToCheck)
        {
            string[] tgas = Directory.GetFiles(directoryToCheck, "*.tga");
            foreach (string f in tgas)
            {
                TGA t = new TGA(f);
                Bitmap b = t.ToBitmap(true);
                if (BMPAllBlack(b))
                {
                    Color color = b.GetPixel(0, 0);
                    Color newColor = Color.FromArgb(255, color);
                    b.SetPixel(0, 0, newColor);
                    TGA newAlphaTGA = new TGA(b);
                    string newAlphaTGAPath = Path.GetDirectoryName(f) + @"\" + Path.GetFileNameWithoutExtension(f) + @".tga";
                    newAlphaTGA.Save(newAlphaTGAPath);
                }
            }
        }
    }
}
