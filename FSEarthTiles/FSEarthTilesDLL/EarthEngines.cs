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

    class EarthEngines
    {

        //constants
        private const Int32 cEngineQueueSize = 10;

        //Mutexes
        private static Mutex mExclusiveMutex;  //exclusive Block of all threads
        private static Mutex mEngine1Mutex;    //Mutex for Engine1 (SubThread 1, ServerVariation 1)
        private static Mutex mEngine2Mutex;    //Mutex for Engine2 (SubThread 2, ServerVariation 2)
        private static Mutex mEngine3Mutex;    //Mutex for Engine3 (SubThread 3, ServerVariation 3)
        private static Mutex mEngine4Mutex;    //Mutex for Engine4 (SubThread 4, ServerVariation 4)

        //Multithreading Synchronising Flags/Signs.
        private static AutoResetEvent mSignForEngine1;
        private static AutoResetEvent mSignForEngine2;
        private static AutoResetEvent mSignForEngine3;
        private static AutoResetEvent mSignForEngine4;

        //Multithreading Mutex protected access only!
        private static TileInfoFIFOQueue mQueueEngine1;       //This are the Input Queues ..the pending Tiles to Download
        private static TileInfoFIFOQueue mQueueEngine2;
        private static TileInfoFIFOQueue mQueueEngine3;
        private static TileInfoFIFOQueue mQueueEngine4;
        private static Tile              mTileOfEngine1;      //This is the Output Tile. No Queue here. MainApp is fast enough.
        private static Tile              mTileOfEngine2;
        private static Tile              mTileOfEngine3;
        private static Tile              mTileOfEngine4;
        private static Boolean           mTileOfEngine1Ready;      //This Sign Flag
        private static Boolean           mTileOfEngine2Ready;
        private static Boolean           mTileOfEngine3Ready;
        private static Boolean           mTileOfEngine4Ready;
        private static Boolean           mTileInWorkEngine1;        //True when the Engine is corrently downloading a Tile
        private static Boolean           mTileInWorkEngine2;
        private static Boolean           mTileInWorkEngine3;
        private static Boolean           mTileInWorkEngine4;
        private static TileInfo          mWorkTileInfoEngine1;      //The Information of the corrently Tile the Engine downloads
        private static TileInfo          mWorkTileInfoEngine2;
        private static TileInfo          mWorkTileInfoEngine3;
        private static TileInfo          mWorkTileInfoEngine4;
        private static String            mStatusFromEngines;

        //Engines Own Stuff
        private static Bitmap mNoTileFoundEngine1;
        private static Bitmap mNoTileFoundEngine2;
        private static Bitmap mNoTileFoundEngine3;
        private static Bitmap mNoTileFoundEngine4;

        //Cookies
        private static CookieCollection mEngine1Cookies;
        private static CookieCollection mEngine2Cookies;
        private static CookieCollection mEngine3Cookies;
        private static CookieCollection mEngine4Cookies;
        private static Boolean mEngine1HandleCookies;
        private static Boolean mEngine2HandleCookies;
        private static Boolean mEngine3HandleCookies;
        private static Boolean mEngine4HandleCookies;

        private static String mEngine1Proxy;
        private static String mEngine2Proxy;
        private static String mEngine3Proxy;
        private static String mEngine4Proxy;

        public static void PrepareMultiThreading()
        {
            //Set up the  Threading
            mExclusiveMutex = new Mutex();
            mEngine1Mutex = new Mutex();
            mEngine2Mutex = new Mutex();
            mEngine3Mutex = new Mutex();
            mEngine4Mutex = new Mutex();
            mSignForEngine1 = new AutoResetEvent(false);
            mSignForEngine2 = new AutoResetEvent(false);
            mSignForEngine3 = new AutoResetEvent(false);
            mSignForEngine4 = new AutoResetEvent(false);
            mQueueEngine1 = new TileInfoFIFOQueue(cEngineQueueSize);
            mQueueEngine2 = new TileInfoFIFOQueue(cEngineQueueSize);
            mQueueEngine3 = new TileInfoFIFOQueue(cEngineQueueSize);
            mQueueEngine4 = new TileInfoFIFOQueue(cEngineQueueSize);

            mTileOfEngine1 = new Tile();
            mTileOfEngine2 = new Tile();
            mTileOfEngine3 = new Tile();
            mTileOfEngine4 = new Tile();
            mTileOfEngine1Ready = false;
            mTileOfEngine2Ready = false;
            mTileOfEngine3Ready = false;
            mTileOfEngine4Ready = false;
            mTileInWorkEngine1 = false;
            mTileInWorkEngine2 = false;
            mTileInWorkEngine3 = false;
            mTileInWorkEngine4 = false;

            mStatusFromEngines = "";

            TileInfo vEmptyTileInfo = new TileInfo();

            mWorkTileInfoEngine1 = new TileInfo(vEmptyTileInfo);
            mWorkTileInfoEngine2 = new TileInfo(vEmptyTileInfo);
            mWorkTileInfoEngine3 = new TileInfo(vEmptyTileInfo);
            mWorkTileInfoEngine4 = new TileInfo(vEmptyTileInfo);

            mEngine1Cookies = new CookieCollection();
            mEngine2Cookies = new CookieCollection();
            mEngine3Cookies = new CookieCollection();
            mEngine4Cookies = new CookieCollection();
            
            mEngine1HandleCookies = false;
            mEngine2HandleCookies = false;
            mEngine3HandleCookies = false;
            mEngine4HandleCookies = false;

            mEngine1Proxy = "direct";
            mEngine2Proxy = "direct";
            mEngine3Proxy = "direct";
            mEngine4Proxy = "direct";

        }

        public static void SetProxyEngine1(String iProxy)
        {
            mEngine1Mutex.WaitOne();
            mEngine1Proxy = iProxy;
            mEngine1Mutex.ReleaseMutex();
        }
        
        public static void SetProxyEngine2(String iProxy)
        {
            mEngine2Mutex.WaitOne();
            mEngine2Proxy = iProxy;
            mEngine2Mutex.ReleaseMutex();
        }

        public static void SetProxyEngine3(String iProxy)
        {
            mEngine3Mutex.WaitOne();
            mEngine3Proxy = iProxy;
            mEngine3Mutex.ReleaseMutex();
        }

        public static void SetProxyEngine4(String iProxy)
        {
            mEngine4Mutex.WaitOne();
            mEngine4Proxy = iProxy;
            mEngine4Mutex.ReleaseMutex();
        }

        public static void SetCookiesEngine1(CookieCollection iCookieCollection, Boolean iHandleCookies)
        {
            mEngine1Mutex.WaitOne();
            mEngine1Cookies = new CookieCollection(); //delete old List
            mEngine1Cookies.Add(iCookieCollection);
            mEngine1HandleCookies = iHandleCookies;
            mEngine1Mutex.ReleaseMutex();
        }

        public static void SetCookiesEngine2(CookieCollection iCookieCollection, Boolean iHandleCookies)
        {
            mEngine2Mutex.WaitOne();
            mEngine2Cookies = new CookieCollection(); //delete old List
            mEngine2Cookies.Add(iCookieCollection);
            mEngine2HandleCookies = iHandleCookies;
            mEngine2Mutex.ReleaseMutex();
        }

        public static void SetCookiesEngine3(CookieCollection iCookieCollection, Boolean iHandleCookies)
        {
            mEngine3Mutex.WaitOne();
            mEngine3Cookies = new CookieCollection(); //delete old List
            mEngine3Cookies.Add(iCookieCollection);
            mEngine3HandleCookies = iHandleCookies;
            mEngine3Mutex.ReleaseMutex();
        }

        public static void SetCookiesEngine4(CookieCollection iCookieCollection, Boolean iHandleCookies)
        {
            mEngine4Mutex.WaitOne();
            mEngine4Cookies = new CookieCollection(); //delete old List
            mEngine4Cookies.Add(iCookieCollection);
            mEngine4HandleCookies = iHandleCookies;
            mEngine4Mutex.ReleaseMutex();
        }

        public static void SetNoTileFoundBitmapEngine1(Bitmap iNoTileFound)
        {
            mEngine1Mutex.WaitOne();
            mNoTileFoundEngine1 = (Bitmap)iNoTileFound.Clone();
            mEngine1Mutex.ReleaseMutex();
        }


        public static void SetNoTileFoundBitmapEngine2(Bitmap iNoTileFound)
        {
            mEngine2Mutex.WaitOne();
            mNoTileFoundEngine2 = (Bitmap)iNoTileFound.Clone();
            mEngine2Mutex.ReleaseMutex();
        }


        public static void SetNoTileFoundBitmapEngine3(Bitmap iNoTileFound)
        {
            mEngine3Mutex.WaitOne();
            mNoTileFoundEngine3 = (Bitmap)iNoTileFound.Clone();
            mEngine3Mutex.ReleaseMutex();
        }


        public static void SetNoTileFoundBitmapEngine4(Bitmap iNoTileFound)
        {
            mEngine4Mutex.WaitOne();
            mNoTileFoundEngine4 = (Bitmap)iNoTileFound.Clone();
            mEngine4Mutex.ReleaseMutex();
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
        public static void Engine1Kingdom()
        {
            //Preparation
            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceReference = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;

            do //Endless Loop
            {

                //Get TileInfo from FIFO Queue
                do
                {
                    mSignForEngine1.WaitOne();

                    mEngine1Mutex.WaitOne();
                    if (!mQueueEngine1.IsEmpty())
                    {
                        mWorkTileInfoEngine1 = mQueueEngine1.GetNextTileInfoClone();
                        mTileInWorkEngine1 = true;
                    }
                    mEngine1Mutex.ReleaseMutex();


                } while (!mTileInWorkEngine1);


                //Do the Work
                Tile vTile = new Tile(mWorkTileInfoEngine1);

                //Engine1 uses ServiceVariation 0 fix
                vServiceStringBegin = EarthConfig.mServiceUrlBegin0[mWorkTileInfoEngine1.mService - 1];
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[mWorkTileInfoEngine1.mService - 1];
                vServiceReference = EarthConfig.mServiceReferer[mWorkTileInfoEngine1.mService - 1];
                //vServiceUserAgent = EarthConfig.mServiceCodeing[mWorkTileInfoEngine1.mService - 1];
                vServiceUserAgent = EarthConfig.mServiceUserAgent[mWorkTileInfoEngine1.mService - 1];

                mExclusiveMutex.WaitOne();
                vTileCode = MapAreaCoordToTileCodeForEnginesOnly(mWorkTileInfoEngine1.mAreaCodeX, mWorkTileInfoEngine1.mAreaCodeY, mWorkTileInfoEngine1.mLevel, mWorkTileInfoEngine1.mService);
                mExclusiveMutex.ReleaseMutex();

                vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;

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
                        

                        vTileProvider = EarthConfig.mServiceName[EarthConfig.mSelectedService-1];
                        vTileFilename = EarthConfig.mWorkFolder + "\\cache\\";
                        vTileFilename += mWorkTileInfoEngine1.mAreaCodeX + "_" + mWorkTileInfoEngine1.mAreaCodeY + "_" + mWorkTileInfoEngine1.mLevel + "_"  + vTileProvider +".jpg";
                        
                        vTileCached = CheckForCachedFile(vTileFilename);

                        // File is not cached get from web
                        if (mWorkTileInfoEngine1.mSkipTile|| vTileCached )
                        
                        {
                            // Use our locally cached file

                            if (!mWorkTileInfoEngine1.mSkipTile)
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
                                    SolidBrush brush = new SolidBrush(Color.FromArgb(EarthConfig.mBlankTileColorRed,EarthConfig.mBlankTileColorGreen,EarthConfig.mBlankTileColorBlue ));
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
                            request.Referer = vServiceReference;
                            request.UserAgent = vServiceUserAgent ;

                            if (!EarthCommon.StringCompare(mEngine1Proxy, "direct"))
                            {
                                WebProxy vProxy = new WebProxy("http://" + mEngine1Proxy + "/", true);
                                request.Proxy = vProxy;
                            }

                            if (mEngine1HandleCookies)
                            {
                                request.CookieContainer = new CookieContainer();
                                if (mEngine1Cookies.Count > 0)
                                {
                                    request.CookieContainer.Add(mEngine1Cookies);
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

                            if (EarthConfig.mUseCache&&!mWorkTileInfoEngine1.mSkipTile )
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
                                    vTile.StoreBitmap(mNoTileFoundEngine1);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;
                                }
                            }
                            else
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found
                                    SetStatusFromEngines("Can not web-access Engine1: " + vFullTileAddress);
                                    vTile.StoreBitmap(mNoTileFoundEngine1);
                                    vTile.MarkAsBadBitmap();
                                    vTileReady = true;
                                }
                            }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not web-access Engine1 (2of2) : " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine1);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not web-access Engine1 (1of2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not other-access Engine1 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine1);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not other-access Engine1 (1of2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while ((vRetries <= 1) && (!vTileReady));

                Boolean vTileOutputFree = false;
                do
                {
                    mEngine1Mutex.WaitOne();
                    if (!mTileOfEngine1Ready)
                    {
                        vTileOutputFree = true;
                    }
                    mEngine1Mutex.ReleaseMutex();
                    if (!vTileOutputFree)
                    {
                        Thread.Sleep(50); //ok wait a little give the Application Time. 
                    }
                } while (!vTileOutputFree); //we can not continue before the output placebecomes free

                mEngine1Mutex.WaitOne();
                mTileOfEngine1 = new Tile(vTile);
                mTileOfEngine1Ready = true;
                mTileInWorkEngine1 = false;
                if (!mQueueEngine1.IsEmpty())
                {
                    mSignForEngine1.Set();  //sign self to continue
                }
                mEngine1Mutex.ReleaseMutex();

            } while (true); //Repeat endless

        }


        public static void Engine2Kingdom()
        {
            //Preparation
            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceReference = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;

            do //Endless Loop
            {

                //Get TileInfo from FIFO Queue
                do
                {
                    mSignForEngine2.WaitOne();

                    mEngine2Mutex.WaitOne();
                    if (!mQueueEngine2.IsEmpty())
                    {
                        mWorkTileInfoEngine2 = mQueueEngine2.GetNextTileInfoClone();
                        mTileInWorkEngine2 = true;
                    }
                    mEngine2Mutex.ReleaseMutex();


                } while (!mTileInWorkEngine2);


                //Do the Work
                Tile vTile = new Tile(mWorkTileInfoEngine2);

                //Engine2 uses ServiceVariation 1 fix
                vServiceStringBegin = EarthConfig.mServiceUrlBegin1[mWorkTileInfoEngine2.mService - 1];
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[mWorkTileInfoEngine2.mService - 1];
                vServiceReference = EarthConfig.mServiceReferer[mWorkTileInfoEngine2.mService - 1];
                //vServiceUserAgent = EarthConfig.mServiceCodeing[mWorkTileInfoEngine2.mService - 1];
                vServiceUserAgent = EarthConfig.mServiceUserAgent[mWorkTileInfoEngine2.mService - 1];

                mExclusiveMutex.WaitOne();
                vTileCode = MapAreaCoordToTileCodeForEnginesOnly(mWorkTileInfoEngine2.mAreaCodeX, mWorkTileInfoEngine2.mAreaCodeY, mWorkTileInfoEngine2.mLevel, mWorkTileInfoEngine2.mService);
                mExclusiveMutex.ReleaseMutex();

                vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;

                Int64 vRetries = 0;
                Boolean vTileReady = false;

                //Access
                do //Repeat
                {
                    try
                    {

                        // Generate new bitmap file name
                        String vTileFilename = "";
                        String vTileProvider = "";
                        Boolean vTileCached = false;

                        vTileProvider = EarthConfig.mServiceName[EarthConfig.mSelectedService - 1];
                        vTileFilename = EarthConfig.mWorkFolder + "\\cache\\";
                        vTileFilename += mWorkTileInfoEngine2.mAreaCodeX + "_" + mWorkTileInfoEngine2.mAreaCodeY + "_" + mWorkTileInfoEngine2.mLevel + "_"  + vTileProvider + ".jpg";

                        vTileCached = CheckForCachedFile(vTileFilename);
                        // File is not cached get from web
                        if (vTileCached || mWorkTileInfoEngine2.mSkipTile)

                                                    
                        {
                            // Use our locally cached file

                            if (!mWorkTileInfoEngine2.mSkipTile)
                            {
                                Bitmap mybitmap = new Bitmap(vTileFilename);
                                vTile.StoreBitmap(mybitmap);
                                vTile.MarkAsGoodBitmap();
                                vTileReady = true;
                            }

                            else
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
                        request.Referer = vServiceReference;
                        request.UserAgent = vServiceUserAgent ;

                        if (!EarthCommon.StringCompare(mEngine2Proxy, "direct"))
                        {
                            WebProxy vProxy = new WebProxy("http://" + mEngine2Proxy + "/", true);
                            request.Proxy = vProxy;
                        }

                        if (mEngine2HandleCookies)
                        {
                            request.CookieContainer = new CookieContainer();
                            if (mEngine2Cookies.Count > 0)
                            {
                                request.CookieContainer.Add(mEngine2Cookies);
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
                        if (EarthConfig.mUseCache && !mWorkTileInfoEngine2.mSkipTile)
                        
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
                                    vTile.StoreBitmap(mNoTileFoundEngine2);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;
                                }
                            }
                            else
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found
                                    SetStatusFromEngines("Can not web-access Engine2: " + vFullTileAddress);
                                    vTile.StoreBitmap(mNoTileFoundEngine2);
                                    vTile.MarkAsBadBitmap();
                                    vTileReady = true;
                                }
                            }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not web-access Engine2 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine2);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not web-access Engine2 (1of2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not other-access Engine2 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine2);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not other-access Engine2 (1of2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while ((vRetries <= 1) && (!vTileReady));

                Boolean vTileOutputFree = false;
                do
                {
                    mEngine2Mutex.WaitOne();
                    if (!mTileOfEngine2Ready)
                    {
                        vTileOutputFree = true;
                    }
                    mEngine2Mutex.ReleaseMutex();
                    if (!vTileOutputFree)
                    {
                        Thread.Sleep(50); //ok wait a little give the Application Time. 
                    }
                } while (!vTileOutputFree); //we can not continue before the output placebecomes free

                mEngine2Mutex.WaitOne();
                mTileOfEngine2 = new Tile(vTile);
                mTileOfEngine2Ready = true;
                mTileInWorkEngine2 = false;
                if (!mQueueEngine2.IsEmpty())
                {
                    mSignForEngine2.Set();  //sign self to continue
                }
                mEngine2Mutex.ReleaseMutex();

            } while (true); //Repeat endless
        }


        public static void Engine3Kingdom()
        {
            //Preparation
            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceReference = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;

            do //Endless Loop
            {

                //Get TileInfo from FIFO Queue
                do
                {
                    mSignForEngine3.WaitOne();

                    mEngine3Mutex.WaitOne();
                    if (!mQueueEngine3.IsEmpty())
                    {
                        mWorkTileInfoEngine3 = mQueueEngine3.GetNextTileInfoClone();
                        mTileInWorkEngine3 = true;
                    }
                    mEngine3Mutex.ReleaseMutex();


                } while (!mTileInWorkEngine3);


                //Do the Work
                Tile vTile = new Tile(mWorkTileInfoEngine3);

                //Engine3 uses ServiceVariation 2 fix
                vServiceStringBegin = EarthConfig.mServiceUrlBegin2[mWorkTileInfoEngine3.mService - 1];
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[mWorkTileInfoEngine3.mService - 1];
                vServiceReference = EarthConfig.mServiceReferer[mWorkTileInfoEngine3.mService - 1];
                //vServiceUserAgent = EarthConfig.mServiceCodeing[mWorkTileInfoEngine3.mService - 1];
                vServiceUserAgent = EarthConfig.mServiceUserAgent[mWorkTileInfoEngine3.mService - 1];

                mExclusiveMutex.WaitOne();
                vTileCode = MapAreaCoordToTileCodeForEnginesOnly(mWorkTileInfoEngine3.mAreaCodeX, mWorkTileInfoEngine3.mAreaCodeY, mWorkTileInfoEngine3.mLevel, mWorkTileInfoEngine3.mService);
                mExclusiveMutex.ReleaseMutex();

                vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;

                Int64 vRetries = 0;
                Boolean vTileReady = false;

                //Access
                do //Repeat
                {
                    try
                    {

                        // Generate new bitmap file name
                        String vTileFilename = "";
                        String vTileProvider = "";
                        Boolean vTileCached = false;

                        vTileProvider = EarthConfig.mServiceName[EarthConfig.mSelectedService - 1];

                        vTileFilename = EarthConfig.mWorkFolder + "\\cache\\";
                        vTileFilename += mWorkTileInfoEngine3.mAreaCodeX + "_" + mWorkTileInfoEngine3.mAreaCodeY + "_" + mWorkTileInfoEngine3.mLevel + "_"  + vTileProvider + ".jpg";

                        vTileCached = CheckForCachedFile(vTileFilename);

                        // File is not cached get from web
                        if (vTileCached || mWorkTileInfoEngine3.mSkipTile)
                        
                            
                        {
                            // Use our locally cached file

                            if (!mWorkTileInfoEngine3.mSkipTile)
                            {
                                Bitmap mybitmap = new Bitmap(vTileFilename);
                                vTile.StoreBitmap(mybitmap);
                                vTile.MarkAsGoodBitmap();
                                vTileReady = true;
                            }

                            else
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
                            request.Referer = vServiceReference;
                            request.UserAgent = vServiceUserAgent;

                            if (!EarthCommon.StringCompare(mEngine3Proxy, "direct"))
                            {
                                WebProxy vProxy = new WebProxy("http://" + mEngine3Proxy + "/", true);
                                request.Proxy = vProxy;
                            }

                            if (mEngine3HandleCookies)
                            {
                                request.CookieContainer = new CookieContainer();
                                if (mEngine3Cookies.Count > 0)
                                {
                                    request.CookieContainer.Add(mEngine3Cookies);
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

                            if (EarthConfig.mUseCache && !mWorkTileInfoEngine3.mSkipTile)
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
                                    vTile.StoreBitmap(mNoTileFoundEngine3);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;
                                }
                            }
                            else
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found
                                    SetStatusFromEngines("Can not web-access Engine3: " + vFullTileAddress);
                                    vTile.StoreBitmap(mNoTileFoundEngine3);
                                    vTile.MarkAsBadBitmap();
                                    vTileReady = true;
                                }
                            }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not web-access Engine3 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine3);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not web-access Engine3 (1of2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not other-access Engine3 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine3);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not other-access Engine3 (1of2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while ((vRetries <= 1) && (!vTileReady));

                Boolean vTileOutputFree = false;
                do
                {
                    mEngine3Mutex.WaitOne();
                    if (!mTileOfEngine3Ready)
                    {
                        vTileOutputFree = true;
                    }
                    mEngine3Mutex.ReleaseMutex();
                    if (!vTileOutputFree)
                    {
                        Thread.Sleep(50); //ok wait a little give the Application Time. 
                    }
                } while (!vTileOutputFree); //we can not continue before the output placebecomes free

                mEngine3Mutex.WaitOne();
                mTileOfEngine3 = new Tile(vTile);
                mTileOfEngine3Ready = true;
                mTileInWorkEngine3 = false;
                if (!mQueueEngine3.IsEmpty())
                {
                    mSignForEngine3.Set();  //sign self to continue
                }
                mEngine3Mutex.ReleaseMutex();

            } while (true); //Repeat endless
        }

        public static void Engine4Kingdom()
        {
            //Preparation
            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceReference = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;

            do //Endless Loop
            {

                //Get TileInfo from FIFO Queue
                do
                {
                    mSignForEngine4.WaitOne();

                    mEngine4Mutex.WaitOne();
                    if (!mQueueEngine4.IsEmpty())
                    {
                        mWorkTileInfoEngine4 = mQueueEngine4.GetNextTileInfoClone();
                        mTileInWorkEngine4 = true;
                    }
                    mEngine4Mutex.ReleaseMutex();


                } while (!mTileInWorkEngine4);


                //Do the Work
                Tile vTile = new Tile(mWorkTileInfoEngine4);

                //Engine4 uses ServiceVariation 3 fix
                vServiceStringBegin = EarthConfig.mServiceUrlBegin3[mWorkTileInfoEngine4.mService - 1];
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[mWorkTileInfoEngine4.mService - 1];
                vServiceReference = EarthConfig.mServiceReferer[mWorkTileInfoEngine4.mService - 1];
                //vServiceUserAgent = EarthConfig.mServiceCodeing[mWorkTileInfoEngine4.mService - 1];
                vServiceUserAgent = EarthConfig.mServiceUserAgent[mWorkTileInfoEngine4.mService - 1];


                mExclusiveMutex.WaitOne();
                vTileCode = MapAreaCoordToTileCodeForEnginesOnly(mWorkTileInfoEngine4.mAreaCodeX, mWorkTileInfoEngine4.mAreaCodeY, mWorkTileInfoEngine4.mLevel, mWorkTileInfoEngine4.mService);
                mExclusiveMutex.ReleaseMutex();

                vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;

                Int64 vRetries = 0;
                Boolean vTileReady = false;

                //Access
                do //Repeat
                {
                    try
                    {

                        // Generate new bitmap file name
                        String vTileFilename = "";
                        String vTileProvider = "";
                        Boolean vTileCached = false;

                        vTileProvider = EarthConfig.mServiceName[EarthConfig.mSelectedService - 1];
                        vTileFilename = EarthConfig.mWorkFolder + "\\cache\\";
                        vTileFilename += mWorkTileInfoEngine4.mAreaCodeX + "_" + mWorkTileInfoEngine4.mAreaCodeY + "_" + mWorkTileInfoEngine4.mLevel + "_" +  vTileProvider + ".jpg";

                        vTileCached = CheckForCachedFile(vTileFilename);

                        // File is not cached get from web
                        if (vTileCached || mWorkTileInfoEngine4.mSkipTile)
                       
                        {
                            // Use our locally cached file

                            if (!mWorkTileInfoEngine4.mSkipTile)
                            {
                                Bitmap mybitmap = new Bitmap(vTileFilename);
                                vTile.StoreBitmap(mybitmap);
                                vTile.MarkAsGoodBitmap();
                                vTileReady = true;
                            }

                            else
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
                            request.Referer = vServiceReference;
                            request.UserAgent = vServiceUserAgent;

                            if (!EarthCommon.StringCompare(mEngine4Proxy, "direct"))
                            {
                                WebProxy vProxy = new WebProxy("http://" + mEngine4Proxy + "/", true);
                                request.Proxy = vProxy;
                            }

                            if (mEngine4HandleCookies)
                            {
                                request.CookieContainer = new CookieContainer();
                                if (mEngine4Cookies.Count > 0)
                                {
                                    request.CookieContainer.Add(mEngine4Cookies);
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

                            if (EarthConfig.mUseCache && !mWorkTileInfoEngine4.mSkipTile)
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
                                    vTile.StoreBitmap(mNoTileFoundEngine4);
                                    vTile.MarkAsGoodBitmap();
                                    vTileReady = true;
                                }
                            }
                            else
                            {
                                if (vRetries >= 1)
                                {
                                    //Set No Tile Found
                                    SetStatusFromEngines("Can not web-access Engine4: " + vFullTileAddress);
                                    vTile.StoreBitmap(mNoTileFoundEngine4);
                                    vTile.MarkAsBadBitmap();
                                    vTileReady = true;
                                }
                            }
                        }
                        else if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not web-access Engine4 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine4);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not web-access Engine4 (1of2): " + vFullTileAddress);
                        }
                    }
                    catch
                    {
                        if (vRetries >= 1)
                        {
                            //exit
                            SetStatusFromEngines("Can not other-access Engine4 (2of2): " + vFullTileAddress);

                            vTile.StoreBitmap(mNoTileFoundEngine4);
                            vTile.MarkAsBadBitmap();
                            vTileReady = true;
                        }
                        else
                        {
                            SetStatusFromEngines("Can not other-access Engine4 (1of2): " + vFullTileAddress);
                        }

                    }
                    vRetries++;
                } while ((vRetries <= 1) && (!vTileReady));

                Boolean vTileOutputFree = false;
                do
                {
                    mEngine4Mutex.WaitOne();
                    if (!mTileOfEngine4Ready)
                    {
                        vTileOutputFree = true;
                    }
                    mEngine4Mutex.ReleaseMutex();
                    if (!vTileOutputFree)
                    {
                        Thread.Sleep(50); //ok wait a little give the Application Time. 
                    }
                } while (!vTileOutputFree); //we can not continue before the output placebecomes free

                mEngine4Mutex.WaitOne();
                mTileOfEngine4 = new Tile(vTile);
                mTileOfEngine4Ready = true;
                mTileInWorkEngine4 = false;
                if (!mQueueEngine4.IsEmpty())
                {
                    mSignForEngine4.Set();  //sign self to continue
                }
                mEngine4Mutex.ReleaseMutex();

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

        public static Boolean CheckForTileOfEngine1()
        {
            Boolean vThereIsATileFromEngine1;

            mEngine1Mutex.WaitOne();
            vThereIsATileFromEngine1 = mTileOfEngine1Ready;
            mEngine1Mutex.ReleaseMutex();

            return vThereIsATileFromEngine1;
        }
        
        public static Boolean CheckForTileOfEngine2()
        {
            Boolean vThereIsATileFromEngine2;

            mEngine2Mutex.WaitOne();
            vThereIsATileFromEngine2 = mTileOfEngine2Ready;
            mEngine2Mutex.ReleaseMutex();

            return vThereIsATileFromEngine2;
        }

        public static Boolean CheckForTileOfEngine3()
        {
            Boolean vThereIsATileFromEngine3;

            mEngine3Mutex.WaitOne();
            vThereIsATileFromEngine3 = mTileOfEngine3Ready;
            mEngine3Mutex.ReleaseMutex();

            return vThereIsATileFromEngine3;
        }

        public static Boolean CheckForTileOfEngine4()
        {
            Boolean vThereIsATileFromEngine4;

            mEngine4Mutex.WaitOne();
            vThereIsATileFromEngine4 = mTileOfEngine4Ready;
            mEngine4Mutex.ReleaseMutex();

            return vThereIsATileFromEngine4;
        }

        public static Tile GetTileOfEngine1()
        {
            Tile vTileOfEngine1;

            mEngine1Mutex.WaitOne();
            vTileOfEngine1            = new Tile(mTileOfEngine1);
            mTileOfEngine1Ready       = false;
            mEngine1Mutex.ReleaseMutex();

            return vTileOfEngine1;
        }


        public static Tile GetTileOfEngine2()
        {
            Tile vTileOfEngine2;

            mEngine2Mutex.WaitOne();
            vTileOfEngine2 = new Tile(mTileOfEngine2);
            mTileOfEngine2Ready = false;
            mEngine2Mutex.ReleaseMutex();

            return vTileOfEngine2;
        }


        public static Tile GetTileOfEngine3()
        {
            Tile vTileOfEngine3;

            mEngine3Mutex.WaitOne();
            vTileOfEngine3 = new Tile(mTileOfEngine3);
            mTileOfEngine3Ready = false;
            mEngine3Mutex.ReleaseMutex();

            return vTileOfEngine3;
        }


        public static Tile GetTileOfEngine4()
        {
            Tile vTileOfEngine4;

            mEngine4Mutex.WaitOne();
            vTileOfEngine4 = new Tile(mTileOfEngine4);
            mTileOfEngine4Ready = false;
            mEngine4Mutex.ReleaseMutex();

            return vTileOfEngine4;
        }


        public static Int32 GetFreeSpaceOfEngine1()
        {
            Int32 vFreeEngine1;

            mEngine1Mutex.WaitOne();
            vFreeEngine1 = mQueueEngine1.GetFreeSpace();
            mEngine1Mutex.ReleaseMutex();

            return vFreeEngine1;
        }

        public static Int32 GetFreeSpaceOfEngine2()
        {
            Int32 vFreeEngine2;

            mEngine2Mutex.WaitOne();
            vFreeEngine2 = mQueueEngine2.GetFreeSpace();
            mEngine2Mutex.ReleaseMutex();

            return vFreeEngine2;
        }

        public static Int32 GetFreeSpaceOfEngine3()
        {
            Int32 vFreeEngine3;

            mEngine3Mutex.WaitOne();
            vFreeEngine3 = mQueueEngine3.GetFreeSpace();
            mEngine3Mutex.ReleaseMutex();

            return vFreeEngine3;
        }

        public static Int32 GetFreeSpaceOfEngine4()
        {
            Int32 vFreeEngine4;

            mEngine4Mutex.WaitOne();
            vFreeEngine4 = mQueueEngine4.GetFreeSpace();
            mEngine4Mutex.ReleaseMutex();

            return vFreeEngine4;
        }


        public static Int32 AddTileInfoToEngine1(TileInfo iTileInfo)
        {
            Int32 vFreeEngine1;

            mEngine1Mutex.WaitOne();
            mQueueEngine1.AddTileInfoNoDoubles(iTileInfo);
            vFreeEngine1 = mQueueEngine1.GetFreeSpace();
            mSignForEngine1.Set();
            mEngine1Mutex.ReleaseMutex();

            return vFreeEngine1;
        }

        public static Int32 AddTileInfoToEngine2(TileInfo iTileInfo)
        {
            Int32 vFreeEngine2;

            mEngine2Mutex.WaitOne();
            mQueueEngine2.AddTileInfoNoDoubles(iTileInfo);
            vFreeEngine2 = mQueueEngine2.GetFreeSpace();
            mSignForEngine2.Set();
            mEngine2Mutex.ReleaseMutex();

            return vFreeEngine2;
        }

        public static Int32 AddTileInfoToEngine3(TileInfo iTileInfo)
        {
            Int32 vFreeEngine3;

            mEngine3Mutex.WaitOne();
            mQueueEngine3.AddTileInfoNoDoubles(iTileInfo);
            vFreeEngine3 = mQueueEngine3.GetFreeSpace();
            mSignForEngine3.Set();
            mEngine3Mutex.ReleaseMutex();

            return vFreeEngine3;
        }

        public static Int32 AddTileInfoToEngine4(TileInfo iTileInfo)
        {
            Int32 vFreeEngine4;

            mEngine4Mutex.WaitOne();
            mQueueEngine4.AddTileInfoNoDoubles(iTileInfo);
            vFreeEngine4 = mQueueEngine4.GetFreeSpace();
            mSignForEngine4.Set();
            mEngine4Mutex.ReleaseMutex();

            return vFreeEngine4;
        }

        public static void EmptyEngine1Queue()
        {
            mEngine1Mutex.WaitOne();
            mQueueEngine1.DoEmpty();
            mEngine1Mutex.ReleaseMutex();
        }

        public static void EmptyEngine2Queue()
        {
            mEngine2Mutex.WaitOne();
            mQueueEngine2.DoEmpty();
            mEngine2Mutex.ReleaseMutex();
        }
        
        public static void EmptyEngine3Queue()
        {
            mEngine3Mutex.WaitOne();
            mQueueEngine3.DoEmpty();
            mEngine3Mutex.ReleaseMutex();
        }

        public static void EmptyEngine4Queue()
        {
            mEngine4Mutex.WaitOne();
            mQueueEngine4.DoEmpty();
            mEngine4Mutex.ReleaseMutex();
        }

        public static Int32 GetEnginesQueueSize()
        {
            return cEngineQueueSize;
        }

        public static Boolean IsEngine1TileFree()
        {
            Boolean vTileFree = false;

            Int32 vFreeEngine1;

            mEngine1Mutex.WaitOne();
            vFreeEngine1 = mQueueEngine1.GetFreeSpace();
            if ((vFreeEngine1 == cEngineQueueSize) && !mTileInWorkEngine1 && !mTileOfEngine1Ready)
            {
                vTileFree = true;
            } 
            mEngine1Mutex.ReleaseMutex();
            return vTileFree;
        }

        public static Boolean IsEngine2TileFree()
        {
            Boolean vTileFree = false;

            Int32 vFreeEngine2;

            mEngine2Mutex.WaitOne();
            vFreeEngine2 = mQueueEngine2.GetFreeSpace();
            if ((vFreeEngine2 == cEngineQueueSize) && !mTileInWorkEngine2 && !mTileOfEngine2Ready)
            {
                vTileFree = true;
            }
            mEngine2Mutex.ReleaseMutex();
            return vTileFree;
        }

        public static Boolean IsEngine3TileFree()
        {
            Boolean vTileFree = false;

            Int32 vFreeEngine3;

            mEngine3Mutex.WaitOne();
            vFreeEngine3 = mQueueEngine3.GetFreeSpace();
            if ((vFreeEngine3 == cEngineQueueSize) && !mTileInWorkEngine3 && !mTileOfEngine3Ready)
            {
                vTileFree = true;
            }
            mEngine3Mutex.ReleaseMutex();
            return vTileFree;
        }

        public static Boolean IsEngine4TileFree()
        {
            Boolean vTileFree = false;

            Int32 vFreeEngine4;

            mEngine4Mutex.WaitOne();
            vFreeEngine4 = mQueueEngine4.GetFreeSpace();
            if ((vFreeEngine4 == cEngineQueueSize) && !mTileInWorkEngine4 && !mTileOfEngine4Ready)
            {
                vTileFree = true;
            }
            mEngine4Mutex.ReleaseMutex();
            return vTileFree;
        }

    }
}
