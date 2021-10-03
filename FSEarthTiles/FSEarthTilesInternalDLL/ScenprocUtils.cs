using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

namespace FSEarthTilesInternalDLL
{
    public class ScenprocUtils
    {
        public static bool ScenProcRunning = false;
        private static bool shouldStop = false;
        private static HashSet<Thread> runningThreads = new HashSet<Thread>();
        private static HashSet<string> runningTiles = new HashSet<string>();
        public static string scriptsDir = null;

        private static Dictionary<string, string> overPassServers = new Dictionary<string, string>
        {
            { "DE","http://overpass-api.de/api/interpreter" },
            { "FR","http://api.openstreetmap.fr/oapi/interpreter" },
            { "KU","https://overpass.kumi.systems/api/interpreter" },
            { "RU","http://overpass.osm.rambler.ru/cgi/interpreter" },
            { "MAP", "https://overpass-api.de/api/map?bbox=" }
        };

        public static void ClearZombieQueries()
        {

            using (var wc = new System.Net.WebClient())
            {
                try
                {
                    // make sure to kill any zombie queries...
                    wc.DownloadString("http://overpass-api.de/api/kill_my_queries");
                }
                catch (System.Net.WebException)
                {
                }
            }
        }

        private static string GetOverpassData(string[] query, string bbox, string serverCode)
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

                    if (shouldStop)
                    {
                        return null;
                    }

                    using (var wc = new System.Net.WebClient())
                    {
                        try
                        {
                            contents = wc.DownloadString(url);
                            keepTrying = false;
                            break;
                        }
                        catch (System.Net.WebException)
                        {
                            if (shouldStop)
                            {
                                return null;
                            }
                            Console.WriteLine("Download failed using " + server + "... trying new overpass server in " + sleepTime + " seconds");
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

        private static string GetBbox(double endLat, double startLon, double startLat, double endLon, string serverCode)
        {
            string bbox = "(" + startLat + ", " + startLon + ", " + endLat + ", " + endLon + ")";
            if (serverCode == "MAP")
            {
                bbox = startLon + "," + startLat + "," + endLon + "," + endLat;
            }

            return bbox;
        }

        private static void DownloadTileChunked(string workFolder, double startLong, double stopLong, double startLat, double stopLat)
        {
            double minLon = startLong;
            double maxLon = startLong;
            double minLat = startLat;
            double maxLat = startLat;
            string[] buildingsAndTreesTags = { "way[\"natural\"]", "way[\"landuse\"]", "way[\"leisure\"]", "way[\"building\"]",
                                               "rel[\"natural\"]", "rel[\"landuse\"]", "rel[\"leisure\"]", "rel[\"building\"]" };

            List<List<double[]>> chunks = CommonFunctions.GetPiecesFromGrid(startLong, stopLong, startLat, stopLat, 0.5);

            for (int i = 0; i < chunks.Count; i++)
            {
                for (int j = 0; j < chunks[i].Count; j++)
                {
                    string scenprocDataDir = getOSMDataPath(workFolder, startLong, stopLong, startLat, stopLat);
                    string osmFilePath = scenprocDataDir + @"\scenproc_osm_data" + i.ToString() + "_" + j.ToString() + ".osm";
                    if (!File.Exists(osmFilePath))
                    {
                        minLat = chunks[i][j][1];
                        minLon = chunks[i][j][0];
                        maxLat = chunks[i][j][3];
                        maxLon = chunks[i][j][2];
                        Console.WriteLine("Attempting to download OSM data from " + minLat + ", " + minLon + " to " + maxLat + ", " + maxLon);
                        string bbox = GetBbox(maxLat, minLon, minLat, maxLon, "MAP");
                        if (shouldStop)
                        {
                            return;
                        }
                        string osm = GetOverpassData(buildingsAndTreesTags, bbox, "MAP");

                        Directory.CreateDirectory(scenprocDataDir);
                        File.WriteAllText(osmFilePath, osm);
                        Console.WriteLine("Download successful");
                    }
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int FreeConsole();

        // for all these scenproc functions, we pass an EarthMultiArea because this is always the full area. FSEarthTilesForm mEarthArea
        // initially has the same coordinates as the mEarthMuliArea, but then it gets overwritten to the individual reference areas it is building
        // we now call these functions before mEarthArea has been overwritten, but I want to make this API change in case we want to call these
        // functions in the future AFTER it has been overwritten
        private static void StartScenProcAndWaitUntilFinished(EarthMultiArea iEarthArea, string scenprocLoc, string scenprocScript, string workFolder)
        {
            Thread t = new Thread(() => RunScenproc(iEarthArea, scenprocLoc, scenprocScript, workFolder));
            runningThreads.Add(t);
            t.Start();
            ScenProcRunning = true;
            t.Join();
            runningThreads.Remove(t);
            if (runningThreads.Count == 0)
            {
                ScenProcRunning = false;
            }
        }

        public static void RunScenprocThreaded(EarthMultiArea iEarthArea, string scenprocLoc, string scenprocScript, string workFolder)
        {
            shouldStop = false;
            Thread t = new Thread(() => StartScenProcAndWaitUntilFinished(iEarthArea, scenprocLoc, scenprocScript, workFolder));
            t.Start();
        }

        public static void TellScenprocToTerminate()
        {
            shouldStop = true;
            FreeConsole();
            ScenProcRunning = false;
        }

        private static string getOSMDataPath(string workFolder, double startLong, double stopLong, double startLat, double stopLat)
        {
            return workFolder + @"\OSM_data\Scenproc\" + startLong + stopLong + startLat + stopLat;
        }

        public static void RunScenproc(EarthMultiArea iEarthArea, string scenprocLoc, string scenprocScript, string workFolder)
        {
            AllocConsole();
            // set Console stdin and stdout again, or get crashes on subsequent calls of this function. Why? it appears these handles
            // are set when the program first starts, even though we don't have a console. Free'ing and the alloc'ing a new console
            // causes the handles to not be set correctly to the new console, and Console.WriteLine crashes with an invalid handle exception
            // see: https://stackoverflow.com/questions/42612872/exception-when-using-console-window-in-a-form-application
            TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
            Console.SetIn(new StreamReader(Console.OpenStandardInput()));

            double startLong = iEarthArea.AreaSnapStartLongitude < iEarthArea.AreaSnapStopLongitude ? iEarthArea.AreaSnapStartLongitude : iEarthArea.AreaSnapStopLongitude;
            double stopLong = startLong == iEarthArea.AreaSnapStartLongitude ? iEarthArea.AreaSnapStopLongitude : iEarthArea.AreaSnapStartLongitude;
            double startLat = iEarthArea.AreaSnapStartLatitude < iEarthArea.AreaSnapStopLatitude ? iEarthArea.AreaSnapStartLatitude : iEarthArea.AreaSnapStopLatitude;
            double stopLat = startLat == iEarthArea.AreaSnapStartLatitude ? iEarthArea.AreaSnapStopLatitude : iEarthArea.AreaSnapStartLatitude;

            if (shouldStop)
            {
                return;
            }
            bool tileAlreadyRunning = false;
            string tileStr = startLong.ToString() + stopLong.ToString() + startLat.ToString() + stopLat.ToString();
            lock (runningTiles)
            {
                if (runningTiles.Contains(tileStr))
                {
                    tileAlreadyRunning = true;
                }
                else
                {
                    runningTiles.Add(tileStr);
                }
            }
            if (tileAlreadyRunning)
            {
                return;
            }
            DownloadTileChunked(workFolder, startLong, stopLong, startLat, stopLat);
            string scenprocDataDir = getOSMDataPath(workFolder, startLong, stopLong, startLat, stopLat);
            string[] osmFiles = Directory.GetFiles(scenprocDataDir, "*.osm");
            foreach (string osmFile in osmFiles)
            {
                if (shouldStop)
                {
                    return;
                }
                Console.WriteLine("Running Scenproc for " + startLong + ", " + startLat + " to " + stopLong + ", " + stopLat + " using file " + osmFile + ". Note: Scenproc windows will be minimized to the taskbar.");
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = scenprocLoc;
                proc.StartInfo.Arguments = "\"" + scriptsDir + @"\" + scenprocScript + "\" /run \"" + osmFile + "\" \"" + EarthConfig.mSceneryFolderTexture + "\"";
                proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                proc.Start();
                Thread.Sleep(500);
                proc.WaitForExit();
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }

            lock (runningTiles)
            {
                runningTiles.Remove(tileStr);
            }
            FreeConsole();
        }
    }
}
