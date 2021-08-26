using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

//-------------------------------------------------------------------------------------
// 
//  FS Earth Tiles Area Data
// 
//-------------------------------------------------------------------------------------

namespace FSEarthTilesInternalDLL
{
    public class EarthArea
    {
        //constant
        const Double cAreaSnapEpsilon             = 1.0e-12;                    //used to avoid jumping to a new grid snap on exact entered coords
        const Double cAreaSnapTileEpsilon         = 1.0e-5;
        const Double cAreaSnapLOD13Epsilon        = 1.0e-5;
        const Double cAreaFSResamplingSnapEpsilon = 1.0e-12;                    //used for FSResamplingSnap (has to be between 1e-8(pixel size limit on -4 quartor) and  1e-13(calculation accuracy limit) ! and as small as possible (because outside equartor pixels are smaller!))

        //The effective used and snapped on Off/LOD13/LatLong/Tiles/Pixel Area Coords
        //If AreaSnap is Off then it is equal entered
        protected Double mAreaSnapStartLatitude;
        protected Double mAreaSnapStopLatitude;
        protected Double mAreaSnapStartLongitude;
        protected Double mAreaSnapStopLongitude;

        //Area Start-Stop Pixel Position within the Tile 
        private Int64 mXPixelPosAreaStart;
        private Int64 mXPixelPosAreaStop;
        private Int64 mYPixelPosAreaStart;
        private Int64 mYPixelPosAreaStop;

        //Nr of Tiles to loop through on Download to cover the Area and the true total Pixel the Area covers 
        private Int64 mAreaTilesInX;
        private Int64 mAreaTilesInY;
        private Int64 mAreaPixelsInX;
        private Int64 mAreaPixelsInY;
        private Int64 mFetchTilesTotal;

        //Area Code for Start (North-West-Corner) and Stop (South-Est-Corner) Pos
        private Int64 mAreaCodeXStart;
        private Int64 mAreaCodeXStop;
        private Int64 mAreaCodeYStart;
        private Int64 mAreaCodeYStop;

        //Exact Texture-Pixel-Area Coords before undistortion (Downloaded texture is always snapped to Pixel by it's nature)
        private Double mAreaPixelStartLongitude;
        private Double mAreaPixelStopLongitude;
        private Double mAreaPixelStartLatitude;
        private Double mAreaPixelStopLatitude;


        //Exact FS-Pixel-Grid Resampled AreaSnap coordinates
        private Double mAreaFSResampledStartLongitude;
        private Double mAreaFSResampledStopLongitude;
        private Double mAreaFSResampledStartLatitude;
        private Double mAreaFSResampledStopLatitude;
        
        //Exact Pxiels Count of FS-Resampled texture
        private Int64  mAreaFSResampledPixelsInX;
        private Int64  mAreaFSResampledPixelsInY;
        private Int64  mAreaFSResampledLOD;                    //The LOD depends on the Resolution Level and is +1 (Texture larger)

        public Boolean mBlendNorthBorder; //InternalUsed Only
        public Boolean mBlendEastBorder; //InternalUsed Only
        public Boolean mBlendSouthBorder; //InternalUsed Only
        public Boolean mBlendWestBorder; //InternalUsed Only


        public EarthArea()
        {
            mAreaSnapStartLatitude = 0.0;
            mAreaSnapStopLatitude = 0.0;
            mAreaSnapStartLongitude = 0.0;
            mAreaSnapStopLongitude = 0.0;
            mXPixelPosAreaStart = 0;
            mXPixelPosAreaStop = 0;
            mYPixelPosAreaStart = 0;
            mYPixelPosAreaStop = 0;

            mAreaTilesInX = 0;
            mAreaTilesInY = 0;
            mAreaPixelsInX = 0;
            mAreaPixelsInY = 0;
            mFetchTilesTotal = 0;


            mAreaCodeXStart = 0;
            mAreaCodeXStop = 0;
            mAreaCodeYStart = 0;
            mAreaCodeYStop = 0;

            mAreaPixelStartLongitude = 0.0;
            mAreaPixelStopLongitude = 0.0;
            mAreaPixelStartLatitude = 0.0;
            mAreaPixelStopLatitude = 0.0;

            mAreaFSResampledStartLongitude = 0.0;
            mAreaFSResampledStopLongitude = 0.0;
            mAreaFSResampledStartLatitude = 0.0;
            mAreaFSResampledStopLatitude = 0.0;

            mAreaFSResampledPixelsInX = 0;
            mAreaFSResampledPixelsInY = 0;
            mAreaFSResampledLOD = 0;  
        }

        public EarthArea(EarthArea iArea)
        {
            Copy(iArea);
        }

        public EarthArea Clone()
        {
            EarthArea vEarthArea = new EarthArea(this);
            return vEarthArea;
        }

        public void Copy(EarthArea iArea)
        {
            mAreaSnapStartLatitude  = iArea.mAreaSnapStartLatitude;
            mAreaSnapStopLatitude   = iArea.mAreaSnapStopLatitude;
            mAreaSnapStartLongitude = iArea.mAreaSnapStartLongitude;
            mAreaSnapStopLongitude  = iArea.mAreaSnapStopLongitude;
            mXPixelPosAreaStart     = iArea.mXPixelPosAreaStart;
            mXPixelPosAreaStop      = iArea.mXPixelPosAreaStop;
            mYPixelPosAreaStart     = iArea.mYPixelPosAreaStart;
            mYPixelPosAreaStop      = iArea.mYPixelPosAreaStop;

            mAreaTilesInX    = iArea.mAreaTilesInX;
            mAreaTilesInY    = iArea.mAreaTilesInY;
            mAreaPixelsInX   = iArea.mAreaPixelsInX;
            mAreaPixelsInY   = iArea.mAreaPixelsInY;
            mFetchTilesTotal = iArea.mFetchTilesTotal;


            mAreaCodeXStart  = iArea.mAreaCodeXStart;
            mAreaCodeXStop   = iArea.mAreaCodeXStop;
            mAreaCodeYStart  = iArea.mAreaCodeYStart;
            mAreaCodeYStop   = iArea.mAreaCodeYStop;

            mAreaPixelStartLongitude = iArea.mAreaPixelStartLongitude;
            mAreaPixelStopLongitude  = iArea.mAreaPixelStopLongitude;
            mAreaPixelStartLatitude  = iArea.mAreaPixelStartLatitude;
            mAreaPixelStopLatitude   = iArea.mAreaPixelStopLatitude;

            mAreaFSResampledStartLongitude = iArea.mAreaFSResampledStartLongitude;
            mAreaFSResampledStopLongitude  = iArea.mAreaFSResampledStopLongitude;
            mAreaFSResampledStartLatitude  = iArea.mAreaFSResampledStartLatitude;
            mAreaFSResampledStopLatitude   = iArea.mAreaFSResampledStopLatitude;
        
            mAreaFSResampledPixelsInX = iArea.mAreaFSResampledPixelsInX;
            mAreaFSResampledPixelsInY = iArea.mAreaFSResampledPixelsInY;
            mAreaFSResampledLOD       = iArea.mAreaFSResampledLOD;

            mBlendNorthBorder = iArea.mBlendNorthBorder;
            mBlendEastBorder = iArea.mBlendEastBorder;
            mBlendSouthBorder = iArea.mBlendSouthBorder;
            mBlendWestBorder = iArea.mBlendWestBorder;
        }


        public void CalculateAreaSnapCoordsAndCode(EarthInputArea iInputArea, tAreaSnapMode iAreaSnapMode, Int64 iLevel)
        {
            //Initialize Tile Snap (correct for Area Snap on Tiles)

            Double vAreaStartLongitude = iInputArea.AreaStartLongitude;
            Double vAreaStopLongitude  = iInputArea.AreaStopLongitude;
            Double vAreaStartLatitude  = iInputArea.AreaStartLatitude;
            Double vAreaStopLatitude   = iInputArea.AreaStopLatitude;

            if (iAreaSnapMode != tAreaSnapMode.eOff)
            {
                //Snap Mode .. avoid jumping to higher grid on exact entered coords
                if (iAreaSnapMode == tAreaSnapMode.eLOD13)
                {
                    //LOD13
                    vAreaStartLongitude += cAreaSnapLOD13Epsilon;
                    vAreaStopLongitude -= cAreaSnapLOD13Epsilon;
                    vAreaStartLatitude -= cAreaSnapLOD13Epsilon;
                    vAreaStopLatitude += cAreaSnapLOD13Epsilon;
                }
                else if (iAreaSnapMode == tAreaSnapMode.eTiles)
                {
                    //LOD13
                    vAreaStartLongitude += cAreaSnapTileEpsilon;
                    vAreaStopLongitude -= cAreaSnapTileEpsilon;
                    vAreaStartLatitude -= cAreaSnapTileEpsilon;
                    vAreaStopLatitude += cAreaSnapTileEpsilon;
                }
                else
                {
                    //Normal / LatLong
                    vAreaStartLongitude += cAreaSnapEpsilon;
                    vAreaStopLongitude -= cAreaSnapEpsilon;
                    vAreaStartLatitude -= cAreaSnapEpsilon;
                    vAreaStopLatitude += cAreaSnapEpsilon;

                }
            }

            mAreaCodeXStart = EarthMath.GetAreaCodeX(vAreaStartLongitude, iLevel);
            mAreaCodeXStop  = EarthMath.GetAreaCodeX(vAreaStopLongitude,  iLevel);
            mAreaCodeYStart = EarthMath.GetAreaCodeY(vAreaStartLatitude,  iLevel);
            mAreaCodeYStop  = EarthMath.GetAreaCodeY(vAreaStopLatitude,   iLevel);

            mAreaSnapStartLongitude = EarthMath.GetAreaTileLeftLongitude  (mAreaCodeXStart, iLevel);
            mAreaSnapStopLongitude  = EarthMath.GetAreaTileRightLongitude (mAreaCodeXStop,  iLevel);
            mAreaSnapStartLatitude  = EarthMath.GetAreaTileTopLatitude    (mAreaCodeYStart, iLevel);
            mAreaSnapStopLatitude   = EarthMath.GetAreaTileBottomLatitude (mAreaCodeYStop,  iLevel);


            mXPixelPosAreaStart = 0;
            mXPixelPosAreaStop  = 255;
            mYPixelPosAreaStart = 0;
            mYPixelPosAreaStop  = 255;

            if (iAreaSnapMode == tAreaSnapMode.eTiles)
            {
                //Snap on Tiles
                //Nothing to do. All proper initialized already
            }
            if (iAreaSnapMode == tAreaSnapMode.eOff)
            {
                //No Change in AreaCode, equal Tiles

                //Snap Coords indentical
                mAreaSnapStartLatitude  = vAreaStartLatitude;
                mAreaSnapStopLatitude   = vAreaStopLatitude;
                mAreaSnapStartLongitude = vAreaStartLongitude;
                mAreaSnapStopLongitude  = vAreaStopLongitude;

                mXPixelPosAreaStart  = EarthMath.GetPixelPosWithinTileX(vAreaStartLongitude, iLevel);
                mXPixelPosAreaStop   = EarthMath.GetPixelPosWithinTileX(vAreaStopLongitude,  iLevel);
                mYPixelPosAreaStart  = EarthMath.GetPixelPosWithinTileY(vAreaStartLatitude,  iLevel);
                mYPixelPosAreaStop   = EarthMath.GetPixelPosWithinTileY(vAreaStopLatitude,   iLevel);
            }
            if (iAreaSnapMode == tAreaSnapMode.ePixel)
            {
                //No Change in AreaCode, equal Tiles

                //Snap on Pixels
                mXPixelPosAreaStart = EarthMath.GetPixelPosWithinTileX(vAreaStartLongitude, iLevel);
                mXPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileX(vAreaStopLongitude,  iLevel);
                mYPixelPosAreaStart = EarthMath.GetPixelPosWithinTileY(vAreaStartLatitude,  iLevel);
                mYPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileY(vAreaStopLatitude,   iLevel);

                //ok not the most direct way but a safe one with the functions we already have
                Double vAreaStartLongitudeLeft = EarthMath.GetAreaTileLeftLongitude(mAreaCodeXStart, iLevel);
                Double vAreaStartLatitudeTop   = EarthMath.GetAreaTileTopLatitude  (mAreaCodeYStart, iLevel);
                Double vAreaStopLongitudeLeft  = EarthMath.GetAreaTileLeftLongitude(mAreaCodeXStop,  iLevel);
                Double vAreaStopLatitudeTop    = EarthMath.GetAreaTileTopLatitude  (mAreaCodeYStop,  iLevel);

                mAreaSnapStartLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(vAreaStartLongitudeLeft,  mXPixelPosAreaStart,    iLevel);
                mAreaSnapStopLongitude  = EarthMath.GetLongitudeFromLongitudeAndPixel(vAreaStopLongitudeLeft,  (mXPixelPosAreaStop+1),  iLevel);
                mAreaSnapStartLatitude  = EarthMath.GetLatitudeFromLatitudeAndPixel  (vAreaStartLatitudeTop,   -mYPixelPosAreaStart,    iLevel);
                mAreaSnapStopLatitude   = EarthMath.GetLatitudeFromLatitudeAndPixel  (vAreaStopLatitudeTop,   -(mYPixelPosAreaStop+1),  iLevel);

            }
            if (iAreaSnapMode == tAreaSnapMode.eLOD13)
            {
                //Snap on LOD13
                mAreaSnapStartLongitude = Math.Truncate((vAreaStartLongitude + 180.0) * EarthMath.InvLOD13LongitudeResolution)        * EarthMath.LOD13LongitudeResolution - 180.0;
                mAreaSnapStopLongitude = (Math.Truncate((vAreaStopLongitude  + 180.0) * EarthMath.InvLOD13LongitudeResolution) + 1.0) * EarthMath.LOD13LongitudeResolution - 180.0;

                //carefully we map with negative Latitude cords here so the sign turns
                mAreaSnapStartLatitude = Math.Truncate((vAreaStartLatitude - 90.0) * EarthMath.InvLOD13LatitudeResolution)        * EarthMath.LOD13LatitudeResolution + 90.0;
                mAreaSnapStopLatitude = (Math.Truncate((vAreaStopLatitude  - 90.0) * EarthMath.InvLOD13LatitudeResolution) - 1.0) * EarthMath.LOD13LatitudeResolution + 90.0;

                mAreaCodeXStart = EarthMath.GetAreaCodeX(mAreaSnapStartLongitude, iLevel);
                mAreaCodeXStop  = EarthMath.GetAreaCodeX(mAreaSnapStopLongitude,  iLevel);
                mAreaCodeYStart = EarthMath.GetAreaCodeY(mAreaSnapStartLatitude,  iLevel);
                mAreaCodeYStop  = EarthMath.GetAreaCodeY(mAreaSnapStopLatitude,   iLevel);

                mXPixelPosAreaStart = EarthMath.GetPixelPosWithinTileX(mAreaSnapStartLongitude, iLevel);
                mXPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileX(mAreaSnapStopLongitude,  iLevel);
                mYPixelPosAreaStart = EarthMath.GetPixelPosWithinTileY(mAreaSnapStartLatitude,  iLevel);
                mYPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileY(mAreaSnapStopLatitude,   iLevel);

            }
            if (iAreaSnapMode == tAreaSnapMode.eLatLong)
            {
                //Snap to whole minutes in the Latitude Longitude System
                mAreaSnapStartLongitude =  Math.Truncate((vAreaStartLongitude + 180.0) * EarthMath.InvLatLongLongitudeResolution)        * EarthMath.LatLongLongitudeResolution - 180.0;
                mAreaSnapStopLongitude  = (Math.Truncate((vAreaStopLongitude  + 180.0) * EarthMath.InvLatLongLongitudeResolution) + 1.0) * EarthMath.LatLongLongitudeResolution - 180.0;

                //carefully we map with positve Latitude here
                mAreaSnapStartLatitude = (Math.Truncate((vAreaStartLatitude + 90.0) * EarthMath.InvLatLongLatitudeResolution) + 1.0) * EarthMath.LatLongLatitudeResolution - 90.0;
                mAreaSnapStopLatitude  =  Math.Truncate((vAreaStopLatitude  + 90.0) * EarthMath.InvLatLongLatitudeResolution)        * EarthMath.LatLongLatitudeResolution - 90.0;

                mAreaCodeXStart  = EarthMath.GetAreaCodeX(mAreaSnapStartLongitude, iLevel);
                mAreaCodeXStop   = EarthMath.GetAreaCodeX(mAreaSnapStopLongitude,  iLevel);
                mAreaCodeYStart  = EarthMath.GetAreaCodeY(mAreaSnapStartLatitude,  iLevel);
                mAreaCodeYStop   = EarthMath.GetAreaCodeY(mAreaSnapStopLatitude,   iLevel);

                mXPixelPosAreaStart = EarthMath.GetPixelPosWithinTileX(mAreaSnapStartLongitude, iLevel);
                mXPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileX(mAreaSnapStopLongitude,  iLevel);
                mYPixelPosAreaStart = EarthMath.GetPixelPosWithinTileY(mAreaSnapStartLatitude,  iLevel);
                mYPixelPosAreaStop  = EarthMath.GetPixelPosWithinTileY(mAreaSnapStopLatitude,   iLevel);

            }
          
            //Calculate exact Texture-Pixel-Area Coords before undistortion (Downloaded texture is always snapped to Pixel by it's nature)
            Double vNewAreaStartLongitudeLeft = EarthMath.GetAreaTileLeftLongitude(mAreaCodeXStart, iLevel);
            Double vNewAreaStartLatitudeTop   = EarthMath.GetAreaTileTopLatitude  (mAreaCodeYStart, iLevel);
            Double vNewAreaStopLongitudeLeft  = EarthMath.GetAreaTileLeftLongitude(mAreaCodeXStop,  iLevel);
            Double vNewAreaStopLatitudeTop    = EarthMath.GetAreaTileTopLatitude  (mAreaCodeYStop,  iLevel);

            mAreaPixelStartLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(vNewAreaStartLongitudeLeft, mXPixelPosAreaStart,     iLevel);
            mAreaPixelStopLongitude  = EarthMath.GetLongitudeFromLongitudeAndPixel(vNewAreaStopLongitudeLeft, (mXPixelPosAreaStop + 1), iLevel);
            mAreaPixelStartLatitude  = EarthMath.GetLatitudeFromLatitudeAndPixel  (vNewAreaStartLatitudeTop,  -mYPixelPosAreaStart,     iLevel);
            mAreaPixelStopLatitude   = EarthMath.GetLatitudeFromLatitudeAndPixel  (vNewAreaStopLatitudeTop,  -(mYPixelPosAreaStop + 1), iLevel);

            CalculateAreaFSResampledCoords(iLevel);

            //CompleteAreaCodes Area Info
            Int64 vOverBorderAreaCodeX = mAreaCodeXStop;
            if (mAreaCodeXStop < mAreaCodeXStart) //Then We have alongitudinal overrun
            {
                vOverBorderAreaCodeX += EarthMath.GetAreaCodeSize(iLevel);
            }

            mAreaTilesInX = vOverBorderAreaCodeX - mAreaCodeXStart + 1;
            mAreaTilesInY = mAreaCodeYStop - mAreaCodeYStart + 1;

            mAreaPixelsInX = 256 * mAreaTilesInX - mXPixelPosAreaStart - (255 - mXPixelPosAreaStop);
            mAreaPixelsInY = 256 * mAreaTilesInY - mYPixelPosAreaStart - (255 - mYPixelPosAreaStop);

            mFetchTilesTotal = mAreaTilesInX * mAreaTilesInY;

        }
  



        public void CalculateAreaFSResampledCoords(Int64 iLevel)
        {
            const Double cAreaFSResamplingPixelEpsilon = 0.000001;

            //Resample to  ResampleLOD
            if (EarthConfig.mFSPreResampleingAllowShrinkTexture)
            {
                //shrink the Texture if FetchLevel <= Set Thershold
                if (EarthConfig.mFetchLevel <= EarthConfig.mFSPreResampleingShrinkTextureThresholdResolutionLevel)
                {
                    mAreaFSResampledLOD = EarthMath.cLevel0CodeDeep - iLevel - 2; //yes shrink
                }
                else
                {
                    mAreaFSResampledLOD = EarthMath.cLevel0CodeDeep - iLevel - 1; //enlarge
                }
            }
            else
            {
                //Normal is enlarge, this is what the FS resampler / compiler does usually also
                mAreaFSResampledLOD = EarthMath.cLevel0CodeDeep - iLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD. This improves quality a lot. because it works like a free antialiasing, breaking the pixel look just in the right quantity
            }
            //Initialize ResampleLOD (Pixel Resolution that means +8 steps 256 pixel each LOD.)
            Double vTemp = Convert.ToDouble(1 << Convert.ToInt32(mAreaFSResampledLOD + 8));
            Double vInvResampleLODLatitudeResolution  = vTemp / 90.0;
            Double vInvResampleLODLongitudeResolution = vTemp / 120.0;
            Double vResampleLODLatitudeResolution  = (1.0 / vInvResampleLODLatitudeResolution);
            Double vResampleLODLongitudeResolution = (1.0 / vInvResampleLODLongitudeResolution);

            Double vAreaStartLongitude     = mAreaSnapStartLongitude + cAreaFSResamplingSnapEpsilon;
            Double vAreaStopLongitude      = mAreaSnapStopLongitude  - cAreaFSResamplingSnapEpsilon;
            Double vAreaStartLatitude      = mAreaSnapStartLatitude  - cAreaFSResamplingSnapEpsilon;
            Double vAreaStopLatitude       = mAreaSnapStopLatitude   + cAreaFSResamplingSnapEpsilon;

            //And Snap to It
            mAreaFSResampledStartLongitude = Math.Truncate((vAreaStartLongitude + 180.0) * vInvResampleLODLongitudeResolution) * vResampleLODLongitudeResolution - 180.0;
            mAreaFSResampledStopLongitude = (Math.Truncate((vAreaStopLongitude + 180.0) * vInvResampleLODLongitudeResolution) + 1.0) * vResampleLODLongitudeResolution - 180.0;

            //carefully we map with negative Latitude cords here so the sign turns
            mAreaFSResampledStartLatitude = Math.Truncate((vAreaStartLatitude - 90.0) * vInvResampleLODLatitudeResolution) * vResampleLODLatitudeResolution + 90.0;
            mAreaFSResampledStopLatitude  = (Math.Truncate((vAreaStopLatitude - 90.0) * vInvResampleLODLatitudeResolution) - 1.0) * vResampleLODLatitudeResolution + 90.0;

            mAreaFSResampledPixelsInX = Convert.ToInt64(Math.Truncate(vInvResampleLODLongitudeResolution * (mAreaFSResampledStopLongitude - mAreaFSResampledStartLongitude) + cAreaFSResamplingPixelEpsilon));
            mAreaFSResampledPixelsInY = Convert.ToInt64(Math.Truncate(vInvResampleLODLatitudeResolution * (mAreaFSResampledStartLatitude - mAreaFSResampledStopLatitude)    + cAreaFSResamplingPixelEpsilon));
        }





        public Double AreaSnapStartLatitude  { get { return mAreaSnapStartLatitude;  } }
        public Double AreaSnapStopLatitude   { get { return mAreaSnapStopLatitude;   } }
        public Double AreaSnapStartLongitude { get { return mAreaSnapStartLongitude; } }
        public Double AreaSnapStopLongitude  { get { return mAreaSnapStopLongitude;  } }

        public Int64 XPixelPosAreaStart      { get { return mXPixelPosAreaStart; } }
        public Int64 XPixelPosAreaStop       { get { return mXPixelPosAreaStop;  } }
        public Int64 YPixelPosAreaStart      { get { return mYPixelPosAreaStart; } }
        public Int64 YPixelPosAreaStop       { get { return mYPixelPosAreaStop;  } }

        public Int64 AreaTilesInX            { get { return mAreaTilesInX;    } }
        public Int64 AreaTilesInY            { get { return mAreaTilesInY;    } }
        public Int64 AreaPixelsInX           { get { return mAreaPixelsInX;   } }
        public Int64 AreaPixelsInY           { get { return mAreaPixelsInY;   } }
        public Int64 FetchTilesTotal         { get { return mFetchTilesTotal; } }

        public Int64 AreaCodeXStart          { get { return mAreaCodeXStart; } }
        public Int64 AreaCodeXStop           { get { return mAreaCodeXStop;  } }
        public Int64 AreaCodeYStart          { get { return mAreaCodeYStart; } }
        public Int64 AreaCodeYStop           { get { return mAreaCodeYStop;  } }

        public Double AreaPixelStartLongitude { get { return mAreaPixelStartLongitude; } }
        public Double AreaPixelStopLongitude  { get { return mAreaPixelStopLongitude;  } }
        public Double AreaPixelStartLatitude  { get { return mAreaPixelStartLatitude;  } }
        public Double AreaPixelStopLatitude   { get { return mAreaPixelStopLatitude;   } }

        public Double AreaFSResampledStartLongitude { get { return mAreaFSResampledStartLongitude; } }
        public Double AreaFSResampledStopLongitude  { get { return mAreaFSResampledStopLongitude; } }
        public Double AreaFSResampledStartLatitude  { get { return mAreaFSResampledStartLatitude; } }
        public Double AreaFSResampledStopLatitude   { get { return mAreaFSResampledStopLatitude; } }

        public Int64 AreaFSResampledPixelsInX { get { return mAreaFSResampledPixelsInX; } }
        public Int64 AreaFSResampledPixelsInY { get { return mAreaFSResampledPixelsInY; } }
        public Int64 AreaFSResampledLOD       { get { return mAreaFSResampledLOD; } }            

    }




    public class ExcludeAreaInfo
    {
        private Int64 mExcludeAreaCodeXStart;
        private Int64 mExcludeAreaCodeXStop;
        private Int64 mExcludeAreaCodeYStart;
        private Int64 mExcludeAreaCodeYStop;
        private Double mExcludeAreaStartLongitude;
        private Double mExcludeAreaStopLongitude;
        private Double mExcludeAreaStartLatitude;
        private Double mExcludeAreaStopLatitude;

        public Int64 TileCount()
        {
            Int64 Count;
            Count = (this.mExcludeAreaCodeXStop - this.mExcludeAreaCodeXStart + 1) * (this.mExcludeAreaCodeYStop - this.mExcludeAreaCodeYStart + 1);
            return Count;

        }

        public ExcludeAreaInfo(Double StartLatitude, Double StartLongitude)
        {

            mExcludeAreaStartLatitude = StartLatitude;
            mExcludeAreaStartLongitude = StartLongitude;


        }

        public ExcludeAreaInfo(Double StartLatitude, Double StartLongitude, Double StopLatitude, Double StopLongitude)
        {

            mExcludeAreaStartLatitude = StartLatitude;
            mExcludeAreaStartLongitude = StartLongitude;
            mExcludeAreaStopLatitude = StopLatitude;
            mExcludeAreaStopLongitude = StopLongitude;


        }

        public ExcludeAreaInfo()
        {
        }

        public double ExcludeAreaStartLongitude
        {
            get { return mExcludeAreaStartLongitude; }
            set { mExcludeAreaStartLongitude = value; }

        }

        public double ExcludeAreaStartLatitude
        {
            get { return mExcludeAreaStartLatitude; }
            set { mExcludeAreaStartLatitude = value; }

        }

        public double ExcludeAreaStopLongitude
        {
            get { return mExcludeAreaStopLongitude; }
            set { mExcludeAreaStopLongitude = value; }

        }

        public double ExcludeAreaStopLatitude
        {
            get { return mExcludeAreaStopLatitude; }
            set { mExcludeAreaStopLatitude = value; }

        }

        public Int64 ExcludeAreaCodeXStart
        {
            get { return mExcludeAreaCodeXStart; }
            set { mExcludeAreaCodeXStart = value; }
        }
        public Int64 ExcludeAreaCodeXStop
        {
            get { return mExcludeAreaCodeXStop; }
            set { mExcludeAreaCodeXStop = value; }
        }
        public Int64 ExcludeAreaCodeYStart
        {
            get { return mExcludeAreaCodeYStart; }
            set { mExcludeAreaCodeYStart = value; }
        }
        public Int64 ExcludeAreaCodeYStop
        {
            get { return mExcludeAreaCodeYStop; }
            set { mExcludeAreaCodeYStop = value; }
        }
    }


    public class ExcludeArea : IEnumerable<ExcludeAreaInfo>
    {

        public List<ExcludeAreaInfo> Areas = new List<ExcludeAreaInfo>();

        public IEnumerator<ExcludeAreaInfo> GetEnumerator()
        {
            return this.Areas.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}
