using System;
using System.Collections.Generic;
using System.Text;
using FSEarthMasksInternalDLL;

namespace FSEarthMasksDLL
{
    public class AutumnScript
    {
        //Season routines are taken and converted of Thomas M. Seasons java routines

        //The following Methodes will be called by FSEarthMasks:
        //MakeAutumn(MasksTexture iTexture)

        public void MakeAutumn(MasksTexture iTexture)
        {
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

                    Boolean vDontAlterColor = MasksConfig.mSpareOutWaterForSeasonsGeneration && iTexture.IsWaterOrWaterTransition(vX, vY);

                    if (!vDontAlterColor)
                    {
                        vSum = vRed + vGreen + vBlue;

                        // Convert to autumn colors. Reduce green in all
                        // colors; reduce red in similar way, when the pixel
                        // is very bright; make red and blue brighter without
                        // touching green, when the pixel is rather dark


                        if ((vSum < MasksConfig.mAutumnDarkConditionRGBSumLessThanValue) &&
                            (vSum > MasksConfig.mAutumnDarkConditionRGBSumLargerThanValue))
                        {
                            // Dark pixel, but not black:
                            vRed   += MasksConfig.mAutumnDarkRedAddition;
                            vGreen += MasksConfig.mAutumnDarkGreenAddition;
                            vBlue  += MasksConfig.mAutumnDarkBlueAddition;
                        }
                        else if ((vSum >= MasksConfig.mAutumnBrightConditionRGBSumLargerEqualThanValue) &&
                                 (vSum < MasksConfig.mAutumnBrightConditionRGBSumLessThanValue))
                        {
                            //rather bright pixel
                            vRed   += MasksConfig.mAutumnBrightRedAddition;
                            vGreen += MasksConfig.mAutumnBrightGreenAddition;
                            vBlue  += MasksConfig.mAutumnBrightBlueAddition;
                        }
                        else if ((MasksConfig.mAutumnGreenishConditionBlueIntegerFactor * vBlue) < (MasksConfig.mAutumnGreenishConditionGreenIntegerFactor * vGreen))  //1.4*blue < Green
                        {
                            //very greenish pixel
                            vRed   += MasksConfig.mAutumnGreenishRedAddition;
                            vGreen += MasksConfig.mAutumnGreenishGreenAddition;
                            vBlue  += MasksConfig.mAutumnGreenishBlueAddition;
                        }
                        else
                        {
                            vRed   += MasksConfig.mAutumnRestRedAddition;
                            vGreen += MasksConfig.mAutumnRestGreenAddition;
                            vBlue  += MasksConfig.mAutumnRestBlueAddition;
                        }

                        iTexture.LimitRGBValues(ref vRed, ref vGreen, ref vBlue);

                    }

                    iTexture.SetPixelRGB(vX, vY, vRed, vGreen, vBlue);
                }
            }
        }
    }
}
