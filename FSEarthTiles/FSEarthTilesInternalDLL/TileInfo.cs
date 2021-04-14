using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public class TileInfo
    {


        public TileInfo()
        {
            mAreaCodeX = 0;
            mAreaCodeY = 0;
            mLevel     = 0;
            mService   = 0;
            mSkipTile = false;
        }

        public TileInfo(TileInfo iTileInfo)
        {
            mAreaCodeX = iTileInfo.mAreaCodeX;
            mAreaCodeY = iTileInfo.mAreaCodeY;
            mLevel     = iTileInfo.mLevel;
            mService   = iTileInfo.mService;
            mSkipTile  = iTileInfo.mSkipTile;

        }


        public TileInfo(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iLevel, Int32 iService, Boolean iSkipTile)
        {
            mAreaCodeX = iAreaCodeX;
            mAreaCodeY = iAreaCodeY;
            mLevel     = iLevel;
            mService   = iService;
            mSkipTile  = iSkipTile;
        }

        public TileInfo Clone()
        {
            return new TileInfo(this);
        }


        public Boolean Equals(TileInfo iTileInfo)
        {
            Boolean vIsEqual = false;

            if ((mAreaCodeX == iTileInfo.mAreaCodeX) &&
                (mAreaCodeY == iTileInfo.mAreaCodeY) &&
                (mLevel     == iTileInfo.mLevel)     &&
                (mService   == iTileInfo.mService)   &&
                (mSkipTile  == iTileInfo.mSkipTile))
            {
                vIsEqual = true;
            }
            return vIsEqual;
        }


        static public Boolean Equals(TileInfo iTileInfo1, TileInfo iTileInfo2)
        {
            return iTileInfo1.Equals(iTileInfo2);
        }


        //Public Datas
        public Int64 mAreaCodeX;  //public or private and use of C#'s get{} set{}
        public Int64 mAreaCodeY;
        public Int64 mLevel;
        public Int32 mService;
        public Boolean mSkipTile;
    }

}
