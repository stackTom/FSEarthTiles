using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutomaticWaterMasking;

namespace FSEarthTilesInternalDLL
{
    public struct tXYCoord
    {
        public Double mX;
        public Double mY;
    }

    public class CommonFunctions
    {
        // probably clever way to do this with format strings to get same resutls as python, but I grew impatient
        private static string GetStringFromDoublWithSignAndPadding(double num, int padding, char toPadWith)
        {
            string sign = "+";
            if (num < 0.0)
            {
                sign = "-";
                num *= -1.0;
            }

            return sign + num.ToString("").PadLeft(padding, toPadWith);
        }

        public static string GetTileName(double[] tile)
        {
            string lat = GetStringFromDoublWithSignAndPadding(tile[0], 2, '0');
            string lon = GetStringFromDoublWithSignAndPadding(tile[1], 3, '0');

            return lat + lon;
        }

        public static string GetTilesPath(string workFolder)
        {
            return workFolder + @"\Tiles";
        }

        public static string GetTilePath(string workFolder, double[] tile)
        {
            string tileName = CommonFunctions.GetTileName(tile);

            return GetTilesPath(workFolder) + @"\" + tileName;
        }

        public static string[] GetPolyFilesFullPath(string workFolder, double[] tile)
        {
            string tileName = CommonFunctions.GetTileName(tile);
            string dataPath = GetTilePath(workFolder, tile) + @"\";
            string[] paths = new string[]
            {
                dataPath + "CoastPolys.OSM",
                dataPath + "InlandWaterPolys.OSM",
                dataPath + "InlandPolys.OSM",
            };

            return paths;
        }

        public static List<double[]> GetTilesToDownload(double startLong, double stopLong, double startLat, double stopLat)
        {
            List<double[]> tilesToDownload = new List<double[]>();
            int startLongInt = (int)Math.Floor(startLong);
            int startLatInt = (int)Math.Floor(startLat);

            double _stopLong = Math.Floor(stopLong);
            double _stopLat = Math.Floor(stopLat);

            int stopLongInt = (int)_stopLong;
            int stopLatInt = (int)_stopLat;

            int latOffset = _stopLat == stopLat ? 0 : 1;
            int longOffset = _stopLong == stopLong ? 0 : 1;

            for (int i = startLatInt; i < stopLatInt + latOffset; i++)
            {
                for (int j = startLongInt; j < stopLongInt + longOffset; j++)
                {
                    double[] tile = new double[2];
                    tile[0] = i;
                    tile[1] = j;
                    tilesToDownload.Add(tile);
                }
            }

            return tilesToDownload;
        }

        // port from NumSharp. Can't just install this lib because .Net 4.0 is too old :c
        public static double[] Convolve(double[] arr1, double[] arr2, string mode = "full")
        {
            int nf = arr1.Length;
            int ng = arr2.Length;

            double[] numSharpReturn = null;

            double[] np1 = arr1;
            double[] np2 = arr2;

            switch (mode)
            {
                case "full":
                    {
                        int n = nf + ng - 1;

                        var outArray = new double[n];

                        for (int idx = 0; idx < n; ++idx)
                        {
                            int jmn = (idx >= ng - 1) ? (idx - (ng - 1)) : 0;
                            int jmx = (idx < nf - 1) ? idx : nf - 1;

                            for (int jdx = jmn; jdx <= jmx; ++jdx)
                            {
                                outArray[idx] += (np1[jdx] * np2[idx - jdx]);
                            }
                        }

                        numSharpReturn = outArray;

                        break;
                    }
                case "valid":
                    {
                        var min_v = (nf < ng) ? np1 : np2;
                        var max_v = (nf < ng) ? np2 : np1;

                        int n = Math.Max(nf, ng) - Math.Min(nf, ng) + 1;

                        double[] outArray = new double[n];

                        for (int idx = 0; idx < n; ++idx)
                        {
                            int kdx = idx;

                            for (int jdx = (min_v.Length - 1); jdx >= 0; --jdx)
                            {
                                outArray[idx] += min_v[jdx] * max_v[kdx];
                                ++kdx;
                            }
                        }

                        numSharpReturn = outArray;

                        break;
                    }
                case "same":
                    {
                        // followed the discussion on
                        // https://stackoverflow.com/questions/38194270/matlab-convolution-same-to-numpy-convolve
                        // implemented numpy convolve because we follow numpy
                        var npad = arr2.Length - 1;

                        double[] np1New = null;

                        if (npad % 2 == 1)
                        {
                            npad = (int)Math.Floor(((double)npad) / 2.0);

                            np1New = (double[])np1.Clone();

                            np1New.ToList().AddRange(new double[npad + 1]);
                            var puffer = (new double[npad]).ToList();
                            puffer.AddRange(np1New);
                            np1New = puffer.ToArray();
                        }
                        else
                        {
                            npad = npad / 2;

                            np1New = (double[])np1.Clone();

                            var puffer = np1New.ToList();
                            puffer.AddRange(new double[npad]);
                            np1New = puffer.ToArray();

                            puffer = (new double[npad]).ToList();
                            puffer.AddRange(np1New);
                            np1New = puffer.ToArray();
                        }


                        numSharpReturn = Convolve(np1New, arr2, "valid");
                        break;
                    }

            }

            return numSharpReturn;
        }

        // port from NumSharp. Can't just install this lib because .Net 4.0 is too old :c
        public static double[][] Transpose(double[][] arr)
        {
            double[][] nd = new double[arr[0].Length][];

            for (int idx = 0; idx < nd.Length; idx++)
            {
                nd[idx] = new double[arr.Length];
                for (int jdx = 0; jdx < nd[idx].Length; jdx++)
                {
                    nd[idx][jdx] = arr[jdx][idx];
                }
            }

            return nd;
        }

        public static List<List<double[]>> GetPiecesFromGrid(double startX, double stopX, double startY, double stopY, double OFFSET)
        {
            double minX = startX;
            double maxX = startX;
            double minY = startY;
            double maxY = startY;

            List<List<double[]>> pieces = new List<List<double[]>>();

            while (maxX < stopX)
            {
                List<double[]> slice = new List<double[]>();
                maxX += OFFSET;
                if (maxX > stopX)
                {
                    maxX = stopX;
                }
                while (maxY < stopY)
                {
                    maxY += OFFSET;
                    if (maxY > stopY)
                    {
                        maxY = stopY;
                    }

                    slice.Add(new double[] { minX, minY, maxX, maxY });

                    minY = maxY;
                }
                minX = maxX;
                minY = startY;
                maxY = startY;
                pieces.Add(slice);
            }

            return pieces;
        }

        public static bool BitmapAllBlack(Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = data.Stride;
            bool allBlack = true;
            const uint BLACK_32BIT_VAL = 4278190080;
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < bmp.Height && allBlack; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        uint c = *((uint*)&ptr[(x * 4) + y * stride]); // red value at this point (proxy for whiteness)

                        if (c != BLACK_32BIT_VAL)
                        {
                            allBlack = false;
                            break;
                        }
                    }
                }
            }
            bmp.UnlockBits(data);

            return allBlack;
        }

        // other code is based on the fact that startLat < stopLong and
        // startLong < stopLong. This function takes care of that
        public static void SetStartAndStopCoords(ref double startLat, ref double startLong, ref double stopLat, ref double stopLong)
        {
            if (startLat > stopLat)
            {
                double temp = startLat;
                startLat = stopLat;
                stopLat = temp;
            }
            if (startLong > stopLong)
            {
                double temp = startLong;
                startLong = stopLong;
                stopLong = temp;
            }
        }

        public static tXYCoord ConvertXYLatLongToPixel( tXYCoord iXYCoord, Double startLat, Double startLong, Double vPixelPerLongitude, Double vPixelPerLatitude)
        {
            tXYCoord vPixelXYCoord;

            vPixelXYCoord.mX = vPixelPerLongitude * (iXYCoord.mX - startLong);
            vPixelXYCoord.mY = vPixelPerLatitude * (startLat - iXYCoord.mY);

            return vPixelXYCoord;
        }

        public static tXYCoord CoordToPixel(double lat, double longi, int mAreaPixelCountInX, int mAreaPixelCountInY,
                                     double mAreaNWCornerLatitude, double mAreaNWCornerLongitude, Double vPixelPerLongitude,
                                     Double vPixelPerLatitude)
        {
            tXYCoord tempCoord;
            tempCoord.mX = longi;
            tempCoord.mY = lat;
            tXYCoord pixel = CommonFunctions.ConvertXYLatLongToPixel(tempCoord, mAreaNWCornerLatitude, mAreaNWCornerLongitude, vPixelPerLongitude, vPixelPerLatitude);
            pixel.mX -= 0.5f;
            pixel.mY -= 0.5f;

            return pixel;
        }

        private static PointF PointToPointF(AutomaticWaterMasking.Point p)
        {
            PointF ret = new PointF((float)p.X, (float)p.Y);

            return ret;
        }

        public static List<PointF[]> ReadPolyFile(string polyFilePath)
        {
            List<PointF[]> polys = new List<PointF[]>();
            string OSMXML = File.ReadAllText(polyFilePath);
            Dictionary<string, Way<AutomaticWaterMasking.Point>> wayIDsToWays = AreaKMLFromOSMDataCreator.GetWays(OSMXML, true);
            foreach (KeyValuePair<string, Way<AutomaticWaterMasking.Point>> kv in wayIDsToWays)
            {
                Way<AutomaticWaterMasking.Point> way = kv.Value;
                PointF[] points = new PointF[way.Count];
                int i = 0;
                foreach (AutomaticWaterMasking.Point p in way)
                {
                    points[i] = PointToPointF(p);
                    i++;
                }
            }

            return polys;
        }


        public static List<PointF[]> ReadWaterPolyFiles(double startLong, double stopLong, double startLat, double stopLat, string mWorkFolder)
        {
            List<double[]> tilesDownloaded = GetTilesToDownload(startLong, stopLong, startLat, stopLat);
            List<PointF[]> allPolys = new List<PointF[]>();

            foreach (double[] tile in tilesDownloaded)
            {
                string[] polyPaths = CommonFunctions.GetPolyFilesFullPath(mWorkFolder, tile);
                foreach (string polyPath in polyPaths)
                {
                    List<PointF[]> polys = ReadPolyFile(polyPath);
                    allPolys.AddRange(polys);
                }
            }

            return allPolys;
        }

        public static void FixTLS()
        {
            // this old .net framework is missing some of the SecurityProcolType's
            // this is hack to get them. needed, otherwise certain https url's throw
            // exception and don't work
            const SecurityProtocolType tls13 = (SecurityProtocolType)12288;
            const SecurityProtocolType tls12 = (SecurityProtocolType)3072;
            const SecurityProtocolType tls11 = (SecurityProtocolType)768;
            const SecurityProtocolType tls = (SecurityProtocolType)192;
            const SecurityProtocolType ssl3 = (SecurityProtocolType)48;
            ServicePointManager.SecurityProtocol = tls13 | tls12 | tls11 | tls | ssl3;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }
    }
}

