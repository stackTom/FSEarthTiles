using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Globalization;
using FSEarthTilesInternalDLL;
using TGASharpLib;
using System.Collections.Concurrent;
using System.Linq;
using System.Drawing.Imaging;

//----------------------------------------------------------------------------
//            FS Earth Tiles  v1.0       HB-100 July 2008
//      
//         written / programmed by HB-100       (copyright HB-100)
//
//
//       This porgram and the source code is dedicated to the
//                Flight Simulator Community
//
//   and it's tireless effort to transform the Flight Simulator into an awesome
//   experience for everyone!
//
//
//        The program and the code may be used free. 
//              No limitations from my side.
//
//
//    Thank's to the following aktive contributers during the development: 
//            Steffen I.   (Expansions/Patches/Service3)
//            Wolfram R.   (Feedback/Ideas/Testing)
//            Christian B. (Hadn't started this project when there had been no TileProxy)
//            Antoine D.   (FS Discussions/Ideas exchange)
//            Thomas  M.   (CHPro Seasons Routines)
//            Oleg S.      (C# Script Library)
//            Kerry        
//            Jojo
//            fly-a-lot  and all the people of the FSPassengers forum
//
//----------------------------------------------------------------------------

//             The  Tile Levels
//------------------------------------------------------------------------------------------------------------
// Resolution              0.5m/pix  1m/pix  2m/pix 4m/pix ...      whole world  |  And Future: 0.25m/pix ...
// Earth Tiles Levels        0       1       2     3   ...  16    17    18       |               -1       ...
// Services    Levels       18      17      16    15   ...   2     1     0       |               19       ...
//------------------------------------------------------------------------------------------------------------


//----------------------------------------------------------------------------------------------------
//  for information on the Earth-Projection-Mathe (Tile mapping)and FS2004's LOD13 check EarthMath.cs
//----------------------------------------------------------------------------------------------------


//--------------------------------------------------------------------------------------------------------------------------
//
// Version 1.02 Updates
// M Smedley
// March 2012
//
//
//
// Fixed Google maps Useragent for web requests
//
// Added map caching under the work folder. User is prompted to clear out the cache at the start of a new download session.
// Added method to exclude user selected areas from being downloaded
// Added blank tiles to final bitmap
// Added KML area and exclude code
// Added point in polygon routines to exclude tiles 
// 
//
//--------------------------------------------------------------------------------------------------------------------------




namespace FSEarthTilesDLL
{


    struct MasksResampleWorker
    {
        public string AreaFileString;
        public EarthArea mEarthArea;           //The complete snapped Area coords information. That's the description of the Area that will be downloaded
        public EarthMultiArea mEarthMultiArea;      //The complete snapped Multi Area coords information. That's the describtion of the MultiArea that will be downloaded in steps of single Areas
        public AreaInfo mCurrentAreaInfo;
        public Int64 mCurrentActiveAreaNr;
        public Int64 mCurrentDownloadedTilesTotal;
        public Boolean mMultiAreaMode;
        public EarthAreaTexture mEarthAreaTexture;
    }

    public partial class FSEarthTilesForm : Form, FSEarthTilesInternalInterface, FSEarthTilesInterface
    {

        static Boolean mDebugMode = false;   //If true then the after download work will be done in the main thread also. The Application will not continue after the after download work is done.
                                             //Important: Set this to false in release!

        //-- Types --

        enum tCursor
        {
            eDefault,
            eCenter,
            eCorners,
            eBorderLeftRight,
            eBorderUpDown
        }

        //-- Struct --

        struct tWindowsElementLocation
        {
            public Boolean mInitialized;
            public Int32 mWindowOriginalWidth;
            public Int32 mWindowOriginalHeight;
            public Int32 mBitmapGroupBoxXPos;
            public Int32 mBitmapGroupBoxYPos;
            public Int32 mBitmapGroupBoxWidth;
            public Int32 mBitmapGroupBoxHeight;
            public Int32 mNormalPictureBoxWidth;
            public Int32 mNormalPictureBoxHeight;
            public Int32 mSmallPictureBoxXPos;
            public Int32 mDisplayNWButtonXPos;
            public Int32 mDisplayNEButtonXPos;
            public Int32 mDisplaySWButtonXPos;
            public Int32 mDisplaySEButtonXPos;
            public Int32 mDisplayCenterButtonXPos;
            public Int32 mResolutionTableLabelXPos;
            public Int32 mJumpToCornerLabelXPos;
            public Int32 mZoomLabelXPos;
            public Int32 mZoomSelectorBoxXPos;
            public Int32 mProxyLabelXPos;
            public Int32 mNextButtonXPos;
            public Int32 mDlQueueLabelXPos;
            public Int32 mProgressQueue1Box1XPos;
            public Int32 mProgressQueue1Box2XPos;
            public Int32 mProgressQueue1Box3XPos;
            public Int32 mProgressQueue1Box4XPos;
            public Int32 mServiceSourcesLabelXPos;
            public Int32 mCookieLabelXPos;
            public Int32 mMemoryGroupBoxXPos;
            public Int32 mVersionsLabelXPos;
            public Int32 mVersionsLabelYPos;
            public Int32 mStatusBoxXPos;
            public Int32 mStatusBoxYPos;
            public Int32 mStatusBoxWidth;
            public Int32 mStatusBoxHeight;
            public Int32 mWWWButtonXPos;
            public Int32 mWWWButtonYPos;
            public Int32 mWebBrowserPanicButtonXPos;
            public Int32 mWebBrowserPanicButtonYPos;
        }


        //-- Constants--

        //User drawing input Area
        const Double cAktiveSpotPixelDiameter          = 3.0;             //User click active diameter of input area corner and center point
        const Double cOnDrawingMaxMapScrollSpeedPixel  = 20.0;            //Defines the maximum scroll speed in Pixel when User Drawing is active.
        const Double cOnDrawingMapActivePixelBorder    = 20.0;            //Scrolling Active Zone within the Display on drawing.

        //Memory Sizes        
        const Double cPixelsPerTile           = 256.0*256.0;              //[Pixels]
        const Double cPixelToTileFactor       = 1.0/ cPixelsPerTile;      //[Tiles/Pixels],  1 Pixel = cPixelToTileFactor Tiles

        const Double cFetchTileSize           = 0.012;                    //[MByte] ca. 12.5 KByte per tile
        const Double cSceneryTileSize         = 0.20;                     //[MByte] ca.  1.1 * the raw bmp
        const Double cUsedMemoryPerPixel      = 3.0 / (1024.0 * 1024.0);  //[MByte] 1Pixel in a 24Bit Bmp (RGB) requires 3 Bytes 
        const Double cApplicationMemoryOffset = 150.0;                    //[MByte] Don't know what is 150 MByte large in this App.. guess the .NET
        const Double cWorkSpaceMemoryOffset   = 2.0;                      //[MByte]

        const Int64 cMaxDrawnAndHandledAreas  = 3000;                     //Don't make it too large, painting and calculating them becomes extermly slow

        //-- Members --

        //Appl-Controll Flags
        Boolean mConfigInitDone;               // true when the Application Initialisation was successful 
        Boolean mInputCoordsValidity;          // true when the entered coordinates are valid
        Boolean mAreaProcessRunning;           // true when the Area is processed (the whole thing from Download to finsih scenery)
        Boolean mStopProcess;                  // true when the Area build process should be stopped
        Boolean mAllowDisplayToSetStatus;      // true when the Display is allowed to print status/coord information
        Boolean mTimeOutBlock;                 // true when download is blocked due to a time out
        Boolean mFirstEvent;                   // true when it is the very first Main Timer Event Call of the application
        Boolean mApplicationExitCheck;         // true when the area work is in progress (mAreaProcessRunning) and we wait for a status change for possible Appl exit
        Boolean mMultiAreaMode;                // true when MultiArea mode is on.
        Boolean mAutoReference;                // true when Auto Single Area Reference mode is on
        Boolean mBlockInputChangeHandle;       // true when handleInputDataChange execution should be blocked. That Makes safe block Update possible
        Boolean mFirstMainTimerEventHappend;   // true when the very first Main Timer Event Call of the application happen (method enter)
        Boolean mSkipAreaProcessFlag=false;    // True when we do not want to clear the areas to skip flags 
        //The active Area data containers (objects)
        EarthInputArea   mEarthInputArea;      //The Input coords. That's the coords the user enters eighter direct or by drawing the Input Area.
        EarthArea        mEarthArea;           //The complete snapped Area coords information. That's the description of the Area that will be downloaded
        EarthMultiArea   mEarthMultiArea;      //The complete snapped Multi Area coords information. That's the describtion of the MultiArea that will be downloaded in steps of single Areas
        EarthAreaTexture mEarthAreaTexture;    //The downloaded Texture of the Area
        EarthArea mLastMeshCreatedEarthArea; // let's us see if we've created mesh for this area already
        EarthArea mLastCheckedForAllWaterEarthArea; // let's us see if we've checked for AllWater() for this area already

        //(Backup Data) Single Reference Area Stored Datas. (Used for Single-Multi Mode change and as Reference Area for Multi)
        EarthInputArea   mEarthSingleReferenceInputArea; //The Reference Input Area. The MultiArea is build of this Areas
        EarthArea        mEarthSingleReferenceArea;      //The Reference  Area. The MultiArea is build of such Areas
        Boolean          mSingleReferenceAreaValid;      //true when at least one first vaild Reference has been stored.

        //(Backup Data) Multi Area Stored Datas.        (Used for Single-Multi Mode change)
        EarthInputArea mEarthMultiAreaBackupInputArea;  //Data Backup's for Single - Multi Mode change 
        EarthMultiArea mEarthMultiAreaBackupArea;       //Data Backup's for Single - Multi Mode change 
        Boolean        mMultiAreaBackupValid;           //true when at least one first vaild Backup has been stored.

        EarthKML       mEarthKML;                       //AreaKML.kml file reader 
        Boolean mUsingKML = false;
        //Current datas during processing
        AreaInfo mCurrentAreaInfo;
        Int64    mCurrentActiveAreaNr;
        Int64    mCurrentDownloadedTilesTotal; 

        //Status counter of Area Download
        Int64 mAreaDownloadTilesCount;
        Int64 mAreaDownloadTilesCountIncludingFailed;
        Int64 mAreaDownloadTilesMiss;
        Int64 mAreaDownloadRepetitions;
        Int64 mLastDownloadProcessTileMisses;

        //Download Speed Control
        Int32  mAreaTilePushTick;

        //Tile Download Time Out Handling
        Int32   mTileFailedStartTickCount;      // The time in ticks (milliseconds) of the first Tile download failed
        Boolean mTileFailedTimeOutCountAktive;  // True when the Time Out Counting is in porgress

        //Area Download Tile Check List
        List<TileInfo> mAreaTilesInfoDownloadCheckList;  //Check/control List of required Tiles to download and fill the area

        //Special Tiles
        Bitmap mNoTileFound;
        Bitmap mTileRequested;
        Bitmap mTileSkipped;
        
        //Global Random Generator
        Random mRandomGenerator;

        tWindowsElementLocation mWindowElementPosition;

        //Cursor
        tCursor mActiveCursor;


        //Mouse / free moving map Handling
        Boolean mMouseDownFlag;
        Int64   mMouseDownX;
        Int64   mMouseDownY;


        //DisplayCoords
        Double mDisplayCenterLongitude;
        Double mDisplayCenterLatitude;


        //User Area Draw
        Boolean mUserDrawAreaActive;
        Boolean mUserSetCenterActive;
        
        Boolean mMapScrollingOnUserDrawing; //true = actiive, false = not active
        Double  mAreaDrawFixPointLatitude;
        Double  mAreaDrawFixPointLongitude;
        Int64   mUserDrawingMousePosXScrolling; //Stored MousePosition for AutoScroll (with timer) on Drawing
        Int64   mUserDrawingMousePosYScrolling;

        // User exclude areas
        Boolean mUserDrawExcludeArea;   // True when we are drawing areas to exclude
        Double mExcludeAreaDrawFixPointLatitude;
        Double mExcludeAreaDrawFixPointLongitude;

        // Download Area Tiles

        DownloadTiles DownloadAreaTiles;
        ProcessAreas AreaProcessInfo=new ProcessAreas();

        // Initialise exclude area array
        //ExcludeArea[] vExcludeAreas = ExcludeArea.NewInitArray(0);
       ExcludeArea vExcludeAreas= new ExcludeArea();

        //Ringbuffer Display Cache for speeding up the display only, not used for download
        const Int32 cDisplayTileCacheSize = 200;
        TileRingBufferCache mDisplayTileCache;


        //Display and General Area Download Queue Main Thread
        const Int32 cDisplayTileQueueSize = 40;  //Keep low so uniteresting old/tiles becomes skipped
        TileInfoFIFOQueue mTileInfoDisplayQueue;
        TileInfoFIFOQueue mTileInfoAreaQueue;    //size assigned at runtime fitting to area (size equals total tiles in area to download) 
        AreaInfoFIFOQueue mAreaInfoAreaQueue;    //the queue for multi area download


        //Engine Work Load Tile Controll list for Main Thread to avoid multiple request of the same tiles
        List<TileInfo> mToEngineDelegatedTilesControllList1;
        List<TileInfo> mToEngineDelegatedTilesControllList2;     //used if we want to request tiles twice. (better for display speed up because often one blocks a little)
        Boolean        mAllowTileDoubleRequest;                  //Allows to queue/request a specific Tile up to twice for speed up purpose (if by random queued in the same engine it will be requests 1 time only)


        //Suspicious
        List<TileInfo> mSuspiciousList1;    //Suspicious Tiles will be marked bad and resheduled
        List<TileInfo> mSuspiciousList2; 


        //Main thread work feed and harvest mutex
        Mutex mWorkFeedEnginesMutex;        //probabily pointless because there is only 1 thread and timer event come over the form queue as far as I know but safe is safe
        Mutex mSetFriendThreadStatusMutex;  //for Status transfer from Area Processing aftermath thread 

        //And the Threads Themself
        List<Thread>      mEngineThreads;
        Boolean     mThreadsStarted;

        //Area After Download Processing Friend Thread (Texture undistortion / Scenery compilation etc)
        Thread      mAreaAftermathThread;
        String      mStatusFriendThread;
        String      mExitStatusFriendThread;

        // Imagetool thread
        Thread mImageToolThread;
        // CreateMesh thread
        Thread mCreateMeshThread;

        // Producer/consumer to run resample processes
        MultiThreadedQueue mMasksCompilerMultithreadedQueue;

        // Producer/consumer to run image processing (undistortion etc.)
        MultiThreadedQueue mImageProcessingMultithreadedQueue;

        //The WorldWideWeb Browser
        EarthWebForm mEarthWeb;

        TileInfo mLastTileInfo;      //Required to evaluate the Address for the WebBrowser

        //Cookie
        CookieCollection mCookies;
        Boolean          mHandleCookies;             //If true cookieCollection will be atatched to request.
        String           mCookieContent;             //The Content of the Cookies you want to send

        //Proxy
        String mProxy;
        
        //Title BackUp
        String mTitle;

        //And our Main Thread Timer
        System.Windows.Forms.Timer mMainThreadTimer;  //Main Thread Timer

        private bool scenProcWasRunning = false;
        private bool creatingMeshFile = false;


        public FSEarthTilesForm(String[] iApplicationStartArguments, List<String> iDirectConfigurationList, String iFSEarthTilesApplicationFolder)
        {
            mWindowElementPosition.mInitialized = false;  //Somehow InitializeComponent() seems to call a resize event before InitializeFSEarthTiles ever comes into play.
            InitializeComponent();
            InitializeFSEarthTiles(iApplicationStartArguments, iDirectConfigurationList,iFSEarthTilesApplicationFolder);
        }
        
        ~FSEarthTilesForm()
        {
            AbortAllOpenThreads();
        }

        void InitializeFSEarthTiles(String[] iApplicationStartArguments, List<String> iDirectConfigurationList, String iFSEarthTilesApplicationFolder)
        {

            mTitle = this.Text;

            mThreadsStarted       = false;

            mFirstEvent                 = true;
            mFirstMainTimerEventHappend = false;
            mApplicationExitCheck = false;

            mMultiAreaMode        = false;
            mAutoReference        = true;

            mWorkFeedEnginesMutex       = new Mutex();
            mSetFriendThreadStatusMutex = new Mutex();

            mBlockInputChangeHandle = false;

            mAllowDisplayToSetStatus = true;

            mStatusFriendThread     = "";
            mExitStatusFriendThread = "";

            mTileInfoDisplayQueue = new TileInfoFIFOQueue(cDisplayTileQueueSize);
            mTileInfoAreaQueue    = new TileInfoFIFOQueue(10); //Becomes Reassigned so Initial size doesnt really matters
            mAreaInfoAreaQueue    = new AreaInfoFIFOQueue(10); //Becomes Reassigned so Initial size doesnt really matters

            mToEngineDelegatedTilesControllList1 = new List<TileInfo>();
            mToEngineDelegatedTilesControllList2 = new List<TileInfo>();
            mAllowTileDoubleRequest = true;

            mSuspiciousList1 = new List<TileInfo>();  //Suspicious Tiles will be marked bad and resheduled
            mSuspiciousList2 = new List<TileInfo>(); 

            mEarthInputArea   = new EarthInputArea(); 
            mEarthArea        = new EarthArea();
            mEarthMultiArea   = new EarthMultiArea();
            mEarthAreaTexture = new EarthAreaTexture();

            mEarthSingleReferenceInputArea = new EarthInputArea();
            mEarthSingleReferenceArea      = new EarthArea();
            mSingleReferenceAreaValid      = false;

            mEarthMultiAreaBackupInputArea = new EarthInputArea();
            mEarthMultiAreaBackupArea      = new EarthMultiArea();
            mMultiAreaBackupValid          = false;

            mEarthKML = new EarthKML();

            mCurrentAreaInfo = new AreaInfo(0,0);
            mCurrentActiveAreaNr = 1;
            mCurrentDownloadedTilesTotal = 0; 

            mCookies          = new CookieCollection();
            mHandleCookies    = false;
            mRandomGenerator  = new Random();
            mProxy            = "direct";
            mCookieContent    = "";

            mLastTileInfo = new TileInfo();

            mAreaDownloadTilesCount  = 0;
            mAreaDownloadTilesCountIncludingFailed = 0;
            mAreaDownloadTilesMiss   = 0;
            mAreaDownloadRepetitions = 0;

            mAreaTilesInfoDownloadCheckList = new List<TileInfo>();


            mTileFailedStartTickCount     = 0;
            mTileFailedTimeOutCountAktive = false;
            mTimeOutBlock        = false;

            mAreaTilePushTick = 0;

            //intialize statics
            EarthMath.Initialize();
            EarthConfig.Initialize(iFSEarthTilesApplicationFolder);

            //Initialize Timer for AutoScrolling on User drawing
            mMainThreadTimer          = new System.Windows.Forms.Timer();
            mMainThreadTimer.Tick    += new EventHandler(MainThreadTimerEventProcessor);
            mMainThreadTimer.Interval = 25;  // 25ms

            //Intialize variables
            mConfigInitDone     = false;
            mAreaProcessRunning = false;


            mStopProcess = false;

            mMouseDownFlag = false;
            mMouseDownX = 0;
            mMouseDownY = 0;

            mActiveCursor = tCursor.eDefault;

            mUserDrawAreaActive        = false;
            mUserSetCenterActive       = false;
            mMapScrollingOnUserDrawing = false;


            //Init Display Cache Ringbuffer
            mDisplayTileCache = new TileRingBufferCache(cDisplayTileCacheSize);

            //Prepare GUI Enable/Disable
            HandleEnableDisableLongLatInputFields();
            SetVisibilityForIdle();

            //Store and Keep Dimension
            mWindowElementPosition.mWindowOriginalWidth = this.Width;
            mWindowElementPosition.mWindowOriginalHeight = this.Height;
            mWindowElementPosition.mBitmapGroupBoxXPos = BitmapGroupBox.Location.X;
            mWindowElementPosition.mBitmapGroupBoxYPos = BitmapGroupBox.Location.Y;
            mWindowElementPosition.mBitmapGroupBoxWidth = BitmapGroupBox.Width;
            mWindowElementPosition.mBitmapGroupBoxHeight = BitmapGroupBox.Height;
            mWindowElementPosition.mNormalPictureBoxWidth = NormalPictureBox.Width;
            mWindowElementPosition.mNormalPictureBoxHeight = NormalPictureBox.Height;
            mWindowElementPosition.mSmallPictureBoxXPos = SmallPicturePox.Location.X;
            mWindowElementPosition.mDisplayNWButtonXPos = DisplayNWButton.Location.X;
            mWindowElementPosition.mDisplayNEButtonXPos = DisplayNEButton.Location.X;
            mWindowElementPosition.mDisplaySWButtonXPos = DisplaySWButton.Location.X;
            mWindowElementPosition.mDisplaySEButtonXPos = DisplaySEButton.Location.X;
            mWindowElementPosition.mDisplayCenterButtonXPos = DisplayCenterButton.Location.X;
            mWindowElementPosition.mResolutionTableLabelXPos = ResolutionTableLabel.Location.X;
            mWindowElementPosition.mJumpToCornerLabelXPos = JumpToCornerLabel.Location.X;
            mWindowElementPosition.mZoomLabelXPos = ZoomLabel.Location.X;
            mWindowElementPosition.mZoomSelectorBoxXPos = ZoomSelectorBox.Location.X;
            mWindowElementPosition.mProxyLabelXPos = ProxyLabel.Location.X;
            mWindowElementPosition.mNextButtonXPos = NextButton.Location.X;
            mWindowElementPosition.mDlQueueLabelXPos = DlQueueLabel.Location.X;
            mWindowElementPosition.mProgressQueue1Box1XPos = ProgressQueue1Box.Location.X;
            mWindowElementPosition.mProgressQueue1Box2XPos = ProgressQueue2Box.Location.X;
            mWindowElementPosition.mProgressQueue1Box3XPos = ProgressQueue3Box.Location.X;
            mWindowElementPosition.mProgressQueue1Box4XPos = ProgressQueue4Box.Location.X;
            mWindowElementPosition.mServiceSourcesLabelXPos = ServiceSourcesLabel.Location.X;
            mWindowElementPosition.mCookieLabelXPos = CookieLabel.Location.X;
            mWindowElementPosition.mMemoryGroupBoxXPos = MemoryGroupBox.Location.X;
            mWindowElementPosition.mVersionsLabelXPos = VersionsLabel.Location.X;
            mWindowElementPosition.mVersionsLabelYPos = VersionsLabel.Location.Y;
            mWindowElementPosition.mStatusBoxXPos = StatusBox.Location.X;
            mWindowElementPosition.mStatusBoxYPos = StatusBox.Location.Y;
            mWindowElementPosition.mStatusBoxWidth  = StatusBox.Width;
            mWindowElementPosition.mStatusBoxHeight = StatusBox.Height;
            mWindowElementPosition.mWWWButtonXPos = WWWButton.Location.X;
            mWindowElementPosition.mWWWButtonYPos = WWWButton.Location.Y;
            mWindowElementPosition.mWebBrowserPanicButtonXPos = WebBrowserPanicButton.Location.X;
            mWindowElementPosition.mWebBrowserPanicButtonYPos = WebBrowserPanicButton.Location.Y;
            mWindowElementPosition.mInitialized   = true;

            //Intialize Tiles for Error

            Bitmap vEmptyDummyTileBitmap = new Bitmap(256,256);

            mNoTileFound   = vEmptyDummyTileBitmap;
            mTileRequested = vEmptyDummyTileBitmap;
            mTileSkipped   = vEmptyDummyTileBitmap;



            Boolean vContinueInitialisation = true;
            if (vContinueInitialisation)
            {
                try
                {
                    mNoTileFound = new Bitmap(EarthConfig.mStartExeFolder + "\\" + "NoTileFound.jpg");
                }
                catch
                {
                    SetStatus("Initialisation failed. NoTileFound.jpg File Missing!");
                    vContinueInitialisation = false;
                }
            }
            if (vContinueInitialisation)
            {
                try
                {
                    mTileRequested = new Bitmap(EarthConfig.mStartExeFolder + "\\" + "TileRequested.jpg");
                }
                catch
                {
                    SetStatus("Initialisation failed. NoTileFound.jpg File Missing!");
                    vContinueInitialisation = false;
                }
            }
            if (vContinueInitialisation)
            {
                try
                {
                    mTileSkipped = new Bitmap(EarthConfig.mStartExeFolder + "\\" + "TileSkipped.jpg");
                }
                catch
                {
                    SetStatus("Initialisation failed. TileSkipped.jpg File Missing!");
                    vContinueInitialisation = false;
                }

            }

            if (vContinueInitialisation)
            {
                try
                {
                    mTileSkipped = new Bitmap(EarthConfig.mStartExeFolder + "\\" + "Blank.jpg");
                }
                catch
                {
                    SetStatus("Initialisation failed. Blank.jpg File Missing!");
                    vContinueInitialisation = false;
                }

            }

            
            if (vContinueInitialisation)
            {
                try
                {

                    if (EarthConfig.LoadConfigFile(this))
                    {
                        if (EarthConfig.ParseConfigurationList(iDirectConfigurationList))
                        {
                            if (EarthConfig.ParseCommandLineArguments(this,iApplicationStartArguments))
                            {
                                if (FillInFormWithEarthConfigValues())
                                {
                                    PrepareMultiThreading();

                                    mConfigInitDone = true;

                                    SetVisibilityForIdle(); //visibility chanegs depending on read config

                                    SetNextProxy();

                                    SetCookies();

                                    HandleInputChange();

                                    StartMultiThreading();

                                    mMainThreadTimer.Start();

                                    DisplaySpot();

                                    SetLastTileInfoToDisplayCenter();

                                }
                            }
                        }
                    }

                    ScenprocUtils.scriptsDir = Path.GetFullPath(@".\Scenproc_scripts");
                }
                catch
                {
                    SetStatus("Initialisation failed. Reason unknown. Check your FSEarthTiles.ini.");
                    vContinueInitialisation = false;
                }
            }
        }

        void FillInputAreaDatasIntoForm(EarthInputArea iEarthInputArea)
        {
            mBlockInputChangeHandle = true; //BlockUpdate Till everything is filled in
            AreaSizeXBox.Text = Convert.ToString(iEarthInputArea.AreaXSize);
            AreaSizeYBox.Text = Convert.ToString(iEarthInputArea.AreaYSize);
            SetInputCenterCoordLatitudeTo(iEarthInputArea.SpotLatitude);
            SetInputCenterCoordLongitudeTo(iEarthInputArea.SpotLongitude);
            SetInputNWCornerLatitudeTo(iEarthInputArea.AreaStartLatitude);
            SetInputNWCornerLongitudeTo(iEarthInputArea.AreaStartLongitude);
            SetInputSECornerLatitudeTo(iEarthInputArea.AreaStopLatitude);
            SetInputSECornerLongitudeTo(iEarthInputArea.AreaStopLongitude);
            mBlockInputChangeHandle = false;
            HandleInputChange(); //proper and safe to do beacsue is blocked anyway because this FillInputFunction is called with mConfigInitDone = false
        }

        Boolean FillInFormWithEarthConfigValues()
        {
           Boolean vAllOk = true;

           try
           {
               //Label
               String cBreakLine  = "-------------\n";
               String vShrinkOrEnlargSign = ""; 
               String vStringValue;
               Double vValue = 0.25;  // for -1
               Boolean vShrink = true;
               Boolean vShrinkBefore = true;
               vShrinkOrEnlargSign = " ";

               if (!EarthConfig.mFSPreResampleingAllowShrinkTexture)
               {
                   vShrink       = false;
                   vShrinkBefore = false;
                   vShrinkOrEnlargSign = "´";
                   vValue = 0.125;
               }

               String vLabelText = "";
               for (Int32 vResolutionLevel  = 0; vResolutionLevel <=8; vResolutionLevel++)
               {
                  if (EarthConfig.mFSPreResampleingAllowShrinkTexture)
                  {
                      if (vResolutionLevel > EarthConfig.mFSPreResampleingShrinkTextureThresholdResolutionLevel)
                      {
                          vShrink = false;
                          vShrinkOrEnlargSign = "´";
                          if (vResolutionLevel == 0)
                          {
                              vValue = 0.25;
                          }
                      }
                  }

                  if (vShrink == vShrinkBefore)
                  {
                      vValue *= 2.0;
                  }
                  vShrinkBefore = vShrink;

                  if (vResolutionLevel == 1)
                  {
                    vLabelText += cBreakLine;
                  }
                  if (vResolutionLevel == 5)
                  {
                    vLabelText += cBreakLine;
                  }
                  vStringValue = Convert.ToString(vValue);
                  if (vStringValue.Length<=0)
                  {
                      //Dont display anything
                  }
                  else if (vStringValue.Length<=1)
                  {
                      vLabelText += vResolutionLevel + " ->";
                      vLabelText += "  " + vStringValue + vShrinkOrEnlargSign;
                      vLabelText += "m/Pix\n";
                  }
                  else if (vStringValue.Length<=2)
                  {
                      vLabelText += vResolutionLevel + " ->";
                      vLabelText += " " + vStringValue + vShrinkOrEnlargSign;
                      vLabelText += "m/Pix\n";
                  }
                  else if (vStringValue.Length<=3)
                  {
                      vLabelText += vResolutionLevel + " ->";
                      if ((vValue<1.0) && (vShrink))
                      {
                         vLabelText += " " + vStringValue;
                      }
                      else
                      {
                         vLabelText += vStringValue + vShrinkOrEnlargSign;
                      }
                      vLabelText += "m/Pix\n";
                  }
                  else if (vStringValue.Length<=4)
                  {
                      vLabelText += vResolutionLevel + " ->";
                      vLabelText += vStringValue;
                      if (vShrink)
                      {
                          vLabelText += "m/Pix\n";
                      }
                      else
                      {
                          vLabelText += vShrinkOrEnlargSign + "m/Px\n";
                      }
                  }
                  else
                  {
                      //Add nothing just dont display it
                  }

               }
               ResolutionTableLabel.Text = vLabelText;

              /*
0 -> 0.5m/Pix
-------------
1 ->  1 m/Pix
2 ->  2 m/Pix
3 ->  4 m/Pix
4 ->  8 m/Pix
-------------
5 -> 16 m/Pix
6 -> 32 m/Pix
7 -> 64 m/Pix
8 ->128 m/Pix
               */

               //aktive ones
               WorkFolderBox.Text = EarthConfig.mWorkFolder;
               SceneryFolderBox.Text = EarthConfig.mSceneryFolder;

               if (EarthConfig.mUseCache)
               {
                   CacheSceneryBox.Text = "Yes";
               }

               else
               {
                   CacheSceneryBox.Text = "No";
               }

               ZoomSelectorBox.Value = EarthConfig.mZoomLevel;
               DisplayModeBox.Text = EarthConfig.mDisplayMode;

               FetchSelectorBox.Value = EarthConfig.mFetchLevel;
               AreaSnapBox.Text = EarthConfig.GetAreaSnapString();
               ServiceBox.Text = EarthConfig.GetServiceString();
               CompilerSelectorBox.Text = EarthConfig.mSelectedSceneryCompiler;
               CreateMasksBox.Text = EarthConfig.GetCreateMask();
               CompileSceneryBox.Text = EarthConfig.GetCompileScenery();
               AutoRefSelectorBox.Text = EarthConfig.GetAutoReferenceModeString();
               CreateScenprocBox.Text = EarthConfig.GetCreateScenproc();
               //one timer This EarthConfig values are only used and handled once ..exactly here..no updates of this values during runtime in EarthConfig
               AreaDefModeBox.Text = EarthConfig.mAreaDefModeStart;
               mEarthInputArea.Copy(EarthConfig.mAreaInputStart);

               FillInputAreaDatasIntoForm(EarthConfig.mAreaInputStart);

               Boolean vSelectedServiceExist = false;
               Int32 vFirst = -1;
               String vService;
               ServiceBox.Items.Clear();
               for (Int32 vCon = 1; vCon <= 9; vCon++)
               {
                   if (EarthConfig.mServiceExists[vCon - 1])
                   {
                       if (vFirst < 0)
                       {
                           vFirst = vCon;
                       }
                       vService = EarthConfig.mServiceName[vCon-1];
                       if (vService.Length ==0)
                       {
                           vService = "Service" + Convert.ToString(vCon);
                       }
                       ServiceBox.Items.Add(vService.Clone());
                       if (vCon == EarthConfig.mSelectedService)
                       {
                           vSelectedServiceExist = true;
                       }
                   }
               }

                foreach (KeyValuePair<string, LayProvider> kv in EarthConfig.layProviders)
                {
                    if (kv.Value.inGui)
                    {
                        ServiceBox.Items.Add(kv.Key);
                    }
                }

               if (vSelectedServiceExist)
               {
                   ServiceBox.Text = EarthConfig.GetServiceString();
               }
               else
               {
                   if (vFirst >= 0)
                   {

                       if (EarthConfig.mServiceName[vFirst - 1].Length > 0)
                       {
                           ServiceBox.Text = EarthConfig.mServiceName[vFirst - 1];
                           EarthConfig.mSelectedService = vFirst;
                       }
                       else
                       {

                           EarthConfig.mSelectedService = vFirst;
                           ServiceBox.Text = "Service" + Convert.ToString(vFirst);
                       }
                   }
                   else
                   {
                       EarthConfig.mSelectedService = -1;
                       ServiceBox.Text = "invalid";
                       SetStatus("Initialisation failed. No valid Service configurated. Check your FSEarthTiles.ini.");
                       vAllOk = false;
                   }

               }
           }
           catch
           {
               SetStatus("Error. Initialisation failed. Config value processing failed. Check your FSEarthTiles.ini.");
               vAllOk = false;
           }
           return vAllOk;
        }


        void PrepareMultiThreading()
        {
            //Set up the  Threading
            EarthEngines.PrepareMultiThreading();
            mEngineThreads = new List<Thread>(EarthConfig.mMaxDownloadThreads);
            for (int i = 0; i < EarthConfig.mMaxDownloadThreads; i++)
            {
                int kingdomIdx = i; // needed so we don't call kingdom with the last i
                mEngineThreads.Add(new Thread(() => EarthEngines.EngineKingdom(kingdomIdx)));
            }


            ThreadStart vAreaAftermathDelegate = new ThreadStart(AreaAfterDownloadProcessing);
            mAreaAftermathThread               = new Thread(vAreaAftermathDelegate);

            ThreadStart vImageToolThreadDelegate = new ThreadStart(RunImageToolProcessing);
            mImageToolThread = new Thread(vImageToolThreadDelegate);

        }


        void StartMultiThreading()
        {
            
            //do the last preparation. -> Only now we have the NoTileFound in the memory
            EarthEngines.SetNoTileFoundBitmapEngines(mNoTileFound);

            //And start the threading
            foreach (Thread mEngineThread in mEngineThreads)
            {
                mEngineThread.Start();
            }

            mThreadsStarted = true;
        }

        void SetNextProxy()
        {
            if (EarthConfig.mProxyList.Count>0)
            {
                EarthConfig.mProxyListIndex++;
                if (EarthConfig.mProxyListIndex >= EarthConfig.mProxyList.Count)
                {
                    EarthConfig.mProxyListIndex = 0;
                }
                String vProxy = EarthConfig.mProxyList[EarthConfig.mProxyListIndex];
                mProxy = vProxy;
                EarthEngines.SetProxyEngines(vProxy);
                ProxyLabel.Text = vProxy;
                ProxyLabel.Refresh();
            }
            else
            {
                mProxy = "direct";
                EarthEngines.SetProxyEngines("direct");
                ProxyLabel.Text = "direct";
                ProxyLabel.Refresh();
            }
        }

        void CreateCookies() // Creates the CookieColelction mCookies out of the mCookieContent String
        {
            String vWorkString = mCookieContent;
            String vCookieValue1 = "";
            String vCookieValue2 = "";
            String vCookieValue3 = "";
            String vCookieValue4 = "";
            String vCookieValue5 = "";
            String vCookieValue6 = "";
            String vCookieValue7 = "";
            String vCookieValue8 = "";
            String vCookieValue9 = "";
            Int32 vIndex1 = 0;
            Int32 vIndex2 = 0;
            Int32 vIndex3 = 0;
            Int32 vIndex4 = 0;
            Int32 vIndex5 = 0;
            Int32 vIndex6 = 0;
            Int32 vIndex7 = 0;
            Int32 vIndex8 = 0;
            Int32 vIndex9 = 0;
            Int32 vIndex9b = 0;

            try
            {
                do
                {
                    vIndex1 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex1 >= 0)
                    {
                        vCookieValue1 = vWorkString.Remove(vIndex1);
                        vWorkString = vWorkString.Substring(vIndex1 + 1, vWorkString.Length - (vIndex1 + 1));
                    }
                    vIndex2 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex2 >= 0)
                    {
                        vCookieValue2 = vWorkString.Remove(vIndex2);
                        vWorkString = vWorkString.Substring(vIndex2 + 1, vWorkString.Length - (vIndex2 + 1));
                    }
                    vIndex3 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex3 >= 0)
                    {
                        vCookieValue3 = vWorkString.Remove(vIndex3);
                        vWorkString = vWorkString.Substring(vIndex3 + 1, vWorkString.Length - (vIndex3 + 1));
                    }
                    vIndex4 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex4 >= 0)
                    {
                        vCookieValue4 = vWorkString.Remove(vIndex4);
                        vWorkString = vWorkString.Substring(vIndex4 + 1, vWorkString.Length - (vIndex4 + 1));
                    }
                    vIndex5 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex5 >= 0)
                    {
                        vCookieValue5 = vWorkString.Remove(vIndex5);
                        vWorkString = vWorkString.Substring(vIndex5 + 1, vWorkString.Length - (vIndex5 + 1));
                    }
                    vIndex6 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex6 >= 0)
                    {
                        vCookieValue6 = vWorkString.Remove(vIndex6);
                        vWorkString = vWorkString.Substring(vIndex6 + 1, vWorkString.Length - (vIndex6 + 1));
                    }
                    vIndex7 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex7 >= 0)
                    {
                        vCookieValue7 = vWorkString.Remove(vIndex7);
                        vWorkString = vWorkString.Substring(vIndex7 + 1, vWorkString.Length - (vIndex7 + 1));
                    }
                    vIndex8 = vWorkString.IndexOf("\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex8 >= 0)
                    {
                        vCookieValue8 = vWorkString.Remove(vIndex8);
                        vWorkString = vWorkString.Substring(vIndex8 + 1, vWorkString.Length - (vIndex8 + 1));
                    }
                    vIndex9 = vWorkString.IndexOf("*", StringComparison.CurrentCultureIgnoreCase);
                    vIndex9b = vWorkString.IndexOf("*\n", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex9 >= 0)
                    {
                        vCookieValue9 = vWorkString.Remove(vIndex9 + 1); //+1 we want to have the star!
                        if (vIndex9b == vIndex9)
                        {
                            vWorkString = vWorkString.Substring(vIndex9 + 2, vWorkString.Length - (vIndex9 + 2));
                        }
                        else
                        {
                            vWorkString = vWorkString.Substring(vIndex9 + 1, vWorkString.Length - (vIndex9 + 1));
                        }
                    }

                    if (EarthCommon.StringCompare(vCookieValue9, "*"))
                    {
                        //We have a valid Cookie
                        
                        //Cookie decoding this
                        //http://weitips.blogspot.com/2006/04/cookie-file-format-of-internet.html
                        //and
                        //http://seclists.org/basics/2006/Mar/0427.html

                        Cookie vCookie = new Cookie(vCookieValue1, vCookieValue2);
                        Uri vUri = new Uri("http://" + vCookieValue3);
                        vCookie.Domain = vUri.Host;
                        vCookie.Path = vUri.AbsolutePath;
                        
                        //t = 1e-7*(high*pow(2,32)+low) - 11644473600
                        DateTime vDateTime = new DateTime(1970,1,1);
                        Int64 vLow  = Convert.ToInt64(vCookieValue5);
                        Int64 vHigh = Convert.ToInt64(vCookieValue6);
                        vHigh = vHigh << 32;
                        Double vSeconds = (1e-7) * Convert.ToDouble(vHigh + vLow) - 11644473600.0 + 3600.0;
                        vDateTime = vDateTime.AddSeconds(vSeconds);
                        vCookie.Expires = vDateTime;
                        
                        DateTime vDateTime2 = new DateTime(1970, 1, 1);
                        vLow  = Convert.ToInt64(vCookieValue7);
                        vHigh = Convert.ToInt64(vCookieValue8);
                        vHigh = vHigh << 32;
                        vSeconds = (1e-7) * Convert.ToDouble(vHigh + vLow) - 11644473600.0 + 3600.0;
                        vDateTime2 = vDateTime2.AddSeconds(vSeconds);
                        //vCookie.TimeStamp = vDateTime2;

                        mCookies.Add(vCookie);

                        String vHttpCookieString = vCookie.ToString();

                    }

                    vCookieValue9 = ""; //mark as invalid for next turn

                } while (vIndex1 >= 0);
            }
            catch
            {
                SetStatus("  catch -> Cookie decoding failed! Pls COPY and PAST a proper cookie content.");
                Thread.Sleep(2000);
            }
        }

        void SetCookies()
        {
            mCookies = new CookieCollection();
            if (EarthConfig.mUseCookies && !EarthCommon.StringCompare(EarthConfig.mCookieFolder, "Undefined"))
            {
                /* Folder content is invisible to .NET :/
                Boolean vExists = Directory.Exists(EarthConfig.mCookieFolder);
                Boolean vExists2 = File.Exists ("C:\\Dokumente und Einstellungen\\Rol\\Lokale Einstellungen\\Temporary Internet Files\\zz999.gif");

                DirectorySecurity fSecurity = Directory.GetAccessControl(EarthConfig.mCookieFolder);
                DirectorySecurity fSecurity2 = Directory.GetAccessControl("E:\\secured");
                fSecurity2.AccessRightType(FileSystemRights.FullControl);
                //String [] vCookieFiles = Directory.GetFiles(EarthConfig.mCookieFolder);
                String [] vCookieFiles = Directory.GetFiles(EarthConfig.mCookieFolder, "*g****e*",SearchOption.AllDirectories);
                //String[] vCookieFiles = Directory.GetFiles(@"C:\\Dokumente und Einstellungen\\Rol\\Lokale Einstellungen\\Temporary Internet Files", "*g*");

                //String [] vCookieFiles = Directory.GetFiles("E:\\secured", "*@*g****e*");
                foreach (String vCookieFile in vCookieFiles) 
                {
                  //ToDo
                    String vTheCookieFile = vCookieFile;
                }

                */

                CreateCookies();

                // To DO read in Cookies
                mHandleCookies = true;
                EarthEngines.SetCookiesEngines(mCookies, true);
                CookieLabel.Text = "Cookies On";
                CookieLabel.Refresh();
            }
            else
            {
                mHandleCookies = false;
                EarthEngines.SetCookiesEngines(mCookies, false);
                CookieLabel.Text = "Cookies Off";
                CookieLabel.Refresh();
            }
        }


        void SetLastTileInfoToDisplayCenter()
        {
            Int64 vDisplayAreaCodeX = EarthMath.GetAreaCodeX(mDisplayCenterLongitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeY = EarthMath.GetAreaCodeY(mDisplayCenterLatitude, EarthConfig.mZoomLevel);
            mLastTileInfo = new TileInfo(vDisplayAreaCodeX, vDisplayAreaCodeY, EarthConfig.mZoomLevel, EarthConfig.mSelectedService,false);
        }
           
 

        void HandleHarvestedAreaTile(Tile iTile)
        {
            if (mAreaTilesInfoDownloadCheckList.Exists(iTile.mTileInfo.Equals)) //We can still get double and foreign Tiles from the Display request that are just processed in the Engines also when we emptied the queues
            {

                Int64 vAreaCodeXBorder = EarthMath.GetAreaCodeSize(EarthConfig.mFetchLevel);

                Int64 vAreaCodeX = iTile.mTileInfo.mAreaCodeX;
                Int64 vAreaCodeY = iTile.mTileInfo.mAreaCodeY;

                Int64 vCountX = iTile.mTileInfo.mAreaCodeX - mEarthArea.AreaCodeXStart;
                Int64 vCountY = iTile.mTileInfo.mAreaCodeY - mEarthArea.AreaCodeYStart;

                //Border overrun?
                if (mEarthArea.AreaCodeXStart > mEarthArea.AreaCodeXStop)
                {
                    if (iTile.mTileInfo.mAreaCodeX < mEarthArea.AreaCodeXStart)
                    {
                        vCountX = (iTile.mTileInfo.mAreaCodeX + vAreaCodeXBorder) - mEarthArea.AreaCodeXStart;
                    }
                }

                //Always draw first downloaded tile regardless if good or no (not = NoTileFound bitmap)
                if ((iTile.IsGoodBitmap()) || (mAreaDownloadTilesCountIncludingFailed == 0))
                {
                    String VMapCode = EarthScriptsHandler.MapAreaCoordToTileCode(vAreaCodeX, vAreaCodeY, EarthConfig.mFetchLevel, "ABCD");
                    SetStatus(VMapCode);
                }


                mAreaDownloadTilesCountIncludingFailed++;
                if (mAreaDownloadTilesCountIncludingFailed == mEarthArea.FetchTilesTotal + 1)
                {
                    mAreaDownloadRepetitions = 1;
                }

                if (iTile.IsGoodBitmap())
                {

                    mAreaDownloadTilesCount++;
                    mCurrentDownloadedTilesTotal++;
                    UpdateAreaAndTileCountStatusLabel();
                    TileCountBox.Invalidate();
                    TileCountBox.Text = Convert.ToString(mAreaDownloadTilesCount);
                    TileCountBox.Refresh();
                    ProgressBox.Invalidate();
                    ProgressBox.Value = (Int32)(Math.Round(Convert.ToDouble(100.0 * mAreaDownloadTilesCount) / Convert.ToDouble(mEarthArea.FetchTilesTotal)));
                    ProgressBox.Refresh();

                    if (mAreaDownloadRepetitions > 0)
                    {
                        mAreaDownloadTilesMiss--;
                        MissingTilesBox.Invalidate();
                        MissingTilesBox.Text = Convert.ToString(mAreaDownloadTilesMiss);
                        MissingTilesBox.Refresh();
                    }
                }
                else
                {
                    if (mAreaDownloadRepetitions == 0)
                    {
                        mAreaDownloadTilesMiss++;
                        MissingTilesBox.Invalidate();
                        MissingTilesBox.Text = Convert.ToString(mAreaDownloadTilesMiss);
                        MissingTilesBox.Refresh();
                    }

                    mTileInfoAreaQueue.AddTileInfo(iTile.mTileInfo); //Queue again
                }

                mEarthAreaTexture.DrawTile(iTile, (Int32)vCountX * 256 - (Int32)mEarthArea.XPixelPosAreaStart, (Int32)vCountY * 256 - (Int32)mEarthArea.YPixelPosAreaStart);

                if (iTile.IsGoodBitmap())
                {
                    mAreaTilesInfoDownloadCheckList.RemoveAll(iTile.mTileInfo.Equals);
                }


                if (mAreaTilesInfoDownloadCheckList.Count == 0)
                {

                    mLastDownloadProcessTileMisses = mAreaDownloadTilesMiss;
                    
                    if (mEarthWeb != null)
                    {
                        mEarthWeb.ResetPanicMode(); //Always Reset The Panic Mode
                    }

                    //Start Area after Download processing (Texture undistortion / Scenery compiling etc)
                    if (mDebugMode)
                    {
                        //!!! For debug we do not start the after work thread but call the methode direct. 
                        //He will block at the end of that processind. (waits for ever on the thread exit of the thread that was never started) 
                        AreaAfterDownloadProcessing();
                    }
                    else
                    {
                        mAreaAftermathThread.Start();
                    }


                }
            }

            if (mStopProcess)
            {

                mAreaTilesInfoDownloadCheckList.Clear();
                EmptyAllJobQueues();

                mEarthAreaTexture.FreeTextureMemory();

                SetVisibilityForIdle();

                mAreaProcessRunning = false;
                mStopProcess = false;

                CalculateAreaCoords(); //HandleInputChange Resets any TimeOutBlock so we don't call that here

            }

        }

        void UpdateAreaAndTileCountStatusLabel()
        {
            String vStatusLabel = "";

            if (mMultiAreaMode)
            {
                vStatusLabel = "Area " + Convert.ToString(mCurrentActiveAreaNr) + " of " + Convert.ToString(mEarthMultiArea.AreaTotal) + "   Tile " + Convert.ToString(mCurrentDownloadedTilesTotal) + " of " + Convert.ToString(mEarthMultiArea.FetchTilesMultiAreaTotal);
            }
            else
            {
                vStatusLabel = "Area " + Convert.ToString(mCurrentActiveAreaNr) + " of " + Convert.ToString(1) + "   Tile " + Convert.ToString(mCurrentDownloadedTilesTotal) + " of " + Convert.ToString(mEarthArea.FetchTilesTotal);
            }
            AreaAndTileCountStatusLabel.Invalidate();
            AreaAndTileCountStatusLabel.Text = vStatusLabel;
            AreaAndTileCountStatusLabel.Refresh();
        }

        Int64 GetDeltaTickCount(Int32 iStoredTickCount)
        {
            Int32 vActualTick = Environment.TickCount;
            Int64 vDeltaTickCount;

            //TickCount is an overruning Int32 in the Range from Int32.MinValue to Int32.MaxValue
            vDeltaTickCount = (Int64)vActualTick - (Int64)iStoredTickCount;

            //Overrun?
            if (vDeltaTickCount < 0)
            {
                //Normal
                vDeltaTickCount += ((Int64)UInt32.MaxValue + 1);
            }


            return vDeltaTickCount;
        }

        Boolean IsTimeOut()
        {
            Boolean vIsTimeOut = false;
            if (mTileFailedTimeOutCountAktive)
            {
                Int64 vMilliSecondsPassed = GetDeltaTickCount(mTileFailedStartTickCount);
                if (vMilliSecondsPassed >= (1000 * EarthConfig.mDownloadFailTimeOut))
                {
                    vIsTimeOut = true;
                }
            }
            return vIsTimeOut;
        }

        void SetTimeOutCounterIfNotAlreadySet()
        {
            if (!mTileFailedTimeOutCountAktive)
            {
                mTileFailedStartTickCount = Environment.TickCount;
                mTileFailedTimeOutCountAktive = true;
            }
        }

        void ResetTimeOutCounter()
        {
            mTileFailedTimeOutCountAktive = false;
            mTimeOutBlock = false;
        }

        void CheckAndHandleSuspiciousTile(Tile xTile)
        {
            Boolean vRemoveFromSuspiciousList = false;

            if (xTile.IsSuspicious()&&!xTile.mTileInfo.mSkipTile )
            {
                if (mSuspiciousList1.Exists(xTile.mTileInfo.Equals))
                {
                    if (mSuspiciousList2.Exists(xTile.mTileInfo.Equals))
                    {
                        vRemoveFromSuspiciousList = true;
                    }
                    else
                    {
                        mSuspiciousList2.Add(xTile.mTileInfo);
                        xTile.MarkAsBadBitmap();
                    }
                }
                else
                {
                    mSuspiciousList1.Add(xTile.mTileInfo);
                    xTile.MarkAsBadBitmap();
                }
            }
            else
            {
                vRemoveFromSuspiciousList = true;
            }

            if (vRemoveFromSuspiciousList)
            {
                //RemoveFromSuspectious Lists
                if (mSuspiciousList1.Exists(xTile.mTileInfo.Equals))
                {
                    TileInfo vTileInfoToRemove = mSuspiciousList1.Find(xTile.mTileInfo.Equals);
                    mSuspiciousList1.Remove(vTileInfoToRemove);
                }
                if (mSuspiciousList2.Exists(xTile.mTileInfo.Equals))
                {
                    TileInfo vTileInfoToRemove = mSuspiciousList2.Find(xTile.mTileInfo.Equals);
                    mSuspiciousList2.Remove(vTileInfoToRemove);
                }
            }
            
            //Debug
            //DlQueueLabel.Invalidate();
            //DlQueueLabel.Text = Convert.ToString(mSuspiciousList1.Count) + "  -  " + Convert.ToString(mSuspiciousList2.Count);
            //DlQueueLabel.Refresh();
        }


        void HarvestThreadEnginesOutput()
        {
            mWorkFeedEnginesMutex.WaitOne();

            String vEngineStatusFeedback = "";
            Tile vTileOfEngine = new Tile();
            Tile vTileOfWWWEngine = new Tile();

            Boolean vThereIsATileFromEngine   = false;
            List<Boolean> vThereIsATileFromEngineNumber = Enumerable.Repeat(false, mEngineThreads.Count).ToList();
            List<Tile> vTilesOfEngines = new List<Tile>(mEngineThreads.Count);
            Boolean vThereIsATileFromWWWEngine = false;

            for (int i = 0; i < mEngineThreads.Count; i++)
            {
                bool tileFromEngine = EarthEngines.CheckForTileOfEngine(i);
                // make sure at least 1 tile from engine
                if (tileFromEngine && !vThereIsATileFromEngine)
                {
                    vThereIsATileFromEngine = tileFromEngine;
                }

                if (tileFromEngine)
                {
                    vTileOfEngine = EarthEngines.GetTileOfEngine(i);
                    vTilesOfEngines.Add(vTileOfEngine);
                    mLastTileInfo = vTileOfEngine.mTileInfo.Clone();

                    if (mAreaProcessRunning)
                    {

                            CheckAndHandleSuspiciousTile(vTileOfEngine);

                        HandleHarvestedAreaTile(vTileOfEngine);
                    }
                    else
                    {
                        if (vTileOfEngine.IsGoodBitmap())
                        {
                            if (!mDisplayTileCache.IsTileInCache(vTileOfEngine.mTileInfo))
                            {
                                mDisplayTileCache.AddTileOverwriteOldTiles(vTileOfEngine);
                            }
                        }
                    }
                }
            }


            if (mEarthWeb != null)
            {
               vThereIsATileFromWWWEngine = mEarthWeb.CheckForTileOfWWWEngine();
            }

            if (vThereIsATileFromWWWEngine)
            {
                vTileOfWWWEngine = mEarthWeb.GetTileOfWWWEngine();

                mLastTileInfo = vTileOfWWWEngine.mTileInfo.Clone();

                if (mAreaProcessRunning)
                {
                    CheckAndHandleSuspiciousTile(vTileOfWWWEngine);
                    HandleHarvestedAreaTile(vTileOfWWWEngine);
                }
                else
                {
                    if (vTileOfWWWEngine.IsGoodBitmap())
                    {
                        if (!mDisplayTileCache.IsTileInCache(vTileOfWWWEngine.mTileInfo))
                        {
                            mDisplayTileCache.AddTileOverwriteOldTiles(vTileOfWWWEngine);
                        }
                    }
                }
            }

            //Selfhealing. Were there any tiles lost? Should never happen and never happend on my system but someone reported FSET blockings on his system 
            if ((mAreaProcessRunning) && (mAreaTilesInfoDownloadCheckList.Count != 0))
            {
                //ChgeckList isn't empty
                if (mTileInfoAreaQueue.IsEmpty())
                {
                    //But the Tile Order FIFO is.
                    Boolean vAtLeastOneTileInAnyQueueOrEngine = false;

                    //Check if Web Engine is Empty 
                    if (mEarthWeb != null)
                    {
                        vAtLeastOneTileInAnyQueueOrEngine = mEarthWeb.CheckForTileOrdered() || mEarthWeb.CheckForTileOfWWWEngine();
                    }
                    if (!vAtLeastOneTileInAnyQueueOrEngine)
                    {
                        vAtLeastOneTileInAnyQueueOrEngine = false;
                        for (int i = 0; i < mEngineThreads.Count; i++)
                        {
                            if (!EarthEngines.IsEngineTileFree(i))
                            {
                                vAtLeastOneTileInAnyQueueOrEngine = true;
                                break;
                            }
                        }
                    }
                    if (!vAtLeastOneTileInAnyQueueOrEngine)
                    {
                        //Text = mTitle + "  <!> Blocking catched: " + Convert.ToString(mAreaTilesInfoDownloadCheckList.Count) + " lost Tiles";
                        //Refresh();
                        SetStatus("EXCEPTION <!> Blocking catched: " + Convert.ToString(mAreaTilesInfoDownloadCheckList.Count) + " lost Tiles detected!");
                        Thread.Sleep(3000);
                        SetStatus(" ...writeing debug information file into work folder...");
                        Thread.Sleep(1000);
                        WriteDebugFile();
                        SetStatus(" ...selfhealing in progress..");
                        Thread.Sleep(2000);
                        foreach (TileInfo vTileInfo in mAreaTilesInfoDownloadCheckList)
                        {
                            mTileInfoAreaQueue.AddTileInfo(vTileInfo);
                        }
                    }
                }
            }


            if (mAreaProcessRunning)
            {
                //Area Download
                FeedThreadEnginesWithNewWork();
            }
            else
            {
                //Display
                if ((vThereIsATileFromEngine) || (vThereIsATileFromWWWEngine))
                {
                    for (int i = 0; i < mEngineThreads.Count; i++)
                    {
                        Boolean vThereIsATileFromThisEngine = vThereIsATileFromEngineNumber[i];
                        if (vThereIsATileFromThisEngine)
                        {
                            Tile vTileOfCurEngine = vTilesOfEngines[i];
                            if (mToEngineDelegatedTilesControllList1.Exists(vTileOfCurEngine.mTileInfo.Equals))
                            {
                                TileInfo vTileInfoToRemove = mToEngineDelegatedTilesControllList1.Find(vTileOfCurEngine.mTileInfo.Equals);
                                mToEngineDelegatedTilesControllList1.Remove(vTileInfoToRemove);
                            }
                            if (mToEngineDelegatedTilesControllList2.Exists(vTileOfCurEngine.mTileInfo.Equals))
                            {
                                TileInfo vTileInfoToRemove = mToEngineDelegatedTilesControllList2.Find(vTileOfCurEngine.mTileInfo.Equals);
                                mToEngineDelegatedTilesControllList2.Remove(vTileInfoToRemove);
                            }
                        }
                    }
                    if (vThereIsATileFromWWWEngine)
                    {
                        if (mToEngineDelegatedTilesControllList1.Exists(vTileOfWWWEngine.mTileInfo.Equals))
                        {
                            TileInfo vTileInfoToRemoveWWW = mToEngineDelegatedTilesControllList1.Find(vTileOfWWWEngine.mTileInfo.Equals);
                            mToEngineDelegatedTilesControllList1.Remove(vTileInfoToRemoveWWW);
                        }
                        if (mToEngineDelegatedTilesControllList2.Exists(vTileOfWWWEngine.mTileInfo.Equals))
                        {
                            TileInfo vTileInfoToRemoveWWW = mToEngineDelegatedTilesControllList2.Find(vTileOfWWWEngine.mTileInfo.Equals);
                            mToEngineDelegatedTilesControllList2.Remove(vTileInfoToRemoveWWW);
                        }
                    }
                    
                    DisplayWhereYouJustAreAgain();
                }

            }

            //for debugging
            //SetStatus(Convert.ToString(mTileInfoDisplayQueue.GetEntriesCount()) + "  -  " + Convert.ToString(mToEngineDelegatedTilesControllList1.Count) + "  -  " + Convert.ToString(mToEngineDelegatedTilesControllList2.Count));

            vEngineStatusFeedback = EarthEngines.GetStatusFromEngines();

            if (!mTimeOutBlock)
            {
                if (vEngineStatusFeedback.Length > 0)
                {
                    SetStatus(vEngineStatusFeedback);
                    Thread.Sleep(500); //Else user sees nothing ok we need a time check instead blocking with sleep -> ToDo
                }
            }

            //TimeOutHandling and free bitmap
            for (int i = 0; i < mEngineThreads.Count; i++)
            {
                if (vThereIsATileFromEngineNumber[i])
                {
                    if (vTilesOfEngines[i].IsGoodBitmap())
                    {
                        ResetTimeOutCounter();
                    }
                    else
                    {
                        SetTimeOutCounterIfNotAlreadySet();
                    }
                    vTilesOfEngines[i].FreeBitmap();
                }
            }
            if (vThereIsATileFromWWWEngine)
            {
                if (vTileOfWWWEngine.IsGoodBitmap())
                {
                    ResetTimeOutCounter();
                }
                else
                {
                    SetTimeOutCounterIfNotAlreadySet();
                }
                vTileOfWWWEngine.FreeBitmap();
            }
            if (IsTimeOut())
            {
                if (!mTimeOutBlock)
                {
                    mTimeOutBlock = true;

                    mAllowDisplayToSetStatus = false;
                    if (mAreaProcessRunning)
                    {
                        mStopProcess = true;
                    }

                    SetStatus("Time Out limit reached (DownloadFailTimeOut).  YOUR CONNECTION HAS BROKEN. Stopping all processes");
                    Thread.Sleep(1000);

                }
            }

            mWorkFeedEnginesMutex.ReleaseMutex();
        }


        void EmptyAllJobQueues()
        {
            mWorkFeedEnginesMutex.WaitOne();
            mTileInfoDisplayQueue.DoEmpty();
            mToEngineDelegatedTilesControllList1.Clear();
            mToEngineDelegatedTilesControllList2.Clear();
            mSuspiciousList1.Clear();
            mSuspiciousList2.Clear();
            EarthEngines.EmptyEnginesQueue();
            mWorkFeedEnginesMutex.ReleaseMutex();
        }


        void FeedThreadEnginesWithNewWork()
        {

            mWorkFeedEnginesMutex.WaitOne();

            List<Int32> vFreeEngines = new List<int>(mEngineThreads.Count);
            for (int i = 0; i < mEngineThreads.Count; i++)
            {
                vFreeEngines.Add(0);
            }
            Int32 vFreeWWWEngine  = 0;
            Int32 vTotalFreeSlots = 0;

            Boolean mIsWebEngine = (mEarthWeb != null);

            if (mIsWebEngine)
            {
                mIsWebEngine = mEarthWeb.Created;
            }

            for (int i = 0; i < vFreeEngines.Count; i++)
            {
                vFreeEngines[i] = EarthEngines.GetFreeSpaceOfEngine(i);
            }

            if (mIsWebEngine)
            {
                vFreeWWWEngine = mEarthWeb.GetFreeSpaceOfWWWEngine();
            }

            //DoWeHaveWorkPending?

            Boolean vWeHaveWork = (((!mAreaProcessRunning) && (!mTileInfoDisplayQueue.IsEmpty())) ||
                                    ((mAreaProcessRunning) && (!mTileInfoAreaQueue.IsEmpty())));

            Int64 vTilesWeMayPut = 1000000; //The numbers of tiles we may put into the queue this time. ->a lot ;)

            if (mAreaProcessRunning)
            {
                Int64 vMilliSecondsPassed = GetDeltaTickCount(mAreaTilePushTick);
                vTilesWeMayPut = Convert.ToInt64(Math.Truncate(EarthConfig.mMaxDownloadSpeed * 0.001 * vMilliSecondsPassed));
                if (vTilesWeMayPut > 0)
                {
                    mAreaTilePushTick = Environment.TickCount;
                }
            }

            if ((vWeHaveWork) && (!mTimeOutBlock) && (vTilesWeMayPut > 0))
            {

                foreach (Int32 vFreeEngine in vFreeEngines)
                {
                    vTotalFreeSlots += vFreeEngine;
                }

                if (mIsWebEngine)
                {
                    vTotalFreeSlots = vFreeWWWEngine;
                }

                while ((vWeHaveWork) && (vTotalFreeSlots > 0) && (vTilesWeMayPut > 0))
                {
                    //Find out the most Empty Queue and choose random Engine if multiple with equal free slots
                    //= efficient work load balancing

                    TileInfo vTileInfo;
                    Boolean vTileIsInControllList1;
                    Boolean vTileIsInControllList2;

                    if (mAreaProcessRunning)
                    {
                        vTileInfo = mTileInfoAreaQueue.GetNextTileInfoClone();
                        vTileIsInControllList1 = false; //No job control list for Area download.  Can not be used here.
                        vTileIsInControllList2 = false;
                    }
                    else
                    {
                        vTileInfo = mTileInfoDisplayQueue.GetNextTileInfoClone();
                        vTileIsInControllList1 = mToEngineDelegatedTilesControllList1.Exists(vTileInfo.Equals);
                        vTileIsInControllList2 = mToEngineDelegatedTilesControllList2.Exists(vTileInfo.Equals);
                    }

                    //queue the tile if AllowMultipleTileRequest or NotAllowed And NotAlreadySheduled)
                    if ((!vTileIsInControllList1) ||
                        (mAllowTileDoubleRequest) && (!vTileIsInControllList2))
                    {
                        if (!vTileIsInControllList1)
                        {
                            if (!mAreaProcessRunning) //No job control list for Area download.
                            {
                                mToEngineDelegatedTilesControllList1.Add(vTileInfo);

                                if ((mIsWebEngine) && (!vTileIsInControllList2)) //In Case of Web also add in second to surpress double request (disturbing in case of WebBrowser, user cosmetic)
                                {
                                    mToEngineDelegatedTilesControllList2.Add(vTileInfo);
                                }
                            }
                        }
                        else
                        {
                            mToEngineDelegatedTilesControllList2.Add(vTileInfo);
                        }


                        Int32 vMaxFreeSlots = 0;
                        Int32 vNrOfEnginesWithMaxFreeSlots = 0;
                        Int32[] vEngineArray = new Int32[mEngineThreads.Count + 1];
                        for (int i = 0; i < vEngineArray.Length; i++)
                        {
                            vEngineArray[i] = 0;
                        }

                        Int32 vWinnerEngine = 0;

                        foreach (Int32 vFreeEngine in vFreeEngines)
                        {
                            if (!mIsWebEngine && (vFreeEngine > vMaxFreeSlots)) { vMaxFreeSlots = vFreeEngine; };
                        }
                        if (mIsWebEngine && (vFreeWWWEngine > vMaxFreeSlots)) { vMaxFreeSlots = vFreeWWWEngine; };
                        for (int i = 0; i < vFreeEngines.Count; i++)
                        {
                            if (!mIsWebEngine && (vFreeEngines[i] >= vMaxFreeSlots)) { vEngineArray[vNrOfEnginesWithMaxFreeSlots] = i + 1; vNrOfEnginesWithMaxFreeSlots++; };
                        }
                        if (mIsWebEngine && (vFreeWWWEngine >= vMaxFreeSlots)) { vEngineArray[vNrOfEnginesWithMaxFreeSlots] = vFreeEngines.Count + 1; vNrOfEnginesWithMaxFreeSlots++; };

                        Int32 vRandom = mRandomGenerator.Next(1, vNrOfEnginesWithMaxFreeSlots + 1); //a Randomnumber between 1 and vNrOfEnginesWithMaxFreeSlots


                        //And the Winner Engine is...tata
                        vWinnerEngine = vEngineArray[vRandom - 1] - 1;


                        if ((mIsWebEngine) && (vWinnerEngine!=vFreeEngines.Count))
                        {
                            Thread.Sleep(10); //then Error
                        }

                        for (int i = 0; i < vFreeEngines.Count; i++)
                        {
                            if (vWinnerEngine == i)
                            {
                                vFreeEngines[i] = EarthEngines.AddTileInfoToEngine(i, vTileInfo);
                            }
                        }
                        if (vWinnerEngine == vFreeEngines.Count)
                        {
                            vFreeWWWEngine = mEarthWeb.AddTileInfoToWWWEngine(vTileInfo);
                        }
                        vTilesWeMayPut--;
                        vTotalFreeSlots = 0;
                        foreach (Int32 vFreeEngine in vFreeEngines)
                        {
                            vTotalFreeSlots += vFreeEngine;
                        }

                        if (mIsWebEngine)
                        {
                            vTotalFreeSlots = vFreeWWWEngine;
                        }

                    }

                    vWeHaveWork = (((!mAreaProcessRunning) && (!mTileInfoDisplayQueue.IsEmpty())) ||
                                   ((mAreaProcessRunning) && (!mTileInfoAreaQueue.IsEmpty())));
                }

            }

            Int32 vEnginesQueueSize = EarthEngines.GetEnginesQueueSize();

            ProgressQueue1Box.Invalidate();
            ProgressQueue1Box.Value = ((100 * (vEnginesQueueSize - vFreeEngines[0])) / vEnginesQueueSize);
            ProgressQueue1Box.Refresh();

            ProgressQueue2Box.Invalidate();
            ProgressQueue2Box.Value = ((100 * (vEnginesQueueSize - vFreeEngines[1])) / vEnginesQueueSize);
            ProgressQueue2Box.Refresh();

            ProgressQueue3Box.Invalidate();
            ProgressQueue3Box.Value = ((100 * (vEnginesQueueSize - vFreeEngines[2])) / vEnginesQueueSize);
            ProgressQueue3Box.Refresh();

            ProgressQueue4Box.Invalidate();
            ProgressQueue4Box.Value = ((100 * (vEnginesQueueSize - vFreeEngines[3])) / vEnginesQueueSize);
            ProgressQueue4Box.Refresh();

            mWorkFeedEnginesMutex.ReleaseMutex();
        }



        String FixFormatPicCoord(Int64 iValue)
        {
            //MaxValue ca. 220'000;
            String vCoord = "";
            if (iValue < 100000)
            {
                vCoord += "0";
            }
            if (iValue < 10000)
            {
                vCoord += "0";
            }
            if (iValue < 1000)
            {
                vCoord += "0";
            }
            if (iValue < 100)
            {
                vCoord += "0";
            }
            if (iValue < 10)
            {
                vCoord += "0";
            }
            vCoord += Convert.ToString(iValue);
            return vCoord;
        }



        void HandleEnableDisableLongLatInputFields()
        {
            //Prepare GUI Enable/Disable
            if (EarthCommon.StringCompare(AreaDefModeBox.Text, "2Points"))
            {
                //2 Points
                CenterLatLabel.Visible = false;
                CenterLongLabel.Visible = false;
                CenterGroup.Visible = false;
                AreaSizeXBox.Enabled = false;
                AreaSizeYBox.Enabled = false;
                NWCornerGroup.Visible = true;
                SECornerGroup.Visible = true;
                NWLatLabel.Visible = true;
                NWLongLabel.Visible = true;
                SELatLabel.Visible = true;
                SELongLabel.Visible = true;
                HalfLeftButton.Visible = true;
                HalfRightButton.Visible = true;
                HalfUpButton.Visible = true;
                HalfDownButton.Visible = true;
                HalveLabel.Visible = true;
            }
            else
            {
                //default 1 Point
                HalfLeftButton.Visible = false;
                HalfRightButton.Visible = false;
                HalfUpButton.Visible = false;
                HalfDownButton.Visible = false;
                HalveLabel.Visible = false;
                NWCornerGroup.Visible = false;
                SECornerGroup.Visible = false;
                NWLatLabel.Visible = false;
                NWLongLabel.Visible = false;
                SELatLabel.Visible = false;
                SELongLabel.Visible = false;
                CenterGroup.Visible = true;
                CenterLatLabel.Visible = true;
                CenterLongLabel.Visible = true;
                AreaSizeXBox.Enabled = true;
                AreaSizeYBox.Enabled = true;
            }
        }


        void DisplayPicture(Bitmap iDisplayBmp)
        {
            NormalPictureBox.Invalidate();
            NormalPictureBox.Image = (Bitmap)iDisplayBmp.Clone();
            NormalPictureBox.Refresh();
        }

        String GetFixSpaceWorldCoordGrad(Double iWorldCoord)
        {
            String vGrad = EarthMath.GetWorldCoordGrad(iWorldCoord);
            while (vGrad.Length < 3)
            {
                vGrad = "0" + vGrad;
            }
            return vGrad;
        }

        String GetFixSpaceWorldCoordMinute(Double iWorldCoord)
        {
            String vMinute = EarthMath.GetWorldCoordMinute(iWorldCoord);
            while (vMinute.Length < 2)
            {
                vMinute = "0" + vMinute;
            }
            return vMinute;
        }

        String GetFixSpaceWorldCoordSec(Double iWorldCoord)
        {
            //Works for Seconds tounded to 100th second only 
            String vSeconds = Convert.ToString(EarthMath.GetWorldCoordSecInDouble(iWorldCoord), NumberFormatInfo.InvariantInfo);
            Int32 vIndex = vSeconds.IndexOf(".", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                //we have a '.'
                if (vIndex == 0)
                {
                    vSeconds = "00" + vSeconds;
                }
                if (vIndex == 1)
                {
                    vSeconds = "0" + vSeconds;
                }
                while (vSeconds.Length < 5)
                {
                    vSeconds = vSeconds + "0";
                }
            }
            else
            {
                //No .
                vSeconds = vSeconds + ".00";
                while (vSeconds.Length < 5)
                {
                    vSeconds = "0" + vSeconds;
                }
            }

            vSeconds = vSeconds.Remove(2, 1); //FSX seems alergic so cut out the '.' and make a block
            return vSeconds;
        }



        String GetAreaFileString()
        {
            String vAreaFileName;

            if (!EarthConfig.mUseInformativeAreaNames)
            {
                vAreaFileName = "";
            }
            else
            {
                //Example:
                //Area_Lm3_SnapOff_N048594237_N048593649_E008235579_E008240475.bmp, so it can be sorted well in the directory
                //FSX doesnt load .bgl's that were troubless compiled with but had more complicated chars in the names like a ' ... so back to basics

                if (EarthConfig.mFetchLevel >= 0)
                {
                    vAreaFileName = "_Lp";
                    vAreaFileName += Convert.ToString(EarthConfig.mFetchLevel);
                }
                else
                {
                    vAreaFileName = "_Lm";
                    vAreaFileName += Convert.ToString(-EarthConfig.mFetchLevel);
                }
                vAreaFileName += "_Snap" + EarthConfig.GetAreaSnapString();
                vAreaFileName += "_" + EarthMath.GetSignLatitude(mEarthArea.AreaSnapStartLatitude);

                vAreaFileName += GetFixSpaceWorldCoordGrad(mEarthArea.AreaSnapStartLatitude);
                vAreaFileName += GetFixSpaceWorldCoordMinute(mEarthArea.AreaSnapStartLatitude);
                vAreaFileName += GetFixSpaceWorldCoordSec(mEarthArea.AreaSnapStartLatitude);
                vAreaFileName += "_" + EarthMath.GetSignLatitude(mEarthArea.AreaSnapStopLatitude);
                vAreaFileName += GetFixSpaceWorldCoordGrad(mEarthArea.AreaSnapStopLatitude);
                vAreaFileName += GetFixSpaceWorldCoordMinute(mEarthArea.AreaSnapStopLatitude);
                vAreaFileName += GetFixSpaceWorldCoordSec(mEarthArea.AreaSnapStopLatitude);
                vAreaFileName += "_" + EarthMath.GetSignLongitude(mEarthArea.AreaSnapStartLongitude);
                vAreaFileName += GetFixSpaceWorldCoordGrad(mEarthArea.AreaSnapStartLongitude);
                vAreaFileName += GetFixSpaceWorldCoordMinute(mEarthArea.AreaSnapStartLongitude);
                vAreaFileName += GetFixSpaceWorldCoordSec(mEarthArea.AreaSnapStartLongitude);
                vAreaFileName += "_" + EarthMath.GetSignLongitude(mEarthArea.AreaSnapStopLongitude);
                vAreaFileName += GetFixSpaceWorldCoordGrad(mEarthArea.AreaSnapStopLongitude);
                vAreaFileName += GetFixSpaceWorldCoordMinute(mEarthArea.AreaSnapStopLongitude);
                vAreaFileName += GetFixSpaceWorldCoordSec(mEarthArea.AreaSnapStopLongitude);

            }
            return vAreaFileName;
        }



        void QueueMultiAreaAreas()
        {
            if (mMultiAreaMode)
            {
                mAreaInfoAreaQueue.DoEmpty();

                Int64 vMultix = mEarthMultiArea.AreaCountInX;
                Int64 vMultiy = mEarthMultiArea.AreaCountInY;

                Int64 vAreaPosX = 0;
                Int64 vAreaPosY = 0;
                AreaInfo vAreaInfo = new AreaInfo(0, 0);
                EarthArea vEarthArea = new EarthArea();

                AreaInfo vLargestAreaInfo = new AreaInfo(0, 0);
                Int64 vPixelTotal = 0;

                //Check for Memory by find out and allocating the largest
                Boolean vAllocationOk = false;

                for (vAreaPosY = 0; vAreaPosY < vMultiy; vAreaPosY++)
                {
                    for (vAreaPosX = 0; vAreaPosX < vMultix; vAreaPosX++)
                    {
                        vAreaInfo = new AreaInfo(vAreaPosX, vAreaPosY);
                        vEarthArea = mEarthMultiArea.CalculateSingleAreaFormMultiArea(vAreaInfo, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                        Int64 vTotalPixel = vEarthArea.AreaPixelsInX * vEarthArea.AreaPixelsInY;
                        if (vTotalPixel > vPixelTotal)
                        {
                            vPixelTotal = vTotalPixel;
                            vLargestAreaInfo = vAreaInfo.Clone();
                        }
                    }
                }

                vEarthArea = mEarthMultiArea.CalculateSingleAreaFormMultiArea(vLargestAreaInfo, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                SetStatus(" Testing Memory Allocation .. ");
                Thread.Sleep(1000);

                if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                {
                    vAllocationOk = mEarthAreaTexture.TestMemoryAllocation(vEarthArea, tMemoryAllocTestMode.eResampler, "Reference Area", this);
                }
                else
                {
                    vAllocationOk = mEarthAreaTexture.TestMemoryAllocation(vEarthArea, tMemoryAllocTestMode.eNormal, "Reference Area", this);
                }


                if (!vAllocationOk)
                {
                    mAreaProcessRunning = false;
                    mStopProcess = false;
                    HandleInputChange();
                }
                else
                {
                    //Fine we can go on

                    mAreaInfoAreaQueue = new AreaInfoFIFOQueue((Int32)vMultix * (Int32)vMultiy);

                    for (vAreaPosY = 0; vAreaPosY < vMultiy; vAreaPosY++)
                    {
                        for (vAreaPosX = 0; vAreaPosX < vMultix; vAreaPosX++)
                        {
                            vAreaInfo = new AreaInfo(vAreaPosX, vAreaPosY);
                            if (EarthConfig.mShuffleAreasForDownload)
                            {
                                mAreaInfoAreaQueue.AddAreaInfoRandomPosition(vAreaInfo);
                            }
                            else
                            {
                                mAreaInfoAreaQueue.AddAreaInfo(vAreaInfo);
                            }
                        }
                    }
                }
            }
        }

        void CreateNextSingleAreaFromMultiArea()
        {
            Int64 Count;
            if (mMultiAreaMode)
            {
                mCurrentAreaInfo = mAreaInfoAreaQueue.GetNextAreaInfoClone();

                //That's right we override the Main EarthArea so we don't have to care about a special separate object handling
                //The information can be restored at the end of all download processes
                mLastMeshCreatedEarthArea = mEarthArea;
                mLastCheckedForAllWaterEarthArea = mEarthArea;
                mEarthArea = mEarthMultiArea.CalculateSingleAreaFormMultiArea(mCurrentAreaInfo, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                mEarthArea.mBlendNorthBorder = false;
                mEarthArea.mBlendEastBorder  = false;
                mEarthArea.mBlendSouthBorder = false;
                mEarthArea.mBlendWestBorder  = false;
                
                if (EarthConfig.mBlendBorders)
                {
                    if (mCurrentAreaInfo.mAreaNrInX == 0)
                    {
                        mEarthArea.mBlendWestBorder = true;
                    }
                    if (mCurrentAreaInfo.mAreaNrInY == 0)
                    {
                        mEarthArea.mBlendNorthBorder = true;
                    }
                    if (mCurrentAreaInfo.mAreaNrInX == (mEarthMultiArea.AreaCountInX - 1))
                    {
                        mEarthArea.mBlendEastBorder = true;
                    }
                    if (mCurrentAreaInfo.mAreaNrInY == (mEarthMultiArea.AreaCountInY - 1))
                    {
                        mEarthArea.mBlendSouthBorder = true;
                    }
                }

                mCurrentActiveAreaNr = mEarthMultiArea.AreaTotal - mAreaInfoAreaQueue.GetEntriesCount();
                AreaCountBox.Invalidate();
                AreaCountBox.Text = Convert.ToString(mCurrentActiveAreaNr);
                AreaCountBox.Refresh();

            }
            else
            {
                if (EarthConfig.mBlendBorders)
                {
                    mEarthArea.mBlendNorthBorder = true;
                    mEarthArea.mBlendEastBorder  = true;
                    mEarthArea.mBlendSouthBorder = true;
                    mEarthArea.mBlendWestBorder  = true;
                }
            }

        }


        void QueueAreaTiles()
        {
            try
            {
                Boolean vAllocationOk = true;

                mLastDownloadProcessTileMisses = 0;

                mDisplayTileCache.EmptyTileCache(); //Free RAM from balast

                Focus();
                //Application.DoEvents();

                mAreaTilesInfoDownloadCheckList.Clear();   //ok we already cleared before calling this methode and it's empty before that anyway..but lets code safe

                if (!mMultiAreaMode)
                {
                    if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                    {
                        vAllocationOk = mEarthAreaTexture.TestMemoryAllocation(mEarthArea, tMemoryAllocTestMode.eResampler, "Area", this);
                    }
                    else
                    {
                        vAllocationOk = mEarthAreaTexture.TestMemoryAllocation(mEarthArea, tMemoryAllocTestMode.eNormal, "Area", this);
                    }
                }

                if (!vAllocationOk)
                {
                    mAreaProcessRunning = false;
                    mStopProcess = false;
                    //mSkipAreaProcessFlag = true;
                    HandleInputChange();
                }
                else
                {
                    vAllocationOk = mEarthAreaTexture.AllocateTextureMemory(mEarthArea);

                    if (!vAllocationOk)
                    {
                        mAreaProcessRunning = false;
                        mStopProcess = false;
                        HandleInputChange();
                        SetStatus(" -> NOT ENOUGH MEMORY!   (.NET / Win32Appl limitation)   Try a smaller Area.");
                        Thread.Sleep(2000);
                    }
                }

                if (vAllocationOk)
                {

                    EarthScriptsHandler.DoBeforeDownload(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);

                    SetVisibilityForDownloadActive();
                    //Application.DoEvents();

                    TileCountBox.Invalidate();
                    TileCountBox.Text = Convert.ToString(0);
                    TileCountBox.Refresh();
                    ProgressBox.Invalidate();
                    ProgressBox.Value = 0;
                    ProgressBox.Refresh();
                    MissingTilesBox.Invalidate();
                    MissingTilesBox.Text = Convert.ToString(0);
                    MissingTilesBox.Refresh();

                    mAreaDownloadTilesCount = 0;
                    mAreaDownloadTilesCountIncludingFailed = 0;
                    mAreaDownloadTilesMiss = 0;
                    mAreaDownloadRepetitions = 0;


                    Int64 vAreaCodeXBorder = EarthMath.GetAreaCodeSize(EarthConfig.mFetchLevel);
                    Int64 vAreaCodeX = mEarthArea.AreaCodeXStart;
                    Int64 vAreaCodeY = mEarthArea.AreaCodeYStart;

                    mTileInfoAreaQueue = new TileInfoFIFOQueue((Int32)mEarthArea.AreaTilesInX * (Int32)mEarthArea.AreaTilesInY);

                    // Need to add our excluded tiles here

                    for (Int64 vCountY = 0; vCountY < mEarthArea.AreaTilesInY; vCountY++)
                    {
                        vAreaCodeX = mEarthArea.AreaCodeXStart;
                        for (Int64 vCountX = 0; vCountX < mEarthArea.AreaTilesInX; vCountX++)
                        {
                            TileInfo vTileInfo = new TileInfo(vAreaCodeX, vAreaCodeY, EarthConfig.mFetchLevel, EarthConfig.mSelectedService,false);
                            if (CheckForTileExclusion(vTileInfo))
                            {

                                vTileInfo.mSkipTile = true;

                            }

                            else
                            {

                                vTileInfo.mSkipTile = false;
                            }

                            if (EarthConfig.mShuffleTilesForDownload)
                            {
                                mTileInfoAreaQueue.AddTileInfoRandomPosition(vTileInfo);
                            }
                            else
                            {
                                mTileInfoAreaQueue.AddTileInfo(vTileInfo);
                            }
                            mAreaTilesInfoDownloadCheckList.Add(vTileInfo.Clone());
                            vAreaCodeX++;
                            if (vAreaCodeX >= vAreaCodeXBorder)
                            {
                                vAreaCodeX = 0;
                            }
                        }
                        vAreaCodeY++;
                    }

                    //and start the download
                    mAreaTilePushTick = Environment.TickCount;
                    Thread.Sleep(25); //so we put work into the engines right on this first call (doesn't have to be but better for debugging)
                    FeedThreadEnginesWithNewWork();
                }

            }
            catch (System.Exception e)
            {
                mAreaProcessRunning = false;
                mStopProcess = false;
                HandleInputChange();
                SetStatus("Memory or Code conflict! - QueueAreaTiles() - " + e.ToString());
                Thread.Sleep(2000);
            }

        }

        private void createMeshFiles()
        {
            try
            {
                double startLong = mEarthArea.AreaSnapStartLongitude;
                double stopLong = mEarthArea.AreaSnapStopLongitude;
                double startLat = mEarthArea.AreaSnapStartLatitude;
                double stopLat = mEarthArea.AreaSnapStopLatitude;

                if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                {
                    startLong = mEarthArea.AreaFSResampledStartLongitude;
                    stopLong = mEarthArea.AreaFSResampledStopLongitude;
                    startLat = mEarthArea.AreaFSResampledStartLatitude;
                    stopLat = mEarthArea.AreaFSResampledStopLatitude;
                }

                CommonFunctions.SetStartAndStopCoords(ref startLat, ref startLong, ref stopLat, ref stopLong);

                List<double[]> tilesToDownload = CommonFunctions.GetTilesToDownload(startLong, stopLong, startLat, stopLat);

                foreach (double[] tile in tilesToDownload)
                {
                    string meshFilePath = CommonFunctions.GetMeshFileFullPath(EarthConfig.mWorkFolder, tile);
                    if (!File.Exists(meshFilePath))
                    {
                        string tileName = CommonFunctions.GetTileName(tile);
                        SetStatusFromFriendThread("Creating mesh from OSM data for tile " + tileName);
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo.FileName = EarthConfig.mStartExeFolder + "\\" + "createMesh.exe";
                        proc.StartInfo.Arguments = tile[0] + " " + tile[1] + " \"" + EarthConfig.mWorkFolder + "\" " + tileName;
                        proc.Start();
                        Thread.Sleep(500);
                        proc.WaitForExit();
                        if (!proc.HasExited)
                        {
                            proc.Kill();
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                // need to do it this way. guess it's call pyinstaller programs spawn subprocesses?
                // just calling proc.Kill() keeps createMesh.exe running
                foreach (var p in System.Diagnostics.Process.GetProcessesByName("createMesh"))
                {
                    p.Kill();
                }
                mAreaProcessRunning = false;
                creatingMeshFile = false;
            }
        }

        private void RunImageProcessing(MasksResampleWorker w)
        {
            EarthScriptsHandler.DoBeforeResampleing(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);

            ProcessDownloadedArea(w);

            EarthScriptsHandler.DoAfterDownload(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);

            if (!mStopProcess)
            {
                mMasksCompilerMultithreadedQueue.Enqueue(w);
            }
        }


        private void RunMasksAndSceneryCompiler(MasksResampleWorker w)
        {
            if (!mStopProcess)
            {
                ProcessMasks(w);

                bool bitmapAllWater = false;
                string areaMaskBitmapPath = EarthConfig.mWorkFolder + "\\" + "AreaMask" + w.AreaFileString + ".bmp";
                if (EarthConfig.mCreateWaterMaskBitmap && EarthConfig.skipAllWaterTiles)
                {

                    using (Bitmap bmp = new Bitmap(areaMaskBitmapPath))
                    {
                        bitmapAllWater = CommonFunctions.BitmapAllBlack(bmp);
                    }
                }

                if (!mStopProcess)
                {
                    if (!bitmapAllWater)
                    {
                        EarthScriptsHandler.DoBeforeFSCompilation(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);

                        ProcessSceneryCompilation(w);

                        EarthScriptsHandler.DoAfterFSCompilation(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);
                    }
                    else
                    {
                        CleanupFiles(w);
                        String vAreaThumbnailFileName = "AreaThumbnail" + w.AreaFileString + ".bmp";
                        if (File.Exists(EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName))
                        {
                            File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName);
                        }
                    }
                }
            }
        }

        //AfterMath-Thread start point
        //--- AreaAftermathThread territory
        void AreaAfterDownloadProcessing()
        {
            if (!mStopProcess)
            {
                MasksResampleWorker w = new MasksResampleWorker();
                w.AreaFileString = GetAreaFileString();
                w.mEarthArea = mEarthArea.Clone();
                w.mEarthMultiArea = mEarthMultiArea.Clone();
                w.mCurrentAreaInfo = mCurrentAreaInfo.Clone();
                w.mCurrentActiveAreaNr = mCurrentActiveAreaNr;
                w.mCurrentDownloadedTilesTotal = mCurrentDownloadedTilesTotal;
                w.mMultiAreaMode = mMultiAreaMode;
                w.mEarthAreaTexture = mEarthAreaTexture.Clone();
                mImageProcessingMultithreadedQueue.Enqueue(w);
            }
            else
            {
                mEarthAreaTexture.FreeTextureMemory();
            }

           

        }


        //--- AreaAftermathThread territory
        void ProcessDownloadedArea(MasksResampleWorker w)
        {
            try
            {
                if (!mStopProcess)
                {
                    Boolean vUndistortionExecuted = false;

                    if (EarthConfig.mUndistortionMode != tUndistortionMode.eOff)
                    {
                        if (EarthConfig.mUndistortionMode == tUndistortionMode.eGood)
                        {
                            //SetStatusFromFriendThread("Undistortion Mode is set to 'Good' -> processing Mercator Y only");
                            //Thread.Sleep(500);
                            w.mEarthAreaTexture.UndistortTextureY(w.mEarthArea, tYUndistortionMode.eMercatorOnly, this);
                            vUndistortionExecuted = true;
                        }
                        else if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfect)
                        {
                            //SetStatusFromFriendThread("Undistortion Mode is set to 'Perfect' -> processing exact X and Y undistortion");
                            //Thread.Sleep(500);

                            w.mEarthAreaTexture.UndistortTextureY(w.mEarthArea, tYUndistortionMode.eExact, this);
                            vUndistortionExecuted = true;

                            if ((EarthConfig.mAreaSnapMode != tAreaSnapMode.eTiles) && (EarthConfig.mAreaSnapMode != tAreaSnapMode.ePixel))
                            {
                                w.mEarthAreaTexture.UndistortTextureX(w.mEarthArea, this);
                                vUndistortionExecuted = true;
                            }
                            else
                            {
                                SetStatusFromFriendThread("No undistortion in X required. Texture is already perfect. (AreSnap is on Tiles or Pixel)");
                                Thread.Sleep(500);
                            }
                        }
                        else if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                        {
                            tPictureEnhancement vPictureEnhancement;
                            vPictureEnhancement.mBrightness = EarthConfig.mBrightness;
                            vPictureEnhancement.mContrast = EarthConfig.mContrast;
                            Boolean vContinue = w.mEarthAreaTexture.FSResampleTexture(w.mEarthArea, tResampleMode.eMercator, vPictureEnhancement, this);
                            if (vContinue)
                            {
                                vUndistortionExecuted = true;
                            }
                            else
                            {
                                SetExitStatusFromFriendThread(" Job Aborted ->  Resample Failed! Probabily run out of Memory. Retry with a smaller Reference Area.");
                                mStopProcess = true; //Error Stop!
                            }
                        }
                        else
                        {
                            SetStatusFromFriendThread("Unknown Undistortion Mode! Check your Settings! -> skipp undistortion");
                            Thread.Sleep(500);
                        }
                    }
                    else
                    {
                        SetStatusFromFriendThread("Undistortion is Off  -> skipping undistortion");
                        Thread.Sleep(500);
                    }

                    if (vUndistortionExecuted)
                    {
                        SetStatusFromFriendThread("Finished processing texture undistortion.");
                        if (EarthConfig.mSuppressPitchBlackPixels)
                        {
                            Thread.Sleep(500);
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }

                    if (EarthConfig.mUndistortionMode != tUndistortionMode.ePerfectHighQualityFSPreResampling)
                    {
                        tPictureEnhancement vPictureEnhancement;
                        vPictureEnhancement.mBrightness = EarthConfig.mBrightness;
                        vPictureEnhancement.mContrast   = EarthConfig.mContrast;
                        if ((vPictureEnhancement.mBrightness != 0.0) || (vPictureEnhancement.mContrast != 0.0))
                        {
                            SetStatusFromFriendThread("Processing Color enhancement...");
                            Thread.Sleep(500);
                            w.mEarthAreaTexture.EnhanceColor(vPictureEnhancement);
                        }
                    }

                    if (EarthConfig.mSuppressPitchBlackPixels)
                    {
                       SetStatusFromFriendThread("Suppress pitch black pixels ...");
                       Thread.Sleep(500);
                       w.mEarthAreaTexture.SuppressPitchBlackPixels();
                    }

                    if (!mStopProcess)
                    {

                        
                        String vAreaBmpFileName = "Area" + w.AreaFileString + ".bmp";
                        String vTextureDestination = EarthConfig.mWorkFolder + "\\" + vAreaBmpFileName;

                        
                        String vAreaThumbnailFileName = "AreaThumbnail" + w.AreaFileString + ".bmp";
                        String vThumbnailDestination = EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName;

                        SetStatusFromFriendThread("Saving Area Bitmap to File.");
                        Thread.Sleep(500);

                        w.mEarthAreaTexture.SaveBitmap(vTextureDestination);

                        SetStatusFromFriendThread("Creating Thumbnail and AreaInfo.");
                        Thread.Sleep(500);

                        w.mEarthAreaTexture.CreateAndSaveThumbnail(vThumbnailDestination);

                        SaveAreaInfo(w);

                    }
                }


                w.mEarthAreaTexture.FreeTextureMemory();
            }

            catch (System.Exception e)
            {

                w.mEarthAreaTexture.FreeTextureMemory();

                SetExitStatusFromFriendThread("Memory or Code conflict! - ProcessDownloadedArea() - " + e.ToString());
                mStopProcess = true; //pretend scenery compilation
            }
        }



        void CleanupFiles(MasksResampleWorker w)
        {
            try
            {

                if (!EarthConfig.mKeepAreaInfFile)
                {
                    String vAreaInfoFileName = "AreaFSInfo" + w.AreaFileString + ".inf";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaInfoFileName);
                }
                if (!EarthConfig.mKeepAreaMaskInfFile)
                {
                    String vAreaFS2004MasksInfoFileName = "AreaFS2004MasksInfo" + w.AreaFileString + ".inf";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaFS2004MasksInfoFileName);
                    String vAreaFSXMasksInfoFileName = "AreaFSXMasksInfo" + w.AreaFileString + ".inf";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaFSXMasksInfoFileName);
                }
                if (!EarthConfig.mKeepAreaMaskSeasonInfFile)
                {
                    String vAreaFS2004MasksSeasonsInfoFileName = "AreaFS2004MasksSeasonsInfo" + w.AreaFileString + ".inf";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaFS2004MasksSeasonsInfoFileName);
                    String vAreaFSXMasksSeasonsInfoFileName = "AreaFSXMasksSeasonsInfo" + w.AreaFileString + ".inf";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaFSXMasksSeasonsInfoFileName);
                }
                if (!EarthConfig.mKeepAreaEarthInfoFile)
                {
                    String vAreaEarthInfoFileName = "AreaEarthInfo" + w.AreaFileString + ".txt";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaEarthInfoFileName);
                }
                if (!EarthConfig.mKeepSourceBitmap)
                {
                    String vAreaBmpFileName = "Area" + w.AreaFileString + ".bmp";
                    File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaBmpFileName);
                }

                //MaskStuff
                if (EarthConfig.mCreateAreaMask)
                {
                    String vAreaMaskFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaMask" + w.AreaFileString + ".bmp";
                    String vAreaSummerFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaSummer" + w.AreaFileString + ".bmp";
                    String vAreaNightFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaNight" + w.AreaFileString + ".bmp";
                    String vAreaSpringFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaSpring" + w.AreaFileString + ".bmp";
                    String vAreaAutumnFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaAutumn" + w.AreaFileString + ".bmp";
                    String vAreaWinterFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaWinter" + w.AreaFileString + ".bmp";
                    String vAreaHardWinterFullFilePath = EarthConfig.mWorkFolder + "\\" + "AreaHardWinter" + w.AreaFileString + ".bmp";

                    if (!EarthConfig.mKeepSummerBitmap)
                    {
                        File.Delete(vAreaSummerFullFilePath);
                    }
                    if (!EarthConfig.mKeepMaskBitmap)
                    {
                        File.Delete(vAreaMaskFullFilePath);
                    }
                    if (!EarthConfig.mKeepSeasonsBitmaps)
                    {
                        File.Delete(vAreaNightFullFilePath);
                        File.Delete(vAreaSpringFullFilePath);
                        File.Delete(vAreaAutumnFullFilePath);
                        File.Delete(vAreaWinterFullFilePath);
                        File.Delete(vAreaHardWinterFullFilePath);
                    }
                }
            }
            catch
            {
                //ignore
            }
        }


        //--- AreaAftermathThread territory
        void ProcessSceneryCompilation(MasksResampleWorker w)
        {
            try
            {
                Boolean vSceneryCompilerReturnOk = true;
                Boolean vContinue = true;

                if ((!EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "")) && (!EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "None")) && (EarthConfig.mCompileScenery) && (!mStopProcess))
                {
                    EarthConfig.mSceneryCompiler = "";
                    EarthConfig.mSceneryImageTool = "";
                    if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FSX/P3D"))
                    {
                        EarthConfig.mSceneryCompiler = EarthConfig.mFSXSceneryCompiler;
                        EarthConfig.mSceneryImageTool = "";
                    }
                    if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FS2004"))
                    {
                        EarthConfig.mSceneryCompiler = EarthConfig.mFS2004SceneryCompiler;
                        EarthConfig.mSceneryImageTool = EarthConfig.mFS2004SceneryImageTool;
                    }

                    if (vContinue)
                    {
                        SetStatusFromFriendThread("Starting FS Scenery Compiler...");
                        vSceneryCompilerReturnOk = StartSceneryCompiler(w);
                    }
                }

                //clean up rest of work
                CleanupFiles(w);

                if (vSceneryCompilerReturnOk)
                {
                    //SetExitStatusFromFriendThread("Done.            The Last completed Area contains " + Convert.ToString(mLastDownloadProcessTileMisses) + " faulty or missing Tiles.");
                    //It's always zero fault because 0 fault politic so just say Done.
                }
                else
                {
                    SetExitStatusFromFriendThread("A Failure happend in trying to compile the scenery or processing it's created files.");
                }
            }
            catch (System.Exception e)
            {
                SetExitStatusFromFriendThread("Memory or Code conflict! - ProcessSceneryCompilation() - " + e.ToString());
                mStopProcess = true; //stop 
            }

        }

        void ProcessMasks(MasksResampleWorker w)
        {
            try
            {
                if ((EarthConfig.mCreateAreaMask) && (!mStopProcess))
                {
                    if (EarthConfig.mUseScalableVectorGraphicsTool)
                    {
                        SetStatusFromFriendThread("Starting SVG Tool");
                        StartSVGTool();
                    }

                    EarthScriptsHandler.DoBeforeFSEarthMasks(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);

                    SetStatusFromFriendThread("Starting FS Earth Masks Tool");
                    StartFSEarthMasks(w);

                    EarthScriptsHandler.DoAfterFSEarthMasks(w.mEarthArea, w.AreaFileString, w.mEarthMultiArea, w.mCurrentAreaInfo, w.mCurrentActiveAreaNr, w.mCurrentDownloadedTilesTotal, w.mMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                SetExitStatusFromFriendThread("Memory or Code conflict! - ProcessMasks() - " + e.ToString());
                mStopProcess = true; //stop 
            }
        }


        Boolean StartFSEarthMasks(MasksResampleWorker w)
        {
            System.Diagnostics.Process proc = null;
            try
            {
                if (File.Exists(EarthConfig.mStartExeFolder + "\\" + EarthConfig.mFSEarthMasks))
                {

                    String vAreaEarthInfoFileName = "AreaEarthInfo" + w.AreaFileString + ".txt";

                    proc = new System.Diagnostics.Process();
                    //proc.EnableRaisingEvents = false;
                    //proc.StartInfo.UseShellExecute = false;
                    //proc.StartInfo.RedirectStandardOutput = true;
                    //proc.StartInfo.CreateNoWindow = true;
                    //proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    proc.StartInfo.FileName = EarthConfig.mStartExeFolder + "\\" + EarthConfig.mFSEarthMasks;
                    proc.StartInfo.Arguments = "\"" + EarthConfig.mWorkFolder + "\\" + vAreaEarthInfoFileName + "\"";
                    proc.Start();
                    SetStatusFromFriendThread("FS Earth Masks active. Waiting for completion.");
                    Thread.Sleep(500);
                    proc.WaitForExit();
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    };

                    return true;
                }
                else
                {
                    SetStatusFromFriendThread("FSEarthMasks exe in FSEarthTiles is missing!");
                    return false;
                }
            }
            catch (ThreadAbortException)
            {
                if (proc != null)
                {
                    proc.Kill();
                }

                return false;
            }
            catch
            {
                SetStatusFromFriendThread("Error in StartFSEarthMasks() Unknown Reason.");
                return false;
            }
        }

        Boolean StartSVGTool()
        {
            try
            {
                if (File.Exists(EarthConfig.mScalableVectorGraphicsTool))
                {

                    String vAreaBitmapFileName = "Area" + GetAreaFileString() + ".bmp";

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    //proc.EnableRaisingEvents = false;
                    //proc.StartInfo.UseShellExecute = false;
                    //proc.StartInfo.RedirectStandardOutput = true;
                    //proc.StartInfo.CreateNoWindow = true;
                    //proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    proc.StartInfo.FileName =  EarthConfig.mScalableVectorGraphicsTool;
                    proc.StartInfo.Arguments = "\"" + EarthConfig.mWorkFolder + "\\" + vAreaBitmapFileName + "\"";
                    proc.Start();
                    SetStatusFromFriendThread("SVG Tool active. Waiting for completion.");
                    Thread.Sleep(500);
                    proc.WaitForExit();
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    };

                    return true;
                }
                else
                {
                    SetStatusFromFriendThread("The SVG Tool is missing! Check ScalableVectorGraphicsTool in the config.");
                    Thread.Sleep(2000);
                    return false;
                }
            }
            catch
            {
                SetStatusFromFriendThread("Error in StartFSEarthMasks() Unknown Reason.");
                return false;
            }
        }


        void SetVisibilityForDownloadActive()
        {
            TrueAreaCoords.Visible = false;
            DisplayModeBox.Visible = false;
            DispModeLabel.Visible = false;
            AutoModeLabel.Visible = false;
            MissingTilesBox.Visible = true;
            AreaAndTileCountStatusLabel.Visible = true;
            MissingTilesLabel.Visible = true;
            EditRefButton.Visible = false;
            DrawButton.Visible = false;
            PlaceButton.Visible = false;
            RefModeButton.Visible = false;
            AutoRefSelectorBox.Visible = false;
            TrueAreaCoords.Refresh();
            MissingTilesBox.Refresh();
            AreaAndTileCountStatusLabel.Refresh();
            MissingTilesLabel.Refresh();
            EditRefButton.Refresh();
            DrawButton.Refresh();
            PlaceButton.Refresh();
            RefModeButton.Refresh();
            AutoRefSelectorBox.Refresh();
        }

        void SetVisibilityForIdle()
        {
            AreaAndTileCountStatusLabel.Visible = false;
            MissingTilesLabel.Visible = false;
            MissingTilesBox.Visible = false;
            MissingTilesBox.Visible = false;
            TrueAreaCoords.Visible = true;
            EditRefButton.Visible = true;
            DrawButton.Visible = true;
            PlaceButton.Visible = true;
            if (mMultiAreaMode)
            {
                RefModeButton.Visible = true;
            }
            else
            {
                RefModeButton.Visible = false;
            }
            SetFixOrAutoModeVisibility();
            if (EarthConfig.mShowDisplayModeSelector)
            {
                DisplayModeBox.Visible = true;
                DispModeLabel.Visible = true;
            }
            else
            {
                DisplayModeBox.Visible = false;
                DispModeLabel.Visible = false;
            }
            AreaAndTileCountStatusLabel.Refresh();
            MissingTilesLabel.Refresh();
            MissingTilesBox.Refresh();
            MissingTilesBox.Refresh();
            TrueAreaCoords.Refresh();
            EditRefButton.Refresh();
            DrawButton.Refresh();
            PlaceButton.Refresh();
            RefModeButton.Refresh();
            DisplayModeBox.Refresh();
            DispModeLabel.Refresh();
        }



        //Downloads a Tile form a server. This is still used for the Tile Display mode only. The standatd access is over the engines (multithreading) 
        Tile FetchAreaCodeTile(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iLevel, Int32 iService)
        {
            Boolean vValidService     = false;
            String  vFullTileAddress  = "";
            String  vServiceReference = "";

            Tile vTile = null;

            if (EarthConfig.layServiceMode)
            {
                vTile = new Tile(iAreaCodeX, iAreaCodeY, iLevel, EarthConfig.layServiceSelected,false);
            }
            else
            {
                vTile = new Tile(iAreaCodeX, iAreaCodeY, iLevel, iService,false);
            }
            vFullTileAddress = EarthScriptsHandler.CreateWebAddress(iAreaCodeX, iAreaCodeY, iLevel, iService);

            vServiceReference = EarthConfig.mServiceReferer[iService - 1];

            vValidService = true;

            if (vValidService)
            {
                Int64 vRetries = 0;
                //Access
                do //Repeat endless
                {
                    try
                    {
                        //For simulating bad connection:
                        if (EarthConfig.mSimulateBadConnection)
                        {
                            Random myRandom = new Random();
                            if (myRandom.NextDouble() > 0.50)
                            {
                                WebException vError = new WebException();
                                throw vError;
                            }
                        }
                        if (EarthConfig.mSimulateBlockConnection)
                        {
                            WebException vError = new WebException();
                            throw vError;
                        }

                        Uri myTileUri = new Uri(vFullTileAddress);

                        //-- Cookie Access Test Code --
                        //Uri myCookieUri = new Uri("http://www.******.com");
                        //System.Net.HttpWebRequest cookierequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(myCookieUri);
                        //cookierequest.Referer = vServiceReference;
                        //cookierequest.CookieContainer = new CookieContainer();
                        //if (mCookies.Count > 0)
                        //{
                        //    cookierequest.CookieContainer.Add(mCookies);
                        //}

                        //if (!EarthCommon.StringCompare(mProxy, "direct"))
                        //{
                        //   WebProxy vProxy = new WebProxy("http://" + mProxy + "/", true);
                        //   request.Proxy = vProxy;
                        //}

                        //HttpWebResponse cookieresponse = (HttpWebResponse)cookierequest.GetResponse();

                        //if (cookieresponse.Cookies.Count > 0)
                        //{
                        //    mCookies = cookieresponse.Cookies;
                        //}

                        //System.IO.Stream vCookieDummyStream;
                        //vCookieDummyStream = cookieresponse.GetResponseStream();
                        //vCookieDummyStream.Close(); //Close Stream
                        //-----------------

                        System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(myTileUri);
                        if (EarthConfig.layServiceMode)
                        {
                            request.Referer = myTileUri.Scheme + "://" + request.Host;
                        }
                        else
                        {
                            request.Referer = vServiceReference;
                        }

                        //test code
                        //Uri vProxyUri = new Uri("http://130.149.49.26:3124/");
                        //WebProxy vProxy = new WebProxy(vProxyUri, true); 
                        //WebProxy vProxy = new WebProxy("http://130.149.49.26:3124/", true);
                        //WebProxy vProxy = new WebProxy("http://127.0.0.1:8118/", true);
                        //request.Proxy = vProxy;
                        //

                        if (!EarthCommon.StringCompare(mProxy, "direct"))
                        {
                            WebProxy vProxy = new WebProxy("http://" + mProxy + "/", true);
                            request.Proxy = vProxy;
                        }

                        if (mHandleCookies)
                        {
                            request.CookieContainer = new CookieContainer();
                            if (mCookies.Count > 0)
                            {
                                request.CookieContainer.Add(mCookies);
                            }
                        }

                        System.Net.WebResponse response = request.GetResponse();

                        System.IO.Stream vPicStreamReadBack;

                        vPicStreamReadBack = response.GetResponseStream();

                        Bitmap mybitmap = new Bitmap(vPicStreamReadBack);

                        vPicStreamReadBack.Close(); //Close Stream/File;

                        vTile.StoreBitmap(mybitmap);
                        vTile.MarkAsGoodBitmap();

                        return vTile;
                    }
                    catch (System.Net.WebException e)
                    {
                        if(e.Status == WebExceptionStatus.ProtocolError)
                        {
                           HttpStatusCode vStatusCode = ((HttpWebResponse)e.Response).StatusCode;

                           if ((vStatusCode == HttpStatusCode.NotFound) || (vStatusCode == HttpStatusCode.BadRequest))
                           {
                               if (vRetries >= 1)
                               {
                                   //Set No Tile Found Mark as good
                                   vTile.StoreBitmap(mNoTileFound);
                                   vTile.MarkAsGoodBitmap();
                                   return vTile;
                               }
                           }
                           else
                           {
                               if (vRetries >= 1)
                               {
                                   //Set No Tile Found
                                   SetStatus("Can not web-access): " + vFullTileAddress);
                                   vTile.StoreBitmap(mNoTileFound);
                                   vTile.MarkAsBadBitmap();
                                   return vTile;
                               }
                           }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatus("Can not web-access (2 of 2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFound);
                            vTile.MarkAsBadBitmap();

                            return vTile;
                        }
                        else
                        {
                            SetStatus("Can not web-access (1 of 2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatus("Can not other-access (2 of 2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFound);
                            vTile.MarkAsBadBitmap();

                            return vTile;
                        }
                        else
                        {
                            SetStatus("Can not other-access (1 of 2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while (true);
            }
            else
            {
                SetStatus("Unknown Service or TileCode");
                vTile.StoreBitmap(mNoTileFound);
                vTile.MarkAsBadBitmap();
                return vTile;
            }
        }


        void SetAreaSnapInfo(String iStatus)
        {
            TrueAreaCoords.Invalidate();
            TrueAreaCoords.Text = iStatus;
            TrueAreaCoords.Refresh();
        }


        void SetStatus(String iStatus)
        {
            StatusBox.Invalidate();
            StatusBox.Text = iStatus;
            StatusBox.Refresh();
        }


        void SetStatusFromFriendThread(String iStatus)
        {
            mSetFriendThreadStatusMutex.WaitOne();
            mStatusFriendThread = iStatus;
            mSetFriendThreadStatusMutex.ReleaseMutex();
        }


        void SetExitStatusFromFriendThread(String iStatus)
        {
            mSetFriendThreadStatusMutex.WaitOne();
            mExitStatusFriendThread = iStatus;
            mSetFriendThreadStatusMutex.ReleaseMutex();
        }


        String GetStatusFromFriendThread()
        {
            String vStatusFriendThread;

            mSetFriendThreadStatusMutex.WaitOne();
            vStatusFriendThread = mStatusFriendThread;
            mStatusFriendThread = "";
            mSetFriendThreadStatusMutex.ReleaseMutex();

            return vStatusFriendThread;
        }

        String GetExitStatusFromFriendThread()
        {
            String vStatusFriendThread;

            mSetFriendThreadStatusMutex.WaitOne();
            vStatusFriendThread = mExitStatusFriendThread;
            mStatusFriendThread = "";
            mSetFriendThreadStatusMutex.ReleaseMutex();

            return vStatusFriendThread;
        }





        Int64 RecodeAreaCodeTopLeftCorner(Int64 iAreaCode, Int64 iFromLevel, Int64 iToLevel)
        {
            Int32 vDeltaLevel = Convert.ToInt32(iToLevel - iFromLevel);
            Int64 vNewAreaCode = iAreaCode;

            if (vDeltaLevel == 0)
            {
                return vNewAreaCode;
            }
            else if (iToLevel < iFromLevel)
            {
                vDeltaLevel = -vDeltaLevel;
                vNewAreaCode = vNewAreaCode << vDeltaLevel; //*2^vDeltaLevel
                return vNewAreaCode;
            }
            else
            {
                vNewAreaCode = vNewAreaCode >> vDeltaLevel; //  div 2^vDeltaLevel
                return vNewAreaCode;
            }
        }


        Int64 RecodeAreaCodeBottomRightCorner(Int64 iAreaCode, Int64 iFromLevel, Int64 iToLevel)
        {
            Int32 vDeltaLevel = Convert.ToInt32(iToLevel - iFromLevel);
            Int64 vNewAreaCode = iAreaCode;

            if (vDeltaLevel == 0)
            {
                return vNewAreaCode;
            }
            else if (iToLevel < iFromLevel)
            {
                vDeltaLevel = -vDeltaLevel;
                vNewAreaCode += 1;
                vNewAreaCode = vNewAreaCode << vDeltaLevel; //*2^vDeltaLevel
                vNewAreaCode -= 1;
                return vNewAreaCode;
            }
            else
            {
                vNewAreaCode += 1;
                vNewAreaCode = vNewAreaCode >> vDeltaLevel; //  div 2^vDeltaLevel
                vNewAreaCode -= 1;
                return vNewAreaCode;
            }
        }




        void SetInputNWCornerLatitudeTo(Double iStartLatitude)
        {
            NWLatGradBox.Text = EarthMath.GetWorldCoordGrad(iStartLatitude);
            NWLatMinuteBox.Text = EarthMath.GetWorldCoordMinute(iStartLatitude);
            NWLatSecBox.Text = EarthMath.GetWorldCoordSec(iStartLatitude);
            NWLatSignBox.Text = EarthMath.GetSignLatitude(iStartLatitude);
        }

        void SetInputNWCornerLongitudeTo(Double iStartLongitude)
        {
            NWLongGradBox.Text = EarthMath.GetWorldCoordGrad(iStartLongitude);
            NWLongMinuteBox.Text = EarthMath.GetWorldCoordMinute(iStartLongitude);
            NWLongSecBox.Text = EarthMath.GetWorldCoordSec(iStartLongitude);
            NWLongSignBox.Text = EarthMath.GetSignLongitude(iStartLongitude);
        }

        void SetInputSECornerLatitudeTo(Double iStopLatitude)
        {
            SELatGradBox.Text = EarthMath.GetWorldCoordGrad(iStopLatitude);
            SELatMinuteBox.Text = EarthMath.GetWorldCoordMinute(iStopLatitude);
            SELatSecBox.Text = EarthMath.GetWorldCoordSec(iStopLatitude);
            SELatSignBox.Text = EarthMath.GetSignLatitude(iStopLatitude);
        }

        void SetInputSECornerLongitudeTo(Double iStopLongitude)
        {
            SELongGradBox.Text = EarthMath.GetWorldCoordGrad(iStopLongitude);
            SELongMinuteBox.Text = EarthMath.GetWorldCoordMinute(iStopLongitude);
            SELongSecBox.Text = EarthMath.GetWorldCoordSec(iStopLongitude);
            SELongSignBox.Text = EarthMath.GetSignLongitude(iStopLongitude);
        }

        void SetInputCornerCoordsTo(Double iStartLatitude, Double iStartLongitude, Double iStopLatitude, Double iStopLongitude)
        {
            mBlockInputChangeHandle = true; //BlockUpdate Till everything is filled in
            SetInputNWCornerLatitudeTo(iStartLatitude);
            SetInputNWCornerLongitudeTo(iStartLongitude);
            SetInputSECornerLatitudeTo(iStopLatitude);
            SetInputSECornerLongitudeTo(iStopLongitude);
            mBlockInputChangeHandle = false;
            if (EarthCommon.StringCompare(AreaDefModeBox.Text, "2Points"))
            {
                HandleInputChange();
            }
        }

        void UpdateCornerCoordsInput()
        {
            SetInputCornerCoordsTo(mEarthInputArea.AreaStartLatitude, mEarthInputArea.AreaStartLongitude, mEarthInputArea.AreaStopLatitude, mEarthInputArea.AreaStopLongitude);
        }


        void SetInputCenterCoordLatitudeTo(Double iLatitude)
        {
            LatGradBox.Text = EarthMath.GetWorldCoordGrad(iLatitude);
            LatMinuteBox.Text = EarthMath.GetWorldCoordMinute(iLatitude);
            LatSecBox.Text = EarthMath.GetWorldCoordSec(iLatitude);
            LatSignBox.Text = EarthMath.GetSignLatitude(iLatitude);
        }

        void SetInputCenterCoordLongitudeTo(Double iLongitude)
        {
            LongGradBox.Text = EarthMath.GetWorldCoordGrad(iLongitude);
            LongMinuteBox.Text = EarthMath.GetWorldCoordMinute(iLongitude);
            LongSecBox.Text = EarthMath.GetWorldCoordSec(iLongitude);
            LongSignBox.Text = EarthMath.GetSignLongitude(iLongitude);
        }

        void SetInputCenterCoordsTo(Double iLatitude, Double iLongitude)
        {

            mBlockInputChangeHandle = true; //BlockUpdate Till everything is filled in
            SetInputCenterCoordLatitudeTo(iLatitude);
            SetInputCenterCoordLongitudeTo(iLongitude);
            mBlockInputChangeHandle = false;
            if (EarthCommon.StringCompare(AreaDefModeBox.Text, "1Point"))
            {
              HandleInputChange();
            }
        }


        void UpdateCenterCoordsInput()
        {
            SetInputCenterCoordsTo(mEarthInputArea.SpotLatitude, mEarthInputArea.SpotLongitude);
            AreaSizeXBox.Invalidate();
            AreaSizeXBox.Text = Convert.ToString(Math.Round(mEarthInputArea.AreaXSize, 3));
            AreaSizeXBox.Refresh();
            AreaSizeYBox.Invalidate();
            AreaSizeYBox.Text = Convert.ToString(Math.Round(mEarthInputArea.AreaYSize, 3));
            AreaSizeYBox.Refresh();
        }

        Double GetAreaYSizeInNm(Double iStartLatitude, Double iStopLatitude)
        {
            Double vPointLatitude = 0.5 * (iStartLatitude + iStopLatitude);
            Double vLocalRadius = Math.Cos(EarthMath.cDegreeToRadFactor * vPointLatitude) * EarthMath.cEarthRadius;
            Double vLocalCircumference = 2.0 * EarthMath.cPi * vLocalRadius;
            Double vAreaYSize = EarthMath.cMeterTonmFactor * EarthMath.cEarthCircumference * (iStartLatitude - iStopLatitude) * EarthMath.cInv360Grad;
            return vAreaYSize;
        }


        Double GetAreaXSizeInNm(Double iStartLatitude, Double iStopLatitude, Double iStartLongitude, Double iStopLongitude)
        {
            Double vPointLatitude = 0.5 * (iStartLatitude + iStopLatitude);
            Double vLocalRadius = Math.Cos(EarthMath.cDegreeToRadFactor * vPointLatitude) * EarthMath.cEarthRadius;
            Double vLocalCircumference = 2.0 * EarthMath.cPi * vLocalRadius;
            Double vAreaXSize = 0.0;
            if (iStopLongitude >= iStartLongitude)
            {
                vAreaXSize = EarthMath.cMeterTonmFactor * vLocalCircumference * (iStopLongitude - iStartLongitude) * EarthMath.cInv360Grad;
            }
            else
            {
                vAreaXSize = EarthMath.cMeterTonmFactor * vLocalCircumference * (360.0 + iStopLongitude - iStartLongitude) * EarthMath.cInv360Grad;
            }
            return vAreaXSize;
        }


        void CalculateAreaInputCoords()
        {
            //1Point -> 2Point and 2Point -> 1 Point
            
            if (EarthCommon.StringCompare(AreaDefModeBox.Text, "2Points"))
            {
                //form 2Points
                mEarthInputArea.SpotLongitude = 0.5 * (mEarthInputArea.AreaStartLongitude + mEarthInputArea.AreaStopLongitude);
                mEarthInputArea.SpotLatitude = 0.5 * (mEarthInputArea.AreaStartLatitude + mEarthInputArea.AreaStopLatitude);
                mEarthInputArea.AreaYSize = GetAreaYSizeInNm(mEarthInputArea.AreaStartLatitude, mEarthInputArea.AreaStopLatitude);
                mEarthInputArea.AreaXSize = GetAreaXSizeInNm(mEarthInputArea.AreaStartLatitude, mEarthInputArea.AreaStopLatitude, mEarthInputArea.AreaStartLongitude, mEarthInputArea.AreaStopLongitude);
                UpdateCenterCoordsInput();
            }
            else
            {
                //from 1Point
                //Calculate Longtitudinal Distances
                Double vLocalRadius = Math.Cos(EarthMath.cDegreeToRadFactor * mEarthInputArea.SpotLatitude) * EarthMath.cEarthRadius;
                Double vLocalCircumference = 2.0 * EarthMath.cPi * vLocalRadius;
                Double vXHalfAreaDistance = 0.5 * mEarthInputArea.AreaXSize * EarthMath.cnmToMeterFactor;
                Double vYHalfAreaDistance = 0.5 * mEarthInputArea.AreaYSize * EarthMath.cnmToMeterFactor;

                //Earth Pole Check
                if ((2.0 * vXHalfAreaDistance) >= vLocalCircumference)
                {
                    //Full circle!
                    mEarthInputArea.AreaStartLongitude = -180.0;
                    mEarthInputArea.AreaStopLongitude = +180.0;
                }
                else
                {
                    //Note we may overrun the +/-180 Grad here but we need a continues unbroken Angle in the Area
                    //So this will be taken care of later
                    Double vLongHalfDistInGrad = 360.0 * vXHalfAreaDistance / vLocalCircumference;
                    mEarthInputArea.AreaStartLongitude = mEarthInputArea.SpotLongitude - vLongHalfDistInGrad;
                    mEarthInputArea.AreaStopLongitude = mEarthInputArea.SpotLongitude + vLongHalfDistInGrad;
                }

                //Calculate Latitudinal Distances
                Double vLatHalfDistInGrad = 360.0 * vYHalfAreaDistance / EarthMath.cEarthCircumference;

                mEarthInputArea.AreaStartLatitude = mEarthInputArea.SpotLatitude + vLatHalfDistInGrad;
                mEarthInputArea.AreaStopLatitude = mEarthInputArea.SpotLatitude - vLatHalfDistInGrad;

                mEarthInputArea.AreaStartLatitude = EarthMath.CleanLatitude(mEarthInputArea.AreaStartLatitude);
                mEarthInputArea.AreaStopLatitude = EarthMath.CleanLatitude(mEarthInputArea.AreaStopLatitude);

                UpdateCornerCoordsInput();
            }
        }

        void DisplayAreaSnapCords()
        {
            String vSnapStartLongitude;
            String vSnapStopLongitude;
            String vSnapStartLatitude;
            String vSnapStopLatitude;
            String vAreaSnapString;
            String vAreaSizeXString;
            String vAreaSizeYString;

            Double vAreaSizeX;
            Double vAreaSizeY;

            if (mMultiAreaMode)
            {
                vSnapStartLatitude = EarthMath.GetSignLatitude(mEarthMultiArea.AreaSnapStartLatitude);
                vSnapStartLatitude += EarthMath.GetWorldCoordGrad(mEarthMultiArea.AreaSnapStartLatitude) + "° ";
                vSnapStartLatitude += EarthMath.GetWorldCoordMinute(mEarthMultiArea.AreaSnapStartLatitude) + "' ";
                vSnapStartLatitude += EarthMath.GetWorldCoordSec(mEarthMultiArea.AreaSnapStartLatitude) + "'' ";
                vSnapStartLongitude = EarthMath.GetSignLongitude(mEarthMultiArea.AreaSnapStartLongitude);
                vSnapStartLongitude += EarthMath.GetWorldCoordGrad(mEarthMultiArea.AreaSnapStartLongitude) + "° ";
                vSnapStartLongitude += EarthMath.GetWorldCoordMinute(mEarthMultiArea.AreaSnapStartLongitude) + "' ";
                vSnapStartLongitude += EarthMath.GetWorldCoordSec(mEarthMultiArea.AreaSnapStartLongitude) + "'' ";
                vSnapStopLatitude = EarthMath.GetSignLatitude(mEarthMultiArea.AreaSnapStopLatitude);
                vSnapStopLatitude += EarthMath.GetWorldCoordGrad(mEarthMultiArea.AreaSnapStopLatitude) + "° ";
                vSnapStopLatitude += EarthMath.GetWorldCoordMinute(mEarthMultiArea.AreaSnapStopLatitude) + "' ";
                vSnapStopLatitude += EarthMath.GetWorldCoordSec(mEarthMultiArea.AreaSnapStopLatitude) + "'' ";
                vSnapStopLongitude = EarthMath.GetSignLongitude(mEarthMultiArea.AreaSnapStopLongitude);
                vSnapStopLongitude += EarthMath.GetWorldCoordGrad(mEarthMultiArea.AreaSnapStopLongitude) + "° ";
                vSnapStopLongitude += EarthMath.GetWorldCoordMinute(mEarthMultiArea.AreaSnapStopLongitude) + "' ";
                vSnapStopLongitude += EarthMath.GetWorldCoordSec(mEarthMultiArea.AreaSnapStopLongitude) + "'' ";

                vAreaSizeX = GetAreaXSizeInNm(mEarthMultiArea.AreaSnapStartLatitude, mEarthMultiArea.AreaSnapStopLatitude, mEarthMultiArea.AreaSnapStartLongitude, mEarthMultiArea.AreaSnapStopLongitude);
                vAreaSizeY = GetAreaYSizeInNm(mEarthMultiArea.AreaSnapStartLatitude, mEarthMultiArea.AreaSnapStopLatitude);

            }
            else
            {
                vSnapStartLatitude = EarthMath.GetSignLatitude(mEarthArea.AreaSnapStartLatitude);
                vSnapStartLatitude += EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStartLatitude) + "° ";
                vSnapStartLatitude += EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStartLatitude) + "' ";
                vSnapStartLatitude += EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStartLatitude) + "'' ";
                vSnapStartLongitude = EarthMath.GetSignLongitude(mEarthArea.AreaSnapStartLongitude);
                vSnapStartLongitude += EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStartLongitude) + "° ";
                vSnapStartLongitude += EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStartLongitude) + "' ";
                vSnapStartLongitude += EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStartLongitude) + "'' ";
                vSnapStopLatitude = EarthMath.GetSignLatitude(mEarthArea.AreaSnapStopLatitude);
                vSnapStopLatitude += EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStopLatitude) + "° ";
                vSnapStopLatitude += EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStopLatitude) + "' ";
                vSnapStopLatitude += EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStopLatitude) + "'' ";
                vSnapStopLongitude = EarthMath.GetSignLongitude(mEarthArea.AreaSnapStopLongitude);
                vSnapStopLongitude += EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStopLongitude) + "° ";
                vSnapStopLongitude += EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStopLongitude) + "' ";
                vSnapStopLongitude += EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStopLongitude) + "'' ";

                vAreaSizeX = GetAreaXSizeInNm(mEarthArea.AreaSnapStartLatitude, mEarthArea.AreaSnapStopLatitude, mEarthArea.AreaSnapStartLongitude, mEarthArea.AreaSnapStopLongitude);
                vAreaSizeY = GetAreaYSizeInNm(mEarthArea.AreaSnapStartLatitude, mEarthArea.AreaSnapStopLatitude);
            }

            vAreaSizeXString = Convert.ToString(Math.Round(vAreaSizeX, 3));
            vAreaSizeYString = Convert.ToString(Math.Round(vAreaSizeY, 3));

            if (EarthCommon.StringCompare(AreaSnapBox.Text, "Off"))
            {
                vAreaSnapString = "Area Snap is " + AreaSnapBox.Text + ":   North-West-Corner: " + vSnapStartLatitude + "  " + vSnapStartLongitude + "   South-East-Corner: " + vSnapStopLatitude + "  " + vSnapStopLongitude + "   " + vAreaSizeXString + "nm X " + vAreaSizeYString + "nm";
            }
            else
            {
                vAreaSnapString = "Area snapped on " + AreaSnapBox.Text + ":   North-West-Corner: " + vSnapStartLatitude + "  " + vSnapStartLongitude + "   South-East-Corner: " + vSnapStopLatitude + "  " + vSnapStopLongitude + "   " + vAreaSizeXString + "nm X " + vAreaSizeYString + "nm";

            }

            SetAreaSnapInfo(vAreaSnapString);
        }

        Int64 GetFullSnappedAreaZoomLevel()
        {
            Double vAreaSizeX = 0;
            Double vAreaSizeY = 0;

            Double vAreaSnapStartLatitude;
            Double vAreaSnapStopLatitude;
            Double vAreaSnapStartLongitude;
            Double vAreaSnapStopLongitude;

            if (mMultiAreaMode)
            {
                vAreaSizeX = GetAreaXSizeInNm(mEarthMultiArea.AreaSnapStartLatitude, mEarthMultiArea.AreaSnapStopLatitude, mEarthMultiArea.AreaSnapStartLongitude, mEarthMultiArea.AreaSnapStopLongitude);
                vAreaSizeY = GetAreaYSizeInNm(mEarthMultiArea.AreaSnapStartLatitude, mEarthMultiArea.AreaSnapStopLatitude);
                vAreaSnapStartLatitude = mEarthMultiArea.AreaSnapStartLatitude;
                vAreaSnapStopLatitude = mEarthMultiArea.AreaSnapStopLatitude;
                vAreaSnapStartLongitude = mEarthMultiArea.AreaSnapStartLongitude;
                vAreaSnapStopLongitude = mEarthMultiArea.AreaSnapStopLongitude;
            }
            else
            {
                vAreaSizeX = GetAreaXSizeInNm(mEarthArea.AreaSnapStartLatitude, mEarthArea.AreaSnapStopLatitude, mEarthArea.AreaSnapStartLongitude, mEarthArea.AreaSnapStopLongitude);
                vAreaSizeY = GetAreaYSizeInNm(mEarthArea.AreaSnapStartLatitude, mEarthArea.AreaSnapStopLatitude);
                vAreaSnapStartLatitude = mEarthArea.AreaSnapStartLatitude;
                vAreaSnapStopLatitude = mEarthArea.AreaSnapStopLatitude;
                vAreaSnapStartLongitude = mEarthArea.AreaSnapStartLongitude;
                vAreaSnapStopLongitude = mEarthArea.AreaSnapStopLongitude;
            }

            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1; //well not really center but the reference point!
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;

            Int64 vFreeKeepBorderPixelX = NormalPictureBox.Size.Width >> 3; // 1/8 of display
            Int64 vFreeKeepBorderPixelY = NormalPictureBox.Size.Height >> 3;

            Int64 vZoomLvl = EarthMath.cLevel0CodeDeep;

            //no speed required here so we do it slow and simple with a for loop... 

            for (Int64 vTryLvl = 0; vTryLvl < EarthMath.cLevel0CodeDeep; vTryLvl++)
            {
                Double vDisplayAreaTopLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, vDisplayPixelCenterY - vFreeKeepBorderPixelY, vTryLvl);
                Double vDisplayAreaBottomLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -(NormalPictureBox.Size.Height - vDisplayPixelCenterY - vFreeKeepBorderPixelY), vTryLvl);
                Double vDisplayAreaRightLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, (NormalPictureBox.Size.Width - vDisplayPixelCenterX - vFreeKeepBorderPixelX), vTryLvl);
                Double vDisplayAreaLeftLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, -(vDisplayPixelCenterX - vFreeKeepBorderPixelX), vTryLvl);

                if ((vAreaSnapStartLatitude < vDisplayAreaTopLatitude) &&
                    (vAreaSnapStopLatitude > vDisplayAreaBottomLatitude) &&
                    (vAreaSnapStartLongitude > vDisplayAreaLeftLongitude) &&
                    (vAreaSnapStopLongitude < vDisplayAreaRightLongitude))
                {
                    vZoomLvl = vTryLvl;
                    vTryLvl = EarthMath.cLevel0CodeDeep; //found so abort
                }
            }
            return vZoomLvl;
        }


        void CalculateAreaSnapCoordsAndCode()
        {
            if (mMultiAreaMode)
            {
                //MultiArea
                mEarthSingleReferenceArea.CalculateAreaSnapCoordsAndCode(mEarthSingleReferenceInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                mEarthMultiArea.CalculateAreaSnapCoordsAndCodeForMultiArea(mEarthInputArea, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                mEarthArea.Copy(mEarthMultiArea);
            }
            else
            {
                //Normal
                mEarthArea.CalculateAreaSnapCoordsAndCode(mEarthInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
            }
            DisplayAreaSnapCords();
        }


        void CalculateMemorySizeAndDisplayMemoryAndTileCount()
        {

            Int64 vDownloadSize = 0;
            Int64 vScenerySize = 0;
            Int64 vRequiredRam = 0;
            Int64 vRequiredWorkSpace = 0;
            Int64 vSkippedAreasCount = 0;

            vSkippedAreasCount=Convert.ToInt64(AreaProcessInfo.GetNumberOfAreas2Download());


            //Required Ram
            if (mMultiAreaMode)
            {
                Int64 vTotalPixels = mEarthSingleReferenceArea.AreaPixelsInX * mEarthSingleReferenceArea.AreaPixelsInY;

                if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                {
                    if ((mEarthSingleReferenceArea.AreaFSResampledPixelsInX * mEarthSingleReferenceArea.AreaFSResampledPixelsInY) > vTotalPixels)
                    {
                        vTotalPixels = (mEarthSingleReferenceArea.AreaFSResampledPixelsInX * mEarthSingleReferenceArea.AreaFSResampledPixelsInY);
                    }
                }

                vRequiredRam = 2 * Convert.ToInt64(Math.Round(Convert.ToDouble(vTotalPixels * cUsedMemoryPerPixel)));   //MByte (space for 2 pictures is required)
            }
            else
            {
                vRequiredRam = 2 * Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthArea.AreaPixelsInX * mEarthArea.AreaPixelsInY * cUsedMemoryPerPixel)));   //MByte
            }


            //DonloadSize
            if (mMultiAreaMode)
            {
                Int64 AverageAreaSize=0;
                vDownloadSize = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthMultiArea.FetchTilesMultiAreaTotal * cFetchTileSize)));   //MByte
                AverageAreaSize=vDownloadSize/mEarthMultiArea.AreaTotal;
                vDownloadSize = AverageAreaSize * AreaProcessInfo.GetNumberOfAreas2Download();
            }
            else
            {
                vDownloadSize = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthArea.FetchTilesTotal * cFetchTileSize)));   //MByte
            }


            //ScenerySize
            if (mMultiAreaMode)
            {
                Int64 AverageAreaScenerySize = 0; 
                vScenerySize = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthMultiArea.AreaPixelsInX * mEarthMultiArea.AreaPixelsInY * cPixelToTileFactor * cSceneryTileSize)));   //MByte
                AverageAreaScenerySize = vScenerySize / mEarthMultiArea.AreaTotal;
                vScenerySize = AverageAreaScenerySize * AreaProcessInfo.GetNumberOfAreas2Download();
            }
            else
            {
                 vScenerySize = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthArea.AreaPixelsInX * mEarthArea.AreaPixelsInY * cPixelToTileFactor * cSceneryTileSize)));   //MByte
            }
            if (EarthConfig.mCreateAreaMask)
            {
                vScenerySize = vScenerySize + vScenerySize>>1; //(*1.5)

                if ((EarthConfig.mCreateAutumnBitmap) ||
                    (EarthConfig.mCreateHardWinterBitmap) || 
                    (EarthConfig.mCreateNightBitmap) || 
                    (EarthConfig.mCreateSpringBitmap) || 
                    (EarthConfig.mCreateSummerBitmap)|| 
                    (EarthConfig.mCreateWinterBitmap))
                {
                    vScenerySize = vScenerySize + vScenerySize >> 1; //(*1.5)
                }
            }


            //WorkSize
            if (mMultiAreaMode)
            {
                Int64 AverageWorkSpace = 0;
                if ((EarthConfig.mKeepSourceBitmap) ||
                    (EarthConfig.mKeepSummerBitmap) || 
                    (EarthConfig.mKeepMaskBitmap) || 
                    (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vRequiredWorkSpace = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthMultiArea.AreaPixelsInX * mEarthMultiArea.AreaPixelsInY * cUsedMemoryPerPixel + cWorkSpaceMemoryOffset)));   //MByte
                    AverageWorkSpace = vRequiredWorkSpace / mEarthMultiArea.AreaTotal;
                    vRequiredWorkSpace = AverageWorkSpace * AreaProcessInfo.GetNumberOfAreas2Download();
                    
                }
                else
                {
                    vRequiredWorkSpace = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthSingleReferenceArea.AreaPixelsInX * mEarthSingleReferenceArea.AreaPixelsInY * cUsedMemoryPerPixel + cWorkSpaceMemoryOffset)));   //MByte
                }
            }
            else
            {
                vRequiredWorkSpace = Convert.ToInt64(Math.Round(Convert.ToDouble(mEarthArea.AreaPixelsInX * mEarthArea.AreaPixelsInY * cUsedMemoryPerPixel + cWorkSpaceMemoryOffset)));   //MByte
            }


            if (EarthConfig.mCreateAreaMask)
            {
                Int64 vFactor = 1;
 
                if ((EarthConfig.mCreateAutumnBitmap) && (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vFactor++;
                }
                if ((EarthConfig.mCreateHardWinterBitmap) && (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vFactor++;
                }
                if ((EarthConfig.mCreateNightBitmap) && (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vFactor++;
                }
                if ((EarthConfig.mCreateSpringBitmap) && (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vFactor++;
                }
                if ((EarthConfig.mCreateWinterBitmap) && (EarthConfig.mKeepSeasonsBitmaps))
                {
                    vFactor++;
                }
                if ((EarthConfig.mCreateSummerBitmap) && (EarthConfig.mKeepSummerBitmap))
                {
                    vFactor++;
                } 
                if ((EarthConfig.mKeepMaskBitmap) && (!EarthCommon.StringCompare(CompilerSelectorBox.Text, "FS2004")))
                {
                    vFactor++;
                }
                vRequiredWorkSpace = vFactor * vRequiredWorkSpace;
                if (EarthConfig.mUseCache) { vRequiredWorkSpace += vDownloadSize; }

            }


            //and display the values
            if (mMultiAreaMode)
            {
                TileCountBox.Text = Convert.ToString(mEarthMultiArea.FetchTilesMultiAreaTotal);
                AreaCountBox.Text = Convert.ToString(mEarthMultiArea.AreaTotal);
            }
            else
            {
                TileCountBox.Text = Convert.ToString(mEarthArea.FetchTilesTotal);
                AreaCountBox.Text = Convert.ToString(1);
            }

            DownloadSizeBox.Invalidate();
            ScenerySizeBox.Invalidate();
            RequiredRam.Invalidate();
            RequiredWorkSpace.Invalidate();

            DownloadSizeBox.Text = Convert.ToString(vDownloadSize);
            ScenerySizeBox.Text = Convert.ToString(vScenerySize);
            RequiredRam.Text = Convert.ToString(vRequiredRam);
            RequiredWorkSpace.Text = Convert.ToString(vRequiredWorkSpace);

            DownloadSizeBox.Refresh();
            ScenerySizeBox.Refresh();
            RequiredRam.Refresh();
            RequiredWorkSpace.Refresh();

        }


        void CalculateAreaCoords()
        {
            CalculateAreaInputCoords();
            CalculateAreaSnapCoordsAndCode();
            CalculateMemorySizeAndDisplayMemoryAndTileCount();
        }


        void MarkSpotCoordsInvalid()
        {
            mEarthInputArea.SpotLatitude = 0.0;
            mEarthInputArea.SpotLongitude = 0.0;
            CoordsValidityBox.Text = "Invalid!";
            CoordsValidityBox.ForeColor = Color.Firebrick;
            mInputCoordsValidity = false;
            mEarthKML.Areas.Clear();                // Clear the KML area polygons
            mUsingKML = false;                      // We cannot use the kml file if the data is not valid
        }

        void HandleInputChange()
        {
            if (!mAreaProcessRunning && mConfigInitDone && !mBlockInputChangeHandle)
            {
                mAllowDisplayToSetStatus = true;
                ResetTimeOutCounter();
                ProgressBox.Value = 0;
                try
                {
                    Double vLatGrad = Convert.ToDouble(LatGradBox.Text);
                    Double vLatMinute = Convert.ToDouble(LatMinuteBox.Text);
                    Double vLatSec = Convert.ToDouble(LatSecBox.Text);
                    Double vLongGrad = Convert.ToDouble(LongGradBox.Text);
                    Double vLongMinute = Convert.ToDouble(LongMinuteBox.Text);
                    Double vLongSec = Convert.ToDouble(LongSecBox.Text);

                    Double vNWLatGrad = Convert.ToDouble(NWLatGradBox.Text);
                    Double vNWLatMinute = Convert.ToDouble(NWLatMinuteBox.Text);
                    Double vNWLatSec = Convert.ToDouble(NWLatSecBox.Text);
                    Double vNWLongGrad = Convert.ToDouble(NWLongGradBox.Text);
                    Double vNWLongMinute = Convert.ToDouble(NWLongMinuteBox.Text);
                    Double vNWLongSec = Convert.ToDouble(NWLongSecBox.Text);

                    Double vSELatGrad = Convert.ToDouble(SELatGradBox.Text);
                    Double vSELatMinute = Convert.ToDouble(SELatMinuteBox.Text);
                    Double vSELatSec = Convert.ToDouble(SELatSecBox.Text);
                    Double vSELongGrad = Convert.ToDouble(SELongGradBox.Text);
                    Double vSELongMinute = Convert.ToDouble(SELongMinuteBox.Text);
                    Double vSELongSec = Convert.ToDouble(SELongSecBox.Text);

                    Double vAreaXSize = Convert.ToDouble(AreaSizeXBox.Text);
                    Double vAreaYSize = Convert.ToDouble(AreaSizeYBox.Text);

                    Int64 vZoomLevel = Convert.ToInt64(ZoomSelectorBox.Value);
                    Int64 vFetchLevel = Convert.ToInt64(FetchSelectorBox.Value);

                    Double vLatSign = 0.0;
                    Double vLongSign = 0.0;

                    Double vNWLatSign = 0.0;
                    Double vNWLongSign = 0.0;

                    Double vSELatSign = 0.0;
                    Double vSELongSign = 0.0;

                    //CheckSign
                    Boolean vCenterSignInvalid = (((!EarthCommon.StringCompare(LatSignBox.Text, "N")) &&  //yeah I confess I had fun with this condition! ;)
                                                   (!EarthCommon.StringCompare(LatSignBox.Text, "S"))) ||
                                                  ((!EarthCommon.StringCompare(LongSignBox.Text, "E")) &&
                                                   (!EarthCommon.StringCompare(LongSignBox.Text, "W"))));

                    Boolean vNWSignInvalid = (((!EarthCommon.StringCompare(NWLatSignBox.Text, "N")) &&
                                                   (!EarthCommon.StringCompare(NWLatSignBox.Text, "S"))) ||
                                                  ((!EarthCommon.StringCompare(NWLongSignBox.Text, "E")) &&
                                                   (!EarthCommon.StringCompare(NWLongSignBox.Text, "W"))));

                    Boolean vSESignInvalid = (((!EarthCommon.StringCompare(SELatSignBox.Text, "N")) &&
                                                   (!EarthCommon.StringCompare(SELatSignBox.Text, "S"))) ||
                                                  ((!EarthCommon.StringCompare(SELongSignBox.Text, "E")) &&
                                                   (!EarthCommon.StringCompare(SELongSignBox.Text, "W"))));

                    if (vCenterSignInvalid || vNWSignInvalid || vSESignInvalid)
                    {
                        MarkSpotCoordsInvalid();
                    }
                    else
                    {
                        if (EarthCommon.StringCompare(LatSignBox.Text, "N"))
                        {
                            vLatSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(LatSignBox.Text, "S"))
                        {
                            vLatSign = -1.0;
                        }
                        if (EarthCommon.StringCompare(LongSignBox.Text, "E"))
                        {
                            vLongSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(LongSignBox.Text, "W"))
                        {
                            vLongSign = -1.0;
                        }

                        if (EarthCommon.StringCompare(NWLatSignBox.Text, "N"))
                        {
                            vNWLatSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(NWLatSignBox.Text, "S"))
                        {
                            vNWLatSign = -1.0;
                        }
                        if (EarthCommon.StringCompare(NWLongSignBox.Text, "E"))
                        {
                            vNWLongSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(NWLongSignBox.Text, "W"))
                        {
                            vNWLongSign = -1.0;
                        }

                        if (EarthCommon.StringCompare(SELatSignBox.Text, "N"))
                        {
                            vSELatSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(SELatSignBox.Text, "S"))
                        {
                            vSELatSign = -1.0;
                        }
                        if (EarthCommon.StringCompare(SELongSignBox.Text, "E"))
                        {
                            vSELongSign = 1.0;
                        }
                        if (EarthCommon.StringCompare(SELongSignBox.Text, "W"))
                        {
                            vSELongSign = -1.0;
                        }

                        //CheckGrad and AreaSize
                        if ((vLatGrad > 90.0) ||
                            (vLatGrad < 0.0) ||
                            (vLongGrad > 180.0) ||
                            (vLongGrad < 0.0) ||
                            (vNWLatGrad > 90.0) ||
                            (vNWLatGrad < 0.0) ||
                            (vNWLongGrad > 180.0) ||
                            (vNWLongGrad < 0.0) ||
                            (vSELatGrad > 90.0) ||
                            (vSELatGrad < 0.0) ||
                            (vSELongGrad > 180.0) ||
                            (vSELongGrad < 0.0) ||
                            (vAreaXSize > 22000.0) ||
                            (vAreaXSize < 0.0) ||  //hmm we have to deactivate (0.0 instead 0.1) the min areasize check here because it blocks the 2 point processing
                            (vAreaYSize > 11000.0) ||
                            (vAreaYSize < 0.0))
                        {
                            MarkSpotCoordsInvalid();
                        }
                        else
                        {
                            //CheckMinutes and Seconds
                            if ((vLatMinute >= 60.0) ||
                                (vLatMinute < 0.0) ||
                                (vLongMinute >= 60.0) ||
                                (vLongMinute < 0.0) ||
                                (vNWLatMinute >= 60.0) ||
                                (vNWLatMinute < 0.0) ||
                                (vNWLongMinute >= 60.0) ||
                                (vNWLongMinute < 0.0) ||
                                (vSELatMinute >= 60.0) ||
                                (vSELatMinute < 0.0) ||
                                (vSELongMinute >= 60.0) ||
                                (vSELongMinute < 0.0) ||
                                (vLatSec >= 60.0) ||
                                (vLatSec < 0.0) ||
                                (vLongSec >= 60.0) ||
                                (vLongSec < 0.0) ||
                                (vNWLatSec >= 60.0) ||
                                (vNWLatSec < 0.0) ||
                                (vNWLongSec >= 60.0) ||
                                (vNWLongSec < 0.0) ||
                                (vSELatSec >= 60.0) ||
                                (vSELatSec < 0.0) ||
                                (vSELongSec >= 60.0) ||
                                (vSELongSec < 0.0))
                            {
                                MarkSpotCoordsInvalid();
                            }
                            else
                            {
                                //We may Have a Valid Spot Target
                                Double vLat = vLatSign * (vLatGrad + (1.0 / 60.0) * vLatMinute + (1.0 / 3600.0) * vLatSec);
                                Double vLong = vLongSign * (vLongGrad + (1.0 / 60.0) * vLongMinute + (1.0 / 3600.0) * vLongSec);
                                Double vNWLat = vNWLatSign * (vNWLatGrad + (1.0 / 60.0) * vNWLatMinute + (1.0 / 3600.0) * vNWLatSec);
                                Double vNWLong = vNWLongSign * (vNWLongGrad + (1.0 / 60.0) * vNWLongMinute + (1.0 / 3600.0) * vNWLongSec);
                                Double vSELat = vSELatSign * (vSELatGrad + (1.0 / 60.0) * vSELatMinute + (1.0 / 3600.0) * vSELatSec);
                                Double vSELong = vSELongSign * (vSELongGrad + (1.0 / 60.0) * vSELongMinute + (1.0 / 3600.0) * vSELongSec);

                                if ((vLat > 90.0) ||
                                    (vLat < -90.0) ||
                                    (vLong > 180.0) ||
                                    (vLong < -180.0) ||
                                    (vNWLat > 90.0) ||
                                    (vNWLat < -90.0) ||
                                    (vNWLong > 180.0) ||
                                    (vNWLong < -180.0) ||
                                    (vSELat > 90.0) ||
                                    (vSELat < -90.0) ||
                                    (vSELong > 180.0) ||
                                    (vSELong < -180.0) ||
                                    (vNWLat < vSELat) ||        //No warp in Lat possible (but in long it is allowed)
                                    (vNWLong > vSELong))        //Nope in Long forbidden now also
                                {
                                    //Nope not valid
                                    MarkSpotCoordsInvalid();
                                }
                                else
                                {
                                    if ((vZoomLevel < -4) ||
                                        (vZoomLevel > 18) || //aaa
                                        (vFetchLevel < -4) ||
                                        (vFetchLevel > 8))
                                    {
                                        MarkSpotCoordsInvalid();
                                    }
                                    else
                                    {

                                        //wohaa great it's valid!
                                        EarthConfig.mSelectedSceneryCompiler = CompilerSelectorBox.Text;
                                        EarthConfig.SetCompileScenery(CompileSceneryBox.Text);
                                        EarthConfig.SetCreateMask(CreateMasksBox.Text);
                                        EarthConfig.SetAutoReferenceMode(AutoRefSelectorBox.Text);
                                        EarthConfig.SetCreateScenproc(CreateScenprocBox.Text);

                                        if (CacheSceneryBox.Text=="Yes") 
                                        {
                                            EarthConfig.mUseCache = true;
                                        }
                                        else
                                        {
                                            EarthConfig.mUseCache=false;
                                        }

                                        CoordsValidityBox.Text = "ok";
                                        CoordsValidityBox.ForeColor = Color.DarkGreen;
                                        mInputCoordsValidity = true;

                                        if (EarthCommon.StringCompare(AreaDefModeBox.Text, "2Points"))
                                        {
                                            //2Point input
                                            mEarthInputArea.AreaStartLatitude = vNWLat;
                                            mEarthInputArea.AreaStartLongitude = vNWLong;
                                            mEarthInputArea.AreaStopLatitude = vSELat;
                                            mEarthInputArea.AreaStopLongitude = vSELong;
                                        }
                                        else
                                        {
                                            //1point input
                                            mEarthInputArea.SpotLatitude = vLat;
                                            mEarthInputArea.SpotLongitude = vLong;
                                            mEarthInputArea.AreaXSize = vAreaXSize;
                                            mEarthInputArea.AreaYSize = vAreaYSize;
                                        }

                                        EarthConfig.mZoomLevel = vZoomLevel;
                                        EarthConfig.mFetchLevel = vFetchLevel;

                                        EarthConfig.SetAreaSnap(AreaSnapBox.Text);
                                        EarthConfig.SetService(ServiceBox.Text);

                                        EarthConfig.mWorkFolder    = WorkFolderBox.Text;
                                        EarthConfig.mSceneryFolder = SceneryFolderBox.Text;

                                        ServiceSourcesLabel.Text = "Service Sources " + Convert.ToString(EarthConfig.mServiceVariationsCount[EarthConfig.mSelectedService - 1]);
                                        EarthConfig.mDisplayMode = DisplayModeBox.Text;
                                        
                                        CalculateAreaCoords(); //Do it before and after else it planes wrong the first time when data changed

                                        

                                        HandleAutoReferenceArea();
                                        
                                        CalculateAreaCoords(); //after to leave proper
                                        // Add code to set areas info

                                        // Only set the process areas flags for certain input changes
                                        if (!mSkipAreaProcessFlag)
                                        {
                                            AreaProcessInfo = new ProcessAreas();



                                            for (int AreaX = 0; AreaX < mEarthMultiArea.AreaCountInX; AreaX++)
                                            {
                                                for (int AreaY = 0; AreaY < mEarthMultiArea.AreaCountInY; AreaY++)
                                                {
                                                    ProcessAreaInfo vTempAreaInfo = new ProcessAreaInfo();
                                                    vTempAreaInfo.AreaX = AreaX;
                                                    vTempAreaInfo.AreaY = AreaY;
                                                    vTempAreaInfo.ProcessArea = true;
                                                    AreaProcessInfo.AreaInfo.Add(vTempAreaInfo);
                                                }

                                            }
                                        }

                                        mSkipAreaProcessFlag = false;

                                        //Autodisplay removed. Display Tend's to blocks input on slow connections.
                                        //call DisplaySpot() if you like to have it back
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    MarkSpotCoordsInvalid();
                }
            }
        }


        void HandleAutoReferenceArea()
        {
            if (mMultiAreaMode && mAutoReference)
            {
                Int64 vMaxUnitsX = 0;
                Int64 vMaxUnitsY = 0;

                EarthArea vTempMultiAsSingleEarthArea = new EarthArea();
                vTempMultiAsSingleEarthArea.CalculateAreaSnapCoordsAndCode(mEarthInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                Double vStartLatitude = vTempMultiAsSingleEarthArea.AreaSnapStartLatitude;
                Double vStopLatitude = vTempMultiAsSingleEarthArea.AreaSnapStopLatitude;
                Double vStartLongitude = vTempMultiAsSingleEarthArea.AreaSnapStartLongitude;
                Double vStopLongitude = vTempMultiAsSingleEarthArea.AreaSnapStopLongitude;

                Double vStartLatitudeAbs = Math.Abs(vStartLatitude);
                Double vStopLatitudeAbs = Math.Abs(vStopLatitude);

                Boolean vTopLeft = true;  //Ture = Attach Reference Area on North West Corner

                if (vStartLatitudeAbs < vStopLatitudeAbs)
                {
                    vTopLeft = false; //attach Reference Area to South East Corner
                }


                EarthInputArea vUnitInputArea = new EarthInputArea();
                EarthArea vUnitArea = new EarthArea();

                vUnitInputArea.AreaStartLatitude = vStartLatitude;
                vUnitInputArea.AreaStopLatitude = vStopLatitude;
                vUnitInputArea.AreaStartLongitude = vStartLongitude;
                vUnitInputArea.AreaStopLongitude = vStopLongitude;

                //1 = stickt mode keeps exact coords as close as possible
                //2 = freedom of round to next larger count number
                //3 = freedom as in free mode
                //4 = Smallest unit

                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eLatLong)
                {
                    if (vTopLeft)
                    {
                        vUnitInputArea.AreaStopLongitude = vUnitInputArea.AreaStartLongitude + EarthMath.LatLongLongitudeResolution;
                        vUnitInputArea.AreaStopLatitude = vUnitInputArea.AreaStartLatitude - EarthMath.LatLongLatitudeResolution;
                    }
                    else
                    {
                        vUnitInputArea.AreaStartLongitude = vUnitInputArea.AreaStopLongitude - EarthMath.LatLongLongitudeResolution;
                        vUnitInputArea.AreaStartLatitude = vUnitInputArea.AreaStopLatitude + EarthMath.LatLongLatitudeResolution;
                    }
                    vUnitArea.CalculateAreaSnapCoordsAndCode(vUnitInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                    vMaxUnitsX = Convert.ToInt64((vTempMultiAsSingleEarthArea.AreaSnapStopLongitude - vTempMultiAsSingleEarthArea.AreaSnapStartLongitude) / (vUnitArea.AreaSnapStopLongitude - vUnitArea.AreaSnapStartLongitude));
                    vMaxUnitsY = Convert.ToInt64((vTempMultiAsSingleEarthArea.AreaSnapStopLatitude - vTempMultiAsSingleEarthArea.AreaSnapStartLatitude) / (vUnitArea.AreaSnapStopLatitude - vUnitArea.AreaSnapStartLatitude));

                }
                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eLOD13)
                {
                    if (vTopLeft)
                    {
                        vUnitInputArea.AreaStopLongitude = vUnitInputArea.AreaStartLongitude + EarthMath.LOD13LongitudeResolution;
                        vUnitInputArea.AreaStopLatitude = vUnitInputArea.AreaStartLatitude - EarthMath.LOD13LatitudeResolution;
                    }
                    else
                    {
                        vUnitInputArea.AreaStartLongitude = vUnitInputArea.AreaStopLongitude - EarthMath.LOD13LongitudeResolution;
                        vUnitInputArea.AreaStartLatitude = vUnitInputArea.AreaStopLatitude + EarthMath.LOD13LatitudeResolution;
                    }
                    vUnitArea.CalculateAreaSnapCoordsAndCode(vUnitInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                    vMaxUnitsX = Convert.ToInt64((vTempMultiAsSingleEarthArea.AreaSnapStopLongitude - vTempMultiAsSingleEarthArea.AreaSnapStartLongitude) / (vUnitArea.AreaSnapStopLongitude - vUnitArea.AreaSnapStartLongitude));
                    vMaxUnitsY = Convert.ToInt64((vTempMultiAsSingleEarthArea.AreaSnapStopLatitude - vTempMultiAsSingleEarthArea.AreaSnapStartLatitude) / (vUnitArea.AreaSnapStopLatitude - vUnitArea.AreaSnapStartLatitude));
                }
                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eTiles)
                {
                    if (vTopLeft)
                    {
                        //Int64 vCodeX = EarthMath.GetAreaCodeX(vUnitInputArea.AreaStartLongitude, EarthConfig.mFetchLevel);
                        //Int64 vCodeY = EarthMath.GetAreaCodeY(vUnitInputArea.AreaStartLatitude, EarthConfig.mFetchLevel);
                        //vUnitInputArea.AreaStopLongitude = EarthMath.GetAreaTileRightLongitude(vCodeX, EarthConfig.mFetchLevel);
                        //vUnitInputArea.AreaStopLatitude = EarthMath.GetAreaTileBottomLatitude(vCodeY, EarthConfig.mFetchLevel);
                        Int64 vCodeX = EarthMath.GetAreaCodeX(vUnitInputArea.AreaStopLongitude, EarthConfig.mFetchLevel);
                        Int64 vCodeY = EarthMath.GetAreaCodeY(vUnitInputArea.AreaStopLatitude, EarthConfig.mFetchLevel);
                        vUnitInputArea.AreaStartLongitude = EarthMath.GetAreaTileLeftLongitude(vCodeX - 1, EarthConfig.mFetchLevel);
                        vUnitInputArea.AreaStartLatitude = EarthMath.GetAreaTileTopLatitude(vCodeY - 1, EarthConfig.mFetchLevel);
                    }
                    else
                    {
                        //Int64 vCodeX = EarthMath.GetAreaCodeX(vUnitInputArea.AreaStopLongitude, EarthConfig.mFetchLevel);
                        //Int64 vCodeY = EarthMath.GetAreaCodeY(vUnitInputArea.AreaStopLatitude, EarthConfig.mFetchLevel);
                        //vUnitInputArea.AreaStartLongitude = EarthMath.GetAreaTileLeftLongitude(vCodeX - 1, EarthConfig.mFetchLevel);
                        //vUnitInputArea.AreaStartLatitude = EarthMath.GetAreaTileTopLatitude(vCodeY - 1, EarthConfig.mFetchLevel);

                        Int64 vCodeX = EarthMath.GetAreaCodeX(vUnitInputArea.AreaStartLongitude, EarthConfig.mFetchLevel);
                        Int64 vCodeY = EarthMath.GetAreaCodeY(vUnitInputArea.AreaStartLatitude, EarthConfig.mFetchLevel);
                        vUnitInputArea.AreaStopLongitude = EarthMath.GetAreaTileRightLongitude(vCodeX, EarthConfig.mFetchLevel);
                        vUnitInputArea.AreaStopLatitude = EarthMath.GetAreaTileBottomLatitude(vCodeY, EarthConfig.mFetchLevel);
                    }
                    vUnitArea.CalculateAreaSnapCoordsAndCode(vUnitInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);
                    vMaxUnitsX = Convert.ToInt64((vTempMultiAsSingleEarthArea.AreaSnapStopLongitude - vTempMultiAsSingleEarthArea.AreaSnapStartLongitude) / (vUnitArea.AreaSnapStopLongitude - vUnitArea.AreaSnapStartLongitude));
                    vMaxUnitsY = Convert.ToInt64((EarthMath.GetNormedMercatorY(vTempMultiAsSingleEarthArea.AreaSnapStopLatitude) - EarthMath.GetNormedMercatorY(vTempMultiAsSingleEarthArea.AreaSnapStartLatitude)) / (EarthMath.GetNormedMercatorY(vUnitArea.AreaSnapStopLatitude) - EarthMath.GetNormedMercatorY(vUnitArea.AreaSnapStartLatitude)));
                }

                //Pixel Snap we don't support  i.e. we handle it like free mode.
                AreaInfo vTestAreaInfo = new AreaInfo(0, 0);
                EarthArea vTestEarthArea = new EarthArea();
                EarthInputArea vInputArea = new EarthInputArea();
                EarthInputArea vTestInputArea = new EarthInputArea();
                EarthArea vEarthArea = new EarthArea();


                if (((vStopLongitude - vStartLongitude) != 0.0) &&
                    ((vStopLatitude - vStartLatitude) != 0.0))  //Else we risk division through 0
                {

                    //find largest good Reference
                    vEarthArea.Copy(mEarthMultiArea);

                    Int64 vMultix = 0;
                    Int64 vMultiy = 0;
                    Int32 vAttempts = 0;
                    Boolean vSolutionFound = false;

                    Double vXFactor = 1.0;
                    Double vYFactor = 1.0;

                    if ((EarthConfig.mAutoReferenceMode == 4) && (EarthConfig.mAreaSnapMode != tAreaSnapMode.eOff) && (EarthConfig.mAreaSnapMode != tAreaSnapMode.ePixel))
                    {
                        vXFactor = (Double)(vMaxUnitsX);
                        vYFactor = (Double)(vMaxUnitsY);
                    }

                    while ((!vSolutionFound) && (vAttempts < 100000))
                    {

                        vAttempts++;

                        if ((EarthConfig.mAreaSnapMode != tAreaSnapMode.eTiles) &&
                            (EarthConfig.mAreaSnapMode != tAreaSnapMode.ePixel))
                        {
                            //Off, LatLong, LOD13
                            if (vTopLeft)
                            {
                                vInputArea.AreaStartLatitude = vStartLatitude;
                                vInputArea.AreaStopLatitude = vStartLatitude + (vStopLatitude - vStartLatitude) / vYFactor;
                                vInputArea.AreaStartLongitude = vStartLongitude;
                                vInputArea.AreaStopLongitude = vStartLongitude + (vStopLongitude - vStartLongitude) / vXFactor;
                            }
                            else
                            {
                                vInputArea.AreaStartLatitude = vStopLatitude + (vStartLatitude - vStopLatitude) / vYFactor;
                                vInputArea.AreaStopLatitude = vStopLatitude;
                                vInputArea.AreaStartLongitude = vStopLongitude + (vStartLongitude - vStopLongitude) / vXFactor;
                                vInputArea.AreaStopLongitude = vStopLongitude;
                            }
                        }
                        else
                        {
                            //Tiles, Pixels
                            if (vTopLeft)
                            {
                                vInputArea.AreaStartLatitude = vStartLatitude;
                                vInputArea.AreaStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(EarthMath.GetNormedMercatorY(vStartLatitude) + (EarthMath.GetNormedMercatorY(vStopLatitude) - EarthMath.GetNormedMercatorY(vStartLatitude)) / vYFactor);
                                vInputArea.AreaStartLongitude = vStartLongitude;
                                vInputArea.AreaStopLongitude = vStartLongitude + (vStopLongitude - vStartLongitude) / vXFactor;
                            }
                            else
                            {
                                vInputArea.AreaStartLatitude = vStopLatitude + (vStartLatitude - vStopLatitude) / vYFactor;
                                vInputArea.AreaStopLatitude = vStopLatitude;
                                vInputArea.AreaStartLongitude = vStopLongitude + (vStartLongitude - vStopLongitude) / vXFactor;
                                vInputArea.AreaStopLongitude = vStopLongitude;
                            }
                        }

                        vEarthArea.CalculateAreaSnapCoordsAndCode(vInputArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                        if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                        {
                            vSolutionFound = mEarthAreaTexture.IsAreaSizeOk(vEarthArea, tMemoryAllocTestMode.eResampler, 0.99);
                        }
                        else
                        {
                            vSolutionFound = mEarthAreaTexture.IsAreaSizeOk(vEarthArea, tMemoryAllocTestMode.eNormal, 0.99);
                        }

                        if (vSolutionFound)
                        {
                            //complet Input Area Information
                            vInputArea.SpotLongitude = 0.5 * (vInputArea.AreaStartLongitude + vInputArea.AreaStopLongitude);
                            vInputArea.SpotLatitude = 0.5 * (vInputArea.AreaStartLatitude + vInputArea.AreaStopLatitude);
                            vInputArea.AreaYSize = GetAreaYSizeInNm(vInputArea.AreaStartLatitude, vInputArea.AreaStopLatitude);
                            vInputArea.AreaXSize = GetAreaXSizeInNm(vInputArea.AreaStartLatitude, vInputArea.AreaStopLatitude, vInputArea.AreaStartLongitude, vInputArea.AreaStopLongitude);

                            mEarthSingleReferenceInputArea.Copy(vInputArea); //Asign Input Reference First

                            CalculateAreaSnapCoordsAndCode(); // calculate all (Multi and Single) here

                            mSingleReferenceAreaValid = true;

                            //So Now Check Full Multi Area if every single area is Green (should be)

                            vMultix = mEarthMultiArea.GetMultiAreasCountInX(mEarthSingleReferenceArea);
                            vMultiy = mEarthMultiArea.GetMultiAreasCountInY(mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode);

                            Int64 vXAreaAdd = vMultix - 1;

                            if (vXAreaAdd <= 0)
                            {
                                vXAreaAdd = 1;
                            }

                            if (vMultix * vMultiy <= cMaxDrawnAndHandledAreas)
                            {
                                for (Int64 vAreaX = 0; vAreaX < vMultix; vAreaX += vXAreaAdd) // don't go through x just check two coloumns
                                {
                                    for (Int64 vAreaY = 0; vAreaY < vMultiy; vAreaY++) //with Y in the inner Loop we find non working faster (it's almost only Y dependent)
                                    {
                                        vTestAreaInfo = new AreaInfo(vAreaX, vAreaY);
                                        vTestEarthArea = mEarthMultiArea.CalculateSingleAreaFormMultiArea(vTestAreaInfo, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                                        if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                                        {
                                            vSolutionFound = mEarthAreaTexture.IsAreaSizeOk(vTestEarthArea, tMemoryAllocTestMode.eResampler, 0.99);
                                        }
                                        else
                                        {
                                            vSolutionFound = mEarthAreaTexture.IsAreaSizeOk(vTestEarthArea, tMemoryAllocTestMode.eNormal, 0.99);
                                        }
                                        if (!vSolutionFound)
                                        {
                                            break;
                                        }
                                    }
                                    if (!vSolutionFound)
                                    {
                                        break;
                                    }
                                }
                            }

                        }

                        if (!vSolutionFound)
                        {
                            Boolean vRefMode2 = (EarthConfig.mAutoReferenceMode == 2);

                            if ((EarthConfig.mAreaSnapMode == tAreaSnapMode.eOff) || (EarthConfig.mAreaSnapMode == tAreaSnapMode.ePixel) || (EarthConfig.mAutoReferenceMode == 3))
                            {
                                if (vEarthArea.AreaPixelsInX > vEarthArea.AreaPixelsInY)
                                {
                                    vXFactor += 1.0;
                                }
                                else
                                {
                                    vYFactor += 1.0;
                                }
                            }
                            else
                            {
                                if ((vXFactor >= (Double)(vMaxUnitsX)) && (vYFactor >= (Double)(vMaxUnitsY)))
                                {
                                    vAttempts = 100000; //Abort
                                }
                                else
                                {
                                    if (vRefMode2)
                                    {
                                        //RefMode2
                                        if (vEarthArea.AreaPixelsInX > vEarthArea.AreaPixelsInY)
                                        {
                                            if (vXFactor < (Double)(vMaxUnitsX))
                                            {
                                                vXFactor += 1.0;
                                                while ((vMaxUnitsX % (Int32)(vXFactor) != 0) && ((vMaxUnitsX + 1) % (Int32)(vXFactor) != 0))
                                                {
                                                    vXFactor += 1.0;
                                                }
                                            }
                                            else
                                            {
                                                if (vYFactor < (Double)(vMaxUnitsY))
                                                {
                                                    vYFactor += 1.0;
                                                    while ((vMaxUnitsY % (Int32)(vYFactor) != 0) && ((vMaxUnitsY + 1) % (Int32)(vYFactor) != 0))
                                                    {
                                                        vYFactor += 1.0;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (vYFactor < (Double)(vMaxUnitsY))
                                            {
                                                vYFactor += 1.0;
                                                while ((vMaxUnitsY % (Int32)(vYFactor) != 0) && ((vMaxUnitsY + 1) % (Int32)(vYFactor) != 0))
                                                {
                                                    vYFactor += 1.0;
                                                }
                                            }
                                            else
                                            {
                                                if (vXFactor < (Double)(vMaxUnitsX))
                                                {
                                                    vXFactor += 1.0;
                                                    while ((vMaxUnitsX % (Int32)(vXFactor) != 0) && ((vMaxUnitsX + 1) % (Int32)(vXFactor) != 0))
                                                    {
                                                        vXFactor += 1.0;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //RefMode1
                                        if (vEarthArea.AreaPixelsInX > vEarthArea.AreaPixelsInY)
                                        {
                                            if (vXFactor < (Double)(vMaxUnitsX))
                                            {
                                                vXFactor += 1.0;
                                                while (vMaxUnitsX % (Int32)(vXFactor) != 0)
                                                {
                                                    vXFactor += 1.0;
                                                }
                                            }
                                            else
                                            {
                                                if (vYFactor < (Double)(vMaxUnitsY))
                                                {
                                                    vYFactor += 1.0;
                                                    while (vMaxUnitsY % (Int32)(vYFactor) != 0)
                                                    {
                                                        vYFactor += 1.0;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (vYFactor < (Double)(vMaxUnitsY))
                                            {
                                                vYFactor += 1.0;
                                                while (vMaxUnitsY % (Int32)(vYFactor) != 0)
                                                {
                                                    vYFactor += 1.0;
                                                }
                                            }
                                            else
                                            {
                                                if (vXFactor < (Double)(vMaxUnitsX))
                                                {
                                                    vXFactor += 1.0;
                                                    while (vMaxUnitsX % (Int32)(vXFactor) != 0)
                                                    {
                                                        vXFactor += 1.0;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (vSolutionFound)
                    {
                        //Already all done.. we could simple continue;
                        //But better recalculate everything although the caller is doing this.
                        //CalculateAreaCoords(); //Does Caller already
                        //DisplayWhereYouJustAreAgain();
                    }
                    else
                    {
                        //should not happen if memory isn't usead all aup completly on startup.
                        SetStatus(" Auto Reference gives up! Too large or Out of Memory!? Switching to Manual mode!");
                        Thread.Sleep(3000);

                        SetFixMode();

                    }
                }
            }
        }

        void SetFixOrAutoModeVisibility()
        {
            if (mAutoReference)
            {
                SetAutoMode();
            }
            else
            {
                SetFixMode();
            }
        }

        void SetFixMode()
        {
            mAutoReference = false;

            RefModeButton.Invalidate();
            //RefModeButton.Text = "Fix";
            RefModeButton.Text = "*Fix*";
            RefModeButton.ForeColor = System.Drawing.Color.SaddleBrown;
            RefModeButton.Refresh();

            AutoModeLabel.Visible = false;
            AutoRefSelectorBox.Visible = false;
            AutoModeLabel.Refresh();
            AutoRefSelectorBox.Refresh();
        }

        void SetAutoMode()
        {
            mAutoReference = true;

            RefModeButton.Invalidate();
            //RefModeButton.Text = "Auto";
            RefModeButton.Text = "*Auto*";
            RefModeButton.ForeColor = System.Drawing.Color.DarkGreen;
            RefModeButton.Refresh();

            if ((EarthConfig.mAreaSnapMode != tAreaSnapMode.eOff) &&
                (EarthConfig.mAreaSnapMode != tAreaSnapMode.ePixel))
            {
                AutoModeLabel.Visible = true;
                AutoRefSelectorBox.Visible = true;
                AutoModeLabel.Refresh();
                AutoRefSelectorBox.Refresh();
            }
            else
            {
                AutoModeLabel.Visible = false;
                AutoRefSelectorBox.Visible = false;
                AutoModeLabel.Refresh();
                AutoRefSelectorBox.Refresh();
            }
        }

        void DisplayGrid(Graphics iGraphics, Int64 iPixelWide, Int64 iPixelHeight, Double iFromLatitude, Double iToLatitude, Double iFromLongitude, Double iToLongitude)
        {
            try
            {
                const Int64 vMaxTotalGridLinesDisplay = 100; //if there are more then do not draw the grid

                Pen GridPen = new Pen(Color.FromArgb(128, 128, 128, 128), (float)1);

                Double vToLongitudeAntiWarp = iToLongitude;
                if (vToLongitudeAntiWarp <= iFromLongitude)
                {
                    vToLongitudeAntiWarp = vToLongitudeAntiWarp + 360.0;
                }

                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.ePixel)
                {
                    //Pixel
                    Int64 vCodeXStart = EarthMath.GetAreaCodeX(iFromLongitude, EarthConfig.mFetchLevel);
                    Int64 vCodeXStop = EarthMath.GetAreaCodeX(vToLongitudeAntiWarp, EarthConfig.mFetchLevel);
                    Int64 vCodeYStart = EarthMath.GetAreaCodeY(iFromLatitude, EarthConfig.mFetchLevel);
                    Int64 vCodeYStop = EarthMath.GetAreaCodeY(iToLatitude, EarthConfig.mFetchLevel);

                    Int64 vXPixelPosStart = EarthMath.GetPixelPosWithinTileX(iFromLongitude, EarthConfig.mFetchLevel);
                    Int64 vXPixelPosStop = EarthMath.GetPixelPosWithinTileX(vToLongitudeAntiWarp, EarthConfig.mFetchLevel) + 1;
                    Int64 vYPixelPosStart = EarthMath.GetPixelPosWithinTileY(iFromLatitude, EarthConfig.mFetchLevel);
                    Int64 vYPixelPosStop = EarthMath.GetPixelPosWithinTileY(iToLatitude, EarthConfig.mFetchLevel) + 1;

                    Int64 vGridLinesTotal = 256 * (vCodeXStop - vCodeXStart - 1) + 256 * (vCodeYStop - vCodeYStart - 1) + (256 - vXPixelPosStart) + vXPixelPosStop + 1 + (256 - vYPixelPosStart) + vYPixelPosStop + 1;

                    if (vGridLinesTotal <= vMaxTotalGridLinesDisplay)
                    {
                        for (Int64 vXCode = vCodeXStart; vXCode <= vCodeXStop; vXCode++)
                        {
                            Double vTileLongitude = EarthMath.GetAreaTileLeftLongitude(vXCode, EarthConfig.mFetchLevel);
                            for (Int64 vXPixelPos = 0; vXPixelPos <= 255; vXPixelPos++)
                            {
                                Double vLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(vTileLongitude, vXPixelPos, EarthConfig.mFetchLevel);
                                Double vXDisplayPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, vLongitude, EarthConfig.mZoomLevel); //display is on zoomlvl
                                iGraphics.DrawLine(GridPen, (float)vXDisplayPixelPos, (float)0, (float)vXDisplayPixelPos, (float)iPixelHeight);

                            }
                        }
                        //LastLine
                        Double vFinalLongitude = EarthMath.GetAreaTileRightLongitude(vCodeXStop, EarthConfig.mFetchLevel);
                        Double vXFinalDisplayPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, vFinalLongitude, EarthConfig.mZoomLevel);
                        iGraphics.DrawLine(GridPen, (float)vXFinalDisplayPixelPos, (float)0, (float)vXFinalDisplayPixelPos, (float)iPixelHeight);

                        for (Int64 vYCode = vCodeYStart; vYCode <= vCodeYStop; vYCode++)
                        {
                            Double vTileLatitude = EarthMath.GetAreaTileTopLatitude(vYCode, EarthConfig.mFetchLevel);
                            for (Int64 vYPixelPos = 0; vYPixelPos <= 255; vYPixelPos++)
                            {
                                Double vLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(vTileLatitude, -vYPixelPos, EarthConfig.mFetchLevel);
                                Double vYDisplayPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, vLatitude, EarthConfig.mZoomLevel); //display is on zoomlvl
                                iGraphics.DrawLine(GridPen, (float)0, (float)vYDisplayPixelPos, (float)iPixelWide, (float)vYDisplayPixelPos);

                            }
                        }
                        //LastLine
                        Double vFinalLatitude = EarthMath.GetAreaTileBottomLatitude(vCodeYStop, EarthConfig.mFetchLevel);
                        Double vYFinalDisplayPixelPos = EarthMath.GetYPixelDistance(iFromLatitude, vFinalLatitude, EarthConfig.mZoomLevel);
                        iGraphics.DrawLine(GridPen, (float)0, (float)vYFinalDisplayPixelPos, (float)iPixelWide, (float)vYFinalDisplayPixelPos);
                    }
                }
                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eTiles)
                {
                    //Tiles (Displayed Grid is bounf on mFetchLevel)
                    Int64 vCodeXStart = EarthMath.GetAreaCodeX(iFromLongitude, EarthConfig.mFetchLevel);
                    Int64 vCodeXStop = EarthMath.GetAreaCodeX(vToLongitudeAntiWarp, EarthConfig.mFetchLevel);
                    Int64 vCodeYStart = EarthMath.GetAreaCodeY(iFromLatitude, EarthConfig.mFetchLevel);
                    Int64 vCodeYStop = EarthMath.GetAreaCodeY(iToLatitude, EarthConfig.mFetchLevel);

                    Int64 vGridLinesTotal = (vCodeXStop - vCodeXStart) + 1 + (vCodeYStop - vCodeYStart) + 1;

                    if (vGridLinesTotal <= vMaxTotalGridLinesDisplay)
                    {
                        for (Int64 vXCode = vCodeXStart; vXCode <= (vCodeXStop + 1); vXCode++)
                        {
                            Double vLongitude = EarthMath.GetAreaTileLeftLongitude(vXCode, EarthConfig.mFetchLevel);
                            Double vXPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, vLongitude, EarthConfig.mZoomLevel); //display is on zoomlvl
                            iGraphics.DrawLine(GridPen, (float)vXPixelPos, (float)0, (float)vXPixelPos, (float)iPixelHeight);
                        }
                        for (Int64 vYCode = vCodeYStart; vYCode <= (vCodeYStop + 1); vYCode++)
                        {
                            Double vLatitude = EarthMath.GetAreaTileTopLatitude(vYCode, EarthConfig.mFetchLevel);
                            Double vYPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, vLatitude, EarthConfig.mZoomLevel);
                            iGraphics.DrawLine(GridPen, (float)0, (float)vYPixelPos, (float)iPixelWide, (float)vYPixelPos);
                        }
                    }
                }
                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eLatLong)
                {
                    //LatLong
                    Double vGridStartLongitude = Math.Truncate(iFromLongitude * EarthMath.InvLatLongLongitudeResolution) * EarthMath.LatLongLongitudeResolution;
                    Double vGridStopLongitude = (Math.Truncate(vToLongitudeAntiWarp * EarthMath.InvLatLongLongitudeResolution) + 1.0) * EarthMath.LatLongLongitudeResolution;
                    Double vGridStartLatitude = (Math.Truncate(iFromLatitude * EarthMath.InvLatLongLatitudeResolution) + 1.0) * EarthMath.LatLongLatitudeResolution;
                    Double vGridStopLatitude = Math.Truncate(iToLatitude * EarthMath.InvLatLongLatitudeResolution) * EarthMath.LatLongLatitudeResolution;

                    Int64 vGridLinesTotal = Convert.ToInt64((Math.Truncate((vGridStopLongitude - vGridStartLongitude) * EarthMath.InvLatLongLongitudeResolution + (vGridStartLatitude - vGridStopLatitude) * EarthMath.InvLatLongLatitudeResolution)));

                    if (vGridLinesTotal <= vMaxTotalGridLinesDisplay)
                    {
                        for (Double vLongGrid = vGridStartLongitude; vLongGrid <= vGridStopLongitude; vLongGrid += EarthMath.LatLongLongitudeResolution)
                        {
                            Double vXPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, vLongGrid, EarthConfig.mZoomLevel);
                            iGraphics.DrawLine(GridPen, (float)vXPixelPos, (float)0, (float)vXPixelPos, (float)iPixelHeight);
                        }
                        for (Double vLatGrid = vGridStartLatitude; vLatGrid >= vGridStopLatitude; vLatGrid -= EarthMath.LatLongLatitudeResolution)
                        {
                            Double vYPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, vLatGrid, EarthConfig.mZoomLevel);
                            iGraphics.DrawLine(GridPen, (float)0, (float)vYPixelPos, (float)iPixelWide, (float)vYPixelPos);
                        }
                    }
                }
                if (EarthConfig.mAreaSnapMode == tAreaSnapMode.eLOD13)
                {
                    //Lod13
                    Double vGridStartLongitude = Math.Truncate((iFromLongitude + 180.0) * EarthMath.InvLOD13LongitudeResolution) * EarthMath.LOD13LongitudeResolution - 180.0;
                    Double vGridStopLongitude = (Math.Truncate((vToLongitudeAntiWarp + 180.0) * EarthMath.InvLOD13LongitudeResolution) + 1.0) * EarthMath.LOD13LongitudeResolution - 180.0;

                    //carefully we map with negative Latitude cords here so the sign turns
                    Double vGridStartLatitude = Math.Truncate((iFromLatitude - 90.0) * EarthMath.InvLOD13LatitudeResolution) * EarthMath.LOD13LatitudeResolution + 90.0;
                    Double vGridStopLatitude = (Math.Truncate((iToLatitude - 90.0) * EarthMath.InvLOD13LatitudeResolution) - 1.0) * EarthMath.LOD13LatitudeResolution + 90.0;

                    Int64 vGridLinesTotal = Convert.ToInt64((Math.Truncate((vGridStopLongitude - vGridStartLongitude) * EarthMath.LOD13LongitudeResolution + (vGridStartLatitude - vGridStopLatitude) * EarthMath.InvLOD13LatitudeResolution)));

                    if (vGridLinesTotal <= vMaxTotalGridLinesDisplay)
                    {
                        for (Double vLongGrid = vGridStartLongitude; vLongGrid <= vGridStopLongitude; vLongGrid += EarthMath.LOD13LongitudeResolution)
                        {
                            Double vXPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, vLongGrid, EarthConfig.mZoomLevel);
                            iGraphics.DrawLine(GridPen, (float)vXPixelPos, (float)0, (float)vXPixelPos, (float)iPixelHeight);
                        }
                        for (Double vLatGrid = vGridStartLatitude; vLatGrid >= vGridStopLatitude; vLatGrid -= EarthMath.LOD13LatitudeResolution)
                        {
                            Double vYPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, vLatGrid, EarthConfig.mZoomLevel);
                            iGraphics.DrawLine(GridPen, (float)0, (float)vYPixelPos, (float)iPixelWide, (float)vYPixelPos);
                        }
                    }
                }
            }
            catch
            {
                //
            }
        }

        void DisplayDateAndDrawBorder(Graphics iGraphics, Int64 iPixelWide, Int64 iPixelHeight, Double iFromLatitude, Double iToLatitude, Double iFromLongitude, Double iToLongitude)
        {
            try
            {
                Pen vDateBorderPen = new Pen(Color.FromArgb(194, 128, 64, 64), (float)1);

                const Double cEpsilon = 1e-12;

                if ((iToLongitude + cEpsilon <= iFromLongitude) ||
                    (iToLongitude - cEpsilon <= iFromLongitude))
                {
                    Double vXPixelPos = EarthMath.GetXPixelDistance(iFromLongitude, 180.0, EarthConfig.mZoomLevel);
                    iGraphics.DrawLine(vDateBorderPen, (float)vXPixelPos, (float)0, (float)vXPixelPos, (float)iPixelHeight);
                }
                if ((iFromLatitude >= EarthMath.MercatorProjectionCut) && (iToLatitude < EarthMath.MercatorProjectionCut))
                {
                    Double vYPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, EarthMath.MercatorProjectionCut, EarthConfig.mZoomLevel);
                    iGraphics.DrawLine(vDateBorderPen, (float)0, (float)vYPixelPos, (float)iPixelWide, (float)vYPixelPos);
                }
                if ((iFromLatitude > -EarthMath.MercatorProjectionCut) && (iToLatitude <= -EarthMath.MercatorProjectionCut))
                {
                    Double vYPixelPos = -EarthMath.GetYPixelDistance(iFromLatitude, -EarthMath.MercatorProjectionCut, EarthConfig.mZoomLevel);
                    iGraphics.DrawLine(vDateBorderPen, (float)0, (float)vYPixelPos, (float)iPixelWide, (float)vYPixelPos);
                }
            }
            catch
            {
                //
            }
        }


        Double GetOnDisplayPixelPosY(Double iLatitude, Int64 iDisplayHeight)
        {
            Int64 vDisplayPixelCenterY = iDisplayHeight >> 1;
            Double vOnDisplayPixelPosY = Convert.ToDouble(vDisplayPixelCenterY) - EarthMath.GetYPixelDistance(mDisplayCenterLatitude, iLatitude, EarthConfig.mZoomLevel);
            return vOnDisplayPixelPosY;
        }

        Double GetOnDisplayPixelPosX(Double iLongitude, Int64 iDisplayWidth)
        {
            Int64 vDisplayPixelCenterX = iDisplayWidth >> 1;
            Double vOnDisplayPixelPosX = Convert.ToDouble(vDisplayPixelCenterX) + EarthMath.GetXPixelDistance(mDisplayCenterLongitude, iLongitude, EarthConfig.mZoomLevel);
            return vOnDisplayPixelPosX;
        }

        Double GetAlternativeOnDisplayPixelPosX(Double iLongitude, Int64 iDisplayWidth)
        {
            Int64 vDisplayPixelCenterX = iDisplayWidth >> 1;
            Double vDisplayCenterLongitude = mDisplayCenterLongitude;
            if (vDisplayCenterLongitude <= 0.0)
            {
                vDisplayCenterLongitude += 360.0;
            }
            else
            {
                vDisplayCenterLongitude -= 360.0;
            }

            Double vOnDisplayPixelPosX = Convert.ToDouble(vDisplayPixelCenterX) + EarthMath.GetXPixelDistance(vDisplayCenterLongitude, iLongitude, EarthConfig.mZoomLevel);
            return vOnDisplayPixelPosX;
        }

        void DisplayThisFreePosition()
        {
            //Free Position Display. Working for every Display size larger or equal 256x256 pixel. That means you can increase the display size if you like. But mind the speed.

            String vDisplayLatitude;
            String vDisplayLongitude;
            String vDisplayString;

            vDisplayLatitude = EarthMath.GetSignLatitude(mDisplayCenterLatitude);
            vDisplayLatitude += EarthMath.GetWorldCoordGrad(mDisplayCenterLatitude) + "° ";
            vDisplayLatitude += EarthMath.GetWorldCoordMinute(mDisplayCenterLatitude) + "' ";
            vDisplayLatitude += EarthMath.GetWorldCoordSec(mDisplayCenterLatitude) + "'' ";
            vDisplayLongitude = EarthMath.GetSignLongitude(mDisplayCenterLongitude);
            vDisplayLongitude += EarthMath.GetWorldCoordGrad(mDisplayCenterLongitude) + "° ";
            vDisplayLongitude += EarthMath.GetWorldCoordMinute(mDisplayCenterLongitude) + "' ";
            vDisplayLongitude += EarthMath.GetWorldCoordSec(mDisplayCenterLongitude) + "'' ";

            vDisplayString = "   " + vDisplayLatitude + "    " + vDisplayLongitude;

            if (mAllowDisplayToSetStatus)
            {
                SetStatus(vDisplayString);
            }

            Int64 vDisplayAreaCodeX = EarthMath.GetAreaCodeX(mDisplayCenterLongitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeY = EarthMath.GetAreaCodeY(mDisplayCenterLatitude, EarthConfig.mZoomLevel);

            //Display DrawingBitmap Dimensions (depends on zoom lvl..critical the whole world)
            Int64 vDrawingBoardHeight = NormalPictureBox.Size.Height;
            Int64 vDrawingBoardWidth = NormalPictureBox.Size.Width;
            
            Int64 vWorldPixelsCount = 256*EarthMath.GetAreaCodeSize(EarthConfig.mZoomLevel);

            Boolean vOverRun = false;

            if (vDrawingBoardWidth > vWorldPixelsCount)
            {
                vDrawingBoardWidth = vWorldPixelsCount;
                vOverRun = true;
            }
            if (vDrawingBoardHeight > vWorldPixelsCount)
            {
                vDrawingBoardHeight = vWorldPixelsCount;
            }

            Int64 vDisplayPixelCenterX = vDrawingBoardWidth >> 1; //well not really center but the reference point!
            Int64 vDisplayPixelCenterY = vDrawingBoardHeight >> 1;

            Double vDisplayAreaTopLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, vDisplayPixelCenterY, EarthConfig.mZoomLevel);
            Double vDisplayAreaBottomLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -(vDrawingBoardHeight - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vDisplayAreaRightLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, (vDrawingBoardWidth - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
            Double vDisplayAreaLeftLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, -vDisplayPixelCenterX, EarthConfig.mZoomLevel);


            //is an Epsilon required here?
            vDisplayAreaTopLatitude = EarthMath.CleanLatitude(vDisplayAreaTopLatitude);
            vDisplayAreaBottomLatitude = EarthMath.CleanLatitude(vDisplayAreaBottomLatitude);
            vDisplayAreaRightLongitude = EarthMath.CleanLongitude(vDisplayAreaRightLongitude);
            vDisplayAreaLeftLongitude = EarthMath.CleanLongitude(vDisplayAreaLeftLongitude);

            Int64 vDisplayAreaCodeXStart = EarthMath.GetAreaCodeX(vDisplayAreaLeftLongitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeXStop = EarthMath.GetAreaCodeX(vDisplayAreaRightLongitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeYStart = EarthMath.GetAreaCodeY(vDisplayAreaTopLatitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeYStop = EarthMath.GetAreaCodeY(vDisplayAreaBottomLatitude, EarthConfig.mZoomLevel);

            Int64 vXPixelPosDisplayAreaStart = EarthMath.GetPixelPosWithinTileX(vDisplayAreaLeftLongitude, EarthConfig.mZoomLevel);
            Int64 vYPixelPosDisplayAreaStart = EarthMath.GetPixelPosWithinTileY(vDisplayAreaTopLatitude, EarthConfig.mZoomLevel);

            Int64 vOverBorderDisplayAreaCodeX = vDisplayAreaCodeXStop;
            if ((vDisplayAreaRightLongitude < vDisplayAreaLeftLongitude) || vOverRun) //Then We have a longitudinal overrun
            {
                vOverBorderDisplayAreaCodeX += EarthMath.GetAreaCodeSize(EarthConfig.mZoomLevel);
            }

            Int64 vDisplayAreaTilesInX = vOverBorderDisplayAreaCodeX - vDisplayAreaCodeXStart + 1;
            Int64 vDisplayAreaTilesInY = vDisplayAreaCodeYStop - vDisplayAreaCodeYStart + 1;

            Bitmap vDisplayAreaBitmap = new Bitmap((Int32)(vDrawingBoardWidth), (Int32)(vDrawingBoardHeight));
            Graphics vDisplayAreaGraphics = Graphics.FromImage(vDisplayAreaBitmap);


            //Ok we have defined the Display Area, now go and get the Tiles for the Display

            Int64 vDisplayAreaCodeXBorder = EarthMath.GetAreaCodeSize(EarthConfig.mZoomLevel);

            Int64 vDisplayAreaCodeXArea = vDisplayAreaCodeXStart;
            Int64 vDisplayAreaCodeYArea = vDisplayAreaCodeYStart;
            for (Int64 vCountY = 0; vCountY <= vDisplayAreaTilesInY - 1; vCountY++)
            {
                vDisplayAreaCodeXArea = vDisplayAreaCodeXStart;
                for (Int64 vCountX = 0; vCountX <= vDisplayAreaTilesInX - 1; vCountX++)
                {

                    TileInfo vTileInfo = null;
                    if (EarthConfig.layServiceMode)
                    {
                        vTileInfo = new TileInfo(vDisplayAreaCodeXArea, vDisplayAreaCodeYArea, EarthConfig.mZoomLevel, EarthConfig.layServiceSelected,false);
                    }
                    else
                    {
                        vTileInfo = new TileInfo(vDisplayAreaCodeXArea, vDisplayAreaCodeYArea, EarthConfig.mZoomLevel, EarthConfig.mSelectedService,false);
                    }

                    if (mDisplayTileCache.IsTileInCache(vTileInfo))
                    {
                        Bitmap vTileBitmapReference = mDisplayTileCache.GetTileBitmapReference(vTileInfo);
                        //vDisplayAreaGraphics.DrawImage(vTileBitmapReference, (Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256);
                        vDisplayAreaGraphics.DrawImage(vTileBitmapReference, new Rectangle((Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256), new Rectangle(0, 0, 256, 256), GraphicsUnit.Pixel);

                        try
                        {
                            //doesnt seems to be required ..oh well fine
                          //vDisplayAreaGraphics.DrawImage(vTileBitmapReference, new Rectangle((Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart - (Int32)vWorldPixelsCount, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256), new Rectangle(0, 0, 256, 256), GraphicsUnit.Pixel);
                          //vDisplayAreaGraphics.DrawImage(vTileBitmapReference, new Rectangle((Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart + (Int32)vWorldPixelsCount, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256), new Rectangle(0, 0, 256, 256), GraphicsUnit.Pixel);
                        }
                        catch
                        {
                            //do nothing
                        }

                    }
                    else
                    {
                        mTileInfoDisplayQueue.AddTileInfoNoDoubles(vTileInfo);
                        //vDisplayAreaGraphics.DrawImage(mTileRequested, (Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256);
                        vDisplayAreaGraphics.DrawImage(mTileRequested, new Rectangle((Int32)vCountX * 256 - (Int32)vXPixelPosDisplayAreaStart, (Int32)vCountY * 256 - (Int32)vYPixelPosDisplayAreaStart, 256, 256), new Rectangle(0, 0, 256, 256), GraphicsUnit.Pixel);


                    }


                    vDisplayAreaCodeXArea++;
                    if (vDisplayAreaCodeXArea >= vDisplayAreaCodeXBorder)
                    {
                        vDisplayAreaCodeXArea -= vDisplayAreaCodeXBorder;
                    }

                }
                vDisplayAreaCodeYArea++;
            }

            //AddGrid
            DisplayGrid(vDisplayAreaGraphics, vDrawingBoardWidth, vDrawingBoardHeight, vDisplayAreaTopLatitude, vDisplayAreaBottomLatitude, vDisplayAreaLeftLongitude, vDisplayAreaRightLongitude);

            //AddDate Border
            DisplayDateAndDrawBorder(vDisplayAreaGraphics, vDrawingBoardWidth, vDrawingBoardHeight, vDisplayAreaTopLatitude, vDisplayAreaBottomLatitude, vDisplayAreaLeftLongitude, vDisplayAreaRightLongitude);

            try
            {
                //Add corsshair and such sweetness
                const Int32 cCrosshairLength = 50;
                Pen CrosshairPen = new Pen(Color.FromArgb(255, 0, 0, 0), 3);
                Pen CornerPointPen = new Pen(Color.FromArgb(196, 0, 0, 255), 3);
                Pen CenterPointPen = new Pen(Color.FromArgb(196, 0, 255, 0), 3);

                Pen SnappedAreaPen = new Pen(Color.FromArgb(128, 64, 128, 0), 3);
                Pen SnappedAreaPenRed = new Pen(Color.FromArgb(128, 192, 64, 0), 3);
                SolidBrush SnappedAreaBrush = new SolidBrush(Color.FromArgb(64, 64, 128, 0));
                SolidBrush SnappedAreaBrushRed = new SolidBrush(Color.FromArgb(64, 192, 64, 0));
                SolidBrush EnteredAreaBrush = new SolidBrush(Color.FromArgb(64, 0, 64, 200));


                Pen SnappedMultiAreaPen = new Pen(Color.FromArgb(128, 128, 128, 64), 3);
                SolidBrush SnappedMultiAreaBrush = new SolidBrush(Color.FromArgb(64, 128, 128, 64));

                vDisplayAreaGraphics.DrawLine(CrosshairPen, vDisplayPixelCenterX, 0, vDisplayPixelCenterX, cCrosshairLength);
                vDisplayAreaGraphics.DrawLine(CrosshairPen, vDisplayPixelCenterX, vDrawingBoardHeight, vDisplayPixelCenterX, vDrawingBoardHeight - cCrosshairLength);
                vDisplayAreaGraphics.DrawLine(CrosshairPen, 0, vDisplayPixelCenterY, cCrosshairLength, vDisplayPixelCenterY);
                vDisplayAreaGraphics.DrawLine(CrosshairPen, vDrawingBoardWidth, vDisplayPixelCenterY, vDrawingBoardWidth - cCrosshairLength, vDisplayPixelCenterY);
                vDisplayAreaGraphics.DrawEllipse(CrosshairPen, vDisplayPixelCenterX - 1, vDisplayPixelCenterY - 1, 3, 3);

                Double vPixelPerLongitude = EarthMath.GetPixelPerLongitude(EarthConfig.mZoomLevel);
                Double vPixelPerNormedMercatorY = EarthMath.GetPixelPerNormedMercatorY(EarthConfig.mZoomLevel);

                Int32 vMode = 0;

                for (Int64 vTwoModecon = 1; vTwoModecon <= 2; vTwoModecon++)
                {
                    //Snapped Area
                    Double vAreaSnapDisplayPixelTop = GetOnDisplayPixelPosY(mEarthArea.AreaSnapStartLatitude, vDrawingBoardHeight);
                    Double vAreaSnapDisplayPixelBottom = GetOnDisplayPixelPosY(mEarthArea.AreaSnapStopLatitude, vDrawingBoardHeight);
                    Double vAreaSnapDisplayPixelLeft = GetOnDisplayPixelPosX(mEarthArea.AreaSnapStartLongitude, vDrawingBoardWidth);
                    Double vAreaSnapDisplayPixelRight = GetOnDisplayPixelPosX(mEarthArea.AreaSnapStopLongitude, vDrawingBoardWidth);

                    if (vMode == 1)
                    {
                        vAreaSnapDisplayPixelLeft = GetAlternativeOnDisplayPixelPosX(mEarthArea.AreaSnapStartLongitude, vDrawingBoardWidth);
                        vAreaSnapDisplayPixelRight = GetAlternativeOnDisplayPixelPosX(mEarthArea.AreaSnapStopLongitude, vDrawingBoardWidth);
                    }

                    //Entered Area
                    Double vAreaEnteredDisplayPixelTop = GetOnDisplayPixelPosY(mEarthInputArea.AreaStartLatitude, vDrawingBoardHeight);
                    Double vAreaEnteredDisplayPixelBottom = GetOnDisplayPixelPosY(mEarthInputArea.AreaStopLatitude, vDrawingBoardHeight);
                    Double vAreaEnteredDisplayPixelLeft = GetOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, vDrawingBoardWidth);
                    Double vAreaEnteredDisplayPixelRight = GetOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, vDrawingBoardWidth);

                    if (vMode == 1)
                    {
                        vAreaEnteredDisplayPixelLeft = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, vDrawingBoardWidth);
                        vAreaEnteredDisplayPixelRight = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, vDrawingBoardWidth);
                    }

                    //Entered Center
                    Double vSpotDisplayPixelY = GetOnDisplayPixelPosY(mEarthInputArea.SpotLatitude, vDrawingBoardHeight);
                    Double vSpotDisplayPixelX = GetOnDisplayPixelPosX(mEarthInputArea.SpotLongitude, vDrawingBoardWidth);

                    if (vMode == 1)
                    {
                        vSpotDisplayPixelX = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.SpotLongitude, vDrawingBoardWidth);
                    }

                    if (mMultiAreaMode)
                    {
                        Int64 vMultix = mEarthMultiArea.GetMultiAreasCountInX(mEarthSingleReferenceArea);
                        Int64 vMultiy = mEarthMultiArea.GetMultiAreasCountInY(mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode);

                        Double vStepWidthLatitude = mEarthSingleReferenceArea.AreaSnapStopLatitude - mEarthSingleReferenceArea.AreaSnapStartLatitude;
                        Double vStepWidthLongitude = mEarthSingleReferenceArea.AreaSnapStopLongitude - mEarthSingleReferenceArea.AreaSnapStartLongitude;

                        Double vMultiStartLatitude = mEarthMultiArea.AreaSnapStartLatitude;
                        Double vMultiStopLatitude = mEarthMultiArea.AreaSnapStopLatitude;
                        Double vMultiStartLongitude = mEarthMultiArea.AreaSnapStartLongitude;
                        Double vMultiStopLongitude = mEarthMultiArea.AreaSnapStopLongitude;

                        //MerctorY (for Tile and Pixel Area snap mode)
                        Double vMultiStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthMultiArea.AreaSnapStartLatitude);
                        Double vReferenceStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthSingleReferenceArea.AreaSnapStartLatitude);
                        Double vReferenceStopLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthSingleReferenceArea.AreaSnapStopLatitude);
                        Double vStepWidthLatitudeNormedMercatorY = vReferenceStopLatitudeNormedMercatorY - vReferenceStartLatitudeNormedMercatorY;
                        Double vMultiStopLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY + Convert.ToDouble(vMultiy) * vStepWidthLatitudeNormedMercatorY;
                        Double vAreaSnapStartLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY;

                        vAreaSnapDisplayPixelTop = GetOnDisplayPixelPosY(vMultiStartLatitude, vDrawingBoardHeight);
                        vAreaSnapDisplayPixelBottom = GetOnDisplayPixelPosY(vMultiStopLatitude, vDrawingBoardHeight);
                        vAreaSnapDisplayPixelLeft = GetOnDisplayPixelPosX(vMultiStartLongitude, vDrawingBoardWidth);
                        vAreaSnapDisplayPixelRight = GetOnDisplayPixelPosX(vMultiStopLongitude, vDrawingBoardWidth);

                        if (vMode == 1)
                        {
                            vAreaSnapDisplayPixelLeft = GetAlternativeOnDisplayPixelPosX(vMultiStartLongitude, vDrawingBoardWidth);
                            vAreaSnapDisplayPixelRight = GetAlternativeOnDisplayPixelPosX(vMultiStopLongitude, vDrawingBoardWidth);
                        }

                        //Draw Multi area
                        vDisplayAreaGraphics.FillRectangle(SnappedMultiAreaBrush, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                        vDisplayAreaGraphics.DrawRectangle(SnappedMultiAreaPen, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);

                        if (vMultix * vMultiy <= cMaxDrawnAndHandledAreas)
                        {
                            AreaInfo vAreaInfo = new AreaInfo(0, 0);
                            EarthArea vEarthArea = new EarthArea();

                            for (Int64 vAreaY = 0; vAreaY < vMultiy; vAreaY++)
                            {
                                for (Int64 vAreaX = 0; vAreaX < vMultix; vAreaX++)
                                {

                                    for (int idx = 0; idx < AreaProcessInfo.AreaInfo.Count; idx++)
                                    {
                                        if ((AreaProcessInfo.AreaInfo[idx].AreaX == vAreaX) && (AreaProcessInfo.AreaInfo[idx].AreaY == vAreaY))
                                        {
                                            if (AreaProcessInfo.AreaInfo[idx].ProcessArea == false)
                                            {

                                                SnappedAreaBrush.Color=Color.FromArgb(160,Color.Red);
                                                SnappedAreaPen.Color = Color.FromArgb(128, Color.Red);

                                            } 

                                            else
                                            {
                                                SnappedAreaBrush.Color=Color.FromArgb(64, 192, 64, 0);
                                                SnappedAreaPen.Color=Color.FromArgb(128, 64, 128, 0);

                                            }
                                        }

                                    }
                                    
                                    
                                    vMultiStartLatitude = mEarthMultiArea.AreaSnapStartLatitude + Convert.ToDouble(vAreaY) * vStepWidthLatitude;
                                    vMultiStopLatitude = vMultiStartLatitude + vStepWidthLatitude;
                                    vMultiStartLongitude = mEarthMultiArea.AreaSnapStartLongitude + Convert.ToDouble(vAreaX) * vStepWidthLongitude;
                                    vMultiStopLongitude = vMultiStartLongitude + vStepWidthLongitude;

                                    if ((EarthConfig.mAreaSnapMode == tAreaSnapMode.eTiles) || (EarthConfig.mAreaSnapMode == tAreaSnapMode.ePixel))
                                    {
                                        vMultiStartLatitudeNormedMercatorY = vAreaSnapStartLatitudeNormedMercatorY + Convert.ToDouble(vAreaY) * vStepWidthLatitudeNormedMercatorY;
                                        vMultiStopLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY + vStepWidthLatitudeNormedMercatorY;
                                        vMultiStartLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vMultiStartLatitudeNormedMercatorY);
                                        vMultiStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vMultiStopLatitudeNormedMercatorY);
                                    }

                                    vAreaSnapDisplayPixelTop = GetOnDisplayPixelPosY(vMultiStartLatitude, vDrawingBoardHeight);
                                    vAreaSnapDisplayPixelBottom = GetOnDisplayPixelPosY(vMultiStopLatitude, vDrawingBoardHeight);
                                    vAreaSnapDisplayPixelLeft = GetOnDisplayPixelPosX(vMultiStartLongitude, vDrawingBoardWidth);
                                    vAreaSnapDisplayPixelRight = GetOnDisplayPixelPosX(vMultiStopLongitude, vDrawingBoardWidth);

                                    if (vMode == 1)
                                    {
                                        vAreaSnapDisplayPixelLeft = GetAlternativeOnDisplayPixelPosX(vMultiStartLongitude, vDrawingBoardWidth);
                                        vAreaSnapDisplayPixelRight = GetAlternativeOnDisplayPixelPosX(vMultiStopLongitude, vDrawingBoardWidth);
                                    }

                                    //Coloring
                                    vAreaInfo = new AreaInfo(vAreaX, vAreaY);
                                    vEarthArea = mEarthMultiArea.CalculateSingleAreaFormMultiArea(vAreaInfo, mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode, EarthConfig.mFetchLevel);

                                    Boolean vEnoughSpaceForProcessing = false;

                                    if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                                    {
                                        vEnoughSpaceForProcessing = mEarthAreaTexture.IsAreaSizeOk(vEarthArea, tMemoryAllocTestMode.eResampler, 1.0);
                                    }
                                    else
                                    {
                                        vEnoughSpaceForProcessing = mEarthAreaTexture.IsAreaSizeOk(vEarthArea, tMemoryAllocTestMode.eNormal, 1.0);
                                    }

                                    if (vEnoughSpaceForProcessing)
                                    {
                                        vDisplayAreaGraphics.FillRectangle(SnappedAreaBrush, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                                        vDisplayAreaGraphics.DrawRectangle(SnappedAreaPen, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                                    }
                                    else
                                    {
                                        vDisplayAreaGraphics.FillRectangle(SnappedAreaBrushRed, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                                        vDisplayAreaGraphics.DrawRectangle(SnappedAreaPenRed, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        //Draw Single area
                        //Coloring
                        Boolean vEnoughSpaceForProcessing = false;
                        if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
                        {
                            vEnoughSpaceForProcessing = mEarthAreaTexture.IsAreaSizeOk(mEarthArea, tMemoryAllocTestMode.eResampler, 1.0);
                        }
                        else
                        {
                            vEnoughSpaceForProcessing = mEarthAreaTexture.IsAreaSizeOk(mEarthArea, tMemoryAllocTestMode.eNormal, 1.0);
                        }
                        if (vEnoughSpaceForProcessing)
                        {
                            vDisplayAreaGraphics.FillRectangle(SnappedAreaBrush, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                            vDisplayAreaGraphics.DrawRectangle(SnappedAreaPen, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                        }
                        else
                        {
                            vDisplayAreaGraphics.FillRectangle(SnappedAreaBrushRed, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                            vDisplayAreaGraphics.DrawRectangle(SnappedAreaPenRed, (float)vAreaSnapDisplayPixelLeft, (float)vAreaSnapDisplayPixelTop, (float)vAreaSnapDisplayPixelRight - (float)vAreaSnapDisplayPixelLeft + (float)1, (float)vAreaSnapDisplayPixelBottom - (float)vAreaSnapDisplayPixelTop + (float)1);
                        }
                    }

                    //if (vMode == 0)
                    {
                        try
                        {
                            // Displays blue selected area rectangle and corner ellipses
                            vDisplayAreaGraphics.FillRectangle(EnteredAreaBrush, (float)vAreaEnteredDisplayPixelLeft, (float)vAreaEnteredDisplayPixelTop, (float)vAreaEnteredDisplayPixelRight - (float)vAreaEnteredDisplayPixelLeft + (float)1, (float)vAreaEnteredDisplayPixelBottom - (float)vAreaEnteredDisplayPixelTop + (float)1);
                            vDisplayAreaGraphics.DrawEllipse(CornerPointPen, (float)vAreaEnteredDisplayPixelLeft - (float)3, (float)vAreaEnteredDisplayPixelTop - (float)3, (float)7, (float)7);
                            vDisplayAreaGraphics.DrawEllipse(CornerPointPen, (float)vAreaEnteredDisplayPixelRight - (float)3, (float)vAreaEnteredDisplayPixelTop - (float)3, (float)7, (float)7);
                            vDisplayAreaGraphics.DrawEllipse(CornerPointPen, (float)vAreaEnteredDisplayPixelLeft - (float)3, (float)vAreaEnteredDisplayPixelBottom - (float)3, (float)7, (float)7);
                            vDisplayAreaGraphics.DrawEllipse(CornerPointPen, (float)vAreaEnteredDisplayPixelRight - (float)3, (float)vAreaEnteredDisplayPixelBottom - (float)3, (float)7, (float)7);
                            vDisplayAreaGraphics.DrawEllipse(CenterPointPen, (float)vSpotDisplayPixelX - (float)4, (float)vSpotDisplayPixelY - (float)4, (float)9, (float)9);
                        }
                        catch
                        {
                            //ignore overflow error that can happen on zoom -3 and -4 probabily due to large coords for float.
                        }
                    }

                    vMode++;
                }
            }
            catch
            {
                //
            }

            // Draw the exclude area and kml shapes
            DrawKMLShapes(ref vDisplayAreaGraphics, vDrawingBoardHeight, vDrawingBoardWidth);

            //and hand it over to the display

            
            DisplayPicture(vDisplayAreaBitmap);
            vDisplayAreaBitmap.Dispose();

            FeedThreadEnginesWithNewWork();
        }

        void DrawKMLShapes(ref Graphics vDisplayAreaGraphics, long vDrawingBoardHeight, long vDrawingBoardWidth)
        {
            //Excluded Areas
            Double vExcludeAreaDisplayPixelTop = 0;
            Double vExcludeAreaDisplayPixelBottom = 0;
            Double vExcludeAreaDisplayPixelLeft = 0;
            Double vExcludeAreaDisplayPixelRight = 0;
            SolidBrush UnSelectedExcludeAreaBrush = new SolidBrush(Color.FromArgb(96,Color.Orange ));
            SolidBrush SelectedExcludeAreaBrush = new SolidBrush(Color.FromArgb(96,Color.Purple  ));
            SolidBrush ExcludeAreaBrush;
            Pen ExcludeAreaPen = new Pen(Color.FromArgb(128, 128, 0, 0), 3);

            try
            {
                for (int vExcludeCounter = 0; vExcludeCounter < vExcludeAreas.Areas.Count; vExcludeCounter++)
                {
                    //double tempPixel = 0;

                    for (Int64 vMode = 0; vMode < 2; vMode++)
                    {
                        vExcludeAreaDisplayPixelTop = GetOnDisplayPixelPosY(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStartLatitude, vDrawingBoardHeight);
                        vExcludeAreaDisplayPixelBottom = GetOnDisplayPixelPosY(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStopLatitude, vDrawingBoardHeight);
                        vExcludeAreaDisplayPixelLeft = GetOnDisplayPixelPosX(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStartLongitude, vDrawingBoardWidth);
                        vExcludeAreaDisplayPixelRight = GetOnDisplayPixelPosX(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStopLongitude, vDrawingBoardWidth);

                        if (vMode == 1)
                        {
                            vExcludeAreaDisplayPixelLeft = GetAlternativeOnDisplayPixelPosX(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStartLongitude, vDrawingBoardWidth);
                            vExcludeAreaDisplayPixelRight = GetAlternativeOnDisplayPixelPosX(vExcludeAreas.Areas[vExcludeCounter].ExcludeAreaStopLongitude, vDrawingBoardWidth);
                        }

                        if (lsbExcludeAreas.SelectedItems.Count > 0)
                        {
                            if (lsbExcludeAreas.SelectedIndices[0] == vExcludeCounter)
                            {
                                ExcludeAreaBrush = SelectedExcludeAreaBrush;
                            }

                            else
                            {
                                ExcludeAreaBrush = UnSelectedExcludeAreaBrush;
                            }

                        }

                        else
                        {
                            ExcludeAreaBrush = UnSelectedExcludeAreaBrush;
                        }
                        vDisplayAreaGraphics.FillRectangle(ExcludeAreaBrush, (float)vExcludeAreaDisplayPixelLeft, (float)vExcludeAreaDisplayPixelTop, (float)vExcludeAreaDisplayPixelRight - (float)vExcludeAreaDisplayPixelLeft + (float)1, (float)vExcludeAreaDisplayPixelBottom - (float)vExcludeAreaDisplayPixelTop + (float)1);
                        vDisplayAreaGraphics.DrawRectangle(ExcludeAreaPen, (float)vExcludeAreaDisplayPixelLeft, (float)vExcludeAreaDisplayPixelTop, (float)vExcludeAreaDisplayPixelRight - (float)vExcludeAreaDisplayPixelLeft + (float)1, (float)vExcludeAreaDisplayPixelBottom - (float)vExcludeAreaDisplayPixelTop + (float)1);
                    }
                }
            }

            catch
            {
                // Do nothing
            }

            // Test if we are in map move mode
            // Dont draw kml when dragging map to increase speed
            Boolean DisplayKML=true;
            if (mMouseDownFlag )
            {
                if (!mUserDrawExcludeArea && !mUserSetCenterActive && !mUserDrawAreaActive)
                {
                    DisplayKML = false;
                }
             
            }

            if (DisplayKML)
            {
                // Draw the outline of the KML Area 
                try
                {

                    Double vAreaDisplayPixelX1 = 0;
                    Double vAreaDisplayPixelX2 = 0;
                    Double vAreaDisplayPixelY1 = 0;
                    Double vAreaDisplayPixelY2 = 0;

                    for (int vAreaCounter = 0; vAreaCounter < mEarthKML.Areas.Count; vAreaCounter++)
                    {

                        PointF[] AreasPoints = new PointF[mEarthKML.Areas[vAreaCounter].Coords.Count];
                        Boolean FirstPixel = true;
                        Pen AreaPen = new Pen(Color.Purple, 2);
                        SolidBrush AreaBrush = new SolidBrush(Color.FromArgb(64, Color.Yellow));
                        int vCoordCounter = 0;
                        vDisplayAreaGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        vDisplayAreaGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                        vDisplayAreaGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;


                        foreach (KMLCoords PointCoord in mEarthKML.Areas[vAreaCounter])
                        {
                            vAreaDisplayPixelY1 = GetOnDisplayPixelPosY(PointCoord.mLat, vDrawingBoardHeight);
                            vAreaDisplayPixelX1 = GetOnDisplayPixelPosX(PointCoord.mLon, vDrawingBoardWidth);

                            // Area is a polygon shape, so add points to array for later
                            if (mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Area", StringComparison.CurrentCultureIgnoreCase) >= 0)
                            {
                                AreasPoints[vCoordCounter].X = (float)vAreaDisplayPixelX1;
                                AreasPoints[vCoordCounter].Y = (float)vAreaDisplayPixelY1;
                                AreaPen.Color = Color.Yellow;
                                AreaBrush = new SolidBrush(Color.FromArgb(48, Color.Yellow));
                            }

                            else if (mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Exclude", StringComparison.CurrentCultureIgnoreCase) >= 0)
                            {
                                AreasPoints[vCoordCounter].X = (float)vAreaDisplayPixelX1;
                                AreasPoints[vCoordCounter].Y = (float)vAreaDisplayPixelY1;
                                AreaPen.Color = Color.Red;
                                AreaBrush = new SolidBrush(Color.FromArgb(48, Color.Red));
                            }

                            // We need two points if drawing a line
                            if (!FirstPixel) // This is not the first pixel, so we can draw the line
                            {

                                int idx = 0;

                                idx = mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Coast", StringComparison.CurrentCultureIgnoreCase);
                                if (idx >= 0)
                                {
                                    AreaPen.Color = Color.Blue;
                                }

                                idx = mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Pool", StringComparison.CurrentCultureIgnoreCase);
                                if (idx >= 0)
                                {
                                    AreaPen.Color = Color.DarkCyan;
                                }

                                idx = mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Deepwater", StringComparison.CurrentCultureIgnoreCase);
                                if (idx >= 0)
                                {
                                    AreaPen.Color = Color.DarkBlue;
                                }

                                vDisplayAreaGraphics.DrawLine(AreaPen, (float)vAreaDisplayPixelX1, (float)vAreaDisplayPixelY1, (float)vAreaDisplayPixelX2, (float)vAreaDisplayPixelY2);

                            }

                            else // This is the first don't draw
                            {
                                FirstPixel = false;
                            }
                            vAreaDisplayPixelY2 = vAreaDisplayPixelY1;
                            vAreaDisplayPixelX2 = vAreaDisplayPixelX1;
                            vCoordCounter++;

                        }

                        // Draw filled polygon shapes
                        if ((mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Area", StringComparison.CurrentCultureIgnoreCase) >= 0) | (mEarthKML.Areas[vAreaCounter].AreaName.IndexOf("Exclude", StringComparison.CurrentCultureIgnoreCase) >= 0))
                        {
                            vDisplayAreaGraphics.DrawPolygon(AreaPen, AreasPoints);
                            vDisplayAreaGraphics.FillPolygon(AreaBrush, AreasPoints);
                        }

                    }
                }

                catch
                {
                    // Do nothing

                }

            }

        }


        void DisplayThisPositionTile()
        {

            Int64 vDisplayAreaCodeX = EarthMath.GetAreaCodeX(mDisplayCenterLongitude, EarthConfig.mZoomLevel);
            Int64 vDisplayAreaCodeY = EarthMath.GetAreaCodeY(mDisplayCenterLatitude, EarthConfig.mZoomLevel);

            String vCode = "ABCD";
            String VMapCode = EarthScriptsHandler.MapAreaCoordToTileCode(vDisplayAreaCodeX, vDisplayAreaCodeY, EarthConfig.mZoomLevel, vCode);

            if (mAllowDisplayToSetStatus)
            {
                SetStatus(VMapCode);
            }


            TileInfo vTileInfo = new TileInfo(vDisplayAreaCodeX, vDisplayAreaCodeY, EarthConfig.mZoomLevel, EarthConfig.mSelectedService,false);

            if (mDisplayTileCache.IsTileInCache(vTileInfo))
            {
                Bitmap vTileBitmapReference = mDisplayTileCache.GetTileBitmapReference(vTileInfo);
                DisplayPicture(vTileBitmapReference);
            }
            else
            {
                Tile vDisplayTile = FetchAreaCodeTile(vDisplayAreaCodeX, vDisplayAreaCodeY, EarthConfig.mZoomLevel, EarthConfig.mSelectedService);
                if (vDisplayTile.IsGoodBitmap())
                {
                    mDisplayTileCache.AddTileOverwriteOldTiles(vDisplayTile);
                }
                DisplayPicture(vDisplayTile.GetBitmapReference());
                vDisplayTile.FreeBitmap();
            }

        }


        void LimitDisplayCenterLatitude(ref Double xDisplayCenterLatitude)
        {
            Int64 vWorldPixels = 256 * EarthMath.GetAreaCodeSize(EarthConfig.mZoomLevel);
            Double vCenterPixelDistance = (Double)(vWorldPixels >> 1);

            if (NormalPictureBox.Size.Height < vWorldPixels)
            {
                vCenterPixelDistance = (Double)(NormalPictureBox.Size.Height >> 1);
            }
            
            Double vMaxLatitudeDisplayCenter = EarthMath.GetLatitudeFromLatitudeAndPixel(EarthMath.MercatorProjectionCut, -vCenterPixelDistance, EarthConfig.mZoomLevel);
            
            if (xDisplayCenterLatitude < -vMaxLatitudeDisplayCenter)
            {
                xDisplayCenterLatitude = -vMaxLatitudeDisplayCenter;
            }

            if (xDisplayCenterLatitude > vMaxLatitudeDisplayCenter)
            {
                xDisplayCenterLatitude = vMaxLatitudeDisplayCenter;
            }

        }


        void DisplayThisPosition(Double iLongitude, Double iLatitude)
        {
            mDisplayCenterLongitude = iLongitude;
            mDisplayCenterLatitude = iLatitude;

            LimitDisplayCenterLatitude(ref mDisplayCenterLatitude);

            if (EarthCommon.StringCompare(EarthConfig.mDisplayMode, "Free"))
            {
                //Free mode
                DisplayThisFreePosition();
            }
            else
            {
                //Tile mode
                DisplayThisPositionTile();
            }
        }



        void DisplaySpot() //Center
        {
            if (mInputCoordsValidity)
            {
                DisplayThisPosition(mEarthInputArea.SpotLongitude, mEarthInputArea.SpotLatitude);
            }
        }

        void DisplayWhereYouJustAreAgain()  //or in simple word refresh and keep position :)
        {
            if (mInputCoordsValidity)
            {
                DisplayThisPosition(mDisplayCenterLongitude, mDisplayCenterLatitude);
            }
        }


        void DisplaySnappedNorthWestCorner()
        {
            if (mInputCoordsValidity)
            {
                if (mMultiAreaMode)
                {
                    DisplayThisPosition(mEarthMultiArea.AreaSnapStartLongitude, mEarthMultiArea.AreaSnapStartLatitude);
                }
                else
                {
                    DisplayThisPosition(mEarthArea.AreaSnapStartLongitude, mEarthArea.AreaSnapStartLatitude);
                }
            }
        }

        void DisplaySnappedSouthEastCorner()
        {
            if (mInputCoordsValidity)
            {
                if (mMultiAreaMode)
                {
                    DisplayThisPosition(mEarthMultiArea.AreaSnapStopLongitude, mEarthMultiArea.AreaSnapStopLatitude);
                }
                else
                {
                    DisplayThisPosition(mEarthArea.AreaSnapStopLongitude, mEarthArea.AreaSnapStopLatitude);
                }
            }
        }

        void DisplaySnappedNorthEastCorner()
        {
            if (mInputCoordsValidity)
            {
                if (mMultiAreaMode)
                {
                    DisplayThisPosition(mEarthMultiArea.AreaSnapStopLongitude, mEarthMultiArea.AreaSnapStartLatitude);
                }
                else
                {
                    DisplayThisPosition(mEarthArea.AreaSnapStopLongitude, mEarthArea.AreaSnapStartLatitude);
                }
            }
        }

        void DisplaySnappedSouthWestCorner()
        {
            if (mInputCoordsValidity)
            {
                if (mMultiAreaMode)
                {
                    DisplayThisPosition(mEarthMultiArea.AreaSnapStartLongitude, mEarthMultiArea.AreaSnapStopLatitude);
                }
                else
                {
                    DisplayThisPosition(mEarthArea.AreaSnapStartLongitude, mEarthArea.AreaSnapStopLatitude);
                }
            }
        }

        void DisplayEnteredNorthWestCorner()
        {
            if (mInputCoordsValidity)
            {
                DisplayThisPosition(mEarthInputArea.AreaStartLongitude, mEarthInputArea.AreaStartLatitude);
            }
        }

        void DisplayEnteredSouthEastCorner()
        {
            if (mInputCoordsValidity)
            {
                DisplayThisPosition(mEarthInputArea.AreaStopLongitude, mEarthInputArea.AreaStopLatitude);
            }
        }

        void DisplayEnteredNorthEastCorner()
        {
            if (mInputCoordsValidity)
            {
                DisplayThisPosition(mEarthInputArea.AreaStopLongitude, mEarthInputArea.AreaStartLatitude);
            }
        }

        void DisplayEnteredSouthWestCorner()
        {
            if (mInputCoordsValidity)
            {
                if (mMultiAreaMode)
                {
                    DisplayThisPosition(mEarthMultiArea.AreaSnapStartLongitude, mEarthMultiArea.AreaSnapStopLatitude);
                }
                else
                {
                    DisplayThisPosition(mEarthArea.AreaSnapStartLongitude, mEarthArea.AreaSnapStopLatitude);
                }
            }
        }

        private Boolean IsAreaSelectionOk()
        {
            if ((mEarthInputArea.AreaXSize > 0.0) && (mEarthInputArea.AreaYSize > 0.0))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void StartDownload()
        {
            bool shouldStart = !mAreaProcessRunning && !ScenprocUtils.ScenProcRunning
                && ((mMasksCompilerMultithreadedQueue == null && mImageProcessingMultithreadedQueue == null)
                || MultiThreadedQueuesFinished()) && (mImageToolThread == null
                || ImageToolThreadUnstarted() || ImageToolThreadFinished());

            if (shouldStart)
            {
                if (mInputCoordsValidity)
                {
                    if (IsAreaSelectionOk())
                    {
                        if (PrepareWorkingDirectory(true)) // true means for start
                        {
                            if (PrepareSceneryDirectory())
                            {
                                if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FSX/P3D"))
                                {
                                    EarthConfig.mSceneryCompiler = EarthConfig.mFSXSceneryCompiler;
                                    EarthConfig.mSceneryImageTool = "";
                                }
                                if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FS2004"))
                                {
                                    EarthConfig.mSceneryCompiler = EarthConfig.mFS2004SceneryCompiler;
                                    EarthConfig.mSceneryImageTool = EarthConfig.mFS2004SceneryImageTool;
                                }

                                if (EarthConfig.mCreateScenproc)
                                {
                                    if (File.Exists(EarthConfig.mScenprocLoc))
                                    {
                                        string fs9ScriptLoc = ScenprocUtils.scriptsDir + @"\" + EarthConfig.mScenprocFS9Script;
                                        string fsxp3dScriptLoc = ScenprocUtils.scriptsDir + @"\" + EarthConfig.mScenprocFSXP3DScript;
                                        if (EarthConfig.mSelectedSceneryCompiler == "FS2004" && File.Exists(fs9ScriptLoc))
                                        {
                                            ScenprocUtils.RunScenprocThreaded(mEarthMultiArea, EarthConfig.mScenprocLoc, EarthConfig.mScenprocFS9Script, EarthConfig.mWorkFolder);
                                        }
                                        else if (EarthConfig.mSelectedSceneryCompiler == "FSX/P3D" && File.Exists(fsxp3dScriptLoc))
                                        {
                                            ScenprocUtils.RunScenprocThreaded(mEarthMultiArea, EarthConfig.mScenprocLoc, EarthConfig.mScenprocFSXP3DScript, EarthConfig.mWorkFolder);
                                        }
                                    }
                                }
                                if (SceneryCompilerReady())
                                {
                                    
                                    mMasksCompilerMultithreadedQueue = new MultiThreadedQueue(EarthConfig.mMaxResampleThreads);
                                    mMasksCompilerMultithreadedQueue.jobHandler = RunMasksAndSceneryCompiler;

                                    mImageProcessingMultithreadedQueue = new MultiThreadedQueue(EarthConfig.mMaxImageProcessingThreads);
                                    mImageProcessingMultithreadedQueue.jobHandler = RunImageProcessing;
                                    if (EarthConfig.mSceneryCompiler == EarthConfig.mFS2004SceneryCompiler)
                                    {
                                        // reset image tool thread
                                        ThreadStart vImageToolThreadDelegate = new ThreadStart(RunImageToolProcessing);
                                        mImageToolThread = new Thread(vImageToolThreadDelegate);
                                    }

                                    mCurrentAreaInfo = new AreaInfo(0, 0);
                                    mCurrentActiveAreaNr = 1;
                                    mCurrentDownloadedTilesTotal = 0;

                                    mAreaTilesInfoDownloadCheckList.Clear(); //clear before set the process running flag
                                    mStopProcess = false;
                                    mAreaProcessRunning = true;
                                    mApplicationExitCheck = true; // Aim exit check
                                    EmptyAllJobQueues();
                                    ResetTimeOutCounter();
                                    QueueMultiAreaAreas();
                                    if (mAreaProcessRunning)
                                    {
                                        CreateNextSingleAreaFromMultiArea();
                                        UpdateAreaAndTileCountStatusLabel();
                                        EarthScriptsHandler.DoOnStartButtonEvent(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);

                                        Boolean CarryOn = false;

                                        // Test if the single or multi area has at least 1 tile to download

                                        if (mAreaInfoAreaQueue.GetEntriesCount()>0)
                                            {
                                                // Test Multi areas
                                                while (!CarryOn)
                                                {

                                                    //if (AreaHasTilesToDownload(mEarthArea))
                                                    if ((CalculateTilesToDownload()>0)&&(CheckIfAreaIsEnabled()))
                                                    {
                                                        QueueAreaTiles();
                                                        CarryOn = true;
                                                        UpdateAreaAndTileCountStatusLabel();
                                                    }


                                                    else if (!mAreaInfoAreaQueue.IsEmpty())
                                                    {
                                                        mCurrentDownloadedTilesTotal += (mEarthArea.AreaTilesInX * mEarthArea.AreaTilesInY); 
                                                        CreateNextSingleAreaFromMultiArea();
                                                    }

                                                    else
                                                    {
                                                        
                                                        CarryOn = true;
                                                        mStopProcess = false;
                                                        mAreaProcessRunning = false;
                                                        SetStatus("You have excluded all tiles or have not defined a polygon named area if using a KML file!");
                                                        Thread.Sleep(1000);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Test a single area
                                                //if (AreaHasTilesToDownload(mEarthArea))
                                                if (CalculateTilesToDownload() > 0 && (CheckIfAreaIsEnabled()))
                                                {
                                                    QueueAreaTiles();
                                                }

                                                else
                                                {
                                                    mStopProcess = false;
                                                    mAreaProcessRunning = false;
                                                    SetStatus("You have excluded all tiles or have not defined a polygon named area if using a KML file!");
                                                    Thread.Sleep(1000);
                                                }

                                            }

                                       
                                    }
                                    //old: FetchAndDisplayAreaTiles();
                                }
                            }
                        }
                    }
                    else
                    {
                        SetStatus("You want to download an Area of size zero? You must be jokeing!");
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    SetStatus("Your input coordinates are not ok! Fix that first!");
                    Thread.Sleep(1000);
                }
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            StartDownload();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            AbortDownload();
        }

        private void AbortDownload()
        {
            if (mAreaProcessRunning)
            {
                mApplicationExitCheck = false; // Defuse appl exit check on User Abort
                EarthScriptsHandler.DoOnAbortButtonEvent(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);
                mStopProcess = true;
                
            }
            if (ScenprocUtils.ScenProcRunning)
            {
                ScenprocUtils.TellScenprocToTerminate();
            }
            mMasksCompilerMultithreadedQueue.Stop();
            mImageProcessingMultithreadedQueue.Stop();
            if (mImageToolThread != null && mImageToolThread.IsAlive)
            {
                mImageToolThread.Abort();
            }
            if (mCreateMeshThread != null)
            {
                mCreateMeshThread.Abort();
                mCreateMeshThread = null;
            }
        }

        private void LatGradBox_TextChanged(object sender, EventArgs e)
        {
            if (LatGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LongGradBox_TextChanged(object sender, EventArgs e)
        {
            if (LongGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LatMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (LatMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LongMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (LongMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }


        private void LatSecBox_TextChanged(object sender, EventArgs e)
        {
            if (LatSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LongSecBox_TextChanged(object sender, EventArgs e)
        {
            if (LongSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LongSignBox_TextChanged(object sender, EventArgs e)
        {
            if (LongSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void LatSignBox_TextChanged(object sender, EventArgs e)
        {
            if (LatSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void AreaSizeYBox_TextChanged(object sender, EventArgs e)
        {
            if (AreaSizeYBox.Enabled)
            {
                HandleInputChange();
            }
        }

        private void AreaSizeXBox_TextChanged(object sender, EventArgs e)
        {
            if (AreaSizeXBox.Enabled)
            {
                HandleInputChange();
            }
        }

        private void FetchSelectorBox_ValueChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
                
            }
            UpdateExcludeDetails();
        }


        private void TransparencyBox_TextChanged(object sender, EventArgs e)
        {
            HandleInputChange();
        }

        private void SmoothBordersBox_TextChanged(object sender, EventArgs e)
        {
            HandleInputChange();
        }

        private void WorkFolderBox_TextChanged(object sender, EventArgs e)
        {
            HandleInputChange();
        }

        private void SceneryFolderBox_TextChanged(object sender, EventArgs e)
        {
            HandleInputChange();
        }

        private void ZoomSelectorBox_ValueChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning) //else we may not empty queues!
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
                EmptyAllJobQueues();
            }
        }

        private void ServiceBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning) //else we may not empty queues!
            {
                //mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
                EmptyAllJobQueues();
            }
        }

        private void CompilerSelectorBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                
                HandleInputChange();
                //Auto set LOD13 if FS2004 is selected
                if (EarthCommon.StringCompare(CompilerSelectorBox.Text, "FS2004"))
                {
                    AreaSnapBox.Text = "LOD13";
                    EarthConfig.mAreaSnapMode = tAreaSnapMode.eLOD13;
                }
                //Auto set Off if FS2004 is selected
                if (EarthCommon.StringCompare(CompilerSelectorBox.Text, "FSX/P3D"))
                {
                    AreaSnapBox.Text = "Off";
                    EarthConfig.mAreaSnapMode = tAreaSnapMode.eOff;
                }
            }
        }


        private void SaveAreaInfo(MasksResampleWorker w)
        {
            EarthArea vEarthArea = new EarthArea();
            vEarthArea.Copy(w.mEarthArea);  //Copy so it is not dangerous if it is altered in the script

            String vAreaFileNameMiddlePart = w.AreaFileString;

            EarthScriptsHandler.SaveAreaInfo(vEarthArea, vAreaFileNameMiddlePart);
        }




        Boolean SceneryCompilerReady()
        {
            if (!EarthCommon.StringCompare(EarthConfig.mSceneryCompiler, "")&&EarthConfig.mCompileScenery)
            {
                if (!File.Exists(EarthConfig.mSceneryCompiler))
                {
                    SetStatus("SceneryCompiler " + EarthConfig.mSceneryCompiler + " in FSEarthTiles directory is missing!");
                    return false;
                }
            }
            return true;
        }

        private static bool BMPAllBlack(Bitmap b)
        {
            for (int x = 0; x < b.Width; x++)
            {
                for (int y = 0; y < b.Height; y++)
                {
                    // if even one alpha is not black, then bmp is not all black
                    byte curAlphaVal = b.GetPixel(x, y).A;
                    if (curAlphaVal != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public static void ensureTGAsNotAllBlack(string directoryToCheck)
        {
            string[] tgas = Directory.GetFiles(directoryToCheck, "*.tga");
            foreach (string f in tgas)
            {
                TGA t = new TGA(f);
                Bitmap b = t.ToBitmap(true);
                if (BMPAllBlack(b))
                {
                    Color color = b.GetPixel(0, 0);
                    Color newColor = Color.FromArgb(255, color);
                    b.SetPixel(0, 0, newColor);
                    TGA newAlphaTGA = new TGA(b);
                    string newAlphaTGAPath = Path.GetDirectoryName(f) + @"\" + Path.GetFileNameWithoutExtension(f) + @".tga";
                    newAlphaTGA.Save(newAlphaTGAPath);
                }
            }
        }


        Boolean RunImageTool()
        {
            System.Diagnostics.Process procImgTool = null;
            try
            {
                if (!EarthCommon.StringCompare(EarthConfig.mSceneryImageTool, ""))
                {
                    if (File.Exists(EarthConfig.mStartExeFolder + "\\" + EarthConfig.mSceneryImageTool))
                    {

                        SetStatusFromFriendThread("Fixing all black TGAs so imagetool doesn't drop their alpha channel...");

                        // imagetool has a bug/quirk that it drops the alpha channel of an image if it is all one color.
                        // this is a problem for images which are all water. they will stop being masked properly as their alpha channel
                        // will be dropped by imagetool. I cheat a little and set one of its pixels to white in such a case. Imperceptible
                        // difference for humans, but it makes imagetool happy so it doesn't drop the alpha channel and image is properly masked
                        // I could have taken the work of editing the dxt bmp's directly etc etc. But I had a hard time finding .Net libraries to do this
                        // and I don't want to write my own when this simpler solution gets the job done
                        ensureTGAsNotAllBlack(EarthConfig.mWorkFolder);

                        SetStatusFromFriendThread("Starting FS2004 Imagetool..");
                        Thread.Sleep(1000);

                        procImgTool = new System.Diagnostics.Process();
                        //procImgTool.EnableRaisingEvents = false;
                        //procImgTool.StartInfo.UseShellExecute = false;
                        //procImgTool.StartInfo.RedirectStandardOutput = true;
                        //procImgTool.StartInfo.CreateNoWindow = true;
                        //procImgTool.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        procImgTool.StartInfo.FileName = EarthConfig.mStartExeFolder + "\\" + EarthConfig.mSceneryImageTool;
                        procImgTool.StartInfo.Arguments = "-nogui -terrainphoto " + "\"" + EarthConfig.mWorkFolder + "\\" + "*.tga" + "\"";
                        procImgTool.Start();

                        SetStatusFromFriendThread("FS2004 Imagetool active. Waiting for completion.");
                        Thread.Sleep(500);

                        procImgTool.WaitForExit();
                        if (!procImgTool.HasExited)
                        {
                            procImgTool.Kill();
                        };


                        SetStatusFromFriendThread("Copying Texture Files ....");
                        Thread.Sleep(500);

                        String vTgaSubDirectory = EarthConfig.mSceneryFolderTexture + "\\" + "TgaSourceFiles";

                        if (EarthConfig.mFS2004KeepTGAs)
                        {
                            //First create a TGA subdirectory.
                            if (!(Directory.Exists(vTgaSubDirectory)))
                            {
                                Directory.CreateDirectory(vTgaSubDirectory);
                            }
                        }

                        String[] vMipFiles = Directory.GetFiles(EarthConfig.mWorkFolder, "*.mip");

                        for (Int32 vFileCount = 0; vFileCount < vMipFiles.Length; vFileCount++)
                        {
                            String vDestFileName = vMipFiles[vFileCount];
                            String vTgaFullFileName;
                            String vTgaFileName;

                            //replace Ending
                            Int32 vEndingIndex = vDestFileName.IndexOf(".mip");
                            vDestFileName = vDestFileName.Remove(vEndingIndex);
                            vTgaFullFileName = vDestFileName;
                            vDestFileName += ".bmp";
                            vTgaFullFileName += ".tga";
                            vTgaFileName = vTgaFullFileName;
                            //GetRightOfDirectory
                            Int32 vDirectoryIndex = vDestFileName.IndexOf("\\");
                            while (vDirectoryIndex >= 0)
                            {
                                vDestFileName = vDestFileName.Substring(vDirectoryIndex + 1, vDestFileName.Length - (vDirectoryIndex + 1));
                                vTgaFileName = vTgaFileName.Substring(vDirectoryIndex + 1, vTgaFileName.Length - (vDirectoryIndex + 1));
                                vDirectoryIndex = vDestFileName.IndexOf("\\");
                            }

                            //finaly Copy and rename!
                            String vSource = vMipFiles[vFileCount];
                            String vDest = EarthConfig.mSceneryFolderTexture + "\\" + vDestFileName;
                            String vTgaDest = vTgaSubDirectory + "\\" + vTgaFileName;
                            File.Copy(vSource, vDest, true);
                            if (EarthConfig.mFS2004KeepTGAs)
                            {
                                File.Copy(vTgaFullFileName, vTgaDest, true);
                            }
                            File.Delete(vSource);
                            File.Delete(vTgaFullFileName);
                        }
                    }

                    return true;
                }
            }
            catch (ThreadAbortException)
            {
                if (procImgTool != null)
                {
                    procImgTool.Kill();
                }

                return false;
            }

            return false;
        }


        void RunImageToolProcessing()
        {
            if (!RunImageTool())
            {
                SetStatusFromFriendThread("There was an error running ImageTool");
            }
        }


        Boolean StartSceneryCompiler(MasksResampleWorker w)
        {
            System.Diagnostics.Process proc = null;
            try
            {
                if (File.Exists(EarthConfig.mStartExeFolder + "\\" + EarthConfig.mSceneryCompiler))
                {

                    String vAreaInfoFileName = "AreaFSInfo" + w.AreaFileString + ".inf";

                    if (EarthConfig.mCreateAreaMask)
                    {
                        if ((EarthConfig.mCreateWaterMaskBitmap) || (EarthConfig.IsWithSeasons()))
                        {
                            if (EarthCommon.StringCompare(EarthConfig.mSelectedSceneryCompiler, "FS2004"))
                            {
                                if ((EarthConfig.IsWithSeasons()))
                                {
                                    vAreaInfoFileName = "AreaFS2004MasksSeasonsInfo" + w.AreaFileString + ".inf";
                                }
                                else
                                {
                                    vAreaInfoFileName = "AreaFS2004MasksInfo" + w.AreaFileString + ".inf";
                                }
                            }
                            else
                            {
                                if ((EarthConfig.IsWithSeasons()))
                                {
                                    vAreaInfoFileName = "AreaFSXMasksSeasonsInfo" + w.AreaFileString + ".inf";
                                }
                                else
                                {
                                    vAreaInfoFileName = "AreaFSXMasksInfo" + w.AreaFileString + ".inf";
                                }
                            }
                        }
                    }

                    proc = new System.Diagnostics.Process();
                    //proc.EnableRaisingEvents = false;
                    //proc.StartInfo.UseShellExecute = false;
                    //proc.StartInfo.RedirectStandardOutput = true;
                    //proc.StartInfo.CreateNoWindow = true;
                    //proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                    proc.StartInfo.FileName = EarthConfig.mStartExeFolder + "\\" + EarthConfig.mSceneryCompiler;
                    proc.StartInfo.Arguments = "\"" + EarthConfig.mWorkFolder + "\\" + vAreaInfoFileName + "\"";
                    proc.Start();
                    SetStatusFromFriendThread("Scenery Compiler active. Waiting for completion.");
                    Thread.Sleep(500);
                    proc.WaitForExit();
                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    };

                    String vAreaThumbnailFileName = "AreaThumbnail" + w.AreaFileString + ".bmp";

                    if (File.Exists(EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName))
                    {
                        if (File.Exists(EarthConfig.mSceneryFolderTexture + "\\" + vAreaThumbnailFileName))
                        {
                            File.Delete(EarthConfig.mSceneryFolderTexture + "\\" + vAreaThumbnailFileName);
                        }
                        File.Copy(EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName, EarthConfig.mSceneryFolderTexture + "\\" + vAreaThumbnailFileName, true);
                        File.Delete(EarthConfig.mWorkFolder + "\\" + vAreaThumbnailFileName);
                    }

                    return true;
                }
                else
                {
                    SetStatusFromFriendThread("SceneryCompiler in FSEarthTiles is missing!");
                    return false;
                }
            }
            catch (ThreadAbortException)
            {
                if (proc != null)
                {
                    proc.Kill();
                }
                return false;
            }
            catch
            {
                SetStatusFromFriendThread("Error in StartSceneryCompiler(), Probabily a File copy or delete did not work. Check HarddiskSpace and close all open Files and try again.");
                return false;
            }
        }

        Boolean PrepareWorkingDirectory(Boolean iForStart)
        {
            try
            {
                EarthConfig.mWorkFolder = WorkFolderBox.Text;
                Int32 vEndCharIndx = EarthConfig.mWorkFolder.Length - 1;
                if (EarthConfig.mWorkFolder[vEndCharIndx] == '\\')
                {
                    EarthConfig.mWorkFolder = EarthConfig.mWorkFolder.Remove(vEndCharIndx);
                }
                if (!Directory.Exists(EarthConfig.mWorkFolder))
                {
                    Directory.CreateDirectory(EarthConfig.mWorkFolder);
                }

                if (!Directory.Exists(EarthConfig.mWorkFolder + "\\cache"))
                {
                    Directory.CreateDirectory(EarthConfig.mWorkFolder + "\\cache");
                }
                else
                {

                    if (iForStart)
                    {
                        if (EarthConfig.mDeleteCachePrompt)
                        {
                            if (MessageBox.Show("Do you want to clear out the cache?", "Clear Cache", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                //File.Delete(EarthConfig.mWorkFolder + "\\cache\\*.*");

                                string[] filePaths = Directory.GetFiles(EarthConfig.mWorkFolder + "\\cache\\");
                                foreach (string filePath in filePaths)
                                    try
                                    {
                                        File.Delete(filePath);
                                    }


                                    catch
                                    {


                                    }

                            }
                        }
                    }
                    
                }

                return true;
            }
            catch
            {
                if (iForStart)
                {
                    SetStatus("Can not start. Something is wrong with the Working Directory!");
                }
                else
                {
                    SetStatus("Action failed. Could not create the Working Directory!");
                }
                return false;
            }
        }

        Boolean PrepareSceneryDirectory()
        {
            try
            {
                EarthConfig.mSceneryFolder = SceneryFolderBox.Text;
                Int32 vEndCharIndx = EarthConfig.mSceneryFolder.Length - 1;
                if (EarthConfig.mSceneryFolder[vEndCharIndx] == '\\')
                {
                    EarthConfig.mSceneryFolder = EarthConfig.mSceneryFolder.Remove(vEndCharIndx);
                }
                EarthConfig.mSceneryFolderScenery = EarthConfig.mSceneryFolder + "\\" + "scenery";
                EarthConfig.mSceneryFolderTexture = EarthConfig.mSceneryFolder + "\\" + "texture";

                if (!Directory.Exists(EarthConfig.mSceneryFolder))
                {
                    Directory.CreateDirectory(EarthConfig.mSceneryFolder);
                }
                if (!Directory.Exists(EarthConfig.mSceneryFolderScenery))
                {
                    Directory.CreateDirectory(EarthConfig.mSceneryFolderScenery);
                }
                if (!Directory.Exists(EarthConfig.mSceneryFolderTexture))
                {
                    Directory.CreateDirectory(EarthConfig.mSceneryFolderTexture);
                }
                return true;
            }
            catch
            {
                SetStatus("Can not start. Something is wrong with the Scenery Directories!");
                return false;
            }
        }

        private void AreaDefModeBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                HandleEnableDisableLongLatInputFields();
                HandleInputChange();
            }
        }

        private void NWLatGradBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLatGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLatMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLatMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLatSecBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLatSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLatSignBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLatSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLongGradBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLongGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLongMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLongMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLongSecBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLongSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void NWLongSignBox_TextChanged(object sender, EventArgs e)
        {
            if (NWLongSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELatGradBox_TextChanged(object sender, EventArgs e)
        {
            if (SELatGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELatMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (SELatMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELatSecBox_TextChanged(object sender, EventArgs e)
        {
            if (SELatSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELatSignBox_TextChanged(object sender, EventArgs e)
        {
            if (SELatSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELongGradBox_TextChanged(object sender, EventArgs e)
        {
            if (SELongGradBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELongMinuteBox_TextChanged(object sender, EventArgs e)
        {
            if (SELongMinuteBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELongSecBox_TextChanged(object sender, EventArgs e)
        {
            if (SELongSecBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void SELongSignBox_TextChanged(object sender, EventArgs e)
        {
            if (SELongSignBox.Visible)
            {
                HandleInputChange();
            }
        }

        private void AreaSnapBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                if (AreaSnapBox.Visible)
                {
                    
                    HandleInputChange();
                    SetFixOrAutoModeVisibility();
                    DisplayWhereYouJustAreAgain();
                }
            }
        }

        private void CompileSceneryBox_TextChanged(object sender, EventArgs e)
        {
            if (CompileSceneryBox.Visible)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
            }
        }

        private void DisplayCenterButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplaySpot();
            }
        }

        private void DisplayNWButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplaySnappedNorthWestCorner();
            }
        }

        private void DisplayNEButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplaySnappedNorthEastCorner();
            }
        }

        private void DisplaySWButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplaySnappedSouthWestCorner();
            }
        }

        private void DisplaySEButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplaySnappedSouthEastCorner();
            }
        }

        void HandleDisplayModeChangeVisibility()
        {
            if (EarthCommon.StringCompare(EarthConfig.mDisplayMode, "Free"))
            {
                EditRefButton.Visible = true;
                DrawButton.Visible = true;
                PlaceButton.Visible = true;
                RefModeButton.Visible = true;
            }
            else
            {
                EditRefButton.Visible = false;
                DrawButton.Visible = false;
                PlaceButton.Visible = false;
                RefModeButton.Visible = false;
            }

        }


        private void DisplayModeBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                if (DisplayModeBox.Visible)
                {
                    HandleInputChange();
                    HandleDisplayModeChangeVisibility();
                    DisplayWhereYouJustAreAgain();
                }
            }
        }


        void DisplayAreaInFull()
        {
            DisplaySpot();

            Int64 vFullAreaZoomLevel = GetFullSnappedAreaZoomLevel();

            if (vFullAreaZoomLevel < -4)
            {
                vFullAreaZoomLevel = -4;
            }
            if (vFullAreaZoomLevel > 16)
            {
                vFullAreaZoomLevel = 16;
            }
            ZoomSelectorBox.Value = vFullAreaZoomLevel;
        }


        private void DisplayAreaButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                HandleInputChange();
                DisplayAreaInFull();
            }
        }



        Boolean IsOverHotSpotCenter(Int64 iMousePosX, Int64 iMousePosY)
        {
            Double vSpotYPixel = GetOnDisplayPixelPosY(mEarthInputArea.SpotLatitude, NormalPictureBox.Size.Height);
            Double vSpotXPixel = GetOnDisplayPixelPosX(mEarthInputArea.SpotLongitude, NormalPictureBox.Size.Width);
            Double vSpotPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vSpotXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSpotYPixel, 2.0));

            Double vAlternativeSpotXPixel = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.SpotLongitude, NormalPictureBox.Size.Width);
            Double vAlternativeSpotPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vAlternativeSpotXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSpotYPixel, 2.0));

            Boolean vOverHotSpot = (vSpotPixelDistance <= cAktiveSpotPixelDiameter) || (vAlternativeSpotPixelDistance <= cAktiveSpotPixelDiameter);
            return vOverHotSpot;
        }

        Boolean IsOverHotSpotNWCorner(Int64 iMousePosX, Int64 iMousePosY)
        {
            Double vNWCornerYPixel = GetOnDisplayPixelPosY(mEarthInputArea.AreaStartLatitude, NormalPictureBox.Size.Height);
            Double vNWCornerXPixel = GetOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, NormalPictureBox.Size.Width);
            Double vNWCornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vNWCornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vNWCornerYPixel, 2.0));

            Double vAlternativeNWCornerXPixel = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, NormalPictureBox.Size.Width);
            Double vAlternativeNWCornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vAlternativeNWCornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vNWCornerYPixel, 2.0));

            Boolean vOverHotSpot = (vNWCornerPixelDistance <= cAktiveSpotPixelDiameter) || (vAlternativeNWCornerPixelDistance <= cAktiveSpotPixelDiameter);
            return vOverHotSpot;
        }

        Boolean IsOverHotSpotSECorner(Int64 iMousePosX, Int64 iMousePosY)
        {
            Double vSECornerYPixel = GetOnDisplayPixelPosY(mEarthInputArea.AreaStopLatitude, NormalPictureBox.Size.Height);
            Double vSECornerXPixel = GetOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, NormalPictureBox.Size.Width);
            Double vSECornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vSECornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSECornerYPixel, 2.0));

            Double vAlternativeSECornerXPixel = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, NormalPictureBox.Size.Width);
            Double vAlternativeSECornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vAlternativeSECornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSECornerYPixel, 2.0));

            Boolean vOverHotSpot = (vSECornerPixelDistance <= cAktiveSpotPixelDiameter) || (vAlternativeSECornerPixelDistance <= cAktiveSpotPixelDiameter);
            return vOverHotSpot;
        }

        Boolean IsOverHotSpotNECorner(Int64 iMousePosX, Int64 iMousePosY)
        {
            Double vNECornerYPixel = GetOnDisplayPixelPosY(mEarthInputArea.AreaStartLatitude, NormalPictureBox.Size.Height);
            Double vNECornerXPixel = GetOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, NormalPictureBox.Size.Width);
            Double vNECornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vNECornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vNECornerYPixel, 2.0));

            Double vAlternativeNECornerXPixel = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStopLongitude, NormalPictureBox.Size.Width);
            Double vAlternativeNECornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vAlternativeNECornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vNECornerYPixel, 2.0));

            Boolean vOverHotSpot = (vNECornerPixelDistance <= cAktiveSpotPixelDiameter) || (vAlternativeNECornerPixelDistance <= cAktiveSpotPixelDiameter);
            return vOverHotSpot;
        }

        Boolean IsOverHotSpotSWCorner(Int64 iMousePosX, Int64 iMousePosY)
        {
            Double vSWCornerYPixel = GetOnDisplayPixelPosY(mEarthInputArea.AreaStopLatitude, NormalPictureBox.Size.Height);
            Double vSWCornerXPixel = GetOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, NormalPictureBox.Size.Width);
            Double vSWCornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vSWCornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSWCornerYPixel, 2.0));

            Double vAlternativeSWCornerXPixel = GetAlternativeOnDisplayPixelPosX(mEarthInputArea.AreaStartLongitude, NormalPictureBox.Size.Width);
            Double vAlternativeSWCornerPixelDistance = Math.Sqrt(Math.Pow(Convert.ToDouble(iMousePosX) - vAlternativeSWCornerXPixel, 2.0) + Math.Pow(Convert.ToDouble(iMousePosY) - vSWCornerYPixel, 2.0));

            Boolean vOverHotSpot = (vSWCornerPixelDistance <= cAktiveSpotPixelDiameter) || (vAlternativeSWCornerPixelDistance <= cAktiveSpotPixelDiameter);
            return vOverHotSpot;
        }

        private void ReleaseAnyDrawMode()
        {
            mUserDrawAreaActive = false; //and finish
            mUserSetCenterActive = false;
            mUserDrawExcludeArea = false;
            AreaDefModeBox.Enabled = true;
            //Cursor = new Cursor(Cursors.Default.Handle);
            Cursor = Cursors.Default;
            mActiveCursor = tCursor.eDefault;
        }

        private void NormalPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mMouseDownFlag = true;
                mMouseDownX = e.X;
                mMouseDownY = e.Y;

                // Control Key pressed so we are toggling an areas download status
                if (Control.ModifierKeys == Keys.Control)
                {
                    ToggleAreaDownloadStatus(e.X, e.Y);
                    DisplayWhereYouJustAreAgain();
                    mMouseDownFlag = false;
                }
                if (Control.ModifierKeys == Keys.Shift)
                {
                    ToggleAllAreasDownloadStatus(false);
                    DisplayWhereYouJustAreAgain();
                    mMouseDownFlag = false;
                }

                if (Control.ModifierKeys == (Keys.Shift |Keys.Control))
                {
                    ToggleAllAreasDownloadStatus(true);
                    DisplayWhereYouJustAreAgain();
                    mMouseDownFlag = false;
                }

                else
                {

                    Boolean vContinueAktivationCheck = true;
                    Boolean vOverHotSpotCenter = false;

                    if (e.Button.Equals(MouseButtons.Right))
                    {
                        ReleaseAnyDrawMode();
                        vContinueAktivationCheck = false;
                    }

                    if (mUserSetCenterActive)
                    {
                        vContinueAktivationCheck = false;
                        UserSetsNewCenter(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }


                    if (mUserDrawAreaActive)
                    {
                        vContinueAktivationCheck = false;
                        UserSetsNewFirstAreaDrawFixPoint(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }

                    if (mUserDrawExcludeArea)
                    {
                        vContinueAktivationCheck = false;
                        if (UserSetsNewFirstExcludeAreaDrawFixPoint(e.X, e.Y))
                        {
                            DisplayWhereYouJustAreAgain();
                        }
                        else
                        {
                            mUserDrawExcludeArea = false;
                        }
                    }

                    //Check if a HotSpot (center or corner of Input Area) is clicked
                    //Center
                    vOverHotSpotCenter = IsOverHotSpotCenter(e.X, e.Y);
                    if (vOverHotSpotCenter && vContinueAktivationCheck)
                    {
                        //Enter UserCenterMoveMode
                        vContinueAktivationCheck = false;
                        EnterUserCenterMovementMode();
                        UserSetsNewCenter(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }

                    //North-West-Corner
                    vOverHotSpotCenter = IsOverHotSpotNWCorner(e.X, e.Y);
                    if (vOverHotSpotCenter && vContinueAktivationCheck)
                    {
                        //Enter AeraDrawMode
                        vContinueAktivationCheck = false;
                        mAreaDrawFixPointLatitude = mEarthInputArea.AreaStopLatitude;
                        mAreaDrawFixPointLongitude = mEarthInputArea.AreaStopLongitude;
                        EnterUserAreaDrawMode();
                        UserSetsNewAreaSecondPoint(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }

                    //South-East-Corner
                    vOverHotSpotCenter = IsOverHotSpotSECorner(e.X, e.Y);
                    if (vOverHotSpotCenter && vContinueAktivationCheck)
                    {
                        //Enter AeraDrawMode
                        vContinueAktivationCheck = false;
                        mAreaDrawFixPointLatitude = mEarthInputArea.AreaStartLatitude;
                        mAreaDrawFixPointLongitude = mEarthInputArea.AreaStartLongitude;
                        EnterUserAreaDrawMode();
                        UserSetsNewAreaSecondPoint(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }

                    //North-East-Corner
                    vOverHotSpotCenter = IsOverHotSpotNECorner(e.X, e.Y);
                    if (vOverHotSpotCenter && vContinueAktivationCheck)
                    {
                        //Enter AeraDrawMode
                        vContinueAktivationCheck = false;
                        mAreaDrawFixPointLatitude = mEarthInputArea.AreaStopLatitude;
                        mAreaDrawFixPointLongitude = mEarthInputArea.AreaStartLongitude;
                        EnterUserAreaDrawMode();
                        UserSetsNewAreaSecondPoint(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }

                    //South-West-Corner
                    vOverHotSpotCenter = IsOverHotSpotSWCorner(e.X, e.Y);
                    if (vOverHotSpotCenter && vContinueAktivationCheck)
                    {
                        //Enter AeraDrawMode
                        vContinueAktivationCheck = false;
                        mAreaDrawFixPointLatitude = mEarthInputArea.AreaStartLatitude;
                        mAreaDrawFixPointLongitude = mEarthInputArea.AreaStopLongitude;
                        EnterUserAreaDrawMode();
                        UserSetsNewAreaSecondPoint(e.X, e.Y);
                        DisplayWhereYouJustAreAgain();
                    }
                }
            }
        }

        private void NormalPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            mMapScrollingOnUserDrawing = false;

            mMouseDownFlag = false;

            if (mUserSetCenterActive)
            {
                UserSetsNewCenter(e.X, e.Y);
                DisplayWhereYouJustAreAgain();
                mUserDrawAreaActive = false; //and finish
                mUserSetCenterActive = false;
                AreaDefModeBox.Enabled = true;
                //Cursor = new Cursor(Cursors.Default.Handle);
                Cursor = Cursors.Default;
                mActiveCursor = tCursor.eDefault;
            }
            if (mUserDrawAreaActive)
            {
                UserSetsNewAreaSecondPoint(e.X, e.Y);
                DisplayWhereYouJustAreAgain();
                mUserDrawAreaActive = false; //and finish
                mUserSetCenterActive = false;
                AreaDefModeBox.Enabled = true;
                //Cursor = new Cursor(Cursors.Default.Handle);
                Cursor = Cursors.Default;
                mActiveCursor = tCursor.eDefault;
            }

            if (mUserDrawExcludeArea)
            {
                // Set second exclude point
                DisplayWhereYouJustAreAgain();
                mUserDrawAreaActive = false; //and finish
                mUserSetCenterActive = false;
                mUserDrawExcludeArea = false;
                AreaDefModeBox.Enabled = true;
                //Cursor = new Cursor(Cursors.Default.Handle);
                Cursor = Cursors.Default;
                mActiveCursor = tCursor.eDefault;
            }

            DisplayThisFreePosition();

        }

        void MapScrollingOnUserDrawing(Int64 iMousePosX, Int64 iMousePosY)
        {
            mUserDrawingMousePosXScrolling = iMousePosX;
            mUserDrawingMousePosYScrolling = iMousePosY;

            Double vDisplayXDimension = Convert.ToDouble(NormalPictureBox.Size.Width);
            Double vDisplayYDimension = Convert.ToDouble(NormalPictureBox.Size.Height);
            Double vMouseXPos = Convert.ToDouble(iMousePosX);
            Double vMouseYPos = Convert.ToDouble(iMousePosY);

            Double vScrollingInX = 0.0;
            Double vScrollingInY = 0.0;

            if (vMouseXPos <= cOnDrawingMapActivePixelBorder)
            {
                Double vScrollValue = 0.5 * (cOnDrawingMapActivePixelBorder - vMouseXPos + 1);
                if (vScrollValue > cOnDrawingMaxMapScrollSpeedPixel)
                {
                    vScrollValue = cOnDrawingMaxMapScrollSpeedPixel;
                }
                vScrollingInX -= vScrollValue;
            }
            if (vMouseXPos >= (vDisplayXDimension - cOnDrawingMapActivePixelBorder))
            {
                Double vScrollValue = 0.5 * (vMouseXPos - (vDisplayXDimension - cOnDrawingMapActivePixelBorder) + 1);
                if (vScrollValue > cOnDrawingMaxMapScrollSpeedPixel)
                {
                    vScrollValue = cOnDrawingMaxMapScrollSpeedPixel;
                }
                vScrollingInX += vScrollValue;
            }
            if (vMouseYPos <= cOnDrawingMapActivePixelBorder)
            {
                Double vScrollValue = 0.5 * (cOnDrawingMaxMapScrollSpeedPixel - vMouseYPos + 1);
                if (vScrollValue > cOnDrawingMaxMapScrollSpeedPixel)
                {
                    vScrollValue = cOnDrawingMaxMapScrollSpeedPixel;
                }
                vScrollingInY -= vScrollValue;
            }
            if (vMouseYPos >= (vDisplayYDimension - cOnDrawingMapActivePixelBorder))
            {
                Double vScrollValue = 0.5 * (vMouseYPos - (vDisplayYDimension - cOnDrawingMapActivePixelBorder) + 1);
                if (vScrollValue > cOnDrawingMaxMapScrollSpeedPixel)
                {
                    vScrollValue = cOnDrawingMaxMapScrollSpeedPixel;
                }
                vScrollingInY += vScrollValue;
            }
            if ((vScrollingInX != 0.0) || (vScrollingInY != 0.0))
            {
                Double vNewDisplayCenterLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(vScrollingInY), EarthConfig.mZoomLevel);
                Double vNewDisplayCenterLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(vScrollingInX), EarthConfig.mZoomLevel);
                vNewDisplayCenterLatitude = EarthMath.CleanLatitude(vNewDisplayCenterLatitude);
                vNewDisplayCenterLongitude = EarthMath.CleanLongitude(vNewDisplayCenterLongitude);
                if (mUserSetCenterActive)
                {
                    UserSetsNewCenter(iMousePosX, iMousePosY);
                }
                else if (mUserDrawAreaActive)
                {
                    UserSetsNewAreaSecondPoint(iMousePosX, iMousePosY);
                }
                DisplayThisPosition(vNewDisplayCenterLongitude, vNewDisplayCenterLatitude);
            }
        }

        void UserSetsNewCenter(Int64 iMousePosX, Int64 iMousePosY)
        {
            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
            Double vNewSpotLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vNewSpotLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);

            vNewSpotLatitude = EarthMath.CleanLatitude(vNewSpotLatitude);
            vNewSpotLongitude = EarthMath.CleanLongitude(vNewSpotLongitude);

            //placeing is with 1 point placement so....

            //CalculateMax Lattitude 
            Double vYHalfAreaDistance = 0.5 * mEarthInputArea.AreaYSize * EarthMath.cnmToMeterFactor;
            Double vLatHalfDistInGrad = 360.0 * vYHalfAreaDistance / EarthMath.cEarthCircumference;
            Double vLatitudeSpotMax = EarthMath.MercatorProjectionCut - vLatHalfDistInGrad;
            Double vLatitudeSpotMin = -EarthMath.MercatorProjectionCut + vLatHalfDistInGrad;

            Double VTemp;

            VTemp = EarthMath.GetToAHunderThSecRoundedWorldCoord(vLatitudeSpotMax);

            if (VTemp > vLatitudeSpotMax)
            {
                vLatitudeSpotMax = VTemp - (1.0 / 360000.0);
            }
            else
            {
                vLatitudeSpotMax = VTemp;
            }

            VTemp = EarthMath.GetToAHunderThSecRoundedWorldCoord(vLatitudeSpotMin);
            if (VTemp < vLatitudeSpotMin)
            {
                vLatitudeSpotMin = VTemp + (1.0 / 360000.0);
            }
            else
            {
                vLatitudeSpotMin = VTemp;
            }

            vNewSpotLatitude = EarthMath.GetToAHunderThSecRoundedWorldCoord(vNewSpotLatitude);

            if (vNewSpotLatitude > vLatitudeSpotMax)
            {
                vNewSpotLatitude = vLatitudeSpotMax;
            }
            if (vNewSpotLatitude < vLatitudeSpotMin)
            {
                vNewSpotLatitude = vLatitudeSpotMin;
            }

            Double vLocalRadius = Math.Cos(EarthMath.cDegreeToRadFactor * vNewSpotLatitude) * EarthMath.cEarthRadius;
            Double vLocalCircumference = 2.0 * EarthMath.cPi * vLocalRadius;
            Double vXHalfAreaDistance = 0.5 * mEarthInputArea.AreaXSize * EarthMath.cnmToMeterFactor;

            Double vLongitudeSpotMax = +180.0;
            Double vLongitudeSpotMin = -180.0;

            //Earth Pole Check
            if ((2.0 * vXHalfAreaDistance) >= vLocalCircumference)
            {
                //Full circle! -180.0 to +180.0
                //then Spot doesnt matter anymore
                //nothing to do
            }
            else
            {
                Double vLongHalfDistInGrad = 360.0 * vXHalfAreaDistance / vLocalCircumference;
                vLongitudeSpotMax = +180.0 - vLongHalfDistInGrad; //-3e-5;
                vLongitudeSpotMin = -180.0 + vLongHalfDistInGrad; //+3e-5;

                VTemp = EarthMath.GetToAHunderThSecRoundedWorldCoord(vLongitudeSpotMax);
                if (VTemp > vLongitudeSpotMax)
                {
                    vLongitudeSpotMax = VTemp - (1.0 / 360000.0);
                }
                else
                {
                    vLongitudeSpotMax = VTemp;
                }

                VTemp = EarthMath.GetToAHunderThSecRoundedWorldCoord(vLongitudeSpotMin);
                if (VTemp < vLongitudeSpotMin)
                {
                    vLongitudeSpotMin = VTemp + (1.0 / 360000.0);
                }
                else
                {
                    vLongitudeSpotMin = VTemp;
                }
            }

            if (vNewSpotLongitude > vLongitudeSpotMax)
            {
                vNewSpotLongitude = vLongitudeSpotMax;
            }
            if (vNewSpotLongitude < vLongitudeSpotMin)
            {
                vNewSpotLongitude = vLongitudeSpotMin;
            }

            vNewSpotLatitude  = EarthMath.LimitLatitude(vNewSpotLatitude);
            vNewSpotLongitude = EarthMath.LimitLongitude(vNewSpotLongitude);

            SetInputCenterCoordsTo(vNewSpotLatitude, vNewSpotLongitude);
        }

        // Calculate the first lat/long of our exclude area
        Boolean UserSetsNewFirstExcludeAreaDrawFixPoint(Int64 iMousePosX, Int64 iMousePosY)
        {
            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
            Double vNewExcludeAreaStartLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vNewExcludeAreaStartLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
            vNewExcludeAreaStartLatitude = EarthMath.CleanLatitude(vNewExcludeAreaStartLatitude);
            vNewExcludeAreaStartLongitude = EarthMath.CleanLongitude(vNewExcludeAreaStartLongitude);
            mExcludeAreaDrawFixPointLatitude = EarthMath.LimitLatitude(vNewExcludeAreaStartLatitude);
            mExcludeAreaDrawFixPointLongitude = EarthMath.LimitLongitude(vNewExcludeAreaStartLongitude);
            
            // Eastern hemisphere
            if ((mEarthInputArea.SpotLongitude >= 0) && (vNewExcludeAreaStartLongitude >= 0) || (mEarthInputArea.SpotLongitude < 0) && (vNewExcludeAreaStartLongitude < 0))
            {
                vExcludeAreas.Areas.Add(new ExcludeAreaInfo(vNewExcludeAreaStartLatitude, vNewExcludeAreaStartLongitude, vNewExcludeAreaStartLatitude, vNewExcludeAreaStartLongitude));
                lsbExcludeAreas.Items.Add("");
                UpdateExcludeDetails();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set the 2nd point of a new exclude zone.
        /// </summary>
        /// <param name="iMousePosX"></param>
        /// <param name="iMousePosY"></param>
        void UserSetsNewExcludeAreaSecondPoint(Int64 iMousePosX, Int64 iMousePosY)
        {
            int idx;
            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
            Double vNewExcludeAreaSecondLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vNewExcludeAreaSecondLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
            vNewExcludeAreaSecondLatitude = EarthMath.CleanLatitude(vNewExcludeAreaSecondLatitude);
            vNewExcludeAreaSecondLongitude = EarthMath.CleanLongitude(vNewExcludeAreaSecondLongitude);

            Double vOldExcludeAreaSecondLatitude;
            Double vOldExcludeAreaSecondLongitude;

            idx = vExcludeAreas.Areas.Count - 1;

            Double vDistPoint1 = ((mExcludeAreaDrawFixPointLongitude - vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude) * (mExcludeAreaDrawFixPointLongitude - vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude) +
                                  (mExcludeAreaDrawFixPointLatitude - vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude) * (mExcludeAreaDrawFixPointLatitude - vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude));

            Double vDistPoint2 = ((mAreaDrawFixPointLongitude - vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude) * (mAreaDrawFixPointLongitude - vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude) +
                                  (mAreaDrawFixPointLatitude - vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude) * (mAreaDrawFixPointLatitude - vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude));

            if (vDistPoint1 <= vDistPoint2)
            {
                vOldExcludeAreaSecondLatitude = vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude;
                vOldExcludeAreaSecondLongitude = vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude;
            }
            else
            {
                vOldExcludeAreaSecondLatitude = vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude;
                vOldExcludeAreaSecondLongitude = vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude;
            }

            if (((vNewExcludeAreaSecondLongitude - vOldExcludeAreaSecondLongitude) > 180.0) ||
                ((vNewExcludeAreaSecondLongitude - vOldExcludeAreaSecondLongitude) < -180.0))
            {
                //DateBorderOverRun
                if (vNewExcludeAreaSecondLongitude < 0.0)
                {
                    vNewExcludeAreaSecondLongitude += 360.0;
                }
                else
                {
                    vNewExcludeAreaSecondLongitude -= 360.0;
                }
            }

            vNewExcludeAreaSecondLatitude = EarthMath.LimitLatitude(vNewExcludeAreaSecondLatitude);
            vNewExcludeAreaSecondLongitude = EarthMath.LimitLongitude(vNewExcludeAreaSecondLongitude);
            //Set and sort Area Coords
            Double vNWCornerLatitude = mExcludeAreaDrawFixPointLatitude;
            Double vNWCornerLongitude = mExcludeAreaDrawFixPointLongitude;
            Double vSECornerLatitude = vNewExcludeAreaSecondLatitude;
            Double vSECornerLongitude = vNewExcludeAreaSecondLongitude;
            if (vNewExcludeAreaSecondLatitude > vNWCornerLatitude)
            {
                vNWCornerLatitude = vNewExcludeAreaSecondLatitude;
            }
            if (mExcludeAreaDrawFixPointLatitude < vSECornerLatitude)
            {
                vSECornerLatitude = mExcludeAreaDrawFixPointLatitude;
            }
            if (vNewExcludeAreaSecondLongitude < vNWCornerLongitude)
            {
                vNWCornerLongitude = vNewExcludeAreaSecondLongitude;
            }
            if (mExcludeAreaDrawFixPointLongitude > vSECornerLongitude)
            {
                vSECornerLongitude = mExcludeAreaDrawFixPointLongitude;
            }

            vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude = vNWCornerLatitude;
            vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude = vNWCornerLongitude;
            vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude = vSECornerLatitude;
            vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude = vSECornerLongitude;

            vExcludeAreas.Areas[idx].ExcludeAreaCodeXStart = EarthMath.GetAreaCodeX(vNWCornerLongitude, EarthConfig.mFetchLevel);
            vExcludeAreas.Areas[idx].ExcludeAreaCodeXStop = EarthMath.GetAreaCodeX(vSECornerLongitude, EarthConfig.mFetchLevel);
            vExcludeAreas.Areas[idx].ExcludeAreaCodeYStart = EarthMath.GetAreaCodeY(vNWCornerLatitude, EarthConfig.mFetchLevel);
            vExcludeAreas.Areas[idx].ExcludeAreaCodeYStop = EarthMath.GetAreaCodeY(vSECornerLatitude, EarthConfig.mFetchLevel);
 
            
            UpdateExcludeDetails();

            lsbExcludeAreas.SelectedIndex = lsbExcludeAreas.Items.Count - 1;

        }

        /// <summary>
        /// Update the Exclude Zone listbox with new data
        /// </summary>
        void UpdateExcludeDetails()
        {
            Double vNWCornerLatitude;
            Double vNWCornerLongitude;
            Double vSECornerLatitude;
            Double vSECornerLongitude;
            string ExcludeDetails;
            String vSign;
            Int64 ExcludeTileCount;
            int idx = 0;

            //ExcludeAreaInfo Ex_Area;
            
            foreach (ExcludeAreaInfo Ex_Area in vExcludeAreas)
            {
                
                vNWCornerLatitude = Ex_Area.ExcludeAreaStartLatitude;
                vNWCornerLongitude = Ex_Area.ExcludeAreaStartLongitude;
                vSECornerLatitude = Ex_Area.ExcludeAreaStopLatitude;
                vSECornerLongitude = Ex_Area.ExcludeAreaStopLongitude;

                Ex_Area.ExcludeAreaCodeXStart = EarthMath.GetAreaCodeX(vNWCornerLongitude, EarthConfig.mFetchLevel);
                Ex_Area.ExcludeAreaCodeXStop = EarthMath.GetAreaCodeX(vSECornerLongitude, EarthConfig.mFetchLevel);
                Ex_Area.ExcludeAreaCodeYStart = EarthMath.GetAreaCodeY(vNWCornerLatitude, EarthConfig.mFetchLevel);
                Ex_Area.ExcludeAreaCodeYStop = EarthMath.GetAreaCodeY(vSECornerLatitude, EarthConfig.mFetchLevel);

                ExcludeTileCount = Ex_Area.TileCount();

                ExcludeDetails = EarthMath.GetSignLatitude(Ex_Area.ExcludeAreaStartLatitude);
                ExcludeDetails += EarthMath.GetWorldCoordGradPadded(Ex_Area.ExcludeAreaStartLatitude) + "° ";
                ExcludeDetails += EarthMath.GetWorldCoordMinutePadded(Ex_Area.ExcludeAreaStartLatitude) + "' ";
                ExcludeDetails += EarthMath.GetWorldCoordSecPadded(Ex_Area.ExcludeAreaStartLatitude) + "'' ";

                ExcludeDetails += ", ";
                ExcludeDetails += EarthMath.GetSignLongitude(Ex_Area.ExcludeAreaStartLongitude);
                ExcludeDetails += EarthMath.GetWorldCoordGradPadded(Ex_Area.ExcludeAreaStartLongitude) + "° ";
                ExcludeDetails += EarthMath.GetWorldCoordMinutePadded(Ex_Area.ExcludeAreaStartLongitude) + "' ";
                ExcludeDetails += EarthMath.GetWorldCoordSecPadded(Ex_Area.ExcludeAreaStartLongitude) + "'' ";

                ExcludeDetails += " X ";
                ExcludeDetails += EarthMath.GetSignLatitude(Ex_Area.ExcludeAreaStopLatitude);
                ExcludeDetails += EarthMath.GetWorldCoordGradPadded(Ex_Area.ExcludeAreaStopLatitude) + "° ";
                ExcludeDetails += EarthMath.GetWorldCoordMinutePadded(Ex_Area.ExcludeAreaStopLatitude) + "' ";
                ExcludeDetails += EarthMath.GetWorldCoordSecPadded(Ex_Area.ExcludeAreaStopLatitude) + "'' ";

                ExcludeDetails +=", ";
                ExcludeDetails += EarthMath.GetSignLongitude(Ex_Area.ExcludeAreaStopLongitude);
                ExcludeDetails += EarthMath.GetWorldCoordGradPadded(Ex_Area.ExcludeAreaStopLongitude) + "° ";
                ExcludeDetails += EarthMath.GetWorldCoordMinutePadded(Ex_Area.ExcludeAreaStopLongitude) + "' ";
                ExcludeDetails += EarthMath.GetWorldCoordSecPadded(Ex_Area.ExcludeAreaStopLongitude) + "'' ";

                ExcludeDetails += " : " + ExcludeTileCount + " tiles";
                lsbExcludeAreas.Items[idx] = ExcludeDetails;

                idx++;

            }





        }

        void UserSetsNewFirstAreaDrawFixPoint(Int64 iMousePosX, Int64 iMousePosY)
        {

            mEarthKML.Areas.Clear();
            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
            Double vNewAreaStartLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vNewAreaStartLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
            vNewAreaStartLatitude = EarthMath.CleanLatitude(vNewAreaStartLatitude);
            vNewAreaStartLongitude = EarthMath.CleanLongitude(vNewAreaStartLongitude);
            mAreaDrawFixPointLatitude = EarthMath.LimitLatitude(vNewAreaStartLatitude);
            mAreaDrawFixPointLongitude = EarthMath.LimitLongitude(vNewAreaStartLongitude);
            SetInputCornerCoordsTo(mAreaDrawFixPointLatitude, mAreaDrawFixPointLongitude, mAreaDrawFixPointLatitude, mAreaDrawFixPointLongitude);
        }

        void UserSetsNewAreaSecondPoint(Int64 iMousePosX, Int64 iMousePosY)
        {
            Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
            Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
            Double vNewAreaSecondLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
            Double vNewAreaSecondLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
            vNewAreaSecondLatitude = EarthMath.CleanLatitude(vNewAreaSecondLatitude);
            vNewAreaSecondLongitude = EarthMath.CleanLongitude(vNewAreaSecondLongitude);

            Double vOldAreaSecondLatitude;
            Double vOldAreaSecondLongitude;

            Double vDistPoint1 = ((mAreaDrawFixPointLongitude - mEarthInputArea.AreaStartLongitude) * (mAreaDrawFixPointLongitude - mEarthInputArea.AreaStartLongitude) +
                                  (mAreaDrawFixPointLatitude - mEarthInputArea.AreaStartLatitude) * (mAreaDrawFixPointLatitude - mEarthInputArea.AreaStartLatitude));

            Double vDistPoint2 = ((mAreaDrawFixPointLongitude - mEarthInputArea.AreaStopLongitude) * (mAreaDrawFixPointLongitude - mEarthInputArea.AreaStopLongitude) +
                                  (mAreaDrawFixPointLatitude - mEarthInputArea.AreaStopLatitude) * (mAreaDrawFixPointLatitude - mEarthInputArea.AreaStopLatitude));

            if (vDistPoint1 <= vDistPoint2)
            {
                vOldAreaSecondLatitude  = mEarthInputArea.AreaStopLatitude;
                vOldAreaSecondLongitude = mEarthInputArea.AreaStopLongitude;
            }
            else
            {
                vOldAreaSecondLatitude = mEarthInputArea.AreaStartLatitude;
                vOldAreaSecondLongitude = mEarthInputArea.AreaStartLongitude;
            }

            if (((vNewAreaSecondLongitude - vOldAreaSecondLongitude) > 180.0) ||
                ((vNewAreaSecondLongitude - vOldAreaSecondLongitude) < -180.0))
            {
                //DateBorderOverRun
                if (vNewAreaSecondLongitude < 0.0)
                {
                    vNewAreaSecondLongitude += 360.0;
                }
                else 
                {
                    vNewAreaSecondLongitude -= 360.0;
                }
            }

            vNewAreaSecondLatitude = EarthMath.LimitLatitude(vNewAreaSecondLatitude);
            vNewAreaSecondLongitude = EarthMath.LimitLongitude(vNewAreaSecondLongitude);
            //Set and sort Area Coords
            Double vNWCornerLatitude = mAreaDrawFixPointLatitude;
            Double vNWCornerLongitude = mAreaDrawFixPointLongitude;
            Double vSECornerLatitude = vNewAreaSecondLatitude;
            Double vSECornerLongitude = vNewAreaSecondLongitude;
            if (vNewAreaSecondLatitude > vNWCornerLatitude)
            {
                vNWCornerLatitude = vNewAreaSecondLatitude;
            }
            if (mAreaDrawFixPointLatitude < vSECornerLatitude)
            {
                vSECornerLatitude = mAreaDrawFixPointLatitude;
            }
            if (vNewAreaSecondLongitude < vNWCornerLongitude)
            {
                vNWCornerLongitude = vNewAreaSecondLongitude;
            }
            if (mAreaDrawFixPointLongitude > vSECornerLongitude)
            {
                vSECornerLongitude = mAreaDrawFixPointLongitude;
            }
            SetInputCornerCoordsTo(vNWCornerLatitude, vNWCornerLongitude, vSECornerLatitude, vSECornerLongitude);
        }

        void DragMap(Int64 iDeltaX, Int64 iDeltaY)
        {
            if ((iDeltaX != 0) || (iDeltaY != 0))
            {
                Double vDisplayCenterLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, Convert.ToDouble(iDeltaY), EarthConfig.mZoomLevel);
                Double vDisplayCenterLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, -Convert.ToDouble(iDeltaX), EarthConfig.mZoomLevel);
                vDisplayCenterLatitude = EarthMath.CleanLatitude(vDisplayCenterLatitude);
                vDisplayCenterLongitude = EarthMath.CleanLongitude(vDisplayCenterLongitude);
                DisplayThisPosition(vDisplayCenterLongitude, vDisplayCenterLatitude);
            }
        }


        bool MultiThreadedQueuesFinished()
        {
            bool multithreadedQueuesFinished = mMasksCompilerMultithreadedQueue != null && mImageProcessingMultithreadedQueue != null
                                    && mMasksCompilerMultithreadedQueue.AllDone() && mImageProcessingMultithreadedQueue.AllDone();

            return multithreadedQueuesFinished;
        }

        bool ImageToolThreadUnstarted()
        {
            return (mImageToolThread.ThreadState & ThreadState.Unstarted) == ThreadState.Unstarted;
        }

        bool ImageToolThreadFinished()
        {
            return mImageToolThread != null
                && ((mImageToolThread.ThreadState & ThreadState.Stopped) == ThreadState.Stopped)
                || ((mImageToolThread.ThreadState & ThreadState.Aborted) == ThreadState.Aborted);
        }

        private void CreateMeshFiles()
        {
            creatingMeshFile = true;
            createMeshFiles();
            creatingMeshFile = false;
        }

        private void FinishProcessingArea()
        {
            mAreaInfoAreaQueue.DoEmpty();

            SetVisibilityForIdle();

            mAreaProcessRunning = false; //Do here else HandleInput doesnt do anything
            mSkipAreaProcessFlag = true;
            HandleInputChange();
            DisplayWhereYouJustAreAgain();

            String vExitStatusFeedback = GetExitStatusFromFriendThread();

            if (vExitStatusFeedback.Length > 0)
            {
                SetStatus(vExitStatusFeedback);
            }

            mAllowDisplayToSetStatus = false; //Block Display from overwriting the Final Status
            mStopProcess = false;

            mMasksCompilerMultithreadedQueue.CompleteAdding();
            mImageProcessingMultithreadedQueue.CompleteAdding();
            if (!mMasksCompilerMultithreadedQueue.AllDone())
            {
                SetStatus("Waiting on FSEarthMasks, Undistortion, and FS Scenery Compiler threads to finish.");
            }
            else if (!mImageProcessingMultithreadedQueue.AllDone())
            {
                SetStatus("Waiting on image processing threads to finish.");
            }
            else if (EarthConfig.mCreateScenproc && ScenprocUtils.ScenProcRunning)
            {
                SetStatus("Waiting on Scenproc to finish");
                scenProcWasRunning = true;
            }
            EarthScriptsHandler.DoWhenEverthingIsDone(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);
        }

        private Boolean TryAdvancingToOtherArea()
        {
            Boolean CarryOn=false;
            Boolean vReallyFinish = true;

            while (!CarryOn )
            {
                if (!mAreaInfoAreaQueue.IsEmpty())
                {
                    CreateNextSingleAreaFromMultiArea();
                    //if (AreaHasTilesToDownload(mEarthArea))
                    if (CalculateTilesToDownload() > 0 && (CheckIfAreaIsEnabled()))
                    {
                        vReallyFinish = false;
                        QueueAreaTiles();
                        CarryOn = true;

                    }
                    else
                    {
                        mCurrentDownloadedTilesTotal += (mEarthArea.AreaTilesInX * mEarthArea.AreaTilesInY);
                    }

                }
                else
                {
                    CarryOn = true;

                }
            }

            return vReallyFinish;
        }

        void MainThreadTimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {


            if ((mFirstEvent) && (mFirstMainTimerEventHappend))
            {
                return;
            }

            mFirstMainTimerEventHappend = true;

            //Very First Event?
            if (mFirstEvent)
            {
                Size vKeepSize = this.Size;
                this.AutoSize = false; //Disable Auto Size  to avoid sizeing blocking.  AutoSize is used to adapt FSET for the very first start. This is required due a problem with eighter XP SP3 or small Displays. It sized FSET wrong and cut the status bar and the www key.
                this.Size        = vKeepSize;
                this.Refresh();
                this.MinimumSize = vKeepSize;

                //Flag Becomes reseted at the end
                if (EarthConfig.mUseCSharpScripts)
                {
                    //Install Scripts
                    SetStatus("Installing TileCodeingScript .. ");
                    EarthScriptsHandler.TryInstallTileCodeingScript(EarthConfig.mStartExeFolder, EarthConfig.mStartExeFolder);

                    SetStatus("Installing AreaInfoFileCreationScript .. ");
                    EarthScriptsHandler.TryInstallAreaInfoFileCreationScript(EarthConfig.mStartExeFolder, EarthConfig.mStartExeFolder);

                    SetStatus("Installing CustomizedProcessesScript .. ");
                    EarthScriptsHandler.TryInstallCustomizedProcessesScript(EarthConfig.mStartExeFolder, EarthConfig.mStartExeFolder);
                }

                //test
                //EarthScriptsHandler.MapAreaCoordToTileCode(10, 11, 2, "xyz");

                if (EarthConfig.mOpenWebBrowsersOnStart)
                {
                    WWWButton_Click(null, null);
                }

                SetStatus("... Testing Available Memory... ");
                mEarthAreaTexture.MaxBitmapSizeEvaluation();

                EditRefButton_Click(null, null);

                //Set Focus to Zoom Box so User can right ahead Zoom out
                ZoomSelectorBox.Focus();
            }


            if (mMapScrollingOnUserDrawing)
            {
                MapScrollingOnUserDrawing(mUserDrawingMousePosXScrolling, mUserDrawingMousePosYScrolling);
            }

            //Handle WorldWideWeb TickEvent and CloseEvent (attention not thread safe)
            if (mEarthWeb != null)
            {
                mEarthWeb.TimerTick();

                if (!mEarthWeb.Created)
                {
                    if (mEarthWeb.CheckForTileOrdered() || mEarthWeb.CheckForTileOfWWWEngine())
                    {
                        TileInfo vTileInfo = new TileInfo();

                        if (mEarthWeb.CheckForTileOrdered())
                        {
                            vTileInfo = mEarthWeb.GetOrderedTileInfoAndAbortProcess();
                        }

                        if (mEarthWeb.CheckForTileOfWWWEngine())
                        {
                            Tile vTileOfWWWEngine = mEarthWeb.GetTileOfWWWEngine();
                            vTileInfo = vTileOfWWWEngine.mTileInfo.Clone();
                        }


                        //Resque Tile in lose (else we get Engine leaks)
                        if (mAreaProcessRunning)
                        {
                            mTileInfoAreaQueue.AddTileInfo(vTileInfo);
                        }
                        else
                        {
                            //For Display Remove from Control List so it becomes sheduled again
                            if (mToEngineDelegatedTilesControllList1.Exists(vTileInfo.Equals))
                            {
                                TileInfo vTileInfoToRemoveWWW = mToEngineDelegatedTilesControllList1.Find(vTileInfo.Equals);
                                mToEngineDelegatedTilesControllList1.Remove(vTileInfoToRemoveWWW);
                            }
                            if (mToEngineDelegatedTilesControllList2.Exists(vTileInfo.Equals))
                            {
                                TileInfo vTileInfoToRemoveWWW = mToEngineDelegatedTilesControllList2.Find(vTileInfo.Equals);
                                mToEngineDelegatedTilesControllList2.Remove(vTileInfoToRemoveWWW);
                            }
                        }
                    }

                    mEarthWeb.Dispose();
                    mEarthWeb = null;
                    Focus();
                }
            }

            bool shouldCreateMesh = EarthConfig.mCreateWaterMaskBitmap && EarthConfig.mCreateAreaMask
                    && mAreaProcessRunning && !creatingMeshFile && mLastMeshCreatedEarthArea != mEarthArea;
            if (shouldCreateMesh)
            {
                ThreadStart ts = new ThreadStart(CreateMeshFiles);
                mCreateMeshThread = new Thread(ts);
                creatingMeshFile = true;
                mLastMeshCreatedEarthArea = mEarthArea;
                mCreateMeshThread.Start();
            }

            if (creatingMeshFile)
            {
                return;
            }

            if (mAreaProcessRunning && EarthConfig.skipAllWaterTiles && mLastCheckedForAllWaterEarthArea != mEarthArea)
            {
                mLastCheckedForAllWaterEarthArea = mEarthArea;
                if (AreaAllWater())
                {
                    if (mAreaInfoAreaQueue.IsEmpty())
                    {
                        FinishProcessingArea();
                    }
                    else
                    {
                        TryAdvancingToOtherArea();
                    }
                    return;
                }
            }

            HarvestThreadEnginesOutput();
            FeedThreadEnginesWithNewWork();


            // scenproc wasn't done when the area process was done. but now it is. so inform user so they aren't confused
            bool multithreadedQueuesFinished = MultiThreadedQueuesFinished();
            bool shouldStartImageTool = multithreadedQueuesFinished && EarthConfig.mSceneryCompiler == EarthConfig.mFS2004SceneryCompiler
                                        && mImageToolThread != null
                                        && ImageToolThreadUnstarted();
            if (shouldStartImageTool)
            {
                mImageToolThread.Start();
            }
            if (multithreadedQueuesFinished && !mAreaProcessRunning)
            {
                if (mImageToolThread != null && mImageToolThread.IsAlive)
                {
                    SetStatus("Waiting on ImageTool to finish.");
                }
                // the below else if and else can be refactored into 1 else block, but I think how it is now is more
                // readable
                else if (!scenProcWasRunning || !ScenprocUtils.ScenProcRunning)
                {
                    // scenproc wasn't running, or it was but now it's not
                    SetStatus("Done.");
                    scenProcWasRunning = false;
                    mMasksCompilerMultithreadedQueue.SetTotalJobsDone(0);
                }
                else
                {
                    SetStatus("Done.");
                    mMasksCompilerMultithreadedQueue.SetTotalJobsDone(0);
                }
            }

            if (mStopProcess)
            {
                mAreaProcessRunning = false;
            }

            //Get Status from Area Processing Friend Thread
            if (mAreaProcessRunning)
            {
                String vStatusFeedback = GetStatusFromFriendThread();

                if (vStatusFeedback.Length > 0)
                {
                    SetStatus(vStatusFeedback);
                }

                // make sure that we don't download more than 10 areas ahead of how many areas we've resampled
                // this has the benefit of preventing OOM from keeping too many downloaded areas in memory
                // also frees up cpu time to resample etc as a minor side effect
                long deficit =  mCurrentActiveAreaNr - mImageProcessingMultithreadedQueue.GetTotalJobsDone();
                //Finish work is when thread exited
                if (deficit < 10 && mAreaAftermathThread != null) //can be zero already on a close application concurrency.
                {
                    if (mAreaAftermathThread.ThreadState == ThreadState.Stopped)
                    {
                        //Thread exited create new one to reset status to unstarted
                        ThreadStart vAreaAftermathDelegate = new ThreadStart(AreaAfterDownloadProcessing);
                        mAreaAftermathThread = new Thread(vAreaAftermathDelegate);

                        Boolean vReallyFinish = true;

                        if (mMultiAreaMode && !mStopProcess)
                        {
                            vReallyFinish = TryAdvancingToOtherArea();
                        }

                        if (vReallyFinish)
                        {
                            FinishProcessingArea();

                        }

                    }
                }
            }

            //Very First Event?
            if (mFirstEvent)
            {
                mFirstEvent = false;

                EarthScriptsHandler.DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);

                if (EarthConfig.mAutoStartDownload)
                {
                    //Autostart Download
                    StartDownload();
                }
            }

            if ((mApplicationExitCheck) && (!mAreaProcessRunning))
            {
                mApplicationExitCheck = false;
                if (EarthConfig.mAutoExitApplication)
                {
                    Application.Exit();
                }
            }

            //For Debug the counters have to return to zero on idle if all is ok
            //DlQueueLabel.Invalidate();
            //DlQueueLabel.Text = Convert.ToString(mToEngineDelegatedTilesControllList1.Count) + "  -  " + Convert.ToString(mToEngineDelegatedTilesControllList2.Count);
            //DlQueueLabel.Refresh();

        }
        

        private void NormalPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mAreaProcessRunning)
            {


                // Control key is pressed so ignore mouse movement
                if (!(Control.ModifierKeys ==Keys.Control))
                {

                    if (mMouseDownFlag)
                    {
                        if (mUserSetCenterActive)
                        {
                            UserSetsNewCenter(e.X, e.Y);
                            DisplayWhereYouJustAreAgain();
                            MapScrollingOnUserDrawing(e.X, e.Y);
                            mMapScrollingOnUserDrawing = true;
                        }
                        else if (mUserDrawAreaActive)
                        {
                            UserSetsNewAreaSecondPoint(e.X, e.Y);
                            DisplayWhereYouJustAreAgain();
                            MapScrollingOnUserDrawing(e.X, e.Y);
                            mMapScrollingOnUserDrawing = true;
                        }

                        else if (mUserDrawExcludeArea)
                        {
                            // Set new 2nd point of exclude area routine ...
                            UserSetsNewExcludeAreaSecondPoint(e.X, e.Y);
                            DisplayWhereYouJustAreAgain();
                            MapScrollingOnUserDrawing(e.X, e.Y);
                            mMapScrollingOnUserDrawing = true;
                        }
                        else
                        {
                            Int64 vDeltaX = e.X - mMouseDownX;
                            Int64 vDeltaY = e.Y - mMouseDownY;
                            DragMap(vDeltaX, vDeltaY);
                        }

                        mMouseDownX = e.X;
                        mMouseDownY = e.Y;
                    }
                    else
                    {
                        if ((!mUserDrawAreaActive) && (!mUserSetCenterActive) && (!mUserDrawExcludeArea))
                        {

                            //Handle Cursor;
                            tCursor vNewCursor = tCursor.eDefault;
                            Boolean vContinue = true;
                            Boolean vOverHotSpotCenter = false;
                            vOverHotSpotCenter = IsOverHotSpotCenter(e.X, e.Y);
                            if (vOverHotSpotCenter && vContinue)
                            {
                                vNewCursor = tCursor.eCenter;
                                vContinue = false;
                            }
                            vOverHotSpotCenter = IsOverHotSpotNWCorner(e.X, e.Y);
                            if (vOverHotSpotCenter && vContinue)
                            {
                                vNewCursor = tCursor.eCorners;
                                vContinue = false;
                            }
                            vOverHotSpotCenter = IsOverHotSpotSECorner(e.X, e.Y);
                            if (vOverHotSpotCenter && vContinue)
                            {
                                vNewCursor = tCursor.eCorners;
                                vContinue = false;
                            }
                            vOverHotSpotCenter = IsOverHotSpotSWCorner(e.X, e.Y);
                            if (vOverHotSpotCenter && vContinue)
                            {
                                vNewCursor = tCursor.eCorners;
                                vContinue = false;
                            }
                            vOverHotSpotCenter = IsOverHotSpotNECorner(e.X, e.Y);
                            if (vOverHotSpotCenter && vContinue)
                            {
                                vNewCursor = tCursor.eCorners;
                                vContinue = false;
                            }

                            if (vNewCursor != mActiveCursor)
                            {
                                switch (vNewCursor)
                                {
                                    case tCursor.eDefault: Cursor = Cursors.Default; break;
                                    case tCursor.eCorners: Cursor = Cursors.Cross; break;
                                    case tCursor.eCenter: Cursor = Cursors.Hand; break;
                                    case tCursor.eBorderLeftRight: Cursor = Cursors.SizeWE; break;
                                    case tCursor.eBorderUpDown: Cursor = Cursors.SizeNS; break;
                                    default: Cursor = Cursors.Default; break;
                                }
                                mActiveCursor = vNewCursor;
                            }
                        }
                    }
                }
            }
        }

        void EnterUserCenterMovementMode()
        {
            AreaDefModeBox.Text = "1Point";
            AreaDefModeBox.Enabled = false;
            mUserDrawAreaActive = false;
            mUserSetCenterActive = true;
            //Cursor = new Cursor(Cursors.Hand.Handle);
            Cursor = Cursors.Hand;
            mActiveCursor = tCursor.eCenter;
        }

        void EnterUserAreaDrawMode()
        {
            AreaDefModeBox.Text = "2Points";
            AreaDefModeBox.Enabled = false;
            mUserSetCenterActive = false;
            mUserDrawAreaActive = true;
            mUserDrawExcludeArea = false;
            //Cursor = new Cursor(Cursors.Cross.Handle);
            Cursor = Cursors.Cross;
            mActiveCursor = tCursor.eCorners;
        }

        private void DoWebBrowser(String vWebAddress)
        {
            Int64 vMaximumTicksTillRecover = 1000000000;

            if (mMainThreadTimer != null)
            {
                vMaximumTicksTillRecover = (1000 * EarthConfig.mWebBrowserRecoverTimer) / (Int64)(mMainThreadTimer.Interval) + 1;
            }

            if (mEarthWeb == null)
            {
                mEarthWeb = new EarthWebForm(vMaximumTicksTillRecover, mNoTileFound, EarthConfig.mWebBrowserNoTileFoundKeyWords);
            }

            if (!mEarthWeb.Created)
            {
                //mEarthWeb = new EarthWebForm(vMaximumTicksTillRecover);
                //mEarthWeb.GoToPage(vWebAddress);
                mEarthWeb.GoToPage(vWebAddress);
                //mEarthWeb.ShowDialog();
                mEarthWeb.Show(this);
                
                //Thread.Sleep(4000);
            }

            mEarthWeb.GoToPage(vWebAddress);
            //mCookieContent = mEarthWeb.GetCookieContent();
            //mEarthWeb.Dispose();
            //mEarthWeb = null;

            //SetCookies();

             // test code
            /*
            Uri myTileUri = new Uri("http://www.simw.com/");
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(myTileUri);

            request.CookieContainer = new CookieContainer();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //System.Net.WebResponse response = request.GetResponse();
            //System.IO.Stream vStream;
            //vStream = response.GetResponseStream();

            Cookie myCookie;

            for (int i = 0; i < response.Cookies.Count; i++)
            {
                myCookie = response.Cookies[i];
            } 
             */
            
        }

        private void HandleWebBrowser(String vWebAddress)
        {
            EarthCommon.CollectGarbage();
            DoWebBrowser(vWebAddress);
            EarthCommon.CollectGarbage();
        }

        private void PlaceButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                EnterUserCenterMovementMode();
            }
        }


        private void EditRefButton_MouseUp(object sender, MouseEventArgs e)
        {

            if (e.Button.Equals(MouseButtons.Right))
            {
                ReleaseAnyDrawMode();
            }
        }


        private void PlaceButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Right))
            {
                ReleaseAnyDrawMode();
            }
        }

        private void FSEarthTilesForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Right))
            {
                ReleaseAnyDrawMode();
            }
        }


        private void FSEarthTilesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            EarthScriptsHandler.DoOnFSEarthTilesClose(mEarthArea.Clone(), GetAreaFileString(), mEarthMultiArea.Clone(), mCurrentAreaInfo.Clone(), mCurrentActiveAreaNr, mCurrentDownloadedTilesTotal, mMultiAreaMode);
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
            if (mThreadsStarted) //no compiler warning for not used
            {
                //nothign to do
            }
            for (int i = 0; i < mEngineThreads.Count; i++)
            {
                Thread mEngineThread = mEngineThreads[i];
                if (mEngineThread != null)
                {
                    mEngineThread.Abort();
                    mEngineThread = null;
                    mEngineThreads[i] = mEngineThread;
                }
            }
            if (mAreaAftermathThread != null)
            {
                mAreaAftermathThread.Abort();
                mAreaAftermathThread = null;
            }
            if (mEarthWeb != null)
            {
                mEarthWeb.Close();
                mEarthWeb.Dispose();
                mEarthWeb = null;
            }
            if (mMasksCompilerMultithreadedQueue != null)
            {
                mMasksCompilerMultithreadedQueue.Stop();
            }
            if (mImageProcessingMultithreadedQueue != null)
            {
                mImageProcessingMultithreadedQueue.Stop();
            }
            if (mImageToolThread != null && mImageToolThread.IsAlive)
            {
                mImageToolThread.Abort();
            }
            if (mCreateMeshThread != null)
            {
                mCreateMeshThread.Abort();
                mCreateMeshThread = null;
            }
            EarthScriptsHandler.CleanUp();
        }

        private void DrawButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Right))
            {
                ReleaseAnyDrawMode();
            }
        }


        private void EditRefButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                if (mMultiAreaMode)
                {
                    mEarthMultiAreaBackupInputArea.Copy(mEarthInputArea);
                    mEarthMultiAreaBackupArea.Copy(mEarthMultiArea);
                    mMultiAreaBackupValid = true;

                    EditRefButton.Invalidate();
                    EditRefButton.Text = "Go Back";
                    EditRefButton.Refresh();

                    mMultiAreaMode = false;

                    RefModeButton.Visible = false;
                    SetFixMode();

                    if (mSingleReferenceAreaValid)
                    {
                        mEarthInputArea.Copy(mEarthSingleReferenceInputArea);
                        mEarthArea.Copy(mEarthSingleReferenceArea); //not really required because it becomes recalculate anyway but we do it

                        EarthInputArea vEarthInputArea = new EarthInputArea();  //create a clone/copy else it changes its context if update is not blocked below
                        vEarthInputArea.Copy(mEarthInputArea);

                        Boolean vKeepConfigInitDone = mConfigInitDone;
                        mConfigInitDone = false; //Block HandleInput Change so we proper update at the end when everything is filled in. Also a lot faster.
                        FillInputAreaDatasIntoForm(vEarthInputArea);
                        mConfigInitDone = vKeepConfigInitDone;
                    }

                    HandleInputChange();
                    DisplayAreaInFull();

                }
                else
                {
                    if ((mEarthInputArea.AreaXSize > 0.0) && (mEarthInputArea.AreaYSize > 0.0))
                    {
                        mEarthSingleReferenceInputArea.Copy(mEarthInputArea);
                        mEarthSingleReferenceArea.Copy(mEarthArea);
                        mSingleReferenceAreaValid = true;

                        EditRefButton.Invalidate();
                        EditRefButton.Text = "Edit Ref";
                        EditRefButton.Refresh();

                        RefModeButton.Visible = true;
                        RefModeButton.Refresh();

                        mMultiAreaMode = true;


                        if (mMultiAreaBackupValid)
                        {
                            mEarthInputArea.Copy(mEarthMultiAreaBackupInputArea);
                            mEarthMultiArea.Copy(mEarthMultiAreaBackupArea); //not really required because it becomes recalculate anyway but we do it

                            EarthInputArea vEarthInputArea = new EarthInputArea();  //create a clone/copy else it changes its context if update is not blocked below
                            vEarthInputArea.Copy(mEarthInputArea);

                            Boolean vKeepConfigInitDone = mConfigInitDone;
                            mConfigInitDone = false; //Block HandleInput Change so we proper update at the end when everything is filled in. Also a lot faster.
                            FillInputAreaDatasIntoForm(vEarthInputArea);
                            mConfigInitDone = vKeepConfigInitDone;
                        }

                        HandleInputChange();
                        DisplayAreaInFull();

                    }
                    else
                    {
                        SetStatus("Can not enter Multi Area Mode. Your Reference Area Size is zero.");
                        Thread.Sleep(2000);
                    }

                }
            }
        }


        private void DrawButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                EnterUserAreaDrawMode();
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
            }
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            SetNextProxy();
        }

        private void WWWButton_Click(object sender, EventArgs e)
        {
            /*
            //-- Cookie Access Test Code --
            Uri myCookieUri = new Uri("http://www.******.com/ncr");
            System.Net.HttpWebRequest cookierequest = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(myCookieUri);
            //cookierequest.Referer = vServiceReference;
            cookierequest.CookieContainer = new CookieContainer();
            //if (mCookies.Count > 0)
            //{
            //    cookierequest.CookieContainer.Add(mCookies);
            //}

            //if (!EarthCommon.StringCompare(mProxy, "direct"))
            //{
            //   WebProxy vProxy = new WebProxy("http://" + mProxy + "/", true);
            //   request.Proxy = vProxy;
            //}

            HttpWebResponse cookieresponse = (HttpWebResponse)cookierequest.GetResponse();

            if (cookieresponse.Cookies.Count > 0)
            {
                //mCookies = cookieresponse.Cookies;
            }

            System.IO.Stream vCookieDummyStream;
            vCookieDummyStream = cookieresponse.GetResponseStream();
            vCookieDummyStream.Close(); //Close Stream
            //-----------------
            */

            if (mConfigInitDone)
            {
                Boolean mIsWebEngine = (mEarthWeb != null);

                if (mIsWebEngine)
                {
                    mIsWebEngine = mEarthWeb.Created;
                }

                if (!mIsWebEngine)
                {
                    if (!mAreaProcessRunning)
                    {
                        SetLastTileInfoToDisplayCenter();
                        EmptyAllJobQueues();
                    }
                    String vWebAddress = EarthScriptsHandler.CreateWebAddress(mLastTileInfo.mAreaCodeX, mLastTileInfo.mAreaCodeY, mLastTileInfo.mLevel, mLastTileInfo.mService);
                    //test vWebAddress = "http://www.simw.com/";
                    HandleWebBrowser(vWebAddress);
                }
            }
        }

        private void FSEarthTilesForm_Resize(object sender, EventArgs e)
        {

            if (mWindowElementPosition.mInitialized)
            {
                Int32 vXNew = this.Width;
                Int32 vYNew = this.Height;

                Int32 vXDelta = vXNew - mWindowElementPosition.mWindowOriginalWidth;
                Int32 vYDelta = vYNew - mWindowElementPosition.mWindowOriginalHeight;

                if (vXDelta < 0)
                {
                    vXDelta = 0;
                }

                if (vYDelta < 0)
                {
                    vYDelta = 0;
                }

                if ((vXDelta >= 0) && (vYDelta >= 0))
                {
                    if ((vXDelta % 2) != 0)
                    {
                        vXDelta -= 1;
                    }

                    if ((vYDelta % 2) != 0)
                    {
                        vYDelta -= 1;
                    }

                    if ((vXDelta >= 0) && (vYDelta >= 0))
                    {

                        WebBrowserPanicButton.Top = mWindowElementPosition.mWebBrowserPanicButtonYPos + vYDelta;
                        WebBrowserPanicButton.Left = mWindowElementPosition.mWebBrowserPanicButtonXPos + vXDelta;
                        WWWButton.Top = mWindowElementPosition.mWWWButtonYPos + vYDelta;
                        WWWButton.Left = mWindowElementPosition.mWWWButtonXPos + vXDelta;
                        StatusBox.Top = mWindowElementPosition.mStatusBoxYPos + vYDelta;
                        StatusBox.Width = mWindowElementPosition.mStatusBoxWidth + vXDelta;
                        VersionsLabel.Top = mWindowElementPosition.mVersionsLabelYPos + vYDelta;
                        VersionsLabel.Left = mWindowElementPosition.mVersionsLabelXPos + vXDelta;

                        MemoryGroupBox.Left = mWindowElementPosition.mMemoryGroupBoxXPos + vXDelta;
                        CookieLabel.Left = mWindowElementPosition.mCookieLabelXPos + vXDelta;

                        DlQueueLabel.Left = mWindowElementPosition.mDlQueueLabelXPos + vXDelta;
                        ProgressQueue1Box.Left = mWindowElementPosition.mProgressQueue1Box1XPos + vXDelta;
                        ProgressQueue2Box.Left = mWindowElementPosition.mProgressQueue1Box2XPos + vXDelta;
                        ProgressQueue3Box.Left = mWindowElementPosition.mProgressQueue1Box3XPos + vXDelta;
                        ProgressQueue4Box.Left = mWindowElementPosition.mProgressQueue1Box4XPos + vXDelta;
                        ServiceSourcesLabel.Left = mWindowElementPosition.mServiceSourcesLabelXPos + vXDelta;

                        NextButton.Left = mWindowElementPosition.mNextButtonXPos + vXDelta;
                        ProxyLabel.Left = mWindowElementPosition.mProxyLabelXPos + vXDelta;
                        ZoomSelectorBox.Left = mWindowElementPosition.mZoomSelectorBoxXPos + vXDelta;
                        ZoomLabel.Left = mWindowElementPosition.mZoomLabelXPos + vXDelta;
                        JumpToCornerLabel.Left = mWindowElementPosition.mJumpToCornerLabelXPos + vXDelta;
                        ResolutionTableLabel.Left = mWindowElementPosition.mResolutionTableLabelXPos + vXDelta;
                        SmallPicturePox.Left = mWindowElementPosition.mSmallPictureBoxXPos + vXDelta;
                        DisplayNWButton.Left = mWindowElementPosition.mDisplayNWButtonXPos + vXDelta;
                        DisplayNEButton.Left = mWindowElementPosition.mDisplayNEButtonXPos + vXDelta;
                        DisplaySWButton.Left = mWindowElementPosition.mDisplaySWButtonXPos + vXDelta;
                        DisplaySEButton.Left = mWindowElementPosition.mDisplaySEButtonXPos + vXDelta;
                        DisplayCenterButton.Left = mWindowElementPosition.mDisplayCenterButtonXPos + vXDelta;

                        BitmapGroupBox.Width = mWindowElementPosition.mBitmapGroupBoxWidth + vXDelta;
                        BitmapGroupBox.Height = mWindowElementPosition.mBitmapGroupBoxHeight + vYDelta;
                        NormalPictureBox.Width = mWindowElementPosition.mNormalPictureBoxWidth + vXDelta;
                        NormalPictureBox.Height = mWindowElementPosition.mNormalPictureBoxHeight + vYDelta;

                        //this.Invalidate();
                        //this.Refresh();
                        if (!mAreaProcessRunning)
                        {
                            DisplayWhereYouJustAreAgain();
                        }
                    }

                }
            }
        }


        private void CreateMasksBox_TextChanged(object sender, EventArgs e)
        {
           HandleInputChange();
        }

        private void RefModeButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                if (mAutoReference)
                {
                    SetFixMode();
                }
                else
                {
                    SetAutoMode();
                }
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
            }
        }

        private void RefModeButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button.Equals(MouseButtons.Right))
            {
                ReleaseAnyDrawMode();
            }
        }



        private List<String> CreateExportHeader()
        {
            List<String> vList = new List<String>();
            vList.Add("#--- Partial FS Earth Tiles ini datas ---");
            vList.Add("");
            vList.Add("[FSEarthTiles]");
            return vList;
        }

        private void AddSwitchesToExport(ref List<String> xList)
        {
            xList.Add("");
            xList.Add("DownloadResolution       = " + Convert.ToInt32(EarthConfig.mFetchLevel));
            xList.Add("StartWithService         = " + ServiceBox.Text);
            xList.Add("SelectedSceneryCompiler  = " + CompilerSelectorBox.Text);
            xList.Add("AreaSnap                 = " + AreaSnapBox.Text);
            xList.Add("CreateAreaMaskBmp        = " + CreateMasksBox.Text);
            xList.Add("CompileScenery           = " + CompileSceneryBox.Text);
            xList.Add("AutoReferenceMode        = " + AutoRefSelectorBox.Text);
            xList.Add("CreateScenproc           = " + CreateScenprocBox.Text);
        }

        private List<String> CreateExportData()
        {
            List<String> vList = CreateExportHeader();
            
            vList.Add("");

            vList.Add("AreaDefinitionMode       = 2Points");
            
            String vSign;

            vSign = "north";
            if( mEarthArea.AreaSnapStartLatitude<0.0)
            {
              vSign = "south";
            }

            vList.Add("NorthWestCornerLatitude  = " 
                + EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStartLatitude) + "deg "
                + EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStartLatitude) + "min "
                + EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStartLatitude) + "sec "
                + vSign);

            vSign = "east";
            if(mEarthArea.AreaSnapStartLongitude<0.0)
            {
              vSign = "west";
            }

            vList.Add("NorthWestCornerLongitude = " 
                + EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStartLongitude) + "deg "
                + EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStartLongitude) + "min "
                + EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStartLongitude) + "sec "
                + vSign);
            
            vSign = "north";
            if( mEarthArea.AreaSnapStopLatitude<0.0)
            {
              vSign = "south";
            }

            vList.Add("SouthEastLatitude        = "
                + EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStopLatitude) + "deg "
                + EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStopLatitude) + "min "
                + EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStopLatitude) + "sec "
                + vSign);

            vSign = "east";
            if(mEarthArea.AreaSnapStopLongitude<0.0)
            {
              vSign = "west";
            }

            vList.Add("SouthEastLongitude       = " 
                + EarthMath.GetWorldCoordGrad(mEarthArea.AreaSnapStopLongitude) + "deg "
                + EarthMath.GetWorldCoordMinute(mEarthArea.AreaSnapStopLongitude) + "min "
                + EarthMath.GetWorldCoordSec(mEarthArea.AreaSnapStopLongitude) + "sec "
                + vSign);

            int idx;
            for (idx=0; idx<vExcludeAreas.Areas.Count;idx++)
            {
                vSign = "north";
                if( mEarthArea.AreaSnapStopLatitude<0.0)
                {
                    vSign = "south";
                }

                vList.Add("ExcludeNWLat" + idx + "            = "
                    + EarthMath.GetWorldCoordGrad(vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude ) + "deg "
                    + EarthMath.GetWorldCoordMinute(vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude) + "min "
                    + EarthMath.GetWorldCoordSec(vExcludeAreas.Areas[idx].ExcludeAreaStartLatitude) + "sec "
                    + vSign);
                          
                vSign = "east";
                if(mEarthArea.AreaSnapStopLongitude<0.0)
                {
                    vSign = "west";
                }

                vList.Add("ExcludeNWLon" + idx + "            = "
                + EarthMath.GetWorldCoordGrad(vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude) + "deg "
                + EarthMath.GetWorldCoordMinute(vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude) + "min "
                + EarthMath.GetWorldCoordSec(vExcludeAreas.Areas[idx].ExcludeAreaStartLongitude) + "sec "
                + vSign);

                vSign = "north";
                if (mEarthArea.AreaSnapStopLatitude < 0.0)
                {
                    vSign = "south";
                }

                vList.Add("ExcludeSELat" + idx + "            = "
                    + EarthMath.GetWorldCoordGrad(vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude) + "deg "
                    + EarthMath.GetWorldCoordMinute(vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude) + "min "
                    + EarthMath.GetWorldCoordSec(vExcludeAreas.Areas[idx].ExcludeAreaStopLatitude) + "sec "
                    + vSign);

                vSign = "east";
                if (mEarthArea.AreaSnapStopLongitude < 0.0)
                {
                    vSign = "west";
                }

                vList.Add("ExcludeSELon" + idx + "            = "
                + EarthMath.GetWorldCoordGrad(vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude) + "deg "
                + EarthMath.GetWorldCoordMinute(vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude) + "min "
                + EarthMath.GetWorldCoordSec(vExcludeAreas.Areas[idx].ExcludeAreaStopLongitude) + "sec "
                + vSign);
            }
                
            return vList;
            


            
        }

        private List<String> CreateExportDataWithFolders()
        {
            List<String> vList = CreateExportData();
            vList.Add("");
            vList.Add("WorkingFolder            = " + EarthConfig.mWorkFolder);
            vList.Add("SceneryFolder            = " + EarthConfig.mSceneryFolder);
            return vList;
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                Clipboard.Clear();
                List<String> vList = CreateExportDataWithFolders();
                String vClipBoardString = "";
                foreach (String vString in vList)
                {
                    vClipBoardString += vString;
                    vClipBoardString += Environment.NewLine;
                }
                Clipboard.SetText(vClipBoardString);
            }
        }

        private void PastButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mEarthKML.Areas.Clear();
                mUsingKML = false;
                if (Clipboard.ContainsText())
                {
                    List<String> vList = new List<String>();
                    String vClipBoardString = Clipboard.GetText();
                    String vTempString = "";
                    Int32 vIndex = vClipBoardString.IndexOf(Environment.NewLine, StringComparison.CurrentCultureIgnoreCase);
                    while (vIndex >= 0)
                    {
                        vTempString = vClipBoardString.Remove(vIndex);
                        vList.Add(vTempString);
                        vClipBoardString = vClipBoardString.Substring(vIndex + Environment.NewLine.Length);
                        vIndex = vClipBoardString.IndexOf(Environment.NewLine, StringComparison.CurrentCultureIgnoreCase);
                    }
                    if (vClipBoardString.Length > 0)
                    {
                        vList.Add(vClipBoardString);
                    }
                    if (vList.Count > 0)
                    {
                        ProcessHotPlugInConfigList(vList);
                    }
                }
            }
        }


        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                PrepareWorkingDirectory(false);

                List<String> vList = CreateExportData();

                StreamWriter vStream;
                String vFilename = EarthConfig.mWorkFolder + "\\" + "PartialFSEarthTiles.ini";

                vStream = new StreamWriter(vFilename);

                if (vStream != null)
                {
                    foreach (String vString in vList)
                    {
                        vStream.WriteLine(vString);
                    }
                    vStream.Close();
                }
            }
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                mUsingKML = false;
                mEarthKML.Areas.Clear();
                StreamReader vStream;

                String vString;

                List<String> vList = new List<String>();

                String vFilename = EarthConfig.mWorkFolder + "\\" + "PartialFSEarthTiles.ini";

                try
                {
                    vStream = new StreamReader(vFilename);

                    if (vStream != null)
                    {
                        //Read first Line
                        vString = vStream.ReadLine();
                        while (vString != null)
                        {
                            vList.Add(vString);
                            //Read next Line
                            vString = vStream.ReadLine();
                        }
                        vStream.Close();
                    }
                    if (vList.Count > 0)
                    {
                        ProcessHotPlugInConfigList(vList);
                    }
                }
                catch
                {
                    //No File do nothing

                }
                DisplayThisFreePosition();
            }
        }

        private void KmlButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                String vFile = EarthConfig.mWorkFolder + "\\" + "AreaKML.kml";
                mUsingKML = true;
                DoLoadKMLFile(vFile);
                
            }
        }

        private void DoLoadKMLFile(String iFile)
        {
            mEarthKML.LoadKMLFile(iFile);

            if (mEarthKML.IsValid())
            {
                if (!mMultiAreaMode)
                {
                    EditRefButton_Click(null, null); //change to mulity mode
                }
                AreaDefModeBox.Text = "2Points";
                SetInputCornerCoordsTo(mEarthKML.StartLatitude, mEarthKML.StartLongitude, mEarthKML.StopLatitude, mEarthKML.StopLongitude);
               
                               
                HandleInputChange();
                DisplayAreaInFull();
            }
        }

        private void ProcessHotPlugInConfigList(List<String> iDirectConfigurationList)
        {
            //copy one time InputStart datas to config. Required for the case that we have no coords passed.
            EarthConfig.mAreaInputStart.Copy(mEarthInputArea);

            if (EarthConfig.ParseConfigurationList(iDirectConfigurationList))
            {
                Boolean vKeepConfigInitDone = mConfigInitDone;
                mConfigInitDone = false;
                if (FillInFormWithEarthConfigValues())
                {
                    mConfigInitDone = vKeepConfigInitDone; //required here also for the next step
                    
                    // Update our exclude areas from the ones in the config area
                    // Clear the listbox and add the new zones
                    vExcludeAreas = EarthConfig.mExcludeAreasConfig;
                    lsbExcludeAreas.Items.Clear();
                    for (int idx = 0; idx < vExcludeAreas.Areas.Count; idx++)
                    {
                        lsbExcludeAreas.Items.Add("");
                    }
                    DisplayWhereYouJustAreAgain();
                    UpdateExcludeDetails();
                    HandleInputChange();
                    DisplayAreaInFull();
                }
                //old config should still be vaild
                mConfigInitDone = vKeepConfigInitDone;
            }
        }


        // FSEarthTilesInternalInterface Implementation
        void FSEarthTilesInternalInterface.SetStatus(String iStatus)
        {
            SetStatus(iStatus);
        }

        void FSEarthTilesInternalInterface.SetStatusFromFriendThread(String iStatus)
        {
            SetStatusFromFriendThread(iStatus);
        }

        void FSEarthTilesInternalInterface.SetExitStatusFromFriendThread(String iStatus)
        {
            SetExitStatusFromFriendThread(iStatus);
        }

        private void InputGroupBox_DragEnter(object sender, DragEventArgs e)
        {
            if ((e.Data.GetDataPresent(DataFormats.FileDrop, false)) ||
                 (e.Data.GetDataPresent(DataFormats.Text, false)))
            {
                e.Effect = DragDropEffects.All;
            }

        }


        private void InputGroupBox_DragDrop(object sender, DragEventArgs e)
        {
            DoDrop(sender,e);
        }

        private void DoDrop(object sender, DragEventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop, false))
                {
                    String[] vFiles = (String[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (String vFile in vFiles)
                    {
                        if (EarthCommon.StringContains(vFile, ".kml"))
                        {
                            DoLoadKMLFile(vFile);
                        }
                        else
                        {
                            StreamReader vStream;

                            String vString;

                            List<String> vList = new List<String>();

                            try
                            {
                                vStream = new StreamReader(vFile);

                                if (vStream != null)
                                {
                                    //Read first Line
                                    vString = vStream.ReadLine();
                                    while (vString != null)
                                    {
                                        vList.Add(vString);
                                        //Read next Line
                                        vString = vStream.ReadLine();
                                    }
                                    vStream.Close();
                                }
                                if (vList.Count > 0)
                                {
                                    ProcessHotPlugInConfigList(vList);
                                }
                            }
                            catch
                            {
                                //No File do nothing
                            }
                        }
                    }
                }

                if (e.Data.GetDataPresent(DataFormats.Text, false))
                {
                    List<String> vList = new List<String>();
                    try
                    {
                        vList = (List<String>)e.Data.GetData(DataFormats.Text);
                        /*String vTheString = (String)e.Data.GetData(DataFormats.Text);
                        String vTempString = "";
                        Int32 vIndex = vTheString.IndexOf(Environment.NewLine, StringComparison.CurrentCultureIgnoreCase);
                        while (vIndex >= 0)
                        {
                            vTempString = vTheString.Remove(vIndex);
                            vList.Add(vTempString);
                            vTheString = vTheString.Substring(vIndex + Environment.NewLine.Length);
                            vIndex = vTheString.IndexOf(Environment.NewLine, StringComparison.CurrentCultureIgnoreCase);
                        }
                        if (vTheString.Length > 0)
                        {
                            vList.Add(vTheString);
                        }
                        */
                        if (vList.Count > 0)
                        {
                            ProcessHotPlugInConfigList(vList);
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Text Drop Error!");
                    }
                }
            }
        }

        private void InputGroupBox_MouseDown(object sender, EventArgs e)
        {
            List<String> vExportData = CreateExportDataWithFolders();
           
            AddSwitchesToExport(ref vExportData); //let's handle full data and all from input field

            DataObject vDataObject = new DataObject(DataFormats.Text, vExportData);

            this.DoDragDrop(vDataObject, DragDropEffects.Copy);
        }

        private void FSEarthTilesForm_MouseDown(object sender, MouseEventArgs e)
        {
            /* we dont use partial drag&drop ..confuses more than helps
            List<String> vExportData = CreateExportDataWithFolders();

            AddSwitchesToExport(ref vExportData);

            DataObject vDataObject = new DataObject(DataFormats.Text, vExportData);

            this.DoDragDrop(vDataObject, DragDropEffects.Copy);
             */
        }

        private void SwitchGroupBox_DragDrop(object sender, DragEventArgs e)
        {
            DoDrop(sender, e);
        }

        private void SwitchGroupBox_MouseDown(object sender, MouseEventArgs e)
        {
          /* we dont use partial drag&drop ..confuses more than helps
            List<String> vExportData = CreateExportHeader();

            AddSwitchesToExport(ref vExportData);

            DataObject vDataObject = new DataObject(DataFormats.Text, vExportData);

            this.DoDragDrop(vDataObject, DragDropEffects.Copy);
          */
        }

        private void SwitchGroupBox_DragEnter(object sender, DragEventArgs e)
        {
            if ((e.Data.GetDataPresent(DataFormats.FileDrop, false)) ||
                 (e.Data.GetDataPresent(DataFormats.Text, false)))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private void FSEarthTilesForm_DragDrop(object sender, DragEventArgs e)
        {
            DoDrop(sender, e);
        }

        private void FSEarthTilesForm_DragEnter(object sender, DragEventArgs e)
        {
            if ((e.Data.GetDataPresent(DataFormats.FileDrop, false)) ||
                 (e.Data.GetDataPresent(DataFormats.Text, false)))
            {
                e.Effect = DragDropEffects.All;
            }
        }

        private Boolean IsBusy()
        {
            if (!mConfigInitDone || mAreaProcessRunning || mFirstEvent || mStopProcess)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void SleepWhileFSETIsStillBusy()
        {
            while (IsBusy())
            {
                Thread.Sleep(200);
            }
        }

        // FSEarthTilesInterface Implementation
        void FSEarthTilesInterface.ProcessHotPlugInConfigList(List<String> iDirectConfigurationList)
        {
            SleepWhileFSETIsStillBusy();
            ProcessHotPlugInConfigList(iDirectConfigurationList);
        }

        Boolean FSEarthTilesInterface.IsBusy()
        {
            return IsBusy();
        }

        void FSEarthTilesInterface.Start()
        {
            SleepWhileFSETIsStillBusy();
            StartDownload();
        }


        void FSEarthTilesInterface.Abort()
        {      
            AbortDownload();
        }

        void FSEarthTilesInterface.WaitTillDoneAndReady()
        {
            while (IsBusy())
            {
                Thread.Sleep(200);
            }
        }

        void FSEarthTilesInterface.SetArea(Double iNWLatitude, Double iNWLongitude, Double iSELatitude, Double iSELongitude)
        {
            SleepWhileFSETIsStillBusy();
            if (!mMultiAreaMode)
            {
                EditRefButton_Click(null, null); //change to mulity mode
            }
            AreaDefModeBox.Text = "2Points";
            SetInputCornerCoordsTo(iNWLatitude, iNWLongitude, iSELatitude, iSELongitude);
            HandleInputChange();
            DisplayAreaInFull();

        }

        void FSEarthTilesInterface.SetReferenceArea(Double iNWLatitude, Double iNWLongitude, Double iSELatitude, Double iSELongitude)
        {
            SleepWhileFSETIsStillBusy();
            if (mMultiAreaMode)
            {
                EditRefButton_Click(null, null); //change to Reference mode
            }
            AreaDefModeBox.Text = "2Points";
            SetInputCornerCoordsTo(iNWLatitude, iNWLongitude, iSELatitude, iSELongitude);
            HandleInputChange();
            DisplayAreaInFull();
        }

        void FSEarthTilesInterface.SetFixMode()
        {
            SleepWhileFSETIsStillBusy();
            SetFixMode();
        }

        void FSEarthTilesInterface.SetAutoMode()
        {
            SleepWhileFSETIsStillBusy();
            SetAutoMode();
        }

        String FSEarthTilesInterface.GetAreaFileNameMiddlePart()
        {
            SleepWhileFSETIsStillBusy();
            String vAreaFileNameMiddlePart = GetAreaFileString();
            return vAreaFileNameMiddlePart;
        }

        String FSEarthTilesInterface.GetWorkingFolder()
        {
            SleepWhileFSETIsStillBusy();
            String vFolder = EarthConfig.mWorkFolder;
            return vFolder;
        }

        String FSEarthTilesInterface.GetSceneryFolder()
        {
            SleepWhileFSETIsStillBusy();
            String vFolder = EarthConfig.mSceneryFolder;
            return vFolder;
        }

        String FSEarthTilesInterface.GetFSEarthTilesApplicationFolder()
        {
            SleepWhileFSETIsStillBusy();
            String vFolder = EarthConfig.mStartExeFolder;
            return vFolder;
        }


        private void AutoRefSelectorBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
            }
        }

        private void WebBrowserPanicButton_Click(object sender, EventArgs e)
        {
            if (mEarthWeb != null)
            {
                mEarthWeb.SetPanicMode();
            }
        }

        private void NormalPictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!mAreaProcessRunning&&(!(Control.ModifierKeys==Keys.Control)))
            {
                Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
                Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
                Double vNewDisplayCenterLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(e.Y - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
                Double vNewDisplayCenterLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(e.X - vDisplayPixelCenterX), EarthConfig.mZoomLevel);

                vNewDisplayCenterLatitude = EarthMath.CleanLatitude(vNewDisplayCenterLatitude);
                vNewDisplayCenterLongitude = EarthMath.CleanLongitude(vNewDisplayCenterLongitude);

                DisplayThisPosition(vNewDisplayCenterLongitude, vNewDisplayCenterLatitude);
                ZoomSelectorBox.Focus();
            }
        }

        private void HalfLeftButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                Double vNWCornerLatitude = mEarthInputArea.AreaStartLatitude;
                Double vSECornerLatitude = mEarthInputArea.AreaStopLatitude;
                Double vNWCornerLongitude = mEarthInputArea.AreaStartLongitude;
                Double vSECornerLongitude = mEarthInputArea.AreaStopLongitude;

                vSECornerLongitude = vNWCornerLongitude + 0.5 * (vSECornerLongitude - vNWCornerLongitude);

                SetInputCornerCoordsTo(vNWCornerLatitude, vNWCornerLongitude, vSECornerLatitude, vSECornerLongitude);
                DisplayWhereYouJustAreAgain();
            }
        }

        private void HalfRightButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                Double vNWCornerLatitude = mEarthInputArea.AreaStartLatitude;
                Double vSECornerLatitude = mEarthInputArea.AreaStopLatitude;
                Double vNWCornerLongitude = mEarthInputArea.AreaStartLongitude;
                Double vSECornerLongitude = mEarthInputArea.AreaStopLongitude;

                vNWCornerLongitude = vNWCornerLongitude + 0.5 * (vSECornerLongitude - vNWCornerLongitude);

                SetInputCornerCoordsTo(vNWCornerLatitude, vNWCornerLongitude, vSECornerLatitude, vSECornerLongitude);
                DisplayWhereYouJustAreAgain();
            }
        }

        private void HalfUpButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                Double vNWCornerLatitude = mEarthInputArea.AreaStartLatitude;
                Double vSECornerLatitude = mEarthInputArea.AreaStopLatitude;
                Double vNWCornerLongitude = mEarthInputArea.AreaStartLongitude;
                Double vSECornerLongitude = mEarthInputArea.AreaStopLongitude;

                vSECornerLatitude = 0.5 * (vSECornerLatitude - vNWCornerLatitude) + vNWCornerLatitude;

                SetInputCornerCoordsTo(vNWCornerLatitude, vNWCornerLongitude, vSECornerLatitude, vSECornerLongitude);
                DisplayWhereYouJustAreAgain();
            }
        }

        private void HalfDownButton_Click(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning)
            {
                Double vNWCornerLatitude = mEarthInputArea.AreaStartLatitude;
                Double vSECornerLatitude = mEarthInputArea.AreaStopLatitude;
                Double vNWCornerLongitude = mEarthInputArea.AreaStartLongitude;
                Double vSECornerLongitude = mEarthInputArea.AreaStopLongitude;

                vNWCornerLatitude = 0.5 * (vSECornerLatitude - vNWCornerLatitude) + vNWCornerLatitude;

                SetInputCornerCoordsTo(vNWCornerLatitude, vNWCornerLongitude, vSECornerLatitude, vSECornerLongitude);
                DisplayWhereYouJustAreAgain();
            }
        }

        private void WriteDebugFile()
        {
            try
            {
                PrepareWorkingDirectory(false);

                List<String> vExportData = CreateExportDataWithFolders();
                AddSwitchesToExport(ref vExportData);

                StreamWriter vStream;

                String vFilename = EarthConfig.mWorkFolder + "\\" + "FSEarthTilesDEBUG.txt";

                vStream = File.AppendText(vFilename);

                if (vStream != null)
                {
                    try
                    {
                        vStream.WriteLine("");
                        vStream.WriteLine(" DEBUG Info " +  DateTime.Now.ToString());
                        vStream.WriteLine(" " + Text);
                        vStream.WriteLine("");
                        
                        Int32 vIndex = 0;
                        foreach (String vString in vExportData)
                        {
                            if (vIndex > 3)
                            {
                                vStream.WriteLine(vString);
                            }
                            vIndex++;
                        }
                        vStream.WriteLine("");
                        vStream.WriteLine("AreaFileNameMiddlePart: " +  GetAreaFileString() );
                        vStream.WriteLine("");
                        vStream.WriteLine("mEarthArea.AreaCodeXStart: " + Convert.ToString(mEarthArea.AreaCodeXStart));
                        vStream.WriteLine("mEarthArea.AreaCodeXStop:  " + Convert.ToString(mEarthArea.AreaCodeXStop));
                        vStream.WriteLine("mEarthArea.AreaCodeYStart: " + Convert.ToString(mEarthArea.AreaCodeYStart));
                        vStream.WriteLine("mEarthArea.AreaCodeYStop:  " + Convert.ToString(mEarthArea.AreaCodeYStop));
                        vStream.WriteLine("mEarthArea.AreaFSResampledLOD:  " + Convert.ToString(mEarthArea.AreaFSResampledLOD));
                        vStream.WriteLine("mEarthArea.AreaFSResampledPixelsInX:  " + Convert.ToString(mEarthArea.AreaFSResampledPixelsInX));
                        vStream.WriteLine("mEarthArea.AreaFSResampledPixelsInY:  " + Convert.ToString(mEarthArea.AreaFSResampledPixelsInY));
                        vStream.WriteLine("mEarthArea.AreaFSResampledStartLatitude:  " + Convert.ToString(mEarthArea.AreaFSResampledStartLatitude));  
                        vStream.WriteLine("mEarthArea.AreaFSResampledStartLongitude:  " + Convert.ToString(mEarthArea.AreaFSResampledStartLongitude)); 
                        vStream.WriteLine("mEarthArea.AreaFSResampledStopLatitude:  " + Convert.ToString(mEarthArea.AreaFSResampledStopLatitude)); 
                        vStream.WriteLine("mEarthArea.AreaFSResampledStopLongitude:  " + Convert.ToString(mEarthArea.AreaFSResampledStopLongitude));
                        vStream.WriteLine("mEarthArea.AreaPixelsInX:  " + Convert.ToString(mEarthArea.AreaPixelsInX));
                        vStream.WriteLine("mEarthArea.AreaPixelsInY:  " + Convert.ToString(mEarthArea.AreaPixelsInY));
                        vStream.WriteLine("mEarthArea.AreaPixelStartLatitude:  " + Convert.ToString(mEarthArea.AreaPixelStartLatitude));
                        vStream.WriteLine("mEarthArea.AreaPixelStartLongitude:  " + Convert.ToString(mEarthArea.AreaPixelStartLongitude));
                        vStream.WriteLine("mEarthArea.AreaPixelStopLatitude:  " + Convert.ToString(mEarthArea.AreaPixelStopLatitude));
                        vStream.WriteLine("mEarthArea.AreaPixelStopLongitude:  " + Convert.ToString(mEarthArea.AreaPixelStopLongitude));
                        vStream.WriteLine("mEarthArea.AreaSnapStartLatitude:  " + Convert.ToString(mEarthArea.AreaSnapStartLatitude));
                        vStream.WriteLine("mEarthArea.AreaSnapStartLongitude:  " + Convert.ToString(mEarthArea.AreaSnapStartLongitude));
                        vStream.WriteLine("mEarthArea.AreaSnapStopLatitude:  " + Convert.ToString(mEarthArea.AreaSnapStopLatitude));
                        vStream.WriteLine("mEarthArea.AreaSnapStopLongitude:  " + Convert.ToString(mEarthArea.AreaSnapStopLongitude));
                        vStream.WriteLine("mEarthArea.AreaTilesInX:  " + Convert.ToString(mEarthArea.AreaTilesInX));
                        vStream.WriteLine("mEarthArea.AreaTilesInY:  " + Convert.ToString(mEarthArea.AreaTilesInY));
                        vStream.WriteLine("mEarthArea.FetchTilesTotal:  " + Convert.ToString(mEarthArea.FetchTilesTotal));
                        vStream.WriteLine("mEarthArea.XPixelPosAreaStart:  " + Convert.ToString(mEarthArea.XPixelPosAreaStart));
                        vStream.WriteLine("mEarthArea.XPixelPosAreaStop:  " + Convert.ToString(mEarthArea.XPixelPosAreaStop));
                        vStream.WriteLine("mEarthArea.YPixelPosAreaStart:  " + Convert.ToString(mEarthArea.YPixelPosAreaStart));
                        vStream.WriteLine("mEarthArea.YPixelPosAreaStop:  " + Convert.ToString(mEarthArea.YPixelPosAreaStop));
                        vStream.WriteLine("");
                        vStream.WriteLine("mEarthMultiArea.AreaCountInX: " + Convert.ToString(mEarthMultiArea.AreaCountInX));
                        vStream.WriteLine("mEarthMultiArea.AreaCountInY: " + Convert.ToString(mEarthMultiArea.AreaCountInY));
                        vStream.WriteLine("mEarthMultiArea.FetchTilesMultiAreaTotal: " + Convert.ToString(mEarthMultiArea.FetchTilesMultiAreaTotal));                       
                        vStream.WriteLine("mEarthMultiArea.AreaCodeXStart: " + Convert.ToString(mEarthMultiArea.AreaCodeXStart));
                        vStream.WriteLine("mEarthMultiArea.AreaCodeXStop:  " + Convert.ToString(mEarthMultiArea.AreaCodeXStop));
                        vStream.WriteLine("mEarthMultiArea.AreaCodeYStart: " + Convert.ToString(mEarthMultiArea.AreaCodeYStart));
                        vStream.WriteLine("mEarthMultiArea.AreaCodeYStop:  " + Convert.ToString(mEarthMultiArea.AreaCodeYStop));
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledLOD:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledLOD));
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledPixelsInX:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledPixelsInX));
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledPixelsInY:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledPixelsInY));
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledStartLatitude:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledStartLatitude));  
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledStartLongitude:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledStartLongitude)); 
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledStopLatitude:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledStopLatitude)); 
                        vStream.WriteLine("mEarthMultiArea.AreaFSResampledStopLongitude:  " + Convert.ToString(mEarthMultiArea.AreaFSResampledStopLongitude));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelsInX:  " + Convert.ToString(mEarthMultiArea.AreaPixelsInX));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelsInY:  " + Convert.ToString(mEarthMultiArea.AreaPixelsInY));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelStartLatitude:  " + Convert.ToString(mEarthMultiArea.AreaPixelStartLatitude));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelStartLongitude:  " + Convert.ToString(mEarthMultiArea.AreaPixelStartLongitude));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelStopLatitude:  " + Convert.ToString(mEarthMultiArea.AreaPixelStopLatitude));
                        vStream.WriteLine("mEarthMultiArea.AreaPixelStopLongitude:  " + Convert.ToString(mEarthMultiArea.AreaPixelStopLongitude));
                        vStream.WriteLine("mEarthMultiArea.AreaSnapStartLatitude:  " + Convert.ToString(mEarthMultiArea.AreaSnapStartLatitude));
                        vStream.WriteLine("mEarthMultiArea.AreaSnapStartLongitude:  " + Convert.ToString(mEarthMultiArea.AreaSnapStartLongitude));
                        vStream.WriteLine("mEarthMultiArea.AreaSnapStopLatitude:  " + Convert.ToString(mEarthMultiArea.AreaSnapStopLatitude));
                        vStream.WriteLine("mEarthMultiArea.AreaSnapStopLongitude:  " + Convert.ToString(mEarthMultiArea.AreaSnapStopLongitude));
                        vStream.WriteLine("mEarthMultiArea.AreaTilesInX:  " + Convert.ToString(mEarthMultiArea.AreaTilesInX));
                        vStream.WriteLine("mEarthMultiArea.AreaTilesInY:  " + Convert.ToString(mEarthMultiArea.AreaTilesInY));
                        vStream.WriteLine("mEarthMultiArea.FetchTilesTotal:  " + Convert.ToString(mEarthMultiArea.FetchTilesTotal));
                        vStream.WriteLine("mEarthMultiArea.XPixelPosAreaStart:  " + Convert.ToString(mEarthMultiArea.XPixelPosAreaStart));
                        vStream.WriteLine("mEarthMultiArea.XPixelPosAreaStop:  " + Convert.ToString(mEarthMultiArea.XPixelPosAreaStop));
                        vStream.WriteLine("mEarthMultiArea.YPixelPosAreaStart:  " + Convert.ToString(mEarthMultiArea.YPixelPosAreaStart));
                        vStream.WriteLine("mEarthMultiArea.YPixelPosAreaStop):  " + Convert.ToString(mEarthMultiArea.YPixelPosAreaStop));
                        vStream.WriteLine("");                       
                        vStream.WriteLine("mCurrentAreaInfo.mAreaNrInX:  " + Convert.ToString(mCurrentAreaInfo.mAreaNrInX));
                        vStream.WriteLine("mCurrentAreaInfo.mAreaNrInY:  " + Convert.ToString(mCurrentAreaInfo.mAreaNrInX));
                        vStream.WriteLine("mCurrentActiveAreaNr:  " + Convert.ToString(mCurrentActiveAreaNr));
                        vStream.WriteLine("mCurrentDownloadedTilesTotal:  " + Convert.ToString(mCurrentDownloadedTilesTotal));
                        vStream.WriteLine("mMultiAreaMode:  " + Convert.ToString(mMultiAreaMode));
                        vStream.WriteLine("");

                        vIndex = 0;
                        foreach (TileInfo vTileInfo in mAreaTilesInfoDownloadCheckList)
                        {
                            vStream.WriteLine("mAreaTilesInfoDownloadCheckList[" + Convert.ToString(vIndex) + "].mAreaCodeX:  " + Convert.ToString(vTileInfo.mAreaCodeX));
                            vStream.WriteLine("mAreaTilesInfoDownloadCheckList[" + Convert.ToString(vIndex) + "].mAreaCodeY:  " + Convert.ToString(vTileInfo.mAreaCodeY));
                            vStream.WriteLine("mAreaTilesInfoDownloadCheckList[" + Convert.ToString(vIndex) + "].mLevel:      " + Convert.ToString(vTileInfo.mLevel));
                            vStream.WriteLine("mAreaTilesInfoDownloadCheckList[" + Convert.ToString(vIndex) + "].mService:    " + Convert.ToString(vTileInfo.mService));
                            vIndex++;
                        }

                        vStream.WriteLine("");

                    }
                    catch
                    {
                        SetStatus(" ERROR 2 in writeing Debug File! ");
                        Thread.Sleep(2000);
                    }
                    vStream.Flush();
                    vStream.Close();
                }
            }
            catch
            {
                SetStatus(" ERROR 1 in writeing Debug File! ");
                Thread.Sleep(2000);
            }
        }

        private void CacheSceneryBox_TextChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning) //else we may not empty queues!
            {
                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
                EmptyAllJobQueues();
            }
        }

        private void CreateScenprocBox_TextChanged(object sender, EventArgs e)
        {
           HandleInputChange();
        }



        private void tabTileSelections_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!mAreaProcessRunning) //else we may not empty queues!
            {

                switch (tabTileSelections.SelectedIndex)
                {

                    // We are switching back to create drawing mode
                    case 0:
                        mUserDrawAreaActive = false;
                        mUserSetCenterActive = false;
                        mUserDrawExcludeArea = false;
                        break;

                    // We are switching to exclude area mode
                    case 1:
                        mUserDrawAreaActive = false;
                        mUserSetCenterActive = false;
                        mUserDrawExcludeArea = false;
                        break;

                }

                mSkipAreaProcessFlag = true;
                HandleInputChange();
                DisplayWhereYouJustAreAgain();
            }

        }

        private void DrawExcludeButton_Click(object sender, EventArgs e)
        {


            
 
            if (!mAreaProcessRunning) //else we may not empty queues!
            {

                mUserDrawAreaActive = false;
                mUserSetCenterActive = false;
                mUserDrawExcludeArea = true;
                lsbExcludeAreas.SelectedIndices.Clear();

            }
        }


        private void DeleteExcludeArea()
        {

            if ((!mAreaProcessRunning) && (lsbExcludeAreas.SelectedItems.Count >0))
            {
                int vLastIndex = lsbExcludeAreas.SelectedIndex;
                vExcludeAreas.Areas.RemoveAt(lsbExcludeAreas.SelectedIndex);
                lsbExcludeAreas.Items.RemoveAt(lsbExcludeAreas.SelectedIndex);
                if (lsbExcludeAreas.Items.Count > 0 && vLastIndex>0)
                {

                    lsbExcludeAreas.SelectedIndex = vLastIndex - 1;

                }
                else if (lsbExcludeAreas.Items.Count > 0 && vLastIndex == 0)
                {
                    lsbExcludeAreas.SelectedIndex = 0;

                }

            }

        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            DeleteExcludeArea();
            DisplayThisFreePosition();
   
        }

        private void lsbExcludeAreas_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (lsbExcludeAreas.SelectedItems.Count > 0)
            {
                DisplayThisFreePosition();
            }
        }



        Boolean CheckForTileExclusion(TileInfo TestTileInfo)
        {

            Boolean SkipTile = false;
            
            for (int TileIdx = 0; TileIdx < DownloadAreaTiles.mTileInfo.Count ; TileIdx++)
            {



                if ((DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX == TestTileInfo.mAreaCodeX) && (DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY == TestTileInfo.mAreaCodeY))
                    {

                        SkipTile = DownloadAreaTiles.mTileInfo[TileIdx].mSkipTile;
                    }
            }

            return SkipTile;

        }




        /// <summary>
        /// Calculate which tiles to download
        /// </summary>
        /// <returns></returns>
        private Int64 CalculateTilesToDownload()
        {
            // Calculate which tiles to download using our selection box.
            // Any tiles that are inside an exclusion box are discarded.
            //
            // If using a KML file then the tiles are selected using our selection box once again.
            // If we have defined area polygons then only tiles inside the polygon are considered.
            // Any tiles inside an exclude polygon are discarded.

            double Lat1;
            double Lat2;
            double Lon1;
            double Lon2;
            DownloadAreaTiles = new DownloadTiles();
            Int64 TotalTiles = 0;

            // Clear and populate the object that has the info about every tile inside the bounds of the selected area
            for (Int64 row = mEarthArea.AreaCodeXStart; row <= mEarthArea.AreaCodeXStop; row++)
            {
                for (Int64 col = mEarthArea.AreaCodeYStart; col <= mEarthArea.AreaCodeYStop; col++)
                {
                    TileInfo mTileInfo = new TileInfo(row, col, EarthConfig.mFetchLevel, EarthConfig.mSelectedService, true);
                    Lat1 = EarthMath.GetAreaTileTopLatitude(col, EarthConfig.mFetchLevel);
                    Lat2 = EarthMath.GetAreaTileBottomLatitude(col, EarthConfig.mFetchLevel);
                    Lon1 = EarthMath.GetAreaTileLeftLongitude(row, EarthConfig.mFetchLevel);
                    Lon2 = EarthMath.GetAreaTileRightLongitude(row, EarthConfig.mFetchLevel);


                    // If using a KML file we then select tiles using the area polygon, so we should skip all tiles
                    // until we find one inside our polygon.
                    // If not using a kml file then all tiles are to be selected for download.
                    if (mUsingKML)
                    {
                        mTileInfo.mSkipTile = true;
                    }

                    else
                    {
                        mTileInfo.mSkipTile = false;
                    }

                    DownloadAreaTiles.mTileInfo.Add(mTileInfo);
                }
            }


            // Search for 'Areas' to keep tiles when using the KML file obviously
            for (int AreaIdx = 0; AreaIdx < mEarthKML.Areas.Count; AreaIdx++)
            {

                // This is an area to keep
                if (mEarthKML.Areas[AreaIdx].AreaName.IndexOf("Area", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    Boolean KeepTile = false;
                    {
                        for (int TileIdx = 0; TileIdx < DownloadAreaTiles.mTileInfo.Count; TileIdx++)
                        {
                            Lat1 = EarthMath.GetAreaTileTopLatitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY, EarthConfig.mFetchLevel);
                            Lat2 = EarthMath.GetAreaTileBottomLatitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY, EarthConfig.mFetchLevel);
                            Lon1 = EarthMath.GetAreaTileLeftLongitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX, EarthConfig.mFetchLevel);
                            Lon2 = EarthMath.GetAreaTileRightLongitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX, EarthConfig.mFetchLevel);
                            KeepTile = mEarthKML.Areas[AreaIdx].DoublePointinArea(Lat1, Lon1, Lat2, Lon2, false);
                            if (KeepTile) { DownloadAreaTiles.mTileInfo[TileIdx].mSkipTile = false; }
                        }
                    }
                }
                //TotalTiles = DownloadAreaTiles.TotalTiles2Download();
            }

            // Drop tiles that are inside an exclude polygon
            for (int AreaIdx = 0; AreaIdx < mEarthKML.Areas.Count; AreaIdx++)
            {

                // This is an area to exclude tiles
                if (mEarthKML.Areas[AreaIdx].AreaName.IndexOf("Exclude", StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    Boolean SkipTile = false;
                    {
                        for (int TileIdx = 0; TileIdx < DownloadAreaTiles.mTileInfo.Count; TileIdx++)
                        {
                            Lat1 = EarthMath.GetAreaTileTopLatitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY, EarthConfig.mFetchLevel);
                            Lat2 = EarthMath.GetAreaTileBottomLatitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY, EarthConfig.mFetchLevel);
                            Lon1 = EarthMath.GetAreaTileLeftLongitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX, EarthConfig.mFetchLevel);
                            Lon2 = EarthMath.GetAreaTileRightLongitude(DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX, EarthConfig.mFetchLevel);
                            SkipTile = mEarthKML.Areas[AreaIdx].DoublePointinArea(Lat1, Lon1, Lat2, Lon2, true);
                            if (SkipTile) { DownloadAreaTiles.mTileInfo[TileIdx].mSkipTile = true; }
                        }
                    }
                }
                //TotalTiles = DownloadAreaTiles.TotalTiles2Download();
            }

            
            // Drop all tiles that are inside a user drawn exclude zone
            for (int ExcludeIdx = 0; ExcludeIdx < vExcludeAreas.Areas.Count; ExcludeIdx++)
            {

                for (int TileIdx = 0; TileIdx < DownloadAreaTiles.mTileInfo.Count; TileIdx++)
                {
                    if ((DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX >= vExcludeAreas.Areas[ExcludeIdx].ExcludeAreaCodeXStart) && (DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeX <= vExcludeAreas.Areas[ExcludeIdx].ExcludeAreaCodeXStop))
                    {
                        if ((DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY >= vExcludeAreas.Areas[ExcludeIdx].ExcludeAreaCodeYStart) && (DownloadAreaTiles.mTileInfo[TileIdx].mAreaCodeY <= vExcludeAreas.Areas[ExcludeIdx].ExcludeAreaCodeYStop))
                        {

                            DownloadAreaTiles.mTileInfo[TileIdx].mSkipTile = true;
                        }
                    }
                }
            }

            // Calculate the number of tiles that are to be downloaded
            TotalTiles = DownloadAreaTiles.TotalTiles2Download();

            return TotalTiles;

        }

        private Boolean AreaAllWater()
        {
            double startLong = mEarthArea.AreaSnapStartLongitude;
            double stopLong = mEarthArea.AreaSnapStopLongitude;
            double startLat = mEarthArea.AreaSnapStartLatitude;
            double stopLat = mEarthArea.AreaSnapStopLatitude;
            int pixelsInX = (int) mEarthArea.AreaPixelsInX;
            int pixelsInY = (int) mEarthArea.AreaPixelsInY;
            if (EarthConfig.mUndistortionMode == tUndistortionMode.ePerfectHighQualityFSPreResampling)
            {
                startLong = mEarthArea.AreaFSResampledStartLongitude;
                stopLong = mEarthArea.AreaFSResampledStopLongitude;
                startLat = mEarthArea.AreaFSResampledStartLatitude;
                stopLat = mEarthArea.AreaFSResampledStopLatitude;
                pixelsInX = (int) mEarthArea.AreaFSResampledPixelsInX;
                pixelsInY = (int) mEarthArea.AreaFSResampledPixelsInY;
            }

            double NWCornerLat = startLat;
            double NWCornerLong = startLong;
            CommonFunctions.SetStartAndStopCoords(ref startLat, ref startLong, ref stopLat, ref stopLong);

            Double vPixelPerLongitude = Convert.ToDouble(pixelsInX) / (stopLong - startLong);
            Double vPixelPerLatitude = Convert.ToDouble(pixelsInY) / (stopLat - startLat);

            var tris = CommonFunctions.ReadAllMeshFiles(startLong, stopLong, startLat, stopLat, EarthConfig.mWorkFolder);
            bool allWater = false;
            using (Bitmap bmp = new Bitmap(pixelsInX, pixelsInY, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    SolidBrush b = new SolidBrush(Color.Black);
                    g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);

                    foreach (var tri in tris)
                    {
                        PointF[] convertedTri = new PointF[3];
                        for (int i = 0; i < convertedTri.Length; i++)
                        {
                            PointF toConvert = tri[i];
                            tXYCoord pixel = CommonFunctions.CoordToPixel(toConvert.Y, toConvert.X, pixelsInX, pixelsInY, NWCornerLat, NWCornerLong, vPixelPerLongitude, vPixelPerLatitude);
                            convertedTri[i] = new PointF((float)pixel.mX, (float)pixel.mY);
                        }

                        g.FillPolygon(b, convertedTri);
                    }

                    allWater = CommonFunctions.BitmapAllBlack(bmp);
                }
            }

            return allWater;
        }

        private Boolean CheckIfAreaIsEnabled()
        {
            Boolean Result = false;
            int CurrentAreaIdx = 0;

            CurrentAreaIdx = mAreaInfoAreaQueue.GetLastEntryIndex() - mAreaInfoAreaQueue.GetEntriesCount();

            for (int idx = 0; idx < AreaProcessInfo.AreaInfo.Count; idx++)
            {
                if ((AreaProcessInfo.AreaInfo[idx].AreaX == mAreaInfoAreaQueue.mAreaInfoStorage[CurrentAreaIdx].mAreaNrInX) && (AreaProcessInfo.AreaInfo[idx].AreaY == mAreaInfoAreaQueue.mAreaInfoStorage[CurrentAreaIdx].mAreaNrInY))
                {
                    Result = AreaProcessInfo.AreaInfo[idx].ProcessArea;
                }
            }
            return Result;

        }


        private void ToggleAreaDownloadStatus(Int64 iMousePosX, Int64 iMousePosY)
        {

            try
            {
                Int64 vDisplayPixelCenterX = NormalPictureBox.Size.Width >> 1;
                Int64 vDisplayPixelCenterY = NormalPictureBox.Size.Height >> 1;
                Double vAreaLatitude = EarthMath.GetLatitudeFromLatitudeAndPixel(mDisplayCenterLatitude, -Convert.ToDouble(iMousePosY - vDisplayPixelCenterY), EarthConfig.mZoomLevel);
                Double vAreaLongitude = EarthMath.GetLongitudeFromLongitudeAndPixel(mDisplayCenterLongitude, Convert.ToDouble(iMousePosX - vDisplayPixelCenterX), EarthConfig.mZoomLevel);
                vAreaLatitude = EarthMath.CleanLatitude(vAreaLatitude);
                vAreaLongitude = EarthMath.CleanLongitude(vAreaLongitude);

                if (mMultiAreaMode)
                {
                    Int64 CurrentAreaX = -1;
                    Int64 CurrentAreaY = -1;

                    Int64 vMultix = mEarthMultiArea.GetMultiAreasCountInX(mEarthSingleReferenceArea);
                    Int64 vMultiy = mEarthMultiArea.GetMultiAreasCountInY(mEarthSingleReferenceArea, EarthConfig.mAreaSnapMode);

                    Double vStepWidthLatitude = mEarthSingleReferenceArea.AreaSnapStopLatitude - mEarthSingleReferenceArea.AreaSnapStartLatitude;
                    Double vStepWidthLongitude = mEarthSingleReferenceArea.AreaSnapStopLongitude - mEarthSingleReferenceArea.AreaSnapStartLongitude;

                    Double vMultiStartLatitude = mEarthMultiArea.AreaSnapStartLatitude;
                    Double vMultiStopLatitude = mEarthMultiArea.AreaSnapStopLatitude;
                    Double vMultiStartLongitude = mEarthMultiArea.AreaSnapStartLongitude;
                    Double vMultiStopLongitude = mEarthMultiArea.AreaSnapStopLongitude;

                    //MerctorY (for Tile and Pixel Area snap mode)
                    Double vMultiStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthMultiArea.AreaSnapStartLatitude);
                    Double vReferenceStartLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthSingleReferenceArea.AreaSnapStartLatitude);
                    Double vReferenceStopLatitudeNormedMercatorY = EarthMath.GetNormedMercatorY(mEarthSingleReferenceArea.AreaSnapStopLatitude);
                    Double vStepWidthLatitudeNormedMercatorY = vReferenceStopLatitudeNormedMercatorY - vReferenceStartLatitudeNormedMercatorY;
                    Double vMultiStopLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY + Convert.ToDouble(vMultiy) * vStepWidthLatitudeNormedMercatorY;
                    Double vAreaSnapStartLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY;

                    if (vMultix * vMultiy <= cMaxDrawnAndHandledAreas)
                    {
                        //AreaInfo vAreaInfo = new AreaInfo(0, 0);
                        //EarthArea vEarthArea = new EarthArea();

                        for (Int64 vAreaY = 0; vAreaY < vMultiy; vAreaY++)
                        {
                            for (Int64 vAreaX = 0; vAreaX < vMultix; vAreaX++)
                            {
                                vMultiStartLatitude = mEarthMultiArea.AreaSnapStartLatitude + Convert.ToDouble(vAreaY) * vStepWidthLatitude;
                                vMultiStopLatitude = vMultiStartLatitude + vStepWidthLatitude;
                                vMultiStartLongitude = mEarthMultiArea.AreaSnapStartLongitude + Convert.ToDouble(vAreaX) * vStepWidthLongitude;
                                vMultiStopLongitude = vMultiStartLongitude + vStepWidthLongitude;

                                if ((EarthConfig.mAreaSnapMode == tAreaSnapMode.eTiles) || (EarthConfig.mAreaSnapMode == tAreaSnapMode.ePixel))
                                {
                                    vMultiStartLatitudeNormedMercatorY = vAreaSnapStartLatitudeNormedMercatorY + Convert.ToDouble(vAreaY) * vStepWidthLatitudeNormedMercatorY;
                                    vMultiStopLatitudeNormedMercatorY = vMultiStartLatitudeNormedMercatorY + vStepWidthLatitudeNormedMercatorY;
                                    vMultiStartLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vMultiStartLatitudeNormedMercatorY);
                                    vMultiStopLatitude = EarthMath.GetLatitudeFromNormedMercatorY(vMultiStopLatitudeNormedMercatorY);
                                }

                                if ((vAreaLatitude < vMultiStartLatitude) && (vAreaLatitude > vMultiStopLatitude) && (vAreaLongitude > vMultiStartLongitude) && (vAreaLongitude < vMultiStopLongitude))
                                {
                                    CurrentAreaX = vAreaX;
                                    CurrentAreaY = vAreaY;
                                }
                            }
                        }
                    }

                    if (CurrentAreaX >= 0 && CurrentAreaY >= 0)
                    {

                        for (int idx = 0; idx < AreaProcessInfo.AreaInfo.Count; idx++)
                        {
                            if ((AreaProcessInfo.AreaInfo[idx].AreaX == CurrentAreaX) && (AreaProcessInfo.AreaInfo[idx].AreaY == CurrentAreaY))
                            {
                                AreaProcessInfo.AreaInfo[idx].ProcessArea = !AreaProcessInfo.AreaInfo[idx].ProcessArea;
                            }

                        }
                    }

                }
                CalculateMemorySizeAndDisplayMemoryAndTileCount();
            }
            catch
            {
                // Do Nothing
            }
        }

        private void ToggleAllAreasDownloadStatus(Boolean SetAll)
        {

                for (int idx = 0; idx < AreaProcessInfo.AreaInfo.Count; idx++)
                {
                    // Select all areas to be excluded
                    if (SetAll)
                    {
                        AreaProcessInfo.AreaInfo[idx].ProcessArea = false;
                    }

                    else // Toggle the status of each area
                    {
                        AreaProcessInfo.AreaInfo[idx].ProcessArea = !AreaProcessInfo.AreaInfo[idx].ProcessArea;
                    }
                }

            CalculateMemorySizeAndDisplayMemoryAndTileCount();
        }

        // Set the focus to the map area when the mouse enters the display
        private void NormalPictureBox_MouseEnter(object sender, EventArgs e)
        {
            NormalPictureBox.Focus();
        }

        // Zoom the map using the mousewheel
        private void NormalPictureBox_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                int ChangedValue = (e.Delta / SystemInformation.MouseWheelScrollDelta)*-1; // swap sign as wheel seems to zoom wrong way
                ZoomSelectorBox.Value += ChangedValue;
            }
            catch
            {
                // Do nothing
            }

        }


    } //end FSEarthTilesForm class

}