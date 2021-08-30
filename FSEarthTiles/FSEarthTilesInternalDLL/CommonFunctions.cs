using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSEarthTilesInternalDLL
{
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

        public static string GetMeshFileFullPath(string workFolder, double[] tile)
        {
            string tileName = CommonFunctions.GetTileName(tile);

            return GetTilePath(workFolder, tile) + @"\Data" + tileName + ".mesh";
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
    }
}

