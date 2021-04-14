using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Globalization;

namespace FSEarthMasksInternalDLL
{
    public enum tLineType
    {
        eNone,
        eCoast,
        eDeepWater,
        eFullWaterPolygon,
        eWaterPoolPolygon,
        eLandPoolPolygon,
        eBlendPoolPolygon
    }

    public struct tXYCoord
    {
        public Double mX;
        public Double mY;
    }

    public struct tLine
    {
        public Single mX1;
        public Single mY1;
        public Single mX2;
        public Single mY2;
        public Single mUo;  // U = ContatcPoint Factor, ContactPoint is within Line when U between 0 and 1  
        public Single mUx;  // U = Uo + Ux*Px + Uy*Py, where P is the Point to check
        public Single mUy;
        public Single mDo;  // D = ContatcPoint Distance 
        public Single mDx;  // D = Do + Dx*Px + Dy*Py, where P is the Point to check
        public Single mDy;
    }

    public struct tPoint
    {
        public Single mX;
        public Single mY;
    }

    public struct tPointWithSquareDistance
    {
        public Single  mX;
        public Single  mY;
        public Single  mSquareDistance;
        public Boolean mValid;
    }

    public enum tWaterRegionType
    {
        eLand,
        eWater,
        eTransition,
        eUndetected
    }


    public struct tPoolPolygon
    {
        public MasksConfig.tPoolType mPoolType;
        public PointF[]              mPolygon;
    }


    public static class MasksCommon
    {
        public static List<tLine>[] mOriginalCoastLines     = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel] The Complet Set from the kml or svg file
        public static List<tLine>[] mOriginalDeepWaterLines = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]

        public static List<tLine>  [] mCoastLines      = new List<tLine>  [(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  Reduced set to fit the whole Area
        public static List<tLine>  [] mDeepWaterLines  = new List<tLine>  [(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  Only for MinDistance calculation usable
        public static List<tPoint> [] mCoastPoints     = new List<tPoint> [(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  Attention Coast and DeepWater will have different Are sizes
        public static List<tPoint> [] mDeepWaterPoints = new List<tPoint> [(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  And can become much larger than the Texture Area
        
        public static List<tLine>[]  mSliceCoastLines      = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  Slice set used for a fix number of rows (say 100)and becomes recreated every this 100.
        public static List<tLine>[]  mSliceDeepWaterLines  = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]  Only for MinDistance calculation usable
        public static List<tPoint>[] mSliceCoastPoints     = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]
        public static List<tPoint>[] mSliceDeepWaterPoints = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize)];   //[Pixel]


        public static List<tLine>[] mForCutCheckCoastLines = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];      //[Pixel]  Reduced to fit the whole Area for cutting / crossing check
        public static List<tLine>[] mForCutCheckDeepWaterLines = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize)];  //[Pixel]  To be used for the Land water detection

        //graphics primitive Polygon arrays (an array of a Lists of Polygons of Polygonpoints array)
        public static List<PointF[]>[]       mTransitionPolygons = new List<PointF []>[(Int32)(MasksConfig.tTransitionType.eSize)];
        public static List<tPoolPolygon>          mPoolPolygons = new List<tPoolPolygon>();

        public static Int32 mStringParserIndex = 0;


        public static void Initialize() //to call as first
        {
            for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
            {
                mOriginalCoastLines[vTransitionType]       = new List<tLine>();
                mOriginalDeepWaterLines[vTransitionType]   = new List<tLine>();

                mCoastLines[vTransitionType]               = new List<tLine>();
                mDeepWaterLines[vTransitionType]           = new List<tLine>();
                mCoastPoints[vTransitionType]              = new List<tPoint>();
                mDeepWaterPoints[vTransitionType]          = new List<tPoint>();

                mSliceCoastLines[vTransitionType]               = new List<tLine>();
                mSliceDeepWaterLines[vTransitionType]           = new List<tLine>();
                mSliceCoastPoints[vTransitionType]              = new List<tPoint>();
                mSliceDeepWaterPoints[vTransitionType]          = new List<tPoint>();             

                mForCutCheckCoastLines[vTransitionType]     = new List<tLine>();
                mForCutCheckDeepWaterLines[vTransitionType] = new List<tLine>();

                mTransitionPolygons[vTransitionType]       = new List<PointF[]>();

            }
        }

        public static Boolean StringCompare(String iString1, String iString2)
        {
            Boolean vIsEqual = String.Equals(iString1, iString2, StringComparison.CurrentCultureIgnoreCase);
            return vIsEqual;
        }


        public static Boolean StringContains(String iWhole, String iPart)
        {
            //different to Contains we want IgnoreCase
            Int32 vIndex = iWhole.IndexOf(iPart, StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void ReverseOrderOfPoolPolygonesInList()
        {
            mPoolPolygons.Reverse();
        }


        public static void ResetStringParserPointer()
        {
            mStringParserIndex = 0;
        }

        public static Boolean IsStringParserEnd(ref String iString)
        {
            if (mStringParserIndex >= iString.Length)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Boolean GetNextDoubleFromStringParser(ref String iString, Char iSeparator, ref Double oValue)
        {
            Double  vValue = 0.0;
            Boolean vValid = false;
            String  vString = "";

            if (!IsStringParserEnd(ref iString))
            {
                while ((mStringParserIndex < iString.Length) && (iString[mStringParserIndex] != iSeparator))
                {
                    vString += iString[mStringParserIndex];
                    mStringParserIndex++;
                }

                if (iString[mStringParserIndex] == iSeparator)
                {
                    mStringParserIndex++;
                }

                vString = vString.Trim();

                try
                {
                    vValue = Convert.ToDouble(vString, NumberFormatInfo.InvariantInfo);
                    oValue = vValue;
                    vValid = true;
                }
                catch
                {
                    //Nothing to do
                }

            }
            return vValid;
        }

        public static PointF[] GetReversePolygon(PointF[] iPolygon)
        {
           PointF[] vReversePolygon = new PointF[iPolygon.Length];
           for (Int32 vIndex = 0; vIndex < iPolygon.Length; vIndex++)
           {
               vReversePolygon[iPolygon.Length - vIndex - 1] = iPolygon[vIndex];
           }
        
           return vReversePolygon;
        }


        public static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


    }
}
