using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml;
using System.Globalization;
using System.IO;

namespace FSEarthTilesInternalDLL
{
    public class EarthKML
    {

        enum tLineType
        {
            eNone,
            eCoast,
            eDeepWater,
            eFullWaterPolygon,
            eWaterPoolPolygon,
            eLandPoolPolygon,
            eBlendPoolPolygon,
            eArea,
            eExclude
        }

        enum tTransitionType
        {
            eWaterTransition = 0,
            eWater2Transition = 1,
            eBlendTransition = 2,
            eSize = eBlendTransition + 1
        }

        struct tXYCoord
        {
            public Double mX;
            public Double mY;
        }



        Boolean mValidKMLDatas;
        Boolean mFirstPointSet;
        Int32   mStringParserIndex;
        
        List<tXYCoord> mRawCoordsLatLong; //[Grad] Longitude Latitude
        
        Double mStartLatitude;
        Double mStopLatitude;
        Double mStartLongitude;
        Double mStopLongitude;

        public Double StartLatitude  { get { return mStartLatitude; } }
        public Double StopLatitude   { get { return mStopLatitude; } }
        public Double StartLongitude { get { return mStartLongitude; } }
        public Double StopLongitude  { get { return mStopLongitude; } }

        public List<KMLAreas> Areas=new List<KMLAreas>();
 



        public EarthKML()
        {
            mRawCoordsLatLong  = new List<tXYCoord>();
                                  
            ClearAllDatas();
        }

        protected void ClearAllDatas()
        {
            mValidKMLDatas = false;
            mFirstPointSet = false;
            mStringParserIndex = 0;
            mStartLatitude = 0.0;
            mStopLatitude = 0.0;
            mStartLongitude = 0.0;
            mStopLongitude = 0.0;
            mRawCoordsLatLong  = new List<tXYCoord>();
            Areas = new List<KMLAreas>();
        }

        public Boolean IsValid()
        {
           return mValidKMLDatas;
        }


        public void LoadKMLFile(String iFileNameAndPath)
        {
            ClearAllDatas();

            if (File.Exists(iFileNameAndPath))
            {
                try
                {
                    tLineType vLineType = tLineType.eNone;
                    tTransitionType vTransitionType = tTransitionType.eWaterTransition;

                    XmlDocument vXMLDoc = new XmlDocument();
                    vXMLDoc.Load(iFileNameAndPath);
                    XmlNodeList vXMLNodeCoast = vXMLDoc.GetElementsByTagName("Placemark");

                    foreach (XmlNode vMyParentNode in vXMLNodeCoast)
                    {
                        vLineType = tLineType.eNone;
                        mRawCoordsLatLong = new List<tXYCoord>();
                        string AreaName = "";

                        foreach (XmlNode vMyChildNode in vMyParentNode.ChildNodes)
                        {
                            if (EarthCommon.StringCompare(vMyChildNode.Name, "Name"))
                            {
                                GetLineAndTransitionType(ref vLineType, ref vTransitionType, vMyChildNode.InnerText);
                                    AreaName = vMyChildNode.InnerText;
                             }
                            if (EarthCommon.StringCompare(vMyChildNode.Name, "LineString"))
                            {
                                foreach (XmlNode vMyChildChildNode in vMyChildNode.ChildNodes)
                                {
                                    if (EarthCommon.StringCompare(vMyChildChildNode.Name, "Coordinates"))
                                    {
                                        AddLatLongVector((Int32)(vTransitionType), vMyChildChildNode.InnerText, vLineType);
                                        KMLAreas mKMLArea = new KMLAreas();
                                        Areas.Add(mKMLArea);
                                        Areas[Areas.Count - 1].AreaName = AreaName;
                                        for (int idx = 0; idx < mRawCoordsLatLong.Count; idx++)
                                        {
                                            KMLCoords iCoords = new KMLCoords();
                                            iCoords.mLon = mRawCoordsLatLong[idx].mX;
                                            iCoords.mLat = mRawCoordsLatLong[idx].mY;
                                            Areas[Areas.Count - 1].Coords.Add(iCoords);

                                        }
                                    }
                                }
                            }
                            if (EarthCommon.StringCompare(vMyChildNode.Name, "Polygon"))
                            {
                                foreach (XmlNode vMyChildChildNode in vMyChildNode.ChildNodes)
                                {
                                    if (EarthCommon.StringCompare(vMyChildChildNode.Name, "outerBoundaryIs"))
                                    {
                                        foreach (XmlNode vMyChildChildChildNode in vMyChildChildNode.ChildNodes)
                                        {
                                            if (EarthCommon.StringCompare(vMyChildChildChildNode.Name, "LinearRing"))
                                            {
                                                foreach (XmlNode vMyChildChildChildChildNode in vMyChildChildChildNode.ChildNodes)
                                                {
                                                    if (EarthCommon.StringCompare(vMyChildChildChildChildNode.Name, "coordinates"))
                                                    {
                                                        AddLatLongVector((Int32)(vTransitionType), vMyChildChildChildChildNode.InnerText, vLineType);
                                                        
                                                            KMLAreas mKMLArea=new KMLAreas();
                                                            Areas.Add(mKMLArea);
                                                            Areas[Areas.Count - 1].AreaName = AreaName;
                                                            for (int idx = 0; idx < mRawCoordsLatLong.Count; idx++)
                                                            {
                                                                KMLCoords iCoords = new KMLCoords();
                                                                iCoords.mLon = mRawCoordsLatLong[idx].mX;
                                                                iCoords.mLat = mRawCoordsLatLong[idx].mY;
                                                                Areas[Areas.Count - 1].Coords.Add(iCoords);

                                                           }

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }

                    if (mFirstPointSet)
                    {
                        if ((mStartLatitude != mStopLatitude) && (mStartLongitude != mStopLongitude))
                        {
                            mValidKMLDatas = true;
                        }
                    }
                }
                catch
                {
                    // do nothing
                }
            }
        }


        private void AddLatLongVector(Int32 iTransitionType, String iParseMe, tLineType iLineType)
        {
            if (iLineType != tLineType.eNone)
            {

                iParseMe = iParseMe.Trim();
                iParseMe += " ";
                try
                {
                    Exception vMyException = new Exception("an exception");

                    ResetStringParserPointer();

                    while (!IsStringParserEnd(ref iParseMe))
                    {
                        Double vValue1 = 0.0;
                        Double vValue2 = 0.0;
                        Double vValue3 = 0.0;
                        tXYCoord vXYCoord;

                        Boolean vValid = false;

                        vValid = GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue1);

                        if (vValid)
                        {
                            vValid = GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue2);
                        }
                        else
                        {
                            if (!IsStringParserEnd(ref iParseMe))
                            {
                                throw vMyException;
                            }
                        }
                        if (vValid)
                        {
                            vValid = GetNextDoubleFromStringParser(ref iParseMe, ' ', ref vValue3);
                        }
                        else
                        {
                            throw vMyException;
                        }
                        if (vValid)
                        {
                            vXYCoord.mX = vValue1;
                            vXYCoord.mY = vValue2;
                            mRawCoordsLatLong.Add(vXYCoord);
                        }
                        else
                        {
                            throw vMyException;
                        }

                    }

                    //Now Add the Vectors to the correct Lists

                    if (iLineType == tLineType.eArea)
                    {
                        if (mRawCoordsLatLong.Count > 0)
                        {
                            if (!mFirstPointSet)
                            {
                                mStartLatitude = mRawCoordsLatLong[0].mY;
                                mStopLatitude = mRawCoordsLatLong[0].mY;
                                mStartLongitude = mRawCoordsLatLong[0].mX;
                                mStopLongitude = mRawCoordsLatLong[0].mX;
                                mFirstPointSet = true;
                            }
                            foreach (tXYCoord vXYCoord in mRawCoordsLatLong)
                            {
                                if (vXYCoord.mX > mStopLongitude)
                                {
                                    mStopLongitude = vXYCoord.mX;
                                }
                                if (vXYCoord.mX < mStartLongitude)
                                {
                                    mStartLongitude = vXYCoord.mX;
                                }
                                if (vXYCoord.mY < mStopLatitude)
                                {
                                    mStopLatitude = vXYCoord.mY;
                                }
                                if (vXYCoord.mY > mStartLatitude)
                                {
                                    mStartLatitude = vXYCoord.mY;
                                }
                            }

                        }
                    }

                }
                catch
                {
                     //error in parsing
                }
            }
        }


        private void GetLineAndTransitionType(ref tLineType oLineType, ref tTransitionType oTransitionType, String iString)
        {
            //Keep the order of the Keywords as follow:

            //CoastTwo
            //Coast
            //DeepWaterTwo
            //DeepWater
            //WaterTwo
            //WaterPool
            //Water
            //LandPool
            //BlendMax
            //BlendOn
            //BlendPool
            //Blend
            //Exclude
            //Area

            if (EarthCommon.StringContains(iString, "CoastTwo"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = tTransitionType.eWater2Transition;
            }
            else if (EarthCommon.StringContains(iString, "Coast"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = tTransitionType.eWaterTransition;
            }
            else if (EarthCommon.StringContains(iString, "DeepWaterTwo"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = tTransitionType.eWater2Transition;
            }
            else if (EarthCommon.StringContains(iString, "DeepWater"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = tTransitionType.eWaterTransition;
            }
            else if (EarthCommon.StringContains(iString, "WaterTwo"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = tTransitionType.eWater2Transition;
            }
            else if (EarthCommon.StringContains(iString, "WaterPool"))
            {
                oLineType = tLineType.eWaterPoolPolygon;
            }
            else if (EarthCommon.StringContains(iString, "Water"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = tTransitionType.eWaterTransition;
            }
            else if (EarthCommon.StringContains(iString, "LandPool"))
            {
                oLineType = tLineType.eLandPoolPolygon;
            }
            else if (EarthCommon.StringContains(iString, "BlendMax"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = tTransitionType.eBlendTransition;
            }
            else if (EarthCommon.StringContains(iString, "BlendOn"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = tTransitionType.eBlendTransition;
            }
            else if (EarthCommon.StringContains(iString, "BlendPool"))
            {
                oLineType = tLineType.eBlendPoolPolygon;
            }
            else if (EarthCommon.StringContains(iString, "Blend"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = tTransitionType.eBlendTransition;
            }
            else if (EarthCommon.StringContains(iString,"Exclude"))
            {
                oLineType = tLineType.eExclude ;
            }
            else if (EarthCommon.StringContains(iString, "Area"))
            {
                oLineType = tLineType.eArea;
            }
            else
            {
                oLineType = tLineType.eNone;
            }
        }

        private void ResetStringParserPointer()
        {
            mStringParserIndex = 0;
        }

        private Boolean IsStringParserEnd(ref String iString)
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

        private Boolean GetNextDoubleFromStringParser(ref String iString, Char iSeparator, ref Double oValue)
        {
            Double vValue = 0.0;
            Boolean vValid = false;
            String vString = "";

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

    }


    public class KMLCoords 
    {

            public Double mLon=0;
            public Double mLat=0;
    }

    public class KMLAreas : IEnumerable<KMLCoords>
    {
        private String mAreaName="";
        private Double mMaxLat;
        private Double mMaxLong;
        private Double mMinLat;
        private Double mMinLong;

        public List<KMLCoords> Coords = new List<KMLCoords>();

        public IEnumerator<KMLCoords> GetEnumerator()
        {
            return this.Coords.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        public String AreaName
        {
            get { return mAreaName; }
            set { mAreaName = value; }

        }

        public Double MaxLat
        {
            get 
            {
                if (this.Coords.Count >= 0) { mMinLat = this.Coords[0].mLat; }
                for (int idx = 0; idx < this.Coords.Count; idx++)
                {
                    if (mMaxLat < this.Coords[idx].mLat) { mMaxLat = this.Coords[idx].mLat; }                     
                }
                return mMaxLat; 
            }
            set {  }
        }

        public Double MinLat
        {
            get
            {
                if (this.Coords.Count >= 0) { mMinLat = this.Coords[0].mLat; }
                for (int idx = 0; idx < this.Coords.Count; idx++)
                {
                    if (mMinLat > this.Coords[idx].mLat) { mMinLat = this.Coords[idx].mLat; }
                }
                return mMinLat;
            }
            set { }
        }

        public Double MaxLong
        {
            get
            {
                if (this.Coords.Count >= 0) { mMaxLong = this.Coords[0].mLon; }
                for (int idx = 0; idx < this.Coords.Count; idx++)
                {
                    if (mMaxLong < this.Coords[idx].mLon) { mMaxLong = this.Coords[idx].mLon; }
                }
                return mMaxLong;
            }
            set { }
        }

        public Double MinLong
        {
            get
            {
                if (this.Coords.Count >= 0) { mMinLong = this.Coords[0].mLon; }
                for (int idx = 0; idx < this.Coords.Count; idx++)
                {
                    if (mMinLong > this.Coords[idx].mLon) { mMinLong = this.Coords[idx].mLon; }
                }
                return mMinLong;
            }
            set { }
        }

        public Boolean DoublePointinArea(Double iLatitude1, Double iLongitude1, Double iLatitude2, Double iLongitude2, Boolean WholeTile)
        {
            Boolean FirstPointisInArea=false;
            Boolean SecondPointisInArea = false;
            Boolean ThirdPointisInArea = false;
            Boolean FourthPointisInArea = false;
            Boolean AllPointsInArea = false;

            FirstPointisInArea = SinglePointinArea(iLatitude1, iLongitude1);
            SecondPointisInArea = SinglePointinArea(iLatitude2, iLongitude2);
            ThirdPointisInArea=SinglePointinArea(iLatitude1, iLongitude2);
            FourthPointisInArea=SinglePointinArea(iLatitude2, iLongitude1);

            if (WholeTile)
            {
                if (FirstPointisInArea && SecondPointisInArea && ThirdPointisInArea && FourthPointisInArea) { AllPointsInArea = true; }
            }

            if (!WholeTile)
            {
                if (FirstPointisInArea || SecondPointisInArea || ThirdPointisInArea || FourthPointisInArea) { AllPointsInArea = true; }
            }
            
            return AllPointsInArea;

        }

        public Boolean SinglePointinArea(Double iLatitude, Double iLongitude)
        {

            int n = this.Coords.Count-1;
            KMLCoords LocationLatLon = new KMLCoords();
            LocationLatLon.mLat = iLatitude;
            LocationLatLon.mLon = iLongitude;


            int wn = 0;    // the winding number counter

            if (this.Coords[0].mLat == this.Coords[n].mLat && this.Coords[0].mLon == this.Coords[n].mLon)
            {

                // loop through all edges of the polygon
                for (int i = 0; i < n; i++)
                {   
                    if (this.Coords[i].mLat <= LocationLatLon.mLat)
                    {         
                        if (this.Coords[i + 1].mLat > LocationLatLon.mLat)      // an upward crossing
                            if (isLeft(this.Coords[i], this.Coords[i + 1], LocationLatLon) > 0)  // Ppint left of edge
                                ++wn;            // have a valid up intersect
                    }
                    else
                    {                       
                        if (this.Coords[i + 1].mLat <= LocationLatLon.mLat)     // a downward crossing
                            if (isLeft(this.Coords[i], this.Coords[i + 1], LocationLatLon) < 0)  // P right of edge
                                --wn;            // have a valid down intersect
                    }
                }
                if (wn != 0)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }


        }

        private static int isLeft(KMLCoords P0, KMLCoords P1, KMLCoords Location)
        {
            double calc = ((P1.mLon - P0.mLon) * (Location.mLat - P0.mLat)
                    - (Location.mLon - P0.mLon) * (P1.mLat - P0.mLat));
            if (calc > 0)
                return 1;
            else if (calc < 0)
                return -1;
            else
                return 0;
        }
    }
}
