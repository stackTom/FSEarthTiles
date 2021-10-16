using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.Globalization;
using System.IO;
using FSEarthMasksInternalDLL;

//----------------------------------------------------------------------------
//            FS Earth Masks (v1.0)
//      
//         written / programmed by HB-100
//
//
//        The program and the code may be used free. 
//              No limitations from my side.
//
//----------------------------------------------------------------------------


namespace FSEarthMasksDLL
{

    public enum tProcessingState
    {
        eProcInitial,
        eProcWater,
        eProcSpring,
        eProcSummer,
        eProcAutumn,
        eProcWinter,
        eProcHardWinter,
        eProcNight,
        eProcFinish
    }

    public partial class FSEarthMasksForm : Form, FSEarthMasksInternalInterface, FSEarthMasksInterface
    {
        static Boolean mDebugMode = false;   //If true then it will run in a single thread only, brings up the GUI at the end only and doesnt close the application
                                             //Important: Set this to false in release!

        private List<tXYCoord>  mRawCoordsLatLong; //[Grad] Longitude Latitude
        private List<tXYCoord>  mRawCoords;        //[Pixel]
        private List<tLine>     mRawLines;         //[Pixel]
        private List<tPoint>    mRawPoints;        //[Pixel]


        //Our Texture Container
        private MasksTexture    mMasksTexture;

        //And our Main Thread Timer
        System.Windows.Forms.Timer mMainThreadTimer;  //Main Thread Timer
        
        //The Work Thread
        Thread  mMainWorkThread;
        Boolean mWorkThreadStarted;
        Boolean mInitialisationFailExit;

        //Bitmaps
        Bitmap mWaterMotion1;
        Bitmap mWaterMotion2;
        Bitmap mWaterMotion3;
        Int32  mWaterMotionFrameCounter;
        Int32  mWaterAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mSnowMotion1;
        Bitmap mSnowMotion2;
        Bitmap mSnowMotion3;
        Int32 mSnowMotionFrameCounter;
        Int32 mSnowAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mAutumn;
        Int32  mAutumnAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mSpring;
        Int32 mSpringAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mSummer;
        Int32 mSummerAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mNight;
        Int32  mNightAndActionCounter;   //Counts the Timer Tick to slow the Motion

        Bitmap mInitial;
        Int32 mInitialAndActionCounter;   //Counts the Timer Tick to slow the Motion
        
        Bitmap mFinish;
        Int32 mFinishAndActionCounter;   //Counts the Timer Tick to slow the Motion

        //Mutex (I know I could use lock just make it identical to other application)
        Mutex mSetFriendThreadStatusMutex;  //for Status transfer from MainWorkProcessing thread

        String           mStatusFriendThread;
        tProcessingState mProcessingState; //For Displaying the correct Bitmaps

        public FSEarthMasksForm(String[] iApplicationStartArguments)
        {
            InitializeComponent();
            InitializeFSEarthMasks(iApplicationStartArguments);
        }

        ~FSEarthMasksForm()
        {
            AbortAllOpenThreads();
        }

        void InitializeFSEarthMasks(String[] iApplicationStartArguments)
        {
            MasksCommon.Initialize();
            MasksConfig.Initialize();

            mWaterMotionFrameCounter = 1;
            mWaterAndActionCounter   = 1000;

            mSnowMotionFrameCounter = 1;
            mSnowAndActionCounter = 1000;

            mAutumnAndActionCounter  = 1000;
            mSpringAndActionCounter  = 1000;
            mSummerAndActionCounter  = 1000;
            mInitialAndActionCounter = 1000;
            mFinishAndActionCounter  = 1000;
            mNightAndActionCounter   = 1000;

            mWorkThreadStarted      = false;
            mInitialisationFailExit = false;

            mProcessingState = tProcessingState.eProcInitial;
            mStatusFriendThread = "";

            mSetFriendThreadStatusMutex = new Mutex();

            ThreadStart vMainWorkDelegate = new ThreadStart(MainWorkProcessing);
            mMainWorkThread = new Thread(vMainWorkDelegate);

            mRawCoordsLatLong  = new List<tXYCoord>();
            mRawCoords         = new List<tXYCoord>();
            mRawLines          = new List<tLine>();
            mRawPoints         = new List<tPoint>();

            mMasksTexture  = new MasksTexture();

            //Initialize Timer for AutoScrolling on User drawing
            mMainThreadTimer = new System.Windows.Forms.Timer();
            mMainThreadTimer.Tick += new EventHandler(MainThreadTimerEventProcessor);
            mMainThreadTimer.Interval = 25;  // 25ms
            
            Boolean vContinue = true;
            Boolean vInitialisationOk = false;

            try
            {
                mWaterMotion1 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksWaterMotion1.jpg");
                mWaterMotion2 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksWaterMotion2.jpg");
                mWaterMotion3 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksWaterMotion3.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksWaterMotion1 (2 and 3)  Jpg's missing!");
                vContinue = false;
            }

            try
            {
                mSnowMotion1 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksSnowMotion1.jpg");
                mSnowMotion2 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksSnowMotion2.jpg");
                mSnowMotion3 = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksSnowMotion3.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksSnowMotion1 (2 and 3)  Jpg's missing!");
                vContinue = false;
            }
            
            try
            {
                mAutumn = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksAutumn.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed Autumn Jpg missing!");
                vContinue = false;
            }

            try
            {
                mSpring = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksSpring.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksSpring Jpg missing!");
                vContinue = false;
            }

            try
            {
                mSummer = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksSummer.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksSummer Jpg missing!");
                vContinue = false;
            }

            try
            {
                mNight = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksNight.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksNight Jpg missing!");
                vContinue = false;
            }

            try
            {
                mInitial = new Bitmap(MasksConfig.mStartExeFolder  + "\\" + "MasksInitial.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksInitial Jpg missing!");
                vContinue = false;
            }

            try
            {
                mFinish = new Bitmap(MasksConfig.mStartExeFolder + "\\" + "MasksFinish.jpg");
            }
            catch
            {
                SetStatus("Initialisation Failed MasksFinish Jpg missing!");
                vContinue = false;
            }

            if (vContinue)
            {
                if (MasksConfig.LoadConfigFile(this))
                {
                    if (MasksConfig.ParseCommandLineArguments(iApplicationStartArguments))
                    {
                        if (MasksConfig.LoadAreaEarthInfoFile(this))
                        {
                            BitmapBox.Image = (Bitmap)mInitial;
                            vInitialisationOk = true;
                        }
                        else
                        {
                            SetStatus("Initialisation Failed Load AreaEarthInfo File!");
                        }
                    }
                    else
                    {
                        SetStatus("Initialisation Failed ParseCommandLineArguments!");
                    }
                }
                else
                {
                    SetStatus("Initialisation Failed Load Config File!");
                }
            }

            if (vInitialisationOk)
            {
                //for ddeebbuugg
                if (mDebugMode)
                {
                    //No thread for ddeebbuugg makes it simple
                    MainWorkProcessing();
                }
                else
                {
                    mMainWorkThread.Start();
                }
                mWorkThreadStarted = true;

            }
            else
            {
                mInitialisationFailExit = true;
            }

            mMainThreadTimer.Start();

        }


        //All the Work is done here
        void MainWorkProcessing()
        {
            try
            {
                if (MasksConfig.mUseCSharpScripts)
                {
                    if (MasksConfig.mCreateSpringBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... Spring ");
                        MasksScriptsHandler.TryInstallSpringScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... Summer ");
                        MasksScriptsHandler.TryInstallSummerScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                    if (MasksConfig.mCreateAutumnBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... Autumn ");
                        MasksScriptsHandler.TryInstallAutumnScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                    if (MasksConfig.mCreateWinterBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... Winter ");
                        MasksScriptsHandler.TryInstallWinterScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                    if (MasksConfig.mCreateHardWinterBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... HardWinter");
                        MasksScriptsHandler.TryInstallHardWinterScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                    if (MasksConfig.mCreateNightBitmap)
                    {
                        SetStatusFromFriendThread(" Load and compile Script ... Night");
                        MasksScriptsHandler.TryInstallNightScript(MasksConfig.mStartExeFolder, MasksConfig.mStartExeFolder);
                    }
                }

                if (MasksConfig.mCreateTransitionPlotGraphicBitmap)
                {
                    SetStatusFromFriendThread(" Save TransitionPlotGraphic Bitmap ... ");
                    mMasksTexture.CreateAndSaveTransitionPlotGraphicBitmap();
                    MasksCommon.CollectGarbage();
                }

                SetStatusFromFriendThread(" Perpare Color Adjustment Tables ... ");
                mMasksTexture.PerpareColorAdjustmentTables();

                SetStatusFromFriendThread(" Load Source Texture Size Info ... ");
                mMasksTexture.LoadSourceBitmapInfo(this);

                SetStatusFromFriendThread(" Allocate Texture Memory  ... ");
                mMasksTexture.AllocateWorkTextureMemory();

                mMasksTexture.ClearCommandArray();

                Bitmap waterMaskBitmap = null;
                bool KMLFileLoaded = false;
                bool areaVectorsFileLoaded = false;
                bool useVectorsForWaterMasks = false;

                if (MasksConfig.mCreateWaterMaskBitmap)
                {
                    //Water
                    KMLFileLoaded = LoadKMLFile();

                    if (KMLFileLoaded && MasksConfig.mUseReversePoolPolygonOrderForKMLFile)
                    {
                        MasksCommon.ReverseOrderOfPoolPolygonesInList();
                    }

                    areaVectorsFileLoaded = LoadAreaVectorsFile();
                    useVectorsForWaterMasks = KMLFileLoaded || areaVectorsFileLoaded;

                    if (useVectorsForWaterMasks)
                    {
                        AddBlendBordersVectors();

                        SetStatusFromFriendThread(" Screening Vectors ... ");
                        mMasksTexture.ReduceLinesCount();

                        for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
                        {
                            if ((MasksCommon.mCoastLines[vTransitionType].Count > 0) ||
                                (MasksCommon.mDeepWaterLines[vTransitionType].Count > 0))  //else nothing to do and we can spare the work
                            {
                                mMasksTexture.ClearWorkMaskBitmap();
                                SetStatusFromFriendThread(" Drawing Vectors  ... ");
                                mMasksTexture.DrawVectorsForWaterRegions(vTransitionType);
                                mMasksTexture.FloodFillWaterLandTransition(vTransitionType, this);
                                SetStatusFromFriendThread(" Copy to Command Array  ... ");
                                mMasksTexture.CopyResultToWorkCommandArray(vTransitionType, this);
                            }
                            if (MasksCommon.mTransitionPolygons[vTransitionType].Count > 0)
                            {
                                mMasksTexture.ClearWorkMaskBitmap();
                                SetStatusFromFriendThread(" Drawing Transition Polyons  ... ");
                                mMasksTexture.DrawTransitionPolygons(vTransitionType);
                                SetStatusFromFriendThread(" Copy to Command Array  ... ");
                                mMasksTexture.CopyTransitionPolygonResultToWorkCommandArray(vTransitionType, this);
                            }
                        }

                        if (MasksCommon.mPoolPolygons.Count > 0)
                        {
                            mMasksTexture.ClearWorkMaskBitmap();
                            SetStatusFromFriendThread(" Drawing Pool Polyons  ... ");
                            mMasksTexture.DrawPoolPolygons();
                            SetStatusFromFriendThread(" Copy to Command Array  ... ");
                            mMasksTexture.CopyPoolPolygonsResultToWorkCommandArray(this);
                        }
                    }
                    else
                    {
                        waterMaskBitmap = mMasksTexture.CreateWaterMaskBitmap(this);
                        if (MasksConfig.skipAllWaterTiles)
                        {
                            // need to create the water mask bitmap whether fsx or fs2004 if we want to skip all black tiles
                            // why? Because the .net Bitmap class doesn't support saving alpha channel into bitmaps, even if
                            // use PixelFormat.Format32bppArgb... see here: https://docs.microsoft.com/en-us/dotnet/api/system.drawing.image.fromfile?redirectedfrom=MSDN&view=windowsdesktop-5.0#System_Drawing_Image_FromFile_System_String_
                            // It nevertheless IS saving the alpha byte into the image. but when either reading the image into memory
                            // or opening with a program like GIMP or IrfanView, there's no alpha value...
                            // trying to read it using lockbits just gives 255 for the alpha no matter what I try.
                            // creating a Tiff image first (which supposedly does have support for alpha channel in .net) just shows an all
                            // white alpha. thankfully resample for fs2004 does see the alpha byte
                            // So the easiest thing is just to make the water mask bitmap when we want to skip tiles
                            SetStatusFromFriendThread(" Save Water-Mask Bitmap  ... ");
                            mMasksTexture.SaveAreaMaskBitmap(waterMaskBitmap);
                        }
                    }
                }


                if (useVectorsForWaterMasks && MasksConfig.mCreateCommandGraphicBitmap)
                {
                    SetStatusFromFriendThread(" Save Command Graphic Bitmap ... ");
                    mMasksTexture.ClearWorkMaskBitmap();
                    mMasksTexture.CreateCommandGraphicBitmap();
                    mMasksTexture.SaveCommandGraphicBitmap();
                }

                //Summer... Summer has colour changes
                if (MasksConfig.mCreateSummerBitmap)
                {
                    //Summer
                    SetProcessingStateFromFriendThread(tProcessingState.eProcSummer);
                    SetStatusFromFriendThread(" Processing Summer...  Load Source");
                    mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    SetStatusFromFriendThread(" Process Colors  ... ");
                    mMasksTexture.GeneralColorAdjustment();
                    mMasksTexture.TransitionAndPoolColorAdjustment(this);
                    MasksScriptsHandler.MakeSummer(mMasksTexture);
                    if (MasksConfig.mCreateFS2004MasksInsteadFSXMasks && MasksConfig.mCreateWaterMaskBitmap)
                    {
                        SetProcessingStateFromFriendThread(tProcessingState.eProcWater);
                        SetStatusFromFriendThread(" Processing Water... ");
                        if (useVectorsForWaterMasks)
                        {
                            mMasksTexture.CreateFS2004WaterInWorkMaskBitmap(this);   //Now Paint Water
                        }
                        else
                        {
                            mMasksTexture.CreateFS2004WaterInAreaBitmap(waterMaskBitmap);
                        }
                    }
                    SetStatusFromFriendThread(" Save Summer Bitmap   ... ");
                    mMasksTexture.SaveAreaMaskSummerBitmap();
                }
                else
                {
                    if (MasksConfig.mCreateFS2004MasksInsteadFSXMasks && MasksConfig.mCreateWaterMaskBitmap)
                    {
                        SetProcessingStateFromFriendThread(tProcessingState.eProcWater);
                        SetStatusFromFriendThread(" Processing Water... Load Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                        if (useVectorsForWaterMasks)
                        {
                            mMasksTexture.CreateFS2004WaterInWorkMaskBitmap(this);   //Now Paint Water
                        }
                        else
                        {
                            mMasksTexture.CreateFS2004WaterInAreaBitmap(waterMaskBitmap);
                        }
                        SetStatusFromFriendThread(" Save Source Bitmap with Alpha Channel  ... ");
                        mMasksTexture.SaveOriginalBitmap(); //But with an Alpha mask
                    }
                }

                if ((MasksConfig.mCreateWaterMaskBitmap) && (!MasksConfig.mCreateFS2004MasksInsteadFSXMasks))
                {
                    SetProcessingStateFromFriendThread(tProcessingState.eProcWater);

                    if (useVectorsForWaterMasks)
                    {
                        if (!MasksConfig.mCreateSummerBitmap)
                        {
                            SetStatusFromFriendThread(" Processing Water-Mask...  Load Source");
                            mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                        } //else the Summer-Texture is already loaded!
                        else
                        {
                            //else the Summer-Texture is already loaded!
                            SetStatusFromFriendThread(" Process Water-Mask  ... ");
                        }
                        mMasksTexture.GreeningWorkBitmap();
                        mMasksTexture.CreateWaterInWorkMaskBitmap(this);   //Now Paint Water
                        SetStatusFromFriendThread(" Save Invert Water-Mask  ... ");
                        mMasksTexture.InverseWorkBitmap();
                        SetStatusFromFriendThread(" Save Water-Mask Bitmap  ... ");
                        mMasksTexture.SaveAreaMaskBitmap();
                    }
                    else if (!MasksConfig.skipAllWaterTiles) // water mask bitmap will have already been made if skipAllWaterTiles were true
                    {
                        SetStatusFromFriendThread(" Save Water-Mask Bitmap  ... ");
                        mMasksTexture.SaveAreaMaskBitmap(waterMaskBitmap);
                    }

                }



                //Seasons
                if (MasksConfig.mCreateSpringBitmap)
                {
                    //Spring
                    SetProcessingStateFromFriendThread(tProcessingState.eProcSpring);
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Processing Spring...  Reload Summer");
                        mMasksTexture.LoadSummerBitmapFileIntoWorkBitmap(this);
                    }
                    else
                    {
                        SetStatusFromFriendThread(" Processing Spring... Reload Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    }
                    MasksScriptsHandler.MakeSpring(mMasksTexture);
                    SetStatusFromFriendThread(" Save Spring Bitmap  ... ");
                    mMasksTexture.SaveAreaMaskSpringBitmap();
                }

                if (MasksConfig.mCreateAutumnBitmap)
                {
                    //Make Autumn
                    SetProcessingStateFromFriendThread(tProcessingState.eProcAutumn);
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Processing Autumn...  Reload Summer");
                        mMasksTexture.LoadSummerBitmapFileIntoWorkBitmap(this);
                    }
                    else
                    {
                        SetStatusFromFriendThread(" Processing Autumn... Reload Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    }
                    MasksScriptsHandler.MakeAutumn(mMasksTexture);
                    SetStatusFromFriendThread(" Save Autumn Bitmap  ... ");
                    mMasksTexture.SaveAreaMaskAutumnBitmap();
                }

                if (MasksConfig.mCreateWinterBitmap)
                {
                    //Make Winter
                    SetProcessingStateFromFriendThread(tProcessingState.eProcWinter);
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Processing Winter...  Reload Summer");
                        mMasksTexture.LoadSummerBitmapFileIntoWorkBitmap(this);
                    }
                    else
                    {
                        SetStatusFromFriendThread(" Processing Winter... Reload Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    }
                    MasksScriptsHandler.MakeWinter(mMasksTexture);
                    SetStatusFromFriendThread(" Save Winter Bitmap  ... ");
                    mMasksTexture.SaveAreaMaskWinterBitmap();
                }

                if (MasksConfig.mCreateHardWinterBitmap)
                {
                    //Make HardWinter
                    SetProcessingStateFromFriendThread(tProcessingState.eProcHardWinter);
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Processing HardWinter...  Reload Summer");
                        mMasksTexture.LoadSummerBitmapFileIntoWorkBitmap(this);
                    }
                    else
                    {
                        SetStatusFromFriendThread(" Processing HardWinter... Reload Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    }
                    MasksScriptsHandler.MakeHardWinter(mMasksTexture);
                    SetStatusFromFriendThread(" Save HardWinter Bitmap  ... ");
                    mMasksTexture.SaveAreaMaskHardWinterBitmap();
                }

                if (MasksConfig.mCreateNightBitmap)
                {
                    //Night
                    SetProcessingStateFromFriendThread(tProcessingState.eProcNight);
                    if (MasksConfig.mCreateSummerBitmap)
                    {
                        SetStatusFromFriendThread(" Processing Night...  Reload Summer");
                        mMasksTexture.LoadSummerBitmapFileIntoWorkBitmap(this);
                    }
                    else
                    {
                        SetStatusFromFriendThread(" Processing Night... Reload Source");
                        mMasksTexture.LoadSourceBitmapFileIntoWorkBitmap(this);
                    }
                    MasksScriptsHandler.MakeNight(mMasksTexture);
                    SetStatusFromFriendThread(" Save Night Bitmap  ... ");
                    mMasksTexture.SaveAreaMaskNightBitmap();
                }

                mMasksTexture.FreeWorkTextureMemory();

                SetProcessingStateFromFriendThread(tProcessingState.eProcFinish);
                SetStatusFromFriendThread(" Done. ");
                Thread.Sleep(1000);
            }
            catch (System.Exception e)
            {
                SetStatusFromFriendThread("Memory or Code conflict! - MainWorkProcessing() - " + e.ToString());
                Thread.Sleep(3000);
            }
        }
        


        void MainThreadTimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            if (mInitialisationFailExit)
            {
                Thread.Sleep(5000); //Give User time to read
                Application.Exit(); //And Exit
            }
            else
            {

                String vStatusFeedback = GetStatusFromFriendThread();
                
                if (vStatusFeedback.Length > 0)
                {
                    SetStatus(vStatusFeedback);
                }

                tProcessingState vProcessingState = GetProcessingStateFromFriendThread();

                if (vProcessingState == tProcessingState.eProcWater)
                {
                    mWaterAndActionCounter++;

                    if (mWaterAndActionCounter >= 5)
                    {
                        mWaterAndActionCounter = 0;

                        switch (mWaterMotionFrameCounter)
                        {
                            case 1:
                                {
                                    BitmapBox.Image =  (Bitmap)mWaterMotion1;
                                    break;
                                }
                            case 2:
                                {
                                    BitmapBox.Image = (Bitmap)mWaterMotion2;
                                    break;
                                }
                            case 3:
                                {
                                    BitmapBox.Image = (Bitmap)mWaterMotion3;
                                    break;
                                }
                            case 4:
                                {
                                    BitmapBox.Image = (Bitmap)mWaterMotion2;
                                    break;
                                }
                            default:
                                {
                                    BitmapBox.Image = (Bitmap)mWaterMotion1;
                                    break;
                                }

                        }
                        mWaterMotionFrameCounter++;
                        if (mWaterMotionFrameCounter > 4)
                        {
                            mWaterMotionFrameCounter = 1;
                        }
                    }
                }

                if ((vProcessingState == tProcessingState.eProcWinter) || (vProcessingState == tProcessingState.eProcHardWinter))
                {
                    mSnowAndActionCounter++;

                    if (mSnowAndActionCounter >= 5)
                    {
                        mSnowAndActionCounter = 0;

                        switch (mSnowMotionFrameCounter)
                        {
                            case 1:
                                {
                                    BitmapBox.Image = (Bitmap)mSnowMotion1;
                                    break;
                                }
                            case 2:
                                {
                                    BitmapBox.Image = (Bitmap)mSnowMotion2;
                                    break;
                                }
                            case 3:
                                {
                                    BitmapBox.Image = (Bitmap)mSnowMotion3;
                                    break;
                                }
                            default:
                                {
                                    BitmapBox.Image = (Bitmap)mSnowMotion1;
                                    break;
                                }

                        }
                        mSnowMotionFrameCounter++;
                        if (mSnowMotionFrameCounter > 3)
                        {
                            mSnowMotionFrameCounter = 1;
                        }
                    }
                }


                if (vProcessingState == tProcessingState.eProcAutumn)
                {
                    mAutumnAndActionCounter++;

                    if (mAutumnAndActionCounter >= 100)
                    {
                        mAutumnAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mAutumn;
                    }
                }

                if (vProcessingState == tProcessingState.eProcSpring)
                {
                    mSpringAndActionCounter++;

                    if (mSpringAndActionCounter >= 100)
                    {
                        mSpringAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mSpring;
                    }
                }

                if (vProcessingState == tProcessingState.eProcSummer)
                {
                    mSummerAndActionCounter++;

                    if (mSummerAndActionCounter >= 100)
                    {
                        mSummerAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mSummer;
                    }
                }

                if (vProcessingState == tProcessingState.eProcNight)
                {
                    mNightAndActionCounter++;

                    if (mNightAndActionCounter >= 100)
                    {
                        mNightAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mNight;
                    }
                }

                if (vProcessingState == tProcessingState.eProcInitial)
                {
                    mInitialAndActionCounter++;

                    if (mInitialAndActionCounter >= 100)
                    {
                        mInitialAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mInitial;
                    }
                }

                if (vProcessingState == tProcessingState.eProcFinish)
                {
                    mFinishAndActionCounter++;

                    if (mFinishAndActionCounter >= 100)
                    {
                        mFinishAndActionCounter = 0;
                        BitmapBox.Image = (Bitmap)mFinish;
                    }
                }

                if (mMainWorkThread.ThreadState == ThreadState.Stopped)
                {
                    //Thread exited create new one to reset status to unstarted
                    ThreadStart vMainWorkDelegate = new ThreadStart(MainWorkProcessing);
                    mMainWorkThread = new Thread(vMainWorkDelegate);

                    mWorkThreadStarted = false;

                    Application.Exit(); //Exit Application

                }
            }
        }

        public void SetStatus(String iStatus)
        {
            StatusBox.Invalidate();
            StatusBox.Text = iStatus;
            StatusBox.Refresh();
        }

        public void SetStatusFromFriendThread(String iStatus)
        {
            mSetFriendThreadStatusMutex.WaitOne();
            mStatusFriendThread = iStatus;
            mSetFriendThreadStatusMutex.ReleaseMutex();
        }

        public String GetStatusFromFriendThread()
        {
            String vStatusFriendThread;

            mSetFriendThreadStatusMutex.WaitOne();
            vStatusFriendThread = mStatusFriendThread;
            mStatusFriendThread = "";
            mSetFriendThreadStatusMutex.ReleaseMutex();

            return vStatusFriendThread;
        }

        public void SetProcessingStateFromFriendThread(tProcessingState iProcessingState)
        {
            mSetFriendThreadStatusMutex.WaitOne();
            mProcessingState = iProcessingState;
            mSetFriendThreadStatusMutex.ReleaseMutex();
        }

        public tProcessingState GetProcessingStateFromFriendThread()
        {
            tProcessingState vProcessingState;

            mSetFriendThreadStatusMutex.WaitOne();
            vProcessingState = mProcessingState;
            mSetFriendThreadStatusMutex.ReleaseMutex();

            return vProcessingState;
        }


        private void GetLineAndTransitionType(ref tLineType oLineType, ref MasksConfig.tTransitionType oTransitionType, String iString)
        {
            //Keep the order of the Keywords as follow:
            
            //CoastTwo
            //Coast
            //DeepWaterTwo
            //DeepWater
            //WaterTwo
            //WaterPool
            //Water
            //LandPool
            //BlendMax
            //BlendOn
            //BlendPool
            //Blend


            if (MasksCommon.StringContains(iString, "CoastTwo"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = MasksConfig.tTransitionType.eWater2Transition;
            }
            else if (MasksCommon.StringContains(iString, "Coast"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = MasksConfig.tTransitionType.eWaterTransition;
            }
            else if (MasksCommon.StringContains(iString, "DeepWaterTwo"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = MasksConfig.tTransitionType.eWater2Transition;
            }
            else if (MasksCommon.StringContains(iString, "DeepWater"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = MasksConfig.tTransitionType.eWaterTransition;
            }
            else if (MasksCommon.StringContains(iString, "WaterTwo"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = MasksConfig.tTransitionType.eWater2Transition;
            }
            else if (MasksCommon.StringContains(iString, "WaterPool"))
            {
                oLineType = tLineType.eWaterPoolPolygon;
            }
            else if (MasksCommon.StringContains(iString, "Water"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = MasksConfig.tTransitionType.eWaterTransition;
            }
            else if (MasksCommon.StringContains(iString, "LandPool"))
            {
                oLineType = tLineType.eLandPoolPolygon;
            }
            else if (MasksCommon.StringContains(iString, "BlendMax"))
            {
                oLineType = tLineType.eDeepWater;
                oTransitionType = MasksConfig.tTransitionType.eBlendTransition;
            }
            else if (MasksCommon.StringContains(iString, "BlendOn"))
            {
                oLineType = tLineType.eCoast;
                oTransitionType = MasksConfig.tTransitionType.eBlendTransition;
            }
            else if (MasksCommon.StringContains(iString, "BlendPool"))
            {
                oLineType = tLineType.eBlendPoolPolygon;
            }
            else if (MasksCommon.StringContains(iString, "Blend"))
            {
                oLineType = tLineType.eFullWaterPolygon;
                oTransitionType = MasksConfig.tTransitionType.eBlendTransition;
            }
            else
            {
                oLineType = tLineType.eNone;
            }
        }


        private bool LoadKMLFile()
        {
            if (MasksConfig.mUseAreaKMLFile)
            {
                if (MasksConfig.mAreaKMLFile != "")
                {
                    if (File.Exists(MasksConfig.mAreaKMLFile))
                    {
                        if (MasksConfig.AreLatLongParametersInConfigOK())
                        {
                            try
                            {
                                SetStatusFromFriendThread(" parsing AreaKML file.. ");

                                tLineType vLineType = tLineType.eNone;
                                MasksConfig.tTransitionType vTransitionType = MasksConfig.tTransitionType.eWaterTransition;

                                XmlDocument vXMLDoc = new XmlDocument();
                                vXMLDoc.Load(MasksConfig.mAreaKMLFile);
                                XmlNodeList vXMLNodeCoast = vXMLDoc.GetElementsByTagName("Placemark");

                                foreach (XmlNode vMyParentNode in vXMLNodeCoast)
                                {
                                    vLineType = tLineType.eNone;
                                    mRawCoordsLatLong = new List<tXYCoord>();
                                    mRawCoords = new List<tXYCoord>();
                                    mRawLines = new List<tLine>();
                                    mRawPoints = new List<tPoint>();

                                    foreach (XmlNode vMyChildNode in vMyParentNode.ChildNodes)
                                    {
                                        if (MasksCommon.StringCompare(vMyChildNode.Name, "Name"))
                                        {
                                            GetLineAndTransitionType(ref vLineType, ref vTransitionType, vMyChildNode.InnerText);
                                        }
                                        if (MasksCommon.StringCompare(vMyChildNode.Name, "LineString"))
                                        {
                                            foreach (XmlNode vMyChildChildNode in vMyChildNode.ChildNodes)
                                            {
                                                if (MasksCommon.StringCompare(vMyChildChildNode.Name, "Coordinates"))
                                                {
                                                    AddLatLongVector((Int32)(vTransitionType), vMyChildChildNode.InnerText, vLineType);
                                                }
                                            }
                                        }
                                        if (MasksCommon.StringCompare(vMyChildNode.Name, "Polygon"))
                                        {
                                            foreach (XmlNode vMyChildChildNode in vMyChildNode.ChildNodes)
                                            {
                                                if (MasksCommon.StringCompare(vMyChildChildNode.Name, "outerBoundaryIs"))
                                                {
                                                    foreach (XmlNode vMyChildChildChildNode in vMyChildChildNode.ChildNodes)
                                                    {
                                                        if (MasksCommon.StringCompare(vMyChildChildChildNode.Name, "LinearRing"))
                                                        {
                                                            foreach (XmlNode vMyChildChildChildChildNode in vMyChildChildChildNode.ChildNodes)
                                                            {
                                                                if (MasksCommon.StringCompare(vMyChildChildChildChildNode.Name, "coordinates"))
                                                                {
                                                                    AddLatLongVector((Int32)(vTransitionType), vMyChildChildChildChildNode.InnerText, vLineType);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                }

                                SetStatusFromFriendThread(" Area KML File loaded. ");
                                Thread.Sleep(2000);

                                return true;

                            }
                            catch
                            {
                                //ops
                                SetStatusFromFriendThread(" Area KML File load failed. ");
                                Thread.Sleep(2000);

                                return false;
                            }
                        }
                        else
                        {
                            //ops
                            SetStatusFromFriendThread(" Area KML File can not be processed without valid Area LatitudeLongitude Coords and PixelCount. ");
                            Thread.Sleep(3000);

                            return false;
                        }
                    }
                    else
                    {
                        SetStatusFromFriendThread("Be aware: Area KML File does not exist.");
                        Thread.Sleep(2000);

                        return false;
                    }
                }
                else
                {
                    SetStatusFromFriendThread("Can not load Area KML File: File path missing.");
                    Thread.Sleep(1500);

                    return false;
                }
            }

            return false;
        }


        private bool LoadAreaVectorsFile()
        {
            if (MasksConfig.mUseAreaVectorsFile)
            {
                if (MasksConfig.mAreaVectorsFile != "")
                {
                    if (File.Exists(MasksConfig.mAreaVectorsFile))
                    {
                        try
                        {
                            SetStatusFromFriendThread(" parsing Area SVG file.. ");

                            tLineType vLineType = tLineType.eNone;
                            MasksConfig.tTransitionType vTransitionType = MasksConfig.tTransitionType.eWaterTransition;

                            XmlDocument vXMLDoc = new XmlDocument();
                            vXMLDoc.Load(MasksConfig.mAreaVectorsFile);
                            XmlNodeList vXMLNodeCoast = vXMLDoc.GetElementsByTagName("path");

                            foreach (XmlNode vMyParentNode in vXMLNodeCoast)
                            {
                                vLineType = tLineType.eNone;
                                mRawCoordsLatLong = new List<tXYCoord>();
                                mRawCoords = new List<tXYCoord>();
                                mRawLines = new List<tLine>();
                                mRawPoints = new List<tPoint>();

                                String vPathInfo = vMyParentNode.OuterXml;

                                GetLineAndTransitionType(ref vLineType, ref vTransitionType, vPathInfo);

                                String vTemp = "";
                                String vPathOnly = "";

                                Int32 vIndex = vPathInfo.IndexOf("d=\"", StringComparison.CurrentCultureIgnoreCase);
                                if (vIndex >= 0)
                                {
                                    vTemp = vPathInfo.Substring(vIndex + 3);
                                    vIndex = vTemp.IndexOf("\"", StringComparison.CurrentCultureIgnoreCase);
                                    if (vIndex >= 0)
                                    {
                                        vPathOnly = vTemp.Remove(vIndex);
                                    }
                                }

                                AddSVGVector((Int32)(vTransitionType), vPathOnly, vLineType);

                            }

                            SetStatusFromFriendThread(" Area SVG File loaded. ");
                            Thread.Sleep(2000);

                            return true;

                        }
                        catch
                        {
                            //ops
                            SetStatusFromFriendThread(" Area SVG File load failed. ");
                            Thread.Sleep(2000);

                            return false;
                        }
                    }
                    else
                    {
                        SetStatusFromFriendThread("Be aware: Area SVG File does not exist.");
                        Thread.Sleep(2000);

                        return false;
                    }
                }
                else
                {
                    SetStatusFromFriendThread("Can not load Area KML File: File path missing.");
                    Thread.Sleep(1500);

                    return false;
                }
            }

            return false;
        }


        //private void AddLatLongVector(Int32 iTransitionType, String iParseMe, tLineType iLineType)
        //{
        //    if (iLineType != tLineType.eNone)
        //    {
        //        try
        //        {
        //            Exception vMyException = new Exception("an exception");

        //            MasksCommon.ResetStringParserPointer();

        //            while (!MasksCommon.IsStringParserEnd(ref iParseMe))
        //            {
        //                Double vValue1 = 0.0;
        //                Double vValue2 = 0.0;
        //                Double vValue3 = 0.0;
        //                tXYCoord vXYCoord;

        //                Boolean vValid = false;

        //                vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue1);

        //                if (vValid)
        //                {
        //                    vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue2);
        //                }
        //                else
        //                {
        //                    if (!MasksCommon.IsStringParserEnd(ref iParseMe))
        //                    {
        //                        throw vMyException;
        //                    }
        //                }
        //                if (vValid)
        //                {
        //                    vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ' ', ref vValue3);
        //                }
        //                else
        //                {
        //                    throw vMyException;
        //                }
        //                if (vValid)
        //                {
        //                    vXYCoord.mX = vValue1;
        //                    vXYCoord.mY = vValue2;
        //                    mRawCoordsLatLong.Add(vXYCoord);
        //                }
        //                else
        //                {
        //                    throw vMyException;
        //                }

        //            }

        //            //Now Add the Vectors to the correct Lists
        //            CreatePixelRawCoords();
        //            if ((iLineType == tLineType.eCoast) || (iLineType == tLineType.eDeepWater))
        //            {
        //                //Transition
        //                CreateLines();
        //                AddPointsToLists(iTransitionType, iLineType);
        //                AddLinesToLists(iTransitionType, iLineType);
        //            }
        //            else
        //            {
        //                //Fix Polygon
        //                AddFixPolygon(iTransitionType, iLineType);
        //            }

        //        }
        //        catch
        //        {
        //            SetStatusFromFriendThread(" Error Catched in XML Coordinates parsing. ");
        //            Thread.Sleep(3000);
        //        }
        //    }
        //}


        private void AddLatLongVector(Int32 iTransitionType, String iParseMe, tLineType iLineType)
        {
            if (iLineType != tLineType.eNone)
            {
                try
                {
                    Exception vMyException = new Exception("an exception");

                    MasksCommon.ResetStringParserPointer();

                    // The code to get longitude and latitude from iParseMe
                    iParseMe = iParseMe.Trim();
                    iParseMe += " ";
                    iParseMe = iParseMe.Remove(iParseMe.Length - 4);
                    iParseMe = iParseMe.Replace("0 ", "");

                    string[] t = iParseMe.Split(',');

                    for (int c = 0; c < t.Length; c++)
                    {
                        try
                        {
                            tXYCoord vXYCoord;
                            string u1 = t[c];
                            string u2 = t[c + 1];
                            Double vValue1 = Convert.ToDouble(u1, NumberFormatInfo.InvariantInfo);
                            Double vValue2 = Convert.ToDouble(u2, NumberFormatInfo.InvariantInfo);

                            vXYCoord.mX = vValue1;
                            vXYCoord.mY = vValue2;
                            mRawCoordsLatLong.Add(vXYCoord);
                            c++;
                        }
                        catch
                        {
                            SetStatusFromFriendThread(" Error with conversion in XML Coordinates parsing. ");
                            Thread.Sleep(3000);
                        }
                    }
                    // End of update
                    // Davy DUCLY

                    /* while (!MasksCommon.IsStringParserEnd(ref iParseMe))
                    {
                    Double vValue1 = 0.0;
                    Double vValue2 = 0.0;
                    Double vValue3 = 0.0;
                    tXYCoord vXYCoord;

                    Boolean vValid = false;

                    vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue1);

                    if (vValid)
                    {
                    vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ',', ref vValue2);
                    }
                    else
                    {
                    if (!MasksCommon.IsStringParserEnd(ref iParseMe))
                    {
                    throw vMyException;
                    }
                    }
                    if (vValid)
                    {
                    vValid = MasksCommon.GetNextDoubleFromStringParser(ref iParseMe, ' ', ref vValue3);
                    }
                    else
                    {
                    throw vMyException;
                    }
                    if (vValid)
                    {
                    vXYCoord.mX = vValue1;
                    vXYCoord.mY = vValue2;
                    mRawCoordsLatLong.Add(vXYCoord);
                    }
                    else
                    {
                    throw vMyException;
                    }

                    }
                    */
                    //Now Add the Vectors to the correct Lists
                    CreatePixelRawCoords();
                    if ((iLineType == tLineType.eCoast) || (iLineType == tLineType.eDeepWater))
                    {
                        //Transition
                        CreateLines();
                        AddPointsToLists(iTransitionType, iLineType);
                        AddLinesToLists(iTransitionType, iLineType);
                    }
                    else
                    {
                        //Fix Polygon
                        AddFixPolygon(iTransitionType, iLineType);
                    }

                }
                catch
                {
                    SetStatusFromFriendThread(" Error Catched in XML Coordinates parsing. ");
                    Thread.Sleep(3000);
                }
            }
        }


        private void AddSVGVector(Int32 iTransitionType, String iParseMe, tLineType iLineType)
        {
            if (iLineType != tLineType.eNone)
            {
                try
                {
                    Exception vMyException = new Exception("an exception");

                    iParseMe = iParseMe.Trim();

                    while (iParseMe.Length > 0)
                    {
                        Double vValue1 = 0.0;
                        Double vValue2 = 0.0;

                        Int32 vIndex;
                        tXYCoord vXYCoord;
                        String vLetter;

                        if (iParseMe.Length > 1)
                        {
                            vLetter = iParseMe.Remove(1);
                            iParseMe = iParseMe.Substring(1);//RemoveLetter;
                            iParseMe = iParseMe.Trim();
                        }
                        else
                        {
                            vLetter = iParseMe;
                            iParseMe = "";
                        }

                        Int32 vTargetCoordPosition = 1;

                        if ((vLetter == "Z") || (vLetter == "z")) //Polygon complete
                        {
                            vTargetCoordPosition = 0;
                        }

                        if ((vLetter == "Q") || (vLetter == "q"))
                        {
                            vTargetCoordPosition = 2;
                        }

                        if ((vLetter == "C") || (vLetter == "c"))
                        {
                            vTargetCoordPosition = 3;
                        }

                        for (Int32 vCoordPos = 1; vCoordPos <= vTargetCoordPosition; vCoordPos++)
                        {
                            vIndex = iParseMe.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                            if (vIndex >= 0)
                            {
                                String vSubString = iParseMe.Remove(vIndex);
                                vSubString = vSubString.Trim();
                                vValue1 = Convert.ToDouble(vSubString, NumberFormatInfo.InvariantInfo);
                                iParseMe = iParseMe.Substring(vIndex + 1);
                            }
                            else
                            {
                                throw vMyException;
                            }

                            vIndex = iParseMe.IndexOf(" ", StringComparison.CurrentCultureIgnoreCase);
                            if (vIndex >= 0)
                            {
                                String vSubString = iParseMe.Remove(vIndex);
                                vSubString = vSubString.Trim();
                                vValue2 = Convert.ToDouble(vSubString, NumberFormatInfo.InvariantInfo);
                                iParseMe = iParseMe.Substring(vIndex + 1);
                            }
                            else
                            {
                                String vSubString = iParseMe;
                                vSubString = vSubString.Trim();
                                vValue2 = Convert.ToDouble(vSubString, NumberFormatInfo.InvariantInfo);
                                iParseMe = "";
                            }
                        }
                        iParseMe = iParseMe.Trim();

                        if ((vLetter != "Z") && (vLetter != "z"))
                        {
                            vXYCoord.mX = vValue1;
                            vXYCoord.mY = vValue2;
                            vXYCoord.mX -= 0.5f;   //Coordinate adaption to Bitmap coord: (0.0f,0.0f) is center/middle of Pixel (0,0)
                            vXYCoord.mY -= 0.5f;   //whereas we due KML and SVG vectors have 0.0,0.0 on Top Left corner of Pixel (0,0)
                            mRawCoords.Add(vXYCoord);
                        }
                    }

                    //Now Add the Vectors to the correct Lists
                    if ((iLineType == tLineType.eCoast) || (iLineType == tLineType.eDeepWater))
                    {
                        //Transition
                        CreateLines();
                        AddPointsToLists(iTransitionType, iLineType);
                        AddLinesToLists(iTransitionType, iLineType);
                    }
                    else
                    {
                        //Fix Polygon
                        AddFixPolygon(iTransitionType, iLineType);
                    }
                }
                catch
                {
                    SetStatusFromFriendThread(" Error Catched in XML SVG vector datas parsing. ");
                    Thread.Sleep(3000);
                }
            }
        }

        private void CreatePixelRawCoords()
        {
            foreach (tXYCoord vXYCoord in mRawCoordsLatLong)
            {
                tXYCoord vXYCoordPixel = ConvertXYLatLongToPixel(vXYCoord);
                vXYCoordPixel.mX -= 0.5f;   //Coordinate adaption to Bitmap coord: (0.0f,0.0f) is center/middle of Pixel (0,0)
                vXYCoordPixel.mY -= 0.5f;   //whereas we due KML and SVG vectors have 0.0,0.0 on Top Left corner of Pixel (0,0)
                mRawCoords.Add(vXYCoordPixel);
            }
        }


        private void CreateLines()
        {
            try
            {
                tXYCoord vLastCoord;
                vLastCoord.mX = 0.0;
                vLastCoord.mY = 0.0;

                Boolean vFirst = true;

                foreach (tXYCoord vXYCoord in mRawCoords)
                {
                    tPoint vPoint;
                    vPoint.mX = Convert.ToSingle(vXYCoord.mX);
                    vPoint.mY = Convert.ToSingle(vXYCoord.mY);
                    mRawPoints.Add(vPoint);

                    if (!vFirst)
                    {
                        //To Do ..only use Lines within Area or passing through Area ..cut them at border)

                        //Calculate (fast) point Distance check Line Parameters (vector geometrie)
                        //                       _
                        //                     o P
                        //                     ^
                        //                     | D
                        //           _         v        _
                        //  Line     A o--------------o B
                        //             |<- U ->|
                        //
                        // Formula 
                        //               _       ( Bx - Ax )  !  _       ( Ay - By  )
                        //  Equations I: A + U * (         )  =  P + L * (          )
                        //                       ( By-Ay   )             ( Bx  - Ax )
                        //
                        //       and II: D = L * (Sqrt((Ay-By)^2 + (Bx-Ax)^2))
                        //
                        //
                        //  Solution:
                        //               (Ax-Bx) * (Ax-Px) + (Ay-By)*(Ay-Py)
                        // Factor U =  --------------------------------------
                        //                      (Ax-Bx)^2 + (Ay-By)^2
                        //
                        //              (Ax-Bx)*Py -(Ay-Py)*Px - Ax*By + Ay*Bx
                        // Factor D =  -----------------------------------------
                        //                   Sqrt((Ax-Bx)^2 + (Ay-By)^2)
                        //
                        //   can be brought to a simpler Formula with some constants U that we can calculate in ahead:
                        //  ->  U = Uo + Ux*Px + Uy*Py
                        //  ->  D = Do + Dx*Px + Dy*Py
                        // 
                        // you see some math class is a usefull thing to improve the hobby ;)
                        //
                        Double vAx = vLastCoord.mX;
                        Double vAy = vLastCoord.mY;
                        Double vBx = vXYCoord.mX;
                        Double vBy = vXYCoord.mY;

                        //ToDo Check for div through 0
                        Double vUNenner = (vAx - vBx) * (vAx - vBx) + (vAy - vBy) * (vAy - vBy);
                        Double vUInvNenner = 1.0 / vUNenner;
                        Double vDInvNenner = 1.0 / Math.Sqrt(vUNenner);
                        Double vUo = vUInvNenner * (vAx * (vAx - vBx) + vAy * (vAy - vBy));
                        Double vUx = vUInvNenner * (-(vAx - vBx));
                        Double vUy = vUInvNenner * (-(vAy - vBy));
                        Double vDo = vDInvNenner * (-vAx * vBy + vAy * vBx);
                        Double vDx = vDInvNenner * (-(vAy - vBy));
                        Double vDy = vDInvNenner * (+(vAx - vBx));

                        tLine vLine;

                        vLine.mX1 = Convert.ToSingle(vAx);
                        vLine.mY1 = Convert.ToSingle(vAy);
                        vLine.mX2 = Convert.ToSingle(vBx);
                        vLine.mY2 = Convert.ToSingle(vBy);
                        vLine.mUo = Convert.ToSingle(vUo);
                        vLine.mUx = Convert.ToSingle(vUx);
                        vLine.mUy = Convert.ToSingle(vUy);
                        vLine.mDo = Convert.ToSingle(vDo);
                        vLine.mDx = Convert.ToSingle(vDx);
                        vLine.mDy = Convert.ToSingle(vDy);

                        mRawLines.Add(vLine);

                    }

                    vLastCoord.mX = vXYCoord.mX;
                    vLastCoord.mY = vXYCoord.mY;
                    vFirst = false;
                }
            }
            catch
            {
                //och
                SetStatusFromFriendThread("Error Probabily Vector of 0 Length (division through 0)");
                Thread.Sleep(3000);
            }

        }

        private void AddLinesToLists(Int32 iTransitionType, tLineType iLineType)
        {

            if (iLineType == tLineType.eCoast)
            {
                foreach (tLine vLine in mRawLines)
                {
                   MasksCommon.mOriginalCoastLines[iTransitionType].Add(vLine);
                   MasksCommon.mCoastLines[iTransitionType].Add(vLine);
                   MasksCommon.mSliceCoastLines[iTransitionType].Add(vLine);
                   MasksCommon.mForCutCheckCoastLines[iTransitionType].Add(vLine);
                }
            }
            if (iLineType == tLineType.eDeepWater)
            {
                foreach (tLine vLine in mRawLines)
                {
                    MasksCommon.mOriginalDeepWaterLines[iTransitionType].Add(vLine);
                    MasksCommon.mDeepWaterLines[iTransitionType].Add(vLine);
                    MasksCommon.mSliceDeepWaterLines[iTransitionType].Add(vLine);
                    MasksCommon.mForCutCheckDeepWaterLines[iTransitionType].Add(vLine);
                }
            }
        }

        private void AddPointsToLists(Int32 iTransitionType, tLineType iLineType)
        {

            if (iLineType == tLineType.eCoast)
            {
                foreach (tPoint vPoint in mRawPoints)
                {
                    MasksCommon.mCoastPoints[iTransitionType].Add(vPoint);
                    MasksCommon.mSliceCoastPoints[iTransitionType].Add(vPoint);
                }
            }
            if (iLineType == tLineType.eDeepWater)
            {
                foreach (tPoint vPoint in mRawPoints)
                {
                    MasksCommon.mDeepWaterPoints[iTransitionType].Add(vPoint);
                    MasksCommon.mSliceDeepWaterPoints[iTransitionType].Add(vPoint);
                }
            }
        }


        private void AddFixPolygon(Int32 iTransitionType, tLineType iLineType)
        {
            if (mRawCoords.Count >= 3) //Polygon with Less than 3 points are nonsense
            {
                PointF[] vPointFArray = new PointF[mRawCoords.Count];

                Int32 vIndex = 0;
                foreach (tXYCoord vPoint in mRawCoords)
                {
                    PointF vPointF = new PointF((Single)(vPoint.mX),(Single)(vPoint.mY));
                    vPointFArray[vIndex] = vPointF;
                    vIndex++;
                }

                if (iLineType == tLineType.eFullWaterPolygon)
                {
                    MasksCommon.mTransitionPolygons[iTransitionType].Add(vPointFArray);
                }
                else if (iLineType == tLineType.eWaterPoolPolygon)
                {
                   tPoolPolygon vPoolPolygon;
                   vPoolPolygon.mPoolType = MasksConfig.tPoolType.eWaterPool;
                   vPoolPolygon.mPolygon = vPointFArray;
                   MasksCommon.mPoolPolygons.Add(vPoolPolygon);
                }
                else if (iLineType == tLineType.eLandPoolPolygon)
                {
                    tPoolPolygon vPoolPolygon;
                    vPoolPolygon.mPoolType = MasksConfig.tPoolType.eLandPool;
                    vPoolPolygon.mPolygon = vPointFArray;
                    MasksCommon.mPoolPolygons.Add(vPoolPolygon);
                }
                else if (iLineType == tLineType.eBlendPoolPolygon)
                {
                    tPoolPolygon vPoolPolygon;
                    vPoolPolygon.mPoolType = MasksConfig.tPoolType.eBlendPool;
                    vPoolPolygon.mPolygon = vPointFArray;
                    MasksCommon.mPoolPolygons.Add(vPoolPolygon);
                }
                else
                {
                    //Do nothing
                }
            }
        }

        private tXYCoord ConvertXYLatLongToPixel(tXYCoord iXYCoord)
        {
            tXYCoord vPixelXYCoord;

            Double vPixelPerLongitude = Convert.ToDouble(MasksConfig.mAreaPixelCountInX) / (MasksConfig.mAreaSECornerLongitude - MasksConfig.mAreaNWCornerLongitude);
            Double vPixelPerLatitude = Convert.ToDouble(MasksConfig.mAreaPixelCountInY) / (MasksConfig.mAreaNWCornerLatitude - MasksConfig.mAreaSECornerLatitude);

            vPixelXYCoord.mX = vPixelPerLongitude * (iXYCoord.mX - MasksConfig.mAreaNWCornerLongitude);
            vPixelXYCoord.mY = vPixelPerLatitude * (MasksConfig.mAreaNWCornerLatitude - iXYCoord.mY);

            return vPixelXYCoord;
        }


        private void AddBlendBordersVectors()
        {
            if (mMasksTexture.IsPixelInfoInited())
            {
                Int32 vPixelCountInX = mMasksTexture.GetPixelCountInX();
                Int32 vPixelCountInY = mMasksTexture.GetPixelCountInY();

                if ((MasksConfig.mBlendBorderDistance < (Single)(2 * vPixelCountInX + 2)) &&
                    (MasksConfig.mBlendBorderDistance < (Single)(2 * vPixelCountInY + 2)))
                {

                    //Do only if enough Pixel space
                    
          
                    mRawCoordsLatLong = new List<tXYCoord>();
                    mRawCoords = new List<tXYCoord>();
                    mRawLines = new List<tLine>();
                    mRawPoints = new List<tPoint>();

                    tXYCoord vNWCoord;
                    tXYCoord vNECoord;
                    tXYCoord vSECoord;
                    tXYCoord vSWCoord;

                    vNWCoord.mX = -1.0f;
                    vNWCoord.mY = -1.0f;
                    vNECoord.mX = (Single)(vPixelCountInX);
                    vNECoord.mY = -1.0f;
                    vSECoord.mX = (Single)(vPixelCountInX);
                    vSECoord.mY = (Single)(vPixelCountInY);
                    vSWCoord.mX = -1.0f;
                    vSWCoord.mY = (Single)(vPixelCountInY);

                    if (MasksConfig.mBlendNorthBorder)
                    {
                        mRawCoords.Add(vNWCoord);
                        mRawCoords.Add(vNECoord);
                        BorderAddLineAndClearRawLists(tLineType.eDeepWater);
                    }
                    if (MasksConfig.mBlendEastBorder)
                    {
                        mRawCoords.Add(vNECoord);
                        mRawCoords.Add(vSECoord);
                        BorderAddLineAndClearRawLists(tLineType.eDeepWater);
                    }
                    if (MasksConfig.mBlendSouthBorder)
                    {
                        mRawCoords.Add(vSECoord);
                        mRawCoords.Add(vSWCoord);
                        BorderAddLineAndClearRawLists(tLineType.eDeepWater);
                    }
                    if (MasksConfig.mBlendWestBorder)
                    {
                        mRawCoords.Add(vSWCoord);
                        mRawCoords.Add(vNWCoord);
                        BorderAddLineAndClearRawLists(tLineType.eDeepWater);
                    }


                    mRawCoordsLatLong = new List<tXYCoord>();
                    mRawCoords = new List<tXYCoord>();
                    mRawLines = new List<tLine>();
                    mRawPoints = new List<tPoint>();


                    if (MasksConfig.mBlendNorthBorder)
                    {
                        vNWCoord.mY += MasksConfig.mBlendBorderDistance;
                        vNECoord.mY += MasksConfig.mBlendBorderDistance;
                    }
                    if (MasksConfig.mBlendEastBorder)
                    {
                        vNECoord.mX -= MasksConfig.mBlendBorderDistance;
                        vSECoord.mX -= MasksConfig.mBlendBorderDistance;
                    }
                    if (MasksConfig.mBlendSouthBorder)
                    {
                        vSECoord.mY -= MasksConfig.mBlendBorderDistance;
                        vSWCoord.mY -= MasksConfig.mBlendBorderDistance;
                    }
                    if (MasksConfig.mBlendWestBorder)
                    {
                        vSWCoord.mX += MasksConfig.mBlendBorderDistance;
                        vNWCoord.mX += MasksConfig.mBlendBorderDistance;
                    }

                    if (MasksConfig.mBlendNorthBorder)
                    {
                        mRawCoords.Add(vNWCoord);
                        mRawCoords.Add(vNECoord);
                        BorderAddLineAndClearRawLists(tLineType.eCoast);
                    }
                    if (MasksConfig.mBlendEastBorder)
                    {
                        mRawCoords.Add(vNECoord);
                        mRawCoords.Add(vSECoord);
                        BorderAddLineAndClearRawLists(tLineType.eCoast);
                    }
                    if (MasksConfig.mBlendSouthBorder)
                    {
                        mRawCoords.Add(vSECoord);
                        mRawCoords.Add(vSWCoord);
                        BorderAddLineAndClearRawLists(tLineType.eCoast);
                    }
                    if (MasksConfig.mBlendWestBorder)
                    {
                        mRawCoords.Add(vSWCoord);
                        mRawCoords.Add(vNWCoord);
                        BorderAddLineAndClearRawLists(tLineType.eCoast);
                    }

                }
            }
        }


        private void BorderAddLineAndClearRawLists(tLineType iLineType)
        {
            CreateLines();
            AddPointsToLists((Int32)(MasksConfig.tTransitionType.eBlendTransition), iLineType);
            AddLinesToLists((Int32)(MasksConfig.tTransitionType.eBlendTransition), iLineType);
            mRawCoordsLatLong.Clear();
            mRawCoords.Clear();
            mRawLines.Clear();
            mRawPoints.Clear();
        }

        private void FSEarthMasksForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            AbortAllOpenThreads();
        }

        private void AbortAllOpenThreads()
        {
            if (mMainThreadTimer != null)
            {
                mMainThreadTimer.Stop();
                mMainThreadTimer.Dispose();
                mMainThreadTimer = null;
            }
            if (mWorkThreadStarted) //no compiler warning for not used
            {
                //nothign to do
            }

            if (mMainWorkThread != null)
            {
                mMainWorkThread.Abort();
                mMainWorkThread = null;
            }
            
            MasksScriptsHandler.CleanUp();
        }
    }
}