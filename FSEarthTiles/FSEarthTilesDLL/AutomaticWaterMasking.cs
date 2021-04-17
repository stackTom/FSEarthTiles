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


using Way = System.Collections.Generic.List<FSEarthTilesDLL.AreaKMLFromOSMDataCreator.Point>;

namespace FSEarthTilesDLL
{

    class AreaKMLFromOSMDataCreator
    {
        public struct Point
        {
            public double X;
            public double Y;
            public Point(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }
        }
        private static int sides = 1000000;
        private static bool doIt = false;
        private static bool doItSides = false;
        private static int theside = 65;
        private static List<Way> GetWays(string OSMKML)
        {
            XmlDocument d = new XmlDocument();
            d.LoadXml(OSMKML);
            XmlNodeList wayTags = d.GetElementsByTagName("way");
            Dictionary<string, List<string>> waysToNodes = new Dictionary<string, List<string>>();
            Dictionary<string, Point> nodesToCoords = new Dictionary<string, Point>();

            XmlNodeList nodeTags = d.GetElementsByTagName("node");
            foreach (XmlElement node in nodeTags)
            {
                double lat = Convert.ToDouble(node.GetAttribute("lat"));
                double lon = Convert.ToDouble(node.GetAttribute("lon"));
                string id = node.GetAttribute("id");
                Point coords = new Point(lon, lat);
                nodesToCoords.Add(id, coords);
            }
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
                waysToNodes.Add(id, nodes);
            }
            List<Way> ways = new List<Way>();
            foreach (KeyValuePair<string, List<string>> kv in waysToNodes)
            {
                string wayID = kv.Key;
                List<string> nodIDs = kv.Value;
                Way way = new Way();
                foreach (string id in nodIDs)
                {
                    Point coords = nodesToCoords[id];
                    way.Add(coords);
                }
                ways.Add(way);
            }
            return ways;
        }

        // the below code is thanks to Rod Stephens (http://csharphelper.com/blog/2020/12/enlarge-a-polygon-that-has-colinear-vertices-in-c/)
        // I've modified it slightly for this use case, but the fundamental idea is the same
        // ------------------------------------------------------------------------------------------------------
        private static Way GetEnlargedPolygon(Way old_points, double offset)
        {
            Way enlarged_points = new Way();
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
        private static Way GetEnlargedLine(Way old_points, double offset)
        {
            Way enlarged_points = GetEnlargedPolygon(old_points, offset);
            // Move the points by the offset.
            int j = 0;
            int i = old_points.Count - 1;
            int k = 1;
            if (old_points.Count == 2)
            {
                enlarged_points.Add(new Point());
                enlarged_points.Add(new Point());
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

        private static Way getShiftedWay(Way way)
        {
            Way shiftedWay = new Way();
            Point firstCoord = way[0];
            Point lastCoord = way[way.Count - 1];
            bool closedWay = firstCoord.X == lastCoord.X && firstCoord.Y == lastCoord.Y;

            Way ps = new Way();
            Way t = new Way();
            int mult = 100000;
            foreach (Point coord in way)
            {
                ps.Add(new Point(coord.X * mult, coord.Y * mult));
            }
            if (closedWay)
            {
                ps.RemoveAt(ps.Count - 1);
            }
            Way deepWaterPoints = null;
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
        public static string createWaterKMLFromOSM(string waterOSM, string coastOSM)
        {
            List<Way> waterWays = GetWays(waterOSM);
            List<Way> coastWays = GetWays(coastOSM);
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
            foreach (Way way in coastWays)
            {
                // the we take the coast from osm and use that as our DeepWater.
                // why? because polygon buffering algorithm I found online breaks if try to encase original polygon with new,
                // bigger one, but works great if make a new, slightly smaller polygon encased by the original, bigger one
                kml.Add("<Placemark>");
                kml.Add("<name>DeepWater</name>");
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
                kml.Add("<Placemark>");
                kml.Add("<name>Coast</name>");
                kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
                kml.Add("<LineString>");
                kml.Add("<coordinates>");
                string coastCoords = "";
                Way shiftedWay = getShiftedWay(way);
                foreach (Point coord in shiftedWay)
                {
                    coastCoords += coord.X + "," + coord.Y + ",0 ";
                }
                coastCoords = coastCoords.Remove(coastCoords.Length - 1, 1);
                kml.Add(coastCoords);
                kml.Add("</coordinates>");
                kml.Add("</LineString>");
                kml.Add("</Placemark>");
            }
            foreach (Way way in waterWays)
            {
                kml.Add("<Placemark>");
                kml.Add("<name>Blend</name>");
                kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
                kml.Add("<LineString>");
                kml.Add("<coordinates>");
                string coords = "";
                foreach (Point coord in way)
                {
                    coords += coord.X + "," + coord.Y + ",0 ";
                }
                coords = coords.Remove(coords.Length - 1, 1);
                kml.Add(coords);
                kml.Add("</coordinates>");
                kml.Add("</LineString>");
                kml.Add("</Placemark>");
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
            double PADDING = 0.05;
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
    }
}
