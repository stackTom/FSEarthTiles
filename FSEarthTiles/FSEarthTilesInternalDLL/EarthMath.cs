using System;
using System.Collections.Generic;
using System.Text;


//             Mercator Projection a Map projection used by Earth Services
//-------------------------------------------------------------------------------------
//
// While the Longitude is stright forward mapped (1:1) 
// the Latitude Angle Phi is distorted / transformed into a flat Map with the Marcator-Projection
//
// Mercator-Projection Formula
// check: http://en.wikipedia.org/wiki/Mercator_projection
//
// y   = atanh(sin(Phi));  or  y   = 0.5*ln((1+sin(Phi))/(1-sin(Phi))); or  y= asinh(tan(Phi)); or y=ln(tan(0.5*Pi+0.5*Phi));
// Phi = atan(sinh(y));    or  Phi = 2.0 * arctan (e^y) - 0.5*Pi;
//
//  .. and With Cutting/Norming:
//
// y   = atanh(sin(Phi)) / yMax;
// Phi = atan(sinh(y * yMax));
//
// The YMax in this Mercator Projection is Pi!
//
// The calculated MercatorProjectionCut Value is then = 180*atan(sinh(pi))/pi;
// That's: 85.05112877980659 [grad] Latitude
// The poles are missing
//------------------------------------------------------------------------------------------



//             FS2004 and LOD13 
//----------------------------------------------
//
//  FS LOD's and Tile Levels is something different. While the Service-Tiles are projected through the Mercator projection
//  the FlightSimulators Tiles are more or less projected 1:1. Unfortunatly the FS2004 persists in a LOD13 (4.8meter/pix) feed   
//  and creats water gaps if you dont fee complete LOD13 tiles. For FSX this is no issue.
//
// From the FS2004 SDK docu (resampler):
// In FS2004, go to the Scenery Library and Add this area 
// The ids of the LOD13 quadtree cells inside the corners of the scenery 
// area are:
// Northwest: U=6889 V=4269
// Southeast: U=6891 V=4272
// The lat/lon bounds in degrees of the scenery area are:
// North =   90 -     4269 *  (90 / 2^13) =  43.099365234375000
// South =   90 - (4272+1) *  (90 / 2^13) =  43.055419921875000
// East  = -180 +     6889 * (120 / 2^13) = -79.086914062500000
// West  = -180 + (6891+1) * (120 / 2^13) = -79.042968750000000
//-------------------------------------------------------------


//             Lanczos3 
//----------------------------------------------------------------------
// Formula is wight(x) = (sin(pi*x) / (pi*x)) * (sin(pi*x/3) / (pi*x/3))
//----------------------------------------------------------------------


namespace FSEarthTilesInternalDLL
{
    public class EarthMath
    {

        //constants
        public const Double cPi                  = Math.PI;                             //3.1415926535897932384626433832795;
        public const Double cEarthCircumference  = 40075000;                            //[meter]
        public const Double cEarthRadius         = cEarthCircumference / (2.0 * cPi);   //[meter]
        public const Double cDegreeToRadFactor   = cPi / 180.0;                         //[rad/Grad]
        public const Double cRadToDegreeFactor   = 180 / cPi;                           //[Grad/rad]
        public const Double cnmToMeterFactor     = 1852.0;                              //[meter/nm] nm=nautical miles
        public const Double cMeterTonmFactor     = 1.0 / cnmToMeterFactor;              //[nm/meter]
        public const Double cInv360Grad          = 1.0 / 360.0;                         //[1/Grad]

        //AreaCode
        public const Int64  cLevel0CodeDeep          = 18;
        public const Int64  cLevelNegativeCodeDeep   =  4; // means -4


        public static void Initialize() //Has to be called first before you can use the class
        {
            mMercatorProjectionCut     = cRadToDegreeFactor * Math.Atan(Math.Sinh(cPi));
            mMercatorYNormingMax       = 0.5 * Math.Log((1.0 + Math.Sin(cDegreeToRadFactor * mMercatorProjectionCut)) / (1.0 - Math.Sin(cDegreeToRadFactor * mMercatorProjectionCut)));
            mMercatorYNormingMaxInvert = 1.0 / mMercatorYNormingMax;
           
            mAreaCodeSizeTableInt64  = new Int64 [cLevel0CodeDeep + 1];
            mAreaCodeSizeTableDouble = new Double[cLevel0CodeDeep + 1];

            mAreaCodeSizeNegativeLevelTableInt64  = new Int64[cLevelNegativeCodeDeep + 1];
            mAreaCodeSizeNegativeLevelTableDouble = new Double[cLevelNegativeCodeDeep + 1];

            Int64 vTableValue = 1;

            for (Int64 vCon=cLevel0CodeDeep; vCon>=0; vCon--)
            {
                mAreaCodeSizeTableInt64[vCon]  = vTableValue;
                mAreaCodeSizeTableDouble[vCon] = Convert.ToDouble(vTableValue);
                vTableValue <<= 1; //* 2
            }

            vTableValue >>= 1; //  shift one back (div 2) to have equal count in area index 0 = level 0
                               // and we continue with the negative zoom/resolution levels
            for (Int64 vCon = 0; vCon <= cLevelNegativeCodeDeep; vCon++)
            {
                mAreaCodeSizeNegativeLevelTableInt64[vCon]  = vTableValue;
                mAreaCodeSizeNegativeLevelTableDouble[vCon] = Convert.ToDouble(vTableValue);
                vTableValue <<= 1; //* 2
            }


            //Initialize LOD13
            mLOD13LatitudeResolution = (90.0 / Math.Pow(2.0, 13.0)); //  90 / 2^13
            mLOD13LongitudeResolution = (120.0 / Math.Pow(2.0, 13.0)); // 120 / 2^13
            mInvLOD13LatitudeResolution = 1.0 / mLOD13LatitudeResolution;
            mInvLOD13LongitudeResolution = 1.0 / mLOD13LongitudeResolution;

            //Initialize LatLong
            mLatLongLatitudeResolution = 1.0 / 60.0;    //[Grad]  // 1 minute
            mLatLongLongitudeResolution = 1.0 / 60.0;    //[Grad]  
            mInvLatLongLatitudeResolution = 60.0;          //[1/Grad]
            mInvLatLongLongitudeResolution = 60.0;          //[1/Grad]

        }


        public static Int64 GetAreaCodeSize(Int64 iLevel)
        {
            Int64 vAreaCodeSize;
            if (iLevel >= 0)
            {
                vAreaCodeSize = mAreaCodeSizeTableInt64[iLevel];
            }
            else
            {
                vAreaCodeSize = mAreaCodeSizeNegativeLevelTableInt64[-iLevel];
            }
            return vAreaCodeSize;
        }

        private static Double GetAreaCodeSizeInDouble(Int64 iLevel)
        {
            Double vAreaCodeSize;
            if (iLevel >= 0)
            {
                vAreaCodeSize = mAreaCodeSizeTableDouble[iLevel];
            }
            else
            {
                vAreaCodeSize = mAreaCodeSizeNegativeLevelTableDouble[-iLevel];
            }
            return vAreaCodeSize;
        }

        //Coord convertings
        public static Double GetAreaTileLeftLongitude(Int64 iAreaCodeX, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vLongitude    = 360.0 * Convert.ToDouble(iAreaCodeX) / vAreaCodeSize - 180.0;
            return vLongitude;
        }

        public static Double GetAreaTileRightLongitude(Int64 iAreaCodeX, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vLongitude = 360.0 * Convert.ToDouble(iAreaCodeX + 1) / vAreaCodeSize - 180.0;
            return vLongitude;
        }

        public static Double GetLongitudePerPixel(Int64 iLevel)
        {
            Double vAreaCodeSize      = GetAreaCodeSizeInDouble(iLevel);
            Double vLongitudePerPixel = 360.0 / (256.0 * vAreaCodeSize);
            return vLongitudePerPixel;
        }

        public static Double GetNormedMercatorYPerPixel(Int64 iLevel)
        {
            Double vAreaCodeSize            = GetAreaCodeSizeInDouble(iLevel);
            Double vNormedMercatorYPerPixel = 2.0 / (256.0 * vAreaCodeSize);
            return vNormedMercatorYPerPixel;
        }

        public static Double GetPixelPerLongitude(Int64 iLevel)
        {
            Double vAreaCodeSize      = GetAreaCodeSizeInDouble(iLevel);
            Double vPixelPerLongitude = (256.0 * vAreaCodeSize) / 360.0;
            return vPixelPerLongitude;
        }

        public static Double GetPixelPerNormedMercatorY(Int64 iLevel)
        {
            Double vAreaCodeSize            = GetAreaCodeSizeInDouble(iLevel);
            Double vPixelPerNormedMercatorY = (256.0 * vAreaCodeSize) / 2.0;
            return vPixelPerNormedMercatorY;
        }

        public static Double GetAreaTileTopLatitude(Int64 iAreaCodeY, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vMercatorYMap = 2.0 * Convert.ToDouble(iAreaCodeY) / vAreaCodeSize;
            Double vMercatorYOnSpot = 1.0 - vMercatorYMap;
            Double vLatitude = GetLatitudeFromNormedMercatorY(vMercatorYOnSpot);
            return vLatitude;
        }

        public static Double GetAreaTileBottomLatitude(Int64 iAreaCodeY, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vMercatorYMap = 2.0 * Convert.ToDouble(iAreaCodeY + 1) / vAreaCodeSize;
            Double vMercatorYOnSpot = 1.0 - vMercatorYMap;
            Double vLatitude = GetLatitudeFromNormedMercatorY(vMercatorYOnSpot);
            return vLatitude;
        }



        public static Int64 GetAreaCodeX(Double iLongitude, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vLongAreaAbs = iLongitude + 180.0;
            //vLongAreaAbs goes to the right now from 0 to +360.0
            Double vBrokenValue = vAreaCodeSize * vLongAreaAbs * cInv360Grad;
            Int64 vAreaCodeX = Convert.ToInt64(Math.Truncate(vBrokenValue)); //downCut
            return vAreaCodeX;
        }


        public static Double GetNormedMercatorY(Double iLatitude)
        {
            // limes to 90.0 transforms to infinit very fast therefore we need to cut safe
            Double vNormedMercatorY;
            Double vSafeLatitude = iLatitude;
            if (vSafeLatitude > 89.0)
            {
                vSafeLatitude = 89.0;
            }
            if (vSafeLatitude < -89.0)
            {
                vSafeLatitude = -89.0;
            }
            vNormedMercatorY = mMercatorYNormingMaxInvert * 0.5 * Math.Log((1.0 + Math.Sin(cDegreeToRadFactor * vSafeLatitude)) / (1.0 - Math.Sin(cDegreeToRadFactor * vSafeLatitude)));

            return vNormedMercatorY;
        }

        public static Double GetLatitudeFromNormedMercatorY(Double vNormedMercatorY)
        {
            Double vLatitude = cRadToDegreeFactor * Math.Atan(Math.Sinh(vNormedMercatorY * mMercatorYNormingMax));
            return vLatitude;
        }

        public static Int64 GetAreaCodeY(Double iLatitude, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            //vMercatorYOnSpot is within +/-1.0, +90.0Grad -> +1.0 / -90.0Grad -> -1.0
            Double vMercatorYOnSpot = GetNormedMercatorY(iLatitude);
            Double vMercatorYMap = -vMercatorYOnSpot + 1.0;
            //Note sign turn!
            //vMercatorYMap goes downward now from 0 to +2.0
            Double vBrokenValue = vAreaCodeSize * vMercatorYMap * 0.5;
            Int64 vAreaCodeY = Convert.ToInt64(Math.Truncate(vBrokenValue)); //downCut
            return vAreaCodeY;
        }

        public static Int64 GetPixelPosWithinTileX(Double iLongitude, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            Double vLongAreaAbs = iLongitude + 180.0;
            //vLongAreaAbs goes to the right now from 0 to +360.0
            Double vBrokenValue = vAreaCodeSize * vLongAreaAbs * cInv360Grad;
            Double vBrokenValueTrancuted = Math.Truncate(vBrokenValue);
            Int64 vAreaCodeX = Convert.ToInt64(vBrokenValueTrancuted); //downCut
            Int64 vPixelPosWithinTileX = Convert.ToInt64(Math.Truncate(256.0 * (vBrokenValue - vBrokenValueTrancuted)));
            return vPixelPosWithinTileX;
        }


        public static Int64 GetPixelPosWithinTileY(Double iLatitude, Int64 iLevel)
        {
            Double vAreaCodeSize = GetAreaCodeSizeInDouble(iLevel);
            //vMercatorYOnSpot is within +/-1.0, +90.0Grad -> +1.0 / -90.0Grad -> -1.0
            Double vMercatorYOnSpot = GetNormedMercatorY(iLatitude);
            Double vMercatorYMap = -vMercatorYOnSpot + 1.0;
            //Note sign turn!
            //vMercatorYMap goes downward now from 0 to +2.0
            Double vBrokenValue = vAreaCodeSize * vMercatorYMap * 0.5;
            Double vBrokenValueTrancuted = Math.Truncate(vBrokenValue);
            Int64 vAreaCodeY = Convert.ToInt64(vBrokenValueTrancuted);
            Int64 vPixelPosWithinTileY = Convert.ToInt64(Math.Truncate(256.0 * (vBrokenValue - vBrokenValueTrancuted)));
            return vPixelPosWithinTileY;
        }

        public static Double GetXPixelDistance(Double iFromLongitude, Double iToLongitude, Int64 iZoomLevel)
        {
            //Use with uncleaned longitude coord
            Double vPixelPerLogitude = EarthMath.GetPixelPerLongitude(iZoomLevel);
            Double vXPixelDistance = (iToLongitude - iFromLongitude) * vPixelPerLogitude;
            return vXPixelDistance;
        }

        public static Double GetYPixelDistance(Double iFromLatitude, Double iToLatitude, Int64 iZoomLevel)
        {
            Double vPixelPerNormedMercatorY = EarthMath.GetPixelPerNormedMercatorY(iZoomLevel);
            Double vFromLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(iFromLatitude);
            Double vToLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(iToLatitude);
            Double vYPixelDistance = (vToLatitudeNormedMercatorY - vFromLatitudeNormedMercatorY) * vPixelPerNormedMercatorY;
            return vYPixelDistance;
        }

        public static Double GetLongitudeFromLongitudeAndPixel(Double iFromLongitude, Double iPixelDistX, Int64 iZoomLevel)
        {
            //returns uncleaned longitude coord
            Double vLogitudePerPixel = EarthMath.GetLongitudePerPixel(iZoomLevel);
            Double vLogitude = iFromLongitude + vLogitudePerPixel * iPixelDistX;
            return vLogitude;
        }

        public static Double GetLatitudeFromLatitudeAndPixel(Double iFromLatitude, Double iPixelDistY, Int64 iZoomLevel)
        {
            Double vNormedMercatorYPerPixel = EarthMath.GetNormedMercatorYPerPixel(iZoomLevel);
            Double vFromLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(iFromLatitude);
            Double vToLatitudeNormedMercatorY = vFromLatitudeNormedMercatorY + vNormedMercatorYPerPixel * iPixelDistY;
            Double vLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vToLatitudeNormedMercatorY);
            return vLatitude;
        }

        public static Double GetToAHunderThSecRoundedWorldCoord(Double iWorldCoord)
        {
            Int64  vWorldCoordHunerdOfSec = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Double vRoundedWorldCoord;
            if (iWorldCoord >= 0.0)
            {
                 vRoundedWorldCoord = (1.0/360000.0) * (Double)(vWorldCoordHunerdOfSec);
            }
            else
            {
                 vRoundedWorldCoord = - (1.0/360000.0) * (Double)(vWorldCoordHunerdOfSec);
            }
            return vRoundedWorldCoord;
        }

        public static Int64 RoundWorldCoordToAHunderThSec(Double iWorldCoord)
        {
            Int64 vWorldCoordHunerdOfSec = Convert.ToInt64(Math.Round(360000.0 * Math.Abs(iWorldCoord)));
            return vWorldCoordHunerdOfSec;
        }


        public static String GetWorldCoordGrad(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Int64 vGrad = vRoundedWorldCoord / 360000;
            return Convert.ToString(vGrad);
            //return vGrad.ToString("00");
        }

        public static String GetWorldCoordGradPadded(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Int64 vGrad = vRoundedWorldCoord / 360000;
            //return Convert.ToString(vGrad);
            return vGrad.ToString("00");
        }

        public static String GetWorldCoordMinutePadded(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Int64 vMin = (vRoundedWorldCoord % 360000) / 6000;
            //return Convert.ToString(vMin);
            return vMin.ToString("00.00");
        }

        public static String GetWorldCoordMinute(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Int64 vMin = (vRoundedWorldCoord % 360000) / 6000;
            return Convert.ToString(vMin);
            //return vMin.ToString("00.00");
        }


        public static Double GetWorldCoordSecInDouble(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Double vSec = 0.01 * Convert.ToDouble(vRoundedWorldCoord % 6000);
            return vSec;

        }

        public static String GetWorldCoordSec(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Double vSec = 0.01 * Convert.ToDouble(vRoundedWorldCoord % 6000);
            return Convert.ToString(vSec);
            //return vSec.ToString("00.00");
        }

        public static String GetWorldCoordSecPadded(Double iWorldCoord)
        {
            Int64 vRoundedWorldCoord = RoundWorldCoordToAHunderThSec(iWorldCoord);
            Double vSec = 0.01 * Convert.ToDouble(vRoundedWorldCoord % 6000);
            //return Convert.ToString(vSec);
            return vSec.ToString("00.00");
        }

        public static String GetSignLatitude(Double iLatitude)
        {
            if (iLatitude >= 0.0)
            {
                return ("N");
            }
            else
            {
                return ("S");
            }
        }


        public static String GetSignLongitude(Double iLongitude)
        {
            if (iLongitude >= 0.0)
            {
                return ("E");
            }
            else
            {
                return ("W");
            }
        }

        public static Double LimitLatitude(Double iLatitude)
        {
            Double vLimitedLatitude = iLatitude;
            if (iLatitude > mMercatorProjectionCut)
            {
                vLimitedLatitude = mMercatorProjectionCut;
            }
            if (iLatitude < -mMercatorProjectionCut)
            {
                vLimitedLatitude = -mMercatorProjectionCut;
            }
            return vLimitedLatitude;
        }

        public static Double LimitLongitude(Double iLongitude)
        {
            Double vLimitedLongitude = iLongitude;
            if (iLongitude > 180.0)
            {
                vLimitedLongitude = 180.0;
            }
            if (iLongitude < -180.0)
            {
                vLimitedLongitude = -180.0;
            }
            return vLimitedLongitude;
        }

        public static Double CleanLatitude(Double iLatitude)
        {
            Double vCleanedLatitude = iLatitude;
            if (iLatitude > 90.0)
            {
                vCleanedLatitude = 90.0;
            }
            if (iLatitude < -90.0)
            {
                vCleanedLatitude = -90.0;
            }
            return vCleanedLatitude;
        }

        public static Double CleanLongitude(Double iLongitude)
        {
            Double vCleanedLongitude = iLongitude;
            if (iLongitude > 180.0)
            {
                vCleanedLongitude = iLongitude - 360.0;
            }
            if (iLongitude < -180.0)
            {
                vCleanedLongitude = iLongitude + 360.0;
            }
            return vCleanedLongitude;
        }

        public static Double Sinc(Double iX)
        {
            const Double cSincEpsilon = 1e-12;
            Double vY;
            if ((iX >= -cSincEpsilon) && (iX <= cSincEpsilon))
            {
                vY = 1.0;
            }
            else
            {
                vY = Math.Sin(EarthMath.cPi * iX) / (EarthMath.cPi * iX);
            }
            return vY;
        }


        public static Double Lanczos3(Double iX)
        {
            const Double cLanczosNr = 3.0;
            const Double cInvAThird = 1.0 / cLanczosNr;
            Double vY;
            if ((iX >= cLanczosNr) || (iX <= -cLanczosNr))
            {
                vY = 0.0;
            }
            else
            {
                vY = Sinc(iX) * Sinc(cInvAThird * iX);
            }
            return vY;
        }


        public static Double LOD13LatitudeResolution       { get { return mLOD13LatitudeResolution; } }
        public static Double LOD13LongitudeResolution      { get { return mLOD13LongitudeResolution; } }
        public static Double InvLOD13LatitudeResolution    { get { return mInvLOD13LatitudeResolution; } }
        public static Double InvLOD13LongitudeResolution   { get { return mInvLOD13LongitudeResolution; } }
        public static Double LatLongLatitudeResolution     { get { return mLatLongLatitudeResolution; } }
        public static Double LatLongLongitudeResolution    { get { return mLatLongLongitudeResolution; } }
        public static Double InvLatLongLatitudeResolution  { get { return mInvLatLongLatitudeResolution; } }
        public static Double InvLatLongLongitudeResolution { get { return mInvLatLongLongitudeResolution; } }

        public static Double MercatorProjectionCut { get { return mMercatorProjectionCut; } }

        //Mercator Projection
        private static Double   mMercatorProjectionCut; //[grad]
        private static Double   mMercatorYNormingMax;
        private static Double   mMercatorYNormingMaxInvert;
        private static Double[] mAreaCodeSizeTableDouble;
        private static Int64 [] mAreaCodeSizeTableInt64;
        private static Double[] mAreaCodeSizeNegativeLevelTableDouble;
        private static Int64 [] mAreaCodeSizeNegativeLevelTableInt64;  
  
        //LOD13
        private static Double mLOD13LatitudeResolution;  //[Grad]
        private static Double mLOD13LongitudeResolution; //[Grad]
        private static Double mInvLOD13LatitudeResolution;  //[1/Grad]
        private static Double mInvLOD13LongitudeResolution; //[1/Grad]

        //LatLong
        private static Double mLatLongLatitudeResolution;     //[Grad]  //The Area Snap LatLong mode grid
        private static Double mLatLongLongitudeResolution;    //[Grad]  //will be set to 1 minute
        private static Double mInvLatLongLatitudeResolution;  //[1/Grad]
        private static Double mInvLatLongLongitudeResolution; //[1/Grad]

    }


 


        public class GeoUTMConverter
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Zone { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public Hemisphere Hemi { get; set; }

            private double pi = 3.14159265358979;
            private double sm_a = 6378137.0;
            private double sm_b = 6356752.314;
            private double sm_EccSquared = 6.69437999013e-03;
            private double UTMScaleFactor = 0.9996;

            public enum Hemisphere
            {
                Northern = 0,
                Southern = 1
            }

            public GeoUTMConverter()
            {

            }

            public void ToUTM(double Latitude, double Longitude)
            {
                this.Latitude = Latitude;
                this.Longitude = Longitude;

                Zone = Math.Floor((Longitude + 180.0) / 6) + 1;
                GeoUTMConverterXY(DegToRad(Latitude), DegToRad(Longitude), Zone);
            }

            public void ToLatLon(double X, double Y, int zone, Hemisphere Hemi)
            {
                this.X = X;
                this.Y = Y;

                this.Zone = zone;

                if (Hemi == Hemisphere.Northern)
                {
                    UTMXYToLatLon(X, Y, false);
                }
                else
                {
                    UTMXYToLatLon(X, Y, true);
                }
            }

            private double DegToRad(double degrees)
            {
                return (degrees / 180.0 * pi);
            }

            private double RadToDeg(double radians)
            {
                return (radians / pi * 180.0);
            }

            private double MetersToFeet(double meters)
            {
                return (meters * 3.28084);
            }

            private double FeetToMeters(double feet)
            {
                return (feet / 3.28084);
            }

            private double ArcLengthOfMeridian(double phi)
            {
                double alpha, beta, gamma, delta, epsilon, n;
                double result;

                /* Precalculate n */
                n = (sm_a - sm_b) / (sm_a + sm_b);

                /* Precalculate alpha */
                alpha = ((sm_a + sm_b) / 2.0)
                   * (1.0 + (Math.Pow(n, 2.0) / 4.0) + (Math.Pow(n, 4.0) / 64.0));

                /* Precalculate beta */
                beta = (-3.0 * n / 2.0) + (9.0 * Math.Pow(n, 3.0) / 16.0)
                   + (-3.0 * Math.Pow(n, 5.0) / 32.0);

                /* Precalculate gamma */
                gamma = (15.0 * Math.Pow(n, 2.0) / 16.0)
                    + (-15.0 * Math.Pow(n, 4.0) / 32.0);

                /* Precalculate delta */
                delta = (-35.0 * Math.Pow(n, 3.0) / 48.0)
                    + (105.0 * Math.Pow(n, 5.0) / 256.0);

                /* Precalculate epsilon */
                epsilon = (315.0 * Math.Pow(n, 4.0) / 512.0);

                /* Now calculate the sum of the series and return */
                result = alpha
                    * (phi + (beta * Math.Sin(2.0 * phi))
                        + (gamma * Math.Sin(4.0 * phi))
                        + (delta * Math.Sin(6.0 * phi))
                        + (epsilon * Math.Sin(8.0 * phi)));

                return result;

            }

            private double UTMCentralMeridian(double zone)
            {
                return (DegToRad(-183.0 + (zone * 6.0)));
            }

            private double FootpointLatitude(double y)
            {
                double y_, alpha_, beta_, gamma_, delta_, epsilon_, n;
                double result;

                /* Precalculate n (Eq. 10.18) */
                n = (sm_a - sm_b) / (sm_a + sm_b);

                /* Precalculate alpha_ (Eq. 10.22) */
                /* (Same as alpha in Eq. 10.17) */
                alpha_ = ((sm_a + sm_b) / 2.0)
                    * (1 + (Math.Pow(n, 2.0) / 4) + (Math.Pow(n, 4.0) / 64));

                /* Precalculate y_ (Eq. 10.23) */
                y_ = y / alpha_;

                /* Precalculate beta_ (Eq. 10.22) */
                beta_ = (3.0 * n / 2.0) + (-27.0 * Math.Pow(n, 3.0) / 32.0)
                    + (269.0 * Math.Pow(n, 5.0) / 512.0);

                /* Precalculate gamma_ (Eq. 10.22) */
                gamma_ = (21.0 * Math.Pow(n, 2.0) / 16.0)
                    + (-55.0 * Math.Pow(n, 4.0) / 32.0);

                /* Precalculate delta_ (Eq. 10.22) */
                delta_ = (151.0 * Math.Pow(n, 3.0) / 96.0)
                    + (-417.0 * Math.Pow(n, 5.0) / 128.0);

                /* Precalculate epsilon_ (Eq. 10.22) */
                epsilon_ = (1097.0 * Math.Pow(n, 4.0) / 512.0);

                /* Now calculate the sum of the series (Eq. 10.21) */
                result = y_ + (beta_ * Math.Sin(2.0 * y_))
                    + (gamma_ * Math.Sin(4.0 * y_))
                    + (delta_ * Math.Sin(6.0 * y_))
                    + (epsilon_ * Math.Sin(8.0 * y_));

                return result;
            }

            private double[] MapLatLonToXY(double phi, double lambda, double lambda0)
            {
                double[] xy = new double[2];

                double N, nu2, ep2, t, t2, l;

                double l3coef, l4coef, l5coef, l6coef, l7coef, l8coef;

                double tmp;



                /* Precalculate ep2 */

                ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0)) / Math.Pow(sm_b, 2.0);



                /* Precalculate nu2 */

                nu2 = ep2 * Math.Pow(Math.Cos(phi), 2.0);



                /* Precalculate N */

                N = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nu2));



                /* Precalculate t */

                t = Math.Tan(phi);

                t2 = t * t;
                tmp = (t2 * t2 * t2) - Math.Pow(t, 6.0);

                /* Precalculate l */
                l = lambda - lambda0;

                /* Precalculate coefficients for l**n in the equations below
                   so a normal human being can read the expressions for easting
                   and northing
                   -- l**1 and l**2 have coefficients of 1.0 */

                l3coef = 1.0 - t2 + nu2;

                l4coef = 5.0 - t2 + 9 * nu2 + 4.0 * (nu2 * nu2);

                l5coef = 5.0 - 18.0 * t2 + (t2 * t2) + 14.0 * nu2
                    - 58.0 * t2 * nu2;

                l6coef = 61.0 - 58.0 * t2 + (t2 * t2) + 270.0 * nu2
                    - 330.0 * t2 * nu2;

                l7coef = 61.0 - 479.0 * t2 + 179.0 * (t2 * t2) - (t2 * t2 * t2);
                l8coef = 1385.0 - 3111.0 * t2 + 543.0 * (t2 * t2) - (t2 * t2 * t2);

                /* Calculate easting (x) */
                xy[0] = N * Math.Cos(phi) * l
                    + (N / 6.0 * Math.Pow(Math.Cos(phi), 3.0) * l3coef * Math.Pow(l, 3.0))
                    + (N / 120.0 * Math.Pow(Math.Cos(phi), 5.0) * l5coef * Math.Pow(l, 5.0))
                    + (N / 5040.0 * Math.Pow(Math.Cos(phi), 7.0) * l7coef * Math.Pow(l, 7.0));

                /* Calculate northing (y) */
                xy[1] = ArcLengthOfMeridian(phi)
                    + (t / 2.0 * N * Math.Pow(Math.Cos(phi), 2.0) * Math.Pow(l, 2.0))
                    + (t / 24.0 * N * Math.Pow(Math.Cos(phi), 4.0) * l4coef * Math.Pow(l, 4.0))
                    + (t / 720.0 * N * Math.Pow(Math.Cos(phi), 6.0) * l6coef * Math.Pow(l, 6.0))
                    + (t / 40320.0 * N * Math.Pow(Math.Cos(phi), 8.0) * l8coef * Math.Pow(l, 8.0));


                return xy;
            }

            private double[] MapXYToLatLon(double x, double y, double lambda0)
            {
                double[] latlon = new double[2];

                double phif, Nf, Nfpow, nuf2, ep2, tf, tf2, tf4, cf;
                double x1frac, x2frac, x3frac, x4frac, x5frac, x6frac, x7frac, x8frac;
                double x2poly, x3poly, x4poly, x5poly, x6poly, x7poly, x8poly;

                /* Get the value of phif, the footpoint latitude. */
                phif = FootpointLatitude(y);

                /* Precalculate ep2 */
                ep2 = (Math.Pow(sm_a, 2.0) - Math.Pow(sm_b, 2.0))
                      / Math.Pow(sm_b, 2.0);

                /* Precalculate cos (phif) */
                cf = Math.Cos(phif);

                /* Precalculate nuf2 */
                nuf2 = ep2 * Math.Pow(cf, 2.0);

                /* Precalculate Nf and initialize Nfpow */
                Nf = Math.Pow(sm_a, 2.0) / (sm_b * Math.Sqrt(1 + nuf2));
                Nfpow = Nf;

                /* Precalculate tf */
                tf = Math.Tan(phif);
                tf2 = tf * tf;
                tf4 = tf2 * tf2;

                /* Precalculate fractional coefficients for x**n in the equations
                   below to simplify the expressions for latitude and longitude. */
                x1frac = 1.0 / (Nfpow * cf);

                Nfpow *= Nf;   /* now equals Nf**2) */
                x2frac = tf / (2.0 * Nfpow);

                Nfpow *= Nf;   /* now equals Nf**3) */
                x3frac = 1.0 / (6.0 * Nfpow * cf);

                Nfpow *= Nf;   /* now equals Nf**4) */
                x4frac = tf / (24.0 * Nfpow);

                Nfpow *= Nf;   /* now equals Nf**5) */
                x5frac = 1.0 / (120.0 * Nfpow * cf);

                Nfpow *= Nf;   /* now equals Nf**6) */
                x6frac = tf / (720.0 * Nfpow);

                Nfpow *= Nf;   /* now equals Nf**7) */
                x7frac = 1.0 / (5040.0 * Nfpow * cf);

                Nfpow *= Nf;   /* now equals Nf**8) */
                x8frac = tf / (40320.0 * Nfpow);

                /* Precalculate polynomial coefficients for x**n.
                   -- x**1 does not have a polynomial coefficient. */
                x2poly = -1.0 - nuf2;

                x3poly = -1.0 - 2 * tf2 - nuf2;

                x4poly = 5.0 + 3.0 * tf2 + 6.0 * nuf2 - 6.0 * tf2 * nuf2
                    - 3.0 * (nuf2 * nuf2) - 9.0 * tf2 * (nuf2 * nuf2);

                x5poly = 5.0 + 28.0 * tf2 + 24.0 * tf4 + 6.0 * nuf2 + 8.0 * tf2 * nuf2;

                x6poly = -61.0 - 90.0 * tf2 - 45.0 * tf4 - 107.0 * nuf2
                    + 162.0 * tf2 * nuf2;

                x7poly = -61.0 - 662.0 * tf2 - 1320.0 * tf4 - 720.0 * (tf4 * tf2);

                x8poly = 1385.0 + 3633.0 * tf2 + 4095.0 * tf4 + 1575 * (tf4 * tf2);

                /* Calculate latitude */
                latlon[0] = phif + x2frac * x2poly * (x * x)
                    + x4frac * x4poly * Math.Pow(x, 4.0)
                    + x6frac * x6poly * Math.Pow(x, 6.0)
                    + x8frac * x8poly * Math.Pow(x, 8.0);

                /* Calculate longitude */
                latlon[1] = lambda0 + x1frac * x
                    + x3frac * x3poly * Math.Pow(x, 3.0)
                    + x5frac * x5poly * Math.Pow(x, 5.0)
                    + x7frac * x7poly * Math.Pow(x, 7.0);

                return latlon;
            }

            private void GeoUTMConverterXY(double lat, double lon, double zone)
            {
                double[] xy = MapLatLonToXY(lat, lon, UTMCentralMeridian(zone));

                xy[0] = xy[0] * UTMScaleFactor + 500000.0;
                xy[1] = xy[1] * UTMScaleFactor;
                if (xy[1] < 0.0)
                    xy[1] = xy[1] + 10000000.0;

                //this.X = xy[0];
                //this.Y = xy[1];

                this.X = Convert.ToInt32(xy[0]);
                this.Y = Convert.ToInt32(xy[1]);

                //this.X = FeetToMeters(this.X);
                //this.Y = FeetToMeters(this.Y);
            }

            private void UTMXYToLatLon(double x, double y, bool southhemi)
            {
                double cmeridian;

                x -= 500000.0;
                x /= UTMScaleFactor;

                /* If in southern hemisphere, adjust y accordingly. */
                if (southhemi)
                    y -= 10000000.0;

                y /= UTMScaleFactor;

                cmeridian = UTMCentralMeridian(Zone);
                double[] latlon = MapXYToLatLon(x, y, cmeridian);

                this.Latitude = RadToDeg(latlon[0]);
                this.Longitude = RadToDeg(latlon[1]);


            }


        }
    

}
