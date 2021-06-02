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

        public static string GetTileFolderName(double[] tile)
        {
            string lat = GetStringFromDoublWithSignAndPadding(tile[0], 2, '0');
            string lon = GetStringFromDoublWithSignAndPadding(tile[1], 3, '0');

            return lat + lon;
        }

        public static List<double[]> GetTilesToDownload(double startLong, double stopLong, double startLat, double stopLat)
        {
            List<double[]> tilesToDownload = new List<double[]>();
            int minLong = (int)Math.Floor(startLong);
            int maxLong = (int)Math.Floor(stopLong);
            int minLat = (int)Math.Floor(stopLat);
            int maxLat = (int)Math.Floor(startLat);

            for (int i = minLat; i < maxLat + 1; i++)
            {
                for (int j = minLong; j < maxLong + 1; j++)
                {
                    double[] tile = new double[2];
                    tile[0] = i;
                    tile[1] = j;
                    tilesToDownload.Add(tile);
                }
            }

            return tilesToDownload;
        }
    }
}
