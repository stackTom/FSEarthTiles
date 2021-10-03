using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using FSEarthTilesInternalDLL;
using System.Text.RegularExpressions;



//-------------------------------------------------------------------------------------
// 
//  public accessible FS Earth Tiles Configuration Data Package
// 
//-------------------------------------------------------------------------------------

namespace FSEarthTilesInternalDLL
{

    public enum tAreaSnapMode
    {
        eOff,
        eLOD13,
        eLatLong,
        eTiles,
        ePixel
    }

    public enum tUndistortionMode
    {
        eOff,
        eGood,
        ePerfect,
        ePerfectHighQualityFSPreResampling
    }


    public enum tConfigFileTab
    {
        eNone,
        eFSEarthTiles,
        eProxyList,
        eSevice1,
        eSevice2,
        eSevice3,
        eSevice4,
        eSevice5,
        eSevice6,
        eSevice7,
        eSevice8,
        eSevice9
    }


    public class LayProvider
    {
        public List<string> variations;
        public string name;
        public bool inGui;

        public LayProvider(string name)
        {
            this.name = name;
        }

        public string getURL(int variationIdx, long x, long y, long zoom)
        {
            string variation = null;
            if (variationIdx >= variations.Count)
            {
                variation = variations[0];
            }
            else
            {
                variation = variations[variationIdx];
            }
            variation = Regex.Replace(variation, "{x}", x.ToString());
            variation = Regex.Replace(variation, "{y}", y.ToString());
            variation = Regex.Replace(variation, "{z}", zoom.ToString());
            variation = Regex.Replace(variation, "{zoom}", zoom.ToString());

            return variation;
        }

        public int MapIdxToVariationIdx(int idx)
        {
            return idx % variations.Count;
        }

        public int GetRandomVariationIdx()
        {
            Random random = new Random();

            return random.Next(0, variations.Count);
        }
    }


    public class EarthConfig
    {
        //Error Simulation. Make sure this flags are set to FALSE in Releases!
        public static Boolean mSimulateBadConnection;
        public static Boolean mSimulateBlockConnection;

        //Exe Start Folders
        public static String mStartExeFolder;        //FSEarthTiles Application Start Folder, Exe location (Application.StartupPath)
        public static String mStartOperatingFolder;  //FSEarthTiles Application Start Folder, Exe's operation folder (current directory)
        public static EarthKML mStartEarthKML;       //if there is a KML file passed by program start argument it is loaded here.

        public ExcludeArea vExcludeAreas = new ExcludeArea();

        //Configurations
        public static List<String> mFSEarthConfigFile = new List<String>();
        public static List<String> mFSEarthTilesEntries = new List<String>();
        public static List<String> mProxyListEntries = new List<String>();
        public static List<String> mService1Entries = new List<String>();
        public static List<String> mService2Entries = new List<String>();
        public static List<String> mService3Entries = new List<String>();
        public static List<String> mService4Entries = new List<String>();
        public static List<String> mService5Entries = new List<String>();
        public static List<String> mService6Entries = new List<String>();
        public static List<String> mService7Entries = new List<String>();
        public static List<String> mService8Entries = new List<String>();
        public static List<String> mService9Entries = new List<String>();

        //Internal
        const Int32 cServices = 9;
        public static String[] mServiceUrlBegin0 = new String[cServices] { "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://" }; //fix 4 Variations for each Service
        public static String[] mServiceUrlBegin1 = new String[cServices] { "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://" };
        public static String[] mServiceUrlBegin2 = new String[cServices] { "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://" };
        public static String[] mServiceUrlBegin3 = new String[cServices] { "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://", "http://" };
        public static String[] mServiceUrlEnd = new String[cServices] { "", "", "", "", "", "", "", "", "" };
        public static String[] mServiceReferer = new String[cServices] { "", "", "", "", "", "", "", "", "" };
        public static String[] mServiceUserAgent = new String[cServices] { "", "", "", "", "", "", "", "", "" };
        public static String[] mServiceCodeing = new String[cServices] { "", "", "", "", "", "", "", "", "" };
        public static String[] mServiceName = new String[cServices] { "", "", "", "", "", "", "", "", "" };
        public static String[] mServiceVariations = new String[cServices] { "aaaa", "aaaa", "aaaa", "aaaa", "aaaa", "aaaa", "aaaa", "aaaa", "aaaa", };
        public static Int64[] mServiceVariationsCount = new Int64[cServices] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public static Boolean[] mServiceExists = new Boolean[cServices] { false, false, false, false, false, false, false, false, false };
        public static List<String> mProxyList = new List<String>();
        public static Int32 mProxyListIndex = -1;
        
        //used as configuration
        public static String mSelectedSceneryCompiler;
        public static String mFSXSceneryCompiler;
        public static String mFS2004SceneryCompiler;
        public static String mFS2004SceneryImageTool;
        public static Boolean mFS2004KeepTGAs;
        public static Boolean mKeepAreaInfFile;
        public static Boolean mKeepAreaMaskInfFile;
        public static Boolean mKeepAreaMaskSeasonInfFile;
        public static Boolean mKeepAreaEarthInfoFile;
        public static Boolean mKeepSourceBitmap;
        public static Boolean mKeepSummerBitmap;
        public static Boolean mKeepMaskBitmap;
        public static Boolean mKeepSeasonsBitmaps;

        public static Boolean mUseInformativeAreaNames;
        public static Boolean mCompileWithAreaMask;
        public static Boolean mCreateAreaMask;
        public static Boolean mCompileScenery;
        public static Boolean mSuppressPitchBlackPixels;
        public static Boolean mUseCookies;
        public static Boolean mUseCSharpScripts;
        public static Boolean mCreateScenproc;
        
        public static Boolean mUseCache;                        // Either use or dont use the cache directory
        public static Boolean mDeleteCachePrompt;
        
        public static String mPreProgram1CommandLine;
        public static String mPreProgram2CommandLine;
        public static String mPostProgram1CommandLine;
        public static String mPostProgram2CommandLine;
        public static String mCookieFolder;
        

        public static String mWebBrowserNoTileFoundKeyWords;

        //FSPreResampler shrink enlarge logic (enlarge is default)
        public static Boolean mFSPreResampleingAllowShrinkTexture;
        public static Int64   mFSPreResampleingShrinkTextureThresholdResolutionLevel;

        //used when compiling
        public static String mSceneryCompiler;
        public static String mSceneryImageTool;

        public static String mWorkFolder;
        public static String mSceneryFolder;
        public static String mSceneryFolderScenery;
        public static String mSceneryFolderTexture;


        //FSEarthMasks
        public static String mFSEarthMasks;

        public static Boolean mCreateWaterMaskBitmap;
        public static Boolean mCreateSummerBitmap;
        public static Boolean mCreateNightBitmap;
        public static Boolean mCreateSpringBitmap;
        public static Boolean mCreateAutumnBitmap;
        public static Boolean mCreateWinterBitmap;
        public static Boolean mCreateHardWinterBitmap;
        
        public static Boolean mUseScalableVectorGraphicsTool;
        public static String  mScalableVectorGraphicsTool;

        public static Boolean mUseAreaKMLFile;

        //Borders
        public static Boolean mBlendBorders;


        //AreaSnapMode
        public static tAreaSnapMode mAreaSnapMode;
        public static Int64 mAutoReferenceMode;

        //Zoom and Download FSEarthTiles-Level
        public static Int64 mZoomLevel;
        public static Int64 mFetchLevel;

        //Selected Service
        public static Int32 mSelectedService;

        //Display Mode
        public static String  mDisplayMode;             //Free or Tile (normal free)
        public static Boolean mShowDisplayModeSelector; //show selector on GUI

        //Texture Undistortion Mode
        public static tUndistortionMode mUndistortionMode;

        //Texture Color Works
        public static Double mBrightness;
        public static Double mContrast;

        //Download Fail TimeOut
        public static Int64 mDownloadFailTimeOut;  //In seconds

        //Additional Download Settings
        public static Boolean mShuffleTilesForDownload;
        public static Boolean mShuffleAreasForDownload;
        public static Double mMaxDownloadSpeed;


        //--- ApplicationStart One Time Shoot only (get's not updated durign runtime anymore)
        public static String mAreaDefModeStart;
        public static EarthInputArea mAreaInputStart;
        
        //--- Auto Start/Exit
        public static Boolean mAutoStartDownload;
        public static Boolean mAutoExitApplication;

        //-- WebBrowser
        public static Boolean mOpenWebBrowsersOnStart;
        public static Int64 mWebBrowserRecoverTimer;

        //-- Memory Usage
        public static Double mMaxMemoryUsageFactor;

        //Compile options
        public static Boolean mUseLODLimits;
        public static Int64   mMinimumDestinationLOD;
        public static Int64   mCompressionQuality;

        public static ExcludeArea mExcludeAreasConfig = new ExcludeArea();

        public static int mBlankTileColorRed;
        public static int mBlankTileColorGreen;
        public static int mBlankTileColorBlue;


        // scenproc
        public static String mScenprocLoc;
        public static String mScenprocFS9Script;
        public static String mScenprocFSXP3DScript;

        // multithreading
        public static int mMaxResampleThreads;
        public static int mMaxDownloadThreads;

        // custom Lay files
        public static Dictionary<string, LayProvider> layProviders = new Dictionary<string, LayProvider>();
        public static bool layServiceMode = false;
        public static string layServiceSelected = null;

        public static void Initialize(String iFSEarthTilesApplicationFolder) //to call as first
        {

            mSimulateBadConnection   = false;  //Make sure this is set to FALSE in Releases!
            mSimulateBlockConnection = false;  //Make sure this is set to FALSE in Releases!
        
            mStartExeFolder       = Application.StartupPath;
            mStartOperatingFolder = Directory.GetCurrentDirectory(); //Not used just info

            if (iFSEarthTilesApplicationFolder != null)
            {
                if (iFSEarthTilesApplicationFolder != "")
                {
                    mStartExeFolder = iFSEarthTilesApplicationFolder;
                }
            }

            mStartEarthKML = new EarthKML();
            


            //---- Fill in StartUp Default Values --  (Form Values)

            mWorkFolder = "D:\\FSEarthTiles\\work";
            mSceneryFolder = "D:\\FSX\\Addon Scenery\\FSEarthTiles";
           
            mSceneryFolderScenery = ""; //Handled by FSEarthTilesForm
            mSceneryFolderTexture = ""; //Handled by FSEarthTilesForm

            mFSEarthMasks = "FSEarthMasks.exe";

            mCookieFolder = "Undefined";
            mUseCookies = false;

            mZoomLevel = 3;
            mDisplayMode = "Free";
            mShowDisplayModeSelector = false;

            mFetchLevel = 2;
            mAreaSnapMode = tAreaSnapMode.eOff;
            mSelectedService = 2;
            mAutoReferenceMode = 1;

            mSelectedSceneryCompiler = "FSX/P3D";
            mCompileWithAreaMask = true;
            mCompileScenery = true;
            mUseCache = true;
            mDeleteCachePrompt = true;

            
            // ------------

            // undecided
            mCreateAreaMask = false;
            mUseCSharpScripts = true;
            mCreateScenproc = false;

            // ----------

            //internal only
            mFSXSceneryCompiler = "resampleFSX.exe";
            mFS2004SceneryCompiler = "resampleFS2004.exe";
            mFS2004SceneryImageTool = "imagetoolFS2004.exe";

            mFS2004KeepTGAs = true;

            mKeepAreaInfFile       = true;
            mKeepAreaMaskInfFile   = true;
            mKeepAreaMaskSeasonInfFile = true;
            mKeepAreaEarthInfoFile = true;
            mKeepSourceBitmap      = true;
            mKeepSummerBitmap      = true;
            mKeepMaskBitmap        = true;
            mKeepSeasonsBitmaps    = true;

            mUseInformativeAreaNames  = true;
            mSuppressPitchBlackPixels = true;

            mFSPreResampleingAllowShrinkTexture = false;
            mFSPreResampleingShrinkTextureThresholdResolutionLevel = 1;

            mSceneryCompiler = "";
            mSceneryImageTool = "";

            mPreProgram1CommandLine = "None";
            mPreProgram2CommandLine = "None";
            mPostProgram1CommandLine = "None";
            mPostProgram2CommandLine = "None";

            mWebBrowserNoTileFoundKeyWords = "";

            mUndistortionMode = tUndistortionMode.eGood;
            
            mBrightness = 0.0;
            mContrast   = 0.0;

            mDownloadFailTimeOut = 180;

            mShuffleTilesForDownload = false;
            mShuffleAreasForDownload = false;
            mMaxDownloadSpeed = 1000000000.0;   //[tiles/sec]

            mMaxResampleThreads = 4;
            mMaxDownloadThreads = 4;

            //Intialshoot one timer
            mAreaDefModeStart = "1Point";
            mAreaInputStart = new EarthInputArea();

            Double vLatitude = 44.0 + (25.0 / 60.0); // 44grad 25min 00sec north 
            Double vLongitude = 8.0 + (51.0 / 60.0); // 8grad 51min 00sec east

            mAreaInputStart.SpotLatitude = vLatitude;
            mAreaInputStart.SpotLongitude = vLongitude;
            mAreaInputStart.AreaXSize = 2.0;
            mAreaInputStart.AreaYSize = 1.0;

            mAreaInputStart.AreaStartLatitude = vLatitude;
            mAreaInputStart.AreaStopLatitude = vLatitude;
            mAreaInputStart.AreaStartLongitude = vLongitude;
            mAreaInputStart.AreaStopLongitude = vLongitude;

            //--Auto Start/Exit
            mAutoStartDownload    = false;
            mAutoExitApplication = false;

            //-- WebBrowser
            mOpenWebBrowsersOnStart = false;
            mWebBrowserRecoverTimer = 20;

            //-- Memory Usage
            mMaxMemoryUsageFactor = 1.0;

            //Compiler Options
            mUseLODLimits = false;
            mMinimumDestinationLOD = 13;
            mCompressionQuality    = 100;

            //Masks
            mCreateWaterMaskBitmap  = true;
            mCreateSummerBitmap     = false;
            mCreateNightBitmap      = false;
            mCreateSpringBitmap     = false;
            mCreateAutumnBitmap     = false;
            mCreateWinterBitmap     = false;
            mCreateHardWinterBitmap = false;

            mBlendBorders = false;

            mUseScalableVectorGraphicsTool   = false;
            mScalableVectorGraphicsTool = "D:\\Inkscape\\inkscape.exe";

            mUseAreaKMLFile = true;

            mBlankTileColorRed=0;
            mBlankTileColorGreen=0;
            mBlankTileColorBlue=0;


        }


        public static void ClearConfigLists()
        {
            //Clear Lists First
            mFSEarthConfigFile.Clear();
            mFSEarthTilesEntries.Clear();
            mProxyListEntries.Clear();
            mService1Entries.Clear();
            mService2Entries.Clear();
            mService3Entries.Clear();
            mService4Entries.Clear();
            mService5Entries.Clear();
            mService6Entries.Clear();
            mService7Entries.Clear();
            mService8Entries.Clear();
            mService9Entries.Clear();
            mExcludeAreasConfig.Areas.Clear();
        }


        public static Boolean LoadConfigFile(FSEarthTilesInternalInterface iFSEarthTilesInternalInterface)
        {
            StreamReader myStream;

            String vFileName = "FSEarthTiles.ini";
            String vRawLine;
            
            ClearConfigLists();

            try
            {
                myStream = new StreamReader(EarthConfig.mStartExeFolder + "\\" + vFileName);
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
                    iFSEarthTilesInternalInterface.SetStatus("Missing FSEarthTiles.ini Config File!");
                    return false;
                }
            }
            catch
            {
                iFSEarthTilesInternalInterface.SetStatus("Missing FSEarthTiles.ini Config File!");
                return false;
            }
        }


        public static void LoadAsArgumentPassedConfigFile(FSEarthTilesInternalInterface iFSEarthTilesInternalInterface, String iConfigFile)
        {
            StreamReader myStream;

            String vFileName = iConfigFile;
            String vRawLine;

            ClearConfigLists();

            try
            {
                myStream = new StreamReader(iConfigFile);
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
                }
                else
                {
                    iFSEarthTilesInternalInterface.SetStatus("Can not load the as argument passed Config File!");
                    Thread.Sleep(2000); //give time to read
                }
            }
            catch
            {
                iFSEarthTilesInternalInterface.SetStatus("Can not load the as argument passed Config File!");
                Thread.Sleep(2000); //give time to read
            }
        }


        public static Boolean ParseConfigurationList(List<String> iDirectConfigurationList)
        {
            if (iDirectConfigurationList != null)
            {
                //Clear Lists First
                ClearConfigLists();

                foreach (String vString in iDirectConfigurationList)
                {
                    AddConfigLine(vString);
                }

                AnalyseConfigFile();
            }

            return true;
        }


        private static void SplitConfigFile()
        {

            tConfigFileTab vConfigFileTab = tConfigFileTab.eNone;

            for (Int32 vRow = 0; vRow < EarthConfig.mFSEarthConfigFile.Count; vRow++)
            {
                String vFocus = EarthConfig.mFSEarthConfigFile[vRow];

                Int32 vIndex1 = vFocus.IndexOf("[", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex2 = vFocus.IndexOf("]", StringComparison.CurrentCultureIgnoreCase);

                if ((vIndex1 >= 0) && (vIndex2 >= 0))
                {
                    //ConfigFileTab change
                    String vCutString;
                    vCutString = vFocus.Substring(vIndex1 + 1, vIndex2 - vIndex1 - 1);
                    Int32 vNewIndex;
                    vNewIndex = vCutString.IndexOf("FSEarthTiles", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eFSEarthTiles;
                    }
                    vNewIndex = vCutString.IndexOf("ProxyList", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eProxyList;
                    }
                    vNewIndex = vCutString.IndexOf("Service1", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice1;
                        mServiceExists[0] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service2", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice2;
                        mServiceExists[1] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service3", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice3;
                        mServiceExists[2] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service4", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice4;
                        mServiceExists[3] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service5", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice5;
                        mServiceExists[4] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service6", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice6;
                        mServiceExists[5] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service7", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice7;
                        mServiceExists[6] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service8", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice8;
                        mServiceExists[7] = true;
                    }
                    vNewIndex = vCutString.IndexOf("Service9", StringComparison.CurrentCultureIgnoreCase);
                    if (vNewIndex >= 0)
                    {
                        vConfigFileTab = tConfigFileTab.eSevice9;
                        mServiceExists[8] = true;
                    }
                }
                else
                {
                    if (vConfigFileTab == tConfigFileTab.eFSEarthTiles)
                    {
                        EarthConfig.mFSEarthTilesEntries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eProxyList)
                    {
                        EarthConfig.mProxyListEntries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice1)
                    {
                        EarthConfig.mService1Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice2)
                    {
                        EarthConfig.mService2Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice3)
                    {
                        EarthConfig.mService3Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice4)
                    {
                        EarthConfig.mService4Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice5)
                    {
                        EarthConfig.mService5Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice6)
                    {
                        EarthConfig.mService6Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice7)
                    {
                        EarthConfig.mService7Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice8)
                    {
                        EarthConfig.mService8Entries.Add(vFocus);
                    }
                    if (vConfigFileTab == tConfigFileTab.eSevice9)
                    {
                        EarthConfig.mService9Entries.Add(vFocus);
                    }
                }
            }
        }

        private static String GetLeftSideOfConfigString(String iString)
        {

            Int32 vNewIndex = iString.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
            String vCutString;
            vCutString = iString.Substring(0,vNewIndex-1 );
            vCutString = vCutString.Trim();
            return vCutString;



        }

        private static String GetRightSideOfConfigString(String iString)
        {
            Int32 vNewIndex = iString.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
            String vCutString;
            vCutString = iString.Substring(vNewIndex + 1, iString.Length - (vNewIndex + 1));
            vCutString = vCutString.Trim();
            return vCutString;
        }

        private static void AnalyseStartupServiceConfigTab()
        {


            for (Int32 vRow = 0; vRow < EarthConfig.mFSEarthTilesEntries.Count; vRow++)
            {
                String vFocus = EarthConfig.mFSEarthTilesEntries[vRow];
                Int32 vIndex1 = vFocus.IndexOf("StartWithService", StringComparison.CurrentCultureIgnoreCase);
                if (vIndex1 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    SetService(vCutString);
                }
            }

        }

        private static void AnalyseFSEarthTilesConfigTab()
        {
            ExcludeAreaInfo tExAreaInfo = new ExcludeAreaInfo();
            int ExcludeCount = 0;
            int ExcludeAreaCornerCounter = 0;
            //mExcludeAreasConfig.Areas.Clear();

            for (Int32 vRow = 0; vRow < EarthConfig.mFSEarthTilesEntries.Count; vRow++)
            {




                String vFocus = EarthConfig.mFSEarthTilesEntries[vRow];
                Int32 vIndex1 = vFocus.IndexOf("WorkingFolder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex2 = vFocus.IndexOf("SceneryFolder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex3 = vFocus.IndexOf("StartWithService", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex4 = vFocus.IndexOf("SelectedSceneryCompiler", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex5 = vFocus.IndexOf("FSXSceneryCompiler", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex6 = vFocus.IndexOf("FS2004SceneryCompiler", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex7 = vFocus.IndexOf("FS2004SceneryImageTool", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex8 = vFocus.IndexOf("FS2004KeepTGAs", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex9 = vFocus.IndexOf("KeepAreaInfFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex10 = vFocus.IndexOf("TextureUndistortion", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex11 = vFocus.IndexOf("DownloadFailTimeOut", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex12 = vFocus.IndexOf("ShuffleTilesForDownload", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex13 = vFocus.IndexOf("ShuffleAreasForDownload", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex14 = vFocus.IndexOf("AreaSnap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex15 = vFocus.IndexOf("DownloadResolution", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex16 = vFocus.IndexOf("Zoom", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex17 = vFocus.IndexOf("BlendBorders", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex18 = vFocus.IndexOf("AreaDefinitionMode", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex19 = vFocus.IndexOf("AreaSizeX", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex20 = vFocus.IndexOf("AreaSizeY", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex21 = vFocus.IndexOf("CenterLatitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex22 = vFocus.IndexOf("CenterLongitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex23 = vFocus.IndexOf("NorthWestCornerLatitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex24 = vFocus.IndexOf("NorthWestCornerLongitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex25 = vFocus.IndexOf("SouthEastLatitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex26 = vFocus.IndexOf("SouthEastLongitude", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex27 = vFocus.IndexOf("UseInformativeAreaNames", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex28 = vFocus.IndexOf("CreateAreaMaskBmp", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex29 = vFocus.IndexOf("CompileScenery", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex30 = vFocus.IndexOf("CompileWithAreaMask", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex31 = vFocus.IndexOf("PreProgram1CommandLine", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex32 = vFocus.IndexOf("PreProgram2CommandLine", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex33 = vFocus.IndexOf("PostProgram1CommandLine", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex34 = vFocus.IndexOf("PostProgram2CommandLine", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex35 = vFocus.IndexOf("MaxDownloadSpeed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex36 = vFocus.IndexOf("AutoStartDownload", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex37 = vFocus.IndexOf("AutoExitApplication", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex38 = vFocus.IndexOf("Brightness", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex39 = vFocus.IndexOf("Contrast", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex40 = vFocus.IndexOf("CookieFolder", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex41 = vFocus.IndexOf("UseCookies", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex42 = vFocus.IndexOf("MinimumDestinationLOD", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex43 = vFocus.IndexOf("UseLODLimits", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex44 = vFocus.IndexOf("FSEarthMasksTool", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex45 = vFocus.IndexOf("UseScalableVectorGraphicsTool", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex46 = vFocus.IndexOf("ScalableVectorGraphicsTool", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex47 = vFocus.IndexOf("CreateWaterMaskBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex48 = vFocus.IndexOf("CreateNightBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex49 = vFocus.IndexOf("CreateSpringBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex50 = vFocus.IndexOf("CreateAutumnBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex51 = vFocus.IndexOf("CreateWinterBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex52 = vFocus.IndexOf("CreateHardWinterBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex53 = vFocus.IndexOf("UseAreaKMLFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex54 = vFocus.IndexOf("OpenWebBrowsersOnStart", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex55 = vFocus.IndexOf("CompressionQuality", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex56 = vFocus.IndexOf("FSPreResampleingAllowShrinkTexture", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex57 = vFocus.IndexOf("FSPreResampleingShrinkTextureThresholdResolutionLevel", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex58 = vFocus.IndexOf("SuppressPitchBlackPixels", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex59 = vFocus.IndexOf("KeepAreaMaskInfFile ", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex60 = vFocus.IndexOf("KeepAreaEarthInfoFile", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex61 = vFocus.IndexOf("KeepSourceBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex62 = vFocus.IndexOf("KeepSummerBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex63 = vFocus.IndexOf("KeepMaskBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex64 = vFocus.IndexOf("KeepSeasonsBitmaps", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex65 = vFocus.IndexOf("ShowDisplaySelector", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex66 = vFocus.IndexOf("UseCSharpScripts", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex67 = vFocus.IndexOf("CreateSummerBitmap", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex68 = vFocus.IndexOf("WebBrowserRecoverTimer", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex69 = vFocus.IndexOf("MaxMemoryUsageFactor", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex70 = vFocus.IndexOf("AutoReferenceMode", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex71 = vFocus.IndexOf("WebBrowserNoTileFoundKeyWords", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex72 = vFocus.IndexOf("KeepAreaMaskSeasonInfFile ", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex73 = vFocus.IndexOf("UseCache", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex74 = vFocus.IndexOf("ExcludeNWLat", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex75 = vFocus.IndexOf("ExcludeNWLon", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex76 = vFocus.IndexOf("ExcludeSELat", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex77 = vFocus.IndexOf("ExcludeSELon", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex78 = vFocus.IndexOf("BlankTileColorRed", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex79 = vFocus.IndexOf("BlankTileColorGreen", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex80 = vFocus.IndexOf("BlankTileColorBlue", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex81 = vFocus.IndexOf("CacheDeletePrompt", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex82 = vFocus.IndexOf("scenproc_loc", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex83 = vFocus.IndexOf("FS9_scenproc_script", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex84 = vFocus.IndexOf("FSX_P3D_scenproc_script", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex85 = vFocus.IndexOf("CreateScenproc", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex86 = vFocus.IndexOf("MaxResampleThreads", StringComparison.CurrentCultureIgnoreCase);
                Int32 vIndex87 = vFocus.IndexOf("MaxDownloadThreads", StringComparison.CurrentCultureIgnoreCase);

                if (vIndex1 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mWorkFolder = vCutString;
                }
                if (vIndex2 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mSceneryFolder = vCutString;
                }
                if (vIndex3 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    SetService(vCutString);
                }
                if (vIndex4 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mSelectedSceneryCompiler = vCutString;
                }
                if (vIndex5 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFSXSceneryCompiler = vCutString;
                }
                if (vIndex6 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFS2004SceneryCompiler = vCutString;
                }
                if (vIndex7 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFS2004SceneryImageTool = vCutString;
                }
                if (vIndex8 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFS2004KeepTGAs = GetBooleanFromString(vCutString);
                }
                if (vIndex9 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepAreaInfFile = GetBooleanFromString(vCutString);
                }
                if (vIndex10 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    SetUndistortion(vCutString);
                }
                if (vIndex11 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vTimeOutTime = Convert.ToInt64(vCutString);
                        EarthConfig.mDownloadFailTimeOut = vTimeOutTime;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex12 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mShuffleTilesForDownload = GetBooleanFromString(vCutString);
                }
                if (vIndex13 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mShuffleAreasForDownload = GetBooleanFromString(vCutString);
                }
                if (vIndex14 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    SetAreaSnap(vCutString);
                }
                if (vIndex15 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vDownloadResolution = Convert.ToInt64(vCutString);
                        mFetchLevel = vDownloadResolution;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex16 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vZoom = Convert.ToInt64(vCutString);
                        mZoomLevel = vZoom;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex17 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mBlendBorders = GetBooleanFromString(vCutString);
                }
                if (vIndex18 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mAreaDefModeStart = vCutString;
                }
                if (vIndex19 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        //For and Back converting makes sure we only store a valid number (input numbers format countryspecific)
                        Double vAreaSizeX = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                        mAreaInputStart.AreaXSize = vAreaSizeX;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex20 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        //For and Back converting makes sure we only store a valid number (input numbers format countryspecific)
                        Double vAreaSizeY = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                        mAreaInputStart.AreaYSize = vAreaSizeY;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex21 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.SpotLatitude = GetIniFileLatitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex22 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.SpotLongitude = GetIniFileLongitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex23 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.AreaStartLatitude = GetIniFileLatitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex24 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.AreaStartLongitude = GetIniFileLongitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex25 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.AreaStopLatitude = GetIniFileLatitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex26 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        mAreaInputStart.AreaStopLongitude = GetIniFileLongitude(vCutString);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex27 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mUseInformativeAreaNames = GetBooleanFromString(vCutString);
                }
                if (vIndex28 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateAreaMask = GetBooleanFromString(vCutString);
                }
                if (vIndex29 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCompileScenery = GetBooleanFromString(vCutString);
                }
                if (vIndex30 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCompileWithAreaMask = GetBooleanFromString(vCutString);
                }
                if (vIndex31 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mPreProgram1CommandLine = vCutString;
                }
                if (vIndex32 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mPreProgram2CommandLine = vCutString;
                }
                if (vIndex33 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mPostProgram1CommandLine = vCutString;
                }
                if (vIndex34 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mPostProgram2CommandLine = vCutString;
                }
                if (vIndex35 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        EarthConfig.mMaxDownloadSpeed = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex36 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mAutoStartDownload = GetBooleanFromString(vCutString);
                }
                if (vIndex37 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mAutoExitApplication = GetBooleanFromString(vCutString);
                }
                if (vIndex38 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Double vBrightness = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                        EarthConfig.mBrightness = vBrightness;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex39 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Double vContrast = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                        EarthConfig.mContrast = vContrast;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex40 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mCookieFolder = vCutString;
                }
                if (vIndex41 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseCookies = GetBooleanFromString(vCutString);
                }
                if (vIndex42 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vMinimumDestinationLOD = Convert.ToInt64(vCutString);
                        EarthConfig.mMinimumDestinationLOD = vMinimumDestinationLOD;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex43 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseLODLimits = GetBooleanFromString(vCutString);
                }
                if (vIndex44 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFSEarthMasks = vCutString;
                }
                if (vIndex45 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseScalableVectorGraphicsTool = GetBooleanFromString(vCutString);
                }
                if (vIndex46 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mScalableVectorGraphicsTool = vCutString;
                }
                if (vIndex47 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateWaterMaskBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex48 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateNightBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex49 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateSpringBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex50 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateAutumnBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex51 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateWinterBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex52 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateHardWinterBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex53 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseAreaKMLFile = GetBooleanFromString(vCutString);
                }
                if (vIndex54 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mOpenWebBrowsersOnStart = GetBooleanFromString(vCutString);
                }
                if (vIndex55 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vCompressionQuality = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                        EarthConfig.mCompressionQuality = vCompressionQuality;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex56 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mFSPreResampleingAllowShrinkTexture = GetBooleanFromString(vCutString);
                }
                if (vIndex57 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vFSPreResampleingShrinkTextureThresholdResolutionLevel = Convert.ToInt64(vCutString);
                        EarthConfig.mFSPreResampleingShrinkTextureThresholdResolutionLevel = vFSPreResampleingShrinkTextureThresholdResolutionLevel;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex58 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mSuppressPitchBlackPixels = GetBooleanFromString(vCutString);
                }

                if (vIndex59 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepAreaMaskInfFile = GetBooleanFromString(vCutString);
                }
                if (vIndex60 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepAreaEarthInfoFile = GetBooleanFromString(vCutString);
                }
                if (vIndex61 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepSourceBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex62 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepSummerBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex63 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepMaskBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex64 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepSeasonsBitmaps = GetBooleanFromString(vCutString);
                }
                if (vIndex65 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mShowDisplayModeSelector = GetBooleanFromString(vCutString);
                }
                if (vIndex66 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseCSharpScripts = GetBooleanFromString(vCutString);
                }
                if (vIndex67 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mCreateSummerBitmap = GetBooleanFromString(vCutString);
                }
                if (vIndex68 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vWebBrowserRecoverTimer = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                        EarthConfig.mWebBrowserRecoverTimer = vWebBrowserRecoverTimer;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex69 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Double vMaxMemoryUsageFactor = Convert.ToDouble(vCutString, NumberFormatInfo.InvariantInfo);
                        if ((vMaxMemoryUsageFactor > 0.0) && (vMaxMemoryUsageFactor <= 1.0))
                        {
                            EarthConfig.mMaxMemoryUsageFactor = vMaxMemoryUsageFactor;
                        }
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex70 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        Int64 vAutoReferenceMode = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                        EarthConfig.mAutoReferenceMode = vAutoReferenceMode;
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
                if (vIndex71 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mWebBrowserNoTileFoundKeyWords = vCutString;
                }
                if (vIndex72 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mKeepAreaMaskSeasonInfFile = GetBooleanFromString(vCutString);
                }

                if (vIndex73 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mUseCache = GetBooleanFromString(vCutString);
                }

                // Next 4 areas test for each of the corners that define an exclude zone

                if (vIndex74 >= 0)
                {
                    try
                    {
                        if (ExcludeAreaCornerCounter == 0)
                        {

                            tExAreaInfo = new ExcludeAreaInfo();
                            String vCutString = GetLeftSideOfConfigString(vFocus);
                            String vDesc = "ExcludeNWLat";
                            vCutString = vCutString.Substring(vDesc.Length, vCutString.Length - vDesc.Length);
                            ExcludeCount = Convert.ToInt32 (vCutString, NumberFormatInfo.InvariantInfo);
                            vCutString = GetRightSideOfConfigString(vFocus);
                            //GetIniFileLongitude(vCutString);
                            GetIniFileLatitude(vCutString);
                            tExAreaInfo.ExcludeAreaStartLatitude = GetIniFileLatitude(vCutString);
                            ExcludeAreaCornerCounter++;
                        }
                        else
                        {
                            ExcludeAreaCornerCounter = 0;
                        }
                    }


                    catch
                    {
                        ExcludeAreaCornerCounter = 0;

                    }
                }

                if (vIndex75 >= 0)
                {
                    try
                    {

                        if (ExcludeAreaCornerCounter == 1)
                        {
                            String vCutString = GetLeftSideOfConfigString(vFocus);
                            String vDesc = "ExcludeNWLon";
                            vCutString = vCutString.Substring(vDesc.Length, vCutString.Length - vDesc.Length);
                            Int64 NewExcludeCount = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                            if (NewExcludeCount == ExcludeCount)
                            {
                                vCutString = GetRightSideOfConfigString(vFocus);
                                GetIniFileLongitude(vCutString);
                                tExAreaInfo.ExcludeAreaStartLongitude = GetIniFileLongitude(vCutString);
                                ExcludeAreaCornerCounter++;
                            }
                            else
                            {
                                ExcludeAreaCornerCounter = 0;
                            }
                        }
                        else
                        {
                            ExcludeAreaCornerCounter = 0;
                        }
                    }
                    catch
                    {
                        ExcludeAreaCornerCounter = 0;
                    }

                }

                if (vIndex76 >= 0)
                {
                    try
                    {
                        if (ExcludeAreaCornerCounter == 2)
                        {

                            String vCutString = GetLeftSideOfConfigString(vFocus);
                            String vDesc = "ExcludeSELat";
                            vCutString = vCutString.Substring(vDesc.Length, vCutString.Length - vDesc.Length);
                            Int64 NewExcludeCount = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                            if (NewExcludeCount == ExcludeCount)
                            {
                                vCutString = GetRightSideOfConfigString(vFocus);
                                //GetIniFileLongitude(vCutString);
                                GetIniFileLatitude(vCutString);
                                tExAreaInfo.ExcludeAreaStopLatitude = GetIniFileLatitude(vCutString);
                                ExcludeAreaCornerCounter++;
                            }
                            else
                            {
                                ExcludeAreaCornerCounter = 0;
                            }
                        }
                        else
                        {
                            ExcludeAreaCornerCounter = 0;
                        }
                    }
                    catch
                    {
                        ExcludeAreaCornerCounter = 0;
                    }
                }

                if (vIndex77 >= 0)
                {
                    try
                    {
                        if (ExcludeAreaCornerCounter == 3)
                        {


                            String vCutString = GetLeftSideOfConfigString(vFocus);
                            String vDesc = "ExcludeSELon";
                            vCutString = vCutString.Substring(vDesc.Length, vCutString.Length - vDesc.Length);
                            Int64 NewExcludeCount = Convert.ToInt64(vCutString, NumberFormatInfo.InvariantInfo);
                            if (NewExcludeCount == ExcludeCount)
                            {
                                vCutString = GetRightSideOfConfigString(vFocus);
                                GetIniFileLongitude(vCutString);
                                tExAreaInfo.ExcludeAreaStopLongitude = GetIniFileLongitude(vCutString);
                                mExcludeAreasConfig.Areas.Add(tExAreaInfo);
                                ExcludeAreaCornerCounter = 0;
                            }
                            else
                            {
                                ExcludeAreaCornerCounter = 0;
                            }
                        }

                        else
                        {
                            ExcludeAreaCornerCounter = 0;

                        }

                    }

                    catch
                    {
                        ExcludeAreaCornerCounter = 0;
                        if (mExcludeAreasConfig.Areas.Count == ExcludeCount+1)
                        {

                            mExcludeAreasConfig.Areas.RemoveAt(ExcludeCount);
                        }
                    }
                }

                if (vIndex78 >= 0)
                {
                    try
                    {
                        String vCutString = GetRightSideOfConfigString(vFocus);
                        int ColorValue = Convert.ToInt16 (vCutString, NumberFormatInfo.InvariantInfo);
                        if (ColorValue > 255 || ColorValue < 0) { ColorValue = 255; }
                        mBlankTileColorRed = ColorValue;
                    }


                    catch
                    {
                        // Do Nothing
                    }

                }

                if (vIndex79 >= 0)
                {
                    try
                    {
                        String vCutString = GetRightSideOfConfigString(vFocus);
                        int ColorValue = Convert.ToInt16(vCutString, NumberFormatInfo.InvariantInfo);
                        if (ColorValue > 255 || ColorValue < 0) { ColorValue = 255; }
                        mBlankTileColorGreen = ColorValue;
                    }


                    catch
                    {
                        // Do Nothing
                    }

                }

                if (vIndex80 >= 0)
                {
                    try
                    {
                        String vCutString = GetRightSideOfConfigString(vFocus);
                        int ColorValue = Convert.ToInt16(vCutString, NumberFormatInfo.InvariantInfo);
                        if (ColorValue > 255 || ColorValue < 0) { ColorValue = 255; }
                        mBlankTileColorBlue = ColorValue;
                    }


                    catch
                    {
                        // Do Nothing
                    }
                }

                if (vIndex81 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mDeleteCachePrompt = GetBooleanFromString(vCutString);
                }

                if (vIndex82 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mScenprocLoc = vCutString;
                }

                if (vIndex83 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mScenprocFS9Script = vCutString;
                }

                if (vIndex84 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    EarthConfig.mScenprocFSXP3DScript = vCutString;
                }

                if (vIndex85 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    mCreateScenproc = GetBooleanFromString(vCutString);
                }

                if (vIndex86 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        EarthConfig.mMaxResampleThreads = Convert.ToInt32(vCutString, NumberFormatInfo.InvariantInfo);
                        if (EarthConfig.mMaxResampleThreads < 1)
                        {
                            EarthConfig.mMaxResampleThreads = 1;
                        }
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }

                if (vIndex87 >= 0)
                {
                    String vCutString = GetRightSideOfConfigString(vFocus);
                    try
                    {
                        EarthConfig.mMaxDownloadThreads = Convert.ToInt32(vCutString, NumberFormatInfo.InvariantInfo);
                        if (EarthConfig.mMaxDownloadThreads < 4)
                        {
                            EarthConfig.mMaxDownloadThreads = 4;
                        }
                    }
                    catch
                    {
                        //ignore if failed
                    }
                }
            }
        }

        private static void AnalyseProxyListConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mProxyListEntries.Count; vRow++)
            {
                String vFocus = EarthConfig.mProxyListEntries[vRow];
                vFocus = vFocus.Trim();
                if (!EarthCommon.StringCompare(vFocus,""))
                {
                  EarthConfig.mProxyList.Add(vFocus);
                }
            }
        }


        private static void PopulateLayProviders()
        {
            string providersFolder = EarthConfig.mStartExeFolder + @"\Providers";
            string[] providerFolders = Directory.GetDirectories(providersFolder);
            foreach (string providerFolder in providerFolders)
            {
                string[] layFiles = Directory.GetFiles(providerFolder);

                foreach (string layFile in layFiles)
                {
                    if (Path.GetExtension(layFile) != ".swp") // Oscar used vim ;)
                    {
                        ParseLayFile(layFile);
                    }
                }
            }
        }


        private static void CreateServiceVariations(Int32 iServiceIndex)
        {
            //Make Sure we have 4 identical Variation to edit, no old stuff allowed!

            EarthConfig.mServiceVariationsCount[iServiceIndex] = 1;
            EarthConfig.mServiceUrlBegin1[iServiceIndex] = EarthConfig.mServiceUrlBegin0[iServiceIndex];
            EarthConfig.mServiceUrlBegin2[iServiceIndex] = EarthConfig.mServiceUrlBegin0[iServiceIndex];
            EarthConfig.mServiceUrlBegin3[iServiceIndex] = EarthConfig.mServiceUrlBegin0[iServiceIndex];

            String vWorkString = EarthConfig.mServiceVariations[iServiceIndex];
            String vPartString = "";
            Boolean vMatchFound = false;
            Int32 vIndex = 0;

            do
            {
                vPartString = vWorkString;
                vIndex = vWorkString.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                if (vIndex >= 0)
                {
                    vPartString = vWorkString.Remove(vIndex);
                    vPartString = vPartString.Trim();
                    vWorkString = vWorkString.Substring(vIndex + 1, vWorkString.Length - (vIndex + 1));
                    vWorkString = vWorkString.Trim();
                }
                else
                {
                    vWorkString = "";
                }
                if (vPartString.Length >= 0)
                {
                    vIndex = EarthConfig.mServiceUrlBegin0[iServiceIndex].IndexOf(vPartString, StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex >= 0)
                    {
                        vMatchFound = true;
                    }
                }
            } while ((!vMatchFound) && (vWorkString.Length > 0));

            if (vMatchFound)
            {
                //Same Game again this time Replace the ServerVariations
                Int32 vVariationIndex = vIndex;
                Int32 vVariationLength = vPartString.Length;
                Int32 vVariationCounter = 0;

                vWorkString = EarthConfig.mServiceVariations[iServiceIndex];
                do
                {
                    vPartString = vWorkString;
                    vIndex = vWorkString.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                    if (vIndex >= 0)
                    {
                        vPartString = vWorkString.Remove(vIndex);
                        vPartString = vPartString.Trim();
                        vWorkString = vWorkString.Substring(vIndex + 1, vWorkString.Length - (vIndex + 1));
                        vWorkString = vWorkString.Trim();
                    }
                    else
                    {
                        vWorkString = "";
                    }
                    if (vPartString.Length == vVariationLength)
                    {
                        switch (vVariationCounter)
                        {
                            case 0:
                                EarthConfig.mServiceUrlBegin0[iServiceIndex] = EarthConfig.mServiceUrlBegin0[iServiceIndex].Remove(vVariationIndex, vVariationLength);
                                EarthConfig.mServiceUrlBegin0[iServiceIndex] = EarthConfig.mServiceUrlBegin0[iServiceIndex].Insert(vVariationIndex, vPartString);
                                break;
                            case 1:
                                EarthConfig.mServiceUrlBegin1[iServiceIndex] = EarthConfig.mServiceUrlBegin1[iServiceIndex].Remove(vVariationIndex, vVariationLength);
                                EarthConfig.mServiceUrlBegin1[iServiceIndex] = EarthConfig.mServiceUrlBegin1[iServiceIndex].Insert(vVariationIndex, vPartString);
                                break;
                            case 2:
                                EarthConfig.mServiceUrlBegin2[iServiceIndex] = EarthConfig.mServiceUrlBegin2[iServiceIndex].Remove(vVariationIndex, vVariationLength);
                                EarthConfig.mServiceUrlBegin2[iServiceIndex] = EarthConfig.mServiceUrlBegin2[iServiceIndex].Insert(vVariationIndex, vPartString);
                                break;
                            case 3:
                                EarthConfig.mServiceUrlBegin3[iServiceIndex] = EarthConfig.mServiceUrlBegin3[iServiceIndex].Remove(vVariationIndex, vVariationLength);
                                EarthConfig.mServiceUrlBegin3[iServiceIndex] = EarthConfig.mServiceUrlBegin3[iServiceIndex].Insert(vVariationIndex, vPartString);
                                break;
                            default:
                                break;

                        }
                        vVariationCounter++;
                    }
                } while (vWorkString.Length > 0);
                if (vVariationCounter > 4)
                {
                    vVariationCounter = 4; //4 Variation maximum
                }
                if (vVariationCounter > 1)
                {
                    EarthConfig.mServiceVariationsCount[iServiceIndex] = vVariationCounter;
                }
            }
            else
            {
                //No Valid Variations found
                EarthConfig.mServiceVariationsCount[iServiceIndex] = 1;
            }
        }

        private static void AnalyseServiceConfig(String iFocus, Int32 ServiceIndex)
        {
            Int32 vIndex1 = iFocus.IndexOf("ServiceCodeing", StringComparison.CurrentCultureIgnoreCase);
            Int32 vIndex2 = iFocus.IndexOf("ServiceUrl", StringComparison.CurrentCultureIgnoreCase);
            Int32 vIndex3 = iFocus.IndexOf("Referer", StringComparison.CurrentCultureIgnoreCase);
            Int32 vIndex4 = iFocus.IndexOf("Useragent", StringComparison.CurrentCultureIgnoreCase);
            Int32 vIndex5 = iFocus.IndexOf("ServerVariations", StringComparison.CurrentCultureIgnoreCase);
            Int32 vIndex6 = iFocus.IndexOf("ServiceName", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex1 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                EarthConfig.mServiceCodeing[ServiceIndex] = vCutString;
            }
            if (vIndex2 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                Int32 vNewIndex2 = vCutString.IndexOf("%s", StringComparison.CurrentCultureIgnoreCase);
                if (vNewIndex2 >= 0)
                {
                    EarthConfig.mServiceUrlBegin0[ServiceIndex] = vCutString.Remove(vNewIndex2);
                    EarthConfig.mServiceUrlBegin1[ServiceIndex] = EarthConfig.mServiceUrlBegin0[ServiceIndex];
                    EarthConfig.mServiceUrlBegin2[ServiceIndex] = EarthConfig.mServiceUrlBegin0[ServiceIndex];
                    EarthConfig.mServiceUrlBegin3[ServiceIndex] = EarthConfig.mServiceUrlBegin0[ServiceIndex];
                    EarthConfig.mServiceUrlEnd[ServiceIndex] = vCutString.Substring(vNewIndex2 + 2, vCutString.Length - (vNewIndex2 + 2));
                    if (EarthConfig.mServiceVariationsCount[ServiceIndex] > 1)
                    {
                        CreateServiceVariations(ServiceIndex);
                    }
                    else
                    {
                        EarthConfig.mServiceVariationsCount[ServiceIndex] = 1;
                    }
                }

            }
            if (vIndex3 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                EarthConfig.mServiceReferer[ServiceIndex] = vCutString;
            }
            if (vIndex4 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                EarthConfig.mServiceUserAgent[ServiceIndex] = vCutString;
            }
            if (vIndex5 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                EarthConfig.mServiceVariations[ServiceIndex] = vCutString;
                if (EarthConfig.mServiceVariationsCount[ServiceIndex] > 0)
                {
                    CreateServiceVariations(ServiceIndex);
                }
                else
                {
                    EarthConfig.mServiceVariationsCount[ServiceIndex] = 4; //Pre-Assumption becomes corrected when CreateServiceVariations is called
                }
            }

            if (vIndex6 >= 0)
            {
                Int32 vNewIndex = iFocus.IndexOf("=", StringComparison.CurrentCultureIgnoreCase);
                String vCutString;
                vCutString = iFocus.Substring(vNewIndex + 1, iFocus.Length - (vNewIndex + 1));
                vCutString = vCutString.Trim();
                EarthConfig.mServiceName[ServiceIndex] = vCutString;
            }
        }


        private static void AnalyseService1ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService1Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService1Entries[vRow], 0);
            }
        }

        private static void AnalyseService2ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService2Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService2Entries[vRow], 1);
            }
        }

        private static void AnalyseService3ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService3Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService3Entries[vRow], 2);
            }
        }

        private static void AnalyseService4ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService4Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService4Entries[vRow], 3);
            }
        }

        private static void AnalyseService5ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService5Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService5Entries[vRow], 4);
            }
        }

        private static void AnalyseService6ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService6Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService6Entries[vRow], 5);
            }
        }

        private static void AnalyseService7ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService7Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService7Entries[vRow], 6);
            }
        }

        private static void AnalyseService8ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService8Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService8Entries[vRow], 7);
            }
        }

        private static void AnalyseService9ConfigTab()
        {
            for (Int32 vRow = 0; vRow < EarthConfig.mService9Entries.Count; vRow++)
            {
                AnalyseServiceConfig(EarthConfig.mService9Entries[vRow], 8);
            }
        }

        private static void AnalyseConfigFile()
        {
            SplitConfigFile();
            AnalyseFSEarthTilesConfigTab();
            AnalyseProxyListConfigTab();
            AnalyseService1ConfigTab();
            AnalyseService2ConfigTab();
            AnalyseService3ConfigTab();
            AnalyseService4ConfigTab();
            AnalyseService5ConfigTab();
            AnalyseService6ConfigTab();
            AnalyseService7ConfigTab();
            AnalyseService8ConfigTab();
            AnalyseService9ConfigTab();
            AnalyseStartupServiceConfigTab();
            //AnalyseFSEarthTilesConfigTab();
            PopulateLayProviders();
        }

        private static void AddConfigLine(String iConfigLine)
        {
            //Remove Comment
            Int32 iIndexOfComment = iConfigLine.IndexOf("#", StringComparison.CurrentCultureIgnoreCase);
            if (iIndexOfComment >= 0)
            {
                String vCutString;
                vCutString = iConfigLine.Remove(iIndexOfComment);
                vCutString = vCutString.Trim();
                EarthConfig.mFSEarthConfigFile.Add(vCutString);
            }
            else
            {
                String vCutString;
                vCutString = iConfigLine.Trim();
                EarthConfig.mFSEarthConfigFile.Add(vCutString);
            }

        }

        public static String GetAreaSnapString()
        {
            //Friend Thread can not read the AreaSnapBox so we need to use the config value
            String vAreaSnap = "Off";

            switch (EarthConfig.mAreaSnapMode)
            {
                case tAreaSnapMode.eTiles: vAreaSnap = "Tiles"; break;
                case tAreaSnapMode.ePixel: vAreaSnap = "Pixel"; break;
                case tAreaSnapMode.eLOD13: vAreaSnap = "LOD13"; break;
                case tAreaSnapMode.eLatLong: vAreaSnap = "LatLong"; break;
                default: break;
            }

            return vAreaSnap;
        }


        public static void SetAreaSnap(String iAreaSnap)
        {
            if (EarthCommon.StringCompare(iAreaSnap, "Off"))
            {
                EarthConfig.mAreaSnapMode = tAreaSnapMode.eOff;
            }
            if (EarthCommon.StringCompare(iAreaSnap, "LOD13"))
            {
                EarthConfig.mAreaSnapMode = tAreaSnapMode.eLOD13;
            }
            if (EarthCommon.StringCompare(iAreaSnap, "LatLong"))
            {
                EarthConfig.mAreaSnapMode = tAreaSnapMode.eLatLong;
            }
            if (EarthCommon.StringCompare(iAreaSnap, "Tiles"))
            {
                EarthConfig.mAreaSnapMode = tAreaSnapMode.eTiles;
            }
            if (EarthCommon.StringCompare(iAreaSnap, "Pixel"))
            {
                EarthConfig.mAreaSnapMode = tAreaSnapMode.ePixel;
            }
        }


        public static void SetService(String iService)
        {
            if (layProviders.ContainsKey(iService))
            {
                EarthConfig.layServiceMode = true;
                EarthConfig.layServiceSelected = iService;
                return;
            }
            EarthConfig.layServiceMode = false;
            EarthConfig.layServiceSelected = null;

            String vCompareString;
            for (Int32 vCon = 1; vCon <= 9; vCon++)
            {
                //vCompareString = "Service" + Convert.ToString(vCon);
                vCompareString = EarthConfig.mServiceName[vCon - 1];
                if (EarthCommon.StringCompare(iService, vCompareString))
                {
                    EarthConfig.mSelectedService = vCon;
                }
            }


        }

        public static String GetServiceString()
        {
            String vService = "";

            if (mServiceName[mSelectedService - 1].Length > 0)
            {
                vService = mServiceName[mSelectedService - 1];
            }
            else
            {
                vService="Service" + Convert.ToString(mSelectedService);
            }
            return vService;
        }

        private static Boolean GetBooleanFromString(String iString)
        {
            Boolean vValue;
            if (EarthCommon.StringCompare(iString, "Yes"))
            {
                vValue = true;
            }
            else
            {
                vValue = false;
            }
            return vValue;
        }


        private static String GetBooleanString(Boolean iValue)
        {
            String vString;
            if (iValue)
            {
                vString = "Yes";
            }
            else
            {
                vString = "No";
            }
            return vString;
        }

        public static void SetCompileWithMask(String iString)
        {
            mCompileWithAreaMask = GetBooleanFromString(iString);
        }

        public static string GetCompileWithMask()
        {
            String vStringValue = GetBooleanString(mCompileWithAreaMask);
            return vStringValue;
        }

        public static void SetCreateMask(String iString)
        {
            mCreateAreaMask = GetBooleanFromString(iString);
        }

        public static string GetCreateMask()
        {
            String vStringValue = GetBooleanString(mCreateAreaMask);
            return vStringValue;
        }

        public static void SetCompileScenery(String iString)
        {
            mCompileScenery = GetBooleanFromString(iString);
        }

        public static string GetCompileScenery()
        {
            String vStringValue = GetBooleanString(mCompileScenery);
            return vStringValue;
        }

        public static void SetUndistortion(String iString)
        {
            if (EarthCommon.StringCompare(iString, "Off"))
            {
                EarthConfig.mUndistortionMode = tUndistortionMode.eOff;
            }
            if (EarthCommon.StringCompare(iString, "Good"))
            {
                EarthConfig.mUndistortionMode = tUndistortionMode.eGood;
            }
            if (EarthCommon.StringCompare(iString, "Perfect"))
            {
                EarthConfig.mUndistortionMode = tUndistortionMode.ePerfect;
            }
            if (EarthCommon.StringCompare(iString, "PerfectHighQualityFSPreResampling"))
            {
                EarthConfig.mUndistortionMode = tUndistortionMode.ePerfectHighQualityFSPreResampling;
            }
        }

       public static void SetAutoReferenceMode(String iAutoRefMode)
       {
           try
           {
               Int64 vValue = Convert.ToInt64(iAutoRefMode);
               if ((vValue>=1) && (vValue<=4))
               {
                  mAutoReferenceMode = vValue;
               }
           }
           catch
           {
             // do nothing
           }
       }

        public static String GetAutoReferenceModeString()
        {
            String vString = "1";
            try
            {
                vString = Convert.ToString(mAutoReferenceMode);
            }
            catch
            {
                // do nothing
            }
            return vString;
        }

        public static void SetCreateScenproc(String iString)
        {
            mCreateScenproc = GetBooleanFromString(iString);
        }

        public static string GetCreateScenproc()
        {
            String vStringValue = GetBooleanString(mCreateScenproc);
            return vStringValue;
        }

        public static Double GetIniFileLatitude(String iString)
        {
            Double vLatitude = 0.0;
            String vString = iString;
            String vPartString = "";
            Int32 vIndex = -1;

            //accept grad instead degree also
            vIndex = vString.IndexOf("grad", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLatitude += Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 4, vString.Length - (vIndex + 4));
                vString = vString.Trim();
            }

            //deg
            vIndex = vString.IndexOf("deg", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLatitude += Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }

            //minutes
            vIndex = vString.IndexOf("min", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLatitude += (1.0 / 60) * Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }

            //seconds
            vIndex = vString.IndexOf("sec", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLatitude += (1.0 / 3600.0) * Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }

            //Sign (default North)
            if (EarthCommon.StringCompare(vString, "south"))
            {
                vLatitude = -vLatitude;
            }

            vLatitude = EarthMath.CleanLatitude(vLatitude);
            return vLatitude;
        }

        public static Double GetIniFileLongitude(String iString)
        {
            Double vLongitude = 0.0;
            String vString = iString;
            String vPartString = "";
            Int32 vIndex = -1;

            //accept grad instead degree also
            vIndex = vString.IndexOf("grad", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLongitude += Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 4, vString.Length - (vIndex + 4));
                vString = vString.Trim();
            }

            //deg
            vIndex = vString.IndexOf("deg", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLongitude += Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }
            //minutes
            vIndex = vString.IndexOf("min", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLongitude += (1.0 / 60) * Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }

            //seconds
            vIndex = vString.IndexOf("sec", StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                vPartString = vString.Remove(vIndex);
                vLongitude += (1.0 / 3600.0) * Convert.ToDouble(vPartString, NumberFormatInfo.InvariantInfo);
                vString = vString.Substring(vIndex + 3, vString.Length - (vIndex + 3));
                vString = vString.Trim();
            }

            //Sign (default Eeast)
            if (EarthCommon.StringCompare(vString, "west"))
            {
                vLongitude = -vLongitude;
            }

            vLongitude = EarthMath.CleanLongitude(vLongitude);
            return vLongitude;
        }


        //Thanks to steffen
        public static Boolean ParseCommandLineArguments(FSEarthTilesInternalInterface iFSEarthTilesInternalInterface, String[] iApplicationStartArguments)
        {
            String  vArgumentString = "";
            Boolean vAllOK          = true;
            Boolean vCompileScenery = false;

            if (iApplicationStartArguments != null)
            {
                if (iApplicationStartArguments.Length == 1)
                {
                    //If Argument count = 1 then it is a Config or a KML File passed as argument!
                    String vFile = iApplicationStartArguments[0];
                    if (EarthCommon.StringContains(vFile, ".kml"))
                    {
                        //KML file
                        mStartEarthKML.LoadKMLFile(vFile);

                        if (mStartEarthKML.IsValid())
                        {
                            mAreaDefModeStart = "2Points";
                            mAreaInputStart.AreaStartLatitude = mStartEarthKML.StartLatitude;
                            mAreaInputStart.AreaStopLatitude = mStartEarthKML.StopLatitude;
                            mAreaInputStart.AreaStartLongitude = mStartEarthKML.StartLongitude;
                            mAreaInputStart.AreaStopLongitude = mStartEarthKML.StopLongitude;
                        }
                    }
                    else
                    {
                        //Config File (or Partial Config Files)
                        LoadAsArgumentPassedConfigFile(iFSEarthTilesInternalInterface, vFile);
                    }
                }
                else
                {
                    for (Int32 vArgumentNr = 0; vArgumentNr < iApplicationStartArguments.Length; vArgumentNr++)
                    {
                        try
                        {
                            vArgumentString = iApplicationStartArguments[vArgumentNr];
                            if (EarthCommon.StringCompare(vArgumentString, "--width"))
                            {
                                vArgumentNr++;
                                Double vValue = Convert.ToDouble(iApplicationStartArguments[vArgumentNr], NumberFormatInfo.InvariantInfo);
                                mAreaInputStart.AreaXSize = vValue;
                                mAreaDefModeStart = "1Point";
                                continue;
                            }
                            if (EarthCommon.StringCompare(vArgumentString, "--height"))
                            {
                                vArgumentNr++;
                                Double vValue = Convert.ToDouble(iApplicationStartArguments[vArgumentNr], NumberFormatInfo.InvariantInfo);
                                mAreaInputStart.AreaYSize = vValue;
                                mAreaDefModeStart = "1Point";
                                continue;
                            }
                            if (EarthCommon.StringCompare(vArgumentString, "--zoom"))
                            {
                                vArgumentNr++;
                                Int32 vValue = Convert.ToInt32(iApplicationStartArguments[vArgumentNr]);
                                mFetchLevel = vValue;
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--lat"))
                            {
                                Double vLatitude = ParseLatitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.SpotLatitude = vLatitude;
                                mAreaDefModeStart = "1Point";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--lon"))
                            {
                                Double vLongitude = ParseLongitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.SpotLongitude = vLongitude;
                                mAreaDefModeStart = "1Point";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--north"))
                            {
                                Double vLatitude = ParseLatitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.AreaStartLatitude = vLatitude;
                                mAreaDefModeStart = "2Points";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--west"))
                            {
                                Double vLongitude = ParseLongitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.AreaStartLongitude = vLongitude;
                                mAreaDefModeStart = "2Points";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--south"))
                            {
                                Double vLatitude = ParseLatitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.AreaStopLatitude = vLatitude;
                                mAreaDefModeStart = "2Points";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--east"))
                            {
                                Double vLongitude = ParseLongitude(vArgumentNr, iApplicationStartArguments);
                                vArgumentNr += 4;
                                mAreaInputStart.AreaStopLongitude = vLongitude;
                                mAreaDefModeStart = "2Points";
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--snap"))
                            {
                                vArgumentNr++;
                                String vValue = iApplicationStartArguments[vArgumentNr];
                                SetAreaSnap(vValue);
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--compile"))
                            {
                                mCompileScenery = true;
                                vCompileScenery = true;
                                continue;
                            }

                            if (EarthCommon.StringCompare(vArgumentString, "--fetch"))
                            {
                                if (!vCompileScenery)
                                {
                                    mCompileScenery = false;
                                }

                                if (vAllOK)
                                {
                                    mAutoStartDownload = true;
                                    mAutoExitApplication = true;
                                }
                                continue;
                            }
                            if (EarthCommon.StringCompare(vArgumentString, "--AutoStartDownload"))
                            {
                                if (vAllOK)
                                {
                                    mAutoStartDownload = true;
                                }
                                continue;
                            }
                            if (EarthCommon.StringCompare(vArgumentString, "--AutoExitApplication"))
                            {
                                if (vAllOK)
                                {
                                    mAutoExitApplication = true;
                                }
                                continue;
                            }
                        }
                        catch (System.FormatException)
                        {
                            vAllOK = false;
                            MessageBox.Show("A number was expected but not found.", "Command line argument " + vArgumentString + " malformed.");
                        }
                    }
                }
            }
            return vAllOK;
        }


        private static Double ParseLatitude(Int32 iArgumentNr, string[] iApplicationStartArguments)
        {
           Double vLatitude;
           vLatitude = Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 1], NumberFormatInfo.InvariantInfo);
           vLatitude += (1.0 / 60.0) * Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 2], NumberFormatInfo.InvariantInfo);
           vLatitude += (1.0 / 3600.0) * Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 3], NumberFormatInfo.InvariantInfo);
           String vHemisphere = iApplicationStartArguments[iArgumentNr+4];
           if (EarthCommon.StringCompare(vHemisphere, "S"))
           {
              vLatitude = -vLatitude;
           }
           return vLatitude;
        }
        
        private static Double ParseLongitude(Int32 iArgumentNr, string[] iApplicationStartArguments)
        {
           Double vLongitude;
           vLongitude = Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 1], NumberFormatInfo.InvariantInfo);
           vLongitude += (1.0 / 60.0) * Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 2], NumberFormatInfo.InvariantInfo);
           vLongitude += (1.0 / 3600.0) * Convert.ToDouble(iApplicationStartArguments[iArgumentNr + 3], NumberFormatInfo.InvariantInfo);
           String vHemisphere = iApplicationStartArguments[iArgumentNr+4];
           if (EarthCommon.StringCompare(vHemisphere, "W"))
           {
              vLongitude = -vLongitude;
           }
           return vLongitude;
        }

        public static Boolean IsWithSeasons()  //summer and water do not count as season
        {
            Boolean vWithSeasons = false;

            if ((EarthConfig.mCreateNightBitmap) ||
                (EarthConfig.mCreateSpringBitmap) ||
                (EarthConfig.mCreateAutumnBitmap) ||
                (EarthConfig.mCreateWinterBitmap) ||
                (EarthConfig.mCreateHardWinterBitmap))
            {
                vWithSeasons = true;
            }

            return vWithSeasons;
        }

        public static void ParseLayFile(string filePath)
        {
            string rgstr = @"{switch:([^}]+)(,\s*[^}]+)*}";
            Regex rg = new Regex(rgstr);
            string[] fileContents = File.ReadAllLines(filePath);
            bool inGui = true;
            foreach (string line in fileContents)
            {
                if (line.Contains("in_GUI"))
                {
                    string[] toks = line.Split('=');
                    string inGuiStr = toks[1].Trim();
                    inGui = (inGuiStr == "True");
                }
            }
            foreach (string line in fileContents)
            {
                string url = Regex.Replace(line, "url_template=", "");
                if (url == line || url == "" || line[0] == '#')
                {
                    url = Regex.Replace(line, "url_prefix=", "");
                }
                if (url != line && url != "" && url[0] != '#')
                {
                    string switchStr = rg.Match(url).Value;
                    switchStr = Regex.Replace(switchStr, @"{switch:", "");
                    switchStr = Regex.Replace(switchStr, @"}", "");
                    LayProvider lp = new LayProvider(Path.GetFileNameWithoutExtension(filePath));
                    lp.inGui = inGui;
                    if (switchStr == "")
                    {
                        lp.variations = new List<string>();
                        lp.variations.Add(url);
                    }
                    else
                    {
                        string[] variationValues = switchStr.Split(',');
                        List<string> variations = new List<string>();
                        foreach (string s in variationValues)
                        {
                            variations.Add(Regex.Replace(url, rgstr, s));
                        }
                        lp.variations = variations;
                    }
                    layProviders.Add(lp.name, lp);
                }
            }
        }

    }


  
}
