using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using FSEarthTilesInternalDLL;

namespace FSEarthTilesDLL
{

    public partial class EarthWebForm : Form
    {

        protected UInt32[,] mBitmapArray;   
        protected GCHandle  mGCHandle;

        Bitmap   mBitmap;
        Graphics mGraphics;
        String   mCookieContent;
        
        TileInfo mTileInfo;
        Tile     mTile;
        Boolean  mTileOrdered;
        Boolean  mTileReady;
        String   mTilePage;

        Boolean   mWaitForRendered;
        Rectangle mRenderRectangle;
        String    mRenderURL;

        Int32     mRenderingWaitCountdown;

        Int32     mSaveCounter;

        Int64    mMaxTicksTillRecover;
        Int64    mTicksCounter;

        Bitmap   mNoTileFound;

        List<String> mWebBrowserNoTileFoundKeyWords;

        Boolean  mPanicMode;

        private Object mWWWLock = new Object();


        public EarthWebForm(Int64 iMaxTicksTillRecover, Bitmap iNoTileFound, String iWebBrowserNoTileFoundKeyWords)
        {
            InitializeComponent();

            mNoTileFound = (Bitmap)iNoTileFound.Clone();

            CreateNoTileFoundKeyWordsList(iWebBrowserNoTileFoundKeyWords);

            mPanicMode = false;

            mCookieContent = "";
            mTileOrdered = false;
            mTileReady = false;
            mTilePage = "";
            mTileInfo = new TileInfo();
            mTile = new Tile();
            mWaitForRendered = false;
            mRenderRectangle = new Rectangle();
            mRenderURL = "";
            mRenderingWaitCountdown = 0;
            mSaveCounter = 0;
            mTicksCounter = 0;
            mMaxTicksTillRecover = iMaxTicksTillRecover;

            mBitmapArray = new UInt32[256, 256]; //Bitmap in Memory is [Y,X]. It does not work the other way!
            mGCHandle = GCHandle.Alloc(mBitmapArray, GCHandleType.Pinned);
            IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mBitmapArray, 0);
            mBitmap = new Bitmap(256, 256, 256 << 2, System.Drawing.Imaging.PixelFormat.Format32bppRgb, vPointer);
            mGraphics = Graphics.FromImage(mBitmap);
        }

       ~EarthWebForm()
        {
            mGraphics.Dispose();
            mBitmap.Dispose();
            mGCHandle.Free();
            mBitmapArray = new UInt32[4, 4];
        }

        public void SetPanicMode()
        {
            lock (mWWWLock)
            {
                String vKeepAddress = WebAddressBox.Text;

                WebAddressBox.Invalidate();
                WebAddressBox.Text = "*** PANIC MODE SET! ***";
                WebAddressBox.Refresh();

                Thread.Sleep(2000);
                mPanicMode = true;

                WebAddressBox.Invalidate();
                WebAddressBox.Text = vKeepAddress;
                WebAddressBox.Refresh();
            }
        }

        public void ResetPanicMode()
        {
            lock (mWWWLock)
            {
                if (mPanicMode)
                {
                    String vKeepAddress = WebAddressBox.Text;

                    WebAddressBox.Invalidate();
                    WebAddressBox.Text = "*** PANIC MODE RESETED! ***";
                    WebAddressBox.Refresh();

                    Thread.Sleep(1000);
                    mPanicMode = false;

                    WebAddressBox.Invalidate();
                    WebAddressBox.Text = vKeepAddress;
                    WebAddressBox.Refresh();
                }
            }
        }

        private void CreateNoTileFoundKeyWordsList(String iWebBrowserNoTileFoundKeyWords)
        {

            mWebBrowserNoTileFoundKeyWords = new List<String>();

            String vWorkString = iWebBrowserNoTileFoundKeyWords.Trim();
            String vPartString;

            Int32 vIndex = 0;

            do
            {
                vIndex = vWorkString.IndexOf(",", StringComparison.CurrentCultureIgnoreCase);
                if (vIndex >= 0)
                {
                    vPartString = vWorkString.Remove(vIndex);
                    vPartString = vPartString.Trim();
                    vWorkString = vWorkString.Substring(vIndex + 1, vWorkString.Length - (vIndex + 1));
                    vWorkString = vWorkString.Trim();
                    if (vPartString.Length >= 0)
                    {
                        mWebBrowserNoTileFoundKeyWords.Add(vPartString);
                    }
                }
                else
                {
                    if (vWorkString.Length > 0)
                    {
                        mWebBrowserNoTileFoundKeyWords.Add(vWorkString);
                    }
                    vWorkString = "";
                }
            } while (vWorkString.Length > 0);
        }


        public void TimerTick()
        {
            lock (mWWWLock)
            {
                if (this.Created)
                {
                    if (mTileOrdered)
                    {
                        mTicksCounter++;
                    }

                    if (mWaitForRendered)
                    {
                        TryToGatherRenderedImage();
                    }

                    if (!mTileReady)
                    {
                        if (mTicksCounter >= mMaxTicksTillRecover)
                        {
                            SetTileRequestFailed();
                        }
                    }
                }
            }
        }

        private void TryToGatherRenderedImage()
        {
            lock (mWWWLock)
            {
                try
                {
                    if (mTileOrdered && (mTilePage == mRenderURL))
                    {
                        Bitmap vTempBitmap = new Bitmap(360, 360);
                        WebBrowser.DrawToBitmap(vTempBitmap, new Rectangle(0, 0, 360, 360));
                        mGraphics.DrawImage(vTempBitmap, new Rectangle(0, 0, 256, 256), mRenderRectangle, GraphicsUnit.Pixel);
                        //mBitmap.Save("D:\\test.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                        //Check for blank Tile (Browser doesn't tells us when the thing has been rendered, that's really a pain, there is no event no attribute nothing..uff)

                        Boolean vIsBlank = true;

                        Boolean vCorrectWorking = true;

                        Color TestColor1 = mBitmap.GetPixel(0, 0);  // For Tests Seems to help that it gets really copied !?
                        UInt32 vValue1 = mBitmapArray[0, 0];

                        if ((vValue1 & 0x00FFFFFF) != ((UInt32)(TestColor1.R << 16) + (UInt32)(TestColor1.G << 8) + (UInt32)(TestColor1.B)))
                        {
                            vCorrectWorking = false;
                            //Thread.Sleep(100); //For debugging
                            //like you can see with this test the Area and the Bitmap becomes sometimes completly disconnected..but why??
                            //mBitmapArray[0, 0] = 0xFF010203;
                            //Color TestColortest = mBitmap.GetPixel(0, 0); 
                            //mBitmap.SetPixel(0, 0, Color.FromArgb(255, 04, 05, 06));
                            //UInt32 vValuetest = mBitmapArray[0, 0];

                        }

                        Color TestColor2 = mBitmap.GetPixel(255, 0);
                        UInt32 vValue2 = mBitmapArray[0, 255];

                        if ((vValue2 & 0x00FFFFFF) != ((UInt32)(TestColor2.R << 16) + (UInt32)(TestColor2.G << 8) + (UInt32)(TestColor2.B)))
                        {
                            vCorrectWorking = false;
                        }

                        Color TestColor3 = mBitmap.GetPixel(0, 255);
                        UInt32 vValue3 = mBitmapArray[255, 0];

                        if ((vValue3 & 0x00FFFFFF) != ((UInt32)(TestColor3.R << 16) + (UInt32)(TestColor3.G << 8) + (UInt32)(TestColor3.B)))
                        {
                            vCorrectWorking = false;
                        }

                        Color TestColor4 = mBitmap.GetPixel(255, 255);
                        UInt32 vValue4 = mBitmapArray[255, 255];

                        if ((vValue4 & 0x00FFFFFF) != ((UInt32)(TestColor4.R << 16) + (UInt32)(TestColor4.G << 8) + (UInt32)(TestColor4.B)))
                        {
                            vCorrectWorking = false;
                        }


                        Color vRefColor = vTempBitmap.GetPixel(0, 0); //usually there is a border check mRenderRectangle

                        if ((TestColor1.R != vRefColor.R) ||
                            (TestColor1.G != vRefColor.G) ||
                            (TestColor1.B != vRefColor.B) ||
                            (TestColor2.R != vRefColor.R) ||
                            (TestColor2.G != vRefColor.G) ||
                            (TestColor2.B != vRefColor.B) ||
                            (TestColor3.R != vRefColor.R) ||
                            (TestColor3.G != vRefColor.G) ||
                            (TestColor3.B != vRefColor.B) ||
                            (TestColor4.R != vRefColor.R) ||
                            (TestColor4.G != vRefColor.G) ||
                            (TestColor4.B != vRefColor.B))
                        {
                            vIsBlank = false;
                        }

                        if (vIsBlank)
                        {
                            if (vCorrectWorking)
                            {
                                UInt32 vRefColorValue = (UInt32)(vRefColor.R << 16) + (UInt32)(vRefColor.G << 8) + (UInt32)(vRefColor.B);
                                UInt32 vValue = 0;

                                for (Int32 vY = 0; vY < 256; vY++)
                                {
                                    for (Int32 vX = 0; vX < 256; vX++)
                                    {
                                        vValue = mBitmapArray[vY, vX];
                                        vValue &= 0x00FFFFFF;
                                        if (vValue != vRefColorValue)
                                        {
                                            vIsBlank = false;
                                            vX = 256; //Abort
                                            vY = 256;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Color ValColor = mBitmap.GetPixel(0, 0);
                                for (Int32 vY = 0; vY < 256; vY++)
                                {
                                    for (Int32 vX = 0; vX < 256; vX++)
                                    {
                                        ValColor = mBitmap.GetPixel(vX, vY);
                                        if ((vRefColor.R != ValColor.R) ||
                                            (vRefColor.G != ValColor.G) ||
                                            (vRefColor.B != ValColor.B))
                                        {
                                            vIsBlank = false;
                                            vX = 256; //Abort
                                            vY = 256;
                                        }
                                    }
                                }
                            }
                        }


                        if (vIsBlank)
                        {
                            //for debugging
                            //vTempBitmap.Save("D:\\test.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                            mRenderingWaitCountdown--;
                            if (mRenderingWaitCountdown > 0)
                            {
                                mWaitForRendered = true;
                                //Application.DoEvents(); stack overflow if you include this..hu!?
                                Thread.Sleep(125); //give Browser a break.. it can't handle the heat and blocks if we bombard it with checks every 50ms.
                                //Application.DoEvents(); stack overflow if you include this..hu!?
                                Focus();
                                Invalidate();
                                Refresh();
                                Thread.Sleep(125);
                            }
                            else
                            {
                                mRenderingWaitCountdown = 0;
                                if (!mPanicMode)
                                {
                                    SetTileRequestFailed();
                                }
                                else
                                {
                                    //Tile is all white but we set it instead NoTileFound. Changes are height that it is a white tile. (happend)
                                    //alternative: SetNoTileFound(); 
                                    mTile.StoreBitmap(mBitmap);
                                    mTile.MarkAsGoodBitmap();
                                    mWaitForRendered = false;
                                    mTileReady = true;
                                    mTileOrdered = false;
                                    mTilePage = "";
                                    mTicksCounter = 0;
                                }
                            }
                        }
                        else
                        {
                            //String vCount = GetFileNameCounter();
                            //mBitmap.Save("D:\\QuickTest\\SourceBmp" + vCount + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);
                            mTile.StoreBitmap(mBitmap);
                            mTile.MarkAsGoodBitmap();
                            mWaitForRendered = false;
                            mTileReady = true;
                            mTileOrdered = false;
                            mTilePage = "";
                            mTicksCounter = 0;
                        }

                        vTempBitmap.Dispose();
                    }
                }
                catch
                {
                    //hopala
                }
            }
        }


        public String GetCookieContent()
        {
            return mCookieContent;
        }

        public void GoToPage(String iPage)
        {
            lock(mWWWLock)
            {

                WebBrowser.Navigate(iPage);

                //bool loadFinished = false;

                //WebBrowser.DocumentCompleted += delegate { loadFinished = true; };

                //WebBrowser.Navigate(iPage);

                //while (!loadFinished)
                //{
                //    Thread.Sleep(100);
                //    Application.DoEvents();
                //}
            }
        }

        private String GetFileNameCounter()
        {
            String vCount = "";
            if (mSaveCounter < 10)
            {
                vCount = "000" + Convert.ToString(mSaveCounter);
            }
            else if (mSaveCounter < 100)
            {
                vCount = "00" + Convert.ToString(mSaveCounter);
            }
            else if (mSaveCounter < 1000)
            {
                vCount = "0" + Convert.ToString(mSaveCounter);
            }
            else
            {
                vCount = Convert.ToString(mSaveCounter);
            }
            return vCount;
        }

        public Tile GetTileOfWWWEngine()
        {
            lock (mWWWLock)
            {
                /* for debug
                if (!mTileReady)
                {
                    Thread.Sleep(3000); 
                }

                String vCount = GetFileNameCounter();

                if (mTile.IsGoodBitmap())
                {
                    mBitmap.Save("D:\\QuickTest\\GoodBmp" + vCount + ".bmp");
                    //mTile.GetBitmapReference().Save("D:\\QuickTest\\GoodTile" + vCount + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                }
                else
                {
                    mBitmap.Save("D:\\QuickTest\\BadBmp" + vCount + ".bmp");
                    //mTile.GetBitmapReference().Save("D:\\QuickTest\\BadTile" + vCount + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                }
                mSaveCounter++;
                */

                mTileReady = false;
                mWaitForRendered = false;
                mTilePage = "";
                return mTile.Clone();
            }
        }

        public Boolean CheckForTileOfWWWEngine()
        {
            lock (mWWWLock)
            {
                return mTileReady;
            }
        }


        public TileInfo GetOrderedTileInfoAndAbortProcess()
        {
            lock (mWWWLock)
            {
                mTileOrdered = false;
                mTileReady = false;
                mWaitForRendered = false;
                mTilePage = "";
                return mTileInfo.Clone();
            }
        }


        public Boolean CheckForTileOrdered()
        {
            lock (mWWWLock)
            {
                return mTileOrdered;
            }
        }

        public Int32 GetFreeSpaceOfWWWEngine()
        {
            lock (mWWWLock)
            {
                Int32 vFreeSpace = 1;
                if ((mTileOrdered) || (mTileReady)) //A ReadyTile has to be fetched first before we can continue
                {
                    vFreeSpace = 0;
                }
                return vFreeSpace;
            }
        }

        public Int32 AddTileInfoToWWWEngine(TileInfo iTileInfo)
        {
            lock(mWWWLock)
            {
                Int32 vFreeSpace = GetFreeSpaceOfWWWEngine();

                if (vFreeSpace > 0)
                {
                    if (this.Created)
                    {
                        mTileInfo    = iTileInfo.Clone();
                        mTile        = new Tile(mTileInfo);
                        String vWebAddress = EarthScriptsHandler.CreateWebAddress(mTileInfo.mAreaCodeX, mTileInfo.mAreaCodeY, mTileInfo.mLevel, mTileInfo.mService);
                        mTilePage    = vWebAddress;
                        mTileOrdered = true;
                        mTileReady   = false;
                        mWaitForRendered = false;
                        mTicksCounter = 0;
                        GoToPage(vWebAddress);
                    }
                }
                return 0; //No more freed space
            }
        }

        private void EarthWebBrowserBox_FileDownload(object sender, EventArgs e)
        {
        }

        private void EarthWebBrowserBox_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {

            //WebBrowser.Invalidate();
            //WebBrowser.Refresh();
            //WebAddressBox.Invalidate();
            //WebAddressBox.Text = e.Url.AbsoluteUri;
            //WebAddressBox.Refresh();

        }

        private void EarthWebBrowserBox_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            //WebAddressBox.Invalidate();
            //WebAddressBox.Text = e.Url.AbsoluteUri;
            //WebAddressBox.Refresh();
        }

 

        private void EarthWebBrowserBox_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
           
            lock (mWWWLock)
            {

                //WebBrowser.Invalidate();
                //WebBrowser.Refresh();
                WebAddressBox.Invalidate();
                WebAddressBox.Text = e.Url.AbsoluteUri;
                WebAddressBox.Refresh();

                //Give Browser time to finish Rendering.. ? 
                //Thread.Sleep(200);

                Boolean vBusy = WebBrowser.IsBusy;
                WebBrowserReadyState vWebBRowserReadyState = WebBrowser.ReadyState;

                if (vBusy || (!(WebBrowserReadyState.Complete == vWebBRowserReadyState)))
                {
                    Thread.Sleep(500); //Never gets into here  ...The Browser don't give me an event or attribute when rendered! :/
                }

                if (mTilePage == e.Url.AbsoluteUri)
                {
                    try
                    {
                        if (mTileOrdered)
                        {
                            if (WebBrowser.Document.Images.Count > 0)
                            {
                                HtmlElement mHTMLElement = WebBrowser.Document.Images[0];

                                if (((mHTMLElement.OffsetRectangle.Width >= 256) && (mHTMLElement.OffsetRectangle.Height >= 256)) &&
                                    ((mHTMLElement.OffsetRectangle.Width <= 258) && (mHTMLElement.OffsetRectangle.Height <= 258)))
                                {
                                    mRenderRectangle = mHTMLElement.OffsetRectangle;
                                    mRenderRectangle.Height = 256; //258 pixel tiles (service3) are in fact 2 pixel overlapping at right bottom so we need to cut this away.
                                    mRenderRectangle.Width = 256;
                                    mRenderURL = e.Url.AbsoluteUri;
                                    mRenderingWaitCountdown = 6;  //make it 10 as soon as we have the faster check methode
                                    TryToGatherRenderedImage();
                                }
                                else
                                {
                                    //The page with the correct URL contains everything else than a Tile picture 
                                    //We have to selective analyse the context to differ between an internal window page (connection broke)
                                    //and one comming from the services that say No Tile Found
                                    //What's missing is a proper error report from the WebBrowser. :/

                                    String vBody = WebBrowser.DocumentText;
                                    //or  String vBody = WebBrowser.Document.Body.InnerHtml;
                                    //or  String vBody = WebBrowser.Document.Body.InnerText;

                                    if (mPanicMode || IsNoTileFoundKeyWordsInBody(vBody))
                                    {
                                        SetNoTileFound();
                                    }
                                    else
                                    {
                                        SetTileRequestFailed();
                                    }

                                }

                            }
                            else
                            {
                                //The page with the correct URL contains no picture at all

                                String vBody = WebBrowser.DocumentText;
                                //or  String vBody = WebBrowser.Document.Body.InnerHtml;
                                //or  String vBody = WebBrowser.Document.Body.InnerText;

                                if (mPanicMode || IsNoTileFoundKeyWordsInBody(vBody))
                                {
                                    SetNoTileFound();
                                }
                                else
                                {
                                    SetTileRequestFailed();
                                }
                            }
                        }
                    }
                    catch
                    {
                        //ops
                    }
                }
            }
        }

        private Boolean IsNoTileFoundKeyWordsInBody(String iBody)
        {
            Boolean vKeyWordFound = false;

            foreach (String vKeyWord in mWebBrowserNoTileFoundKeyWords)
            {
                if (EarthCommon.StringContains(iBody,vKeyWord))
                {
                    vKeyWordFound = true;
                    break;
                }
            }

            return vKeyWordFound;
        }

        private void SetTileRequestFailed()
        {
            lock (mWWWLock)
            {
                Bitmap vBitmap = new Bitmap(4, 4);
                mTile.StoreBitmap(vBitmap);
                mTile.MarkAsBadBitmap();
                mWaitForRendered = false;
                mTileReady = true;
                mTileOrdered = false;
                mTilePage = "";
                mTicksCounter = 0;
            }
         }

        private void SetNoTileFound()
        {
            lock (mWWWLock)
            {
                mTile.StoreBitmap(mNoTileFound);
                mTile.MarkAsGoodBitmap();
                mWaitForRendered = false;
                mTileReady = true;
                mTileOrdered = false;
                mTilePage = "";
                mTicksCounter = 0;
            }
        }


        private void WebAddressBox_TextChanged(object sender, EventArgs e)
        {
        }

        private void WebAddressBox_KeyUp(object sender, KeyEventArgs e)
        {
            lock (mWWWLock)
            {
                if (e.KeyCode == Keys.Return)
                {
                    GoToPage(WebAddressBox.Text);
                }
            }
        }


        private void CookieTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //not used
        }

        private void EarthWebForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void EarthWebForm_Shown(object sender, EventArgs e)
        {

        }
    }
}