using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace FSEarthTilesInternalDLL
{
    public class ScenprocUtils
    {
        private static Dictionary<string, string> overPassServers = new Dictionary<string, string>
        {
            { "DE","http://overpass-api.de/api/interpreter" },
            { "FR","http://api.openstreetmap.fr/oapi/interpreter" },
            { "KU","https://overpass.kumi.systems/api/interpreter" },
            { "RU","http://overpass.osm.rambler.ru/cgi/interpreter" },
            { "MAP", "https://overpass-api.de/api/map?bbox=" }
        };

        private static string getOverpassData(string[] query, string bbox, string serverCode)
        {
            bool keepTrying = false;
            string contents = null;
            int sleepTime = 1;
            // SSLv3 is old - SecurityProtocolType.Tls12 the replacement for SecurityProtocolType.Ssl3
            // the below line is how you do it in .Net < 4.5
            // this is needed, or get "The request was aborted: Could not create SSL/TLS secure channel."
            // when connecting via https
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            do
            {
                foreach (KeyValuePair<string, string> kv in overPassServers)
                {
                    string server = kv.Value;
                    if (serverCode != null)
                    {
                        server = overPassServers[serverCode];
                    }
                    string url = server;
                    if (serverCode == "MAP")
                    {
                        url += bbox;
                    }
                    else
                    {
                        string queryParams = "?data=(";
                        foreach (string q in query)
                        {
                            queryParams += q + bbox + ";";
                        }
                        queryParams = queryParams.Remove(queryParams.Length - 1, 1);
                        queryParams += ";);(._;>>;);out body;";
                        url += queryParams;
                    }

                    // http://overpass-api.de/api/interpreter?data=(node["aeroway"](20, -77, 21, -76);way["aeroway"](20, -77, 21, -76);rel["aeroway"](20, -77, 21, -76););(._;>>;);out meta;
                    // https://overpass-api.de/api/map?bbox=-77,20,-76.75,20.25
                    using (var wc = new System.Net.WebClient())
                    {
                        try
                        {
                            // make sure to kill any zombie queries...
                            wc.DownloadString("http://overpass-api.de/api/kill_my_queries");

                            contents = wc.DownloadString(url);
                            keepTrying = false;
                            break;
                        }
                        catch (System.Net.WebException e)
                        {
                            Console.WriteLine(e.ToString());
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

        private static string getBbox(double endLat, double startLon, double startLat, double endLon, string serverCode)
        {
            string bbox = "(" + startLat + ", " + startLon + ", " + endLat + ", " + endLon + ")";
            if (serverCode == "MAP")
            {
                bbox = startLon + "," + startLat + "," + endLon + "," + endLat;
            }

            return bbox;
        }

        private static void downloadTileChunked(string workFolder, double[] tile)
        {
            const int NUM_SCENPROC_CHUNKS = 16;
            double NUM_STEPS = Math.Sqrt(NUM_SCENPROC_CHUNKS);
            for (int i = 0; i < NUM_STEPS; i++)
            {
                double minLon = tile[1];
                if (i > 0)
                {
                    minLon = tile[1] + (i / NUM_STEPS);
                }

                double maxLon = tile[1] + ((i + 1) / NUM_STEPS);

                for (int j = 0; j < NUM_STEPS; j++)
                {
                    double minLat = tile[0];
                    if (j > 0)
                    {
                        minLat = tile[0] + (j / NUM_STEPS);
                    }

                    double maxLat = tile[0] + ((j + 1) / NUM_STEPS);
                    Console.WriteLine("Attempting to download OSM data from " + minLat + ", " + minLon + " to " + maxLat + ", " + maxLon);
                    string bbox = getBbox(maxLat, minLon, minLat, maxLon, "MAP");
                    string osm = getOverpassData(null, bbox, "MAP");
                    string osmFilePath = CommonFunctions.GetTilePath(workFolder, tile) + @"\Scenproc_data\scenproc_osm_data" + i.ToString() + j.ToString() + ".osm";

                    File.WriteAllText(osmFilePath, osm);
                }
            }

            Console.WriteLine("Download successful");
        }

        public static void runScenproc(EarthArea iEarthArea, string scenprocLoc, string scenprocScript, string workFolder)
        {
            double startLong = iEarthArea.AreaSnapStartLongitude;
            double stopLong = iEarthArea.AreaSnapStopLongitude;
            double stopLat = iEarthArea.AreaSnapStopLatitude;
            double startLat = iEarthArea.AreaSnapStopLatitude;

            List<double[]> tilesToDownload = CommonFunctions.GetTilesToDownload(startLong, stopLong, startLat, stopLat);

            // TODO: speedup - for tiles at the edge, don't download data for the whole tile?
            foreach (double[] tile in tilesToDownload)
            {
                downloadTileChunked(workFolder, tile);
            }
        }
    }
}
