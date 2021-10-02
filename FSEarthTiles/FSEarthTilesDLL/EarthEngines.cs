using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using FSEarthTilesInternalDLL;

//             FS Earth Tiles Engines (Threading)
//-------------------------------------------------------------------------------------
// FS Earth Tiles uses 4 Engines to download Tiles form the Earth services
// Every Engine is an own Thread.
// 
// Services usually offers 4 access addresses (server variations /service sources) with
// identical material.
//
// Every Engine is fix assigned to one of this addresses.
// If a service has only 1 address all 4 engines accesses to that one.
// 
//-------------------------------------------------------------------------------------


namespace FSEarthTilesDLL
{

    class EarthEngine
    {
        //Mutexes
        public Mutex mEngineMutex; //Mutex for Engine1 (SubThread n, ServerVariation n)
        //Multithreading Synchronising Flags/Signs.
        public AutoResetEvent mSignForEngine; //Multithreading Synchronising Flags/Signs.
        //Multithreading Mutex protected access only!
        public TileInfoFIFOQueue mQueueEngine; //This are the Input Queues ..the pending Tiles to Download
        public Tile mTileOfEngine; //This is the Output Tile. No Queue here. MainApp is fast enough.
        public Boolean mTileOfEngineReady; //This Sign Flag
        public Boolean mTileInWorkEngine; //True when the Engine is corrently downloading a Tile
        public TileInfo mWorkTileInfoEngine; //The Information of the corrently Tile the Engine downloads
        //Engines Own Stuff
        public Bitmap mNoTileFoundEngine;
        //Cookies
        public CookieCollection mEngineCookies;
        public Boolean mEngineHandleCookies;
        public String mEngineProxy;

        public EarthEngine(int cEngineQueueSize)
        {
            mEngineMutex = new Mutex();
            mSignForEngine = new AutoResetEvent(false);
            mQueueEngine = new TileInfoFIFOQueue(cEngineQueueSize);

            mTileOfEngine = new Tile();
            mTileOfEngineReady = false;
            mTileInWorkEngine = false;

            TileInfo vEmptyTileInfo = new TileInfo();

            mWorkTileInfoEngine = new TileInfo(vEmptyTileInfo);

            mEngineCookies = new CookieCollection();
            
            mEngineHandleCookies = false;

            mEngineProxy = "direct";
        }
    }

    class EarthEngines
    {

        //constants
        private const Int32 cEngineQueueSize = 10;
        private static String mStatusFromEngines;

        private static List<EarthEngine> earthEngines = new List<EarthEngine>(EarthConfig.mMaxDownloadThreads);

        //Mutexes
        private static Mutex mExclusiveMutex;  //exclusive Block of all threads

        public static void PrepareMultiThreading()
        {
            //Set up the  Threading
            mExclusiveMutex = new Mutex();
            for (int i = 0; i < EarthConfig.mMaxDownloadThreads; i++)
            {
                earthEngines.Add(new EarthEngine(cEngineQueueSize));
            }

            mStatusFromEngines = "";

        }

        public static void SetProxyEngines(String iProxy)
        {
            foreach (EarthEngine e in earthEngines)
            {
                e.mEngineMutex.WaitOne();
                e.mEngineProxy = iProxy;
                e.mEngineMutex.ReleaseMutex();
            }
        }

        public static void SetCookiesEngines(CookieCollection iCookieCollection, Boolean iHandleCookies)
        {
            foreach (EarthEngine e in earthEngines)
            {
                e.mEngineMutex.WaitOne();
                e.mEngineCookies = new CookieCollection(); //delete old List
                e.mEngineCookies.Add(iCookieCollection);
                e.mEngineHandleCookies = iHandleCookies;
                e.mEngineMutex.ReleaseMutex();
            }
        }

        public static void SetNoTileFoundBitmapEngines(Bitmap iNoTileFound)
        {
            foreach (EarthEngine e in earthEngines)
            {
                e.mEngineMutex.WaitOne();
                e.mNoTileFoundEngine = (Bitmap)iNoTileFound.Clone();
                e.mEngineMutex.ReleaseMutex();
            }
        }


        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        public static Boolean CheckForCachedFile(String FileToCheck) 
        {
            Boolean FileisThere = false;
            
            // Check if the file already exists in the cache directory
            try
            {

                if (System.IO.File.Exists(FileToCheck))
                {
                    System.IO.FileInfo fInfo = new System.IO.FileInfo(FileToCheck);
                    if (fInfo.Length > 0 && EarthConfig.mUseCache)
                    {
                        FileisThere = true;
                    }
                }
                return FileisThere;
            }



            catch
            {
                FileisThere = false;
                return FileisThere;
            }

        }

        public static Boolean SaveTileToCache(String FileToSave, Bitmap TileBitmap)
        {
            try
            {
                EncoderParameters encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);
                TileBitmap.Save(FileToSave, GetEncoder(ImageFormat.Jpeg), encoderParameters);
                return true;
            }

            catch
            {
                return false;
            }
        }



        //Threading Engines Every Engine is 1 independent Thread (yes it is 4 times almost identical code. But it's a lot better for debugging to keep this in 4 seperate methodes)
        public static void EngineKingdom(int engineNumber)
        {
            //Preparation
            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceReference = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;
            EarthEngine ea = earthEngines[engineNumber];

            do //Endless Loop
            {

                //Get TileInfo from FIFO Queue
                do
                {
                    ea.mSignForEngine.WaitOne();

                    ea.mEngineMutex.WaitOne();
                    if (!ea.mQueueEngine.IsEmpty())
                    {
                        ea.mWorkTileInfoEngine = ea.mQueueEngine.GetNextTileInfoClone();
                        ea.mTileInWorkEngine = true;
                    }
                    ea.mEngineMutex.ReleaseMutex();


                } while (!ea.mTileInWorkEngine);


                //Do the Work
                Tile vTile = new Tile(ea.mWorkTileInfoEngine);

                //Engine1 uses ServiceVariation 0 fix
                vServiceStringBegin = EarthConfig.mServiceUrlBegin0[ea.mWorkTileInfoEngine.mService - 1];
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[ea.mWorkTileInfoEngine.mService - 1];
                vServiceReference = EarthConfig.mServiceReferer[ea.mWorkTileInfoEngine.mService - 1];
                //vServiceUserAgent = EarthConfig.mServiceCodeing[mWorkTileInfoEngine1.mService - 1];
                vServiceUserAgent = EarthConfig.mServiceUserAgent[ea.mWorkTileInfoEngine.mService - 1];

                mExclusiveMutex.WaitOne();
                vTileCode = MapAreaCoordToTileCodeForEnginesOnly(ea.mWorkTileInfoEngine.mAreaCodeX, ea.mWorkTileInfoEngine.mAreaCodeY, ea.mWorkTileInfoEngine.mLevel, ea.mWorkTileInfoEngine.mService);
                mExclusiveMutex.ReleaseMutex();

                if (EarthConfig.layServiceMode)
                {
                    vFullTileAddress = EarthConfig.layProviders[EarthConfig.layServiceSelected].getURL(0, ea.mWorkTileInfoEngine.mAreaCodeX, ea.mWorkTileInfoEngine.mAreaCodeY, EarthMath.cLevel0CodeDeep - ea.mWorkTileInfoEngine.mLevel);
                }
                else
                {
                    vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;
                }
                Console.WriteLine(vFullTileAddress);

                Int64 vRetries = 0;
                Boolean vTileReady = false;

                //Access
                do //Repeat
                   // Will try and save bitmap to file and use cached tiles in this area 
                {
                    try
                    {

                        // Generate new bitmap file name
                        String vTileFilename = "";
                        String vTileProvider = "";
                        Boolean vTileCached = false;


                        vTileProvider = EarthConfig.mServiceName[EarthConfig.mSelectedService - 1];
                        vTileFilename = EarthConfig.mWorkFolder + "\\cache\\";
                        vTileFilename += ea.mWorkTileInfoEngine.mAreaCodeX + "_" + ea.mWorkTileInfoEngine.mAreaCodeY + "_" + ea.mWorkTileInfoEngine.mLevel + "_" + vTileProvider + ".jpg";

                        vTileCached = CheckForCachedFile(vTileFilename);

                        // File is not cached get from web
                        if (ea.mWorkTileInfoEngine.mSkipTile || vTileCached)

                        {
                            // Use our locally cached file

                            if (!ea.mWorkTileInfoEngine.mSkipTile)
                            {

                                Bitmap mybitmap = new Bitmap(vTileFilename);
                                vTile.StoreBitmap(mybitmap);
                                vTile.MarkAsGoodBitmap();
                                vTileReady = true;
                            }

                            else
                            {

                                try
                                {
                                    //Bitmap mybitmap = new Bitmap(EarthConfig.mStartExeFolder + "\\" + "Blank.jpg");
                                    Bitmap mybitmap = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                                    Graphics gfx = Graphics.FromImage(mybitmap);
                                    SolidBrush brush = new SolidBrush(Color.FromArgb(EarthConfig.mBlankTileColorRed, EarthConfig.mBlankTileColorGreen, EarthConfig.mBlankTileColorBlue));
                                    {
                                        gfx.FillRectangle(brush, 0, 0, 256, 256);
                                    }
                                    vTile.StoreBitmap(mybitmap);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;


                                }
                                catch
                                {

                                }
                            }


                        }

                        else
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
                            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(myTileUri);
                            if (EarthConfig.layServiceMode)
                            {
                                request.Referer = myTileUri.Scheme + "://" + request.Host;
                                request.UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36";
                            }
                            else
                            {
                                request.Referer = vServiceReference;
                                request.UserAgent = vServiceUserAgent;
                            }
                            Console.WriteLine(request.Referer);
                            Console.WriteLine(request.UserAgent);

                            if (!EarthCommon.StringCompare(ea.mEngineProxy, "direct"))
                            {
                                WebProxy vProxy = new WebProxy("http://" + ea.mEngineProxy + "/", true);
                                request.Proxy = vProxy;
                            }

                            if (ea.mEngineHandleCookies)
                            {
                                request.CookieContainer = new CookieContainer();
                                if (ea.mEngineCookies.Count > 0)
                                {
                                    request.CookieContainer.Add(ea.mEngineCookies);
                                }
                            }

                            System.Net.WebResponse response = request.GetResponse();

                            System.IO.Stream vPicStreamReadBack;

                            vPicStreamReadBack = response.GetResponseStream();

                            Bitmap mybitmap = new Bitmap(vPicStreamReadBack);

                            vPicStreamReadBack.Close(); //Close Stream/File;
                            vTile.StoreBitmap(mybitmap);
                            vTile.MarkAsGoodBitmap();
                            vTileReady = true;

                            if (EarthConfig.mUseCache && !ea.mWorkTileInfoEngine.mSkipTile)
                            {

                                SaveTileToCache(vTileFilename, mybitmap);

                            }

                        }





                    }
                    catch (System.Net.WebException e)
                    {
                        if (e.Status == WebExceptionStatus.ProtocolError)
                        {
                            HttpStatusCode vStatusCode = ((HttpWebResponse)e.Response).StatusCode;
                            if ((vStatusCode == HttpStatusCode.NotFound) || (vStatusCode == HttpStatusCode.BadRequest))
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found and mark as good
                                    vTile.StoreBitmap(ea.mNoTileFoundEngine);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;
                                }
                            }
                            else
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found
                                    SetStatusFromEngines("Can not web-access Engine" + engineNumber.ToString() + ": " + vFullTileAddress);
                                    vTile.StoreBitmap(ea.mNoTileFoundEngine);
                                    vTile.MarkAsBadBitmap();
                                    vTileReady = true;
                                }
                            }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not web-access Engine" + engineNumber.ToString() + " (2of2) : " + vFullTileAddress);

                            vTile.StoreBitmap(ea.mNoTileFoundEngine);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not web-access Engine" + engineNumber.ToString() + " (1of2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not other-access Engine" + engineNumber.ToString() + " (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(ea.mNoTileFoundEngine);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not other-access Engine" + engineNumber.ToString() + " (1of2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while ((vRetries <= 1) && (!vTileReady));

                Boolean vTileOutputFree = false;
                do
                {
                    ea.mEngineMutex.WaitOne();
                    if (!ea.mTileOfEngineReady)
                    {
                        vTileOutputFree = true;
                    }
                    ea.mEngineMutex.ReleaseMutex();
                    if (!vTileOutputFree)
                    {
                        Thread.Sleep(50); //ok wait a little give the Application Time. 
                    }
                } while (!vTileOutputFree); //we can not continue before the output placebecomes free

                ea.mEngineMutex.WaitOne();
                ea.mTileOfEngine = new Tile(vTile);
                ea.mTileOfEngineReady = true;
                ea.mTileInWorkEngine = false;
                if (!ea.mQueueEngine.IsEmpty())
                {
                    ea.mSignForEngine.Set();  //sign self to continue
                }
                ea.mEngineMutex.ReleaseMutex();

            } while (true); //Repeat endless

        }


        //This function is for the Engienes only and the call has to be mutex protected.
        private static String MapAreaCoordToTileCodeForEnginesOnly(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iAreaCodeLevel, Int32 iService)
        {
            String vUseCode = EarthConfig.mServiceCodeing[iService - 1];
            String vResultCode = EarthScriptsHandler.MapAreaCoordToTileCode(iAreaCodeX, iAreaCodeY, iAreaCodeLevel, vUseCode);
            return vResultCode;
        }


        private static void SetStatusFromEngines(String iStatus)
        {
            mExclusiveMutex.WaitOne();
            mStatusFromEngines = iStatus;
            mExclusiveMutex.ReleaseMutex();
        }


        public static String GetStatusFromEngines()
        {
           String vEngineStatusFeedback;

            mExclusiveMutex.WaitOne();
            vEngineStatusFeedback  = mStatusFromEngines;
            mStatusFromEngines     = "";
            mExclusiveMutex.ReleaseMutex();

            return vEngineStatusFeedback;
        }

        public static Boolean CheckForTileOfEngine(int engineNumber)
        {
            Boolean vThereIsATileFromEngine;
            EarthEngine ea = earthEngines[engineNumber];

            ea.mEngineMutex.WaitOne();
            vThereIsATileFromEngine = ea.mTileOfEngineReady;
            ea.mEngineMutex.ReleaseMutex();

            return vThereIsATileFromEngine;
        }

        public static Tile GetTileOfEngine(int engineNumber)
        {
            Tile vTileOfEngine;
            EarthEngine ea = earthEngines[engineNumber];

            ea.mEngineMutex.WaitOne();
            vTileOfEngine            = new Tile(ea.mTileOfEngine);
            ea.mTileOfEngineReady       = false;
            ea.mEngineMutex.ReleaseMutex();

            return vTileOfEngine;
        }


        public static Int32 GetFreeSpaceOfEngine(int engineNumber)
        {
            Int32 vFreeEngine;
            EarthEngine ea = earthEngines[engineNumber];

            ea.mEngineMutex.WaitOne();
            vFreeEngine = ea.mQueueEngine.GetFreeSpace();
            ea.mEngineMutex.ReleaseMutex();

            return vFreeEngine;
        }

        public static Int32 AddTileInfoToEngine(int engineNumber, TileInfo iTileInfo)
        {
            Int32 vFreeEngine;
            EarthEngine ea = earthEngines[engineNumber];

            ea.mEngineMutex.WaitOne();
            ea.mQueueEngine.AddTileInfoNoDoubles(iTileInfo);
            vFreeEngine = ea.mQueueEngine.GetFreeSpace();
            ea.mSignForEngine.Set();
            ea.mEngineMutex.ReleaseMutex();

            return vFreeEngine;
        }

        public static void EmptyEnginesQueue()
        {
            foreach (EarthEngine e in earthEngines)
            {
                e.mEngineMutex.WaitOne();
                e.mQueueEngine.DoEmpty();
                e.mEngineMutex.ReleaseMutex();
            }
        }

        public static Int32 GetEnginesQueueSize()
        {
            return cEngineQueueSize;
        }

        public static Boolean IsEngineTileFree(int engineNumber)
        {
            Boolean vTileFree = false;
            EarthEngine ea = earthEngines[engineNumber];

            Int32 vFreeEngine;

            ea.mEngineMutex.WaitOne();
            vFreeEngine = ea.mQueueEngine.GetFreeSpace();
            if ((vFreeEngine == cEngineQueueSize) && !ea.mTileInWorkEngine && !ea.mTileOfEngineReady)
            {
                vTileFree = true;
            }
            ea.mEngineMutex.ReleaseMutex();
            return vTileFree;
        }

    }
}
