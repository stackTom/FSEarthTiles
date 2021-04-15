using FSEarthTilesInternalDLL;
using FSEarthTilesDLL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FSEarthTilesDLL
{
    class AreaKMLFromOSMDataCreator
    {
        private static int sides = 1000;
        private static List<List<double[]>> GetWays(string path)
        {
            XmlDocument d = new XmlDocument();
            d.Load(path);
            XmlNodeList wayTags = d.GetElementsByTagName("way");
            Dictionary<string, List<string>> waysToNodes = new Dictionary<string, List<string>>();
            Dictionary<string, double[]> nodesToCoords = new Dictionary<string, double[]>();

            XmlNodeList nodeTags = d.GetElementsByTagName("node");
            foreach (XmlElement node in nodeTags)
            {
                double lat = Convert.ToDouble(node.GetAttribute("lat"));
                double lon = Convert.ToDouble(node.GetAttribute("lon"));
                string id = node.GetAttribute("id");
                double[] coords = new double[] { lon, lat };
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
            List<List<double[]>> ways = new List<List<double[]>>();
            int n = 0;
            foreach (KeyValuePair<string, List<string>> kv in waysToNodes)
            {
                string wayID = kv.Key;
                List<string> nodIDs = kv.Value;
                List<double[]> way = new List<double[]>();
                foreach (string id in nodIDs)
                {
                    double[] coords = nodesToCoords[id];
                    way.Add(coords);
                }
                ways.Add(way);
            }
            return ways;
        }
        private static double[] getShift(double[] fromCoord, double[] toCoord)
        {
            double shift = 0.00003;
            // equal lon (x)
            Console.WriteLine("from " + fromCoord[0] + ", " + fromCoord[1] + " to " + toCoord[0] + ", " + toCoord[1]);
            if (fromCoord[0] == toCoord[0])
            {
                // going from north to south, water is on west
                if (fromCoord[1] > toCoord[1])
                {
                    Console.WriteLine("going from north south, water is on west");
                    return new double[] { -shift, 0.0};
                }
                // going from south to north, water is on east
                Console.WriteLine("going from south to north south, water is on east");
                return new double[] { shift, 0.0 };
            }
            // equal lat (y)
            if (fromCoord[1] == toCoord[1])
            {
                // going from east to west, water is on north
                if (fromCoord[0] > toCoord[0])
                {
                    Console.WriteLine("going from east to west, water is on north");
                    return new double[] { 0.0, shift };
                }
                // going from west to east, water is on south
                Console.WriteLine("going from west to east, water is on south");
                return new double[] { 0.0, -shift };
            }
            // neither lat nor lon are equal, look at the slope
            double dx = toCoord[0] - fromCoord[0];
            double dy = toCoord[1] - fromCoord[1];
            if ((dy / dx) > 0)
            {
                if (dy > 0)
                {
                    // going from southwest to northeast, water is on southeast
                    Console.WriteLine("going from southwest to northeast, water is on southeast");
                    return new double[] { shift, -shift };
                }
                // going from northeast to southwest, water is on northwest
                Console.WriteLine("going from northeast to southwest, water is on northwest");
                return new double[] { -shift, shift };
            }
            // at this point slope < 0. should never be 0 because lat's can't equal by this point
            if (dy > 0)
            {
                // going from southeast to northwest, water is on northeast
                Console.WriteLine("going from southeast to northwest, water is on northeast");
                return new double[] { shift, shift };
            }

            // going from northwest to southeast, water is on southwest
            Console.WriteLine("going from northwest to southeast, water is on southwest");
            return new double[] { -shift, -shift };
        }
        private static List<double[]> getShiftedWay(List<double[]> way)
        {
            List<double[]> shiftedWay = new List<double[]>();
            for (int i = 0; i < way.Count - 1; i++)
            {
                if (i == sides)
                {
                }
                double[] fromCoord = way[i];
                double[] toCoord = way[i + 1];
                double[] shift = getShift(fromCoord, toCoord);
                double[] shiftedFromCoord = new double[] { fromCoord[0] + shift[0], fromCoord[1] + shift[1] };
                double[] shiftedToCoord = new double[] { toCoord[0] + shift[0], toCoord[1] + shift[1] };
                Console.WriteLine("shiftedfrom " + shiftedFromCoord[0] + ", " + shiftedFromCoord[1] + " shiftedto " + shiftedToCoord[0] + ", " + shiftedToCoord[1]);
                //shiftedWay.Add(shiftedFromCoord);
                shiftedWay.Add(shiftedToCoord);
            }
            // closed polygon? let's close the shifted way too
            if (way[way.Count - 1][0] == way[0][0] && way[way.Count - 1][1] == way[0][1])
            {
                double[] fromCoord = shiftedWay[shiftedWay.Count - 1];
                double[] toCoord = way[0];

                double[] shift = getShift(fromCoord, toCoord);
                double[] shiftedFromCoord = new double[] { fromCoord[0] + shift[0], fromCoord[1] + shift[1] };
                double[] shiftedToCoord = new double[] { toCoord[0] + shift[0], toCoord[1] + shift[1] };
                shiftedWay.Add(shiftedFromCoord);
            }

            return shiftedWay;
        }
        public static string createWaterKMLFromOSM(string waterOSM, string coastOSM)
        {
            List<List<double[]>> waterWays = GetWays(@"F:\ortho4xpvm\fset-103\FSET\+26-081_water.osm");
            List<List<double[]>> coastWays = GetWays(@"F:\ortho4xpvm\fset-103\FSET\+26-081_coastline.osm");
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
            int n = 0;
            int j = 0;
            n++;
            int i = 0;
            foreach (List<double[]> way in coastWays)
            {
                if (i < 36)
                {
                    i++;
                }
                kml.Add("<Placemark>");
                kml.Add("<name>Coast</name>");
                kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
                kml.Add("<LineString>");
                kml.Add("<coordinates>");
                string coastCoords = "";
                j = 0;
                foreach (double[] coord in way)
                {
                    coastCoords += coord[0] + "," + coord[1] + ",0 ";
                    if (j == sides)
                    {
                    }
                    j++;
                }
                coastCoords = coastCoords.Remove(coastCoords.Length - 1, 1);
                kml.Add(coastCoords);
                kml.Add("</coordinates>");
                kml.Add("</LineString>");
                kml.Add("</Placemark>");
                kml.Add("<Placemark>");
                kml.Add("<name>DeepWater</name>");
                kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
                kml.Add("<LineString>");
                kml.Add("<coordinates>");
                string deepWatercoords = "";
                List<double[]> shiftedWay = getShiftedWay(way);
                j = 0;
                foreach (double[] coord in shiftedWay)
                {
                    deepWatercoords += coord[0] + "," + coord[1] + ",0 ";
                    if (j == sides)
                    {
                    }
                    j++;
                }
                deepWatercoords = deepWatercoords.Remove(deepWatercoords.Length - 1, 1);
                kml.Add(deepWatercoords);
                kml.Add("</coordinates>");
                kml.Add("</LineString>");
                kml.Add("</Placemark>");
                if (i == 36)
                {
                }
                i++;
            }
            n = 0;
/*            foreach (List<double[]> way in waterWays)
            {
                kml.Add("<Placemark>");
                kml.Add("<name>Blend</name>");
                kml.Add("<styleUrl>#yellowLineGreenPoly</styleUrl>");
                kml.Add("<LineString>");
                kml.Add("<coordinates>");
                string coords = "";
                foreach (double[] coord in way)
                {
                    coords += coord[0] + "," + coord[1] + ",0 ";
                }
                coords = coords.Remove(coords.Length - 1, 1);
                kml.Add(coords);
                kml.Add("</coordinates>");
                kml.Add("</LineString>");
                kml.Add("</Placemark>");
                n++;
            }
*/            kml.Add("</Folder>");
            kml.Add("</Document>");
            kml.Add("</kml>");

            return String.Join("\n", kml.ToArray());
        }
    }
    class AutomaticWaterMasking
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
        private static string downloadOsmWaterData(EarthArea iEarthArea, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            string[] waterQueries = { "rel[\"natural\"=\"water\"]", "rel[\"waterway\"=\"riverbank\"]", "way[\"natural\"=\"water\"]", "way[\"waterway\"=\"riverbank\"]", "way[\"waterway\"=\"dock\"]" };
            string waterOSM = null;
            string queryParams = "?data=(";
            string bbox = "(" + iEarthArea.AreaSnapStopLatitude + ", " + iEarthArea.AreaSnapStartLongitude + ", " + iEarthArea.AreaSnapStartLatitude + ", " + iEarthArea.AreaSnapStopLongitude + ")";
            foreach (string query in waterQueries)
            {
                queryParams += query + bbox + ";";
            }
            queryParams = queryParams.Remove(queryParams.Length - 1, 1);
            queryParams += ";);(._;>>;);out body;";

            //waterOSM = downloadOSM(queryParams, iFSEarthTilesInternalInterface);
            waterOSM = File.ReadAllText(@"F:\ortho4xpvm\fset-103\FSET\+26-081_water.osm");

            return waterOSM;
        }
        // http://overpass-api.de/api/interpreter?data=(way["natural"="coastline"](23, -83, 24, -82););(._;>>;);out meta;
        private static string downloadOsmCoastData(EarthArea iEarthArea, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            string[] coastQueries = { "way[\"natural\"=\"coastline\"]" };
            string coastOSM = null;
            string queryParams = "?data=(";
            string bbox = "(" + iEarthArea.AreaSnapStopLatitude + ", " + iEarthArea.AreaSnapStartLongitude + ", " + iEarthArea.AreaSnapStartLatitude + ", " + iEarthArea.AreaSnapStopLongitude + ")";
            foreach (string query in coastQueries)
            {
                queryParams += query + bbox + ";";
            }
            queryParams = queryParams.Remove(queryParams.Length - 1, 1);
            queryParams += ";);(._;>>;);out body;";

            //coastOSM = downloadOSM(queryParams, iFSEarthTilesInternalInterface);
            coastOSM = File.ReadAllText(@"F:\ortho4xpvm\fset-103\FSET\+26-081_coastline.osm");

            return coastOSM;
        }
        public static void createAreaKMLFromOSMData(EarthArea iEarthArea, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            string coastOSM = downloadOsmCoastData(iEarthArea, iFSEarthTilesInternalInterface);
            string waterOSM = downloadOsmWaterData(iEarthArea, iFSEarthTilesInternalInterface);
            string kml = AreaKMLFromOSMDataCreator.createWaterKMLFromOSM(waterOSM, coastOSM);
            File.WriteAllText(EarthConfig.mWorkFolder + "\\AreaKML.kml", kml);
        }
    }
}
