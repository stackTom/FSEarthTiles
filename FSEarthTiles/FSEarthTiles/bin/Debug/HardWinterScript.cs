using System;
using System.Collections.Generic;
using System.Text;
using FSEarthMasksInternalDLL;

namespace FSEarthMasksDLL
{
    public class HardWinterScript
    {
        //Season routines are taken and converted of Thomas M. Seasons java routines
        //This one is a little modified by me with vSnowAllowed..to handle/color water free of snow better

        //Note that this simple Pixel ReColoring has a problem with Jpeg artifacts and causes rectangle snow blocks.
        //This is not that simple to avoid. It would require a sort of Color prefiltering / unsharpening over the whole bitmap.
        //Then recolor and merge with the original in my opinion

        //Warning!!: Before you start codeing something with a second Bitmap and filtering or something.. 
        //Be aware and warnend that you run into a Memory problem.
        //FSET and FSEM are designed to work together with the available memory.
        //FSEM is desined to do all the work with:
        // One 24 Bit-Bitmap and one 8 Bit Command Array in Memory for FSX
        // or 
        // One 32 Bit Bitmap and one 8 Bit Command Array in Memory for FS2004
        // The very Upper Memory Limit for FSEM is the Memory equivalent of two 24 Bitmaps and NO Command Array.
        // This byway made it reuired for FSEM to reload the Source Texture form File every fresh again.
        
        // If you don't want to run into memory trouble this means all in all you are only allowwed to allocate
        // One 8 Bit Array for your whole processing. 
        // If you need more the best way is to store a temporary file on the Harddisk.
 
        //The following Methodes will be called by FSEarthMasks:
        //MakeHardWinter(MasksTexture iTexture)

        public void MakeHardWinter(MasksTexture iTexture)
        {
            Boolean vStreets = true;
            Random  vRandomGenerator = new Random();

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

                    Boolean vIsWater        = iTexture.IsWaterOrWaterTransition(vX, vY);
                    Boolean vDontAlterColor = MasksConfig.mSpareOutWaterForSeasonsGeneration && vIsWater;
                    Boolean vSnowAllowed    = !(MasksConfig.mNoSnowInWaterForWinterAndHardWinter && vIsWater);
                    
                    if (!vDontAlterColor)
                    {
                        vSum = vRed + vGreen + vBlue;

                        if (MasksConfig.mHardWinterStreetsConditionOn &&
                             ((vRed - vBlue)   < MasksConfig.mHardWinterStreetConditionGreyToleranceValue) && ((vRed - vBlue)   > -MasksConfig.mHardWinterStreetConditionGreyToleranceValue) &&
                             ((vRed - vGreen)  < MasksConfig.mHardWinterStreetConditionGreyToleranceValue) && ((vRed - vGreen)  > -MasksConfig.mHardWinterStreetConditionGreyToleranceValue) &&
                             ((vGreen - vBlue) < MasksConfig.mHardWinterStreetConditionGreyToleranceValue) && ((vGreen - vBlue) > -MasksConfig.mHardWinterStreetConditionGreyToleranceValue) &&
                             (vSum > MasksConfig.mHardWinterStreetConditionRGBSumLargerThanValue) &&
                             (vSum < MasksConfig.mHardWinterStreetConditionRGBSumLessThanValue))
                        {
                            Single vAverage = ((Single)(vSum)) / 3.0f;
                            vRed   = (Int32)(MasksConfig.mHardWinterStreetAverageFactor * (vAverage + ((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterStreetAverageAdditionRandomFactor + MasksConfig.mHardWinterStreetAverageAdditionRandomOffset)) + MasksConfig.mHardWinterStreetAverageRedOffset);
                            vGreen = (Int32)(MasksConfig.mHardWinterStreetAverageFactor * (vAverage + ((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterStreetAverageAdditionRandomFactor + MasksConfig.mHardWinterStreetAverageAdditionRandomOffset)) + MasksConfig.mHardWinterStreetAverageGreenOffset);
                            vBlue  = (Int32)(MasksConfig.mHardWinterStreetAverageFactor * (vAverage + ((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterStreetAverageAdditionRandomFactor + MasksConfig.mHardWinterStreetAverageAdditionRandomOffset)) + MasksConfig.mHardWinterStreetAverageBlueOffset);
                        }
                        else if (vSum < MasksConfig.mHardWinterDarkConditionRGBSumLessThanValue)
                        {
                            // If it is very dark(-green), it might be forest or very steep rock.
                            // In this case, we might want to sprinkle some more white pixels
                            // into that area every now and then:
                            if ( vSnowAllowed &&
                                (vGreen > (vRed - MasksConfig.mHardWinterDarkConditionRGDiffValue)) &&
                                (vGreen > vBlue) &&
                                (vRandomGenerator.NextDouble() < MasksConfig.mHardWinterDarkConditionRandomLessThanValue))
                            {
                                vRed   = MasksConfig.mHardWinterDarkRedOffset   + (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterDarkRandomFactor));
                                vGreen = MasksConfig.mHardWinterDarkGreenOffset + (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterDarkRandomFactor));
                                vBlue  = MasksConfig.mHardWinterDarkBlueOffset  + (Int32)(((Single)(vRandomGenerator.NextDouble()) * MasksConfig.mHardWinterDarkRandomFactor));
                            }
                            else
                            {
                                // leave very dark pixel (basically) unchanged:
                                if (vStreets)
                                {
                                    vRed   = (Int32)(MasksConfig.mHardWinterVeryDarkStreetFactor * (Single)(vRed));
                                    vGreen = (Int32)(MasksConfig.mHardWinterVeryDarkStreetFactor * (Single)(vGreen));
                                    vBlue  = (Int32)(MasksConfig.mHardWinterVeryDarkStreetFactor * (Single)(vBlue));
                                }
                                else
                                {
                                    vRed   = (Int32)(MasksConfig.mHardWinterVeryDarkNormalFactor * (Single)(vRed));
                                    vGreen = (Int32)(MasksConfig.mHardWinterVeryDarkNormalFactor * (Single)(vGreen));
                                    vBlue  = (Int32)(MasksConfig.mHardWinterVeryDarkNormalFactor * (Single)(vBlue));
                                }
                            }
                        }
                        else if (vSum >= MasksConfig.mHardWinterAlmostWhiteConditionRGBSumLargerEqualThanValue)
                        {
                            // Almost white already, make it still whiter with a touch of blue:
                            if (vSum <= MasksConfig.mHardWinterAlmostWhiteConditionRGBSumLessEqualThanValue)
                            {
                                vRed = (Int32)(MasksConfig.mHardWinterAlmostWhiteRedFactor * (Single)(vRed));
                                vGreen = (Int32)(MasksConfig.mHardWinterAlmostWhiteGreenFactor * (Single)(vGreen));
                                vBlue = (Int32)(MasksConfig.mHardWinterAlmostWhiteBlueFactor * (Single)(vBlue));
                            }
                        }
                        else
                        {

                            // Let the dominating color shine through
                            // For some funny reason, green pixel may be dominated by red color...
                            if (vSnowAllowed &&
                                ((vRed - MasksConfig.mHardWinterRestConditionRGDiffValue) > vGreen) &&
                                (vRed > vBlue))
                            {
                                // maybe red-12 / red-8 or something like this to distinguish between
                                // wi and hw in lower areas...
                                if (vRed < MasksConfig.mHardWinterRestRedMin)
                                {
                                    vRed = MasksConfig.mHardWinterRestRedMin;
                                }
                                vGreen = vRed + MasksConfig.mHardWinterRestGBOffsetToRed;
                                vBlue = vRed + MasksConfig.mHardWinterRestGBOffsetToRed;
                            }
                            else if ((vGreen >= (vRed - MasksConfig.mHardWinterRestCondition2RGDiffValue)) &&
                                     (vGreen >= vBlue))
                            {
                                if (!vSnowAllowed ||
                                    (vSum < MasksConfig.mHardWinterRestForestConditionRGBSumLessThan))  //0xF0
                                {
                                    // Probably forest...
                                    vGreen += MasksConfig.mHardWinterRestForestGreenOffset;
                                }
                                else
                                {
                                    if (vGreen < MasksConfig.mHardWinterRestNonForestGreenLimit)
                                    {
                                        vGreen = MasksConfig.mHardWinterRestNonForestGreenLimit;
                                    }
                                    vRed = vGreen + MasksConfig.mHardWinterRestNonForestRedOffsetToGreen;
                                    vBlue = vGreen + MasksConfig.mHardWinterRestNonForestBlueOffsetToGreen;
                                }
                            }
                            else  // if (blue >= red && blue > green)
                            {
                                if (vSnowAllowed)
                                {
                                    if (vBlue < MasksConfig.mHardWinterRestRestBlueMin)
                                    {
                                        vBlue = MasksConfig.mHardWinterRestRestBlueMin;
                                    }
                                    vRed = vBlue + MasksConfig.mHardWinterRestRestRGToBlueOffset;
                                    vGreen = vBlue + MasksConfig.mHardWinterRestRestRGToBlueOffset;
                                }
                            }

                        }

                        iTexture.LimitRGBValues(ref vRed, ref vGreen, ref vBlue);

                    }

                    iTexture.SetPixelRGB(vX, vY, vRed, vGreen, vBlue);
                }
            }
        }
    }
}
