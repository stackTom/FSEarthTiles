using System;
using System.Collections.Generic;
using System.Text;
using FSEarthTilesInternalDLL;


namespace FSEarthTilesDLL
{
    public class TileCodeingScript
    {
        //The following Methodes will be called by FSEarthTiles:
        //MapAreaCoordToTileCode(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iAreaCodeLevel, String iUseCode)

        public String MapAreaCoordToTileCode(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iAreaCodeLevel, String iUseCode)
        {

            String vResultCode = "";

            if (EarthCommon.StringCompare(iUseCode, "xyz"))      // example Service 3 thank's to Steffen I.
            {
                // Some Services uses same Lvl system as FSEarthTiles(funny coincidence) Service Lvl z=1 is 1 m/pixel

                Int64 vServiceZ = iAreaCodeLevel;
                Int64 vServiceX = iAreaCodeX;
                Int64 vServiceY;

                if (iAreaCodeLevel < EarthMath.cLevel0CodeDeep)
                {
                    Int32 vYConvertPower = (Int32)(EarthMath.cLevel0CodeDeep - iAreaCodeLevel - 1); //-1 because Service uses positive and negative numbers for Y
                    vServiceY = (1 << vYConvertPower) - 1 - iAreaCodeY;  // 1<<vYConvertPower (is equal to 2^vYConvertPower)
                }
                else
                {
                    //lvl 18, whole world is not covered correct, you can only get the upper (y=0)or lower (y=-1) world half instead the whole.
                    //so make that tile unavailable
                    vServiceX = 0;
                    vServiceY = 0;
                    vServiceZ = 100;
                }

                vResultCode = "x=" + vServiceX + "&y=" + vServiceY + "&z=" + vServiceZ;
            }


            else if (EarthCommon.StringCompare(iUseCode, "xyinvz"))
            {
                Int64 vServiceZ = EarthMath.cLevel0CodeDeep - iAreaCodeLevel;
                Int64 vServiceX = iAreaCodeX;
                Int64 vServiceY = iAreaCodeY;
                vResultCode = "&x=" + vServiceX.ToString() + "&y=" + vServiceY.ToString() + "&z=" + vServiceZ.ToString();
            }

            else if (EarthCommon.StringCompare(iUseCode, "zxy"))
            {
                Int64 vServiceZ = EarthMath.cLevel0CodeDeep - iAreaCodeLevel;
                Int64 vServiceX = iAreaCodeX;
                Int64 vServiceY = iAreaCodeY;
                //vResultCode = "&x=" + vServiceX.ToString() + "&y=" + vServiceY.ToString() + "&z=" + vServiceZ.ToString();
		vResultCode=vServiceZ.ToString() + "//" + vServiceX.ToString() + "//" + vServiceY.ToString();
            }

            else if (EarthCommon.StringCompare(iUseCode, "lxy"))
            {
                Int64 vServiceZ = EarthMath.cLevel0CodeDeep - iAreaCodeLevel;
                Int64 vServiceX = iAreaCodeX;
                Int64 vServiceY = iAreaCodeY;
		vServiceY=vServiceY-59243;
                
		vResultCode="level=" + vServiceZ.ToString() + "&x=" + vServiceX.ToString() + "&y=" + vServiceY.ToString();
		
            }

            else if (EarthCommon.StringCompare(iUseCode, "X1Y1X2Y2" ) ) // examples Services type NPOA (Spanish geografic institute) or
                                                                        // OpenLayers (Provided by MetaCarta, http://openlayers.org/)
            {
                Double vWestBorderLongitudeOfTile = EarthMath.GetAreaTileLeftLongitude (iAreaCodeX, iAreaCodeLevel);
                Double vEastBorderLongitudeOfTile = EarthMath.GetAreaTileRightLongitude (iAreaCodeX, iAreaCodeLevel);
                Double vNorthBorderLatitudeOfTile = EarthMath.GetAreaTileTopLatitude (iAreaCodeY, iAreaCodeLevel);
                Double vSouthBorderLatitudeOfTile = EarthMath.GetAreaTileBottomLatitude (iAreaCodeY, iAreaCodeLevel);

                vResultCode = vWestBorderLongitudeOfTile.ToString().Replace(",", "." ) + "," + vSouthBorderLatitudeOfTile.ToString().Replace
                (",", "." ) + "," + vEastBorderLongitudeOfTile.ToString().Replace(",", "." ) + "," + vNorthBorderLatitudeOfTile.ToString().Replace(",", "." );

            }

            else if (EarthCommon.StringCompare(iUseCode, "zxypng"))
            {
                Int64 vServiceZ = EarthMath.cLevel0CodeDeep - iAreaCodeLevel;
                Int64 vServiceX = iAreaCodeX;
                Int64 vServiceY = iAreaCodeY;
                //vResultCode = "&x=" + vServiceX.ToString() + "&y=" + vServiceY.ToString() + "&z=" + vServiceZ.ToString();
		        vResultCode=vServiceZ.ToString() + "/" + vServiceX.ToString() + "/" + vServiceY.ToString()+".png";
	        }
               

            else if (EarthCommon.StringCompare(iUseCode, "UTMWSEN" ) )
            
            {

                GeoUTMConverter gutmConverterE = new GeoUTMConverter();
                GeoUTMConverter gutmConverterW = new GeoUTMConverter();

                Double vWestBorderLongitudeOfTile = EarthMath.GetAreaTileLeftLongitude (iAreaCodeX, iAreaCodeLevel);
                Double vEastBorderLongitudeOfTile = EarthMath.GetAreaTileRightLongitude (iAreaCodeX, iAreaCodeLevel);
                Double vSouthBorderLatitudeOfTile = EarthMath.GetAreaTileBottomLatitude (iAreaCodeY, iAreaCodeLevel);
                Double vNorthBorderLatitudeOfTile = EarthMath.GetAreaTileTopLatitude (iAreaCodeY, iAreaCodeLevel);


                gutmConverterW.ToUTM(vNorthBorderLatitudeOfTile, vWestBorderLongitudeOfTile);
                gutmConverterE.ToUTM(vSouthBorderLatitudeOfTile, vEastBorderLongitudeOfTile);


                vResultCode=gutmConverterW.X + "," + gutmConverterW.Y  + "," + gutmConverterE.X + "," + gutmConverterE.Y;
            }


            else   // Quad mode,  examples Services 1 and 2
            {
                Int64 vSteps = EarthMath.cLevel0CodeDeep - iAreaCodeLevel;

                Int64 vXStart = 0;
                Int64 vXStop = EarthMath.GetAreaCodeSize(iAreaCodeLevel) - 1;

                Int64 vYStart = 0;
                Int64 vYStop = EarthMath.GetAreaCodeSize(iAreaCodeLevel) - 1;

                Int64 vXHalf;
                Int64 vYHalf;

                Char vC0;
                Char vC1;
                Char vC2;
                Char vC3;

                vC0 = iUseCode[0];
                vC1 = iUseCode[1];
                vC2 = iUseCode[2];
                vC3 = iUseCode[3];


                for (Int64 vCon = 1; vCon <= vSteps; vCon++)
                {
                    vXHalf = (vXStart + vXStop + 1) >> 1;
                    vYHalf = (vYStart + vYStop + 1) >> 1;

                    if (iAreaCodeY < vYHalf)
                    {
                        if (iAreaCodeX < vXHalf)
                        {
                            vResultCode += vC0;
                            vYStop = vYHalf;
                            vXStop = vXHalf;
                        }
                        else
                        {
                            vResultCode += vC1;
                            vYStop = vYHalf;
                            vXStart = vXHalf;
                        }
                    }
                    else
                    {
                        if (iAreaCodeX < vXHalf)
                        {
                            vResultCode += vC2;
                            vYStart = vYHalf;
                            vXStop = vXHalf;
                        }
                        else
                        {
                            vResultCode += vC3;
                            vYStart = vYHalf;
                            vXStart = vXHalf;
                        }
                    }
                }
            }

            return vResultCode;
        }

     



    }

}







