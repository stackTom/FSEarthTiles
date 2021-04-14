using System;
using System.Collections.Generic;
using System.Text;

//-------------------------------------------------------------------------------------
// 
//  FS Earth Tiles Area User Input Data
// 
//-------------------------------------------------------------------------------------

namespace FSEarthTilesInternalDLL
{
    public class EarthInputArea
    {
        //The Entered Area Center + Size (1-Point)
        private Double mSpotLatitude;
        private Double mSpotLongitude;
        private Double mAreaXSize;
        private Double mAreaYSize;

        //The Entered 2-Point Area Coords
        private Double mAreaStartLatitude;
        private Double mAreaStopLatitude;
        private Double mAreaStartLongitude;
        private Double mAreaStopLongitude;

        public void Copy(EarthInputArea iInputArea)
        {
            mSpotLatitude  = iInputArea.mSpotLatitude;
            mSpotLongitude = iInputArea.mSpotLongitude;
            mAreaXSize     = iInputArea.mAreaXSize;
            mAreaYSize     = iInputArea.mAreaYSize;
            mAreaStartLatitude  = iInputArea.mAreaStartLatitude;
            mAreaStopLatitude   = iInputArea.mAreaStopLatitude;
            mAreaStartLongitude = iInputArea.mAreaStartLongitude;
            mAreaStopLongitude  = iInputArea.mAreaStopLongitude;
        }

        public Double SpotLatitude  { get { return mSpotLatitude;  } set { mSpotLatitude  = value; } }
        public Double SpotLongitude { get { return mSpotLongitude; } set { mSpotLongitude = value; } }
        public Double AreaXSize     { get { return mAreaXSize;     } set { mAreaXSize     = value; } }
        public Double AreaYSize     { get { return mAreaYSize;     } set { mAreaYSize     = value; } }

        public Double AreaStartLatitude  { get { return mAreaStartLatitude;  } set { mAreaStartLatitude  = value; } }
        public Double AreaStopLatitude   { get { return mAreaStopLatitude;   } set { mAreaStopLatitude   = value; } }
        public Double AreaStartLongitude { get { return mAreaStartLongitude; } set { mAreaStartLongitude = value; } }
        public Double AreaStopLongitude  { get { return mAreaStopLongitude;  } set { mAreaStopLongitude  = value; } }

    }
}
