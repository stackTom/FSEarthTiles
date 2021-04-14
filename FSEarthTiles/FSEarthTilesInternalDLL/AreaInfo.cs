using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public class AreaInfo
    {

        public AreaInfo()
        {
            mAreaNrInX = 0;
            mAreaNrInY = 0;
        }

        public AreaInfo(AreaInfo iAreaInfo)
        {
            mAreaNrInX = iAreaInfo.mAreaNrInX;
            mAreaNrInY = iAreaInfo.mAreaNrInY;
        }


        public AreaInfo(Int64 iAreaNrInX, Int64 iAreaNrInY)
        {
            mAreaNrInX = iAreaNrInX;
            mAreaNrInY = iAreaNrInY;
        }

        public AreaInfo Clone()
        {
            return new AreaInfo(this);
        }


        public Boolean Equals(AreaInfo iAreaInfo)
        {
            Boolean vIsEqual = false;

            if ((mAreaNrInX == iAreaInfo.mAreaNrInX) &&
                (mAreaNrInY == iAreaInfo.mAreaNrInY))
            {
                vIsEqual = true;
            }
            return vIsEqual;
        }


        static public Boolean Equals(AreaInfo iAreaInfo1, AreaInfo iAreaInfo2)
        {
            return iAreaInfo1.Equals(iAreaInfo2);
        }


        //Public Datas
        public Int64 mAreaNrInX;  //public or private and use of C#'s get{} set{}
        public Int64 mAreaNrInY;
    }
}
