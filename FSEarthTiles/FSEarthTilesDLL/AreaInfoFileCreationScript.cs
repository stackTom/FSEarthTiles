using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Globalization;
using FSEarthTilesInternalDLL;

namespace FSEarthTilesDLL
{
    public class AreaInfoFileCreationScript
    {
        //The following Methodes will be called by FSEarthTiles:
        //SaveAreaInfo(EarthArea iEarthArea, String iAreaFileNameMiddlePart)

        struct tBGLDatasStringPackage
        {
            public String mBGLStartLongitudeString;
            public String mBGLStopLongitudeString;
            public String mBGLStartLatitudeString;
            public String mBGLStopLatitudeString;
            public String mBGLXPixelAngleString;
            public String mBGLYPixelAngleString;
            public String mBGLPixelsInXString;
            public String mBGLPixelsInYString;
            public String mBGLFS2004SeasonsDesitnationStartLongitudeString;
            public String mBGLFS2004SeasonsDesitnationStopLongitudeString;
            public String mBGLFS2004SeasonsDesitnationStartLatitudeString;
            public String mBGLFS2004SeasonsDesitnationStopLatitudeString;
        }


        public void SaveAreaInfo(EarthArea iEarthArea, String iAreaFileNameMiddlePart)  //Entry Methode
        {
            tBGLDatasStringPackage vBGLDatasStringPackage;
            
            vBGLDatasStringPackage = CreateBGLStringPackage(iEarthArea);

            try
            {
                //General Single Source AreaFSInfo.inf File working for FS2004 and FSX
                WriteAreaFSInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart);

                if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FS2004"))
                {
                    //MultiSource (Water, Night and Seasons) AreaFS2004MasksInfo.inf File working for FS2004 only              
                    WriteAreaFS2004MasksInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart);        //water             -> AreaFS2004MasksInfo.inf
                    WriteAreaFS2004MasksSeasonsInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart); //night and seasons -> AreaFS2004MasksSeasonsInfo.inf
                }
                else
                {
                    //MultiSource (Water, Night and Seasons) AreaFSXMasksInfo.inf File working for FSX only                 
                    WriteAreaFSXMasksInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart);           //water             -> AreaFSXMasksInfo.inf
                    WriteAreaFSXMasksSeasonsInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart);    //night and seasons -> AreaFSXMasksSeasonsInfo.inf
                }

                //Are Information passed to FSEarthMasks, AreaEarthInfo.txt File
                WriteAreaAreaEarthInfoFile(vBGLDatasStringPackage, iAreaFileNameMiddlePart);

            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save AreaInfo Files! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
        }


        private tBGLDatasStringPackage CreateBGLStringPackage(EarthArea iEarthArea)
        {

            tBGLDatasStringPackage vBGLDatasStringPackage;

            //Prepare required BGL Datas
            Double vBGLStartLongitude;
            Double vBGLEndLongitude;
            Double vBGLStartLatitude;
            Double vBGLEndLatitude;
            Int64  vBGLPixelsInX;
            Int64  vBGLPixelsInY;
            Double vBGLFS2004SeasonsDesitnationStartLongitude;
            Double vBGLFS2004SeasonsDesitnationEndLongitude;
            Double vBGLFS2004SeasonsDesitnationStartLatitude;
            Double vBGLFS2004SeasonsDesitnationEndLatitude;

            if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
            {
                //FS-Pixel-Grid Resampled Texture
                vBGLStartLongitude = iEarthArea.AreaFSResampledStartLongitude;
                vBGLEndLongitude   = iEarthArea.AreaFSResampledStopLongitude;
                vBGLStartLatitude  = iEarthArea.AreaFSResampledStartLatitude;
                vBGLEndLatitude    = iEarthArea.AreaFSResampledStopLatitude;
                vBGLPixelsInX      = iEarthArea.AreaFSResampledPixelsInX;
                vBGLPixelsInY      = iEarthArea.AreaFSResampledPixelsInY;
            }
            else
            {
                //Normal Texture
                vBGLStartLongitude = iEarthArea.AreaSnapStartLongitude;
                vBGLEndLongitude   = iEarthArea.AreaSnapStopLongitude;
                vBGLStartLatitude  = iEarthArea.AreaSnapStartLatitude;
                vBGLEndLatitude    = iEarthArea.AreaSnapStopLatitude;
                vBGLPixelsInX      = iEarthArea.AreaPixelsInX;
                vBGLPixelsInY      = iEarthArea.AreaPixelsInY;
            }

            Double vBGLXPixelAngle;
            Double vBGLYPixelAngle;

            vBGLYPixelAngle = (vBGLStartLatitude - vBGLEndLatitude) / Convert.ToDouble(vBGLPixelsInY);
            if (vBGLEndLongitude >= vBGLStartLongitude)
            {
                vBGLXPixelAngle = (vBGLEndLongitude - vBGLStartLongitude) / Convert.ToDouble(vBGLPixelsInX);
            }
            else
            {
                vBGLXPixelAngle = (360.0 + vBGLEndLongitude - vBGLStartLongitude) / Convert.ToDouble(vBGLPixelsInX);
            }

            //A bug in FS2004 resample makes it required to subtract a 0.5 Pixel wide border from the true Area size for the FS2004 Seasons inf files Destination section.
            //Without that you get a water border (creates empty LOD13)
            vBGLFS2004SeasonsDesitnationStartLongitude = vBGLStartLongitude + 0.5 * vBGLXPixelAngle;
            vBGLFS2004SeasonsDesitnationEndLongitude   = vBGLEndLongitude   - 0.5 * vBGLXPixelAngle;
            vBGLFS2004SeasonsDesitnationStartLatitude  = vBGLStartLatitude  - 0.5 * vBGLYPixelAngle;
            vBGLFS2004SeasonsDesitnationEndLatitude    = vBGLEndLatitude    + 0.5 * vBGLYPixelAngle;

            //Just an Info
            //If you want to use FS Pixel center mode instead corner mode you can calculate them with this:  (but don't forget to set the FS pixel mode in the .inf File according to your change also.)
            //I tested it and it makes no difference if you make it corret. Note that FSET is all based on corner: All Pixels are within the specified borders in full!
            //for center mode: vBGLStartLongitude += 0.5 * vBGLXPixelAngle;
            //for center mode: vBGLEndLongitude   -= 0.5 * vBGLXPixelAngle;
            //for center mode: vBGLStartLatitude  -= 0.5 * vBGLYPixelAngle;
            //for center mode: vBGLEndLatitude    += 0.5 * vBGLYPixelAngle;

            vBGLDatasStringPackage.mBGLStartLongitudeString = Convert.ToString(vBGLStartLongitude, NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLStopLongitudeString  = Convert.ToString(vBGLEndLongitude,   NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLStartLatitudeString  = Convert.ToString(vBGLStartLatitude,  NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLStopLatitudeString   = Convert.ToString(vBGLEndLatitude,    NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLXPixelAngleString    = Convert.ToString(vBGLXPixelAngle,    NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLYPixelAngleString    = Convert.ToString(vBGLYPixelAngle,    NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLPixelsInXString      = Convert.ToString(vBGLPixelsInX,      NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLPixelsInYString      = Convert.ToString(vBGLPixelsInY,      NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStartLongitudeString = Convert.ToString(vBGLFS2004SeasonsDesitnationStartLongitude, NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStopLongitudeString  = Convert.ToString(vBGLFS2004SeasonsDesitnationEndLongitude,   NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStartLatitudeString  = Convert.ToString(vBGLFS2004SeasonsDesitnationStartLatitude,  NumberFormatInfo.InvariantInfo);
            vBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStopLatitudeString   = Convert.ToString(vBGLFS2004SeasonsDesitnationEndLatitude,    NumberFormatInfo.InvariantInfo);
            return vBGLDatasStringPackage;
        }


        private void WriteAreaFSInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {

            //------- AreaFSInfo.inf ----------

            //General Single Source AreaFSInfo.inf File working for FS2004 and FSX

            StreamWriter vStream;

            String vAreaInfoFileName = "AreaFSInfo" + iAreaFileNameMiddlePart + ".inf";

            String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaInfoFileName;

            vStream = new StreamWriter(vFilename);

            if (vStream != null)
            {
                String vAreaBmpFileName;

                if ((EarthConfig.mCreateAreaMask) && (EarthConfig.mCreateSummerBitmap))
                {
                    vAreaBmpFileName = "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                }
                else
                {
                    vAreaBmpFileName = "Area" + iAreaFileNameMiddlePart + ".bmp";
                }

                String vAreaDestFileName = "Area" + iAreaFileNameMiddlePart;

                vStream.WriteLine("[Destination]");
                vStream.WriteLine("DestDir	            = " + EarthConfig.mSceneryFolderScenery);
                vStream.WriteLine("DestBaseFileName     = " + vAreaDestFileName);
                vStream.WriteLine("BuildSeasons 	    = 0");
                vStream.WriteLine("UseSourceDimensions  = 1");
                vStream.WriteLine("CompressionQuality   = " + Convert.ToString(EarthConfig.mCompressionQuality));

                if (EarthConfig.mUseLODLimits)
                {
                    //We only add the LOD Line when the Texture is lesser the LOD Limit, else you limit your High Resolution Textures to that LOD! 
                    //Same Formula as for mAreaFSResampledLOD
                    Int64 vFSResampledLOD = EarthMath.cLevel0CodeDeep - EarthConfig.mFetchLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD.
                    if (vFSResampledLOD <= EarthConfig.mMinimumDestinationLOD)
                    {
                        vStream.WriteLine("LOD = Auto," + Convert.ToString(EarthConfig.mMinimumDestinationLOD));
                    }
                }

                vStream.WriteLine();
                vStream.WriteLine("[Source]");
                vStream.WriteLine("Type	      = Custom");
                vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                WriteSourceCommonPart(vStream, iBGLDatasStringPackage);

                vStream.Close();
            }

        }

        private void WriteAreaFS2004MasksInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {
            //------- AreaFS2004MasksInfo.inf for Water -----------

            //MultiSource  AreaFS2004MasksInfo.inf File

            if ((EarthConfig.mCreateAreaMask) && (EarthConfig.mCreateWaterMaskBitmap))
            {
                StreamWriter vStream;

                String vAreaMaskInfoFileName = "AreaFS2004MasksInfo" + iAreaFileNameMiddlePart + ".inf";

                String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaMaskInfoFileName;

                vStream = new StreamWriter(vFilename);
                if (vStream != null)
                {

                    String vAreaBmpFileName;

                    if (EarthConfig.mCreateSummerBitmap)
                    {
                        vAreaBmpFileName = "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                    }
                    else
                    {
                        vAreaBmpFileName = "Area" + iAreaFileNameMiddlePart + ".bmp";
                    }

                    String vAreaDestFileName = "Area" + iAreaFileNameMiddlePart;

                    vStream.WriteLine("[Destination]");
                    vStream.WriteLine("DestDir	            = " + EarthConfig.mSceneryFolderScenery);
                    vStream.WriteLine("DestBaseFileName     = " + vAreaDestFileName);
                    vStream.WriteLine("BuildSeasons 	    = 0");
                    vStream.WriteLine("UseSourceDimensions  = 1");
                    vStream.WriteLine("CompressionQuality   = " + Convert.ToString(EarthConfig.mCompressionQuality));

                    if (EarthConfig.mUseLODLimits)
                    {
                        //We only add the LOD Line when the Texture is lesser the LOD Limit, else you limit your High Resolution Textures to that LOD! 
                        //Same Formula as for mAreaFSResampledLOD
                        Int64 vFSResampledLOD = EarthMath.cLevel0CodeDeep - EarthConfig.mFetchLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD.
                        if (vFSResampledLOD <= EarthConfig.mMinimumDestinationLOD)
                        {
                            vStream.WriteLine("LOD = Auto," + Convert.ToString(EarthConfig.mMinimumDestinationLOD));
                        }
                    }

                    vStream.WriteLine();
                    vStream.WriteLine("[Source]");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);

                    vStream.Close();
                }
            }
        }

        private void WriteAreaFS2004MasksSeasonsInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {
            //------- AreaFS2004MasksSeasonsInfo.inf for Water, Night and Seasons -----------

            //MultiSource (Water, Night and Seasons) AreaFS2004MasksSeasonsInfo.inf File

            if ((EarthConfig.mCreateAreaMask) && (EarthConfig.IsWithSeasons()))
            {
                StreamWriter vStream;

                String vAreaMaskInfoFileName = "AreaFS2004MasksSeasonsInfo" + iAreaFileNameMiddlePart + ".inf";

                String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaMaskInfoFileName;

                vStream = new StreamWriter(vFilename);
                if (vStream != null)
                {

                    String vAreaBmpFileName;

                    if (EarthConfig.mCreateSummerBitmap)
                    {
                        vAreaBmpFileName = "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                    }
                    else
                    {
                        vAreaBmpFileName = "Area" + iAreaFileNameMiddlePart + ".bmp";
                    }

                    String vAreaDestFileName = "Area" + iAreaFileNameMiddlePart;

                    vStream.WriteLine("[Destination]");
                    vStream.WriteLine("DestDir	            = " + EarthConfig.mSceneryFolderScenery);
                    vStream.WriteLine("DestBaseFileName     = " + vAreaDestFileName);
                    vStream.WriteLine("BuildSeasons 	    = 1");
                    vStream.WriteLine("UseSourceDimensions  = 0");
                    vStream.WriteLine("NorthLat             = " + iBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStartLatitudeString  + "       ;This special for FS2004 required seasons destination coords have to be the true area coords (LOD13) minus a half pixel border.");
                    vStream.WriteLine("SouthLat             = " + iBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStopLatitudeString   + "       ;That is just one more bug in FS2004 resample.");
                    vStream.WriteLine("WestLon              = " + iBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStartLongitudeString);
                    vStream.WriteLine("EastLon              = " + iBGLDatasStringPackage.mBGLFS2004SeasonsDesitnationStopLongitudeString);
                    vStream.WriteLine("CompressionQuality   = " + Convert.ToString(EarthConfig.mCompressionQuality));

                    if (EarthConfig.mUseLODLimits)
                    {
                        //We only add the LOD Line when the Texture is lesser the LOD Limit, else you limit your High Resolution Textures to that LOD! 
                        //Same Formula as for mAreaFSResampledLOD
                        Int64 vFSResampledLOD = EarthMath.cLevel0CodeDeep - EarthConfig.mFetchLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD.
                        if (vFSResampledLOD <= EarthConfig.mMinimumDestinationLOD)
                        {
                            vStream.WriteLine("LOD = Auto," + Convert.ToString(EarthConfig.mMinimumDestinationLOD));
                        }
                    }

                    Int32 vTotalSources = 6;

                    vStream.WriteLine();
                    vStream.WriteLine("[Source]");
                    vStream.WriteLine("Type            = MultiSource");
                    vStream.WriteLine("NumberOfSources = " + Convert.ToString(vTotalSources));

                    Int32 vSourceNr = 1;

                    //Source1 is original Downloaded Bitmap or the SummerBitmap
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = Summer");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);


                    vSourceNr++;
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = LightMap");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    if (EarthConfig.mCreateNightBitmap)
                    {
                        vStream.WriteLine("SourceFile = " + "AreaNight" + iAreaFileNameMiddlePart + ".bmp");
                    }
                    else
                    {
                        vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    }
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);

                    vSourceNr++;
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = Spring");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    if (EarthConfig.mCreateSpringBitmap)
                    {
                        vStream.WriteLine("SourceFile = " + "AreaSpring" + iAreaFileNameMiddlePart + ".bmp");
                    }
                    else
                    {
                        vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    }
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);


                    vSourceNr++;
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = Fall");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    if (EarthConfig.mCreateAutumnBitmap)
                    {
                        vStream.WriteLine("SourceFile = " + "AreaAutumn" + iAreaFileNameMiddlePart + ".bmp");
                    }
                    else
                    {
                        vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    }
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);


                    vSourceNr++;
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = Winter");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    if (EarthConfig.mCreateWinterBitmap)
                    {
                        vStream.WriteLine("SourceFile = " + "AreaWinter" + iAreaFileNameMiddlePart + ".bmp");
                    }
                    else
                    {
                        vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    }
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);


                    vSourceNr++;
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = HardWinter");
                    vStream.WriteLine("Type	      = Custom");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    if (EarthConfig.mCreateHardWinterBitmap)
                    {
                        vStream.WriteLine("SourceFile = " + "AreaHardWinter" + iAreaFileNameMiddlePart + ".bmp");
                    }
                    else
                    {
                        vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    }
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);

                    vStream.Close();
                }
            }
        }

        private void WriteAreaFSXMasksInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {
            //------- AreaFSXMasksInfo.inf for Water -----------

            //MultiSource (Water) AreaFSXMasksInfo.inf File working for FSX only

            if ((EarthConfig.mCreateAreaMask) && (EarthConfig.mCreateWaterMaskBitmap))
            {
                StreamWriter vStream;

                String vAreaMaskInfoFileName = "AreaFSXMasksInfo" + iAreaFileNameMiddlePart + ".inf";

                String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaMaskInfoFileName;

                vStream = new StreamWriter(vFilename);
                if (vStream != null)
                {

                    String vAreaBmpFileName;

                    if (EarthConfig.mCreateSummerBitmap)
                    {
                        vAreaBmpFileName = "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                    }
                    else
                    {
                        vAreaBmpFileName = "Area" + iAreaFileNameMiddlePart + ".bmp";
                    }

                    String vAreaMaskFileName = "AreaMask" + iAreaFileNameMiddlePart + ".bmp";
                    String vAreaDestFileName = "Area" + iAreaFileNameMiddlePart;

                    vStream.WriteLine("[Destination]");
                    vStream.WriteLine("DestDir	            = " + EarthConfig.mSceneryFolderScenery);
                    vStream.WriteLine("DestBaseFileName     = " + vAreaDestFileName);
                    vStream.WriteLine("BuildSeasons 		= 0");
                    vStream.WriteLine("UseSourceDimensions 	= 1");
                    vStream.WriteLine("CompressionQuality   = " + Convert.ToString(EarthConfig.mCompressionQuality));

                    if (EarthConfig.mUseLODLimits)
                    {
                        //We only add the LOD Line when the Texture is lesser the LOD Limit, else you limit your High Resolution Textures to that LOD! 
                        //Same Formula as for mAreaFSResampledLOD
                        Int64 vFSResampledLOD = EarthMath.cLevel0CodeDeep - EarthConfig.mFetchLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD.
                        if (vFSResampledLOD <= EarthConfig.mMinimumDestinationLOD)
                        {
                            vStream.WriteLine("LOD = Auto," + Convert.ToString(EarthConfig.mMinimumDestinationLOD));
                        }
                    }

                    vStream.WriteLine();
                    vStream.WriteLine("[Source]");
                    vStream.WriteLine("Type            = MultiSource");
                    vStream.WriteLine("NumberOfSources = 2");

                    vStream.WriteLine();
                    vStream.WriteLine("[Source1]");
                    vStream.WriteLine("Type	      = BMP");
                    vStream.WriteLine("Layer	      = Imagery");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                    WriteSourceFSXWaterMaskReference(vStream,2);

                    vStream.WriteLine();
                    vStream.WriteLine("[Source2]");
                    vStream.WriteLine("Type	      = BMP");
                    vStream.WriteLine("Layer	      = None");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    vStream.WriteLine("SourceFile = " + vAreaMaskFileName);
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);

                    vStream.Close();
                }
            }
        }

        private void WriteAreaFSXMasksSeasonsInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {
            //------- AreaFSXMasksSeasonsInfo.inf for Water -----------

            //MultiSource (Water) AreaFSXMasksSeasonsInfo.inf File working for FSX only

            if ((EarthConfig.mCreateAreaMask) && (EarthConfig.IsWithSeasons()))
            {
                StreamWriter vStream;

                String vAreaMaskInfoFileName = "AreaFSXMasksSeasonsInfo" + iAreaFileNameMiddlePart + ".inf";

                String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaMaskInfoFileName;

                vStream = new StreamWriter(vFilename);
                if (vStream != null)
                {

                    String vAreaBmpFileName;

                    if (EarthConfig.mCreateSummerBitmap)
                    {
                        vAreaBmpFileName = "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                    }
                    else
                    {
                        vAreaBmpFileName = "Area" + iAreaFileNameMiddlePart + ".bmp";
                    }

                    String vAreaMaskFileName = "AreaMask" + iAreaFileNameMiddlePart + ".bmp";
                    String vAreaDestFileName = "Area" + iAreaFileNameMiddlePart;

                    vStream.WriteLine("[Destination]");
                    vStream.WriteLine("DestDir	            = " + EarthConfig.mSceneryFolderScenery);
                    vStream.WriteLine("DestBaseFileName     = " + vAreaDestFileName);
                    vStream.WriteLine("BuildSeasons 		= 1");
                    vStream.WriteLine("UseSourceDimensions 	= 1");
                    vStream.WriteLine("CompressionQuality   = " + Convert.ToString(EarthConfig.mCompressionQuality));

                    if (EarthConfig.mUseLODLimits)
                    {
                        //We only add the LOD Line when the Texture is lesser the LOD Limit, else you limit your High Resolution Textures to that LOD! 
                        //Same Formula as for mAreaFSResampledLOD
                        Int64 vFSResampledLOD = EarthMath.cLevel0CodeDeep - EarthConfig.mFetchLevel - 1;  //plain it would be EarthMath.cLevel0CodeDeep - iLevel - 2 but we resample into a higher LOD.
                        if (vFSResampledLOD <= EarthConfig.mMinimumDestinationLOD)
                        {
                            vStream.WriteLine("LOD = Auto," + Convert.ToString(EarthConfig.mMinimumDestinationLOD));
                        }
                    }

                    Int32 vTotalSources = GetSeasonsCount() + 1;

                    if (EarthConfig.mCreateWaterMaskBitmap)
                    {
                        vTotalSources++;
                    }

                    vStream.WriteLine();
                    vStream.WriteLine("[Source]");
                    vStream.WriteLine("Type            = MultiSource");
                    vStream.WriteLine("NumberOfSources = " + Convert.ToString(vTotalSources));

                    Int32 vSourceNr = 1;

                    String vSpringVariation     = "March,April,May";
                    String vSummerVariation     = "June,July,August";
                    String vAutumnVariation     = "September,October";
                    String vWinterVariation     = "November";
                    String vHardWinterVariation = "December,January,February";

                    if (!IsNorthHalfSphere(iBGLDatasStringPackage))
                    {
                        vSpringVariation     = "September,October,November";
                        vSummerVariation     = "December,January,February";
                        vAutumnVariation     = "March,April";
                        vWinterVariation     = "May";
                        vHardWinterVariation = "June,July,August";
                    }

                    if (!EarthConfig.mCreateSpringBitmap)
                    {
                        vSummerVariation = vSpringVariation + "," + vSummerVariation;
                    }
                    if (!EarthConfig.mCreateAutumnBitmap)
                    {
                        vSummerVariation = vSummerVariation + "," + vAutumnVariation;
                    }
                    if (!EarthConfig.mCreateWinterBitmap)
                    {
                        vSummerVariation = vSummerVariation + "," + vWinterVariation;
                    }
                    if (!EarthConfig.mCreateHardWinterBitmap)
                    {
                        vSummerVariation = vSummerVariation + "," + vHardWinterVariation;
                    }

                    //Source1 is original Downloaded Bitmap or the SummerBitmap
                    vStream.WriteLine();
                    vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                    vStream.WriteLine("Season	  = Summer");
                    vStream.WriteLine("Variation  = " + vSummerVariation);
                    vStream.WriteLine("Type	      = BMP");
                    vStream.WriteLine("Layer	  = Imagery");
                    vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                    vStream.WriteLine("SourceFile = " + vAreaBmpFileName);
                    WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                    WriteSourceFSXWaterMaskReference(vStream, vTotalSources);

                    if (EarthConfig.mCreateNightBitmap)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Season	  = LightMap");
                        vStream.WriteLine("Variation  = LightMap");
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = Imagery");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + "AreaNight" + iAreaFileNameMiddlePart + ".bmp");
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                        WriteSourceFSXWaterMaskReference(vStream, vTotalSources);
                    }
                    if (EarthConfig.mCreateSpringBitmap)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Season	  = Spring");
                        vStream.WriteLine("Variation  = " + vSpringVariation);     
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = Imagery");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + "AreaSpring" + iAreaFileNameMiddlePart + ".bmp");
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                        WriteSourceFSXWaterMaskReference(vStream, vTotalSources);
                    }
                    if (EarthConfig.mCreateAutumnBitmap)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Season	  = Fall");
                        vStream.WriteLine("Variation  = " + vAutumnVariation);  
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = Imagery");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + "AreaAutumn" + iAreaFileNameMiddlePart + ".bmp");
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                        WriteSourceFSXWaterMaskReference(vStream, vTotalSources);
                    }
                    if (EarthConfig.mCreateWinterBitmap)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Season	  = Winter");
                        vStream.WriteLine("Variation  = " + vWinterVariation);  
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = Imagery");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + "AreaWinter" + iAreaFileNameMiddlePart + ".bmp");
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                        WriteSourceFSXWaterMaskReference(vStream, vTotalSources);
                    }
                    if (EarthConfig.mCreateHardWinterBitmap)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Season	  = HardWinter");
                        vStream.WriteLine("Variation  = " + vHardWinterVariation); 
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = Imagery");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + "AreaHardWinter" + iAreaFileNameMiddlePart + ".bmp");
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                        WriteSourceFSXWaterMaskReference(vStream, vTotalSources);
                    }
                    if (EarthConfig.mCreateWaterMaskBitmap)  //Water Mask has to be the last that it works (FSX resample bug?)
                    {
                        vSourceNr++;
                        vStream.WriteLine();
                        vStream.WriteLine("[Source" + Convert.ToString(vSourceNr) + "]");
                        vStream.WriteLine("Type	      = BMP");
                        vStream.WriteLine("Layer	  = None");
                        vStream.WriteLine("SourceDir  = " + EarthConfig.mWorkFolder);
                        vStream.WriteLine("SourceFile = " + vAreaMaskFileName);
                        WriteSourceCommonPart(vStream, iBGLDatasStringPackage);
                    }

                    vStream.Close();
                }
            }
        }

        private void WriteSourceCommonPart(StreamWriter iStream, tBGLDatasStringPackage iBGLDatasStringPackage)
        {
            //------- inf file [Source] common part for FSX and FS2004
            iStream.WriteLine("Lon               = " + iBGLDatasStringPackage.mBGLStartLongitudeString + "       ;for top left and bottom right is: " + iBGLDatasStringPackage.mBGLStopLongitudeString);
            iStream.WriteLine("Lat               = " + iBGLDatasStringPackage.mBGLStartLatitudeString + "       ;for top left and bottom right is: " + iBGLDatasStringPackage.mBGLStopLatitudeString);
            iStream.WriteLine("NumOfCellsPerLine = " + iBGLDatasStringPackage.mBGLPixelsInXString + "       ;Pixel is not used in FSX");
            iStream.WriteLine("NumOfLines        = " + iBGLDatasStringPackage.mBGLPixelsInYString + "       ;Pixel is not used in FSX");

            if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FSX"))
            {
                iStream.WriteLine("xDim = " + iBGLDatasStringPackage.mBGLXPixelAngleString);
                iStream.WriteLine("yDim = " + iBGLDatasStringPackage.mBGLYPixelAngleString);
            }
            else
            {
                iStream.WriteLine("CellXdimensionDeg = " + iBGLDatasStringPackage.mBGLXPixelAngleString);
                iStream.WriteLine("CellYdimensionDeg = " + iBGLDatasStringPackage.mBGLYPixelAngleString);
            }

            if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
            {
                iStream.WriteLine("PixelIsPoint      = 0");   //for center mode: vStream.WriteLine("PixelIsPoint      = 1"); 
                iStream.WriteLine("SamplingMethod    = Point");
            }
            else
            {
                //FSEarthDefault
                iStream.WriteLine("PixelIsPoint      = 0");
            }
        }

        private void WriteSourceFSXWaterMaskReference(StreamWriter iStream, Int32 iWaterMaskSourcePosition)
        {
            if (EarthConfig.mCreateWaterMaskBitmap)
            {
                String vSourcePosition = Convert.ToString(iWaterMaskSourcePosition);

                iStream.WriteLine("Channel_BlendMask     = " + vSourcePosition + ".0  ;red  channel, (note:green channel is the grey-info)");
                iStream.WriteLine("Channel_LandWaterMask = " + vSourcePosition + ".2  ;blue channel");
            }
        }


        private Int32 GetSeasonsCount()  //summer and water do not count as season
        {
            Int32 vSeasonsCount = 0;

            if (EarthConfig.mCreateNightBitmap)
            {
               vSeasonsCount++;
            }
            if (EarthConfig.mCreateSpringBitmap)
            {
               vSeasonsCount++;
            }
            if (EarthConfig.mCreateAutumnBitmap)
            {
               vSeasonsCount++;
            }
            if (EarthConfig.mCreateWinterBitmap)
            {
               vSeasonsCount++;
            }
            if (EarthConfig.mCreateHardWinterBitmap)
            {
                vSeasonsCount++;
            }
            return vSeasonsCount;
        }


        private Boolean IsNorthHalfSphere(tBGLDatasStringPackage iBGLDatasStringPackage)
        {
            Boolean vIsNorthHalfSphere = false;

            Double vStartLat = Convert.ToDouble(iBGLDatasStringPackage.mBGLStartLatitudeString, NumberFormatInfo.InvariantInfo);
            Double vStopLat  = Convert.ToDouble(iBGLDatasStringPackage.mBGLStopLatitudeString, NumberFormatInfo.InvariantInfo);
            Double vAverageLat = (vStartLat + vStopLat) / 2.0;

            if (vAverageLat >= 0.0)
            {
                vIsNorthHalfSphere = true;
            }

            return vIsNorthHalfSphere;
        }


        private void WriteAreaAreaEarthInfoFile(tBGLDatasStringPackage iBGLDatasStringPackage, String iAreaFileNameMiddlePart)
        {
            //------- AreaEarthInfo.txt ---------

            //Are Information passed to FSEarthMasks, AreaEarthInfo.txt File

            StreamWriter vStream;

            String vAreaEarthInfoFileName = "AreaEarthInfo" + iAreaFileNameMiddlePart + ".txt";

            String vFilename = EarthConfig.mWorkFolder + "\\" + vAreaEarthInfoFileName;

            vStream = new StreamWriter(vFilename);
            if (vStream != null)
            {
                String vAreaFullFilePath = EarthConfig.mWorkFolder + "\\" + "Area" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaSummerFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaSummer" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaMaskFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaMask" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaNightFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaNight" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaSpringFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaSpring" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaAutumnFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaAutumn" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaWinterFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaWinter" + iAreaFileNameMiddlePart + ".bmp";
                String vAreaHardWinterFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaHardWinter" + iAreaFileNameMiddlePart + ".bmp";

                String vAreaKMLFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaKML" + ".kml";
                String vAreaVectorsFullFilePath = EarthConfig.mWorkFolder + "\\" + "Area" + iAreaFileNameMiddlePart + ".svg";

                vStream.WriteLine("#       FS Earth Tiles  " + vAreaEarthInfoFileName);
                vStream.WriteLine("#                                        ");
                vStream.WriteLine("#       This file is generated by FSEarthTiles ");
                vStream.WriteLine("#       and is used as an input file for FSEarthMasks");
                vStream.WriteLine("#--------------------------------------------------------------------");
                vStream.WriteLine();
                vStream.WriteLine("WorkFolder     = " + EarthConfig.mWorkFolder);
                vStream.WriteLine();

                if (EarthConfig.mBlendNorthBorder)
                {
                    vStream.WriteLine("BlendNorthBorder = Yes");
                }
                else
                {
                    vStream.WriteLine("BlendNorthBorder = No");
                }
                if (EarthConfig.mBlendEastBorder)
                {
                    vStream.WriteLine("BlendEastBorder  = Yes");
                }
                else
                {
                    vStream.WriteLine("BlendEastBorder  = No");
                }
                if (EarthConfig.mBlendSouthBorder)
                {
                    vStream.WriteLine("BlendSouthBorder = Yes");
                }
                else
                {
                    vStream.WriteLine("BlendSouthBorder = No");
                }
                if (EarthConfig.mBlendWestBorder)
                {
                    vStream.WriteLine("BlendWestBorder  = Yes");
                }
                else
                {
                    vStream.WriteLine("BlendWestBorder  = No");
                }

                vStream.WriteLine();

                if (EarthConfig.mCreateWaterMaskBitmap)
                {
                    vStream.WriteLine("CreateWaterMaskBitmap      = Yes    # yes = create Water/Blend Mask. (An AreaVectors file is required for this!)");
                }
                else
                {
                    vStream.WriteLine("CreateWaterMaskBitmap      = No     # yes = create Water/Blend Mask. (An AreaVectors file is required for this!)");
                }


                if (EarthConfig.mCreateSummerBitmap)
                {
                    vStream.WriteLine("CreateSummerBitmap          = Yes    # yes = create Summer  Texture");
                }
                else
                {
                    vStream.WriteLine("CreateSummerBitmap          = No     # yes = create Summer  Texture");
                }

                if (EarthConfig.mCreateNightBitmap)
                {
                    vStream.WriteLine("CreateNightBitmap          = Yes    # yes = create Night  Texture");
                }
                else
                {
                    vStream.WriteLine("CreateNightBitmap          = No     # yes = create Night  Texture");
                }

                if (EarthConfig.mCreateSpringBitmap)
                {
                    vStream.WriteLine("CreateSpringBitmap         = Yes    # yes = create Spring Texture");
                }
                else
                {
                    vStream.WriteLine("CreateSpringBitmap         = No     # yes = create Spring Texture");
                }

                if (EarthConfig.mCreateAutumnBitmap)
                {
                    vStream.WriteLine("CreateAutumnBitmap         = Yes    # yes = create Autumn Texture");
                }
                else
                {
                    vStream.WriteLine("CreateAutumnBitmap         = No     # yes = create Autumn Texture");
                }

                if (EarthConfig.mCreateWinterBitmap)
                {
                    vStream.WriteLine("CreateWinterBitmap         = Yes    # yes = create Winter Textur");
                }
                else
                {
                    vStream.WriteLine("CreateWinterBitmap         = No     # yes = create Winter Textur");
                }

                if (EarthConfig.mCreateHardWinterBitmap)
                {
                    vStream.WriteLine("CreateHardWinterBitmap     = Yes    # yes = create Hard Winter Texture");
                }
                else
                {
                    vStream.WriteLine("CreateHardWinterBitmap     = No     # yes = create Hard Winter Texture");
                }
                vStream.WriteLine();
                if (EarthConfig.mUseAreaKMLFile)
                {
                    vStream.WriteLine("UseAreaKMLFile             = Yes    # yes = use a KML File");
                }
                else
                {
                    vStream.WriteLine("UseAreaKMLFile             = No     # yes = use a KML File");
                }
                if (EarthConfig.mUseScalableVectorGraphicsTool)
                {
                    vStream.WriteLine("UseAreaVectorsFile         = Yes    # yes = use a SVG (ScalableVectorGraphics) File");
                }
                else
                {
                    vStream.WriteLine("UseAreaVectorsFile         = No     # yes = use a SVG (ScalableVectorGraphics) File");
                }
                vStream.WriteLine();
                if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FS2004"))
                {
                    vStream.WriteLine("CreateFS2004MasksInsteadFSXMasks     = Yes     # yes = create Masks for FS2004. Water regions will be painted pitch black.");
                }
                else
                {
                    vStream.WriteLine("CreateFS2004MasksInsteadFSXMasks     = No      # yes = create Masks for FS2004. Water regions will be painted pitch black.");
                }
                vStream.WriteLine();
                vStream.WriteLine();
                vStream.WriteLine("AreaSourceBitmapFile         = " + vAreaFullFilePath + "      # Downloaded Texture");
                vStream.WriteLine("AreaSummerBitmapFile         = " + vAreaSummerFullFilePath + "      # General Summer Texture");
                vStream.WriteLine("AreaMaskBitmapFile           = " + vAreaMaskFullFilePath + "      # Water / Blend Mask");
                vStream.WriteLine("AreaNightBitmapFile          = " + vAreaNightFullFilePath + "      # Night  Texture");
                vStream.WriteLine("AreaSpringBitmapFile         = " + vAreaSpringFullFilePath + "      # Spring Texture");
                vStream.WriteLine("AreaAutumnBitmapFile         = " + vAreaAutumnFullFilePath + "      # Autumn Texture");
                vStream.WriteLine("AreaWinterBitmapFile         = " + vAreaWinterFullFilePath + "      # Winter Texture");
                vStream.WriteLine("AreaHardWinterBitmapFile     = " + vAreaHardWinterFullFilePath + "      # Hard Winter Texture");
                vStream.WriteLine();
                vStream.WriteLine("AreaKMLFile                  = " + vAreaKMLFullFilePath + "      # KML File Lines and Polygons marking Water, Land and Blend border");
                vStream.WriteLine("AreaVectorsFile              = " + vAreaVectorsFullFilePath + "      # SVG File Lines and Polygons marking Water, Land and Blend border");
                vStream.WriteLine();
                vStream.WriteLine();
                vStream.WriteLine("#--- The following information below is required in connection with a KML File only. If you don't work with a KML File you can leave this away.");
                vStream.WriteLine();
                vStream.WriteLine("AreaPixelCountInX = " + iBGLDatasStringPackage.mBGLPixelsInXString);
                vStream.WriteLine("AreaPixelCountInY = " + iBGLDatasStringPackage.mBGLPixelsInYString);
                vStream.WriteLine();
                vStream.WriteLine("AreaNWCornerLatitude      = " + iBGLDatasStringPackage.mBGLStartLatitudeString);
                vStream.WriteLine("AreaNWCornerLongitude     = " + iBGLDatasStringPackage.mBGLStartLongitudeString);
                vStream.WriteLine("AreaSECornerLatitude      = " + iBGLDatasStringPackage.mBGLStopLatitudeString);
                vStream.WriteLine("AreaSECornerLongitude     = " + iBGLDatasStringPackage.mBGLStopLongitudeString);
                vStream.Close();
            }
        }


    }
}
