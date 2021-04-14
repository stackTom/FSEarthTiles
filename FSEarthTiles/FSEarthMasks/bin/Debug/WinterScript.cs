using System;
using System.Collections.Generic;
using System.Text;
using FSEarthMasksInternalDLL;

namespace FSEarthMasksDLL
{
    public class WinterScript
    {
        //Season routines are taken and converted of Thomas M. Seasons java routines
        
        //The following Methodes will be called by FSEarthMasks:
        //MakeWinter(MasksTexture iTexture)

        public void MakeWinter(MasksTexture iTexture)
        {
            Random vRandomGenerator = new Random();

            Int32 vRed = 0;
            Int32 vGreen = 0;
            Int32 vBlue = 0;
            Int32 vSum = 0;

            Int32 vPixelCountInX = iTexture.GetPixelCountInX();
            Int32 vPixelCountInY = iTexture.GetPixelCountInY();

            for (Int32 vY = 0; vY < vPixelCountInY; vY++)
            {
                for (Int32 vX = 0; vX < vPixelCountInX; vX++)
                {

                    iTexture.GetPixelRGB(vX, vY, ref vRed, ref vGreen, ref vBlue);

                    Boolean vIsWater = iTexture.IsWaterOrWaterTransition(vX, vY);
                    Boolean vDontAlterColor = MasksConfig.mSpareOutWaterForSeasonsGeneration && vIsWater;
                    //Boolean vSnowAllowed    = !(MasksConfig.mNoSnowInWaterForWinterAndHardWinter && vIsWater);

                    if (!vDontAlterColor)
                    {
                        vSum = vRed + vGreen + vBlue;

                        // Basically fall routines with snow on greyish fields and streets:
                        if (((vRed - vBlue)   < MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) && ((vRed - vBlue)   > -MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) &&
                            ((vRed - vGreen)  < MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) && ((vRed - vGreen)  > -MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) &&
                            ((vGreen - vBlue) < MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) && ((vGreen - vBlue) > -MasksConfig.mWinterStreetGreyConditionGreyToleranceValue) &&
                             (vSum > MasksConfig.mWinterStreetGreyConditionRGBSumLargerThanValue))
                        {
                            Int32 vMax = vRed;
                            if (vGreen > vMax)
                            {
                                vMax = vGreen;
                            }
                            if (vBlue > vMax)
                            {
                                vMax = vBlue;
                            }
                            Single vMaxDouble = MasksConfig.mWinterStreetGreyMaxFactor * (Single)(vMax);
                            vRed   = (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mWinterStreetGreyRandomFactor + vMax));
                            vGreen = (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mWinterStreetGreyRandomFactor + vMax));
                            vBlue  = (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mWinterStreetGreyRandomFactor + vMax));
                        }
                        else if ((vSum < MasksConfig.mWinterDarkConditionRGBSumLessThanValue) &&
                                 (vSum > MasksConfig.mWinterDarkConditionRGBSumLargerThanValue))
                        {
                            // Rather dark pixel, but not black
                            vRed   += MasksConfig.mWinterDarkRedAddition;
                            vGreen += MasksConfig.mWinterDarkGreenAddition;
                            vBlue  += MasksConfig.mWinterDarkBlueAddition;
                        }
                        else if ((vSum >= MasksConfig.mWinterBrightConditionRGBSumLargerEqualThanValue) &&
                                 (vSum < MasksConfig.mWinterBrightConditionRGBSumLessThanValue))
                        {
                            // rather bright pixel
                            vRed   += MasksConfig.mWinterBrightRedAddition;
                            vGreen += MasksConfig.mWinterBrightGreenAddition;
                            vBlue  += MasksConfig.mWinterBrightBlueAddition;
                        }
                        else if ((MasksConfig.mWinterGreenishConditionBlueIntegerFactor * vBlue) < (MasksConfig.mWinterGreenishConditionGreenIntegerFactor * vGreen))  //1.4*blue < Green
                        {
                            //very greenish pixel
                            vRed   += MasksConfig.mWinterGreenishRedAddition;
                            vGreen += MasksConfig.mWinterGreenishGreenAddition;
                            vBlue  += MasksConfig.mWinterGreenishBlueAddition;
                        }
                        else
                        {
                            vRed   += MasksConfig.mWinterRestRedAddition;
                            vGreen += MasksConfig.mWinterRestGreenAddition;
                            vBlue  += MasksConfig.mWinterRestBlueAddition;
                        }

                        iTexture.LimitRGBValues(ref vRed, ref vGreen, ref vBlue);

                    }

                    iTexture.SetPixelRGB(vX, vY, vRed, vGreen, vBlue);
                }
            }
        }
    }
}
