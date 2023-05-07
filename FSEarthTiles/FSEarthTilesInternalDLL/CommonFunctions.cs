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

    public class MaskingPolys
    {
        public List<Way<AutomaticWaterMasking.Point>> coastWaterPolygons;
        public List<Way<AutomaticWaterMasking.Point>> islands;
        public List<Way<AutomaticWaterMasking.Point>> inlandPolygons;
        public string tileName;

        public override string ToString()
        {
            return tileName;
        }
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
                dataPath + "CoastWaterPolys.OSM",
                dataPath + "IslandPolys.OSM",
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

        private static void ClampPixelToImg(AutomaticWaterMasking.Point pixel, Bitmap bmp)
        {
            if (pixel.X < 0)
            {
                pixel.X = 0;
            }
            if (pixel.X >= bmp.Width)
            {
                pixel.X = bmp.Width - 1;
            }
            if (pixel.Y < 0)
            {
                pixel.Y = 0;
            }
            if (pixel.Y >= bmp.Height)
            {
                pixel.Y = bmp.Height - 1;
            }
        }

        private static bool TileAdjacentToWater(double[] tile, double[] checkTile, AutomaticWaterMasking.Point NW, decimal pixelsPerLon, decimal pixelsPerLat, Bitmap bmp)
        {
            // checkTile is to the north
            if (checkTile[0] > tile[0])
            {
                AutomaticWaterMasking.Point pixel = WaterMasking.CoordToPixel((decimal)tile[0], (decimal)tile[1], NW.Y, NW.X, pixelsPerLon, pixelsPerLat);
                pixel.Y--;
                ClampPixelToImg(pixel, bmp);
                return bmp.GetPixel((int)pixel.X, (int)pixel.Y).ToArgb() == Color.Black.ToArgb();
            }
            // checkTile is to the south
            if (checkTile[0] < tile[0])
            {
                AutomaticWaterMasking.Point pixel = WaterMasking.CoordToPixel((decimal)checkTile[0], (decimal)checkTile[1], NW.Y, NW.X, pixelsPerLon, pixelsPerLat);
                pixel.Y++;
                ClampPixelToImg(pixel, bmp);
                return bmp.GetPixel((int)pixel.X, (int)pixel.Y).ToArgb() == Color.Black.ToArgb();
            }
            // checkTile is to the east
            if (checkTile[1] > tile[1])
            {
                AutomaticWaterMasking.Point pixel = WaterMasking.CoordToPixel((decimal)checkTile[0], (decimal)checkTile[1], NW.Y, NW.X, pixelsPerLon, pixelsPerLat);
                pixel.X++;
                ClampPixelToImg(pixel, bmp);
                return bmp.GetPixel((int)pixel.X, (int)pixel.Y).ToArgb() == Color.Black.ToArgb();
            }
            // checkTile is to the west
            if (checkTile[1] < tile[1])
            {
                AutomaticWaterMasking.Point pixel = WaterMasking.CoordToPixel((decimal)tile[0], (decimal)tile[1], NW.Y, NW.X, pixelsPerLon, pixelsPerLat);
                pixel.X--;
                ClampPixelToImg(pixel, bmp);
                return bmp.GetPixel((int)pixel.X, (int)pixel.Y).ToArgb() == Color.Black.ToArgb();
            }

            return false;
        }

        // this is so ugly... TODO: the real solution is to make a Tile class and override appropriate methods
        private static MaskingPolys DictContainsTile(Dictionary<double[], MaskingPolys> tilesDict, double[] tileToCheck)
        {
            MaskingPolys mp = null;
            foreach (KeyValuePair<double[], MaskingPolys> kv in tilesDict)
            {
                double[] tile = kv.Key;
                mp = kv.Value;
                if (tile[0] == tileToCheck[0] && tile[1] == tileToCheck[1])
                {
                    return mp;
                }
            }

            return null;
        }

        // takes a tile which might be all land or all ocean water, and checks adjacent tiles to tell which is the truth
        private static bool AmbiguousTileHasSeaWater(double[] tile, Dictionary<double[], MaskingPolys> tilePolysMap, AutomaticWaterMasking.Point NW, decimal pixelsPerLon, decimal pixelsPerLat, Bitmap bmp)
        {
            MaskingPolys thisTileMaskingPolys = tilePolysMap[tile];
            // by now, coastwater polys are 0. if we have some inland polys, then this tile should be land as a base (white)
            if (thisTileMaskingPolys.inlandPolygons.Count > 1)
            {
                return false;
            }

            // has islands? the base of the tile should be water as a base(black)
            if (thisTileMaskingPolys.islands.Count > 0)
            {
                return true;
            }
            // no coast water polys, no inland polys (think the middle of the dessert), so look at adjacent tiles
            short[] check = { -1, 1 };
            foreach (short x in check)
            {
                double[] checkTile = new double[] { tile[0], tile[1] + x };
                MaskingPolys mp = DictContainsTile(tilePolysMap, checkTile);
                // only use this tile to check for ambiguity if this tile has coast water polygons...
                if (mp != null && mp.coastWaterPolygons.Count > 0)
                {
                    if (TileAdjacentToWater(tile, checkTile, NW, pixelsPerLon, pixelsPerLat, bmp))
                    {
                        return true;
                    }
                }
            }
            foreach (short y in check)
            {
                double[] checkTile = new double[] { tile[0] + y, tile[1] };
                MaskingPolys mp = DictContainsTile(tilePolysMap, checkTile);
                // only use this tile to check for ambiguity if this tile has coast water polygons...
                if (mp != null && mp.coastWaterPolygons.Count > 0)
                {
                    if (TileAdjacentToWater(tile, checkTile, NW, pixelsPerLon, pixelsPerLat, bmp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static List<Way<AutomaticWaterMasking.Point>> GetUniqueInlandWays(List<Way<AutomaticWaterMasking.Point>> allWays)
        {
            Dictionary<int, List<Way<AutomaticWaterMasking.Point>>> uniqueInlandPolysDict = new Dictionary<int, List<Way<AutomaticWaterMasking.Point>>>();
            if (allWays.Count > 0)
            {
                // pre-populate so below loop runs correctly
                List<Way<AutomaticWaterMasking.Point>> polysWithThisCount = new List<Way<AutomaticWaterMasking.Point>>();
                polysWithThisCount.Add(allWays[0]);
                uniqueInlandPolysDict.Add(allWays[0].Count, polysWithThisCount);
                foreach (Way<AutomaticWaterMasking.Point> inlandPoly in allWays)
                {
                    if (uniqueInlandPolysDict.ContainsKey(inlandPoly.Count))
                    {
                        List<Way<AutomaticWaterMasking.Point>> potentialMatches = uniqueInlandPolysDict[inlandPoly.Count];
                        foreach (Way<AutomaticWaterMasking.Point> alreadyThere in potentialMatches)
                        {
                            if (inlandPoly.DeepEquals(alreadyThere))
                            {
                                break;
                            }
                        }

                        // not there
                        potentialMatches.Add(inlandPoly);
                    }
                    else
                    {
                        polysWithThisCount = new List<Way<AutomaticWaterMasking.Point>>();
                        polysWithThisCount.Add(inlandPoly);
                        uniqueInlandPolysDict.Add(inlandPoly.Count, polysWithThisCount);
                    }

                }
            }
            List<Way<AutomaticWaterMasking.Point>> uniqueInlandPolysList = new List<Way<AutomaticWaterMasking.Point>>();
            foreach (KeyValuePair<int, List<Way<AutomaticWaterMasking.Point>>> kv in uniqueInlandPolysDict)
            {
                List<Way<AutomaticWaterMasking.Point>> polys = kv.Value;
                foreach (Way<AutomaticWaterMasking.Point> way in polys)
                {
                    uniqueInlandPolysList.Add(way);
                }
            }

            return uniqueInlandPolysList;
        }

        public static Bitmap DrawWaterMaskBMP(Dictionary<double[], MaskingPolys> allMaskingPolys, int pixelsX, int pixelsY, AutomaticWaterMasking.Point NW, decimal pixelsPerLon, decimal pixelsPerLat, Graphics g=null, Bitmap bmp=null)
        {
            bool createdNewGraphics = false;
            SolidBrush b = null;
            if (bmp == null)
            {
                bmp = new Bitmap(pixelsX, pixelsY, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                g = Graphics.FromImage(bmp);
                g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);
                createdNewGraphics = true;
            }
            Dictionary<double[], MaskingPolys> ambiguousTiles = new Dictionary<double[], MaskingPolys>();
            List<Way<AutomaticWaterMasking.Point>> allInlandPolys = new List<Way<AutomaticWaterMasking.Point>>();
            foreach (KeyValuePair<double[], MaskingPolys> kv in allMaskingPolys)
            {

                MaskingPolys polys = kv.Value;
                double[] tile = kv.Key;
                // first, draw the coast water polygons
                if (polys.coastWaterPolygons.Count > 0)
                {
                    b = new SolidBrush(Color.Black);
                    WaterMasking.DrawPolygons(bmp, g, b, pixelsPerLon, pixelsPerLat, NW, polys.coastWaterPolygons);
                }
                else
                {
                    // but not the ones in tiles with no coast water polygon
                    ambiguousTiles.Add(tile, polys);
                }

                // now draw the islands
                b = new SolidBrush(Color.White);
                WaterMasking.DrawPolygons(bmp, g, b, pixelsPerLon, pixelsPerLat, NW, polys.islands);
                foreach (Way<AutomaticWaterMasking.Point> way in polys.inlandPolygons)
                {
                    allInlandPolys.Add(way);
                }
            }

            List<Way<AutomaticWaterMasking.Point>> uniqueInlandPolys = GetUniqueInlandWays(allInlandPolys);
            // now, draw the layeredpolygons
            WaterMasking.DrawInlandPolys(uniqueInlandPolys, bmp, g, NW, pixelsPerLon, pixelsPerLat);

            // now handle supicious tiles which are potentially in middle of ocean without a coast intersecting viewport or are all land with no water
            foreach (KeyValuePair<double[], MaskingPolys> kv in ambiguousTiles)
            {
                List<Way<AutomaticWaterMasking.Point>> coastWaterPolygons = new List<Way<AutomaticWaterMasking.Point>>();
                double[] tile = kv.Key;
                MaskingPolys polys = kv.Value;
                if (AmbiguousTileHasSeaWater(tile, allMaskingPolys, NW, pixelsPerLon, pixelsPerLat, bmp))
                {
                    // if all water, draw the extent of this tile as all black. if not all water, the extent will already be all white...
                    Way<AutomaticWaterMasking.Point> tileExtent = new Way<AutomaticWaterMasking.Point>();
                    tileExtent.Add(new AutomaticWaterMasking.Point((decimal)tile[1], (decimal)(tile[0] + 1)));
                    tileExtent.Add(new AutomaticWaterMasking.Point((decimal)tile[1] + 1, (decimal)(tile[0] + 1)));
                    tileExtent.Add(new AutomaticWaterMasking.Point((decimal)(tile[1] + 1), (decimal)tile[0]));
                    tileExtent.Add(new AutomaticWaterMasking.Point((decimal)tile[1], (decimal)tile[0]));
                    tileExtent.Add(new AutomaticWaterMasking.Point((decimal)tile[1], (decimal)(tile[0] + 1)));
                    coastWaterPolygons.Add(tileExtent);
                    b = new SolidBrush(Color.Black);
                    WaterMasking.DrawPolygons(bmp, g, b, pixelsPerLon, pixelsPerLat, NW, coastWaterPolygons);
                    // redraw the islands
                    b = new SolidBrush(Color.White);
                    WaterMasking.DrawPolygons(bmp, g, b, pixelsPerLon, pixelsPerLat, NW, polys.islands);
                    // redraw the layered polygons for this tile
                    WaterMasking.DrawInlandPolys(uniqueInlandPolys, bmp, g, NW, pixelsPerLon, pixelsPerLat);
                }
            }

            if (createdNewGraphics)
            {
                g.Dispose();
            }

            return bmp;
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

        public static List<Way<AutomaticWaterMasking.Point>> ReadPolyFile(string polyFilePath)
        {
            List<Way<AutomaticWaterMasking.Point>> polys = new List<Way<AutomaticWaterMasking.Point>>();
            string OSMXML = File.ReadAllText(polyFilePath);
            Dictionary<string, Way<AutomaticWaterMasking.Point>> wayIDsToWays = OSMXMLParser.GetWays(OSMXML, true);

            return new List<Way<AutomaticWaterMasking.Point>>(wayIDsToWays.Values.ToArray());
        }

        // TODO: this layered poly file stuff is ugly. Find a more elegant solution
        public static List<Way<AutomaticWaterMasking.Point>> ReadLayeredPolyFile(string polyFilePath)
        {
            if (File.Exists(polyFilePath))
            {
                string OSMXML = File.ReadAllText(polyFilePath);
                Dictionary<string, Way<AutomaticWaterMasking.Point>> wayIDsToWays = OSMXMLParser.GetWays(OSMXML, true);

                return wayIDsToWays.Values.ToList();
            }

            return null;
        }

        public static Dictionary<double[], MaskingPolys> ReadWaterPolyFiles(double startLong, double stopLong, double startLat, double stopLat, string mWorkFolder)
        {
            List<double[]> tilesDownloaded = GetTilesToDownload(startLong, stopLong, startLat, stopLat);
            Dictionary<double[], MaskingPolys> allPolys = new Dictionary<double[], MaskingPolys>();

            foreach (double[] tile in tilesDownloaded)
            {
                string[] polyPaths = CommonFunctions.GetPolyFilesFullPath(mWorkFolder, tile);
                MaskingPolys mp = new MaskingPolys();
                mp.coastWaterPolygons = CommonFunctions.ReadPolyFile(polyPaths[0]);
                mp.islands = CommonFunctions.ReadPolyFile(polyPaths[1]);
                mp.inlandPolygons = CommonFunctions.ReadLayeredPolyFile(polyPaths[2]);
                mp.tileName = CommonFunctions.GetTileName(tile);
                allPolys.Add(tile, mp);
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

