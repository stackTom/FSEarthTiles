using System;
using System.Collections.Generic;
using System.Text;
using FSEarthMasksInternalDLL;

namespace FSEarthMasksDLL
{
    public class SpringScript
    {
        //Season routines are taken and converted of Thomas M. Seasons java routines
        //I couldn't find a Spring Texture of Thomas so I made one analog to Autumn

        //The following Methodes will be called by FSEarthMasks:
        //MakeSpring(MasksTexture iTexture)

        public void MakeSpring(MasksTexture iTexture)
        {
            Int32 vRed   = 0;
            Int32 vGreen = 0;
            Int32 vBlue  = 0;
            Int32 vSum   = 0;

            Int32 vPixelCountInX = iTexture.GetPixelCountInX();
            Int32 vPixelCountInY = iTexture.GetPixelCountInY();
            
            for (Int32 vY = 0; vY < vPixelCountInY; vY++)
            {
                for (Int32 vX = 0; vX < vPixelCountInX; vX++)
                {
    
                    iTexture.GetPixelRGB(vX, vY, ref vRed, ref vGreen, ref vBlue);

                    Boolean vDontAlterColor = MasksConfig.mSpareOutWaterForSeasonsGeneration && iTexture.IsWaterOrWaterTransition(vX, vY);

                    if (!vDontAlterColor)
                    {
                        vSum = vRed + vGreen + vBlue;

                        if ((vSum < MasksConfig.mSpringDarkConditionRGBSumLessThanValue) &&
                            (vSum > MasksConfig.mSpringDarkConditionRGBSumLargerThanValue))
                        {
                            // Dark pixel, but not black:
                            vRed   += MasksConfig.mSpringDarkRedAddition;
                            vGreen += MasksConfig.mSpringDarkGreenAddition;
                            vBlue  += MasksConfig.mSpringDarkBlueAddition;
                        }
                        else if ((vSum >= MasksConfig.mSpringBrightConditionRGBSumLargerEqualThanValue) &&
                                 (vSum < MasksConfig.mSpringBrightConditionRGBSumLessThanValue))
                        {
                            //rather bright pixel
                            vRed   += MasksConfig.mSpringBrightRedAddition;
                            vGreen += MasksConfig.mSpringBrightGreenAddition;
                            vBlue  += MasksConfig.mSpringBrightBlueAddition;
                        }
                        else if ((MasksConfig.mSpringGreenishConditionBlueIntegerFactor * vBlue) < (MasksConfig.mSpringGreenishConditionGreenIntegerFactor * vGreen))  //1.4*blue < Green
                        {
                            //very greenish pixel
                            vRed   += MasksConfig.mSpringGreenishRedAddition;
                            vGreen += MasksConfig.mSpringGreenishGreenAddition;
                            vBlue  += MasksConfig.mSpringGreenishBlueAddition;
                        }
                        else
                        {
                            vRed   += MasksConfig.mSpringRestRedAddition;
                            vGreen += MasksConfig.mSpringRestGreenAddition;
                            vBlue  += MasksConfig.mSpringRestBlueAddition;
                        }

                        iTexture.LimitRGBValues(ref vRed, ref vGreen, ref vBlue);

                    }

                    iTexture.SetPixelRGB(vX, vY, vRed, vGreen, vBlue);
                }
            }
        }

    }
}
