using System;
using System.Collections.Generic;
using System.Text;
using FSEarthMasksInternalDLL;

namespace FSEarthMasksDLL
{
    public class NightScript
    {
       
        //My fast attempt of a Night map with my limitted time.
        //Certainly not the best but I believe also not that bad when we only manipulate the color without street recognition or datas feed.
        //You can try to create your own.

        //The following Methodes will be called by FSEarthMasks:
        //MakeNight(MasksTexture iTexture)

        public void MakeNight(MasksTexture iTexture)
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

                    vSum = vRed + vGreen + vBlue;

                    if ((!vIsWater) &&
                        ((vRed - vBlue)   < MasksConfig.mNightStreetGreyConditionGreyToleranceValue) && ((vRed - vBlue)   > -MasksConfig.mNightStreetGreyConditionGreyToleranceValue) &&
                        ((vRed - vGreen)  < MasksConfig.mNightStreetGreyConditionGreyToleranceValue) && ((vRed - vGreen)  > -MasksConfig.mNightStreetGreyConditionGreyToleranceValue) &&
                        ((vGreen - vBlue) < MasksConfig.mNightStreetGreyConditionGreyToleranceValue) && ((vGreen - vBlue) > -MasksConfig.mNightStreetGreyConditionGreyToleranceValue) &&
                        (vSum  > MasksConfig.mNightStreetConditionRGBSumLargerThanValue) &&
                        (vSum <= MasksConfig.mNightStreetConditionRGBSumLessEqualThanValue))
                    {
                        //Stree random dither lights
                        if (vRandomGenerator.NextDouble() < MasksConfig.mNightStreetLightDots1DitherProbabily)
                        {
                            vRed   = MasksConfig.mNightStreetLightDot1Red;
                            vGreen = MasksConfig.mNightStreetLightDot1Green;
                            vBlue  = MasksConfig.mNightStreetLightDot1Blue;
                        }
                        else if (vRandomGenerator.NextDouble() < MasksConfig.mNightStreetLightDots2DitherProbabily)
                        {
                            vRed   = MasksConfig.mNightStreetLightDot2Red;
                            vGreen = MasksConfig.mNightStreetLightDot2Green;
                            vBlue  = MasksConfig.mNightStreetLightDot2Blue;
                        }
                        else if (vRandomGenerator.NextDouble() < MasksConfig.mNightStreetLightDots3DitherProbabily)
                        {
                            vRed   = MasksConfig.mNightStreetLightDot3Red;
                            vGreen = MasksConfig.mNightStreetLightDot3Green;
                            vBlue  = MasksConfig.mNightStreetLightDot3Blue;
                        }
                        else
                        {
                            //Street Make bright and orange
                            vRed   += MasksConfig.mNightStreetRedAddition;
                            vGreen += MasksConfig.mNightStreetGreenAddition;
                            vBlue  += MasksConfig.mNightStreetBlueAddition;
                        }
                    }
                    else
                    {
                        //Normal Land/Water...make factor 2 darker
                        vRed   = Convert.ToInt32(MasksConfig.mNightNonStreetLightness * (Single)(vRed));
                        vGreen = Convert.ToInt32(MasksConfig.mNightNonStreetLightness * (Single)(vGreen));
                        vBlue  = Convert.ToInt32(MasksConfig.mNightNonStreetLightness * (Single)(vBlue));
                    }

                    iTexture.LimitRGBValues(ref vRed, ref vGreen, ref vBlue);

                    iTexture.SetPixelRGB(vX, vY, vRed, vGreen, vBlue);
                }
            }
        }
    }
}
