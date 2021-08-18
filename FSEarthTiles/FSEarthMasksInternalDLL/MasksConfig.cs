using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace FSEarthMasksInternalDLL
{
    public class MasksConfig
    {
        public enum tPoolType
        {
            eWaterPool = 0,
            eLandPool  = 1,
            eBlendPool = 2,
            eSize = eBlendPool + 1
        }

        public enum tTransitionType
        {
            eWaterTransition  = 0,
            eWater2Transition = 1,
            eBlendTransition  = 2,
            eSize = eBlendTransition + 1
        }

        public enum tTransitionSubType
        {
            eTransparency = 0,
            eReflection   = 1,
            eLightness    = 2,
            eColoring     = 3,
            eSize = eColoring + 1
        }
       

        //Tripple S functions: SËntry (Coast) - ConnectionPoint1 - STransition - ConnectionPoint2 - SExit (DeepWater)
        public struct tTrippleSFunction
        {
            public Double  mEntrySFunctionFirstHalfOrder;
            public Double  mEntrySFunctionSecondHalfOrder;
            public Double  mSFunctionToSFunctionConnectionPoint1x;
            public Double  mSFunctionToSFunctionConnectionPoint1y;
            public Double  mTransitionSFunctionFirstHalfOrder;
            public Double  mTransitionSFunctionSecondHalfOrder;
            public Double  mSFunctionToSFunctionConnectionPoint2x;
            public Double  mSFunctionToSFunctionConnectionPoint2y;
            public Double  mExitSFunctionFirstHalfOrder;
            public Double  mExitSFunctionSecondHalfOrder;
            public Boolean mFlipFunction;
            public Boolean mUseEntrySFunctionDistanceLimit;
            public Boolean mStretchTransitionSAndExitSFunctionToFillAnyGap;
            public Double  mEntrySFunctionDistanceLimit;
            public Double  mEntrySFunctionLinearSlopeBegin;
            public Double  mEntrySFunctionLinearSlopeEnd;
            public Double  mTransitionSFunctionLinearSlopeBegin;
            public Double  mTransitionSFunctionLinearSlopeEnd;
            public Double  mExitSFunctionLinearSlopeBegin;
            public Double  mExitSFunctionLinearSlopeEnd;
            public Double  mSFunctionToSFunctionConnectionPoint1Slope;
            public Double  mSFunctionToSFunctionConnectionPoint2Slope; 
        }

        public struct tTransitionColorParameters
        {
            public Single mLighteness;
            public Single mBrightness;
            public Single mContrast;
            public Int32  mColoringRed;
            public Int32  mColoringBlue;
            public Int32  mColoringGreen;
        }

        public struct tPoolParameters
        {
            public Double mTransparency;
            public Double mReflection;
            public Double mLightness;
            public Single mBrightness;
            public Single mContrast;
            public Int32 mColoringRed;
            public Int32 mColoringBlue;
            public Int32 mColoringGreen;
        }

        //Debug Flags
        public static Boolean mLandWaterDetetcionForEverySinglePixel = false;  //for debug only..warning this slows down the water creation to an extrema


        public static String mStartExeFolder;        //FSEarthMasks Application Start Folder, Exe location (Application.StartupPath)
        public static String mStartOperatingFolder;  //FSEarthMasks Application Start Folder, Exe's operation folder (current directory)

        //Configurations
        public static List<String> mFSEarthMasksConfigFileList = new List<String>();
        public static List<String> mAreaEarthInfoFileList = new List<String>();
        public static List<String> mAreaVectorsFileList = new List<String>();

        public static Boolean mCreateWaterMaskBitmap  = true;
        public static Boolean mCreateNightBitmap      = true;
        public static Boolean mCreateSummerBitmap     = true;
        public static Boolean mCreateSpringBitmap     = true;
        public static Boolean mCreateAutumnBitmap     = true;
        public static Boolean mCreateWinterBitmap     = true;
        public static Boolean mCreateHardWinterBitmap = true;

        public static Boolean mBlendNorthBorder = false;
        public static Boolean mBlendEastBorder  = false;
        public static Boolean mBlendSouthBorder = false;
        public static Boolean mBlendWestBorder  = false;


        //used as configuration
        public static String mAreaEarthInfoFile = "";
        public static String mAreaSourceBitmapFile  = "";
        public static String mAreaMaskBitmapFile = "";
        public static String mAreaNightBitmapFile = "";
        public static String mAreaSpringBitmapFile = "";
        public static String mAreaAutumnBitmapFile = "";
        public static String mAreaWinterBitmapFile = "";
        public static String mAreaHardWinterBitmapFile = "";
        public static String mAreaKMLFile = "";
        public static String mAreaVectorsFile = "";
        public static String mWorkFolder = "";

        public static String  mAreaSummerBitmapFile = "";
        public static Boolean mUseAreaKMLFile       = false;
        public static Boolean mUseAreaVectorsFile   = false;
        public static Boolean mCreateFS2004MasksInsteadFSXMasks = false;
               

        public static Int32  mAreaPixelCountInX = 0;
        public static Int32  mAreaPixelCountInY = 0;

        public static Double mAreaNWCornerLatitude  = 0.0;
        public static Double mAreaNWCornerLongitude = 0.0;
        public static Double mAreaSECornerLatitude  = 0.0;
        public static Double mAreaSECornerLongitude = 0.0;

        //General
        public static Int32 mWaterResolutionBitsCount = 1; //not sure here it is strange it seems to kick in full on value 64 and seems to stays full then..so is it 1 or 2 bit? confuesing (set it to 8bit to have no dithering) 
        public static Int32 mBlendResolutionBitsCount = 4; //still see rings with 6 Bit..so let's try 4 Bits

        public static Single  mBlendBorderDistance  = 50.0f; //[Pixel]
        public static Boolean mCreateWaterForFS2004 = true;
        public static Boolean mMergeWaterForFS2004  = false;
        public static Boolean mSpareOutWaterForSeasonsGeneration  = false;
        public static Boolean mNoSnowInWaterForWinterAndHardWinter = true;
        public static Boolean mUseCSharpScripts = true;

        public static Single mGeneralLighteness    = 1.0f;
        public static Single mGeneralBrightness    = 0.0f;
        public static Single mGeneralContrast      = 0.0f;

        public static Int32  mGeneralColoringRed   = 0;
        public static Int32  mGeneralColoringBlue  = 0;
        public static Int32  mGeneralColoringGreen = 0;

        public static Boolean mCreateTransitionPlotGraphicBitmap = false;
        public static Single  mTransitionPlotGraphicSizeFactor   = 1.0f;
        public static Boolean mCreateCommandGraphicBitmap        = false;
        public static Boolean mShowVectorsInCommandGraphicBitmap = true;

        public static Boolean mUseReversePoolPolygonOrderForKMLFile = true;  //Some Kml Tools make the first drawn object the last in the file

        public static tTrippleSFunction         [,] mTrippleSFunctions          = new tTrippleSFunction[(Int32)(tTransitionType.eSize), (Int32)(tTransitionSubType.eSize)];
        public static tTransitionColorParameters [] mTransitionsColorParameters = new tTransitionColorParameters[(Int32)(tTransitionType.eSize)];
        public static tPoolParameters            [] mPoolsParameters            = new tPoolParameters[(Int32)(tPoolType.eSize)];


        // -- Seasons

        // -- HardWinter
        public static Boolean mHardWinterStreetsConditionOn                      =  true;
        public static Int32   mHardWinterStreetConditionGreyToleranceValue       =  31;
        public static Int32   mHardWinterStreetConditionRGBSumLargerThanValue    = 256;
        public static Int32   mHardWinterStreetConditionRGBSumLessThanValue      =  508;
        public static Single  mHardWinterStreetAverageAdditionRandomFactor       =  6.0f;
        public static Single  mHardWinterStreetAverageAdditionRandomOffset       = -2.0f;
        public static Single  mHardWinterStreetAverageFactor                     =  0.9f;
        public static Int32   mHardWinterStreetAverageRedOffset       	         =  0;
        public static Int32   mHardWinterStreetAverageGreenOffset       	     =  0;
        public static Int32   mHardWinterStreetAverageBlueOffset       	         =  10;

        public static Int32   mHardWinterDarkConditionRGBSumLessThanValue        = 96;
        public static Int32   mHardWinterDarkConditionRGDiffValue                = 12;
        public static Single  mHardWinterDarkConditionRandomLessThanValue        = 0.21f;
        public static Single  mHardWinterDarkRandomFactor                        = 11.0f;
        public static Int32   mHardWinterDarkRedOffset                           = 250;
        public static Int32   mHardWinterDarkGreenOffset                         = 253;
        public static Int32   mHardWinterDarkBlueOffset                          = 253;
        public static Single  mHardWinterVeryDarkStreetFactor                    = 1.47f;
        public static Single  mHardWinterVeryDarkNormalFactor                    = 1.27f;

        public static Int32   mHardWinterAlmostWhiteConditionRGBSumLargerEqualThanValue = 608;
        public static Int32   mHardWinterAlmostWhiteConditionRGBSumLessEqualThanValue   = 752;
        public static Single  mHardWinterAlmostWhiteRedFactor                           = 1.06f;
        public static Single  mHardWinterAlmostWhiteGreenFactor                         = 1.09f;
        public static Single  mHardWinterAlmostWhiteBlueFactor                          = 1.10f;

        public static Int32   mHardWinterRestConditionRGDiffValue                       = 10;
        public static Int32   mHardWinterRestRedMin                                     = 250;
        public static Int32   mHardWinterRestGBOffsetToRed                              = -2;
        public static Int32   mHardWinterRestCondition2RGDiffValue                      = 10;
        public static Int32   mHardWinterRestForestConditionRGBSumLessThan              = 240;
        public static Int32   mHardWinterRestForestGreenOffset                          = -30;
        public static Int32   mHardWinterRestNonForestGreenLimit                        = 250;
        public static Int32   mHardWinterRestNonForestRedOffsetToGreen                  = -5;
        public static Int32   mHardWinterRestNonForestBlueOffsetToGreen                 = -2;
        public static Int32   mHardWinterRestRestBlueMin                                = 250;
        public static Int32   mHardWinterRestRestRGToBlueOffset                         = -4;

        // -- Winter
        public static Int32   mWinterStreetGreyConditionGreyToleranceValue       =  47;
        public static Int32   mWinterStreetGreyConditionRGBSumLargerThanValue    =  256;
        public static Single  mWinterStreetGreyMaxFactor                         =  1.4f;
        public static Single  mWinterStreetGreyRandomFactor                      =  11.0f;

        public static Int32   mWinterDarkConditionRGBSumLessThanValue            =  288;
        public static Int32   mWinterDarkConditionRGBSumLargerThanValue          =  18;
        public static Int32   mWinterDarkRedAddition                             =   4;
        public static Int32   mWinterDarkGreenAddition                           = -11;
        public static Int32   mWinterDarkBlueAddition                            =   3;

        public static Int32   mWinterBrightConditionRGBSumLargerEqualThanValue   =  288;
        public static Int32   mWinterBrightConditionRGBSumLessThanValue          =  752;
        public static Int32   mWinterBrightRedAddition                           = -20;
        public static Int32   mWinterBrightGreenAddition                         = -14;
        public static Int32   mWinterBrightBlueAddition                          = -12;

        public static Int32   mWinterGreenishConditionBlueIntegerFactor          =  7;
        public static Int32   mWinterGreenishConditionGreenIntegerFactor         =  5;
        public static Int32   mWinterGreenishRedAddition                         = -13;
        public static Int32   mWinterGreenishGreenAddition                       = -25;
        public static Int32   mWinterGreenishBlueAddition                        =  0;

        public static Int32   mWinterRestRedAddition                             =  0;
        public static Int32   mWinterRestGreenAddition                           = -12;
        public static Int32   mWinterRestBlueAddition                            = 0;

        // -- Autumn
        public static Int32   mAutumnDarkConditionRGBSumLessThanValue            =  288;
        public static Int32   mAutumnDarkConditionRGBSumLargerThanValue          =  18;
        public static Int32   mAutumnDarkRedAddition                             =   9;
        public static Int32   mAutumnDarkGreenAddition                           =  -8;
        public static Int32   mAutumnDarkBlueAddition                            =   8;

        public static Int32   mAutumnBrightConditionRGBSumLargerEqualThanValue   =  288;
        public static Int32   mAutumnBrightConditionRGBSumLessThanValue          =  752;
        public static Int32   mAutumnBrightRedAddition                           = -16;
        public static Int32   mAutumnBrightGreenAddition                         = -10;
        public static Int32   mAutumnBrightBlueAddition                          =  -7;

        public static Int32   mAutumnGreenishConditionBlueIntegerFactor          =  7;
        public static Int32   mAutumnGreenishConditionGreenIntegerFactor         =  5;
        public static Int32   mAutumnGreenishRedAddition                         = -9;
        public static Int32   mAutumnGreenishGreenAddition                       = -20;
        public static Int32   mAutumnGreenishBlueAddition                        =  0;

        public static Int32   mAutumnRestRedAddition                             =  0;
        public static Int32   mAutumnRestGreenAddition                           = -16;
        public static Int32   mAutumnRestBlueAddition                            =  0;

        // -- Spring
        public static Int32   mSpringDarkConditionRGBSumLessThanValue            =  288;
        public static Int32   mSpringDarkConditionRGBSumLargerThanValue          =  18;
        public static Int32   mSpringDarkRedAddition                             =   9;
        public static Int32   mSpringDarkGreenAddition                           =  -8;
        public static Int32   mSpringDarkBlueAddition                            =   8;

        public static Int32   mSpringBrightConditionRGBSumLargerEqualThanValue   =  288;
        public static Int32   mSpringBrightConditionRGBSumLessThanValue          =  752;
        public static Int32   mSpringBrightRedAddition                           =  15;
        public static Int32   mSpringBrightGreenAddition                         =  10;
        public static Int32   mSpringBrightBlueAddition                          =  -10;

        public static Int32   mSpringGreenishConditionBlueIntegerFactor          =  7;
        public static Int32   mSpringGreenishConditionGreenIntegerFactor         =  5;
        public static Int32   mSpringGreenishRedAddition                         =  10;
        public static Int32   mSpringGreenishGreenAddition                       =  5;
        public static Int32   mSpringGreenishBlueAddition                        =  -5;

        public static Int32   mSpringRestRedAddition                             =  0;
        public static Int32   mSpringRestGreenAddition                          =  0;
        public static Int32   mSpringRestBlueAddition                            =  0;

        // -- Night
        public static Int32   mNightStreetGreyConditionGreyToleranceValue       =  11;
        public static Int32   mNightStreetConditionRGBSumLessEqualThanValue     =  510;
        public static Int32   mNightStreetConditionRGBSumLargerThanValue        =  0;

        public static Double  mNightStreetLightDots1DitherProbabily             =  0.01;
        public static Double  mNightStreetLightDots2DitherProbabily             =  0.02;
        public static Double  mNightStreetLightDots3DitherProbabily             =  0.05;

        public static Int32   mNightStreetLightDot1Red                          =  255;
        public static Int32   mNightStreetLightDot1Green                        =  255;
        public static Int32   mNightStreetLightDot1Blue                         =  255;

        public static Int32   mNightStreetLightDot2Red                          =  255;
        public static Int32   mNightStreetLightDot2Green                        =  200;
        public static Int32   mNightStreetLightDot2Blue                         =  140;

        public static Int32   mNightStreetLightDot3Red                          =  255;
        public static Int32   mNightStreetLightDot3Green                        =  180;
        public static Int32   mNightStreetLightDot3Blue                         =   80;

        public static Int32   mNightStreetRedAddition                           =  100;
        public static Int32   mNightStreetGreenAddition                         =   50;
        public static Int32   mNightStreetBlueAddition                          =  -50;

        public static Single  mNightNonStreetLightness                          =  0.5f;


        public static void Initialize() //to call as first
        {
            mStartExeFolder = Application.StartupPath;
            mStartOperatingFolder = Directory.GetCurrentDirectory();  //Not used just info

            //Water Transition1
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x = 0.1;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y = 0.2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mLighteness = 1.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mBrightness = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mContrast   = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringRed   = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringBlue  = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringGreen = 0;

            //Water Transition2
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x = 0.1;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y = 0.2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mLighteness = 1.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mBrightness = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mContrast   = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringRed   = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringBlue  = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringGreen = 0;

            //Blend Transition
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0;

            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x = 1.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder = 2;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = false;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = true;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit = 20.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd = 0.5;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope = 0.0;
            mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope = 0.0; 

            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mLighteness = 1.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mBrightness = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mContrast   = 0.0f;
            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringRed   = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringBlue  = 0;
            mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringGreen = 0;

            //Pools
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mTransparency  = 1.0;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mReflection    = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mLightness     = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mBrightness    = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mContrast      = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringRed   = 0;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringBlue  = 0;
            mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringGreen = 0;

            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mTransparency   = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mReflection     = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mLightness      = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mBrightness     = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mContrast       = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringRed    = 0;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringBlue   = 0;
            mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringGreen  = 0;

            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mTransparency  = 1.0;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mReflection    = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mLightness     = 0.0;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mBrightness    = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mContrast      = 0.0f;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringRed   = 0;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringBlue  = 0;
            mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringGreen = 0;

        }



        public static Boolean LoadConfigFile(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            StreamReader myStream;

            String vFileName = "FSEarthMasks.ini";
            String vRawLine;

            try
            {
                myStream = new StreamReader(MasksConfig.mStartExeFolder + "\\" + vFileName);
                if (myStream != null)
                {
                    //Read first Line
                    vRawLine = myStream.ReadLine();
                    while (vRawLine != null)
                    {
                        AddConfigLine(vRawLine);
                        //Read next Line
                        vRawLine = myStream.ReadLine();
                    }
                    myStream.Close();

                    AnalyseConfigFile();

                    return true;
                    

                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatus("Missing FSEarthMasks.ini Config File!");
                    return false;
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatus("Missing FSEarthMasks.ini Config File!");
                return false;
            }
        }


        public static Boolean LoadAreaEarthInfoFile(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            StreamReader myStream;

            String vFileName = mAreaEarthInfoFile;
            String vRawLine;

            try
            {
                myStream = new StreamReader(vFileName);
                if (myStream != null)
                {
                    //Read first Line
                    vRawLine = myStream.ReadLine();
                    while (vRawLine != null)
                    {
                        AddAreaEarthInfoLine(vRawLine);
                        //Read next Line
                        vRawLine = myStream.ReadLine();
                    }
                    myStream.Close();

                    AnalyseAreaEarthInfoFile();
                    return true;
                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatus("Missing AreaEarthInfo File! (" + mAreaEarthInfoFile + ")");
                    return false;
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatus("Missing AreaEarthInfo File! (" + mAreaEarthInfoFile + ")");
                return false;
            }
        }


        public static Boolean LoadAreaVectorsFile(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            StreamReader myStream;

            String vFileName = mAreaVectorsFile;
            String vRawLine;

            try
            {
                myStream = new StreamReader(vFileName);
                if (myStream != null)
                {
                    //Read first Line
                    vRawLine = myStream.ReadLine();
                    while (vRawLine != null)
                    {
                        AddAreaVectorsLine(vRawLine);
                        //Read next Line
                        vRawLine = myStream.ReadLine();
                    }
                    myStream.Close();

                    AnalyseAreaVectorsFile();
                    return true;
                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatus("Missing AreaVectors File! (" + mAreaVectorsFile + ")");
                    return false;
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatus("Missing AreaVectors File! (" + mAreaVectorsFile + ")");
                return false;
            }
        }


        private static String GetRightSideOfConfigString(String iString)
        {
            Int32 vNewIndex = iString.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
            String vCutString;
            vCutString = iString.Substring(vNewIndex + 1, iString.Length - (vNewIndex + 1));
            vCutString = vCutString.Trim();
            return vCutString;
        }

        private static void AnalyseConfigFile()
        {
            for (Int32 vRow = 0; vRow < mFSEarthMasksConfigFileList.Count; vRow++)
            {
                String vFocus = mFSEarthMasksConfigFileList[vRow];

                Int32 vIndex1 = vFocus.IndexOf("WaterTransitionTransparencyEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex2 = vFocus.IndexOf("WaterTransitionTransparencyEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex3 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex4 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex5 = vFocus.IndexOf("WaterTransitionTransparencyTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex6 = vFocus.IndexOf("WaterTransitionTransparencyTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex7 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex8 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex9 = vFocus.IndexOf("WaterTransitionTransparencyExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex10 = vFocus.IndexOf("WaterTransitionTransparencyExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex11 = vFocus.IndexOf("WaterTransitionTransparencyFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex12 = vFocus.IndexOf("WaterTransitionReflectionEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex13 = vFocus.IndexOf("WaterTransitionReflectionEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex14 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex15 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex16 = vFocus.IndexOf("WaterTransitionReflectionTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex17 = vFocus.IndexOf("WaterTransitionReflectionTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex18 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex19 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex20 = vFocus.IndexOf("WaterTransitionReflectionExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex21 = vFocus.IndexOf("WaterTransitionReflectionExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex22 = vFocus.IndexOf("WaterTransitionReflectionFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex23 = vFocus.IndexOf("WaterTransitionLightnessEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex24 = vFocus.IndexOf("WaterTransitionLightnessEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex25 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex26 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex27 = vFocus.IndexOf("WaterTransitionLightnessTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex28 = vFocus.IndexOf("WaterTransitionLightnessTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex29 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex30 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex31 = vFocus.IndexOf("WaterTransitionLightnessExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex32 = vFocus.IndexOf("WaterTransitionLightnessExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex33 = vFocus.IndexOf("WaterTransitionLightnessFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                //Water Transition2
                Int32 vIndex34 = vFocus.IndexOf("Water2TransitionTransparencyEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex35 = vFocus.IndexOf("Water2TransitionTransparencyEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex36 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex37 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex38 = vFocus.IndexOf("Water2TransitionTransparencyTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex39 = vFocus.IndexOf("Water2TransitionTransparencyTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex40 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex41 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex42 = vFocus.IndexOf("Water2TransitionTransparencyExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex43 = vFocus.IndexOf("Water2TransitionTransparencyExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex44 = vFocus.IndexOf("Water2TransitionTransparencyFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex45 = vFocus.IndexOf("Water2TransitionReflectionEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex46 = vFocus.IndexOf("Water2TransitionReflectionEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex47 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex48 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex49 = vFocus.IndexOf("Water2TransitionReflectionTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex50 = vFocus.IndexOf("Water2TransitionReflectionTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex51 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex52 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex53 = vFocus.IndexOf("Water2TransitionReflectionExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex54 = vFocus.IndexOf("Water2TransitionReflectionExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex55 = vFocus.IndexOf("Water2TransitionReflectionFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex56 = vFocus.IndexOf("Water2TransitionLightnessEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex57 = vFocus.IndexOf("Water2TransitionLightnessEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex58 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex59 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex60 = vFocus.IndexOf("Water2TransitionLightnessTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex61 = vFocus.IndexOf("Water2TransitionLightnessTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex62 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex63 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex64 = vFocus.IndexOf("Water2TransitionLightnessExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex65 = vFocus.IndexOf("Water2TransitionLightnessExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex66 = vFocus.IndexOf("Water2TransitionLightnessFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                //Blend Transition
                Int32 vIndex67 = vFocus.IndexOf("BlendTransitionTransparencyEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex68 = vFocus.IndexOf("BlendTransitionTransparencyEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex69 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex70 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex71 = vFocus.IndexOf("BlendTransitionTransparencyTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex72 = vFocus.IndexOf("BlendTransitionTransparencyTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex73 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase); ;
                Int32 vIndex74 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex75 = vFocus.IndexOf("BlendTransitionTransparencyExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex76 = vFocus.IndexOf("BlendTransitionTransparencyExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex77 = vFocus.IndexOf("BlendTransitionTransparencyFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex78 = vFocus.IndexOf("BlendTransitionReflectionEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex79 = vFocus.IndexOf("BlendTransitionReflectionEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex80 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex81 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex82 = vFocus.IndexOf("BlendTransitionReflectionTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex83 = vFocus.IndexOf("BlendTransitionReflectionTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex84 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex85 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex86 = vFocus.IndexOf("BlendTransitionReflectionExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex87 = vFocus.IndexOf("BlendTransitionReflectionExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex88 = vFocus.IndexOf("BlendTransitionReflectionFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex89 = vFocus.IndexOf("BlendTransitionLightnessEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex90 = vFocus.IndexOf("BlendTransitionLightnessEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex91 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex92 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex93 = vFocus.IndexOf("BlendTransitionLightnessTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex94 = vFocus.IndexOf("BlendTransitionLightnessTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex95 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex96 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex97 = vFocus.IndexOf("BlendTransitionLightnessExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex98 = vFocus.IndexOf("BlendTransitionLightnessExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex99 = vFocus.IndexOf("BlendTransitionLightnessFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex100 = vFocus.IndexOf("WaterPoolTransparency", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex101 = vFocus.IndexOf("WaterPoolReflection", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex102 = vFocus.IndexOf("WaterPoolLightness", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex103 = vFocus.IndexOf("LandPoolTransparency", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex104 = vFocus.IndexOf("LandPoolReflection", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex105 = vFocus.IndexOf("LandPoolLightness", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex106 = vFocus.IndexOf("BlendPoolTransparency", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex107 = vFocus.IndexOf("BlendPoolReflection", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex108 = vFocus.IndexOf("BlendPoolLightness", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex109 = vFocus.IndexOf("WaterResolutionBitsCount", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex110 = vFocus.IndexOf("BlendResolutionBitsCount", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex111 = vFocus.IndexOf("HardWinterStreetsConditionOn", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex112 = vFocus.IndexOf("HardWinterStreetConditionGreyToleranceValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex113 = vFocus.IndexOf("HardWinterStreetConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex114 = vFocus.IndexOf("HardWinterStreetConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex115 = vFocus.IndexOf("HardWinterStreetAverageAdditionRandomFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex116 = vFocus.IndexOf("HardWinterStreetAverageAdditionRandomOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex117 = vFocus.IndexOf("HardWinterStreetAverageFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex118 = vFocus.IndexOf("HardWinterStreetAverageRedOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex119 = vFocus.IndexOf("HardWinterStreetAverageGreenOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex120 = vFocus.IndexOf("HardWinterStreetAverageBlueOffset", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex121 = vFocus.IndexOf("HardWinterDarkConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex122 = vFocus.IndexOf("HardWinterDarkConditionRGDiffValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex123 = vFocus.IndexOf("HardWinterDarkConditionRandomLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex124 = vFocus.IndexOf("HardWinterDarkRandomFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex125 = vFocus.IndexOf("HardWinterDarkRedOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex126 = vFocus.IndexOf("HardWinterDarkGreenOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex127 = vFocus.IndexOf("HardWinterDarkBlueOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex128 = vFocus.IndexOf("HardWinterVeryDarkStreetFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex129 = vFocus.IndexOf("HardWinterVeryDarkNormalFactor", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex130 = vFocus.IndexOf("HardWinterAlmostWhiteConditionRGBSumLargerEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex131 = vFocus.IndexOf("HardWinterAlmostWhiteConditionRGBSumLessEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex132 = vFocus.IndexOf("HardWinterAlmostWhiteRedFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex133 = vFocus.IndexOf("HardWinterAlmostWhiteGreenFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex134 = vFocus.IndexOf("HardWinterAlmostWhiteBlueFactor", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex135 = vFocus.IndexOf("HardWinterRestConditionRGDiffValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex136 = vFocus.IndexOf("HardWinterRestRedMin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex137 = vFocus.IndexOf("HardWinterRestGBOffsetToRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex138 = vFocus.IndexOf("HardWinterRestCondition2RGDiffValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex139 = vFocus.IndexOf("HardWinterRestForestConditionRGBSumLessThan", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex140 = vFocus.IndexOf("HardWinterRestForestGreenOffset", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex141 = vFocus.IndexOf("HardWinterRestNonForestGreenLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex142 = vFocus.IndexOf("HardWinterRestNonForestRedOffsetToGreen", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex143 = vFocus.IndexOf("HardWinterRestNonForestBlueOffsetToGreen", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex144 = vFocus.IndexOf("HardWinterRestRestBlueMin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex145 = vFocus.IndexOf("HardWinterRestRestRGToBlueOffset", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex146 = vFocus.IndexOf("WinterStreetGreyConditionGreyToleranceValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex147 = vFocus.IndexOf("WinterStreetGreyConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex148 = vFocus.IndexOf("WinterStreetGreyMaxFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex149 = vFocus.IndexOf("WinterStreetGreyRandomFactor", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex150 = vFocus.IndexOf("WinterDarkConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex151 = vFocus.IndexOf("WinterDarkConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex152 = vFocus.IndexOf("WinterDarkRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex153 = vFocus.IndexOf("WinterDarkGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex154 = vFocus.IndexOf("WinterDarkBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex155 = vFocus.IndexOf("WinterBrightConditionRGBSumLargerEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex156 = vFocus.IndexOf("WinterBrightConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex157 = vFocus.IndexOf("WinterBrightRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex158 = vFocus.IndexOf("WinterBrightGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex159 = vFocus.IndexOf("WinterBrightBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex160 = vFocus.IndexOf("WinterGreenishConditionBlueIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex161 = vFocus.IndexOf("WinterGreenishConditionGreenIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex162 = vFocus.IndexOf("WinterGreenishRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex163 = vFocus.IndexOf("WinterGreenishGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex164 = vFocus.IndexOf("WinterGreenishBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex165 = vFocus.IndexOf("WinterRestRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex166 = vFocus.IndexOf("WinterRestGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex167 = vFocus.IndexOf("WinterRestBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex168 = vFocus.IndexOf("AutumnDarkConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex169 = vFocus.IndexOf("AutumnDarkConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex170 = vFocus.IndexOf("AutumnDarkRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex171 = vFocus.IndexOf("AutumnDarkGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex172 = vFocus.IndexOf("AutumnDarkBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex173 = vFocus.IndexOf("AutumnBrightConditionRGBSumLargerEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex174 = vFocus.IndexOf("AutumnBrightConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex175 = vFocus.IndexOf("AutumnBrightRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex176 = vFocus.IndexOf("AutumnBrightGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex177 = vFocus.IndexOf("AutumnBrightBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex178 = vFocus.IndexOf("AutumnGreenishConditionBlueIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex179 = vFocus.IndexOf("AutumnGreenishConditionGreenIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex180 = vFocus.IndexOf("AutumnGreenishRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex181 = vFocus.IndexOf("AutumnGreenishGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex182 = vFocus.IndexOf("AutumnGreenishBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex183 = vFocus.IndexOf("AutumnRestRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex184 = vFocus.IndexOf("AutumnRestGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex185 = vFocus.IndexOf("AutumnRestBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex186 = vFocus.IndexOf("SpringDarkConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex187 = vFocus.IndexOf("SpringDarkConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex188 = vFocus.IndexOf("SpringDarkRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex189 = vFocus.IndexOf("SpringDarkGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex190 = vFocus.IndexOf("SpringDarkBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex191 = vFocus.IndexOf("SpringBrightConditionRGBSumLargerEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex192 = vFocus.IndexOf("SpringBrightConditionRGBSumLessThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex193 = vFocus.IndexOf("SpringBrightRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex194 = vFocus.IndexOf("SpringBrightGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex195 = vFocus.IndexOf("SpringBrightBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex196 = vFocus.IndexOf("SpringGreenishConditionBlueIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex197 = vFocus.IndexOf("SpringGreenishConditionGreenIntegerFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex198 = vFocus.IndexOf("SpringGreenishRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex199 = vFocus.IndexOf("SpringGreenishGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex200 = vFocus.IndexOf("SpringGreenishBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex201 = vFocus.IndexOf("SpringRestRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex202 = vFocus.IndexOf("SpringRestGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex203 = vFocus.IndexOf("SpringRestBlueAddition", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex204 = vFocus.IndexOf("GeneralLighteness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex205 = vFocus.IndexOf("GeneralColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex206 = vFocus.IndexOf("GeneralColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex207 = vFocus.IndexOf("GeneralColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex208 = vFocus.IndexOf("WaterTransitionLighteness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex209 = vFocus.IndexOf("WaterTransitionColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex210 = vFocus.IndexOf("WaterTransitionColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex211 = vFocus.IndexOf("WaterTransitionColoringGreen", StringComparison.CurrentCultureIgnoreCase); ;

                Int32 vIndex212 = vFocus.IndexOf("Water2TransitionLighteness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex213 = vFocus.IndexOf("Water2TransitionColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex214 = vFocus.IndexOf("Water2TransitionColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex215 = vFocus.IndexOf("Water2TransitionColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex216 = vFocus.IndexOf("BlendTransitionLighteness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex217 = vFocus.IndexOf("BlendTransitionColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex218 = vFocus.IndexOf("BlendTransitionColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex219 = vFocus.IndexOf("BlendTransitionColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex220 = vFocus.IndexOf("WaterPoolColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex221 = vFocus.IndexOf("WaterPoolColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex222 = vFocus.IndexOf("WaterPoolColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex223 = vFocus.IndexOf("LandPoolColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex224 = vFocus.IndexOf("LandPoolColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex225 = vFocus.IndexOf("LandPoolColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex226 = vFocus.IndexOf("BlendPoolColoringRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex227 = vFocus.IndexOf("BlendPoolColoringBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex228 = vFocus.IndexOf("BlendPoolColoringGreen", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex229 = vFocus.IndexOf("WaterTransitionColoringEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex230 = vFocus.IndexOf("WaterTransitionColoringEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex231 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex232 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex233 = vFocus.IndexOf("WaterTransitionColoringTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex234 = vFocus.IndexOf("WaterTransitionColoringTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex235 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex236 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex237 = vFocus.IndexOf("WaterTransitionColoringExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex238 = vFocus.IndexOf("WaterTransitionColoringExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex239 = vFocus.IndexOf("WaterTransitionColoringFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex240 = vFocus.IndexOf("Water2TransitionColoringEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex241 = vFocus.IndexOf("Water2TransitionColoringEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex242 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex243 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex244 = vFocus.IndexOf("Water2TransitionColoringTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex245 = vFocus.IndexOf("Water2TransitionColoringTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex246 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex247 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex248 = vFocus.IndexOf("Water2TransitionColoringExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex249 = vFocus.IndexOf("Water2TransitionColoringExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex250 = vFocus.IndexOf("Water2TransitionColoringFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex251 = vFocus.IndexOf("BlendTransitionColoringEntrySFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex252 = vFocus.IndexOf("BlendTransitionColoringEntrySFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex253 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint1x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex254 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint1y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex255 = vFocus.IndexOf("BlendTransitionColoringTransitionSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex256 = vFocus.IndexOf("BlendTransitionColoringTransitionSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex257 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint2x", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex258 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint2y", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex259 = vFocus.IndexOf("BlendTransitionColoringExitSFunctionFirstHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex260 = vFocus.IndexOf("BlendTransitionColoringExitSFunctionSecondHalfOrder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex261 = vFocus.IndexOf("BlendTransitionColoringFlipFunction", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex262 = vFocus.IndexOf("CreateTransitionPlotGraphicBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex263 = vFocus.IndexOf("CreateCommandGraphicBitmap", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex264 = vFocus.IndexOf("GeneralBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex265 = vFocus.IndexOf("GeneralContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex266 = vFocus.IndexOf("WaterTransitionBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex267 = vFocus.IndexOf("WaterTransitionContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex268 = vFocus.IndexOf("Water2TransitionBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex269 = vFocus.IndexOf("Water2TransitionContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex270 = vFocus.IndexOf("BlendTransitionBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex271 = vFocus.IndexOf("BlendTransitionContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex272 = vFocus.IndexOf("WaterPoolBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex273 = vFocus.IndexOf("WaterPoolContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex274 = vFocus.IndexOf("LandPoolBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex275 = vFocus.IndexOf("LandPoolContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex276 = vFocus.IndexOf("BlendPoolBrightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex277 = vFocus.IndexOf("BlendPoolContrast", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex278 = vFocus.IndexOf("ShowVectorsInCommandGraphicBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex279 = vFocus.IndexOf("TransitionPlotGraphicSizeFactor", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex280 = vFocus.IndexOf("UseReversePoolPolygonOrderForKMLFile", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex281 = vFocus.IndexOf("WaterTransitionTransparencyUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex282 = vFocus.IndexOf("WaterTransitionTransparencyStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex283 = vFocus.IndexOf("WaterTransitionTransparencyEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex284 = vFocus.IndexOf("WaterTransitionTransparencyEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex285 = vFocus.IndexOf("WaterTransitionTransparencyEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex286 = vFocus.IndexOf("WaterTransitionTransparencyTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex287 = vFocus.IndexOf("WaterTransitionTransparencyTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex288 = vFocus.IndexOf("WaterTransitionTransparencyExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex289 = vFocus.IndexOf("WaterTransitionTransparencyExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex290 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex291 = vFocus.IndexOf("WaterTransitionTransparencySFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex292 = vFocus.IndexOf("WaterTransitionReflectionUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex293 = vFocus.IndexOf("WaterTransitionReflectionStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex294 = vFocus.IndexOf("WaterTransitionReflectionEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex295 = vFocus.IndexOf("WaterTransitionReflectionEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex296 = vFocus.IndexOf("WaterTransitionReflectionEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex297 = vFocus.IndexOf("WaterTransitionReflectionTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex298 = vFocus.IndexOf("WaterTransitionReflectionTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex299 = vFocus.IndexOf("WaterTransitionReflectionExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex300 = vFocus.IndexOf("WaterTransitionReflectionExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex301 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex302 = vFocus.IndexOf("WaterTransitionReflectionSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex303 = vFocus.IndexOf("WaterTransitionLightnessUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex304 = vFocus.IndexOf("WaterTransitionLightnessStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex305 = vFocus.IndexOf("WaterTransitionLightnessEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex306 = vFocus.IndexOf("WaterTransitionLightnessEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex307 = vFocus.IndexOf("WaterTransitionLightnessEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex308 = vFocus.IndexOf("WaterTransitionLightnessTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex309 = vFocus.IndexOf("WaterTransitionLightnessTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex310 = vFocus.IndexOf("WaterTransitionLightnessExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex311 = vFocus.IndexOf("WaterTransitionLightnessExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex312 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex313 = vFocus.IndexOf("WaterTransitionLightnessSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex314 = vFocus.IndexOf("WaterTransitionColoringUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex315 = vFocus.IndexOf("WaterTransitionColoringStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex316 = vFocus.IndexOf("WaterTransitionColoringEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex317 = vFocus.IndexOf("WaterTransitionColoringEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex318 = vFocus.IndexOf("WaterTransitionColoringEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex319 = vFocus.IndexOf("WaterTransitionColoringTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex320 = vFocus.IndexOf("WaterTransitionColoringTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex321 = vFocus.IndexOf("WaterTransitionColoringExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex322 = vFocus.IndexOf("WaterTransitionColoringExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex323 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex324 = vFocus.IndexOf("WaterTransitionColoringSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);


                Int32 vIndex325 = vFocus.IndexOf("Water2TransitionTransparencyUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex326 = vFocus.IndexOf("Water2TransitionTransparencyStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex327 = vFocus.IndexOf("Water2TransitionTransparencyEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex328 = vFocus.IndexOf("Water2TransitionTransparencyEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex329 = vFocus.IndexOf("Water2TransitionTransparencyEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex330 = vFocus.IndexOf("Water2TransitionTransparencyTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex331 = vFocus.IndexOf("Water2TransitionTransparencyTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex332 = vFocus.IndexOf("Water2TransitionTransparencyExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex333 = vFocus.IndexOf("Water2TransitionTransparencyExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex334 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex335 = vFocus.IndexOf("Water2TransitionTransparencySFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex336 = vFocus.IndexOf("Water2TransitionReflectionUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex337 = vFocus.IndexOf("Water2TransitionReflectionStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex338 = vFocus.IndexOf("Water2TransitionReflectionEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex339 = vFocus.IndexOf("Water2TransitionReflectionEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex340 = vFocus.IndexOf("Water2TransitionReflectionEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex341 = vFocus.IndexOf("Water2TransitionReflectionTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex342 = vFocus.IndexOf("Water2TransitionReflectionTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex343 = vFocus.IndexOf("Water2TransitionReflectionExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex344 = vFocus.IndexOf("Water2TransitionReflectionExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex345 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex346 = vFocus.IndexOf("Water2TransitionReflectionSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex347 = vFocus.IndexOf("Water2TransitionLightnessUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex348 = vFocus.IndexOf("Water2TransitionLightnessStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex349 = vFocus.IndexOf("Water2TransitionLightnessEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex350 = vFocus.IndexOf("Water2TransitionLightnessEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex351 = vFocus.IndexOf("Water2TransitionLightnessEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex352 = vFocus.IndexOf("Water2TransitionLightnessTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex353 = vFocus.IndexOf("Water2TransitionLightnessTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex354 = vFocus.IndexOf("Water2TransitionLightnessExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex355 = vFocus.IndexOf("Water2TransitionLightnessExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex356 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex357 = vFocus.IndexOf("Water2TransitionLightnessSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex358 = vFocus.IndexOf("Water2TransitionColoringUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex359 = vFocus.IndexOf("Water2TransitionColoringStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex360 = vFocus.IndexOf("Water2TransitionColoringEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex361 = vFocus.IndexOf("Water2TransitionColoringEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex362 = vFocus.IndexOf("Water2TransitionColoringEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex363 = vFocus.IndexOf("Water2TransitionColoringTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex364 = vFocus.IndexOf("Water2TransitionColoringTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex365 = vFocus.IndexOf("Water2TransitionColoringExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex366 = vFocus.IndexOf("Water2TransitionColoringExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex367 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex368 = vFocus.IndexOf("Water2TransitionColoringSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);


                Int32 vIndex369 = vFocus.IndexOf("BlendTransitionTransparencyUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex370 = vFocus.IndexOf("BlendTransitionTransparencyStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex371 = vFocus.IndexOf("BlendTransitionTransparencyEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex372 = vFocus.IndexOf("BlendTransitionTransparencyEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex373 = vFocus.IndexOf("BlendTransitionTransparencyEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex374 = vFocus.IndexOf("BlendTransitionTransparencyTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex375 = vFocus.IndexOf("BlendTransitionTransparencyTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex376 = vFocus.IndexOf("BlendTransitionTransparencyExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex377 = vFocus.IndexOf("BlendTransitionTransparencyExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex378 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex379 = vFocus.IndexOf("BlendTransitionTransparencySFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex380 = vFocus.IndexOf("BlendTransitionReflectionUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex381 = vFocus.IndexOf("BlendTransitionReflectionStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex382 = vFocus.IndexOf("BlendTransitionReflectionEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex383 = vFocus.IndexOf("BlendTransitionReflectionEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex384 = vFocus.IndexOf("BlendTransitionReflectionEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex385 = vFocus.IndexOf("BlendTransitionReflectionTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex386 = vFocus.IndexOf("BlendTransitionReflectionTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex387 = vFocus.IndexOf("BlendTransitionReflectionExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex388 = vFocus.IndexOf("BlendTransitionReflectionExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex389 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex390 = vFocus.IndexOf("BlendTransitionReflectionSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex391 = vFocus.IndexOf("BlendTransitionLightnessUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex392 = vFocus.IndexOf("BlendTransitionLightnessStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex393 = vFocus.IndexOf("BlendTransitionLightnessEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex394 = vFocus.IndexOf("BlendTransitionLightnessEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex395 = vFocus.IndexOf("BlendTransitionLightnessEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex396 = vFocus.IndexOf("BlendTransitionLightnessTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex397 = vFocus.IndexOf("BlendTransitionLightnessTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex398 = vFocus.IndexOf("BlendTransitionLightnessExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex399 = vFocus.IndexOf("BlendTransitionLightnessExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex400 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex401 = vFocus.IndexOf("BlendTransitionLightnessSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex402 = vFocus.IndexOf("BlendTransitionColoringUseEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex403 = vFocus.IndexOf("BlendTransitionColoringStretchTransitionSAndExitSFunctionToFillAnyGap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex404 = vFocus.IndexOf("BlendTransitionColoringEntrySFunctionDistanceLimit", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex405 = vFocus.IndexOf("BlendTransitionColoringEntrySFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex406 = vFocus.IndexOf("BlendTransitionColoringEntrySFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex407 = vFocus.IndexOf("BlendTransitionColoringTransitionSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex408 = vFocus.IndexOf("BlendTransitionColoringTransitionSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex409 = vFocus.IndexOf("BlendTransitionColoringExitSFunctionLinearSlopeBegin", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex410 = vFocus.IndexOf("BlendTransitionColoringExitSFunctionLinearSlopeEnd", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex411 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint1Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex412 = vFocus.IndexOf("BlendTransitionColoringSFunctionToSFunctionConnectionPoint2Slope", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex413 = vFocus.IndexOf("BlendBorderDistance", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex414 = vFocus.IndexOf("CreateWaterForFS2004", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex415 = vFocus.IndexOf("MergeWaterForFS2004", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex416 = vFocus.IndexOf("SpareOutWaterForSeasonsGeneration", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex417 = vFocus.IndexOf("NightStreetGreyConditionGreyToleranceValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex418 = vFocus.IndexOf("NightStreetConditionRGBSumLessEqualThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex419 = vFocus.IndexOf("NightStreetConditionRGBSumLargerThanValue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex420 = vFocus.IndexOf("NightStreetLightDots1DitherProbabily", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex421 = vFocus.IndexOf("NightStreetLightDots2DitherProbabily", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex422 = vFocus.IndexOf("NightStreetLightDots3DitherProbabily", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex423 = vFocus.IndexOf("NightStreetRedAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex424 = vFocus.IndexOf("NightStreetGreenAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex425 = vFocus.IndexOf("NightStreetBlueAddition", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex426 = vFocus.IndexOf("NightNonStreetLightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex427 = vFocus.IndexOf("NightStreetLightDot1Red", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex428 = vFocus.IndexOf("NightStreetLightDot1Green", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex429 = vFocus.IndexOf("NightStreetLightDot1Blue ", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex430 = vFocus.IndexOf("NightStreetLightDot2Red", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex431 = vFocus.IndexOf("NightStreetLightDot2Green", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex432 = vFocus.IndexOf("NightStreetLightDot2Blue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex433 = vFocus.IndexOf("NightStreetLightDot3Red", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex434 = vFocus.IndexOf("NightStreetLightDot3Green", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex435 = vFocus.IndexOf("NightStreetLightDot3Blue", StringComparison.CurrentCultureIgnoreCase);

                Int32 vIndex436 = vFocus.IndexOf("NoSnowInWaterForWinterAndHardWinter", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex437 = vFocus.IndexOf("UseCSharpScripts", StringComparison.CurrentCultureIgnoreCase);

                if (vIndex1 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex2 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex3 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex4 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex5 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex6 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex7 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex8 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex9 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex10 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex11 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex12 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex13 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex14 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex15 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex16 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex17 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex18 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex19 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex20 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex21 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex22 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex23 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex24 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex25 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex26 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex27 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex28 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex29 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex30 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex31 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex32 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex33 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex34 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex35 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex36 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex37 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex38 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex39 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex40 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex41 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex42 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex43 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex44 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex45 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex46 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex47 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex48 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex49 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex50 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex51 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex52 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex53 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex54 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex55 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex56 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex57 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex58 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex59 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex60 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex61 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex62 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex63 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex64 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex65 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex66 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex67 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex68 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex69 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex70 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex71 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex72 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex73 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex74 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex75 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex76 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex77 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex78 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex79 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex80 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex81 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex82 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex83 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex84 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex85 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex86 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex87 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex88 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex89 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex90 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex91 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex92 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex93 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex94 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex95 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex96 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex97 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex98 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex99 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mFlipFunction = GetBooleanFromString(vCutString);
                }


                if (vIndex100 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mTransparency, vCutString);
                }
                if (vIndex101 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mReflection, vCutString);
                }
                if (vIndex102 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mLightness, vCutString);
                }

                if (vIndex103 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mTransparency, vCutString);
                }
                if (vIndex104 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mReflection, vCutString);
                }
                if (vIndex105 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mLightness, vCutString);
                }

                if (vIndex106 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mTransparency, vCutString);
                }
                if (vIndex107 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mReflection, vCutString);
                }
                if (vIndex108 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mLightness, vCutString);
                }

                if (vIndex109 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mWaterResolutionBitsCount = Convert.ToInt32(vCutString, NumberFormatInfo.InvariantInfo);
                        if (mWaterResolutionBitsCount > 8)
                        {
                            mWaterResolutionBitsCount = 8;
                        }
                        if (mWaterResolutionBitsCount < 1)
                        {
                            mWaterResolutionBitsCount = 1;
                        }
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex110 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mBlendResolutionBitsCount = Convert.ToInt32(vCutString, NumberFormatInfo.InvariantInfo);
                        if (mBlendResolutionBitsCount > 8)
                        {
                            mBlendResolutionBitsCount = 8;
                        }
                        if (mBlendResolutionBitsCount < 1)
                        {
                            mBlendResolutionBitsCount = 1;
                        }
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }

                if (vIndex111 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mHardWinterStreetsConditionOn = GetBooleanFromString(vCutString);
                }
                if (vIndex112 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetConditionGreyToleranceValue, vCutString);
                }
                if (vIndex113 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex114 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex115 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterStreetAverageAdditionRandomFactor, vCutString);
                }
                if (vIndex116 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterStreetAverageAdditionRandomOffset, vCutString);
                }
                if (vIndex117 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterStreetAverageFactor, vCutString);
                }
                if (vIndex118 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetAverageRedOffset, vCutString);
                }
                if (vIndex119 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetAverageGreenOffset, vCutString);
                }
                if (vIndex120 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterStreetAverageBlueOffset, vCutString);
                }
                if (vIndex121 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterDarkConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex122 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterDarkConditionRGDiffValue, vCutString);
                }
                if (vIndex123 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterDarkConditionRandomLessThanValue, vCutString);
                }
                if (vIndex124 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterDarkRandomFactor, vCutString);
                }
                if (vIndex125 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterDarkRedOffset, vCutString);
                }
                if (vIndex126 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterDarkGreenOffset, vCutString);
                }
                if (vIndex127 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterDarkBlueOffset, vCutString);
                }
                if (vIndex128 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterVeryDarkStreetFactor, vCutString);
                }
                if (vIndex129 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterVeryDarkNormalFactor, vCutString);
                }
                if (vIndex130 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterAlmostWhiteConditionRGBSumLargerEqualThanValue, vCutString);
                }
                if (vIndex131 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterAlmostWhiteConditionRGBSumLessEqualThanValue, vCutString);
                }
                if (vIndex132 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterAlmostWhiteRedFactor, vCutString);
                }
                if (vIndex133 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterAlmostWhiteGreenFactor, vCutString);
                }
                if (vIndex134 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mHardWinterAlmostWhiteBlueFactor, vCutString);
                }
                if (vIndex135 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestConditionRGDiffValue, vCutString);
                }
                if (vIndex136 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestRedMin, vCutString);
                }
                if (vIndex137 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestGBOffsetToRed, vCutString);
                }
                if (vIndex138 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestCondition2RGDiffValue, vCutString);
                }
                if (vIndex139 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestForestConditionRGBSumLessThan, vCutString);
                }
                if (vIndex140 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestForestGreenOffset, vCutString);
                }
                if (vIndex141 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestNonForestGreenLimit, vCutString);
                }
                if (vIndex142 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestNonForestRedOffsetToGreen, vCutString);
                }
                if (vIndex143 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestNonForestBlueOffsetToGreen, vCutString);
                }
                if (vIndex144 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestRestBlueMin, vCutString);
                }
                if (vIndex145 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mHardWinterRestRestRGToBlueOffset, vCutString);
                }

                if (vIndex146 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterStreetGreyConditionGreyToleranceValue, vCutString);
                }
                if (vIndex147 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterStreetGreyConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex148 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mWinterStreetGreyMaxFactor, vCutString);
                }
                if (vIndex149 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mWinterStreetGreyRandomFactor, vCutString);
                }

                if (vIndex150 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterDarkConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex151 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterDarkConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex152 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterDarkRedAddition, vCutString);
                }
                if (vIndex153 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterDarkGreenAddition, vCutString);
                }
                if (vIndex154 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterDarkBlueAddition, vCutString);
                }
                if (vIndex155 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterBrightConditionRGBSumLargerEqualThanValue, vCutString);
                }
                if (vIndex156 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterBrightConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex157 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterBrightRedAddition, vCutString);
                }
                if (vIndex158 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterBrightGreenAddition, vCutString);
                }
                if (vIndex159 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterBrightBlueAddition, vCutString);
                }
                if (vIndex160 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterGreenishConditionBlueIntegerFactor, vCutString);
                }
                if (vIndex161 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterGreenishConditionGreenIntegerFactor, vCutString);
                }
                if (vIndex162 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterGreenishRedAddition, vCutString);
                }
                if (vIndex163 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterGreenishGreenAddition, vCutString);
                }
                if (vIndex164 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterGreenishBlueAddition, vCutString);
                }
                if (vIndex165 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterRestRedAddition, vCutString);
                }
                if (vIndex166 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterRestGreenAddition, vCutString);
                }
                if (vIndex167 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mWinterRestBlueAddition, vCutString);
                }
                if (vIndex168 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnDarkConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex169 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnDarkConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex170 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnDarkRedAddition, vCutString);
                }
                if (vIndex171 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnDarkGreenAddition, vCutString);
                }
                if (vIndex172 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnDarkBlueAddition, vCutString);
                }
                if (vIndex173 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnBrightConditionRGBSumLargerEqualThanValue, vCutString);
                }
                if (vIndex174 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnBrightConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex175 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnBrightRedAddition, vCutString);
                }
                if (vIndex176 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnBrightGreenAddition, vCutString);
                }
                if (vIndex177 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnBrightBlueAddition, vCutString);
                }
                if (vIndex178 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnGreenishConditionBlueIntegerFactor, vCutString);
                }
                if (vIndex179 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnGreenishConditionGreenIntegerFactor, vCutString);
                }
                if (vIndex180 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnGreenishRedAddition, vCutString);
                }
                if (vIndex181 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnGreenishGreenAddition, vCutString);
                }
                if (vIndex182 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnGreenishBlueAddition, vCutString);
                }
                if (vIndex183 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnRestRedAddition, vCutString);
                }
                if (vIndex184 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnRestGreenAddition, vCutString);
                }
                if (vIndex185 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mAutumnRestBlueAddition, vCutString);
                }
                if (vIndex186 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringDarkConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex187 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringDarkConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex188 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringDarkRedAddition, vCutString);
                }
                if (vIndex189 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringDarkGreenAddition, vCutString);
                }
                if (vIndex190 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringDarkBlueAddition, vCutString);
                }
                if (vIndex191 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringBrightConditionRGBSumLargerEqualThanValue, vCutString);
                }
                if (vIndex192 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringBrightConditionRGBSumLessThanValue, vCutString);
                }
                if (vIndex193 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringBrightRedAddition, vCutString);
                }
                if (vIndex194 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringBrightGreenAddition, vCutString);
                }
                if (vIndex195 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringBrightBlueAddition, vCutString);
                }
                if (vIndex196 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringGreenishConditionBlueIntegerFactor, vCutString);
                }
                if (vIndex197 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringGreenishConditionGreenIntegerFactor, vCutString);
                }
                if (vIndex198 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringGreenishRedAddition, vCutString);
                }
                if (vIndex199 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringGreenishGreenAddition, vCutString);
                }
                if (vIndex200 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringGreenishBlueAddition, vCutString);
                }
                if (vIndex201 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringRestRedAddition, vCutString);
                }
                if (vIndex202 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringRestGreenAddition, vCutString);
                }
                if (vIndex203 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mSpringRestBlueAddition, vCutString);
                }

                if (vIndex204 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mGeneralLighteness, vCutString);
                }
                if (vIndex205 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mGeneralColoringRed, vCutString);
                }
                if (vIndex206 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mGeneralColoringBlue, vCutString);
                }
                if (vIndex207 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mGeneralColoringGreen, vCutString);
                }

                if (vIndex208 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mLighteness, vCutString);
                }
                if (vIndex209 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringRed, vCutString);
                }
                if (vIndex210 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringBlue, vCutString);
                }
                if (vIndex211 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mColoringGreen, vCutString);
                }

                if (vIndex212 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mLighteness, vCutString);
                }
                if (vIndex213 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringRed, vCutString);
                }
                if (vIndex214 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringBlue, vCutString);
                }
                if (vIndex215 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mColoringGreen, vCutString);
                }

                if (vIndex216 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mLighteness, vCutString);
                }
                if (vIndex217 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringRed, vCutString);
                }
                if (vIndex218 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringBlue, vCutString);
                }
                if (vIndex219 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mColoringGreen, vCutString);
                }

                if (vIndex220 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringRed, vCutString);
                }
                if (vIndex221 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringBlue, vCutString);
                }
                if (vIndex222 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mColoringGreen, vCutString);
                }

                if (vIndex223 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringRed, vCutString);
                }
                if (vIndex224 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringBlue, vCutString);
                }
                if (vIndex225 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mColoringGreen, vCutString);
                }

                if (vIndex226 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringRed, vCutString);
                }
                if (vIndex227 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringBlue, vCutString);
                }
                if (vIndex228 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mColoringGreen, vCutString);
                }

                if (vIndex229 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex230 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex231 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex232 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex233 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex234 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex235 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex236 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex237 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex238 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex239 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = GetBooleanFromString(vCutString);
                }

                if (vIndex240 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex241 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex242 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex243 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex244 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex245 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex246 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex247 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex248 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex249 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex250 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = GetBooleanFromString(vCutString);
                }

                if (vIndex251 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex252 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex253 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1x, vCutString);
                }
                if (vIndex254 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1y, vCutString);
                }
                if (vIndex255 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex256 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex257 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2x, vCutString);
                }
                if (vIndex258 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2y, vCutString);
                }
                if (vIndex259 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionFirstHalfOrder, vCutString);
                }
                if (vIndex260 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionSecondHalfOrder, vCutString);
                }
                if (vIndex261 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mFlipFunction = GetBooleanFromString(vCutString);
                }
                if (vIndex262 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateTransitionPlotGraphicBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex263 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateCommandGraphicBitmap = GetBooleanFromString(vCutString);
                }

                if (vIndex264 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mGeneralBrightness, vCutString);
                }
                if (vIndex265 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mGeneralContrast, vCutString);
                }
                if (vIndex266 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mBrightness, vCutString);
                }
                if (vIndex267 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWaterTransition)].mContrast, vCutString);
                }
                if (vIndex268 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mBrightness, vCutString);
                }
                if (vIndex269 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eWater2Transition)].mContrast, vCutString);
                }
                if (vIndex270 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mBrightness, vCutString);
                }
                if (vIndex271 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionsColorParameters[(Int32)(tTransitionType.eBlendTransition)].mContrast, vCutString);
                }
                if (vIndex272 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mBrightness, vCutString);
                }
                if (vIndex273 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eWaterPool)].mContrast, vCutString);
                }
                if (vIndex274 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mBrightness, vCutString);
                }
                if (vIndex275 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eLandPool)].mContrast, vCutString);
                }
                if (vIndex276 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mBrightness, vCutString);
                }
                if (vIndex277 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mPoolsParameters[(Int32)(tPoolType.eBlendPool)].mContrast, vCutString);
                }
                if (vIndex278 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mShowVectorsInCommandGraphicBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex279 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mTransitionPlotGraphicSizeFactor, vCutString);
                }
                if (vIndex280 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mUseReversePoolPolygonOrderForKMLFile = GetBooleanFromString(vCutString);
                }

                if (vIndex281 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex282 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex283 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex284 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex285 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex286 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex287 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex288 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex289 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex290 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex291 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex292 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex293 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex294 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex295 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex296 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex297 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex298 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex299 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex300 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex301 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex302 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex303 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex304 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex305 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex306 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex307 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex308 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex309 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex310 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex311 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex312 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex313 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex314 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex315 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex316 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex317 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex318 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex319 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex320 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex321 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex322 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex323 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex324 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWaterTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }


                if (vIndex325 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex326 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex327 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex328 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex329 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex330 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex331 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex332 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex333 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex334 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex335 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex336 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex337 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex338 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex339 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex340 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex341 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex342 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex343 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex344 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex345 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex346 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex347 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex348 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex349 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex350 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex351 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex352 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex353 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex354 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex355 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex356 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex357 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex358 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex359 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex360 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex361 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex362 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex363 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex364 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex365 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex366 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex367 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex368 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eWater2Transition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }


                if (vIndex369 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex370 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex371 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex372 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex373 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex374 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex375 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex376 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex377 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex378 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex379 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eTransparency)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex380 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex381 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex382 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex383 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex384 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex385 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex386 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex387 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex388 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex389 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex390 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eReflection)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex391 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex392 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex393 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex394 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex395 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex396 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex397 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex398 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex399 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex400 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex401 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eLightness)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }

                if (vIndex402 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mUseEntrySFunctionDistanceLimit = GetBooleanFromString(vCutString);
                }
                if (vIndex403 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mStretchTransitionSAndExitSFunctionToFillAnyGap = GetBooleanFromString(vCutString);
                }
                if (vIndex404 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionDistanceLimit, vCutString);
                }
                if (vIndex405 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex406 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mEntrySFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex407 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex408 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mTransitionSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex409 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeBegin, vCutString);
                }
                if (vIndex410 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mExitSFunctionLinearSlopeEnd, vCutString);
                }
                if (vIndex411 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint1Slope, vCutString);
                }
                if (vIndex412 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mTrippleSFunctions[(Int32)(tTransitionType.eBlendTransition), (Int32)(tTransitionSubType.eColoring)].mSFunctionToSFunctionConnectionPoint2Slope, vCutString);
                }
                if (vIndex413 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mBlendBorderDistance, vCutString);
                }
                if (vIndex414 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateWaterForFS2004 = GetBooleanFromString(vCutString);
                }
                if (vIndex415 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mMergeWaterForFS2004 = GetBooleanFromString(vCutString);
                }
                if (vIndex416 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mSpareOutWaterForSeasonsGeneration = GetBooleanFromString(vCutString);
                }
                if (vIndex417 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetGreyConditionGreyToleranceValue, vCutString);
                }
                if (vIndex418 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetConditionRGBSumLessEqualThanValue, vCutString);
                }
                if (vIndex419 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetConditionRGBSumLargerThanValue, vCutString);
                }
                if (vIndex420 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mNightStreetLightDots1DitherProbabily, vCutString);
                }
                if (vIndex421 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mNightStreetLightDots2DitherProbabily, vCutString);
                }
                if (vIndex422 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignDoubleFromString(ref mNightStreetLightDots3DitherProbabily, vCutString);
                }
                if (vIndex423 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetRedAddition, vCutString);
                }
                if (vIndex424 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetGreenAddition, vCutString);
                }
                if (vIndex425 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetBlueAddition, vCutString);
                }
                if (vIndex426 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignSingleFromString(ref mNightNonStreetLightness, vCutString);
                }
                if (vIndex427 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot1Red, vCutString);
                }
                if (vIndex428 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot1Green, vCutString);
                }
                if (vIndex429 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot1Blue, vCutString);
                }
                if (vIndex430 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot2Red, vCutString);
                }
                if (vIndex431 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot2Green, vCutString);
                }
                if (vIndex432 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot2Blue, vCutString);
                }
                if (vIndex433 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot3Red, vCutString);
                }
                if (vIndex434 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot3Green, vCutString);
                }
                if (vIndex435 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    TryAssignInt32FromString(ref mNightStreetLightDot3Blue, vCutString);
                }
                if (vIndex436 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mNoSnowInWaterForWinterAndHardWinter = GetBooleanFromString(vCutString);
                }
                if (vIndex437 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mUseCSharpScripts = GetBooleanFromString(vCutString);
                }
            }
        }

        private static void AnalyseAreaEarthInfoFile()
        {
            for (Int32 vRow = 0; vRow < mAreaEarthInfoFileList.Count; vRow++)
            {
                String vFocus = mAreaEarthInfoFileList[vRow];
                Int32 vIndex1 = vFocus.IndexOf("AreaSourceBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex2 = vFocus.IndexOf("AreaMaskBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex3 = vFocus.IndexOf("AreaNightBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex4 = vFocus.IndexOf("AreaSpringBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex5 = vFocus.IndexOf("AreaAutumnBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex6 = vFocus.IndexOf("AreaWinterBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex7 = vFocus.IndexOf("AreaHardWinterBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex8 = vFocus.IndexOf("AreaKMLFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex9 = vFocus.IndexOf("AreaVectorsFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex10 = vFocus.IndexOf("AreaPixelCountInX", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex11 = vFocus.IndexOf("AreaPixelCountInY", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex12 = vFocus.IndexOf("AreaNWCornerLatitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex13 = vFocus.IndexOf("AreaNWCornerLongitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex14 = vFocus.IndexOf("AreaSECornerLatitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex15 = vFocus.IndexOf("AreaSECornerLongitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex16 = vFocus.IndexOf("CreateWaterMaskBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex17 = vFocus.IndexOf("CreateNightBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex18 = vFocus.IndexOf("CreateSpringBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex19 = vFocus.IndexOf("CreateAutumnBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex20 = vFocus.IndexOf("CreateWinterBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex21 = vFocus.IndexOf("CreateHardWinterBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex22 = vFocus.IndexOf("AreaSummerBitmapFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex23 = vFocus.IndexOf("UseAreaKMLFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex24 = vFocus.IndexOf("UseAreaVectorsFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex25 = vFocus.IndexOf("CreateFS2004MasksInsteadFSXMasks", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex26 = vFocus.IndexOf("CreateSummerBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex27 = vFocus.IndexOf("BlendNorthBorder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex28 = vFocus.IndexOf("BlendEastBorder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex29 = vFocus.IndexOf("BlendSouthBorder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex30 = vFocus.IndexOf("BlendWestBorder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex31 = vFocus.IndexOf("WorkFolder", StringComparison.CurrentCultureIgnoreCase);
                
                if (vIndex1 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaSourceBitmapFile = vCutString;
                }
                if (vIndex2 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaMaskBitmapFile = vCutString;
                }
                if (vIndex3 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaNightBitmapFile = vCutString;
                }
                if (vIndex4 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaSpringBitmapFile = vCutString;
                }
                if (vIndex5 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaAutumnBitmapFile = vCutString;
                }
                if (vIndex6 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaWinterBitmapFile = vCutString;
                }
                if (vIndex7 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaHardWinterBitmapFile = vCutString;
                }
                if (vIndex8 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaKMLFile = vCutString;
                }
                if (vIndex9 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaVectorsFile = vCutString;
                }
                if (vIndex10 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaPixelCountInX = Convert.ToInt32(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex11 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaPixelCountInY = Convert.ToInt32(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex12 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaNWCornerLatitude = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex13 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaNWCornerLongitude = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex14 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaSECornerLatitude = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex15 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaSECornerLongitude = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex16 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateWaterMaskBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex17 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateNightBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex18 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateSpringBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex19 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateAutumnBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex20 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateWinterBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex21 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateHardWinterBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex22 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaSummerBitmapFile = vCutString;
                }
                if (vIndex23 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mUseAreaKMLFile   = GetBooleanFromString(vCutString);
                }
                if (vIndex24 >= 0)
                {
                    String vCutString   = GetRightSideOfConfigString(vFocus);
                    mUseAreaVectorsFile = GetBooleanFromString(vCutString);
                }
                if (vIndex25 >= 0)
                {
                    String vCutString   = GetRightSideOfConfigString(vFocus);
                    mCreateFS2004MasksInsteadFSXMasks = GetBooleanFromString(vCutString);
                }
                if (vIndex26 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateSummerBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex27 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mBlendNorthBorder = GetBooleanFromString(vCutString);
                }
                if (vIndex28 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mBlendEastBorder = GetBooleanFromString(vCutString);
                }
                if (vIndex29 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mBlendSouthBorder = GetBooleanFromString(vCutString);
                }
                if (vIndex30 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mBlendWestBorder  = GetBooleanFromString(vCutString);
                }
                if (vIndex31 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mWorkFolder = vCutString;
                }
            }
        }

        private static void AnalyseAreaVectorsFile()
        {
            for (Int32 vRow = 0; vRow < mAreaVectorsFileList.Count; vRow++)
            {
                String vFocus = mAreaVectorsFileList[vRow];
                //and then...
            }
        }

        private static Boolean GetBooleanFromString(String iString)
        {
            Boolean vValue;
            if (MasksCommon.StringCompare(iString, "Yes"))
            {
                vValue = true;
            }
            else
            {
                vValue = false;
            }
            return vValue;
        }

        private static void TryAssignDoubleFromString(ref Double oValue, String iString)
        {
            try
            {
                Double vTempValue = Convert.ToDouble(iString, NumberFormatInfo.InvariantInfo);
                oValue = vTempValue;
            }
            catch
            {
                //ignore if failed
            }
        }

        private static void TryAssignSingleFromString(ref Single oValue, String iString)
        {
            try
            {
                Single vTempValue = Convert.ToSingle(iString, NumberFormatInfo.InvariantInfo);
                oValue = vTempValue;
            }
            catch
            {
                //ignore if failed
            }
        }

        private static void TryAssignInt32FromString(ref Int32 oValue, String iString)
        {
            try
            {
                Int32 vTempValue = Convert.ToInt32(iString, NumberFormatInfo.InvariantInfo);
                oValue = vTempValue;
            }
            catch
            {
                //ignore if failed
            }
        }

        private static void AddConfigLine(String iConfigLine)
        {
            //Remove Comment
            Int32 iIndexOfComment = iConfigLine.IndexOf("#", StringComparison.CurrentCultureIgnoreCase);
            if (iIndexOfComment >= 0)
            {
                String vCutString;
                vCutString = iConfigLine.Remove(iIndexOfComment);
                mFSEarthMasksConfigFileList.Add(vCutString);
            }
            else
            {
                mFSEarthMasksConfigFileList.Add(iConfigLine);
            }
        }

        private static void AddAreaEarthInfoLine(String iConfigLine)
        {
            //Remove Comment
            Int32 iIndexOfComment = iConfigLine.IndexOf("#", StringComparison.CurrentCultureIgnoreCase);
            if (iIndexOfComment >= 0)
            {
                String vCutString;
                vCutString = iConfigLine.Remove(iIndexOfComment);
                mAreaEarthInfoFileList.Add(vCutString);
            }
            else
            {
                mAreaEarthInfoFileList.Add(iConfigLine);
            }
        }


        private static void AddAreaVectorsLine(String iConfigLine)
        {
            //Remove Comment
            Int32 iIndexOfComment = iConfigLine.IndexOf("#", StringComparison.CurrentCultureIgnoreCase);
            if (iIndexOfComment >= 0)
            {
                String vCutString;
                vCutString = iConfigLine.Remove(iIndexOfComment);
                mAreaVectorsFileList.Add(vCutString);
            }
            else
            {
                mAreaVectorsFileList.Add(iConfigLine);
            }
        }

        public static Boolean AreLatLongParametersInConfigOK()
        {

            if ((mAreaPixelCountInX == 0) ||
                (mAreaPixelCountInY == 0) ||
                ((mAreaNWCornerLatitude - mAreaSECornerLatitude) == 0.0) ||
                ((mAreaSECornerLongitude - mAreaNWCornerLongitude) == 0.0))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static Boolean ParseCommandLineArguments(String[] iApplicationStartArguments)
        {
            Boolean vAllOK = true;

            if (iApplicationStartArguments.Length > 0)
            {
                mAreaEarthInfoFile = iApplicationStartArguments[0];
            }
            else
            {
                vAllOK = false;
                MessageBox.Show("FSEarhMasks requires an AreaEarthInfo input file passed as a command line argument!","Input File Missing!");
            }

            return vAllOK;
        }

    }
}
