using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

//-------------------------------------------------------------------------------------
// 
//  FS Earth Tiles Area Texture Data and Texture Processing Methodes
// 
//  The Area-Texture is undistorted with a discrete! implemented Lanczos3 filter.
//  Lanczos is one of the best methode to resize a picture without quality lose 
//
//  Note that we do NOT resize the picture in Undistortion Off/Good/Perfcet just rearange it's content
//  Note that we DO resize the picture in FSResampler mode
//-------------------------------------------------------------------------------------

//    --- Carefully here!!! ---
//
//  This undistortion/filtering code here is no stright forward easy to follow stuff. It is complex.
//
//  First be aware of this:
//  In Undistortion Y we have:  StartLatitude  > StopLatitude,  Pixel-Row    0 is at Top  on StartLatitude  and Last Pixel Row    at StopLatitude  (count increases downward!)    and Filter coord X is in Y pixel orientation also (positive downward!)
//  In Undistortion X we have:  StartLongitude < StopLongitude, Pixel-Column 0 is at Left on StartLongitude and Last Pixel Column at StopLongitude (count increases toward right) and Filter coord X is in X pixel orientation also (positive right!)
// 
//  Also mind the pixel arrangement in the memory. We work with a RGB 24 Bit Bitmap. This is to safe memory. But we process this texture with  UInt32 here.
//
//  Because of Memory Limit I was forced to process the Texture within it's own space. This is done with help of a Ringbuffer containing the last 4 Pixel Rows of the Destination.
//  This Ringbuffer has to be equal the Half Filter Windows size + 1, so the filter always can access unaltered original values.
//
//  This methode with the Ringbuffer can only work if it's garanted, that the Source Row we read is always ahead of the Destination row we wright or at same position but never behind. (well it may be 1 pixel behind that's the catch space and required for the exact mode)
//  Together with the distortion we want to correct (Mercator) it is reuqired to split that in 2 parts (top down part, and down up part)and find the direction change point. Such a point exist when the equartor is part of the area.
//  Unfortunatly and this makes it even more complicated the turn point is NOT at the equartor! The equation to calculate the point is I believe not closed solvable which made a numerical pre search for that point required.
// 
//  Then the Filter itself is Lanczos3. Of course I wanted to use one that delivers best quality, means the least lose on the original texture quality. And this is the one. But in this filter you have to handle not only positive but negative wights also.
//
//  And last, somehow I thought it's a good idea to implement that filter discrete with Integers instead with floats which makes the whole code even more difficult because there we have a latent danger of a bit overrun if you do somehting wrong.
//

//
//  Now this IS Wired!
//  The 24Bit RGB Bitmap Format in the C# .NET Memory is not as you would guess stored one pixel afetr the other like this:
//
//  R1 G1 B1 R2 | G2 B2 R3 G3 | B3 R4 G4 B4  wheras this are 4 pixel stored in 3 UInt32 and R,G,B means Red Green and Blue Value (each 1 Byte)
//
//  but! :
//
//  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
//
//  wow isn't this crazy.. we have fun..ok guess it is faster this way... who knows.
//
// (I should have used the 32Bit format..but that would mean more memory use)
//
// byway the Bitmap in Memory is stored with Top row first to bottom wheras in a File you know it's bottum up.
//

namespace FSEarthTilesInternalDLL
{

    public enum tYUndistortionMode       // X does know exact only.
    {
        eMercatorOnly,
        eExact
    }

    public enum tResampleMode
    {
        eNormal,
        eMercator
    }

    public enum tMemoryAllocTestMode
    {
        eNormal,
        eResampler
    }

    public struct tPictureEnhancement
    {
        public Double mContrast;    //[%] -100.0 ... 0.0 ... +100.0
        public Double mBrightness;  //[%] -100.0 ... 0.0 ... +100.0
    }

    public enum tDoColorWork
    {
        eYesDoColorWork,
        eNoColorWork
    }

    public class EarthAreaTexture
    {

       const Int32  cHalfFilterWindow = 3;                                 //Has to fit with resize filter window (half windows size) for Lanczos3 = 3;
       const Int32  cUndistortionRingbufferSize = cHalfFilterWindow + 1;   //if we don't work with exact snapped coords and exact roudned values there is a chance that we could fall 1 pixel out the windows, so +1 to be on the safe side
       const Double cLanczosResamplerWindowsScaleingPartFactor = 0.0;     //0 means FIX Lanczos3 windows size. 1.0 means scaled with dest/sorce factor (best is 0.0 but you can try 0.25 to get a litte little bit sharpness, but don't go higher else you get an FSX resampler like bad texture again) 

        public EarthAreaTexture()
        {
            tPictureEnhancement vDummyPictureEnhancment; //pretend compiler warning
            vDummyPictureEnhancment.mBrightness = 0;
            vDummyPictureEnhancment.mContrast = 0;

            mMemoryAllocated = false;
            mMaxPixelInX = 1024;
            mMaxPixelInY = 1024;
            mMaxTotalAllowedPixels = (95 * mMaxPixelInX * mMaxPixelInY) / 100; //5% Reserve
        }

        // ---- Y Undistortion -----
        public void UndistortTextureY(EarthArea iEarthArea, tYUndistortionMode iYUndistortionMode, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            Boolean vError = false;
            Int32[] vActualFilterWeights;

            //deafult is exact
            Double vSourceStartLatitude = iEarthArea.AreaPixelStartLatitude;
            Double vSourceStopLatitude  = iEarthArea.AreaPixelStopLatitude;

            if (iYUndistortionMode == tYUndistortionMode.eMercatorOnly)
            {
                vSourceStartLatitude = iEarthArea.AreaSnapStartLatitude;
                vSourceStopLatitude  = iEarthArea.AreaSnapStopLatitude;
            }

            mUndistortionBitmapRowIndex = 0;

            mUndistortionBitmapRowRingbuffer = new UInt32[1, 1]; //else compiler thinks it becomes not assigned
            vActualFilterWeights = new Int32[1];

            try
            {
                mUndistortionBitmapRowRingbuffer = new UInt32[cUndistortionRingbufferSize, mArrayWidth];
                vActualFilterWeights = new Int32[2 * cHalfFilterWindow + 1];
            }
            catch
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("That's not good! I can not allocate the memory for the undistortion filter! ");
                vError = true;
            }

            if ((iEarthArea.AreaPixelsInY > cUndistortionRingbufferSize) && (!vError))  //only undistort if larger than the half of the filter and the ringbuffer (means 5 pixel min)
            {
                Int64 vDestinationPixelRowInY;  //The actual Destination
                Int64 vSourcePixelRowInY;       //The actual Source
                Double vFilterOffsetX;           //The X Offset of to Source Pixel Center to the price calculated pixel/row map  = equal the Filter offser that is to aplay

                Double vLatitudePerPixel = (iEarthArea.AreaSnapStartLatitude - iEarthArea.AreaSnapStopLatitude) / Convert.ToDouble(iEarthArea.AreaPixelsInY);
                Double vHalfLatitudePerPixel = 0.5 * vLatitudePerPixel;
                Double vInvLatitudePerPixel = 1.0 / vLatitudePerPixel;

                Double vStartNormedMercatorY = EarthMath.GetNormedMercatorY(vSourceStartLatitude);
                Double vStopNormedMercatorY = EarthMath.GetNormedMercatorY(vSourceStopLatitude);
                Double vNormedMercatorYPerPixel = (vStartNormedMercatorY - vStopNormedMercatorY) / Convert.ToDouble(iEarthArea.AreaPixelsInY); //Note that this value will be identical to EarthMath.EarthMath.GetNormedMercatorYPerPixel(mFetchLevel) when we have Snap To Tile or Pixel only.
                Double vInvNormedMercatorYPerPixel = 1.0 / vNormedMercatorYPerPixel;
                Double vHalfNormedMercatorY = 0.5 * vNormedMercatorYPerPixel;

                Double vActualLatitude;
                Double vActualNormedMercatorY;
                Double vPixelRowInYOfOriginalTexture;

                Double vPreciseWeight;
                Double vWeightSum;
                Int32 vDiscreteInvWeightSum;

                Int64 vUserInteractionCounter = 0;
                Int64 vPixelCounterForDisplay = 0;

                //Inner Loop members

                const UInt32 cMask1 = 0xFF00FF00;
                const UInt32 cMask2 = 0x00FF00FF;
                const UInt32 cMask3 = 0x0FFC0FFC;
                const UInt32 cMask4 = 0x0FF00FF0;

                UInt32 vWork1;
                UInt32 vWork2;

                UInt32 vNewValue1;
                UInt32 vNewValue2;

                Int32 vWeight;
                UInt32 vWeightenedWork1;
                UInt32 vWeightenedWork2;
                UInt32 vCalculatedValue;

                Int32 vSourceY;
                Int32 vDestY;

                Int32 vFilterRow;

                Int64 vStopDestination;


                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("correcting texture distortion Y ... ");


                // find undistortion direction change point else filter will overrun ringbuffer
                if (iEarthArea.AreaSnapStopLatitude >= 0.0)
                {
                    //North only
                    vStopDestination = iEarthArea.AreaPixelsInY;
                }
                else if (iEarthArea.AreaSnapStartLatitude < 0.0)
                {
                    //South only
                    vStopDestination = 0;
                }
                else
                {
                    //We have to find the point where the distortion changes direction.
                    //unfortunatly this is not at the equartor if th Area is non symetrical to the equator
                    //we need to solve an equation like 
                    // Meractor(TopLat)  + x * DeltaMercatorYPerPixel == Mercator(TopLat + x*DeltaLatPerPixel)
                    //for x  (where x is the pixelpos)
                    //this is a troublesome equation probably not even closed solevable.
                    //so we have to do it the stuip way..
                    Int64 vResult1 = -1;
                    Int64 vResult2 = -1;
                    Int64 vStartDestPixel = 0;
                    for (vStartDestPixel = 0; vStartDestPixel < iEarthArea.AreaPixelsInY; vStartDestPixel++)
                    {
                        vActualLatitude = iEarthArea.AreaSnapStartLatitude - (Convert.ToDouble(vStartDestPixel) * vLatitudePerPixel + vHalfLatitudePerPixel);
                        vActualNormedMercatorY = EarthMath.GetNormedMercatorY(vActualLatitude);
                        vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * ((vStartNormedMercatorY - vActualNormedMercatorY) - vHalfNormedMercatorY);
                        if (vPixelRowInYOfOriginalTexture < Convert.ToDouble(vStartDestPixel))
                        {
                            vResult1 = vStartDestPixel;
                            vStartDestPixel = iEarthArea.AreaPixelsInY; //and abort
                        }
                    }
                    //well lets check the other way also!
                    for (vStartDestPixel = iEarthArea.AreaPixelsInY - 1; vStartDestPixel >= 0; vStartDestPixel--)
                    {
                        vActualLatitude = iEarthArea.AreaSnapStartLatitude - (Convert.ToDouble(vStartDestPixel) * vLatitudePerPixel + vHalfLatitudePerPixel);
                        vActualNormedMercatorY = EarthMath.GetNormedMercatorY(vActualLatitude);
                        vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * ((vStartNormedMercatorY - vActualNormedMercatorY) - vHalfNormedMercatorY);
                        if (vPixelRowInYOfOriginalTexture > Convert.ToDouble(vStartDestPixel))
                        {
                            vResult2 = vStartDestPixel;
                            vStartDestPixel = 0; //and abort
                        }
                    }
                    if (((vResult1 - vResult2) > 2) || ((vResult1 - vResult2) < -2))
                    {
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("..ERROR.. inconsitent solution for undistortion change point hit! ");
                        Thread.Sleep(500);
                    }
                    vStopDestination = Convert.ToInt64(Math.Round(0.5 * Convert.ToDouble(vResult1 + vResult2)));
                    if (vStopDestination == -1)
                    {
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("..ERROR.. no solution for undistortion change point found! ");
                        Thread.Sleep(500);
                    }

                }

                if (iEarthArea.AreaPixelsInY < vStopDestination)
                {
                    vStopDestination = iEarthArea.AreaPixelsInY;
                }
                if (vStopDestination < 0)
                {
                    vStopDestination = 0;
                }



                //Processing World North Half undistortion From Top Latitude to Bottom Latitude

                //Initialize The Ringbuffer like border is a mirror
                for (Int32 vCon = 0; vCon < mArrayWidth; vCon++)
                {
                    for (Int32 vCon2 = 0; vCon2 < cUndistortionRingbufferSize; vCon2++)
                    {
                        mUndistortionBitmapRowRingbuffer[vCon2, vCon] = mAreaBitmapArray[cUndistortionRingbufferSize - vCon2, vCon];
                    }
                }


                for (vDestinationPixelRowInY = 0; vDestinationPixelRowInY < vStopDestination; vDestinationPixelRowInY++)
                {
                    vPixelCounterForDisplay++;

                    vActualLatitude = iEarthArea.AreaSnapStartLatitude - (Convert.ToDouble(vDestinationPixelRowInY) * vLatitudePerPixel + vHalfLatitudePerPixel);
                    vActualNormedMercatorY = EarthMath.GetNormedMercatorY(vActualLatitude);
                    vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * ((vStartNormedMercatorY - vActualNormedMercatorY) - vHalfNormedMercatorY);
                    vFilterOffsetX = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                    vSourcePixelRowInY = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                    vWeightSum = 0.0;

                    for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                    {
                        vPreciseWeight = EarthMath.Lanczos3(Convert.ToDouble(vFilterRow) + vFilterOffsetX);
                        vWeightSum += vPreciseWeight;
                        vActualFilterWeights[vFilterRow + cHalfFilterWindow] = SignedInt32Weight(vPreciseWeight);
                    }

                    vDiscreteInvWeightSum = DiscreteInvWeightSum(vWeightSum);

                    vSourceY = (Int32)vSourcePixelRowInY;
                    vDestY = (Int32)vDestinationPixelRowInY;

                    //speed: for a memcopy we could use System.Buffer.BlockCopy or second best Array.Copy 
                    for (Int32 vCon = 0; vCon < mArrayWidth; vCon++)
                    {
                        vNewValue1 = 0;
                        vNewValue2 = 0;

                        for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                        {
                            if ((vFilterRow + vSourceY) < vDestY)
                            {
                                if (((vFilterRow + vSourceY) - vDestY) >= -cUndistortionRingbufferSize)
                                {
                                    Int32 vRingBuffIndex = mUndistortionBitmapRowIndex + ((vFilterRow + vSourceY) - vDestY);
                                    if (vRingBuffIndex < 0)
                                    {
                                        vRingBuffIndex += cUndistortionRingbufferSize;
                                    }
                                    if (vRingBuffIndex >= cUndistortionRingbufferSize)
                                    {
                                        vRingBuffIndex -= cUndistortionRingbufferSize;
                                    }

                                    vWork1 = cMask1 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 = cMask2 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 <<= 8;
                                }
                                else
                                {
                                    //never happend again after figureing out the correct Stoppoint (vStopDestination) however
                                    //there is a chance this could happen due to rounding if exactly between 2 sources.
                                    //wight for this should be 0 then anyway because out of Filter window so not dramatic
                                    vWork1 = 0;
                                    vWork2 = 0;
                                    //fo debugging:
                                    //if (0 < vActualFilterWeights[vFilterRow + cHalfFilterWindow])
                                    //{
                                    //    SetStatusFromFriendThread("..that doesn't looks good! ");
                                    //}
                                }
                            }
                            else
                            {
                                if ((vFilterRow + vSourceY) >= iEarthArea.AreaPixelsInY)
                                {
                                    ///Mirror border
                                    Int32 vBorderIndex = ((Int32)iEarthArea.AreaPixelsInY << 1) - (vFilterRow + vSourceY) - 2;
                                    vWork1 = cMask1 & mAreaBitmapArray[vBorderIndex, vCon];
                                    vWork2 = cMask2 & mAreaBitmapArray[vBorderIndex, vCon];
                                }
                                else
                                {
                                    //Normal
                                    vWork1 = cMask1 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                                    vWork2 = cMask2 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                                }
                                vWork2 <<= 8;
                            }
                            if (vFilterRow == -cHalfFilterWindow)
                            {
                                //This Value will no more be used so Put in the DestinationPixel before it becomes overwritten!
                                mUndistortionBitmapRowRingbuffer[mUndistortionBitmapRowIndex, vCon] = mAreaBitmapArray[vDestinationPixelRowInY, vCon];
                            }

                            vWeight = vActualFilterWeights[vFilterRow + cHalfFilterWindow];

                            if ((vWeight & 256) == 256)
                            {
                                vWeightenedWork1 = vWork1;
                                vWeightenedWork2 = vWork2;
                            }
                            else
                            {
                                vWeightenedWork1 = 0;
                                vWeightenedWork2 = 0;

                                if ((vWeight & 1) == 1)
                                {
                                    vWeightenedWork1 += vWork1 >> 8;
                                    vWeightenedWork2 += vWork2 >> 8;
                                }
                                if ((vWeight & 2) == 2)
                                {
                                    vWeightenedWork1 += vWork1 >> 7;
                                    vWeightenedWork2 += vWork2 >> 7;
                                }
                                if ((vWeight & 4) == 4)
                                {
                                    vWeightenedWork1 += vWork1 >> 6;
                                    vWeightenedWork2 += vWork2 >> 6;
                                }
                                if ((vWeight & 8) == 8)
                                {
                                    vWeightenedWork1 += vWork1 >> 5;
                                    vWeightenedWork2 += vWork2 >> 5;
                                }
                                if ((vWeight & 16) == 16)
                                {
                                    vWeightenedWork1 += vWork1 >> 4;
                                    vWeightenedWork2 += vWork2 >> 4;
                                }
                                if ((vWeight & 32) == 32)
                                {
                                    vWeightenedWork1 += vWork1 >> 3;
                                    vWeightenedWork2 += vWork2 >> 3;
                                }
                                if ((vWeight & 64) == 64)
                                {
                                    vWeightenedWork1 += vWork1 >> 2;
                                    vWeightenedWork2 += vWork2 >> 2;
                                }
                                if ((vWeight & 128) == 128)
                                {
                                    vWeightenedWork1 += vWork1 >> 1;
                                    vWeightenedWork2 += vWork2 >> 1;
                                }
                            }

                            vWeightenedWork1 >>= 4;
                            vWeightenedWork2 >>= 4;
                            vWeightenedWork1 &= cMask3;
                            vWeightenedWork2 &= cMask3;

                            if (vWeight >= 0)
                            {
                                vNewValue1 += vWeightenedWork1;
                                vNewValue2 += vWeightenedWork2;
                            }
                            else
                            {
                                vNewValue1 -= vWeightenedWork1;
                                vNewValue2 -= vWeightenedWork2;
                            }

                        }

                        //cut negative values
                        if ((vNewValue1 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue1 &= 0x0000FFFF;
                        }
                        if ((vNewValue1 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue1 &= 0xFFFF0000;
                        }
                        if ((vNewValue2 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue2 &= 0x0000FFFF;
                        }
                        if ((vNewValue2 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue2 &= 0xFFFF0000;
                        }

                        //Here we haveto divide through the Sum of The Weights (multiplay with it's inverse
                        //With Lanczos3 we could spare this operation becasue the Sum of Weights is always within:
                        //0.994 (destination pixel is exact between 2 source pixels) and 1.0 (exact source and dest are exact on the pixel)
                        //brightness error-> 0.8pixel for a 128 grey field, 1.5 pixel for bright white  

                        //however let's correct it. Carefully they needs to stay 1 empty bit between the two color values in the NewValues Int32
                        //The following correction is for the WeightSum range of lanczos3 only. you need to expant for more common cases                   
                        
                        //attention not 100% proper check resampler code (proper we need to use a temp variable lik above)
                        if ((vDiscreteInvWeightSum & 1) == 1)
                        {
                            vNewValue1 += (vNewValue1 & 0x0C000C00) >> 9;
                            vNewValue2 += (vNewValue2 & 0x0C000C00) >> 9;
                        }
                        if ((vDiscreteInvWeightSum & 2) == 2)
                        {
                            vNewValue1 += (vNewValue1 & 0x0E000E00) >> 8;
                            vNewValue2 += (vNewValue2 & 0x0E000E00) >> 8;
                        }
                        if ((vDiscreteInvWeightSum & 4) == 4)
                        {
                            vNewValue1 += (vNewValue1 & 0x0F000F00) >> 7;
                            vNewValue2 += (vNewValue2 & 0x0F000F00) >> 7;
                        }


                        //rounding
                        if ((vNewValue1 & 0x00080000) == 0x00080000)
                        {
                            vNewValue1 += 0x00080000;
                        }

                        if ((vNewValue1 & 0x00000008) == 0x00000008)
                        {
                            vNewValue1 += 0x00000008;
                        }
                        if ((vNewValue2 & 0x00080000) == 0x00080000)
                        {
                            vNewValue2 += 0x00080000;
                        }

                        if ((vNewValue2 & 0x00000008) == 0x00000008)
                        {
                            vNewValue2 += 0x00000008;
                        }


                        //cut larger than 255
                        if ((vNewValue1 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0x0000FFFF;
                            vNewValue1 |= 0x0FF00000;
                        }
                        if ((vNewValue1 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0xFFFF0000;
                            vNewValue1 |= 0x00000FF0;
                        }
                        if ((vNewValue2 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0x0000FFFF;
                            vNewValue2 |= 0x0FF00000;
                        }
                        if ((vNewValue2 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0xFFFF0000;
                            vNewValue2 |= 0x00000FF0;
                        }

                        vNewValue1 &= cMask4;
                        vNewValue2 &= cMask4;

                        vCalculatedValue = (vNewValue1 << 4) | (vNewValue2 >> 4);

                        //we have the value! fast assign it!..fast fast..flushhh...too late!...hehe jokeing ;)
                        mAreaBitmapArray[vDestY, vCon] = vCalculatedValue;

                    }

                    //and ringbuffer Index +1
                    mUndistortionBitmapRowIndex++;
                    if (mUndistortionBitmapRowIndex >= cUndistortionRingbufferSize)
                    {
                        mUndistortionBitmapRowIndex = 0;
                    }

                    vUserInteractionCounter++;

                    if ((vUserInteractionCounter >= 100) || (vPixelCounterForDisplay == iEarthArea.AreaPixelsInY))
                    {
                        vUserInteractionCounter = 0;
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("correcting texture distortion   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(iEarthArea.AreaPixelsInY));
                        Application.DoEvents();
                    }

                }

                //Processing World South Half undistortion From Bottom Latitude to Top Latitude

                //Initialize The Ringbuffer like border is a mirror
                for (Int32 vCon = 0; vCon < mArrayWidth; vCon++)
                {
                    for (Int32 vCon2 = 0; vCon2 < cUndistortionRingbufferSize; vCon2++)
                    {
                        mUndistortionBitmapRowRingbuffer[vCon2, vCon] = mAreaBitmapArray[iEarthArea.AreaPixelsInY - (cUndistortionRingbufferSize - vCon2) - 1, vCon];
                    }
                }



                for (vDestinationPixelRowInY = (iEarthArea.AreaPixelsInY - 1); vDestinationPixelRowInY >= vStopDestination; vDestinationPixelRowInY--)
                {
                    vPixelCounterForDisplay++;

                    vActualLatitude = iEarthArea.AreaSnapStartLatitude - (Convert.ToDouble(vDestinationPixelRowInY) * vLatitudePerPixel + vHalfLatitudePerPixel);
                    vActualNormedMercatorY = EarthMath.GetNormedMercatorY(vActualLatitude);
                    vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * ((vStartNormedMercatorY - vActualNormedMercatorY) - vHalfNormedMercatorY);
                    vFilterOffsetX = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                    vSourcePixelRowInY = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                    vWeightSum = 0.0;

                    for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                    {
                        vPreciseWeight = EarthMath.Lanczos3(Convert.ToDouble(vFilterRow) + vFilterOffsetX);
                        vWeightSum += vPreciseWeight;
                        vActualFilterWeights[vFilterRow + cHalfFilterWindow] = SignedInt32Weight(vPreciseWeight);
                    }

                    vDiscreteInvWeightSum = DiscreteInvWeightSum(vWeightSum);

                    vSourceY = (Int32)vSourcePixelRowInY;
                    vDestY = (Int32)vDestinationPixelRowInY;

                    //speed: for a memcopy we could use System.Buffer.BlockCopy or second best Array.Copy 
                    for (Int32 vCon = 0; vCon < mArrayWidth; vCon++)
                    {
                        vNewValue1 = 0;
                        vNewValue2 = 0;

                        for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                        {
                            if ((vFilterRow + vSourceY) > vDestY)
                            {
                                if (((vFilterRow + vSourceY) - vDestY) <= cUndistortionRingbufferSize)
                                {
                                    Int32 vRingBuffIndex = mUndistortionBitmapRowIndex - ((vFilterRow + vSourceY) - vDestY);
                                    if (vRingBuffIndex < 0)
                                    {
                                        vRingBuffIndex += cUndistortionRingbufferSize;
                                    }
                                    if (vRingBuffIndex >= cUndistortionRingbufferSize)
                                    {
                                        vRingBuffIndex -= cUndistortionRingbufferSize;
                                    }
                                    vWork1 = cMask1 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 = cMask2 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 <<= 8;
                                }
                                else
                                {
                                    //never happend again after figureing out the correct Stoppoint however
                                    //there is a chance this could happen due to rounding if exactly between 2 sources.
                                    //wight for this should be 0 then anyway because out of Filter window so not dramatic
                                    vWork1 = 0;
                                    vWork2 = 0;
                                    //fo debugging:
                                    //if (0 < vActualFilterWeights[vFilterRow + cHalfFilterWindow])
                                    //{
                                    //    SetStatusFromFriendThread("..that doesn't looks good! ");
                                    //}
                                }
                            }
                            else
                            {
                                if ((vFilterRow + vSourceY) <= -1)
                                {
                                    ///Mirror border
                                    Int32 vBorderIndex = -(vFilterRow + vSourceY);
                                    vWork1 = cMask1 & mAreaBitmapArray[vBorderIndex, vCon];
                                    vWork2 = cMask2 & mAreaBitmapArray[vBorderIndex, vCon];
                                }
                                else
                                {
                                    //Normal
                                    vWork1 = cMask1 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                                    vWork2 = cMask2 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                                }
                                vWork2 <<= 8;
                            }
                            if (vFilterRow == +cHalfFilterWindow)
                            {
                                //This Value will no more be used so Put in the DestinationPixel before it becomes overwritten!
                                mUndistortionBitmapRowRingbuffer[mUndistortionBitmapRowIndex, vCon] = mAreaBitmapArray[vDestinationPixelRowInY, vCon];
                            }

                            vWeight = vActualFilterWeights[vFilterRow + cHalfFilterWindow];

                            if ((vWeight & 256) == 256)
                            {
                                vWeightenedWork1 = vWork1;
                                vWeightenedWork2 = vWork2;
                            }
                            else
                            {
                                vWeightenedWork1 = 0;
                                vWeightenedWork2 = 0;

                                if ((vWeight & 1) == 1)
                                {
                                    vWeightenedWork1 += vWork1 >> 8;
                                    vWeightenedWork2 += vWork2 >> 8;
                                }
                                if ((vWeight & 2) == 2)
                                {
                                    vWeightenedWork1 += vWork1 >> 7;
                                    vWeightenedWork2 += vWork2 >> 7;
                                }
                                if ((vWeight & 4) == 4)
                                {
                                    vWeightenedWork1 += vWork1 >> 6;
                                    vWeightenedWork2 += vWork2 >> 6;
                                }
                                if ((vWeight & 8) == 8)
                                {
                                    vWeightenedWork1 += vWork1 >> 5;
                                    vWeightenedWork2 += vWork2 >> 5;
                                }
                                if ((vWeight & 16) == 16)
                                {
                                    vWeightenedWork1 += vWork1 >> 4;
                                    vWeightenedWork2 += vWork2 >> 4;
                                }
                                if ((vWeight & 32) == 32)
                                {
                                    vWeightenedWork1 += vWork1 >> 3;
                                    vWeightenedWork2 += vWork2 >> 3;
                                }
                                if ((vWeight & 64) == 64)
                                {
                                    vWeightenedWork1 += vWork1 >> 2;
                                    vWeightenedWork2 += vWork2 >> 2;
                                }
                                if ((vWeight & 128) == 128)
                                {
                                    vWeightenedWork1 += vWork1 >> 1;
                                    vWeightenedWork2 += vWork2 >> 1;
                                }
                            }

                            vWeightenedWork1 >>= 4;
                            vWeightenedWork2 >>= 4;
                            vWeightenedWork1 &= cMask3;
                            vWeightenedWork2 &= cMask3;

                            if (vWeight >= 0)
                            {
                                vNewValue1 += vWeightenedWork1;
                                vNewValue2 += vWeightenedWork2;
                            }
                            else
                            {
                                vNewValue1 -= vWeightenedWork1;
                                vNewValue2 -= vWeightenedWork2;
                            }

                        }

                        //cut negative values
                        if ((vNewValue1 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue1 &= 0x0000FFFF;
                        }
                        if ((vNewValue1 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue1 &= 0xFFFF0000;
                        }
                        if ((vNewValue2 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue2 &= 0x0000FFFF;
                        }
                        if ((vNewValue2 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue2 &= 0xFFFF0000;
                        }

                        //Here we haveto divide through the Sum of The Weights (multiplay with it's inverse
                        //With Lanczos3 we could spare this operation becasue the Sum of Weights is always within:
                        //0.994 (destination pixel is exact between 2 source pixels) and 1.0 (exact source and dest are exact on the pixel)
                        //brightness error-> 0.8pixel for a 128 grey field, 1.5 pixel for bright white  

                        //however let's correct it. Carefully they needs to stay 1 empty bit between the two color values in the NewValues Int32
                        //The following correction is for the WeightSum range of lanczos3 only. you need to expant for more common cases                   
                        if ((vDiscreteInvWeightSum & 1) == 1)
                        {
                            vNewValue1 += (vNewValue1 & 0x0C000C00) >> 9;
                            vNewValue2 += (vNewValue2 & 0x0C000C00) >> 9;
                        }
                        if ((vDiscreteInvWeightSum & 2) == 2)
                        {
                            vNewValue1 += (vNewValue1 & 0x0E000E00) >> 8;
                            vNewValue2 += (vNewValue2 & 0x0E000E00) >> 8;
                        }
                        if ((vDiscreteInvWeightSum & 4) == 4)
                        {
                            vNewValue1 += (vNewValue1 & 0x0F000F00) >> 7;
                            vNewValue2 += (vNewValue2 & 0x0F000F00) >> 7;
                        }


                        //rounding
                        if ((vNewValue1 & 0x00080000) == 0x00080000)
                        {
                            vNewValue1 += 0x00080000;
                        }

                        if ((vNewValue1 & 0x00000008) == 0x00000008)
                        {
                            vNewValue1 += 0x00000008;
                        }
                        if ((vNewValue2 & 0x00080000) == 0x00080000)
                        {
                            vNewValue2 += 0x00080000;
                        }

                        if ((vNewValue2 & 0x00000008) == 0x00000008)
                        {
                            vNewValue2 += 0x00000008;
                        }


                        //cut larger than 255
                        if ((vNewValue1 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0x0000FFFF;
                            vNewValue1 |= 0x0FF00000;
                        }
                        if ((vNewValue1 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0xFFFF0000;
                            vNewValue1 |= 0x00000FF0;
                        }
                        if ((vNewValue2 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0x0000FFFF;
                            vNewValue2 |= 0x0FF00000;
                        }
                        if ((vNewValue2 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0xFFFF0000;
                            vNewValue2 |= 0x00000FF0;
                        }

                        vNewValue1 &= cMask4;
                        vNewValue2 &= cMask4;

                        vCalculatedValue = (vNewValue1 << 4) | (vNewValue2 >> 4);

                        //we have the value! fast assign it!..fast fast..flushhh...too late!...hehe jokeing ;)
                        mAreaBitmapArray[vDestY, vCon] = vCalculatedValue;

                    }

                    //and ringbuffer Index +1
                    mUndistortionBitmapRowIndex++;
                    if (mUndistortionBitmapRowIndex >= cUndistortionRingbufferSize)
                    {
                        mUndistortionBitmapRowIndex = 0;
                    }

                    vUserInteractionCounter++;

                    if ((vUserInteractionCounter >= 100) || (vPixelCounterForDisplay == iEarthArea.AreaPixelsInY))
                    {
                        vUserInteractionCounter = 0;
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("correcting texture distortion    row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(iEarthArea.AreaPixelsInY));
                        Application.DoEvents();
                    }

                }

                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Finished processing texture undistortion. ");
                //Thread.Sleep(500);

            }
        }


              


        // ---- X Undistortion -----

        public void UndistortTextureX(EarthArea iEarthArea, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            Boolean vError = false;
            Int32[] vActualFilterWeights;

            //Does know exact mode only
            Double vSourceStartLongitude = iEarthArea.AreaPixelStartLongitude;
            Double vSourceStopLongitude  = iEarthArea.AreaPixelStopLongitude;


            mUndistortionBitmapRowIndex = 0;

            mUndistortionBitmapRowRingbuffer = new UInt32[1, 1]; //else compiler thinks it becomes not assigned
            vActualFilterWeights = new Int32[1];

            try
            {
                mUndistortionBitmapRowRingbuffer = new UInt32[cUndistortionRingbufferSize, iEarthArea.AreaPixelsInY];
                vActualFilterWeights = new Int32[2 * cHalfFilterWindow + 1];
            }
            catch
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread("That's not good! I can not allocate the memory for the undistortion filter! ");
                vError = true;
            }

            if ((iEarthArea.AreaPixelsInX > cUndistortionRingbufferSize) && (!vError))  //only undistort if larger than the half of the filter and the ringbuffer (means 5 pixel min)
            {
                Int64 vDestinationPixelRowInX;  //The actual Destination
                Int64 vSourcePixelRowInX;       //The actual Source
                Double vFilterOffsetX;           //The X Offset of to Source Pixel Center to the price calculated pixel/row map  = equal the Filter offser that is to aplay

                Double vLongitudePerPixel = (iEarthArea.AreaSnapStopLongitude - iEarthArea.AreaSnapStartLongitude) / Convert.ToDouble(iEarthArea.AreaPixelsInX);
                Double vHalfLongitudePerPixel = 0.5 * vLongitudePerPixel;
                Double vInvLongitudePerPixel = 1.0 / vLongitudePerPixel;

                Double vStopSourceLongitude  = vSourceStopLongitude;
                Double vStartSourceLongitude = vSourceStartLongitude;
                Double vSourceLongitudePerPixel = (vStopSourceLongitude - vStartSourceLongitude) / Convert.ToDouble(iEarthArea.AreaPixelsInX); //Note that this value will be identical to EarthMath.EarthMath.GetSourceLongitudePerPixel(mFetchLevel) when we have Snap To Tile or Pixel only.
                Double vInvSourceLongitudePerPixel = 1.0 / vSourceLongitudePerPixel;
                Double vHalfSourceLongitude = 0.5 * vSourceLongitudePerPixel;

                Double vActualLongitude;
                Double vActualSourceLongitude;
                Double vPixelRowInYOfOriginalTexture;

                Double vPreciseWeight;
                Double vWeightSum;
                Int32 vDiscreteInvWeightSum;

                Int64 vUserInteractionCounter = 0;
                Int64 vPixelCounterForDisplay = 0;

                //Inner Loop members

                const UInt32 cMask1 = 0xFF00FF00;
                const UInt32 cMask2 = 0x00FF00FF;
                const UInt32 cMask3 = 0x0FFC0FFC;
                const UInt32 cMask4 = 0x0FF00FF0;

                UInt32 vWork1;
                UInt32 vWork2;

                UInt32 vNewValue1;
                UInt32 vNewValue2;

                Int32  vWeight;
                UInt32 vWeightenedWork1;
                UInt32 vWeightenedWork2;
                UInt32 vCalculatedValue;

                Int32 vSourceX;
                Int32 vDestX;

                Int32 vFilterRow;

                Int32 vPixelInQuadIndex   = 0;
                Int32 vUInt32IndexModal96 = 0;

                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("correcting texture distortion X ... ");


                //Processing From East Longitude to West Longitude

                //Initialize The Ringbuffer like border is a mirror
                for (Int32 vCon = 0; vCon < iEarthArea.AreaPixelsInY; vCon++)
                {
                    for (Int32 vCon2 = 0; vCon2 < cUndistortionRingbufferSize; vCon2++)
                    {
                        UInt32 vSourcePixel = GetSourcePixel24BitBitmapFormat(cUndistortionRingbufferSize - vCon2, vCon);
                        mUndistortionBitmapRowRingbuffer[vCon2, vCon] = vSourcePixel;
                    }
                }



                for (vDestinationPixelRowInX = 0; vDestinationPixelRowInX < iEarthArea.AreaPixelsInX; vDestinationPixelRowInX++)
                {
                    vPixelCounterForDisplay++;

                    vActualLongitude              = iEarthArea.AreaSnapStartLongitude + (Convert.ToDouble(vDestinationPixelRowInX) * vLongitudePerPixel + vHalfLongitudePerPixel);
                    vActualSourceLongitude        = vActualLongitude;
                    vPixelRowInYOfOriginalTexture = vInvSourceLongitudePerPixel * ((vActualSourceLongitude - vStartSourceLongitude) - vHalfSourceLongitude);
                    vFilterOffsetX                = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                    vSourcePixelRowInX            = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                    vWeightSum = 0.0;

                    for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                    {
                        vPreciseWeight = EarthMath.Lanczos3(Convert.ToDouble(vFilterRow) + vFilterOffsetX);
                        vWeightSum    += vPreciseWeight;
                        vActualFilterWeights[vFilterRow + cHalfFilterWindow] = SignedInt32Weight(vPreciseWeight);
                    }

                    vDiscreteInvWeightSum = DiscreteInvWeightSum(vWeightSum);

                    vSourceX = (Int32)vSourcePixelRowInX;
                    vDestX   = (Int32)vDestinationPixelRowInX;

                    //speed: for a memcopy we could use System.Buffer.BlockCopy or second best Array.Copy 
                    for (Int32 vCon = 0; vCon < iEarthArea.AreaPixelsInY; vCon++)
                    {
                        vNewValue1 = 0;
                        vNewValue2 = 0;

                        for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                        {
                            if ((vFilterRow + vSourceX) < vDestX)
                            {
                                if (((vFilterRow + vSourceX) - vDestX) >= -cUndistortionRingbufferSize)
                                {
                                    Int32 vRingBuffIndex = mUndistortionBitmapRowIndex + ((vFilterRow + vSourceX) - vDestX);
                                    if (vRingBuffIndex < 0)
                                    {
                                        vRingBuffIndex += cUndistortionRingbufferSize;
                                    }
                                    if (vRingBuffIndex >= cUndistortionRingbufferSize)
                                    {
                                        vRingBuffIndex -= cUndistortionRingbufferSize;
                                    }
                                    vWork1 = cMask1 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 = cMask2 & mUndistortionBitmapRowRingbuffer[vRingBuffIndex, vCon];
                                    vWork2 <<= 8;
                                }
                                else
                                {
                                    //never happend again after figureing out the correct Startpoint however
                                    //there is a chance this could happen due to rounding if exactly between 2 sources.
                                    //wight for this should be 0 then anyway because out of Filter window so not dramatic
                                    vWork1 = 0;
                                    vWork2 = 0;
                                    //for debugging:
                                    if (0 < vActualFilterWeights[vFilterRow + cHalfFilterWindow])
                                    {
                                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("..that doesn't looks good! ");
                                    }
                                }
                            }
                            else
                            {
                                if ((vFilterRow + vSourceX) >= iEarthArea.AreaPixelsInX)
                                {
                                    ///Mirror border
                                    Int32 vBorderIndex  = ((Int32)iEarthArea.AreaPixelsInX << 1) - (vFilterRow + vSourceX) - 2;
                                    UInt32 vSourcePixel = GetSourcePixel24BitBitmapFormat(vBorderIndex, vCon);
                                    vWork1 = cMask1 & vSourcePixel;
                                    vWork2 = cMask2 & vSourcePixel;
                                }
                                else
                                {
                                    //Normal
                                    UInt32 vSourcePixel = GetSourcePixel24BitBitmapFormat(vFilterRow + vSourceX, vCon);
                                    vWork1 = cMask1 & vSourcePixel;
                                    vWork2 = cMask2 & vSourcePixel;
                                }
                                vWork2 <<= 8;
                            }
                            if (vFilterRow == -cHalfFilterWindow)
                            {
                                //This Value will no more be used so Put in the DestinationPixel before it becomes overwritten!
                                mUndistortionBitmapRowRingbuffer[mUndistortionBitmapRowIndex, vCon] =  GetSourcePixel24BitBitmapFormat((Int32)vDestinationPixelRowInX, vCon);
                            }

                            vWeight = vActualFilterWeights[vFilterRow + cHalfFilterWindow];

                            if ((vWeight & 256) == 256)
                            {
                                vWeightenedWork1 = vWork1;
                                vWeightenedWork2 = vWork2;
                            }
                            else
                            {
                                vWeightenedWork1 = 0;
                                vWeightenedWork2 = 0;

                                if ((vWeight & 1) == 1)
                                {
                                    vWeightenedWork1 += vWork1 >> 8;
                                    vWeightenedWork2 += vWork2 >> 8;
                                }
                                if ((vWeight & 2) == 2)
                                {
                                    vWeightenedWork1 += vWork1 >> 7;
                                    vWeightenedWork2 += vWork2 >> 7;
                                }
                                if ((vWeight & 4) == 4)
                                {
                                    vWeightenedWork1 += vWork1 >> 6;
                                    vWeightenedWork2 += vWork2 >> 6;
                                }
                                if ((vWeight & 8) == 8)
                                {
                                    vWeightenedWork1 += vWork1 >> 5;
                                    vWeightenedWork2 += vWork2 >> 5;
                                }
                                if ((vWeight & 16) == 16)
                                {
                                    vWeightenedWork1 += vWork1 >> 4;
                                    vWeightenedWork2 += vWork2 >> 4;
                                }
                                if ((vWeight & 32) == 32)
                                {
                                    vWeightenedWork1 += vWork1 >> 3;
                                    vWeightenedWork2 += vWork2 >> 3;
                                }
                                if ((vWeight & 64) == 64)
                                {
                                    vWeightenedWork1 += vWork1 >> 2;
                                    vWeightenedWork2 += vWork2 >> 2;
                                }
                                if ((vWeight & 128) == 128)
                                {
                                    vWeightenedWork1 += vWork1 >> 1;
                                    vWeightenedWork2 += vWork2 >> 1;
                                }
                            }

                            vWeightenedWork1 >>= 4;
                            vWeightenedWork2 >>= 4;
                            vWeightenedWork1 &= cMask3;
                            vWeightenedWork2 &= cMask3;

                            if (vWeight >= 0)
                            {
                                vNewValue1 += vWeightenedWork1;
                                vNewValue2 += vWeightenedWork2;
                            }
                            else
                            {
                                vNewValue1 -= vWeightenedWork1;
                                vNewValue2 -= vWeightenedWork2;
                            }

                        }

                        //cut negative values
                        if ((vNewValue1 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue1 &= 0x0000FFFF;
                        }
                        if ((vNewValue1 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue1 &= 0xFFFF0000;
                        }
                        if ((vNewValue2 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue2 &= 0x0000FFFF;
                        }
                        if ((vNewValue2 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue2 &= 0xFFFF0000;
                        }

                        //Here we haveto divide through the Sum of The Weights (multiplay with it's inverse
                        //With Lanczos3 we could spare this operation becasue the Sum of Weights is always within:
                        //0.994 (destination pixel is exact between 2 source pixels) and 1.0 (exact source and dest are exact on the pixel)
                        //brightness error-> 0.8pixel for a 128 grey field, 1.5 pixel for bright white  

                        //however let's correct it. Carefully they needs to stay 1 empty bit between the two color values in the NewValues Int32
                        //The following correction is for the WeightSum range of lanczos3 only. you need to expant for more common cases                   
                        if ((vDiscreteInvWeightSum & 1) == 1)
                        {
                            vNewValue1 += (vNewValue1 & 0x0C000C00) >> 9;
                            vNewValue2 += (vNewValue2 & 0x0C000C00) >> 9;
                        }
                        if ((vDiscreteInvWeightSum & 2) == 2)
                        {
                            vNewValue1 += (vNewValue1 & 0x0E000E00) >> 8;
                            vNewValue2 += (vNewValue2 & 0x0E000E00) >> 8;
                        }
                        if ((vDiscreteInvWeightSum & 4) == 4)
                        {
                            vNewValue1 += (vNewValue1 & 0x0F000F00) >> 7;
                            vNewValue2 += (vNewValue2 & 0x0F000F00) >> 7;
                        }


                        //rounding
                        if ((vNewValue1 & 0x00080000) == 0x00080000)
                        {
                            vNewValue1 += 0x00080000;
                        }

                        if ((vNewValue1 & 0x00000008) == 0x00000008)
                        {
                            vNewValue1 += 0x00000008;
                        }
                        if ((vNewValue2 & 0x00080000) == 0x00080000)
                        {
                            vNewValue2 += 0x00080000;
                        }

                        if ((vNewValue2 & 0x00000008) == 0x00000008)
                        {
                            vNewValue2 += 0x00000008;
                        }


                        //cut larger than 255
                        if ((vNewValue1 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0x0000FFFF;
                            vNewValue1 |= 0x0FF00000;
                        }
                        if ((vNewValue1 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0xFFFF0000;
                            vNewValue1 |= 0x00000FF0;
                        }
                        if ((vNewValue2 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0x0000FFFF;
                            vNewValue2 |= 0x0FF00000;
                        }
                        if ((vNewValue2 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0xFFFF0000;
                            vNewValue2 |= 0x00000FF0;
                        }

                        vNewValue1 &= cMask4;
                        vNewValue2 &= cMask4;

                        vCalculatedValue = (vNewValue1 << 4) | (vNewValue2 >> 4);

                        //we have the value! fast assign it!..fast fast..flushhh...too late!...hehe jokeing ;)
                        //SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vCon, vCalculatedValue);
                        mAreaBitmapArray[vCon,vDestinationPixelRowInX]=vCalculatedValue;
                    }

                    //and ringbuffer Index +1
                    mUndistortionBitmapRowIndex++;
                    if (mUndistortionBitmapRowIndex >= cUndistortionRingbufferSize)
                    {
                        mUndistortionBitmapRowIndex = 0;
                    }


                    vPixelInQuadIndex++;
                    if (vPixelInQuadIndex >= 4)
                    {
                        vPixelInQuadIndex    = 0;
                        vUInt32IndexModal96 += 3;
                    }


                    vUserInteractionCounter++;

                    if ((vUserInteractionCounter >= 100) || (vPixelCounterForDisplay == iEarthArea.AreaPixelsInX))
                    {
                        vUserInteractionCounter = 0;
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("correcting texture distortion   column " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(iEarthArea.AreaPixelsInX));
                        Application.DoEvents();
                    }

                }

                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Finished processing texture undistortion. ");
                //Thread.Sleep(500);

            }
        }


        //only used for X undistortion  should be an inline function but hmm C# ?   
        private UInt32 GetSourcePixel24BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY)
        {
            Int32  vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
            Int32  vPixelInQuadIndex   = iPixelIndexX % 4;
            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (vPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }
            return vValue;
        }

        //only used for suppersing pitch balck pixel   
        private UInt32 GetSourcePixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY)
        {
            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (iPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }
            return vValue;
        }




        //only used for X undistortion  should be an inline function but hmm C# ?    
        private void SetDestinationPixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY, UInt32 iValue)
        {
            //  Input :  00 Rx Gx Bx
            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

                switch (iPixelInQuadIndex)
                {
                    case 0: //Pixel 1 in Quad
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0xFF000000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue & 0x00FFFFFF);
                        break;
                    case 1: //Pixel 2 in Quad
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue << 24);
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue >> 8);
                        break;
                    case 2: //Pixel 3 in Quad
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0x0000FFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue << 16);
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue >> 16);
                        break;
                    case 3: //Pixel 4 in Quad
                        mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0x000000FF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue << 8);
                        break;
                    default:
                        break; //error
                }
            




           
        }


        //only used for X resampler   
        private UInt32 GetSourcePixelFromResamplerArray24BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY)
        {
            Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
            Int32 vPixelInQuadIndex = iPixelIndexX % 4;
            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (vPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mAreaResampleArray[iPixelIndexY, vUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }

            return vValue;
        }


        private Int32 SignedInt32Weight(Double iValue)
        {
            //Signed Weight
            Int32 vResult = 0;
            Int32 vTemp;

            if (iValue < 0.0)
            {
                vResult = -512;
                iValue = -iValue;
            }
            if (iValue > 1.0)
            {
                vResult = vResult | 256;
            }
            else
            {
                vTemp = Convert.ToInt32(Math.Round(256.0 * iValue));
                vResult = vResult | vTemp;
            }
            return vResult;
        }

        private Int32 DiscreteInvWeightSum(Double iValue)
        {
            //Resolution 1/512 here!
            Double vInvValue = (1.0 / iValue);
            Int32 vFactor = Convert.ToInt32(Math.Round(512.0 * vInvValue));
            return vFactor;
        }

        public Int32 GetUndistortionBufferSize()
        {
            return cUndistortionRingbufferSize;
        }






        // ----  FS Resampler -----
        
        //that's what the official FS resmapler of the SDK's job had been. Can you do it any more quality killing than how tey did it?
        //this requires a lot free memory 
        //note undistortion, resample and brightness and contrast adjust can all be done in one step.. to do for future
        public Boolean FSResampleTexture(EarthArea iEarthArea, tResampleMode iResampleMode, tPictureEnhancement iPictureEnhancement, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            Boolean vContinue = true;

            EarthCommon.CollectGarbage();
           
            Int32 vKeepArrayWidth = mArrayWidth;

            vContinue = CreateAreaResampleArray(iEarthArea);

            if (!vContinue)
            {
                iFSEarthTilesInternalInterface.SetStatusFromFriendThread(" Not ENOUGH MEMORY for Resampler (step1). Try a smaller Area.");
                Thread.Sleep(2000);
            }

            if (vContinue)
            {
                FSResampleTextureY(iEarthArea, iResampleMode, iFSEarthTilesInternalInterface);
                
                //resize mAreaBitmapArray
                try
                {

                    ResamplerPrivateFreeBitmap(); //GarbageCollector doesn't free memory used in the same procedure

                    EarthCommon.CollectGarbage();

                    ResamplerPrivateCalcNewBitmapNewDimension(iEarthArea);

                    ResamplerPrivateReAllocateBitmap(iEarthArea);

                }
                catch
                {
                    iFSEarthTilesInternalInterface.SetStatusFromFriendThread(" Not ENOUGH MEMORY for Resampler (step2.) Try a smaller Area.");
                    Thread.Sleep(2000);
                    vContinue = false;
                }
                if (vContinue)
                {
                    CreateBrightNessAndContrastTable(iPictureEnhancement);

                    FSResampleTextureX(iEarthArea, tDoColorWork.eYesDoColorWork, iFSEarthTilesInternalInterface);
                    
                    //insert for debugging half way processed texture (attention steals memory if you do)
                    //for (int vCon1=0; vCon1<vAreaResampledBitmapSizeY; vCon1++)
                    //{
                    //    for (int vCon2 = 0; vCon2 < vKeepArrayWidth; vCon2++)
                    //    {
                    //        mAreaBitmapArray[vCon1, vCon2] = mAreaResampleArray[vCon1, vCon2];
                    //    }
                    //}

                    ResamplerPrivateFreeResampleArray();
                    
                    EarthCommon.CollectGarbage();

                }
            }

            return vContinue;
        }

        private Boolean CreateAreaResampleArray(EarthArea iEarthArea)
        {
            Int32 vAreaResampledBitmapSizeX = (Int32)(iEarthArea.AreaFSResampledPixelsInX);
            Int32 vAreaResampledBitmapSizeY = (Int32)(iEarthArea.AreaFSResampledPixelsInY);

            //create mAreaResampleArray
            try
            {
                mAreaResampleArray = new UInt32[vAreaResampledBitmapSizeY, mArrayWidth];  //Bitmap in Memory is [Y,X]. It does not work the other way!
                return true;
            }
            catch
            {
                return false;
            }
        }

        private Boolean CreatemAreaResampleAllocTestDummyArray()
        {
            //create mAreaResampleAllocTestDummyArray
            try
            {
                mAreaResampleAllocTestDummyArray = new UInt32[2048, 2048];  // 16-MByte Memory safety Buffer, We need to reserve/keep free some additional space that can be used up till then
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ResamplerPrivateCalcNewBitmapNewDimension(EarthArea iEarthArea)
        {
            Int32 vAreaResampledBitmapSizeX = (Int32)(iEarthArea.AreaFSResampledPixelsInX);
            Int32 vAreaResampledBitmapSizeY = (Int32)(iEarthArea.AreaFSResampledPixelsInY);

            //new dimensions
            // Changed to 4x
            mArrayWidth = (3 * vAreaResampledBitmapSizeX) >> 2;
            if (((3 * vAreaResampledBitmapSizeX) % 4) != 0)
            {
                mArrayWidth += 1;
            }
            mArrayHeight = vAreaResampledBitmapSizeY;
        }


        private void ResamplerPrivateFreeBitmap()
        {
            if (mMemoryAllocated)
            {
                mAreaGraphics.Dispose();
                mAreaBitmap.Dispose();
                mGCHandle.Free();
                mAreaBitmapArray = new UInt32[4, 4];
                mMemoryAllocated = false;
            }
        }

        private void ResamplerPrivateReAllocateBitmap(EarthArea iEarthArea)
        {
            Int32 vAreaResampledBitmapSizeX = (Int32)(iEarthArea.AreaFSResampledPixelsInX);
            Int32 vAreaResampledBitmapSizeY = (Int32)(iEarthArea.AreaFSResampledPixelsInY);

            mAreaBitmapArray = new UInt32[mArrayHeight, mArrayWidth];
            mGCHandle = GCHandle.Alloc(mAreaBitmapArray, GCHandleType.Pinned);
            IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mAreaBitmapArray, 0);
            mAreaBitmap = new Bitmap(vAreaResampledBitmapSizeX, vAreaResampledBitmapSizeY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
            //mAreaBitmap = new Bitmap(vAreaResampledBitmapSizeX, vAreaResampledBitmapSizeY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb , vPointer);
           
            mAreaGraphics = Graphics.FromImage(mAreaBitmap);
            mMemoryAllocated = true;
        }

        private void ResamplerPrivateFreeResampleArray()
        {
            mAreaResampleArray = new UInt32[4, 4];   //or = null
        }

        private void ResamplerPrivateFreeResampleAllocTestDummyArray()
        {
            mAreaResampleAllocTestDummyArray = new UInt32[4, 4];   //or = null
        }

        public void EnhanceColor(tPictureEnhancement iPictureEnhancement)
        {
            CreateBrightNessAndContrastTable(iPictureEnhancement);

            UInt32 vValue;

            for (Int32 vRowY = 0; vRowY < mArrayHeight; vRowY++)
            {
                for (Int32 vQuadColumnX = 0; vQuadColumnX < mArrayWidth; vQuadColumnX++)
                {
                    vValue = mAreaBitmapArray[vRowY, vQuadColumnX];
                    vValue = (mColorCorrectionTable[vValue >> 24] << 24) | (mColorCorrectionTable[(vValue & 0x00FF0000) >> 16] << 16) | (mColorCorrectionTable[(vValue & 0x0000FF00) >> 8] << 8) | (mColorCorrectionTable[vValue & 0x000000FF]);
                    mAreaBitmapArray[vRowY, vQuadColumnX] = vValue;
                }
            }
        }


        public void SuppressPitchBlackPixels()
        {
            UInt32 vValue;

            Int32 vTotalXPixels = mAreaBitmap.Width;
            Int32 vTotalYPixels = mAreaBitmap.Height;

            Int32 vPixelInQuadIndex;
            Int32 vUInt32IndexModal96;

            for (Int32 vRowY = 0; vRowY < vTotalYPixels; vRowY++)
            {
                vPixelInQuadIndex = 0;
                vUInt32IndexModal96 = 0;

                for (Int32 vColumnX = 0; vColumnX < vTotalXPixels; vColumnX++)
                {
                    vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vRowY);
                    
                    if (vValue == 0)
                    {
                        vValue = 0x00010101;
                        SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vRowY, vValue);
                    }
                    
                    vPixelInQuadIndex++;
                    if (vPixelInQuadIndex >= 4)
                    {
                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 += 3;
                    }
                }
            }
        }

        // ---- Y Resampler -----
        //      Source data: mAreaBitmapArray;
        // Destination data: mAreaResampleArray;
        public void FSResampleTextureY(EarthArea iEarthArea,  tResampleMode iResampleMode, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {

            Boolean vError = false;
            Int32[] vActualFilterWeights;

            //Does know exact mode only
            Double vSourceStartLatitude = iEarthArea.AreaSnapStartLatitude;
            Double vSourceStopLatitude = iEarthArea.AreaSnapStopLatitude;

            Double vScale = Convert.ToDouble(iEarthArea.AreaFSResampledPixelsInY) / Convert.ToDouble(iEarthArea.AreaPixelsInY);

            vActualFilterWeights = new Int32[2 * cHalfFilterWindow + 1];


            if (!vError)
            {
                Int64 vDestinationPixelRowInY;  //The actual Destination
                Int64 vSourcePixelRowInY;       //The actual Source
                Double vFilterOffsetX;           //The X Offset of to Source Pixel Center to the price calculated pixel/row map  = equal the Filter offser that is to aplay

                //Destination
                Double vLatitudePerPixel = (iEarthArea.AreaFSResampledStartLatitude - iEarthArea.AreaFSResampledStopLatitude) / Convert.ToDouble(iEarthArea.AreaFSResampledPixelsInY);
                Double vHalfLatitudePerPixel = 0.5 * vLatitudePerPixel;
                Double vInvLatitudePerPixel = 1.0 / vLatitudePerPixel;

                //Source
                Double vStartSourceLatitude = vSourceStartLatitude;
                Double vStopSourceLatitude = vSourceStopLatitude;
                Double vSourceLatitudePerPixel = (vStartSourceLatitude - vStopSourceLatitude) / Convert.ToDouble(iEarthArea.AreaPixelsInY);
                Double vInvSourceLatitudePerPixel = 1.0 / vSourceLatitudePerPixel;
                Double vHalfSourceLatitude = 0.5 * vSourceLatitudePerPixel;

                Double vStartNormedMercatorY = EarthMath.GetNormedMercatorY(vSourceStartLatitude);
                Double vStopNormedMercatorY = EarthMath.GetNormedMercatorY(vSourceStopLatitude);
                Double vNormedMercatorYPerPixel = (vStartNormedMercatorY - vStopNormedMercatorY) / Convert.ToDouble(iEarthArea.AreaPixelsInY);
                Double vInvNormedMercatorYPerPixel = 1.0 / vNormedMercatorYPerPixel;
                Double vHalfNormedMercatorY = 0.5 * vNormedMercatorYPerPixel;

                Double vActualLatitude;
                Double vActualSourceLatitude;
                Double vActualNormedMercatorY;
                Double vPixelRowInYOfOriginalTexture;

                Double vPreciseWeight;
                Double vWeightSum;
                Int32 vDiscreteInvWeightSum;

                Int64 vUserInteractionCounter = 0;
                Int64 vPixelCounterForDisplay = 0;

                //Inner Loop members

                const UInt32 cMask1 = 0xFF00FF00;
                const UInt32 cMask2 = 0x00FF00FF;
                const UInt32 cMask3 = 0x0FFC0FFC;
                const UInt32 cMask4 = 0x0FF00FF0;

                UInt32 vWork1;
                UInt32 vWork2;

                UInt32 vNewValue1;
                UInt32 vNewValue2;

                Int32 vWeight;
                UInt32 vWeightenedWork1;
                UInt32 vWeightenedWork2;
                UInt32 vCalculatedValue;

                Int32 vSourceY;
                Int32 vDestY;

                Int32 vFilterRow;

                Int32 vPixelInQuadIndex = 0;
                Int32 vUInt32IndexModal96 = 0;

                //Processing From Top Latitude to Bottom Latitude

                for (vDestinationPixelRowInY = 0; vDestinationPixelRowInY < iEarthArea.AreaFSResampledPixelsInY; vDestinationPixelRowInY++)
                {
                    vPixelCounterForDisplay++;

                    if (iResampleMode == tResampleMode.eMercator)
                    {
                        vActualLatitude = iEarthArea.AreaSnapStartLatitude - (Convert.ToDouble(vDestinationPixelRowInY) * vLatitudePerPixel + vHalfLatitudePerPixel);
                        vActualNormedMercatorY = EarthMath.GetNormedMercatorY(vActualLatitude);
                        
                        vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * ((vStartNormedMercatorY - vActualNormedMercatorY) - vHalfNormedMercatorY);
                        vFilterOffsetX = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                        vSourcePixelRowInY = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                        //alterantive test code for v0.9 (the same?)
                        //vPixelRowInYOfOriginalTexture = vInvNormedMercatorYPerPixel * (vStartNormedMercatorY - vActualNormedMercatorY);
                        //vSourcePixelRowInY = (Int64)(vPixelRowInYOfOriginalTexture); //Cut it down don't round
                        //vFilterOffsetX = (Convert.ToDouble(vSourcePixelRowInY) + 0.5) - vPixelRowInYOfOriginalTexture;

                    }
                    else
                    {
                        vActualLatitude = iEarthArea.AreaFSResampledStartLatitude - (Convert.ToDouble(vDestinationPixelRowInY) * vLatitudePerPixel + vHalfLatitudePerPixel);
                        vActualSourceLatitude = vActualLatitude;
                        
                        vPixelRowInYOfOriginalTexture = vInvSourceLatitudePerPixel * ((vStartSourceLatitude - vActualSourceLatitude) - vHalfSourceLatitude);
                        vFilterOffsetX = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                        vSourcePixelRowInY = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                        //alterantive test code for v0.9 (the same?)
                        //vPixelRowInYOfOriginalTexture = vInvSourceLatitudePerPixel * (vStartSourceLatitude - vActualSourceLatitude);
                        //vSourcePixelRowInY = (Int64)(vPixelRowInYOfOriginalTexture); //Cut it down don't round
                        //vFilterOffsetX = (Convert.ToDouble(vSourcePixelRowInY) + 0.5) - vPixelRowInYOfOriginalTexture;
                    }

                    vWeightSum = 0.0;

                    for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                    {
                        vPreciseWeight = EarthMath.Lanczos3((cLanczosResamplerWindowsScaleingPartFactor * (vScale - 1.0) + 1.0) * (Convert.ToDouble(vFilterRow) + vFilterOffsetX)); //vScale > 1 means Destination texture has more pixel, means the fonction in X has to shrink or the X value input enlarged. This includes the Offset as well.
                        vWeightSum += vPreciseWeight;
                        vActualFilterWeights[vFilterRow + cHalfFilterWindow] = SignedInt32Weight(vPreciseWeight);
                    }

                    vDiscreteInvWeightSum = DiscreteInvWeightSum(vWeightSum);

                    vSourceY = (Int32)vSourcePixelRowInY;
                    vDestY   = (Int32)vDestinationPixelRowInY;

                    //speed: for a memcopy we could use System.Buffer.BlockCopy or second best Array.Copy 
                    for (Int32 vCon = 0; vCon < mArrayWidth; vCon++)
                    {
                        vNewValue1 = 0;
                        vNewValue2 = 0;

                        for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                        {
                            if ((vFilterRow + vSourceY) < 0)
                            {
                                // v0.9: Mirroring Border in connection with a Resampler seems to be a bad idea. It's much much better without so disabled
                                // strange it worked very well in undistortion..

                                ///Mirror border
                                //Int32 vBorderIndex = -(vFilterRow + vSourceY);
                                //vWork1 = cMask1 & mAreaBitmapArray[vBorderIndex, vCon];
                                //vWork2 = cMask2 & mAreaBitmapArray[vBorderIndex, vCon];
                                
                                //v0.9 disable Mirror repeat last
                                vWork1 = cMask1 & mAreaBitmapArray[0, vCon];
                                vWork2 = cMask2 & mAreaBitmapArray[0, vCon];
                            }
                            else if ((vFilterRow + vSourceY) >= iEarthArea.AreaPixelsInY)
                            {
                                // v0.9: Mirroring Border in connection with a Resampler seems to be a bad idea. It's much much better without so disabled
                                // strange it worked very well in undistortion..

                                ///Mirror border
                                //Int32 vBorderIndex = ((Int32)iEarthArea.AreaPixelsInY << 1) - (vFilterRow + vSourceY) - 2;
                                //vWork1 = cMask1 & mAreaBitmapArray[vBorderIndex, vCon];
                                //vWork2 = cMask2 & mAreaBitmapArray[vBorderIndex, vCon];
                                
                                //v0.9 disable Mirror repeat last
                                vWork1 = cMask1 & mAreaBitmapArray[iEarthArea.AreaPixelsInY-1, vCon];
                                vWork2 = cMask2 & mAreaBitmapArray[iEarthArea.AreaPixelsInY-1, vCon];
                            }
                            else
                            {
                                //Normal
                                vWork1 = cMask1 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                                vWork2 = cMask2 & mAreaBitmapArray[vFilterRow + vSourceY, vCon];
                            }

                            vWork2 <<= 8;

                            vWeight = vActualFilterWeights[vFilterRow + cHalfFilterWindow];

                            if ((vWeight & 256) == 256)
                            {
                                vWeightenedWork1 = vWork1;
                                vWeightenedWork2 = vWork2;
                            }
                            else
                            {
                                vWeightenedWork1 = 0;
                                vWeightenedWork2 = 0;

                                if ((vWeight & 1) == 1)
                                {
                                    vWeightenedWork1 += vWork1 >> 8;
                                    vWeightenedWork2 += vWork2 >> 8;
                                }
                                if ((vWeight & 2) == 2)
                                {
                                    vWeightenedWork1 += vWork1 >> 7;
                                    vWeightenedWork2 += vWork2 >> 7;
                                }
                                if ((vWeight & 4) == 4)
                                {
                                    vWeightenedWork1 += vWork1 >> 6;
                                    vWeightenedWork2 += vWork2 >> 6;
                                }
                                if ((vWeight & 8) == 8)
                                {
                                    vWeightenedWork1 += vWork1 >> 5;
                                    vWeightenedWork2 += vWork2 >> 5;
                                }
                                if ((vWeight & 16) == 16)
                                {
                                    vWeightenedWork1 += vWork1 >> 4;
                                    vWeightenedWork2 += vWork2 >> 4;
                                }
                                if ((vWeight & 32) == 32)
                                {
                                    vWeightenedWork1 += vWork1 >> 3;
                                    vWeightenedWork2 += vWork2 >> 3;
                                }
                                if ((vWeight & 64) == 64)
                                {
                                    vWeightenedWork1 += vWork1 >> 2;
                                    vWeightenedWork2 += vWork2 >> 2;
                                }
                                if ((vWeight & 128) == 128)
                                {
                                    vWeightenedWork1 += vWork1 >> 1;
                                    vWeightenedWork2 += vWork2 >> 1;
                                }
                            }

                            vWeightenedWork1 >>= 4;
                            vWeightenedWork2 >>= 4;
                            vWeightenedWork1 &= cMask3;
                            vWeightenedWork2 &= cMask3;

                            if (vWeight >= 0)
                            {
                                vNewValue1 += vWeightenedWork1;
                                vNewValue2 += vWeightenedWork2;
                            }
                            else
                            {
                                vNewValue1 -= vWeightenedWork1;
                                vNewValue2 -= vWeightenedWork2;
                            }

                        }

                        //cut negative values
                        if ((vNewValue1 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue1 &= 0x0000FFFF;
                        }
                        if ((vNewValue1 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue1 &= 0xFFFF0000;
                        }
                        if ((vNewValue2 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue2 &= 0x0000FFFF;
                        }
                        if ((vNewValue2 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue2 &= 0xFFFF0000;
                        }

                        //Since we have a scaleing we really need to handle the total weight now
                        //Carefully they needs to stay 1 empty bit between the two color values in the NewValues Int32

                        vWeightenedWork1 = vNewValue1;
                        vWeightenedWork2 = vNewValue2;

                        if ((vDiscreteInvWeightSum & 1) == 1)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0C000C00) >> 9;
                            vNewValue2 += (vWeightenedWork2 & 0x0C000C00) >> 9;
                        }
                        if ((vDiscreteInvWeightSum & 2) == 2)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0E000E00) >> 8;
                            vNewValue2 += (vWeightenedWork2 & 0x0E000E00) >> 8;
                        }
                        if ((vDiscreteInvWeightSum & 4) == 4)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0F000F00) >> 7;
                            vNewValue2 += (vWeightenedWork2 & 0x0F000F00) >> 7;
                        }
                        if ((vDiscreteInvWeightSum & 8) == 8)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0F800F80) >> 6;
                            vNewValue2 += (vWeightenedWork2 & 0x0F800F80) >> 6;
                        }
                        if ((vDiscreteInvWeightSum & 16) == 16)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FC00FC0) >> 5;
                            vNewValue2 += (vWeightenedWork2 & 0x0FC00FC0) >> 5;
                        }
                        if ((vDiscreteInvWeightSum & 32) == 32)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FE00FE0) >> 4;
                            vNewValue2 += (vWeightenedWork2 & 0x0FE00FE0) >> 4;
                        }
                        if ((vDiscreteInvWeightSum & 64) == 64)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FF00FF0) >> 3;
                            vNewValue2 += (vWeightenedWork2 & 0x0FF00FF0) >> 3;
                        }
                        if ((vDiscreteInvWeightSum & 128) == 128)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FF80FF8) >> 2;
                            vNewValue2 += (vWeightenedWork2 & 0x0FF80FF8) >> 2;
                        }
                        if ((vDiscreteInvWeightSum & 256) == 256)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FFC0FFC) >> 1;
                            vNewValue2 += (vWeightenedWork2 & 0x0FFC0FFC) >> 1;
                        }

                        //rounding
                        if ((vNewValue1 & 0x00080000) == 0x00080000)
                        {
                            vNewValue1 += 0x00080000;
                        }

                        if ((vNewValue1 & 0x00000008) == 0x00000008)
                        {
                            vNewValue1 += 0x00000008;
                        }
                        if ((vNewValue2 & 0x00080000) == 0x00080000)
                        {
                            vNewValue2 += 0x00080000;
                        }

                        if ((vNewValue2 & 0x00000008) == 0x00000008)
                        {
                            vNewValue2 += 0x00000008;
                        }


                        //cut larger than 255
                        if ((vNewValue1 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0x0000FFFF;
                            vNewValue1 |= 0x0FF00000;
                        }
                        if ((vNewValue1 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0xFFFF0000;
                            vNewValue1 |= 0x00000FF0;
                        }
                        if ((vNewValue2 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0x0000FFFF;
                            vNewValue2 |= 0x0FF00000;
                        }
                        if ((vNewValue2 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0xFFFF0000;
                            vNewValue2 |= 0x00000FF0;
                        }

                        vNewValue1 &= cMask4;
                        vNewValue2 &= cMask4;

                        vCalculatedValue = (vNewValue1 << 4) | (vNewValue2 >> 4);

                        //we have the value! fast assign it!..fast fast..flushhh...too late!...hehe jokeing ;)
                        mAreaResampleArray[vDestY, vCon] = vCalculatedValue;
                    }

                    vPixelInQuadIndex++;
                    if (vPixelInQuadIndex >= 4)
                    {
                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 += 3;
                    }


                    vUserInteractionCounter++;

                    if ((vUserInteractionCounter >= 100) || (vPixelCounterForDisplay == iEarthArea.AreaFSResampledPixelsInY))
                    {
                        vUserInteractionCounter = 0;
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("resampling texture    row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(iEarthArea.AreaFSResampledPixelsInY));
                        Application.DoEvents();
                    }

                }

                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Finished processing texture resampleing. ");
                //Thread.Sleep(500);

            }
        }


        // ---- X Resampler -----
        //      Source data: mAreaResampleArray;  
        // Destination data: mAreaBitmapArray;  
        public void FSResampleTextureX(EarthArea iEarthArea, tDoColorWork iDoColorWork, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {

            Boolean vError = false;
            Int32[] vActualFilterWeights;

            //Does know exact mode only
            Double vSourceStartLongitude = iEarthArea.AreaSnapStartLongitude;
            Double vSourceStopLongitude =  iEarthArea.AreaSnapStopLongitude;

            Double vScale = Convert.ToDouble(iEarthArea.AreaFSResampledPixelsInX) / Convert.ToDouble(iEarthArea.AreaPixelsInX);

            vActualFilterWeights = new Int32[2 * cHalfFilterWindow + 1];


            if (!vError) 
            {
                Int64 vDestinationPixelRowInX;  //The actual Destination
                Int64 vSourcePixelRowInX;       //The actual Source
                Double vFilterOffsetX;           //The X Offset of to Source Pixel Center to the price calculated pixel/row map  = equal the Filter offser that is to aplay

                //Destination
                Double vLongitudePerPixel = (iEarthArea.AreaFSResampledStopLongitude - iEarthArea.AreaFSResampledStartLongitude) / Convert.ToDouble(iEarthArea.AreaFSResampledPixelsInX);
                Double vHalfLongitudePerPixel = 0.5 * vLongitudePerPixel;
                Double vInvLongitudePerPixel = 1.0 / vLongitudePerPixel;

                //Source
                Double vStopSourceLongitude = vSourceStopLongitude;
                Double vStartSourceLongitude = vSourceStartLongitude;
                Double vSourceLongitudePerPixel = (vStopSourceLongitude - vStartSourceLongitude) / Convert.ToDouble(iEarthArea.AreaPixelsInX);
                Double vInvSourceLongitudePerPixel = 1.0 / vSourceLongitudePerPixel;
                Double vHalfSourceLongitude = 0.5 * vSourceLongitudePerPixel;

                Double vActualLongitude;
                Double vActualSourceLongitude;
                Double vPixelRowInYOfOriginalTexture;

                Double vPreciseWeight;
                Double vWeightSum;
                Int32 vDiscreteInvWeightSum;

                Int64 vUserInteractionCounter = 0;
                Int64 vPixelCounterForDisplay = 0;

                //Inner Loop members

                const UInt32 cMask1 = 0xFF00FF00;
                const UInt32 cMask2 = 0x00FF00FF;
                const UInt32 cMask3 = 0x0FFC0FFC;
                const UInt32 cMask4 = 0x0FF00FF0;

                UInt32 vWork1;
                UInt32 vWork2;

                UInt32 vNewValue1;
                UInt32 vNewValue2;

                Int32 vWeight;
                UInt32 vWeightenedWork1;
                UInt32 vWeightenedWork2;
                UInt32 vCalculatedValue;

                Int32 vSourceX;
                Int32 vDestX;

                Int32 vFilterRow;

                Int32 vPixelInQuadIndex = 0;
                Int32 vUInt32IndexModal96 = 0;

                //Processing From East Longitude to West Longitude

                for (vDestinationPixelRowInX = 0; vDestinationPixelRowInX < iEarthArea.AreaFSResampledPixelsInX; vDestinationPixelRowInX++)
                {
                    vPixelCounterForDisplay++;

                    vActualLongitude = iEarthArea.AreaFSResampledStartLongitude + (Convert.ToDouble(vDestinationPixelRowInX) * vLongitudePerPixel + vHalfLongitudePerPixel);
                    vActualSourceLongitude = vActualLongitude;
 
                    vPixelRowInYOfOriginalTexture = vInvSourceLongitudePerPixel * ((vActualSourceLongitude - vStartSourceLongitude) - vHalfSourceLongitude);
                    vFilterOffsetX = Math.Round(vPixelRowInYOfOriginalTexture) - vPixelRowInYOfOriginalTexture;
                    vSourcePixelRowInX = Convert.ToInt64(Math.Round(vPixelRowInYOfOriginalTexture));

                    //alterantive test code for v0.9 (the same?)
                    //vPixelRowInYOfOriginalTexture = vInvSourceLongitudePerPixel * ((vActualSourceLongitude - vStartSourceLongitude));
                    //vSourcePixelRowInX = (Int64)(vPixelRowInYOfOriginalTexture); //Cut it down don't round
                    //vFilterOffsetX = (Convert.ToDouble(vSourcePixelRowInX) + 0.5) - vPixelRowInYOfOriginalTexture;


                    vWeightSum = 0.0;

                    for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                    {
                        vPreciseWeight = EarthMath.Lanczos3((cLanczosResamplerWindowsScaleingPartFactor * (vScale - 1.0) + 1.0) * (Convert.ToDouble(vFilterRow) + vFilterOffsetX)); //vScale > 1 means Destination texture has more pixel, means the fonction in X has to shrink or the X value input enlarged. This includes the Offset as well.
                        vWeightSum += vPreciseWeight;
                        vActualFilterWeights[vFilterRow + cHalfFilterWindow] = SignedInt32Weight(vPreciseWeight);
                    }

                    vDiscreteInvWeightSum = DiscreteInvWeightSum(vWeightSum);

                    vSourceX = (Int32)vSourcePixelRowInX;
                    vDestX = (Int32)vDestinationPixelRowInX;

                    //speed: for a memcopy we could use System.Buffer.BlockCopy or second best Array.Copy 
                    for (Int32 vCon = 0; vCon < iEarthArea.AreaFSResampledPixelsInY; vCon++)
                    {
                        vNewValue1 = 0;
                        vNewValue2 = 0;

                        for (vFilterRow = -cHalfFilterWindow; vFilterRow <= cHalfFilterWindow; vFilterRow++)
                        {
                            if ((vFilterRow + vSourceX) < 0)
                            {
                                // v0.9: Mirroring Border in connection with a Resampler seems to be a bad idea. It's much much better without so disabled
                                // strange it worked very well in undistortion..

                                // Especially the left border (this one) caused anomal look (mostly and strange in second last row).
                                // disable that healed this completly and looks prefect now

                                ///Mirror border
                                //Int32 vBorderIndex  =  - (vFilterRow + vSourceX);
                                //UInt32 vSourcePixel = GetSourcePixelFromResamplerArray24BitBitmapFormat(vBorderIndex, vCon);
                                //vWork1 = cMask1 & vSourcePixel;
                                //vWork2 = cMask2 & vSourcePixel;

                                //v0.9 disable Mirror repeat last
                                UInt32 vSourcePixel = GetSourcePixelFromResamplerArray24BitBitmapFormat(0, vCon);
                                vWork1 = cMask1 & vSourcePixel;
                                vWork2 = cMask2 & vSourcePixel;
                            }
                            else if ((vFilterRow + vSourceX) >= iEarthArea.AreaPixelsInX)
                            {
                                // v0.9: Mirroring Border in connection with a Resampler seems to be a bad idea. It's much much better without so disabled
                                // strange it worked very well in undistortion..

                                ///Mirror border
                                //Int32 vBorderIndex = ((Int32)iEarthArea.AreaPixelsInX << 1) - (vFilterRow + vSourceX) - 2;
                                //UInt32 vSourcePixel = GetSourcePixelFromResamplerArray24BitBitmapFormat(vBorderIndex, vCon);
                                //vWork1 = cMask1 & vSourcePixel;
                                //vWork2 = cMask2 & vSourcePixel;

                                //v0.9 disable Mirror repeat last
                                UInt32 vSourcePixel = GetSourcePixelFromResamplerArray24BitBitmapFormat((Int32)iEarthArea.AreaPixelsInX - 1, vCon);
                                vWork1 = cMask1 & vSourcePixel;
                                vWork2 = cMask2 & vSourcePixel;
                            }
                            else
                            {
                                //Normal
                                UInt32 vSourcePixel = GetSourcePixelFromResamplerArray24BitBitmapFormat(vFilterRow + vSourceX, vCon);
                                vWork1 = cMask1 & vSourcePixel;
                                vWork2 = cMask2 & vSourcePixel;
                            }

                            vWork2 <<= 8;

                            vWeight = vActualFilterWeights[vFilterRow + cHalfFilterWindow];

                            if ((vWeight & 256) == 256)
                            {
                                vWeightenedWork1 = vWork1;
                                vWeightenedWork2 = vWork2;
                            }
                            else
                            {
                                vWeightenedWork1 = 0;
                                vWeightenedWork2 = 0;

                                if ((vWeight & 1) == 1)
                                {
                                    vWeightenedWork1 += vWork1 >> 8;
                                    vWeightenedWork2 += vWork2 >> 8;
                                }
                                if ((vWeight & 2) == 2)
                                {
                                    vWeightenedWork1 += vWork1 >> 7;
                                    vWeightenedWork2 += vWork2 >> 7;
                                }
                                if ((vWeight & 4) == 4)
                                {
                                    vWeightenedWork1 += vWork1 >> 6;
                                    vWeightenedWork2 += vWork2 >> 6;
                                }
                                if ((vWeight & 8) == 8)
                                {
                                    vWeightenedWork1 += vWork1 >> 5;
                                    vWeightenedWork2 += vWork2 >> 5;
                                }
                                if ((vWeight & 16) == 16)
                                {
                                    vWeightenedWork1 += vWork1 >> 4;
                                    vWeightenedWork2 += vWork2 >> 4;
                                }
                                if ((vWeight & 32) == 32)
                                {
                                    vWeightenedWork1 += vWork1 >> 3;
                                    vWeightenedWork2 += vWork2 >> 3;
                                }
                                if ((vWeight & 64) == 64)
                                {
                                    vWeightenedWork1 += vWork1 >> 2;
                                    vWeightenedWork2 += vWork2 >> 2;
                                }
                                if ((vWeight & 128) == 128)
                                {
                                    vWeightenedWork1 += vWork1 >> 1;
                                    vWeightenedWork2 += vWork2 >> 1;
                                }
                            }

                            vWeightenedWork1 >>= 4;
                            vWeightenedWork2 >>= 4;
                            vWeightenedWork1 &= cMask3;
                            vWeightenedWork2 &= cMask3;

                            if (vWeight >= 0)
                            {
                                vNewValue1 += vWeightenedWork1;
                                vNewValue2 += vWeightenedWork2;
                            }
                            else
                            {
                                vNewValue1 -= vWeightenedWork1;
                                vNewValue2 -= vWeightenedWork2;
                            }

                        }

                        //cut negative values
                        if ((vNewValue1 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue1 &= 0x0000FFFF;
                        }
                        if ((vNewValue1 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue1 &= 0xFFFF0000;
                        }
                        if ((vNewValue2 & 0x80000000) == 0x80000000)  //Value is negative
                        {
                            vNewValue2 &= 0x0000FFFF;
                        }
                        if ((vNewValue2 & 0x00008000) == 0x00008000)  //Value is negative
                        {
                            vNewValue2 &= 0xFFFF0000;
                        }

                        //Since we have a scaleing we really need to handle the total weight now
                        //Carefully they needs to stay 1 empty bit between the two color values in the NewValues Int32
                        
                        vWeightenedWork1 = vNewValue1;
                        vWeightenedWork2 = vNewValue2;

                        if ((vDiscreteInvWeightSum & 1) == 1)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0C000C00) >> 9;
                            vNewValue2 += (vWeightenedWork2 & 0x0C000C00) >> 9;
                        }
                        if ((vDiscreteInvWeightSum & 2) == 2)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0E000E00) >> 8;
                            vNewValue2 += (vWeightenedWork2 & 0x0E000E00) >> 8;
                        }
                        if ((vDiscreteInvWeightSum & 4) == 4)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0F000F00) >> 7;
                            vNewValue2 += (vWeightenedWork2 & 0x0F000F00) >> 7;
                        }
                        if ((vDiscreteInvWeightSum & 8) == 8)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0F800F80) >> 6;
                            vNewValue2 += (vWeightenedWork2 & 0x0F800F80) >> 6;
                        }
                        if ((vDiscreteInvWeightSum & 16) == 16)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FC00FC0) >> 5;
                            vNewValue2 += (vWeightenedWork2 & 0x0FC00FC0) >> 5;
                        }
                        if ((vDiscreteInvWeightSum & 32) == 32)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FE00FE0) >> 4;
                            vNewValue2 += (vWeightenedWork2 & 0x0FE00FE0) >> 4;
                        }
                        if ((vDiscreteInvWeightSum & 64) == 64)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FF00FF0) >> 3;
                            vNewValue2 += (vWeightenedWork2 & 0x0FF00FF0) >> 3;
                        }
                        if ((vDiscreteInvWeightSum & 128) == 128)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FF80FF8) >> 2;
                            vNewValue2 += (vWeightenedWork2 & 0x0FF80FF8) >> 2;
                        }
                        if ((vDiscreteInvWeightSum & 256) == 256)
                        {
                            vNewValue1 += (vWeightenedWork1 & 0x0FFC0FFC) >> 1;
                            vNewValue2 += (vWeightenedWork2 & 0x0FFC0FFC) >> 1;
                        }

                        //rounding
                        if ((vNewValue1 & 0x00080000) == 0x00080000)
                        {
                            vNewValue1 += 0x00080000;
                        }

                        if ((vNewValue1 & 0x00000008) == 0x00000008)
                        {
                            vNewValue1 += 0x00000008;
                        }
                        if ((vNewValue2 & 0x00080000) == 0x00080000)
                        {
                            vNewValue2 += 0x00080000;
                        }

                        if ((vNewValue2 & 0x00000008) == 0x00000008)
                        {
                            vNewValue2 += 0x00000008;
                        }


                        //cut larger than 255
                        if ((vNewValue1 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0x0000FFFF;
                            vNewValue1 |= 0x0FF00000;
                        }
                        if ((vNewValue1 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue1 &= 0xFFFF0000;
                            vNewValue1 |= 0x00000FF0;
                        }
                        if ((vNewValue2 & 0x30000000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0x0000FFFF;
                            vNewValue2 |= 0x0FF00000;
                        }
                        if ((vNewValue2 & 0x00003000) != 0) //Value is >255
                        {
                            vNewValue2 &= 0xFFFF0000;
                            vNewValue2 |= 0x00000FF0;
                        }

                        vNewValue1 &= cMask4;
                        vNewValue2 &= cMask4;

                        if (iDoColorWork == tDoColorWork.eYesDoColorWork)                      
                        {
                            vNewValue1 = (mColorCorrectionTable[vNewValue1 >> 20] << 20) | (mColorCorrectionTable[(vNewValue1 >> 4) & 0x000000FF] << 4);
                            vNewValue2 = (mColorCorrectionTable[vNewValue2 >> 20] << 20) | (mColorCorrectionTable[(vNewValue2 >> 4) & 0x000000FF] << 4);
                        }

                        vCalculatedValue = (vNewValue1 << 4) | (vNewValue2 >> 4);

                        //we have the value! fast assign it!..fast fast..flushhh...too late!...hehe jokeing ;)
                        SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vCon, vCalculatedValue);

                    }

                    vPixelInQuadIndex++;
                    if (vPixelInQuadIndex >= 4)
                    {
                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 += 3;
                    }


                    vUserInteractionCounter++;

                    if ((vUserInteractionCounter >= 100) || (vPixelCounterForDisplay == iEarthArea.AreaFSResampledPixelsInX))
                    {
                        vUserInteractionCounter = 0;
                        iFSEarthTilesInternalInterface.SetStatusFromFriendThread("resampling texture    column " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(iEarthArea.AreaFSResampledPixelsInX));
                        Application.DoEvents();
                    }

                }

                //iFSEarthTilesInternalInterface.SetStatusFromFriendThread("Finished processing texture resampleing. ");
                //Thread.Sleep(500);

            }
        }

        private void CreateBrightNessAndContrastTable(tPictureEnhancement iPictureEnhancement)
        {
            mColorCorrectionTable = new UInt32[256];

            Double vBrightness    = Convert.ToDouble(iPictureEnhancement.mBrightness) * 1.275; //+/-127.5 pixel
            Double vContrast      = Convert.ToDouble(iPictureEnhancement.mContrast) * 1.275; //+/-127.5 pixel
            Double vContrastGain  = 1.0;
            Double vNegativeGain  = 1.0;
            Double vStartAdd      = 0.0;

            if (iPictureEnhancement.mContrast == 0)
            {
                //Nothing to do
            }
            else if (iPictureEnhancement.mContrast == 100)
            {
                vContrastGain = 512.0;
            }
            else if (iPictureEnhancement.mContrast == -100)
            {
                vNegativeGain = 0.0;
                vStartAdd     = 127.5;
            }
            else if (iPictureEnhancement.mContrast > 0)
            {
                vContrastGain = 255.0 / (255.0 - (2.0 * vContrast));
            }
            else
            {
                vContrastGain = - (255.0 - (2.0 * vContrast)) / 255.0;
                vStartAdd     = -vContrast;
            }

            for (Int32 vValue = 0; vValue < 256; vValue++)
            {
                //Brightness
                Double vValue2 = Convert.ToDouble(vValue) + vBrightness;              
                Double vValue3;

                //Contrast
                if (iPictureEnhancement.mContrast >= 0)
                {
                    vValue3 = vValue2 - vContrast;
                    vValue3 = vContrastGain * vValue3;
                }
                else
                {
                    vValue3 = vNegativeGain * vValue2;
                    vValue3 = vValue3 + vStartAdd;
                }

                vValue3 = Math.Round(vValue3);
                if (vValue3 > 255.0)
                {
                    vValue3 = 255.0;
                }
                if (vValue3 < 0.0)
                {
                    vValue3 = 0.0;
                }
                UInt32 vValueResult = Convert.ToUInt32(vValue3);
                mColorCorrectionTable[vValue] = vValueResult;
            }
        }

        public Boolean AllocateTextureMemory(EarthArea iEarthArea)
        {

            FreeTextureMemory(); //that function calls CollectGarbage so we don't do it here again

            DoAllocateTextureMemory(iEarthArea);

            return mMemoryAllocated;
        }


        private void DoAllocateTextureMemory(EarthArea iEarthArea)
        {
            
            Int32 vAreaBitmapSizeX = (Int32)(iEarthArea.AreaPixelsInX);
            Int32 vAreaBitmapSizeY = (Int32)(iEarthArea.AreaPixelsInY);

            //Thats a really neat trick to access the Bitmap Data without useing /unsafe pointer code check:  http://www.codeproject.com/cs/media/pointerlessimageproc.asp
            mAreaBitmap      = new Bitmap(4, 4);                //initialize to avoid compiler warning
            mAreaGraphics    = Graphics.FromImage(mAreaBitmap);
            mAreaBitmapArray = new UInt32[4, 4];
            mGCHandle        = GCHandle.Alloc(mAreaBitmapArray);

            Bitmap vReserveMemory = new Bitmap(4, 4);    //Texture Undistortion reuires space for the ring buffer
            Bitmap vReserveAdditionalMemory = new Bitmap(4, 4);    //Some little free Additional Memory is required to run the process

            try
            {
                //old: vAreaBitmap = new Bitmap(vAreaBitmapSizeX, vAreaBitmapSizeY, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //check http://www.codeproject.com/cs/media/pointerlessimageproc.asp
                //expanded it a little to 2 dim array although it might be unsafe if they every change the way a 2D Array is allocated in the memory
                

                // change the 4 back to 3
                mArrayWidth = (3 * vAreaBitmapSizeX) >> 2;
                

                if (((3 * vAreaBitmapSizeX) % 4) != 0)
                {
                    mArrayWidth += 1;
                }

                mArrayHeight = vAreaBitmapSizeY;

                Int32 vUndistortionBufferSize = GetUndistortionBufferSize();

                
                if (3 * vAreaBitmapSizeX > 4 * vAreaBitmapSizeY)
                {
                    vReserveMemory = new Bitmap(vUndistortionBufferSize, vAreaBitmapSizeX);
                }
                else
                {
                    vReserveMemory = new Bitmap(vUndistortionBufferSize, vAreaBitmapSizeY);
                }

                vReserveAdditionalMemory = new Bitmap(512, 512);


                mGCHandle.Free();
                mAreaBitmapArray = new UInt32[mArrayHeight, mArrayWidth];  //Bitmap in Memory is [Y,X]. It does not work the other way!

                mGCHandle       = GCHandle.Alloc(mAreaBitmapArray, GCHandleType.Pinned);
                IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mAreaBitmapArray, 0);
                mAreaBitmap     = new Bitmap(vAreaBitmapSizeX, vAreaBitmapSizeY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
                //mAreaBitmap = new Bitmap(vAreaBitmapSizeX, vAreaBitmapSizeY, vAreaBitmapSizeX*4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, vPointer);

                mAreaGraphics   = Graphics.FromImage(mAreaBitmap);

                vReserveAdditionalMemory.Dispose();
                vReserveMemory.Dispose();

                mMemoryAllocated = true;
            }
            catch
            {
                vReserveAdditionalMemory.Dispose();
                vReserveMemory.Dispose();

                mMemoryAllocated = false;
            }
        }


        public void FreeTextureMemory()
        {
            DoFreeMemory();

            EarthCommon.CollectGarbage();

        }


        private void DoFreeMemory()
        {
            if (mMemoryAllocated)
            {
                mAreaGraphics.Dispose();
                mAreaBitmap.Dispose();
                mGCHandle.Free();
                mAreaBitmapArray = new UInt32[4, 4];

                mMemoryAllocated = false;
            }
        }


        public Boolean TestMemoryAllocation(EarthArea iEarthArea, tMemoryAllocTestMode iAllocTestMode, String iAreaText, FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            Boolean vAllocTestPassed = false;

            FreeTextureMemory(); //that function calls CollectGarbage so we don't do it here again

            DoAllocateTextureMemory(iEarthArea);

            if (mMemoryAllocated)
            {
                vAllocTestPassed = true;
            }
            else
            {
                FreeTextureMemory();
                vAllocTestPassed = false;
                iFSEarthTilesInternalInterface.SetStatus(" -> NOT ENOUGH MEMORY!   (.NET / Win32Appl limitation)   Try a smaller " + iAreaText + ".");
                Thread.Sleep(2000);
            }

            if (vAllocTestPassed)
            {
               if (iAllocTestMode == tMemoryAllocTestMode.eResampler)
               {

                   vAllocTestPassed = CreateAreaResampleArray(iEarthArea);

                   if (vAllocTestPassed)
                   {
                       vAllocTestPassed = CreatemAreaResampleAllocTestDummyArray();
                   }

                   if (!vAllocTestPassed)
                   {
                       ResamplerPrivateFreeResampleArray();
                       ResamplerPrivateFreeResampleAllocTestDummyArray();
                       FreeTextureMemory();
                       iFSEarthTilesInternalInterface.SetStatus(" Not ENOUGH MEMORY for Resampler (step1). Try a smaller " + iAreaText + ".");
                       Thread.Sleep(2000);
                   }
                   if (vAllocTestPassed)
                   {
                       //resize mAreaBitmapArray
                       try
                       {

                           ResamplerPrivateFreeBitmap(); //GarbageCollector doesn't free memory used in the same procedure

                           EarthCommon.CollectGarbage();

                           ResamplerPrivateCalcNewBitmapNewDimension(iEarthArea);

                           ResamplerPrivateReAllocateBitmap(iEarthArea);

                       }
                       catch
                       {
                           ResamplerPrivateFreeResampleArray();
                           ResamplerPrivateFreeResampleAllocTestDummyArray();
                           FreeTextureMemory();
                           iFSEarthTilesInternalInterface.SetStatus(" Not ENOUGH MEMORY for Resampler (step2.) Try a smaller " + iAreaText + ".");
                           Thread.Sleep(2000);
                           vAllocTestPassed = false;
                       }
                       if (vAllocTestPassed)
                       {

                           ResamplerPrivateFreeResampleArray();
                           ResamplerPrivateFreeResampleAllocTestDummyArray();
                           EarthCommon.CollectGarbage();

                       }
                   }
               }
            }


            FreeTextureMemory();

            return vAllocTestPassed;
        }


        public void SaveBitmap(String iDestination)
        {
            if (mMemoryAllocated)
            {
                EarthCommon.CollectGarbage();
                try
                {

                   mAreaBitmap.Save(iDestination, System.Drawing.Imaging.ImageFormat.Bmp  );
                }
                catch (System.Exception e)
                {
                    String vError = e.ToString();
                    MessageBox.Show(vError, "Could not save Area Bitmap! HardDiskFull?");
                    Thread.Sleep(2000); //give user time after ok to react
                }
            }
        }


        public void CreateAndSaveThumbnail(String iDestination)
        {
            //Thumbnail
            if (mMemoryAllocated)
            {
                try
                {
                    Image.GetThumbnailImageAbort myThumbnailCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);
                    Image vThumbnail = mAreaBitmap.GetThumbnailImage(128, 128, myThumbnailCallback, IntPtr.Zero);
                    vThumbnail.Save(iDestination, System.Drawing.Imaging.ImageFormat.Bmp);
                }
                catch (System.Exception e)
                {
                    String vError = e.ToString();
                    MessageBox.Show(vError, "Could not save Area Thumbnail! HardDiskFull?");
                    Thread.Sleep(2000); //give user time after ok to react
                }
            }
        }


        public bool ThumbnailCallback()
        {
            return false;
        }

        public void DrawTile(Tile iTileReference, Int32 iStartPosX, Int32 iStartPosY)
        {
            if (mMemoryAllocated)
            {
                //mAreaGraphics.DrawImage(iTileReference.GetBitmapReference(), iStartPosX, iStartPosY, 256, 256);
                mAreaGraphics.DrawImage(iTileReference.GetBitmapReference(), new Rectangle(iStartPosX, iStartPosY, 256, 256), new Rectangle(0, 0, 256, 256), GraphicsUnit.Pixel);
            }
        }



        private void MaxBitmapSizeEvaluationFreeBitmap1()
        {
            if (mMaxBitmap1Allocated)
            {
                mMaxBitmap1.Dispose();
                mMaxBitmap1GCHandle.Free();
                mMaxBitmap1Array = new UInt32[4, 4];
                mMaxBitmap1Allocated = false;
            }
        }

        private void MaxBitmapSizeEvaluationFreeBitmap2()
        {
            if (mMaxBitmap2Allocated)
            {
                mMaxBitmap2.Dispose();
                mMaxBitmap2GCHandle.Free();
                mMaxBitmap2Array = new UInt32[4, 4];
                mMaxBitmap2Allocated = false;
            }
        }

        private void MaxBitmapSizeEvaluationAllocateBitmap1(Int32 iXPixel, Int32 iYPixel)
        {

            Int32 vArrayWidth;
            Int32 vArrayHeight;

            //new dimensions
            // change the 4's back to 3
            vArrayWidth = (4 * iXPixel) >> 2;
            if (((4 * iXPixel) % 4) != 0)
            {
                vArrayWidth += 1;
            }
            vArrayHeight = iYPixel;

            mMaxBitmap1Array = new UInt32[vArrayHeight, vArrayWidth];
            mMaxBitmap1GCHandle = GCHandle.Alloc(mMaxBitmap1Array, GCHandleType.Pinned);
            IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mMaxBitmap1Array, 0);
            mMaxBitmap1 = new Bitmap(iXPixel, iYPixel, vArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
            //mMaxBitmap1 = new Bitmap(iXPixel, iYPixel, vArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb , vPointer);
            mMaxBitmap1Allocated = true;
        }

        private void MaxBitmapSizeEvaluationAllocateBitmap2(Int32 iXPixel, Int32 iYPixel)
        {

            Int32 vArrayWidth;
            Int32 vArrayHeight;

            //new dimensions
            // change the 4 to 3, both lines
            vArrayWidth = (3 * iXPixel) >> 2;
            if (((3 * iXPixel) % 4) != 0)
            {
                vArrayWidth += 1;
            }
            vArrayHeight = iYPixel;

            mMaxBitmap2Array = new UInt32[vArrayHeight, vArrayWidth];
            mMaxBitmap2GCHandle = GCHandle.Alloc(mMaxBitmap2Array, GCHandleType.Pinned);
            IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mMaxBitmap2Array, 0);
            mMaxBitmap2 = new Bitmap(iXPixel, iYPixel, vArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
            //mMaxBitmap2 = new Bitmap(iXPixel, iYPixel, vArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb , vPointer);
            mMaxBitmap2Allocated = true;
        }


        public void MaxBitmapSizeEvaluation()
        {
            
            Boolean vAllocateDone = false;

            Int32 vStepSize = 20000;
            Int32 vPixelInX = vStepSize; //start max size 20000*20000 = 1.2 GByte each texture (note bgl size limit 2GByte)
            Int32 vPixelInY = vStepSize;

            Int32 vLastGoodPixelInX = 1024;
            Int32 vLastGoodPixelInY = 1024;

            EarthCommon.CollectGarbage();

            for (Int32 vStep = 1; vStep <= 15; vStep++)
            {
                vAllocateDone = false;

                try
                {
                    MaxBitmapSizeEvaluationAllocateBitmap1(vPixelInX, vPixelInY);
                    MaxBitmapSizeEvaluationAllocateBitmap2(vPixelInX, vPixelInY);
                    vAllocateDone = true;
                }
                catch
                {
                    //Nothing to do
                }
                MaxBitmapSizeEvaluationFreeBitmap1();
                MaxBitmapSizeEvaluationFreeBitmap2();
                EarthCommon.CollectGarbage();

                vStepSize = vStepSize >> 1; // div 2;

                if (vAllocateDone)
                {
                    vLastGoodPixelInX = vPixelInX;
                    vLastGoodPixelInY = vPixelInY;

                    if (vStep == 1)
                    {
                        break; //20000 max don't go higher
                    }

                    vPixelInX += vStepSize;
                    vPixelInY += vStepSize;
                }
                else
                {
                    vPixelInX -= vStepSize;
                    vPixelInY -= vStepSize;
                }
            }

            mMaxPixelInX   = (Int64)(vLastGoodPixelInX);
            mMaxPixelInY   = (Int64)(vLastGoodPixelInY);
            mMaxTotalAllowedPixels = (95 * mMaxPixelInX * mMaxPixelInY) / 100; //5% Reserve
        }
         
        public Boolean IsAreaSizeOk(EarthArea iEarthArea, tMemoryAllocTestMode iMemoryAllocTestMode, Double iSafetyFactor)
        {
            if (!IsSizeOk(iEarthArea.AreaPixelsInX, iEarthArea.AreaPixelsInY, iSafetyFactor))
            {
                return false;
            }
            if (tMemoryAllocTestMode.eResampler == iMemoryAllocTestMode)
            {
                //precondition AreaResampleFSXCalculated
                if (!IsSizeOk(iEarthArea.AreaFSResampledPixelsInX, iEarthArea.AreaFSResampledPixelsInY, iSafetyFactor))
                {
                    return false;
                }
            }
            return true;
        }


        public Boolean IsSizeOk(Int64 iXPixel, Int64 iYPixel, Double iSafetyFactor)
        {
            if (((Double)(iXPixel * iYPixel)) <= iSafetyFactor * EarthConfig.mMaxMemoryUsageFactor * (Double)(mMaxTotalAllowedPixels))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private EarthAreaTexture ShallowCopy()
        {
            return (EarthAreaTexture)this.MemberwiseClone();
        }


        private EarthAreaTexture DeepCopy()
        {
            EarthAreaTexture dc = ShallowCopy();
            if (mAreaBitmap != null)
            {
                dc.mAreaBitmap = (Bitmap)mAreaBitmap.Clone();
            }
            if (mAreaBitmapArray != null)
            {
                dc.mAreaBitmapArray = (uint[,])mAreaBitmapArray.Clone();
            }
            if (mUndistortionBitmapRowRingbuffer != null)
            {
                dc.mUndistortionBitmapRowRingbuffer = (uint[,])mUndistortionBitmapRowRingbuffer.Clone();
            }
            if (mAreaResampleArray != null)
            {
                dc.mAreaResampleArray = (uint[,])mAreaResampleArray.Clone();
            }
            if (mAreaResampleAllocTestDummyArray != null)
            {
                dc.mAreaResampleAllocTestDummyArray = (uint[,])mAreaResampleAllocTestDummyArray.Clone();
            }
            if (mColorCorrectionTable != null)
            {
                dc.mColorCorrectionTable = (uint[])mColorCorrectionTable.Clone();
            }
            if (mMaxBitmap1Array != null)
            {
                dc.mMaxBitmap1Array = (uint[,])mMaxBitmap1Array.Clone();
            }
            if (mMaxBitmap2Array != null)
            {
                dc.mMaxBitmap2Array = (uint[,])mMaxBitmap2Array.Clone();
            }

            return dc;
        }


        public EarthAreaTexture Clone()
        {
            return DeepCopy();
        }


        //Direct Area Bitmap Data Bitmap Array
        protected UInt32[,] mAreaBitmapArray;                        //RGB 24Bit Bitmap Data Array
        protected UInt32[,] mUndistortionBitmapRowRingbuffer;        //used for undistorting the texture
        protected Int32 mUndistortionBitmapRowIndex;                 //the index
        protected Int32 mArrayWidth;                                 //mArrayWidth = count of 32Bit Integer in one Bitmap line (BitmapFormat RGB 24Bit)
        protected Int32 mArrayHeight;                                //equal Bitmap height Y

        protected Bitmap   mAreaBitmap;                                //The Bitmap which become initialized and accessed with mAreaBitmapArray
        protected Graphics mAreaGraphics;                            //The Graphis to Draw into the Bitmap
        protected GCHandle mGCHandle;                                //Required stuff to make that direct access Bitmap with Array working
        protected Boolean  mMemoryAllocated;                          //true when Memory is allocated

        protected UInt32[,] mAreaResampleArray;                      //RGB 24Bit temporary  Data Array (simular to bitmap) for Resampler
        protected UInt32[,] mAreaResampleAllocTestDummyArray;        //RGB 24Bit temporary  used for MemoryAllocation Pre-Testing

        protected UInt32[] mColorCorrectionTable;                     //ColorCorrectionTable

        protected Int64     mMaxPixelInX;                            //The Maximum Bitmap Size In X that can be processed with the available memory 
        protected Int64     mMaxPixelInY;                            //The Maximum Bitmap Size in Y that can be processed with the available memory (but only the product X*Y is of significance)
        protected Int64     mMaxTotalAllowedPixels;                  //95% of mMaxPixelInX * mMaxPixelInY, this is the limit we allow (Automode)

        protected UInt32[,] mMaxBitmap1Array;                        //RGB 24Bit Bitmap Data Array
        protected Bitmap    mMaxBitmap1;                             //Bitmap1 for MaxBitmapSize Evaluation
        protected GCHandle  mMaxBitmap1GCHandle;                     //
        protected Boolean   mMaxBitmap1Allocated;                    //

        protected UInt32[,] mMaxBitmap2Array;                        //RGB 24Bit Bitmap Data Array
        protected Bitmap    mMaxBitmap2;                             //Bitmap2 for MaxBitmapSize Evaluation
        protected GCHandle  mMaxBitmap2GCHandle;                     //
        protected Boolean   mMaxBitmap2Allocated;                    //

    }
}
