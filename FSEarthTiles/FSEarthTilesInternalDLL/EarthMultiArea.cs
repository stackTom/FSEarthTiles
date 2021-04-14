using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public class EarthMultiArea : EarthArea
    {
        //Exact Pxiels Count of FS-Resampled texture
        private Int64 mAreaCountInX;
        private Int64 mAreaCountInY;
        private Int64 mAreaTotal;
        private Int64 mFetchTilesMultiAreaTotal;
        

        public EarthMultiArea()
        {
            mAreaCountInX = 0;
            mAreaCountInY = 0;
            mAreaTotal    = 0;
            mFetchTilesMultiAreaTotal = 0;
        }

        public EarthMultiArea(EarthMultiArea iMultiArea)
        {
            Copy(iMultiArea);
        }

        public new EarthMultiArea Clone()
        {
            EarthMultiArea vEarthMultiArea = new EarthMultiArea(this);
            return vEarthMultiArea;
        }

        public void Copy(EarthMultiArea iMultiArea)
        {
            base.Copy(iMultiArea);
            mAreaCountInX = iMultiArea.mAreaCountInX;
            mAreaCountInY = iMultiArea.mAreaCountInY;
            mAreaTotal    = iMultiArea.mAreaTotal;
            mFetchTilesMultiAreaTotal = iMultiArea.mFetchTilesMultiAreaTotal;
        }

        public void CalculateAreaSnapCoordsAndCodeForMultiArea(EarthInputArea iInputArea, EarthArea iReferenceArea, tAreaSnapMode iAreaSnapMode, Int64 iLevel)
        {
            //Calculate Normal Snap first
            base.CalculateAreaSnapCoordsAndCode(iInputArea, iAreaSnapMode, iLevel);

            Int64 vMultix = GetMultiAreasCountInX(iReferenceArea);
            Int64 vMultiy = GetMultiAreasCountInY(iReferenceArea, iAreaSnapMode);

            mAreaCountInX = vMultix;
            mAreaCountInY = vMultiy;
            mAreaTotal    = vMultix * vMultiy;

            if ((EarthConfig.mAreaSnapMode == tAreaSnapMode.eTiles) || (EarthConfig.mAreaSnapMode == tAreaSnapMode.ePixel))
            {
                Double vMultiStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(AreaSnapStartLatitude);
                Double vReferenceStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStartLatitude);
                Double vReferenceStopLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStopLatitude);
                Double vStepWidthLatitudeNormedMercatorY = vReferenceStopLatitudeNormedMercatorY - vReferenceStartLatitudeNormedMercatorY;
                Double vMultiStopLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY + Convert.ToDouble(vMultiy) * vStepWidthLatitudeNormedMercatorY;
                mAreaSnapStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vMultiStopLatitudeNormedMercatorY);

                Double vStepWidthLongitude = iReferenceArea.AreaSnapStopLongitude - iReferenceArea.AreaSnapStartLongitude;
                mAreaSnapStopLongitude = mAreaSnapStartLongitude + Convert.ToDouble(vMultix) * vStepWidthLongitude;
            }
            else
            {
                Double vStepWidthLatitude = iReferenceArea.AreaSnapStopLatitude - iReferenceArea.AreaSnapStartLatitude;
                Double vStepWidthLongitude = iReferenceArea.AreaSnapStopLongitude - iReferenceArea.AreaSnapStartLongitude;
                mAreaSnapStopLatitude = mAreaSnapStartLatitude + Convert.ToDouble(vMultiy) * vStepWidthLatitude;
                mAreaSnapStopLongitude = mAreaSnapStartLongitude + Convert.ToDouble(vMultix) * vStepWidthLongitude;

            }

            //Calculate new Snap of Information of MultiArea
            EarthInputArea vNewInputArea     = new EarthInputArea();
            vNewInputArea.AreaStartLongitude = mAreaSnapStartLongitude;
            vNewInputArea.AreaStopLongitude  = mAreaSnapStopLongitude;
            vNewInputArea.AreaStartLatitude  = mAreaSnapStartLatitude;
            vNewInputArea.AreaStopLatitude   = mAreaSnapStopLatitude;

            base.CalculateAreaSnapCoordsAndCode(vNewInputArea, iAreaSnapMode, iLevel);


            CalculateAreaFSResampledCoords(iLevel);


            //To get the total Tiles we download we need to loop through the Areas because every one may have different Tile Counts and we have overlapping           
            //Some few Area Border Tiles are Downloaded indeed twice now.
            mFetchTilesMultiAreaTotal = 0;

            //plain
            Double vLatitudeStep = iReferenceArea.AreaSnapStopLatitude - iReferenceArea.AreaSnapStartLatitude;
            Double vLongitudeStep = iReferenceArea.AreaSnapStopLongitude - iReferenceArea.AreaSnapStartLongitude;

            //And Mercator
            Double vNormedMercatorYLatitudeStart = EarthMath.GetNormedMercatorY(mAreaSnapStartLatitude);
            Double vReferenceNormedMercatorYLatitudeStart = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStartLatitude);
            Double vReferenceNormedMercatorYLatitudeStop = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStopLatitude);
            Double vLatitudeMercatorYStep = vReferenceNormedMercatorYLatitudeStop - vReferenceNormedMercatorYLatitudeStart;

            EarthArea vSimpleArea = new EarthArea();
            EarthInputArea vSimpleInputArea = new EarthInputArea();

            if ((vMultix * vMultiy )<= 6000)
            {
                for (Int64 vAreaPosY = 0; vAreaPosY < vMultiy; vAreaPosY++)
                {
                    for (Int64 vAreaPosX = 0; vAreaPosX < vMultix; vAreaPosX++)
                    {
                        if ((iAreaSnapMode == tAreaSnapMode.eTiles) || (iAreaSnapMode == tAreaSnapMode.ePixel))
                        {
                            vSimpleInputArea.AreaStartLongitude = mAreaSnapStartLongitude + vAreaPosX * vLongitudeStep;
                            vSimpleInputArea.AreaStopLongitude = mAreaSnapStartLongitude + (vAreaPosX + 1) * vLongitudeStep;
                            Double vSimpleAreaStartLatitudeMercatorY = vNormedMercatorYLatitudeStart + vAreaPosY * vLatitudeMercatorYStep;
                            Double vSimpleAreaStopLatitudeMercatorY = vNormedMercatorYLatitudeStart + (vAreaPosY + 1) * vLatitudeMercatorYStep;
                            vSimpleInputArea.AreaStartLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vSimpleAreaStartLatitudeMercatorY);
                            vSimpleInputArea.AreaStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vSimpleAreaStopLatitudeMercatorY);
                        }
                        else
                        {
                            vSimpleInputArea.AreaStartLongitude = mAreaSnapStartLongitude + vAreaPosX * vLongitudeStep;
                            vSimpleInputArea.AreaStopLongitude = mAreaSnapStartLongitude + (vAreaPosX + 1) * vLongitudeStep;
                            vSimpleInputArea.AreaStartLatitude = mAreaSnapStartLatitude + vAreaPosY * vLatitudeStep;
                            vSimpleInputArea.AreaStopLatitude = mAreaSnapStartLatitude + (vAreaPosY + 1) * vLatitudeStep;
                        }
                        vSimpleArea.CalculateAreaSnapCoordsAndCode(vSimpleInputArea, iAreaSnapMode, iLevel);
                        mFetchTilesMultiAreaTotal += vSimpleArea.FetchTilesTotal;
                    }
                }
            }

        }


        public EarthArea CalculateSingleAreaFormMultiArea(AreaInfo iAreaInfo, EarthArea iReferenceArea, tAreaSnapMode iAreaSnapMode, Int64 iLevel)
        {
            //plain
            Double vLatitudeStep  = iReferenceArea.AreaSnapStopLatitude - iReferenceArea.AreaSnapStartLatitude;
            Double vLongitudeStep = iReferenceArea.AreaSnapStopLongitude - iReferenceArea.AreaSnapStartLongitude;

            //And Mercator
            Double vNormedMercatorYLatitudeStart = EarthMath.GetNormedMercatorY(mAreaSnapStartLatitude);
            Double vReferenceNormedMercatorYLatitudeStart = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStartLatitude);
            Double vReferenceNormedMercatorYLatitudeStop = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStopLatitude);
            Double vLatitudeMercatorYStep = vReferenceNormedMercatorYLatitudeStop - vReferenceNormedMercatorYLatitudeStart;

            EarthArea      vSingleArea      = new EarthArea();
            EarthInputArea vSingleInputArea = new EarthInputArea();

            Int64 vAreaPosX = iAreaInfo.mAreaNrInX;
            Int64 vAreaPosY = iAreaInfo.mAreaNrInY;

            if ((iAreaSnapMode == tAreaSnapMode.eTiles) || (iAreaSnapMode == tAreaSnapMode.ePixel))
            {
                vSingleInputArea.AreaStartLongitude = mAreaSnapStartLongitude + vAreaPosX * vLongitudeStep;
                vSingleInputArea.AreaStopLongitude = mAreaSnapStartLongitude + (vAreaPosX + 1) * vLongitudeStep;
                Double vSimpleAreaStartLatitudeMercatorY = vNormedMercatorYLatitudeStart + vAreaPosY * vLatitudeMercatorYStep;
                Double vSimpleAreaStopLatitudeMercatorY = vNormedMercatorYLatitudeStart + (vAreaPosY + 1) * vLatitudeMercatorYStep;
                vSingleInputArea.AreaStartLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vSimpleAreaStartLatitudeMercatorY);
                vSingleInputArea.AreaStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vSimpleAreaStopLatitudeMercatorY);
            }
            else
            {
                vSingleInputArea.AreaStartLongitude = mAreaSnapStartLongitude + vAreaPosX * vLongitudeStep;
                vSingleInputArea.AreaStopLongitude = mAreaSnapStartLongitude + (vAreaPosX + 1) * vLongitudeStep;
                vSingleInputArea.AreaStartLatitude = mAreaSnapStartLatitude + vAreaPosY * vLatitudeStep;
                vSingleInputArea.AreaStopLatitude = mAreaSnapStartLatitude + (vAreaPosY + 1) * vLatitudeStep;
            }
            vSingleArea.CalculateAreaSnapCoordsAndCode(vSingleInputArea, iAreaSnapMode, iLevel);
            return vSingleArea;
        }


        public Int64 GetMultiAreasCountInX(EarthArea iReferenceArea)
        {
            Double vInXDouble = (mAreaSnapStopLongitude - mAreaSnapStartLongitude) / (iReferenceArea.AreaSnapStopLongitude - iReferenceArea.AreaSnapStartLongitude);
            Int64 vInX = Convert.ToInt64(Math.Truncate(vInXDouble - (1e-10)) + 1);
            return vInX;
        }


        public Int64 GetMultiAreasCountInY(EarthArea iReferenceArea, tAreaSnapMode iAreaSnapMode)
        {
            Double vInYDouble;
            if ((iAreaSnapMode == tAreaSnapMode.eTiles) || (iAreaSnapMode == tAreaSnapMode.ePixel))
            {
                Double vSnapLatitudeStart = EarthMath.GetNormedMercatorY(mAreaSnapStartLatitude);
                Double vSnapLatitudeStop = EarthMath.GetNormedMercatorY(mAreaSnapStopLatitude);
                Double vReferenceLatitudeStart = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStartLatitude);
                Double vReferenceLatitudeStop = EarthMath.GetNormedMercatorY(iReferenceArea.AreaSnapStopLatitude);
                vInYDouble = (vSnapLatitudeStart - vSnapLatitudeStop) / (vReferenceLatitudeStart - vReferenceLatitudeStop);
            }
            else
            {
                vInYDouble = (mAreaSnapStopLatitude - mAreaSnapStartLatitude) / (iReferenceArea.AreaSnapStopLatitude - iReferenceArea.AreaSnapStartLatitude);
            }
            Int64 vInY = Convert.ToInt64(Math.Truncate(vInYDouble - (1e-10)) + 1);
            return vInY;
        }


        public Int64 AreaCountInX { get { return mAreaCountInX; } }
        public Int64 AreaCountInY { get { return mAreaCountInY; } }
        public Int64 AreaTotal    { get { return mAreaTotal; } }
        public Int64 FetchTilesMultiAreaTotal { get { return mFetchTilesMultiAreaTotal; } }
    }

    // Class that holds info whether an area gets downloaded or not
    public class ProcessAreas
    {
        
        public List<ProcessAreaInfo> AreaInfo = new List<ProcessAreaInfo>();

        // Return the number of areas set to download
        public int GetNumberOfAreas2Download()
        {
            int AreaCount = 0;

            for (int idx = 0; idx < this.AreaInfo.Count; idx++)
            {

                if (this.AreaInfo[idx].ProcessArea == true)
                {
                    AreaCount++;

                }
            }
            return AreaCount;
        }
    }

    public class ProcessAreaInfo
    {
        public Boolean ProcessArea; // True if we want to download the area
        public Int64 AreaX;
        public Int64 AreaY;

    }


}
