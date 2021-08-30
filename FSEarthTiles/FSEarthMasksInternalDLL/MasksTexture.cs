using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using FSEarthTilesInternalDLL;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;



//  C# . NET 24Bit RGB Format in Memory
//  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
//

namespace FSEarthMasksInternalDLL
{


    public class MasksTexture
    {
        const Boolean cUseSliceMethode = true;  //true = Use Slice Methode for WaterValue calculations in the transitions for speeding up
        const Boolean cUseCellsMethode = true;  //true = Use Cells Methode for WaterValue calculations in the transitions for speeding up, precondition cUseSliceMethode = true
        const Int32   cYSliceSpace = 300;       //ever cYSliceSpace rows put together a new Vector List valid for the next slice of cYSliceSpace rows
        const Int32   cXCellsSpace = 300;       //ever cXCellsSpace Coloumn put together a new Vector List valid for the next cell of cXCellsSpace columns in the cYSliceSpace rows

        const UInt32 cVectorCommandColorValue     = 255; //a Value of 0 means Empty or Free
        const UInt32 cWaterCommandColorValue      = 200;
        const UInt32 cTransitionCommandColorValue = 150;
        const UInt32 cLandCommandColorValue       = 100;

        const UInt32 cPoolWaterCommandColorValue  = 190;
        const UInt32 cPoolLandCommandColorValue   = 140;
        const UInt32 cPoolBlendCommandColorValue  =  90;

        public enum tWorkCommandCommand  //8Bit Code Field in groups of 2 Bits: | Pools | BlendTrans | WaterTwoTrans | WaterTrans |
        {
            eEmpty                           = 0x00,
            eWaterTransitionWater            = 0x01,
            eWaterTransitionTransition       = 0x02,
            eWaterTransitionLand             = 0x03,
            eWater2TransitionWater           = 0x04,
            eWater2TransitionTransition      = 0x08,
            eWater2TransitionLand            = 0x0C,
            eBlendTransitionFullyTransparent = 0x10,
            eBlendTransitionTransition       = 0x20,
            eBlendTransitionSolid            = 0x30,
            eFixWater                        = 0x40,
            eFixLand                         = 0x80,
            eFixBlend                        = 0xC0
        }

        public struct tTrippleSTransisitonConstants
        {
            //Linear Mapping function x' = ax*x+bx, y' = ay*y+by for rescaling/norming the 3 S functions to 0.0 ... 1.0 for X and Y values
            public Double mLinearMapAEntryx;
            public Double mLinearMapBEntryx;
            public Double mLinearMapAEntryy;
            public Double mLinearMapBEntryy;

            public Double mLinearMapATransitionx;
            public Double mLinearMapBTransitionx;
            public Double mLinearMapATransitiony;
            public Double mLinearMapBTransitiony;

            public Double mLinearMapAExitx;
            public Double mLinearMapBExitx;
            public Double mLinearMapAExity;
            public Double mLinearMapBExity;

            //S function Factors y = a*x^n + b*x
            public Double mSACoastFirstHalf;
            public Double mSACoastSecondHalf;
            public Double mSBCoastFirstHalf;
            public Double mSBCoastSecondHalf;
            public Double mSSlopeCoastMiddle;
            public Double mSATransitionFirstHalf;
            public Double mSATransitionSecondHalf;
            public Double mSBTransitionFirstHalf;
            public Double mSBTransitionSecondHalf;
            public Double mSSlopeTransitionMiddle;
            public Double mSADeepWaterFirstHalf;
            public Double mSADeepWaterSecondHalf;
            public Double mSBDeepWaterFirstHalf;
            public Double mSBDeepWaterSecondHalf;
            public Double mSSlopeDeepWaterMiddle;

            public Single mEntryDistanceLimit;
            public Single mEntryDistanceLimitSquare;
            public Single mEntryDistanceDistancePoint1Square;
            public Single mEntryDistanceLimitPoint1y;
            public Single mEntryDistanceLimitMaxSquare;

        }

        //Water Transition Table Size
        const UInt32 cWaterTransitionTableSize     = 10001; // [0] -> x = 0.0; [10000] -> x = 1.0;

        //Direct Area Bitmap Data Bitmap Array
        protected Int32    mArrayWidth;                                 //mArrayWidth = count of 32Bit Integer in one Bitmap line (BitmapFormat RGB 24Bit)
        protected Int32    mArrayHeight;                                //equal Bitmap height Y
        protected Int32    mPixelCountInX;
        protected Int32    mPixelCountInY;
        protected Boolean  mSourceBitmapLoaded;
        protected Boolean  mPixelInfoInited;


        //We have to work with 3 Bitmaps..theres should never be more than 2 Bitmaps in Memory at any time.
        // I would like to reduce it to 1 Bitmap ata  time, maybe 1 Bitmap + 1 grey /command mask but there are several limits
        // with the C# .net graphics makes this not really possible.

        //Original Bitmap that becomes loaded (no more used in v0.9)
        //protected Bitmap    mSourceBitmap;                          //The original unaltered Area Bitmap that will be loaded
                                                                      //This one can not be processed since you can not asign an usable Array in afterwart
       
        //Bitmap operating mode
        protected Boolean   mSource32BitBitmapMode;                         //Bitmap Mode (true = 32Bit, false = 24Bit)
        protected Boolean   mDestination32BitBitmapMode;                         //Bitmap Mode (true = 32Bit, false = 24Bit)

        //Work Bitmap in it the processing takes place
        public Bitmap    mAreaBitmap;                               //The Bitmap which become initialized and accessed with mAreaBitmapArray
        protected UInt32[,] mAreaBitmapArray;                          //RGB 24Bit / 32Bit Bitmap Data Array
        protected Graphics  mAreaGraphics;                             //The Graphis to Draw into the Bitmap
        protected GCHandle  mGCHandle;                                 //Required stuff to make that direct access Bitmap with Array working
        protected Boolean   mWorkMemoryAllocated;                      //true when Memory is allocated
        protected Byte[,]   mWorkCommandArray;                         //8 Bit commando code area. Allocated with WorkBitmapAllocation  

        //(no more uesed in v0.9) BackUp Bitmap this is used as a storage. for multiy stage processsing (forexampel Land & Water = processing1, Blend=processing2)
        protected Bitmap    mBackUpAreaBitmap;                         //The Bitmap which become initialized and accessed with mAreaBitmapArray
        protected UInt32[,] mBackUpAreaBitmapArray;                    //RGB 24Bit / 32Bit Bitmap Data Array  
        protected Graphics  mBackUpAreaGraphics;                       //The Graphis to Draw into the Bitmap
        protected GCHandle  mBackUpGCHandle;                           //Required stuff to make that direct access Bitmap with Array working
        protected Boolean   mBackUpMemoryAllocated;                    //true when Memory is allocated

        protected Random     mRandomGenerator;
        protected UInt32[]   mColorCorrectionTable;
        protected UInt32[,]  mPoolColorCorrectionTable;
        protected UInt32[,,] mTransitionColorCorrectionTable;

        //Tripple S Transitions  3D Array [,,] [tTransitionType,tTransitionSubType,cWaterTransitionTableSize];
        protected Single [,,] mTrippleSTableDistanceDirect;
        protected Single [,,] mTrippleSTableSquareDirect;
        protected Single[, ,] mTrippleSTableDistanceDirectStreched;
        protected Single[, ,] mTrippleSTableSquareDirectStreched;
        protected tTrippleSTransisitonConstants[,] mTrippleSTransitionConstants;
        protected Single [,] mTrippleSTransitionExitValues;
        protected Boolean[,] mTrippleSTransitionActive;


        protected List<tLine>[,] mCellsCoastLines;        //[Pixel]  Slice set used for a fix number of rows (say 100)and becomes recreated every this 100.
        protected List<tLine>[,] mCellsDeepWaterLines;    //[Pixel]  Only for MinDistance calculation usable
        protected List<tPoint>[,] mCellsCoastPoints;       //[Pixel]
        protected List<tPoint>[,] mCellsDeepWaterPoints;   //[Pixel]

        public MasksTexture()
        {
            mSourceBitmapLoaded = false;
            mWorkMemoryAllocated = false;
            mBackUpMemoryAllocated = false;
            mPixelInfoInited = false;
            mSource32BitBitmapMode = false;
            mDestination32BitBitmapMode = false;
            mRandomGenerator = new Random();

            mTrippleSTableDistanceDirect  = new Single[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize), cWaterTransitionTableSize];
            mTrippleSTableSquareDirect    = new Single[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize), cWaterTransitionTableSize];
            mTrippleSTableDistanceDirectStreched = new Single[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize), cWaterTransitionTableSize];
            mTrippleSTableSquareDirectStreched = new Single[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize), cWaterTransitionTableSize]; 
            mTrippleSTransitionConstants  = new tTrippleSTransisitonConstants[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize)];
            mTrippleSTransitionExitValues = new Single[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize)];
            mTrippleSTransitionActive = new Boolean[(UInt32)(MasksConfig.tTransitionType.eSize), (UInt32)(MasksConfig.tTransitionSubType.eSize)]; 
       
            mCellsCoastLines      = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize),1];   //[Pixel]  Slice set used for a fix number of rows (say 100)and becomes recreated every this 100.
            mCellsDeepWaterLines  = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize),1];   //[Pixel]  Only for MinDistance calculation usable
            mCellsCoastPoints     = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize),1];   //[Pixel]
            mCellsDeepWaterPoints = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize),1];   //[Pixel]
            
            mColorCorrectionTable = new UInt32[256];
            mPoolColorCorrectionTable = new UInt32[(Int32)(MasksConfig.tPoolType.eSize), 256];
            mTransitionColorCorrectionTable = new UInt32[(Int32)(MasksConfig.tTransitionType.eSize),101,256];
        }

        private void InitializeCells()
        {
            if (cUseCellsMethode)
            {
                Int32 vCells = (mPixelCountInX / cXCellsSpace) + 1;

                mCellsCoastLines = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize), vCells];
                mCellsDeepWaterLines = new List<tLine>[(Int32)(MasksConfig.tTransitionType.eSize), vCells];
                mCellsCoastPoints = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize), vCells];
                mCellsDeepWaterPoints = new List<tPoint>[(Int32)(MasksConfig.tTransitionType.eSize), vCells];

                for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
                {
                    for (Int32 vCell = 0; vCell < vCells; vCell++)
                    {
                        mCellsCoastLines[vTransitionType, vCell]      = new List<tLine>();
                        mCellsDeepWaterLines[vTransitionType, vCell]  = new List<tLine>();
                        mCellsCoastPoints[vTransitionType, vCell]     = new List<tPoint>();
                        mCellsDeepWaterPoints[vTransitionType, vCell] = new List<tPoint>();
                    }
                }
            }
        }

        public Boolean AllocateWorkTextureMemory()
        {

            FreeWorkTextureMemory(); 
            DoAllocateWorkTextureMemory();
            return mWorkMemoryAllocated;
        }

        public Boolean AllocateBackUpTextureMemory()
        {

            FreeBackUpTextureMemory();
            DoAllocateBackUpTextureMemory();
            return mBackUpMemoryAllocated;
        }

        /* Don't use. Doesnt works correct with 32BitBitmap
        public void LoadSourceBitmap()
        {
            DoLoadSourceBitmap();
            MasksCommon.CollectGarbage();
        }
        */

        public void SetPixelInfo(Int32 iPixelCountInX, Int32 iPixelCountInY)
        {
            mPixelCountInX = iPixelCountInX;
            mPixelCountInY = iPixelCountInY;
            mPixelInfoInited = true;
            InitializeCells();
        }

        public void LoadSourceBitmapInfo(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            try
            {
                if (File.Exists(MasksConfig.mAreaSourceBitmapFile))
                {
                    FileStream   vFileStream = new FileStream(MasksConfig.mAreaSourceBitmapFile, FileMode.Open, FileAccess.Read);
                    BinaryReader vBinaryReader = new BinaryReader(vFileStream);

                    try
                    {
                        //we could use vBinaryReader.PeekChar() != -1 to check if there are still chars left
                        //But we know what we read don't we? ;)

                        //BitmapFormat info is from here
                        //http://www.fortunecity.com/skyscraper/windows/364/bmpffrmt.html

                        //BitmapFileHeader
                        Int16 vBfType = vBinaryReader.ReadInt16();
                        if (vBfType == 19778)  //vBfType = "BM" ?
                        {
                            Int32 vBfSize      = vBinaryReader.ReadInt32();
                            Int16 vBfReserved1 = vBinaryReader.ReadInt16();
                            Int16 vBfReserved2 = vBinaryReader.ReadInt16();
                            Int32 vBfOffBits   = vBinaryReader.ReadInt32();
                            
                            //BitmapInfoHeader
                            Int32 vBiSize   = vBinaryReader.ReadInt32();
                            Int32 vBiWidth  = vBinaryReader.ReadInt32();
                            Int32 vBiHeight = vBinaryReader.ReadInt32();
                            Int16 vBiPlanes = vBinaryReader.ReadInt16();
                            Int16 vBiCount  = vBinaryReader.ReadInt16();
                            Int32 vBiCompression = vBinaryReader.ReadInt32();

                            if (vBiCompression == 0)
                            {

                                if ((vBiCount >= 24) && (vBiCount <= 32))
                                {
                                    SetPixelInfo(vBiWidth, vBiHeight);
                                    if (vBiCount == 32)
                                    {
                                        mSource32BitBitmapMode = true;
                                    }
                                    else
                                    {
                                        mSource32BitBitmapMode = false;
                                    }
                                    
                                    mDestination32BitBitmapMode = false; //Destination is always 24 Bit except FS2004..

                                    if (MasksConfig.mCreateFS2004MasksInsteadFSXMasks) //if fs2004
                                    {
                                        if (MasksConfig.mCreateWaterMaskBitmap || mSource32BitBitmapMode)
                                        {
                                            mDestination32BitBitmapMode = true; //if create water or there exists already a water channel.
                                        }
                                    }

                                }
                                else
                                {
                                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Source not a 24 or 32 Bit Bitmap!");
                                    Thread.Sleep(3000);
                                }
                            }
                            else
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error can not gandle compressed Bitmaps!");
                                Thread.Sleep(3000);
                            }
                        }
                        else
                        {
                            iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Source Texture File is not a Bitmap!");
                            Thread.Sleep(3000);
                        }

                    }
                    catch(EndOfStreamException)
                    {
                        iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error unexpected End Of File: LoadSourceBitmapInfo");
                        Thread.Sleep(3000);
                    }

                    vBinaryReader.Close();
                    vFileStream.Close();
                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Source Texture File does not exist! Filename:" + MasksConfig.mAreaSourceBitmapFile);
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Code or Memory error catch in: LoadSourceBitmapInfo");
                Thread.Sleep(3000);
            }
        }

        public void LoadSourceBitmapFileIntoWorkBitmap(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            try
            {
                if (mPixelInfoInited)
                {
                    if (File.Exists(MasksConfig.mAreaSourceBitmapFile))
                    {
                        FileStream vFileStream = new FileStream(MasksConfig.mAreaSourceBitmapFile, FileMode.Open, FileAccess.Read);
                        BinaryReader vBinaryReader = new BinaryReader(vFileStream);

                        try
                        {
                            //we could use vBinaryReader.PeekChar() != -1 to check if there are still chars left
                            //But we know what we read don't we? ;)

                            //BitmapFormat info is from here
                            //http://www.fortunecity.com/skyscraper/windows/364/bmpffrmt.html

                            //BitmapFileHeader
                            Int16 vBfType = vBinaryReader.ReadInt16();
                            if (vBfType == 19778)  //vBfType = "BM" ?
                            {
                                Int32 vBfSize = vBinaryReader.ReadInt32();
                                Int16 vBfReserved1 = vBinaryReader.ReadInt16();
                                Int16 vBfReserved2 = vBinaryReader.ReadInt16();
                                Int32 vBfOffBits = vBinaryReader.ReadInt32();

                                //Skip the rest of the Header we want the datas now.
                                Int32 vNrOfBytesToSkip = vBfOffBits - 14; //14 BYtes we already read in                               
                                for (Int32 vSkipBytesCounter = 1; vSkipBytesCounter <= vNrOfBytesToSkip; vSkipBytesCounter++ )
                                {
                                    Byte vByte = vBinaryReader.ReadByte();
                                }

                                //And here we have the Datas!
                                //Bottom up of course
                                try
                                {
                                    if ((mSource32BitBitmapMode) && (mDestination32BitBitmapMode))
                                    {
                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {
                                            for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                                            {
                                                UInt32 vValue = vBinaryReader.ReadUInt32();
                                                mAreaBitmapArray[vY, vX] = vValue;
                                            }
                                        }
                                    }
                                    else if ((!mSource32BitBitmapMode) && (!mDestination32BitBitmapMode))
                                    {

                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {

                                            for (Int32 vX = 0; vX < mArrayWidth; vX ++)
                                            {
                                                // 24Bit Bitmap File and C# .Net share the same Pixel color arrangement:
                                                // B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
                                                // now I know at least where C# has this ugly format from
                                                // from the file! so we can file it in 1:1
                                                UInt32 vValue = vBinaryReader.ReadUInt32();
                                                mAreaBitmapArray[vY, vX] = vValue;   
                                            }
                                        }
                                    }
                                    else if ((mSource32BitBitmapMode) && (!mDestination32BitBitmapMode))
                                    {
                                        Int32 vPixelInQuadIndex;
                                        Int32 vUInt32IndexModal96;

                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {
                                           vPixelInQuadIndex = 0;
                                           vUInt32IndexModal96 = 0;

                                            for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                                            {
                                                UInt32 vValue = vBinaryReader.ReadUInt32();
                                                SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                                                
                                                vPixelInQuadIndex++;
                                                if (vPixelInQuadIndex >= 4)
                                                {
                                                   vPixelInQuadIndex = 0;
                                                   vUInt32IndexModal96 += 3;
                                                }

                                            }
                                        }
                                    }
                                    else // ((!mSource32BitBitmapMode) && (mDestination32BitBitmapMode))
                                    {
                                        Int32 vArrayWidth = (3 * mPixelCountInX) >> 2;
                                        if (((3 * mPixelCountInX) % 4) != 0)
                                        {
                                            vArrayWidth += 1;
                                        }

                                        Int32  vPixelInQuad = 0;
                                        Int32  vX32Bitmap = 0;
                                        UInt32 vKeepValue = 0;
                                        UInt32 vPixelValue = 0;

                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {
                                           vX32Bitmap   = 0;
                                           vPixelInQuad = 0;

                                            for (Int32 vX = 0; vX < vArrayWidth; vX++)
                                            {
                                               
                                                vPixelInQuad++;

                                                // 24Bit Bitmap File and C# .Net share the same Pixel color arrangement:
                                                // B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
                                                // now I know at least where C# has this ugly format from
                                                // from the file! so we can file it in 1:1
                                                UInt32 vValue = vBinaryReader.ReadUInt32();

                                                if (vPixelInQuad==1)
                                                {
                                                    vPixelValue = 0xFF000000 | (0x00FFFFFF & vValue);
                                                    mAreaBitmapArray[vY, vX32Bitmap] = vPixelValue;
                                                    vKeepValue = vValue;
                                                }
                                                else if (vPixelInQuad == 2)
                                                {
                                                    vPixelValue = 0xFF000000 | ((0xFF000000 & vKeepValue) >> 24) | ((0x0000FFFF & vValue) << 8);
                                                    mAreaBitmapArray[vY, vX32Bitmap] = vPixelValue;
                                                    vKeepValue = vValue;
                                                }
                                                else if (vPixelInQuad == 3)
                                                {
                                                    vPixelValue = 0xFF000000 | ((0xFFFF0000 & vKeepValue) >> 16) | ((0x000000FF & vValue) << 16);
                                                    mAreaBitmapArray[vY, vX32Bitmap] = vPixelValue;
                                                    vX32Bitmap++;
                                                    if (vX32Bitmap < mPixelCountInX)
                                                    {
                                                        vPixelValue = 0xFF000000 | ((0xFFFFFF00 & vValue) >> 8);
                                                        mAreaBitmapArray[vY, vX32Bitmap] = vPixelValue;
                                                    }
                                                    vPixelInQuad = 0;
                                                } 
                                                vX32Bitmap++;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error in Reading in Source Bitmap Pxiel Datas!");
                                    Thread.Sleep(3000);
                                }

                            }
                            else
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Source Texture File is not a Bitmap!");
                                Thread.Sleep(3000);
                            }

                        }
                        catch (EndOfStreamException)
                        {
                            iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error unexpected End Of File: LoadSourceBitmapFileIntoWorkBitmap");
                            Thread.Sleep(3000);
                        }

                        vBinaryReader.Close();
                        vFileStream.Close();
                    }
                    else
                    {
                        iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Source Texture File does not exist! Filename:" + MasksConfig.mAreaSourceBitmapFile);
                        Thread.Sleep(3000);
                    }
                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("SourceFileBitmapInfo unknown");
                    Thread.Sleep(3000);
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Code or Memory error catch in: LoadSourceBitmapFileIntoWorkBitmap");
                Thread.Sleep(3000);
            }
        }


        public void LoadSummerBitmapFileIntoWorkBitmap(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            try
            {
                if (mPixelInfoInited)
                {
                    if (File.Exists(MasksConfig.mAreaSummerBitmapFile))
                    {
                        FileStream vFileStream = new FileStream(MasksConfig.mAreaSummerBitmapFile, FileMode.Open, FileAccess.Read);
                        BinaryReader vBinaryReader = new BinaryReader(vFileStream);

                        try
                        {
                            //we could use vBinaryReader.PeekChar() != -1 to check if there are still chars left
                            //But we know what we read don't we? ;)

                            //BitmapFormat info is from here
                            //http://www.fortunecity.com/skyscraper/windows/364/bmpffrmt.html

                            //BitmapFileHeader
                            Int16 vBfType = vBinaryReader.ReadInt16();
                            if (vBfType == 19778)  //vBfType = "BM" ?
                            {
                                Int32 vBfSize = vBinaryReader.ReadInt32();
                                Int16 vBfReserved1 = vBinaryReader.ReadInt16();
                                Int16 vBfReserved2 = vBinaryReader.ReadInt16();
                                Int32 vBfOffBits = vBinaryReader.ReadInt32();

                                //Skip the rest of the Header we want the datas now.
                                Int32 vNrOfBytesToSkip = vBfOffBits - 14; //14 BYtes we already read in                               
                                for (Int32 vSkipBytesCounter = 1; vSkipBytesCounter <= vNrOfBytesToSkip; vSkipBytesCounter++)
                                {
                                    Byte vByte = vBinaryReader.ReadByte();
                                }

                                //And here we have the Datas!
                                //Bottom up of course
                                try
                                {
                                    if (mDestination32BitBitmapMode)
                                    {
                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {
                                            for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                                            {
                                                UInt32 vValue = vBinaryReader.ReadUInt32();
                                                mAreaBitmapArray[vY, vX] = vValue;
                                            }
                                        }
                                    }
                                    else
                                    {

                                        for (Int32 vY = (mPixelCountInY - 1); vY >= 0; vY--)
                                        {

                                            for (Int32 vX = 0; vX < mArrayWidth; vX++)
                                            {
                                                // 24Bit Bitmap File and C# .Net share the same Pixel color arrangement:
                                                // B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
                                                // now I know at least where C# has this ugly format from
                                                UInt32 vValue = vBinaryReader.ReadUInt32();
                                                mAreaBitmapArray[vY, vX] = vValue;
                                            }
                                        }

                                    }
                                }
                                catch
                                {
                                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error in Reading in Summer Bitmap Pixel Datas!");
                                    Thread.Sleep(3000);
                                }

                            }
                            else
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Summer Texture File is not a Bitmap!");
                                Thread.Sleep(3000);
                            }

                        }
                        catch (EndOfStreamException)
                        {
                            iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error unexpected End Of File: LoadSummerBitmapFileIntoWorkBitmap");
                            Thread.Sleep(3000);
                        }

                        vBinaryReader.Close();
                        vFileStream.Close();
                    }
                    else
                    {
                        iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Error Summer Texture File does not exist! Filename:" + MasksConfig.mAreaSummerBitmapFile);
                        Thread.Sleep(3000);
                    }
                }
                else
                {
                    iFSEarthMasksInternalInterface.SetStatusFromFriendThread("SummerFileBitmapInfo unknown");
                    Thread.Sleep(3000);
                }
            }
            catch
            {
                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Code or Memory error catch in: LoadSummerBitmapFileIntoWorkBitmap");
                Thread.Sleep(3000);
            }
        }

        //Dont use! Doesn't work correct with 32bit therefore we wrote an own bmp loader
        /*
        private void DoLoadSourceBitmap()
        {
            try
            {
               mSourceBitmap = new Bitmap(MasksConfig.mAreaSourceBitmapFile);
               mPixelCountInX = mSourceBitmap.Width;
               mPixelCountInY = mSourceBitmap.Height;

               mSource32BitBitmapMode = false;

               if ((mSourceBitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppRgb)    ||
                   (mSourceBitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)   ||
                   (mSourceBitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
               {
                   //32Bit Bitmap ReadIn creates Format32bppRgb. The Alfa Channel is not possibel to read out anymore
                   //Therefore we dont work with 32Bitmap
                   //old m32BitBitmapMode = true;
                   mSource32BitBitmapMode = false;
               }

               if (MasksConfig.mCreateFS2004MasksInsteadFSXMasks && MasksConfig.mCreateWaterMaskBitmap)
               {
                   mDestination32BitBitmapMode = true;
               }
               else
               {
                   mDestination32BitBitmapMode = false;
               }
               mSourceBitmapLoaded = true;
               mPixelInfoInited    = true;
               InitializeCells();
            }
            catch
            {
                //Ops..and now!?
            }
        }
        */

        /*
        public void UnloadSourceBitmap()
        {
            DoUnloadSourceBitmap();
            MasksCommon.CollectGarbage();
        }
        */

        /*
        private void DoUnloadSourceBitmap()
        {
            try
            {
                mSourceBitmap.Dispose();
                mSourceBitmapLoaded = false;
            }
            catch
            {
                //Ops..and now!?
            }
        }
        */

        public void SaveOriginalBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaSourceBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
                if (mDestination32BitBitmapMode)
                {
                    mSource32BitBitmapMode = true; //We overwrite the Source -> from now on we only have 32 BitBitmaps.
                }
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }

            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskSummerBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaSummerBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }


        public void SaveAreaMaskBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaMaskBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskBitmap(Bitmap bm)
        {
            try
            {
                bm.Save(MasksConfig.mAreaMaskBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskAutumnBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaAutumnBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskSpringBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaSpringBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskWinterBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaWinterBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskHardWinterBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaHardWinterBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveAreaMaskNightBitmap()
        {
            try
            {
                mAreaBitmap.Save(MasksConfig.mAreaNightBitmapFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        public void SaveBackUpBitmapForDebugging() //ForTesting purpose only
        {
            try
            {
                mBackUpAreaBitmap.Save("D:\\FSEarthTiles\\work\\BackUpBitmap.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not save Bitmap! HardDiskFull?");
                Thread.Sleep(2000); //give user time after ok to react
            }
            MasksCommon.CollectGarbage();
        }

        private void DoAllocateWorkTextureMemory()
        {

            mAreaBitmap = new Bitmap(4, 4);                //initialize to avoid compiler warning
            mAreaGraphics = Graphics.FromImage(mAreaBitmap);
            mAreaBitmapArray = new UInt32[4, 4];
            mGCHandle = GCHandle.Alloc(mAreaBitmapArray);
                  
            try
            {
                mWorkCommandArray = new Byte[mPixelCountInY, mPixelCountInX]; //Allocate the Commando Array (we keep Y,X order as Bitmap so its simular)

                mArrayWidth = (3 * mPixelCountInX) >> 2;

                if (((3 * mPixelCountInX) % 4) != 0)
                {
                    mArrayWidth += 1;
                }

                if (mDestination32BitBitmapMode)
                {
                    mArrayWidth = mPixelCountInX;
                }

                mArrayHeight = mPixelCountInY;

                mGCHandle.Free();
                mAreaBitmapArray = new UInt32[mArrayHeight, mArrayWidth];  //Bitmap in Memory is [Y,X]. It does not work the other way!

                mGCHandle = GCHandle.Alloc(mAreaBitmapArray, GCHandleType.Pinned);
                IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mAreaBitmapArray, 0);

                if (mDestination32BitBitmapMode)
                {
                    mAreaBitmap = new Bitmap(mPixelCountInX, mPixelCountInY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb, vPointer);
                }
                else
                {
                    mAreaBitmap = new Bitmap(mPixelCountInX, mPixelCountInY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
                }
                mAreaGraphics = Graphics.FromImage(mAreaBitmap);

                mWorkMemoryAllocated = true;
            }
            catch
            {
                mWorkMemoryAllocated = false;
            }

        }


        private void DoAllocateBackUpTextureMemory()
        {

            mBackUpAreaBitmap = new Bitmap(4, 4);                //initialize to avoid compiler warning
            mBackUpAreaGraphics = Graphics.FromImage(mBackUpAreaBitmap);
            mBackUpAreaBitmapArray = new UInt32[4, 4];
            mBackUpGCHandle = GCHandle.Alloc(mBackUpAreaBitmapArray);


            try
            {
                mArrayWidth = (3 * mPixelCountInX) >> 2;

                if (((3 * mPixelCountInX) % 4) != 0)
                {
                    mArrayWidth += 1;
                }

                if (mDestination32BitBitmapMode)
                {
                    mArrayWidth = mPixelCountInX;
                }

                mArrayHeight = mPixelCountInY;

                mBackUpGCHandle.Free();
                mBackUpAreaBitmapArray = new UInt32[mArrayHeight, mArrayWidth];  //Bitmap in Memory is [Y,X]. It does not work the other way!

                mBackUpGCHandle = GCHandle.Alloc(mBackUpAreaBitmapArray, GCHandleType.Pinned);
                IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(mBackUpAreaBitmapArray, 0);

                if (mDestination32BitBitmapMode)
                {
                    mBackUpAreaBitmap = new Bitmap(mPixelCountInX, mPixelCountInY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb, vPointer);
                }
                else
                {
                    mBackUpAreaBitmap = new Bitmap(mPixelCountInX, mPixelCountInY, mArrayWidth << 2, System.Drawing.Imaging.PixelFormat.Format24bppRgb, vPointer);
                }
                mBackUpAreaGraphics = Graphics.FromImage(mBackUpAreaBitmap);

                mBackUpMemoryAllocated = true;
            }
            catch
            {
                mBackUpMemoryAllocated = false;
            }

        }


        public void FreeWorkTextureMemory()
        {
            DoFreeWorkTextureMemory();
            MasksCommon.CollectGarbage();
        }


        private void DoFreeWorkTextureMemory()
        {
            if (mWorkMemoryAllocated)
            {
                mAreaGraphics.Dispose();
                mAreaBitmap.Dispose();
                mGCHandle.Free();
                mAreaBitmapArray  = new UInt32[4, 4];
                mWorkCommandArray = new Byte[4, 4];
                mWorkMemoryAllocated = false;
            }
        }


        public void FreeBackUpTextureMemory()
        {
            DoFreeBackUpTextureMemory();
            MasksCommon.CollectGarbage();
        }


        private void DoFreeBackUpTextureMemory()
        {
            if (mBackUpMemoryAllocated)
            {
                mBackUpAreaGraphics.Dispose();
                mBackUpAreaBitmap.Dispose();
                mBackUpGCHandle.Free();
                mBackUpAreaBitmapArray = new UInt32[4, 4];
                mBackUpMemoryAllocated = false;
            }
        }

        /*
        public void CopyLoadedBitmapIntoBitmapWorkArray()
        {
            if (mSourceBitmapLoaded && mWorkMemoryAllocated)
            {
                mAreaGraphics.DrawImage(mSourceBitmap, new Point(0, 0));
            }
        }
        */

        /*
        public void CopyLoadedBitmapIntoBitmapBackUpArray()
        {
            if (mSourceBitmapLoaded && mBackUpMemoryAllocated)
            {
                mBackUpAreaGraphics.DrawImage(mSourceBitmap, new Point(0, 0));
            }
        }
        */

        public void InverseWorkBitmap()
        {

            if (mWorkMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {
                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                            vValue = (vValue ^ 0xFFFFFFFF) & 0xFFFFFFFF;
                            mAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            vValue = (vValue ^ 0xFFFFFFFF) & 0x00FFFFFF;
                            SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }
                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void ClearWorkMaskBitmap() //Make It Black but don't alter alpha channel
        {
            if (mWorkMemoryAllocated)
            {
                for (Int32 vY = 0; vY < mArrayHeight; vY++)
                {
                    for (Int32 vX = 0; vX < mArrayWidth; vX++)
                    {
                        if (mDestination32BitBitmapMode)
                        {
                            mAreaBitmapArray[vY, vX] = (mAreaBitmapArray[vY, vX] & 0xFF000000);
                        }
                        else
                        {
                            mAreaBitmapArray[vY, vX] = 0x00000000;
                        }
                    }
                }
            }
        }

        public void GreeningWorkBitmap()
        {
            if (mWorkMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {

                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                        }

                        vValue = ((vValue & 0x00FF0000) >> 16) + ((vValue & 0x0000FF00) >> 8) + ((vValue & 0x000000FF));
                        vValue = vValue / 3;
                        //vValue = vValue >> 2; //for  test make more black
                        vValue = vValue << 8; //shift to green position

                        if (mDestination32BitBitmapMode)
                        {
                            mAreaBitmapArray[vY, vX] = (mAreaBitmapArray[vY, vX] & 0xFF000000) | (vValue & 0x00FFFFFF);
                        }
                        else
                        {
                            SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }

        public void PerpareColorAdjustmentTables()
        {
            CreateBrightnessAndContrastTransitionTables();
            CreateBrightnessAndContrastPoolTables();
            CreateBrightnessAndContrastTable(MasksConfig.mGeneralBrightness, MasksConfig.mGeneralContrast); //Has to be last
        }

        public void GeneralColorAdjustment()
        {
            if (mWorkMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;
                Int32 vValueRed;
                Int32 vValueGreen;
                Int32 vValueBlue;

                if ((MasksConfig.mGeneralLighteness != 1.0f) ||  //Color Adaption costs time..only do it if required!
                    (MasksConfig.mGeneralBrightness != 0.0f) ||
                    (MasksConfig.mGeneralContrast   != 0.0f) ||
                    (MasksConfig.mGeneralColoringRed   != 0) ||
                    (MasksConfig.mGeneralColoringGreen != 0) ||
                    (MasksConfig.mGeneralColoringBlue  != 0))
                {

                    for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                    {
                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 = 0;

                        for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                        {

                            if (mDestination32BitBitmapMode)
                            {
                                vValue = mAreaBitmapArray[vY, vX];
                            }
                            else
                            {
                                vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            }

                            vValueRed = (Int32)((vValue & 0x00FF0000) >> 16);
                            vValueGreen = (Int32)((vValue & 0x0000FF00) >> 8);
                            vValueBlue = (Int32)((vValue & 0x000000FF));

                            vValueRed = (Int32)(mColorCorrectionTable[vValueRed]);
                            vValueGreen = (Int32)(mColorCorrectionTable[vValueGreen]);
                            vValueBlue = (Int32)(mColorCorrectionTable[vValueBlue]);

                            vValueRed += MasksConfig.mGeneralColoringRed;
                            vValueGreen += MasksConfig.mGeneralColoringGreen;
                            vValueBlue += MasksConfig.mGeneralColoringBlue;

                            vValueRed = Convert.ToInt32(MasksConfig.mGeneralLighteness * (Single)(vValueRed));
                            vValueGreen = Convert.ToInt32(MasksConfig.mGeneralLighteness * (Single)(vValueGreen));
                            vValueBlue = Convert.ToInt32(MasksConfig.mGeneralLighteness * (Single)(vValueBlue));

                            if (vValueRed > 255)
                            {
                                vValueRed = 255;
                            }
                            else if (vValueRed < 0)
                            {
                                vValueRed = 0;
                            }
                            if (vValueGreen > 255)
                            {
                                vValueGreen = 255;
                            }
                            else if (vValueGreen < 0)
                            {
                                vValueGreen = 0;
                            }
                            if (vValueBlue > 255)
                            {
                                vValueBlue = 255;
                            }
                            else if (vValueBlue < 0)
                            {
                                vValueBlue = 0;
                            }
                            vValue = (UInt32)((vValueRed << 16) | (vValueGreen << 8) | vValueBlue);

                            if (mDestination32BitBitmapMode)
                            {
                                mAreaBitmapArray[vY, vX] = (mAreaBitmapArray[vY, vX] & 0xFF000000) | (vValue & 0x00FFFFFF);
                            }
                            else
                            {
                                SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                            }

                            vPixelInQuadIndex++;
                            if (vPixelInQuadIndex >= 4)
                            {
                                vPixelInQuadIndex = 0;
                                vUInt32IndexModal96 += 3;
                            }
                        }
                    }
                }
            }
        }

        public void CreateBrightnessAndContrastTable(Single iBrightness, Single iContrast)
        {

            Double vBrightness = Convert.ToDouble(iBrightness) * 1.275; //+/-127.5 pixel
            Double vContrast   = Convert.ToDouble(iContrast) * 1.275; //+/-127.5 pixel
            Double vContrastGain = 1.0;
            Double vNegativeGain = 1.0;
            Double vStartAdd = 0.0;

            if (iContrast == 0.0f)
            {
                //Nothing to do
            }
            else if (iContrast == 100.0f)
            {
                vContrastGain = 512.0;
            }
            else if (iContrast == -100.0f)
            {
                vNegativeGain = 0.0;
                vStartAdd = 127.5;
            }
            else if (iContrast > 0.0)
            {
                vContrastGain = 255.0 / (255.0 - (2.0 * vContrast));
            }
            else
            {
                vContrastGain = -(255.0 - (2.0 * vContrast)) / 255.0;
                vStartAdd = -vContrast;
            }

            for (Int32 vValue = 0; vValue < 256; vValue++)
            {
                //Brightness
                Double vValue2 = Convert.ToDouble(vValue) + vBrightness;
                Double vValue3;

                //Contrast
                if (iContrast >= 0.0)
                {
                    vValue3 = vValue2 - vContrast;
                    vValue3 = vContrastGain * vValue3;
                }
                else
                {
                    vValue3 = vNegativeGain * vValue2;
                    vValue3 = vValue3 + vStartAdd;
                }

                vValue3 = Math.Round(vValue3);
                if (vValue3 > 255.0)
                {
                    vValue3 = 255.0;
                }
                if (vValue3 < 0.0)
                {
                    vValue3 = 0.0;
                }
                UInt32 vValueResult = Convert.ToUInt32(vValue3);
                mColorCorrectionTable[vValue] = vValueResult;
            }
        }

        public void CreateBrightnessAndContrastTransitionTables()
        {
            for (Int32 vTransitionType  = 0; vTransitionType<(Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
            {
              for (Int32 vIndex = 0; vIndex<=100; vIndex++)
              {
                Single vBrightness = MasksConfig.mTransitionsColorParameters[vTransitionType].mBrightness;
                Single vContrast   = MasksConfig.mTransitionsColorParameters[vTransitionType].mContrast;
                vBrightness *= 0.01f * (Single)(vIndex);
                vContrast   *= 0.01f * (Single)(vIndex);
                CreateBrightnessAndContrastTable(vBrightness, vContrast);
                for (Int32 vValue=0; vValue<=255; vValue++)
                {
                  mTransitionColorCorrectionTable[vTransitionType,vIndex,vValue] = mColorCorrectionTable[vValue];
                }
              }
            }
        }
        
        public void CreateBrightnessAndContrastPoolTables()
        {
            for (Int32 vPoolType  = 0; vPoolType<(Int32)(MasksConfig.tPoolType.eSize); vPoolType++)
            {
                Single vBrightness = MasksConfig.mPoolsParameters[vPoolType].mBrightness;
                Single vContrast   = MasksConfig.mPoolsParameters[vPoolType].mContrast;
                CreateBrightnessAndContrastTable(vBrightness, vContrast);
                for (Int32 vValue=0; vValue<=255; vValue++)
                {
                  mPoolColorCorrectionTable[vPoolType,vValue] = mColorCorrectionTable[vValue];
                }
            }
        }

        public void TransitionAndPoolColorAdjustment(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            if (mWorkMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                Int32 vValueRed;
                Int32 vValueGreen;
                Int32 vValueBlue;
                Int32 vOrigiValueRed;
                Int32 vOrigiValueGreen;
                Int32 vOrigiValueBlue;
                                           
                Single vCalcValueRed; 
                Single vCalcValueGreen;
                Single vCalcValueBlue;

                Byte vCmdValue = 0;
                Byte vSubCmdValue = 0;

                Single vColoringFactor  = 0.0f;
                Single vLightnessFactor = 0.0f;

                Single vSquareDist1 = 1.0f;
                Single vSquareDist2 = 1.0f;

                Boolean vWorkToDo = false;

                MasksConfig.tTransitionType   vTransitionTypeForLightness    = MasksConfig.tTransitionType.eWaterTransition;
                MasksConfig.tTransitionType   vTransitionTypeForColorness    = MasksConfig.tTransitionType.eWaterTransition;
                MasksConfig.tPoolType         vPoolType                      = MasksConfig.tPoolType.eLandPool;

                if ((MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mLightness != 1.0f) ||  //Color Adaption costs time..only do it if required!
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mBrightness != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mContrast != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mColoringRed != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mColoringGreen != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eWaterPool)].mColoringBlue != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mLightness != 1.0f) || 
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mBrightness != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mContrast != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mColoringRed != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mColoringGreen != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eLandPool)].mColoringBlue != 0) ||                 
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mLightness != 1.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mBrightness != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mContrast != 0.0f) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mColoringRed != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mColoringGreen != 0) ||
                    (MasksConfig.mPoolsParameters[(Int32)(MasksConfig.tPoolType.eBlendPool)].mColoringBlue != 0) ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eLightness)])  ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eColoring)])   ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eLightness)]) ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eColoring)])  ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eLightness)])  ||
                    (mTrippleSTransitionActive[(Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eColoring)]))
                {

                    Int32 vPixelCounterForDisplay = 0;
                    Int32 vRowCountForSlice = -1;

                    for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                    {
                        vPixelCounterForDisplay++;

                        vRowCountForSlice++;
                        if (cUseSliceMethode)
                        {
                            if ((vRowCountForSlice % cYSliceSpace) == 0)
                            {
                                SliceReduceLinesCount(0, vRowCountForSlice, mPixelCountInX - 1, vRowCountForSlice + cYSliceSpace - 1);
                                if (cUseCellsMethode)
                                {
                                    CellsReduceLinesCount(vRowCountForSlice, vRowCountForSlice + cYSliceSpace - 1);
                                }
                            }
                        }


                        if (((vPixelCounterForDisplay % 100) == 0) || (vPixelCounterForDisplay == mPixelCountInY))
                        {
                            iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Calculate Colors ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                        }

                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 = 0;

                        for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                        {

                            if (mDestination32BitBitmapMode)
                            {
                                vValue = mAreaBitmapArray[vY, vX];
                            }
                            else
                            {
                                vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            }

                            vOrigiValueRed = (Int32)((vValue & 0x00FF0000) >> 16);
                            vOrigiValueGreen = (Int32)((vValue & 0x0000FF00) >> 8);
                            vOrigiValueBlue = (Int32)((vValue & 0x000000FF));

                            vValueRed   = vOrigiValueRed;
                            vValueGreen = vOrigiValueGreen;
                            vValueBlue  = vOrigiValueBlue;
                            vCalcValueRed    = (Single)(vValueRed); 
                            vCalcValueGreen  = (Single)(vValueGreen);
                            vCalcValueBlue   = (Single)(vValueBlue);
                            vTransitionTypeForLightness = MasksConfig.tTransitionType.eWaterTransition;
                            vTransitionTypeForColorness = MasksConfig.tTransitionType.eWaterTransition;

                            vColoringFactor  = 0.0f;
                            vLightnessFactor = 0.0f;
                            vWorkToDo = false;

                            //Check Pixel Commando    
                            vCmdValue = mWorkCommandArray[vY, vX];

                            vSubCmdValue = vCmdValue;
                            vSubCmdValue &= 0xC0;

                            if (vSubCmdValue == 0x00)
                            {
                                //Transitions

                                //Water
                                vSubCmdValue = vCmdValue;
                                vSubCmdValue &= 0x03;

                                switch (vSubCmdValue)
                                {
                                    case (Byte)(tWorkCommandCommand.eWaterTransitionWater):
                                        {
                                            vLightnessFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWaterTransition), (UInt32)(MasksConfig.tTransitionSubType.eLightness)];
                                            vColoringFactor  = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWaterTransition), (UInt32)(MasksConfig.tTransitionSubType.eColoring)];
                                            vTransitionTypeForLightness = MasksConfig.tTransitionType.eWaterTransition;
                                            vTransitionTypeForColorness = MasksConfig.tTransitionType.eWaterTransition;
                                            vWorkToDo = true;
                                            break;
                                        }
                                    case (Byte)(tWorkCommandCommand.eWaterTransitionTransition):
                                        {
                                            CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                            vLightnessFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eLightness), vSquareDist1, vSquareDist2);
                                            vColoringFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eColoring), vSquareDist1, vSquareDist2);
                                            vTransitionTypeForLightness = MasksConfig.tTransitionType.eWaterTransition;
                                            vTransitionTypeForColorness = MasksConfig.tTransitionType.eWaterTransition;
                                            vWorkToDo = true;
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }

                                //WaterTwo
                                vSubCmdValue = vCmdValue;
                                vSubCmdValue &= 0x0C;

                                switch (vSubCmdValue)
                                {
                                    case (Byte)(tWorkCommandCommand.eWater2TransitionWater):
                                        {
                                            Single vTempLightnessFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWater2Transition), (UInt32)(MasksConfig.tTransitionSubType.eLightness)];
                                            Single vTempColoringFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWater2Transition), (UInt32)(MasksConfig.tTransitionSubType.eColoring)];

                                            if (vTempLightnessFactor > vLightnessFactor)
                                            {
                                                vLightnessFactor = vTempLightnessFactor;
                                                vTransitionTypeForLightness = MasksConfig.tTransitionType.eWater2Transition;
                                            }
                                            if (vTempColoringFactor > vColoringFactor)
                                            {
                                                vColoringFactor = vTempColoringFactor;
                                                vTransitionTypeForColorness = MasksConfig.tTransitionType.eWater2Transition;
                                            }

                                            vWorkToDo = true;

                                            break;
                                        }
                                    case (Byte)(tWorkCommandCommand.eWater2TransitionTransition):
                                        {
                                            CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                            Single vTempLightnessFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eLightness), vSquareDist1, vSquareDist2);
                                            Single vTempColoringFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eColoring), vSquareDist1, vSquareDist2);
                                            if (vTempLightnessFactor > vLightnessFactor)
                                            {
                                                vLightnessFactor = vTempLightnessFactor;
                                                vTransitionTypeForLightness = MasksConfig.tTransitionType.eWater2Transition;
                                            }
                                            if (vTempColoringFactor > vColoringFactor)
                                            {
                                                vColoringFactor = vTempColoringFactor;
                                                vTransitionTypeForColorness = MasksConfig.tTransitionType.eWater2Transition;
                                            }

                                            vWorkToDo = true;

                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }


                                //Blend
                                vSubCmdValue = vCmdValue;
                                vSubCmdValue &= 0x30;

                                switch (vSubCmdValue)
                                {
                                    case (Byte)(tWorkCommandCommand.eBlendTransitionFullyTransparent):
                                        {
                                            Single vTempLightnessFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eBlendTransition), (UInt32)(MasksConfig.tTransitionSubType.eLightness)];
                                            Single vTempColoringFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eBlendTransition), (UInt32)(MasksConfig.tTransitionSubType.eColoring)];
                                            if (vTempLightnessFactor > vLightnessFactor)
                                            {
                                                vLightnessFactor = vTempLightnessFactor;
                                                vTransitionTypeForLightness = MasksConfig.tTransitionType.eBlendTransition;
                                            }
                                            if (vTempColoringFactor > vColoringFactor)
                                            {
                                                vColoringFactor = vTempColoringFactor;
                                                vTransitionTypeForColorness = MasksConfig.tTransitionType.eBlendTransition;
                                            }
                                            
                                            vWorkToDo = true;

                                            break;
                                        }
                                    case (Byte)(tWorkCommandCommand.eBlendTransitionTransition):
                                        {
                                            CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                            Single vTempLightnessFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eLightness), vSquareDist1, vSquareDist2);
                                            Single vTempColoringFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eColoring), vSquareDist1, vSquareDist2);
                                            if (vTempLightnessFactor > vLightnessFactor)
                                            {
                                                vLightnessFactor = vTempLightnessFactor;
                                                vTransitionTypeForLightness = MasksConfig.tTransitionType.eBlendTransition;
                                            }
                                            if (vTempColoringFactor > vColoringFactor)
                                            {
                                                vColoringFactor = vTempColoringFactor;
                                                vTransitionTypeForColorness = MasksConfig.tTransitionType.eBlendTransition;
                                            }

                                            vWorkToDo = true;

                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }

                                if (vWorkToDo)
                                {
                                    Int32 vLightness = Convert.ToInt32(100.0f * vLightnessFactor);
                                    vCalcValueRed = mTransitionColorCorrectionTable[(UInt32)(vTransitionTypeForLightness), vLightness, vOrigiValueRed];
                                    vCalcValueGreen = mTransitionColorCorrectionTable[(UInt32)(vTransitionTypeForLightness), vLightness, vOrigiValueGreen];
                                    vCalcValueBlue = mTransitionColorCorrectionTable[(UInt32)(vTransitionTypeForLightness), vLightness, vOrigiValueBlue];
                                    vCalcValueRed += vColoringFactor * MasksConfig.mTransitionsColorParameters[(UInt32)(vTransitionTypeForColorness)].mColoringRed;
                                    vCalcValueGreen += vColoringFactor * MasksConfig.mTransitionsColorParameters[(UInt32)(vTransitionTypeForColorness)].mColoringGreen;
                                    vCalcValueBlue += vColoringFactor * MasksConfig.mTransitionsColorParameters[(UInt32)(vTransitionTypeForColorness)].mColoringBlue;
                                    Single vLightnessFactor2 = vLightnessFactor * (MasksConfig.mTransitionsColorParameters[(UInt32)(vTransitionTypeForLightness)].mLighteness - 1.0f) + 1.0f;
                                    vValueRed = Convert.ToInt32(vLightnessFactor2 * vCalcValueRed);
                                    vValueGreen = Convert.ToInt32(vLightnessFactor2 * vCalcValueGreen);
                                    vValueBlue = Convert.ToInt32(vLightnessFactor2 * vCalcValueBlue);
                                }
                            }
                            else
                            {
                                //Pools Pools overwrite everything!
                                switch (vSubCmdValue)
                                {
                                    case (Byte)(tWorkCommandCommand.eFixWater):
                                        {
                                            vLightnessFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eWaterPool)].mLightness);
                                            vWorkToDo = true;
                                            break;
                                        }
                                    case (Byte)(tWorkCommandCommand.eFixLand):
                                        {
                                            vLightnessFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eLandPool)].mLightness);
                                            vWorkToDo = true;
                                            break;
                                        }
                                    case (Byte)(tWorkCommandCommand.eFixBlend):
                                        {
                                            vLightnessFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eBlendPool)].mLightness);
                                            vWorkToDo = true;
                                            break;
                                        }
                                    default:
                                        {
                                            break;
                                        }
                                }
                                if (vWorkToDo)
                                {
                                    vCalcValueRed = mPoolColorCorrectionTable[(UInt32)(vPoolType), vOrigiValueRed];
                                    vCalcValueGreen = mPoolColorCorrectionTable[(UInt32)(vPoolType), vOrigiValueGreen];
                                    vCalcValueBlue = mPoolColorCorrectionTable[(UInt32)(vPoolType), vOrigiValueBlue];
                                    vCalcValueRed += vColoringFactor * MasksConfig.mPoolsParameters[(UInt32)(vPoolType)].mColoringRed;
                                    vCalcValueGreen += vColoringFactor * MasksConfig.mPoolsParameters[(UInt32)(vPoolType)].mColoringGreen;
                                    vCalcValueBlue += vColoringFactor * MasksConfig.mPoolsParameters[(UInt32)(vPoolType)].mColoringBlue;
                                    vValueRed = Convert.ToInt32(vLightnessFactor * vCalcValueRed);
                                    vValueGreen = Convert.ToInt32(vLightnessFactor * vCalcValueGreen);
                                    vValueBlue = Convert.ToInt32(vLightnessFactor * vCalcValueBlue);
                                }
                            }

                            if (vValueRed > 255)
                            {
                                vValueRed = 255;
                            }
                            else if (vValueRed < 0)
                            {
                                vValueRed = 0;
                            }
                            if (vValueGreen > 255)
                            {
                                vValueGreen = 255;
                            }
                            else if (vValueGreen < 0)
                            {
                                vValueGreen = 0;
                            }
                            if (vValueBlue > 255)
                            {
                                vValueBlue = 255;
                            }
                            else if (vValueBlue < 0)
                            {
                                vValueBlue = 0;
                            }
                            vValue = (UInt32)((vValueRed << 16) | (vValueGreen << 8) | vValueBlue);

                            if (mDestination32BitBitmapMode)
                            {
                                mAreaBitmapArray[vY, vX] = (mAreaBitmapArray[vY, vX] & 0xFF000000) | (vValue & 0x00FFFFFF);
                            }
                            else
                            {
                                SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                            }

                            vPixelInQuadIndex++;
                            if (vPixelInQuadIndex >= 4)
                            {
                                vPixelInQuadIndex = 0;
                                vUInt32IndexModal96 += 3;
                            }
                        }
                    }
                }
            }
        }


        public void GreeningBackUpBitmap()  
        {
            if (mBackUpMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {

                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mBackUpAreaBitmapArray[vY, vX];
                        }
                        else
                        {
                            vValue = GetBackUpSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                        }

                        vValue = ((vValue & 0x00FF0000) >> 16) + ((vValue & 0x0000FF00) >> 8) + ((vValue & 0x000000FF));
                        vValue = vValue / 3;
                        //vValue = vValue >> 2; //for  test make more black
                        vValue = vValue << 8; //shift to green position

                        if (mDestination32BitBitmapMode)
                        {
                            mBackUpAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            SetBackUpDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void MergeWorkBitmapRedAndBlueChannelIntoBackUpBitmap()
        {
            if (mWorkMemoryAllocated && mBackUpMemoryAllocated)
            {
                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValueWork;
                UInt32 vValueBackUp;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {

                        if (mDestination32BitBitmapMode)
                        {
                            vValueWork   = mAreaBitmapArray[vY, vX];
                            vValueBackUp = mBackUpAreaBitmapArray[vY, vX];
                            vValueBackUp = ((vValueWork & 0x00FF00FF) | (vValueBackUp & 0xFF00FF00));
                            mBackUpAreaBitmapArray[vY, vX] = vValueBackUp;
                        }
                        else
                        {
                            vValueWork = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            vValueBackUp = GetBackUpSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);

                            vValueBackUp = ((vValueWork & 0x00FF00FF) | (vValueBackUp & 0xFF00FF00));
                            SetBackUpDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValueBackUp);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void CopyWorkBitmapIntoBackUpBitmap()
        {
            if (mWorkMemoryAllocated && mBackUpMemoryAllocated)
            {
                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {
                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                            mBackUpAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            SetBackUpDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }
                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void CopyBackUpBitmapIntoWorkBitmap()
        {
            if (mWorkMemoryAllocated && mBackUpMemoryAllocated)
            {
                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {
                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mBackUpAreaBitmapArray[vY, vX];
                            mAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            vValue = GetBackUpSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void GetPixelRGB(Int32 iPixelIndexX, Int32 iPixelIndexY, ref Int32 oRed, ref Int32 oGreen, ref Int32 oBlue)
        {
            UInt32 vValue;

            if (mDestination32BitBitmapMode)
            {
                vValue = mAreaBitmapArray[iPixelIndexY, iPixelIndexX];
            }
            else
            {
                Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
                Int32 vPixelInQuadIndex = iPixelIndexX % 4;

                //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
                //  Output:  00 Rx Gx Bx

                switch (vPixelInQuadIndex)
                {
                    case 0: //Pixel 1 in Quad
                        vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96];
                        break;
                    case 1: //Pixel 2 in Quad
                        vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) << 8);
                        break;
                    case 2: //Pixel 3 in Quad
                        vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) << 16);
                        break;
                    case 3: //Pixel 4 in Quad
                        vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) >> 8;
                        break;
                    default:
                        vValue = 0; //error
                        break;
                }
            }
            
            oRed   = (Int32)((vValue & 0x00FF0000) >> 16);
            oGreen = (Int32)((vValue & 0x0000FF00) >> 8);
            oBlue  = (Int32)(vValue & 0x000000FF);
        }

        private UInt32 GetSourcePixel24and32BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY)
        {
            if (mDestination32BitBitmapMode)
            {
                return mAreaBitmapArray[iPixelIndexY, iPixelIndexX];
            }
            else
            {
                Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
                Int32 vPixelInQuadIndex = iPixelIndexX % 4;
                UInt32 vValue;

                //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
                //  Output:  00 Rx Gx Bx

                switch (vPixelInQuadIndex)
                {
                    case 0: //Pixel 1 in Quad
                        vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96];
                        break;
                    case 1: //Pixel 2 in Quad
                        vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) << 8);
                        break;
                    case 2: //Pixel 3 in Quad
                        vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) << 16);
                        break;
                    case 3: //Pixel 4 in Quad
                        vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) >> 8;
                        break;
                    default:
                        vValue = 0; //error
                        break;
                }
                return vValue;
            }
        }

        private UInt32 GetSourcePixel24BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY)
        {
            Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
            Int32 vPixelInQuadIndex = iPixelIndexX % 4;
            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (vPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }
            return vValue;
        }

        private UInt32 GetBackUpSourcePixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY)
        {

            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (iPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }
            return vValue;
        }

        public void LimitRGBValues(ref Int32 xRed, ref Int32 xGreen, ref Int32 xBlue)
        {
            if (xRed > 255)
            {
                xRed = 255;
            }
            else if (xRed < 0)
            {
                xRed = 0;
            }
            if (xGreen > 255)
            {
                xGreen = 255;
            }
            else if (xGreen < 0)
            {
                xGreen = 0;
            }
            if (xBlue > 255)
            {
                xBlue = 255;
            }
            else if (xBlue < 0)
            {
                xBlue = 0;
            }
        }

        public void SetPixelRGB(Int32 iPixelIndexX, Int32 iPixelIndexY, Int32 iRed, Int32 iGreen, Int32 iBlue)
        {
            UInt32 vValue = (UInt32)((iRed << 16) | (iGreen << 8) | (iBlue));

            if (mDestination32BitBitmapMode)
            {
                mAreaBitmapArray[iPixelIndexY, iPixelIndexX] = (mAreaBitmapArray[iPixelIndexY, iPixelIndexX] & 0xFF000000) | (vValue & 0x00FFFFFF);
            }
            else
            {
                //  Input :  00 Rx Gx Bx
                //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

                Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
                Int32 vPixelInQuadIndex = iPixelIndexX % 4;

                switch (vPixelInQuadIndex)
                {
                    case 0: //Pixel 1 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (vValue & 0x00FFFFFF);
                        break;
                    case 1: //Pixel 2 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (vValue << 24);
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (vValue >> 8);
                        break;
                    case 2: //Pixel 3 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (vValue << 16);
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (vValue >> 16);
                        break;
                    case 3: //Pixel 4 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (vValue << 8);
                        break;
                    default:
                        break; //error
                }
            }
        }

        private void SetDestinationPixel24and32BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY, UInt32 iValue)
        {
            if (mDestination32BitBitmapMode)
            {
                mAreaBitmapArray[iPixelIndexY, iPixelIndexX] = iValue;
            }
            else
            {
                //  Input :  00 Rx Gx Bx
                //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

                Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
                Int32 vPixelInQuadIndex = iPixelIndexX % 4;

                switch (vPixelInQuadIndex)
                {
                    case 0: //Pixel 1 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (iValue & 0x00FFFFFF);
                        break;
                    case 1: //Pixel 2 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (iValue << 24);
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (iValue >> 8);
                        break;
                    case 2: //Pixel 3 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (iValue << 16);
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (iValue >> 16);
                        break;
                    case 3: //Pixel 4 in Quad
                        mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (iValue << 8);
                        break;
                    default:
                        break; //error
                }
            }
        }


        private void SetDestinationPixel24BitBitmapFormat(Int32 iPixelIndexX, Int32 iPixelIndexY, UInt32 iValue)
        {
            //  Input :  00 Rx Gx Bx
            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

            Int32 vUInt32IndexModal96 = 3 * (iPixelIndexX >> 2);  //or (x>>2)<<1 + (X>>2) but danger of compiler simplifies
            Int32 vPixelInQuadIndex = iPixelIndexX % 4;

            switch (vPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0xFF000000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (iValue & 0x00FFFFFF);
                    break;
                case 1: //Pixel 2 in Quad
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96] = (0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96]) | (iValue << 24);
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (iValue >> 8);
                    break;
                case 2: //Pixel 3 in Quad
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1] = (0x0000FFFF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 1]) | (iValue << 16);
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (iValue >> 16);
                    break;
                case 3: //Pixel 4 in Quad
                    mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2] = (0x000000FF & mAreaBitmapArray[iPixelIndexY, vUInt32IndexModal96 + 2]) | (iValue << 8);
                    break;
                default:
                    break; //error
            }
        }


        private UInt32 GetSourcePixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY)
        {

            UInt32 vValue;

            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3
            //  Output:  00 Rx Gx Bx

            switch (iPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    vValue = 0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96];
                    break;
                case 1: //Pixel 2 in Quad
                    vValue = ((0xFF000000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) >> 24) | ((0x0000FFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) << 8);
                    break;
                case 2: //Pixel 3 in Quad
                    vValue = ((0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) >> 16) | ((0x000000FF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) << 16);
                    break;
                case 3: //Pixel 4 in Quad
                    vValue = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) >> 8;
                    break;
                default:
                    vValue = 0; //error
                    break;
            }
            return vValue;
        }

   
        private void SetDestinationPixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY, UInt32 iValue)
        {
            //  Input :  00 Rx Gx Bx
            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

            switch (iPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0xFF000000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue & 0x00FFFFFF);
                    break;
                case 1: //Pixel 2 in Quad
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0x00FFFFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue << 24);
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0xFFFF0000 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue >> 8);
                    break;
                case 2: //Pixel 3 in Quad
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0x0000FFFF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue << 16);
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0xFFFFFF00 & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue >> 16);
                    break;
                case 3: //Pixel 4 in Quad
                    mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0x000000FF & mAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue << 8);
                    break;
                default:
                    break; //error
            }
        }

        private void SetBackUpDestinationPixel24BitBitmapFormat(Int32 iUInt32IndexModal96, Int32 iPixelInQuadIndex, Int32 iPixelIndexY, UInt32 iValue)
        {
            //  Input :  00 Rx Gx Bx
            //  Memory:  B2 R1 G1 B1 | G3 B3 R2 G2 | R4 G4 B4 R3

            switch (iPixelInQuadIndex)
            {
                case 0: //Pixel 1 in Quad
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0xFF000000 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue & 0x00FFFFFF);
                    break;
                case 1: //Pixel 2 in Quad
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96] = (0x00FFFFFF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96]) | (iValue << 24);
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0xFFFF0000 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue >> 8);
                    break;
                case 2: //Pixel 3 in Quad
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1] = (0x0000FFFF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 1]) | (iValue << 16);
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0xFFFFFF00 & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue >> 16);
                    break;
                case 3: //Pixel 4 in Quad
                    mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2] = (0x000000FF & mBackUpAreaBitmapArray[iPixelIndexY, iUInt32IndexModal96 + 2]) | (iValue << 8);
                    break;
                default:
                    break; //error
            }
        }

        public tPointWithSquareDistance GetCoastMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {


            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vCoastMinPoint;

            vCoastMinPoint.mX = 0.0f;
            vCoastMinPoint.mY = 0.0f;
            vCoastMinPoint.mSquareDistance = 1.0e12f;
            vCoastMinPoint.mValid = false;

            foreach (tPoint vPoint in MasksCommon.mCoastPoints[iTrippleSType])
            {
                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vCoastMinPoint.mSquareDistance)
                {
                    vCoastMinPoint.mSquareDistance = vTempDist;
                    vCoastMinPoint.mX = vPoint.mX;
                    vCoastMinPoint.mY = vPoint.mY;
                    vCoastMinPoint.mValid = true;
                }
            }


            foreach (tLine vLine in MasksCommon.mCoastLines[iTrippleSType])
            {

                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vCoastMinPoint.mSquareDistance)
                    {
                        vCoastMinPoint.mSquareDistance = vTempDist;
                        vCoastMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vCoastMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vCoastMinPoint.mValid = true;
                    }
                }
            }

            return vCoastMinPoint;
        }

        
        public tPointWithSquareDistance GetSliceCoastMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {


            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vCoastMinPoint;

            vCoastMinPoint.mX = 0.0f;
            vCoastMinPoint.mY = 0.0f;
            vCoastMinPoint.mSquareDistance = 1.0e12f;
            vCoastMinPoint.mValid = false;

            foreach (tPoint vPoint in MasksCommon.mSliceCoastPoints[iTrippleSType])
            {
                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vCoastMinPoint.mSquareDistance)
                {
                    vCoastMinPoint.mSquareDistance = vTempDist;
                    vCoastMinPoint.mX = vPoint.mX;
                    vCoastMinPoint.mY = vPoint.mY;
                    vCoastMinPoint.mValid = true;
                }
            }


            foreach (tLine vLine in MasksCommon.mSliceCoastLines[iTrippleSType])
            {

                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vCoastMinPoint.mSquareDistance)
                    {
                        vCoastMinPoint.mSquareDistance = vTempDist;
                        vCoastMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vCoastMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vCoastMinPoint.mValid = true;
                    }
                }
            }

            return vCoastMinPoint;
        }


        public tPointWithSquareDistance GetCellsCoastMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {


            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vCoastMinPoint;

            vCoastMinPoint.mX = 0.0f;
            vCoastMinPoint.mY = 0.0f;
            vCoastMinPoint.mSquareDistance = 1.0e12f;
            vCoastMinPoint.mValid = false;

            Int32 vCell = ((Int32)(iXp)) / cXCellsSpace;

            foreach (tPoint vPoint in mCellsCoastPoints[iTrippleSType, vCell])
            {
                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vCoastMinPoint.mSquareDistance)
                {
                    vCoastMinPoint.mSquareDistance = vTempDist;
                    vCoastMinPoint.mX = vPoint.mX;
                    vCoastMinPoint.mY = vPoint.mY;
                    vCoastMinPoint.mValid = true;
                }
            }


            foreach (tLine vLine in mCellsCoastLines[iTrippleSType, vCell])
            {

                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vCoastMinPoint.mSquareDistance)
                    {
                        vCoastMinPoint.mSquareDistance = vTempDist;
                        vCoastMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vCoastMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vCoastMinPoint.mValid = true;
                    }
                }
            }

            return vCoastMinPoint;
        }


        public tPointWithSquareDistance GetDeepWaterMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {

            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vDeepWaterMinPoint;

            vDeepWaterMinPoint.mX = 0.0f;
            vDeepWaterMinPoint.mY = 0.0f;
            vDeepWaterMinPoint.mSquareDistance = 1.0e12f;
            vDeepWaterMinPoint.mValid = false;

            foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[iTrippleSType])
            {

                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                {
                    vDeepWaterMinPoint.mSquareDistance = vTempDist;
                    vDeepWaterMinPoint.mX = vPoint.mX;
                    vDeepWaterMinPoint.mY = vPoint.mY;
                    vDeepWaterMinPoint.mValid = true;
                }
            }

            foreach (tLine vLine in MasksCommon.mDeepWaterLines[iTrippleSType])
            {
                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                    {
                        vDeepWaterMinPoint.mSquareDistance = vTempDist;
                        vDeepWaterMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vDeepWaterMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vDeepWaterMinPoint.mValid = true;
                    }
                }
            }

            return vDeepWaterMinPoint;
        }

        public tPointWithSquareDistance GetSliceDeepWaterMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {

            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vDeepWaterMinPoint;

            vDeepWaterMinPoint.mX = 0.0f;
            vDeepWaterMinPoint.mY = 0.0f;
            vDeepWaterMinPoint.mSquareDistance = 1.0e12f;
            vDeepWaterMinPoint.mValid = false;

            foreach (tPoint vPoint in MasksCommon.mSliceDeepWaterPoints[iTrippleSType])
            {

                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                {
                    vDeepWaterMinPoint.mSquareDistance = vTempDist;
                    vDeepWaterMinPoint.mX = vPoint.mX;
                    vDeepWaterMinPoint.mY = vPoint.mY;
                    vDeepWaterMinPoint.mValid = true;
                }
            }

            foreach (tLine vLine in MasksCommon.mSliceDeepWaterLines[iTrippleSType])
            {
                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                    {
                        vDeepWaterMinPoint.mSquareDistance = vTempDist;
                        vDeepWaterMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vDeepWaterMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vDeepWaterMinPoint.mValid = true;
                    }
                }
            }

            return vDeepWaterMinPoint;
        }


        public tPointWithSquareDistance GetCellsDeepWaterMinPoint(Int32 iTrippleSType, Single iXp, Single iYp)
        {

            Single vTempDist;
            Single vTempUFactor;

            tPointWithSquareDistance vDeepWaterMinPoint;

            vDeepWaterMinPoint.mX = 0.0f;
            vDeepWaterMinPoint.mY = 0.0f;
            vDeepWaterMinPoint.mSquareDistance = 1.0e12f;
            vDeepWaterMinPoint.mValid = false;

            Int32 vCell = ((Int32)(iXp)) / cXCellsSpace;

            foreach (tPoint vPoint in mCellsDeepWaterPoints[iTrippleSType, vCell])
            {

                vTempDist = (vPoint.mX - iXp) * (vPoint.mX - iXp) + (vPoint.mY - iYp) * (vPoint.mY - iYp);
                if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                {
                    vDeepWaterMinPoint.mSquareDistance = vTempDist;
                    vDeepWaterMinPoint.mX = vPoint.mX;
                    vDeepWaterMinPoint.mY = vPoint.mY;
                    vDeepWaterMinPoint.mValid = true;
                }
            }

            foreach (tLine vLine in mCellsDeepWaterLines[iTrippleSType, vCell])
            {
                vTempUFactor = vLine.mUo + vLine.mUx * iXp + vLine.mUy * iYp;
                if ((vTempUFactor > 0.0) && (vTempUFactor < 1.0)) //Distance Within Line?
                {
                    vTempDist = vLine.mDo + vLine.mDx * iXp + vLine.mDy * iYp;
                    vTempDist = vTempDist * vTempDist;
                    if (vTempDist < vDeepWaterMinPoint.mSquareDistance)
                    {
                        vDeepWaterMinPoint.mSquareDistance = vTempDist;
                        vDeepWaterMinPoint.mX = vTempUFactor * (vLine.mX2 - vLine.mX1) + vLine.mX1;
                        vDeepWaterMinPoint.mY = vTempUFactor * (vLine.mY2 - vLine.mY1) + vLine.mY1;
                        vDeepWaterMinPoint.mValid = true;
                    }
                }
            }

            return vDeepWaterMinPoint;
        }


        public Int32 IsVectorCuttingDeepWater(Int32 iTrippleSType, Single iXp1, Single iYp1, Single iXp2, Single iYp2)
        {

            const Single cCutEpsilon = 12e-7f;  //6e-7 seems to be too small

            Single vTempN;
            Single vTempNInv;
            Single vTempUz;
            Single vTempLz;
            Single vTempU;
            Single vTempL;

            Single vAx;
            Single vAy;
            Single vBx;
            Single vBy;

            Single vCx = iXp1;
            Single vCy = iYp1;
            Single vDx = iXp2;
            Single vDy = iYp2;

            Int32 vCuttingDeepWater = 0;

            foreach (tLine vLine in MasksCommon.mForCutCheckDeepWaterLines[iTrippleSType])
            {
                vAx = vLine.mX1;
                vAy = vLine.mY1;
                vBx = vLine.mX2;
                vBy = vLine.mY2;
                vTempN = (vBx - vAx) * (vDy - vCy) - (vBy - vAy) * (vDx - vCx);
                if (vTempN != 0.0)
                {
                    vTempNInv = 1.0f / vTempN;
                    vTempUz = -vCy * vDx + vCx * vDy - (vDy - vCy) * vAx + (vDx - vCx) * vAy;
                    vTempU  = vTempUz * vTempNInv;
                    
                    if ((vTempU > 0.0f) && (vTempU < 1.0f))
                    {
                        //The cut point is within the Line AB 
                        vTempLz = vAy * vBx - vAx * vBy + (vBy - vAy) * vCx - (vBx - vAx) * vCy;
                        vTempL  = vTempLz * vTempNInv;
                        if ((vTempL > 0.0f) && (vTempL < 1.0f))
                        {
                            //And it is within Lin CD
                            //Is it a critical cut?
                            if (((vTempU < (cCutEpsilon)) && (vTempU > (-cCutEpsilon))) ||
                                ((vTempU < (1.0f + cCutEpsilon)) && (vTempU > (1.0f - cCutEpsilon))))
                            {
                                //Yes critical so exit
                                vCuttingDeepWater = -1;
                                break;
                            }
                            else
                            {
                                // continue..depending on how many we cut even/odd it can still be within
                                vCuttingDeepWater = 1 - vCuttingDeepWater;
                            }
                        }
                    }
                }
            }

            return vCuttingDeepWater;
        }


        public Int32 IsVectorCuttingCoast(Int32 iTrippleSType, Single iXp1, Single iYp1, Single iXp2, Single iYp2)
        {

            const Single cCutEpsilon = 12e-7f;  //6e-7 seems to be too small

            Single vTempN;
            Single vTempNInv;
            Single vTempUz;
            Single vTempLz;
            Single vTempU;
            Single vTempL;

            Single vAx;
            Single vAy;
            Single vBx;
            Single vBy;

            Single vCx = iXp1;
            Single vCy = iYp1;
            Single vDx = iXp2;
            Single vDy = iYp2;

            Int32 vCuttingCoast = 0; // -1 critical cutting edge, 0 = cutting an even number of lines, 1 = cutting an odd number of vectros

            foreach (tLine vLine in MasksCommon.mForCutCheckCoastLines[iTrippleSType])
            {
                vAx = vLine.mX1;
                vAy = vLine.mY1;
                vBx = vLine.mX2;
                vBy = vLine.mY2;
                vTempN = (vBx - vAx) * (vDy - vCy) - (vBy - vAy) * (vDx - vCx);
                if (vTempN != 0.0)
                {
                    vTempNInv = 1.0f / vTempN;
                    vTempUz = -vCy * vDx + vCx * vDy - (vDy - vCy) * vAx + (vDx - vCx) * vAy;
                    vTempU = vTempUz * vTempNInv;

                    if ((vTempU > 0.0f) && (vTempU < 1.0f))
                    {
                        //The cut point is within the Line AB 
                        vTempLz = vAy * vBx - vAx * vBy + (vBy - vAy) * vCx - (vBx - vAx) * vCy;
                        vTempL = vTempLz * vTempNInv;
                        if ((vTempL > 0.0f) && (vTempL < 1.0f))
                        {
                            //And it is within Lin CD
                            //Is it a critical cut?
                            if (((vTempU < (cCutEpsilon)) && (vTempU > (-cCutEpsilon))) ||
                                ((vTempU < (1.0f + cCutEpsilon)) && (vTempU > (1.0f - cCutEpsilon))))
                            {
                                //Yes critical so exit
                                vCuttingCoast = -1;
                                break;
                            }
                            else
                            {
                                // continue..depending on how many we cut even/odd it can still be within
                                vCuttingCoast = 1 - vCuttingCoast;
                            }
                        }
                    }
                }
            }

            return vCuttingCoast;
        }


        public Int32 IsVectorCuttingAnotherVector(tLine iLine, Single iXp1, Single iYp1, Single iXp2, Single iYp2)
        {

            const Single cCutEpsilon = 12e-7f;  //6e-7 seems to be too small

            Single vTempN;
            Single vTempNInv;
            Single vTempUz;
            Single vTempLz;
            Single vTempU;
            Single vTempL;

            Single vAx;
            Single vAy;
            Single vBx;
            Single vBy;

            Single vCx = iXp1;
            Single vCy = iYp1;
            Single vDx = iXp2;
            Single vDy = iYp2;

            Int32 vCuttingCoast = 0; // -1 critical cutting edge, 0 = cutting an even number of lines, 1 = cutting an odd number of vectros

            vAx = iLine.mX1;
            vAy = iLine.mY1;
            vBx = iLine.mX2;
            vBy = iLine.mY2;
            vTempN = (vBx - vAx) * (vDy - vCy) - (vBy - vAy) * (vDx - vCx);

            if (vTempN != 0.0)
            {
                vTempNInv = 1.0f / vTempN;
                vTempUz = -vCy * vDx + vCx * vDy - (vDy - vCy) * vAx + (vDx - vCx) * vAy;
                vTempU = vTempUz * vTempNInv;

                if ((vTempU > 0.0f) && (vTempU < 1.0f))
                {
                    //The cut point is within the Line AB 
                    vTempLz = vAy * vBx - vAx * vBy + (vBy - vAy) * vCx - (vBx - vAx) * vCy;
                    vTempL = vTempLz * vTempNInv;
                    if ((vTempL > 0.0f) && (vTempL < 1.0f))
                    {
                        //And it is within Lin CD
                        //Is it a critical cut?
                        if (((vTempU < (cCutEpsilon)) && (vTempU > (-cCutEpsilon))) ||
                            ((vTempU < (1.0f + cCutEpsilon)) && (vTempU > (1.0f - cCutEpsilon))))
                        {
                            //Yes critical so exit
                            vCuttingCoast = -1;
                        }
                        else
                        {
                            // continue..depending on how many we cut even/odd it can still be within
                            vCuttingCoast = 1 - vCuttingCoast;
                        }
                    }
                }
            }

            return vCuttingCoast;
        }


        public void DrawUserVisibleVectorsIntoWorkBitmap()
        {
            try
            {

                Color vCoastColor = Color.FromArgb(255, 255, 0, 0);
                Color vDeepWaterColor = Color.FromArgb(255, 0, 0, 255);
                Pen vCoastPen = new Pen(vCoastColor, 1.0f);
                Pen vDeepWaterPen = new Pen(vDeepWaterColor, 1.0f);
                Pen vTransitionPolygonsPen = new Pen(Color.FromArgb(255, 50, 128, 255), 1.0f);
                Pen vPoolPolygonsPen = new Pen(Color.FromArgb(255, 200, 128, 255), 1.0f);

                for (Int32 vTrippleSType = 0; vTrippleSType < (Int32)(MasksConfig.tTransitionType.eSize); vTrippleSType++)
                {
                    foreach (tLine vLine in MasksCommon.mCoastLines[vTrippleSType])
                    {
                        tLine vCuttedLine = CutLineOnBitmap(vLine);
                        mAreaGraphics.DrawLine(vCoastPen, vCuttedLine.mX1, vCuttedLine.mY1, vCuttedLine.mX2, vCuttedLine.mY2); //C# .NET 2.0 bug makes this required (incomplete lines specially when they are long and go outside the bmp)
                    }

                    foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTrippleSType])
                    {
                        mAreaGraphics.DrawEllipse(vCoastPen, vPoint.mX - 4.0f, vPoint.mY - 4.0f, 8.0f, 8.0f);
                        mAreaGraphics.DrawEllipse(vCoastPen, vPoint.mX - 0.51f, vPoint.mY - 0.51f, 1.02f, 1.02f); //C# .NET 2.0 bug
                    }

                    foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTrippleSType])
                    {
                        tLine vCuttedLine = CutLineOnBitmap(vLine);
                        mAreaGraphics.DrawLine(vDeepWaterPen, vCuttedLine.mX1, vCuttedLine.mY1, vCuttedLine.mX2, vCuttedLine.mY2); //C# .NET 2.0 bug
                    }

                    foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTrippleSType])
                    {
                        mAreaGraphics.DrawEllipse(vDeepWaterPen, vPoint.mX - 4.0f, vPoint.mY - 4.0f, 8.0f, 8.0f);
                        mAreaGraphics.DrawEllipse(vDeepWaterPen, vPoint.mX - 0.51f, vPoint.mY - 0.51f, 1.02f, 1.02f); //C# .NET 2.0 bug
                    }

                    foreach (PointF[] vPolygon in MasksCommon.mTransitionPolygons[vTrippleSType])
                    {
                        PointF[] vCutPolygon = CutPolygonOnBitmap(vPolygon);
                        mAreaGraphics.DrawPolygon(vPoolPolygonsPen, vCutPolygon); //C# .NET 2.0 bug
                    }
                }

                foreach (tPoolPolygon vPoolPolygon in MasksCommon.mPoolPolygons)
                {
                    PointF[] vCutPolygon = CutPolygonOnBitmap(vPoolPolygon.mPolygon);
                    mAreaGraphics.DrawPolygon(vTransitionPolygonsPen, vCutPolygon); //C# .NET 2.0 bug
                }
            }
            catch
            {
                //MaybeShowErrorInStatus
            }
        }


        public void DrawVectorsForWaterRegions(Int32 iTrippleSType)
        {
            //The Green Field is the Command Mask!
            // A R G B
            Color vColor = Color.FromArgb(255, 0, (Int32)(cVectorCommandColorValue), 0);
            Pen vVectorPen = new Pen(vColor, 1.0f);

            foreach (tLine vLine in MasksCommon.mCoastLines[iTrippleSType])
            {
                tLine vCuttedLine = CutLineOnBitmap(vLine);
                mAreaGraphics.DrawLine(vVectorPen, vCuttedLine.mX1, vCuttedLine.mY1, vCuttedLine.mX2, vCuttedLine.mY2); //C# .NET 2.0 bug makes it required to cut the line self on the bitmap borders
            }

            foreach (tLine vLine in MasksCommon.mDeepWaterLines[iTrippleSType])
            {
                tLine vCuttedLine = CutLineOnBitmap(vLine);
                mAreaGraphics.DrawLine(vVectorPen, vCuttedLine.mX1, vCuttedLine.mY1, vCuttedLine.mX2, vCuttedLine.mY2); //C# .NET 2.0 bug
            }

            //The above accidential creates holes at the start or stop points(C# bug or precision?) where the flood fill floods through
            //so we have to set the Point also.
            //SetPixel requires Coord checks therefore we use rectangle
            //Rectangle and Ellipse however seems to need at least -0.1f and 0.2f in size that it works in the detected critical points
            //That large size creats a little unproper look.
            foreach (tPoint vPoint in MasksCommon.mCoastPoints[iTrippleSType])
            {
                mAreaGraphics.DrawEllipse(vVectorPen, vPoint.mX - 0.51f, vPoint.mY - 0.51f, 1.02f, 1.02f); //C# .NET 2.0 bug
            }
            foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[iTrippleSType])
            {
                mAreaGraphics.DrawEllipse(vVectorPen, vPoint.mX - 0.51f, vPoint.mY - 0.51f, 1.02f, 1.02f); //C# .NET 2.0 bug
            }
        }

        public void DrawTransitionPolygons(Int32 iTrippleSType)
        {
            //The Green Field is the Command Mask!
            // A R G B
            SolidBrush vPolygonBrush = new SolidBrush(Color.FromArgb(255, 0,(Int32)(cWaterCommandColorValue), 0));
            foreach (PointF [] vPolygon in MasksCommon.mTransitionPolygons[iTrippleSType])
            {
                PointF[] vCutPolygon = CutPolygonOnBitmap(vPolygon);
                mAreaGraphics.FillPolygon(vPolygonBrush, vCutPolygon); //C# .NET 2.0 bug beter go save and to for filled polygon also. (for not filled it is required) 
            }
        }

        public void DrawPoolPolygons()
        {
            //The Green Field is the Command Mask!
            // A R G B

            SolidBrush vPoolWaterBrush = new SolidBrush(Color.FromArgb(255, 0, (Int32)(cPoolWaterCommandColorValue), 0));
            SolidBrush vPoolLandBrush = new SolidBrush(Color.FromArgb(255, 0, (Int32)(cPoolLandCommandColorValue), 0));
            SolidBrush vPoolBlendBrush = new SolidBrush(Color.FromArgb(255, 0, (Int32)(cPoolBlendCommandColorValue), 0));

            foreach (tPoolPolygon vPoolPolygon in MasksCommon.mPoolPolygons)
            {
                PointF[] vCutPolygon = CutPolygonOnBitmap(vPoolPolygon.mPolygon); //C# .NET 2.0 bug

                if (vPoolPolygon.mPoolType == MasksConfig.tPoolType.eWaterPool)
                {
                    mAreaGraphics.FillPolygon(vPoolWaterBrush, vCutPolygon); //C# .NET 2.0 bug
                }
                else if (vPoolPolygon.mPoolType == MasksConfig.tPoolType.eLandPool)
                {
                    mAreaGraphics.FillPolygon(vPoolLandBrush, vCutPolygon);
                }
                else if (vPoolPolygon.mPoolType == MasksConfig.tPoolType.eBlendPool)
                {
                    mAreaGraphics.FillPolygon(vPoolBlendBrush, vCutPolygon);
                }
            }
        }

        // this is ported almost verbatim from Ortho4XP's code. I find it very confusing code to read
        // TODO: try to refactor this into a clearer format. Also, use camel case
        private List<PointF[]> readMeshFile(string meshFilePath)
        {
            System.IO.StreamReader f_mesh = new System.IO.StreamReader(meshFilePath);
            string[] lineContents = f_mesh.ReadLine().Trim().Split();
            float mesh_version = Convert.ToSingle(lineContents[lineContents.Length - 1]);
            int has_water = mesh_version >= 1.3f ? 7 : 3;
            // skip ahead 3
            for (int i = 0; i < 3; i++)
            {
                f_mesh.ReadLine();
            }
            int nbr_pt_in = Convert.ToInt32(f_mesh.ReadLine());
            double[] pt_in = new double[5 * nbr_pt_in];
            for (int i = 0; i < nbr_pt_in; i++)
            {
                int lc = 0;
                lineContents = f_mesh.ReadLine().Split();
                for (int j = 5 * i; j < 5 * i + 3; j++)
                {
                    pt_in[j] = Convert.ToDouble(lineContents[lc]);
                    lc++;
                }
            }
            // skip ahead 3
            for (int i = 0; i < 3; i++)
            {
                f_mesh.ReadLine();
            }
            for (int i = 0; i < nbr_pt_in; i++)
            {
                int lc = 0;
                lineContents = f_mesh.ReadLine().Split();
                for (int j = 5 * i + 3; j < 5 * i + 5; j++)
                {
                    pt_in[j] = Convert.ToDouble(lineContents[lc]);
                    lc++;
                }
            }
            // skip ahead 2
            for (int i = 0; i < 2; i++)
            {
                f_mesh.ReadLine();
            }
            int nbr_tri_in = Convert.ToInt32(f_mesh.ReadLine());

            List<PointF[]> tris = new List<PointF[]>();

            for (int i = 0; i < nbr_tri_in; i++)
            {
                lineContents = f_mesh.ReadLine().Split();
                int n1 = Convert.ToInt32(lineContents[0]) - 1;
                int n2 = Convert.ToInt32(lineContents[1]) - 1;
                int n3 = Convert.ToInt32(lineContents[2]) - 1;
                int tri_type = Convert.ToInt32(lineContents[3]) - 1;
                tri_type += 1;

                bool use_masks_for_inland = true; // possibly allow for changing in the future?
                if (tri_type == 0 || (tri_type & has_water) == 0 || ((tri_type & has_water) < 2 && !use_masks_for_inland))
                {
                    continue;
                }
                float lon1 = (float) pt_in[5 * n1];
                float lat1 = (float) pt_in[5 * n1 + 1];
                float lon2 = (float) pt_in[5 * n2];
                float lat2 = (float) pt_in[5 * n2 + 1];
                float lon3 = (float) pt_in[5 * n3];
                float lat3 = (float) pt_in[5 * n3 + 1];

                var tri = new PointF[] {
                    new PointF(lon1, lat1),
                    new PointF(lon2, lat2),
                    new PointF(lon3, lat3),
                    new PointF(lon1, lat1),
                };

                tris.Add(tri);
            }

            return tris;
        }

        private List<PointF[]> ReadAllMeshFiles()
        {
            double startLong = MasksConfig.mAreaNWCornerLongitude < MasksConfig.mAreaSECornerLongitude ? MasksConfig.mAreaNWCornerLongitude : MasksConfig.mAreaSECornerLongitude;
            double stopLong = startLong == MasksConfig.mAreaNWCornerLongitude ? MasksConfig.mAreaSECornerLongitude : MasksConfig.mAreaNWCornerLongitude;
            double startLat = MasksConfig.mAreaNWCornerLatitude < MasksConfig.mAreaSECornerLatitude ? MasksConfig.mAreaNWCornerLatitude : MasksConfig.mAreaSECornerLatitude;
            double stopLat = startLat == MasksConfig.mAreaNWCornerLatitude ? MasksConfig.mAreaSECornerLatitude : MasksConfig.mAreaNWCornerLatitude;
            List<double[]> tilesDownloaded = CommonFunctions.GetTilesToDownload(startLong, stopLong, startLat, stopLat);


            List<PointF[]> allTris = new List<PointF[]>();
            foreach (double[] tile in tilesDownloaded)
            {
                string meshPath = CommonFunctions.GetMeshFileFullPath(MasksConfig.mWorkFolder, tile);
                List<PointF[]> tris = readMeshFile(meshPath);
                allTris.AddRange(tris);
            }

            return allTris;
        }

        private tXYCoord ConvertPixelToXYLatLong(tXYCoord iXYPixel)
        {
            tXYCoord vLatLongCoord;

            Double vPixelPerLongitude = Convert.ToDouble(MasksConfig.mAreaPixelCountInX) / (MasksConfig.mAreaSECornerLongitude - MasksConfig.mAreaNWCornerLongitude);
            Double vPixelPerLatitude = Convert.ToDouble(MasksConfig.mAreaPixelCountInY) / (MasksConfig.mAreaNWCornerLatitude - MasksConfig.mAreaSECornerLatitude);

            vLatLongCoord.mX = (iXYPixel.mX / vPixelPerLongitude) + MasksConfig.mAreaNWCornerLongitude;
            vLatLongCoord.mY = MasksConfig.mAreaNWCornerLatitude - (iXYPixel.mY / vPixelPerLatitude);

            return vLatLongCoord;
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

        public tWaterRegionType CalculateWaterTransitionRegionType(Int32 iTrippleSType, Single iXp, Single iYp)
        {
            tWaterRegionType vWaterRegionType;

            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;

            vCoastMinPoint = GetCoastMinPoint(iTrippleSType, iXp, iYp);
            vDeepWaterMinPoint = GetDeepWaterMinPoint(iTrippleSType, iXp, iYp);

            Int32 vCutsALine = 0;

            vWaterRegionType = tWaterRegionType.eLand; //Default Land

            if ((vCoastMinPoint.mValid) && (vDeepWaterMinPoint.mValid))
            {
                vCutsALine = IsVectorCuttingDeepWater(iTrippleSType, iXp, iYp, vCoastMinPoint.mX, vCoastMinPoint.mY);
                if (vCutsALine == -1)
                {
                    vCutsALine = IsVectorCuttingDeepWater(iTrippleSType, iXp + 0.15f, iYp + 0.15f, vCoastMinPoint.mX, vCoastMinPoint.mY);
                }
                if (vCutsALine == -1)
                {
                    vCutsALine = IsVectorCuttingDeepWater(iTrippleSType, iXp - 0.15f, iYp - 0.15f, vCoastMinPoint.mX, vCoastMinPoint.mY);
                }
                if (vCutsALine == -1)
                {
                    vCutsALine = IsVectorCuttingDeepWater(iTrippleSType, iXp + 0.15f, iYp -0.15f, vCoastMinPoint.mX, vCoastMinPoint.mY);
                }
                if (vCutsALine == -1)
                {
                    vCutsALine = IsVectorCuttingDeepWater(iTrippleSType, iXp - 0.15f, iYp + 0.15f, vCoastMinPoint.mX, vCoastMinPoint.mY);
                }
                if (vCutsALine == -1)
                {
                    vWaterRegionType = tWaterRegionType.eUndetected;
                }
                else
                {
                    if (vCutsALine == 0)
                    {
                        vCutsALine = IsVectorCuttingCoast(iTrippleSType, iXp, iYp, vDeepWaterMinPoint.mX, vDeepWaterMinPoint.mY);
                        if (vCutsALine == -1)
                        {
                            vCutsALine = IsVectorCuttingCoast(iTrippleSType, iXp + 0.15f, iYp + 0.15f, vDeepWaterMinPoint.mX, vDeepWaterMinPoint.mY);
                        }
                        if (vCutsALine == -1)
                        {
                            vCutsALine = IsVectorCuttingCoast(iTrippleSType, iXp - 0.15f, iYp - 0.15f, vDeepWaterMinPoint.mX, vDeepWaterMinPoint.mY);
                        }
                        if (vCutsALine == -1)
                        {
                            vCutsALine = IsVectorCuttingCoast(iTrippleSType, iXp + 0.15f, iYp - 0.15f, vDeepWaterMinPoint.mX, vDeepWaterMinPoint.mY);
                        }
                        if (vCutsALine == -1)
                        {
                            vCutsALine = IsVectorCuttingCoast(iTrippleSType, iXp - 0.15f, iYp + 0.15f, vDeepWaterMinPoint.mX, vDeepWaterMinPoint.mY);
                        } 
                    }
                    if (vCutsALine == -1)
                    {
                        vWaterRegionType = tWaterRegionType.eUndetected;
                    }
                    else if (vCutsALine == 1)
                    {
                        //there are lines in between so check for the shortest and that's it.
                        if (vCoastMinPoint.mSquareDistance > vDeepWaterMinPoint.mSquareDistance)
                        {
                            vWaterRegionType = tWaterRegionType.eWater;
                        } //else it's Land.. but we alerady set
                    }
                    else
                    {
                        vWaterRegionType = tWaterRegionType.eTransition;
                    }
                }

            } //else it's Land.. but we alerady set

            return vWaterRegionType;

        }

        public void CalculateTransitionSquares(Int32 iTrippleSType, Single iXp, Single iYp, Boolean iUseSlice, Boolean iUseCells, ref Single oSquare1, ref Single oSquare2)
        {

            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;

            if (iUseSlice)
            {
                if (iUseCells)
                {
                    vCoastMinPoint = GetCellsCoastMinPoint(iTrippleSType, iXp, iYp);
                    vDeepWaterMinPoint = GetCellsDeepWaterMinPoint(iTrippleSType, iXp, iYp);
                }
                else
                {
                    vCoastMinPoint = GetSliceCoastMinPoint(iTrippleSType, iXp, iYp);
                    vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(iTrippleSType, iXp, iYp);
                }
            }
            else
            {
                vCoastMinPoint = GetCoastMinPoint(iTrippleSType, iXp, iYp);
                vDeepWaterMinPoint = GetDeepWaterMinPoint(iTrippleSType, iXp, iYp);
            }

            if ((vCoastMinPoint.mValid) && (vDeepWaterMinPoint.mValid))
            {
                oSquare1 = vCoastMinPoint.mSquareDistance;
                oSquare2 = vDeepWaterMinPoint.mSquareDistance;
            }
            else
            {
                oSquare1 = 0.0f;
                oSquare2 = 1.0f;
            }
        }


        public Single CalculateTransitionSquareRelationFactor(Int32 iTrippleSType, Single iXp, Single iYp, Boolean iUseSlice, Boolean iUseCells)
        {
            Single vSquareRelationFactor = 0.0f;
          
            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;

            if (iUseSlice)
            {
                if (iUseCells)
                {
                    vCoastMinPoint = GetCellsCoastMinPoint(iTrippleSType, iXp, iYp);
                    vDeepWaterMinPoint = GetCellsDeepWaterMinPoint(iTrippleSType, iXp, iYp);
                }
                else
                {
                    vCoastMinPoint = GetSliceCoastMinPoint(iTrippleSType, iXp, iYp);
                    vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(iTrippleSType, iXp, iYp);
                }
            }
            else
            {
                vCoastMinPoint = GetCoastMinPoint(iTrippleSType, iXp, iYp);
                vDeepWaterMinPoint = GetDeepWaterMinPoint(iTrippleSType, iXp, iYp);
            }

            if ((vCoastMinPoint.mValid) && (vDeepWaterMinPoint.mValid))
            {
                Single vSquareDistSum = vCoastMinPoint.mSquareDistance + vDeepWaterMinPoint.mSquareDistance;

                if (vSquareDistSum != 0.0)
                {
                    //round Toward Water
                    vSquareRelationFactor = vCoastMinPoint.mSquareDistance / vSquareDistSum;
                }

            }

            return vSquareRelationFactor;

        }


        public void SliceReduceLinesCount(Int32 iX1, Int32 iY1,  Int32 iX2, Int32 iY2)
        {
            Single vCoastYMin;
            Single vCoastYMax;
            Single vCoastXMin;
            Single vCoastXMax;

            Single vDeepWaterYMin;
            Single vDeepWaterYMax;
            Single vDeepWaterXMin;
            Single vDeepWaterXMax;

            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;

            for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
            {

                vCoastYMin = (Single)(iY1);
                vCoastYMax = (Single)(iY2);
                vCoastXMin = (Single)(iX1);
                vCoastXMax = (Single)(iX2);

                vDeepWaterYMin = (Single)(iY1);
                vDeepWaterYMax = (Single)(iY2);
                vDeepWaterXMin = (Single)(iX1);
                vDeepWaterXMax = (Single)(iX2);

                MasksCommon.mSliceCoastLines[vTransitionType].Clear();
                MasksCommon.mSliceDeepWaterLines[vTransitionType].Clear();
                MasksCommon.mSliceCoastPoints[vTransitionType].Clear();
                MasksCommon.mSliceDeepWaterPoints[vTransitionType].Clear();

                //Top Line
                for (Int32 vX = iX1; vX <= iX2; vX++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(vX), (Single)(iY1));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mY < vCoastYMin)
                        {
                            vCoastYMin = vCoastMinPoint.mY;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(vX), (Single)(iY1));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mY < vDeepWaterYMin)
                        {
                            vDeepWaterYMin = vDeepWaterMinPoint.mY;
                        }
                    }
                }


                //Bottom Line
                for (Int32 vX = iX1; vX <= iX2; vX++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(vX), (Single)(iY2));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mY > vCoastYMax)
                        {
                            vCoastYMax = vCoastMinPoint.mY;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(vX), (Single)(iY2));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mY > vDeepWaterYMax)
                        {
                            vDeepWaterYMax = vDeepWaterMinPoint.mY;
                        }
                    }
                }

                //Left Line
                for (Int32 vY = iY1; vY <= iY2; vY++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(iX1), (Single)(vY));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mX < vCoastXMin)
                        {
                            vCoastXMin = vCoastMinPoint.mX;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(iX1), (Single)(vY));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mX < vDeepWaterXMin)
                        {
                            vDeepWaterXMin = vDeepWaterMinPoint.mX;
                        }
                    }
                }

                //Right Line
                for (Int32 vY = iY1; vY <= iY2; vY++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(iX2), (Single)(vY));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mX > vCoastXMax)
                        {
                            vCoastXMax = vCoastMinPoint.mX;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(iX2), (Single)(vY));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mX > vDeepWaterXMax)
                        {
                            vDeepWaterXMax = vDeepWaterMinPoint.mX;
                        }
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                {
                    if ((vPoint.mY >= vCoastYMin) && (vPoint.mY <= vCoastYMax) && (vPoint.mX >= vCoastXMin) && (vPoint.mX <= vCoastXMax))
                    {
                        MasksCommon.mSliceCoastPoints[vTransitionType].Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                {
                    if (((vLine.mY1 >= vCoastYMin) || (vLine.mY2 >= vCoastYMin)) &&
                        ((vLine.mY1 <= vCoastYMax) || (vLine.mY2 <= vCoastYMax)) &&
                        ((vLine.mX1 >= vCoastXMin) || (vLine.mX2 >= vCoastXMin)) &&
                        ((vLine.mX1 <= vCoastXMax) || (vLine.mX2 <= vCoastXMax)))
                    {
                        MasksCommon.mSliceCoastLines[vTransitionType].Add(vLine);
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                {
                    if ((vPoint.mY >= vDeepWaterYMin) && (vPoint.mY <= vDeepWaterYMax) && (vPoint.mX >= vDeepWaterXMin) && (vPoint.mX <= vDeepWaterXMax))
                    {
                        MasksCommon.mSliceDeepWaterPoints[vTransitionType].Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                {
                    if (((vLine.mY1 >= vDeepWaterYMin) || (vLine.mY2 >= vDeepWaterYMin)) &&
                        ((vLine.mY1 <= vDeepWaterYMax) || (vLine.mY2 <= vDeepWaterYMax)) &&
                        ((vLine.mX1 >= vDeepWaterXMin) || (vLine.mX2 >= vDeepWaterXMin)) &&
                        ((vLine.mX1 <= vDeepWaterXMax) || (vLine.mX2 <= vDeepWaterXMax)))
                    {
                        MasksCommon.mSliceDeepWaterLines[vTransitionType].Add(vLine);
                    }
                }
            }

        }


        public void CellsReduceLinesCount(Int32 iY1, Int32 iY2)
        {
            Single vCoastYMin;
            Single vCoastYMax;
            Single vCoastXMin;
            Single vCoastXMax;

            Single vDeepWaterYMin;
            Single vDeepWaterYMax;
            Single vDeepWaterXMin;
            Single vDeepWaterXMax;

            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;

            Int32 vNrOfCells = (mPixelCountInX / cXCellsSpace) + 1;


             for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
             {

                for (Int32 vCell = 0; vCell < vNrOfCells; vCell++)
                {
                    Int32 iX1 = vCell * cXCellsSpace;
                    Int32 iX2 = (vCell + 1) * cXCellsSpace - 1;

                    vCoastYMin = (Single)(iY1);
                    vCoastYMax = (Single)(iY2);
                    vCoastXMin = (Single)(iX1);
                    vCoastXMax = (Single)(iX2);

                    vDeepWaterYMin = (Single)(iY1);
                    vDeepWaterYMax = (Single)(iY2);
                    vDeepWaterXMin = (Single)(iX1);
                    vDeepWaterXMax = (Single)(iX2);

                    mCellsCoastLines[vTransitionType, vCell].Clear();
                    mCellsDeepWaterLines[vTransitionType, vCell].Clear();
                    mCellsCoastPoints[vTransitionType, vCell].Clear();
                    mCellsDeepWaterPoints[vTransitionType, vCell].Clear();

                    //Top Line
                    for (Int32 vX = iX1; vX <= iX2; vX++)
                    {
                        vCoastMinPoint = GetSliceCoastMinPoint(vTransitionType, (Single)(vX), (Single)(iY1));

                        if (vCoastMinPoint.mValid)
                        {
                            if (vCoastMinPoint.mY < vCoastYMin)
                            {
                                vCoastYMin = vCoastMinPoint.mY;
                            }
                        }

                        vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(vTransitionType, (Single)(vX), (Single)(iY1));

                        if (vDeepWaterMinPoint.mValid)
                        {
                            if (vDeepWaterMinPoint.mY < vDeepWaterYMin)
                            {
                                vDeepWaterYMin = vDeepWaterMinPoint.mY;
                            }
                        }
                    }


                    //Bottom Line
                    for (Int32 vX = iX1; vX <= iX2; vX++)
                    {
                        vCoastMinPoint = GetSliceCoastMinPoint(vTransitionType, (Single)(vX), (Single)(iY2));

                        if (vCoastMinPoint.mValid)
                        {
                            if (vCoastMinPoint.mY > vCoastYMax)
                            {
                                vCoastYMax = vCoastMinPoint.mY;
                            }
                        }

                        vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(vTransitionType, (Single)(vX), (Single)(iY2));

                        if (vDeepWaterMinPoint.mValid)
                        {
                            if (vDeepWaterMinPoint.mY > vDeepWaterYMax)
                            {
                                vDeepWaterYMax = vDeepWaterMinPoint.mY;
                            }
                        }
                    }

                    //Left Line
                    for (Int32 vY = iY1; vY <= iY2; vY++)
                    {
                        vCoastMinPoint = GetSliceCoastMinPoint(vTransitionType, (Single)(iX1), (Single)(vY));

                        if (vCoastMinPoint.mValid)
                        {
                            if (vCoastMinPoint.mX < vCoastXMin)
                            {
                                vCoastXMin = vCoastMinPoint.mX;
                            }
                        }

                        vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(vTransitionType, (Single)(iX1), (Single)(vY));

                        if (vDeepWaterMinPoint.mValid)
                        {
                            if (vDeepWaterMinPoint.mX < vDeepWaterXMin)
                            {
                                vDeepWaterXMin = vDeepWaterMinPoint.mX;
                            }
                        }
                    }

                    //Right Line
                    for (Int32 vY = iY1; vY <= iY2; vY++)
                    {
                        vCoastMinPoint = GetSliceCoastMinPoint(vTransitionType, (Single)(iX2), (Single)(vY));

                        if (vCoastMinPoint.mValid)
                        {
                            if (vCoastMinPoint.mX > vCoastXMax)
                            {
                                vCoastXMax = vCoastMinPoint.mX;
                            }
                        }

                        vDeepWaterMinPoint = GetSliceDeepWaterMinPoint(vTransitionType, (Single)(iX2), (Single)(vY));

                        if (vDeepWaterMinPoint.mValid)
                        {
                            if (vDeepWaterMinPoint.mX > vDeepWaterXMax)
                            {
                                vDeepWaterXMax = vDeepWaterMinPoint.mX;
                            }
                        }
                    }

                    foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                    {
                        if ((vPoint.mY >= vCoastYMin) && (vPoint.mY <= vCoastYMax) && (vPoint.mX >= vCoastXMin) && (vPoint.mX <= vCoastXMax))
                        {
                            mCellsCoastPoints[vTransitionType, vCell].Add(vPoint);
                        }
                    }


                    foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                    {
                        if (((vLine.mY1 >= vCoastYMin) || (vLine.mY2 >= vCoastYMin)) &&
                            ((vLine.mY1 <= vCoastYMax) || (vLine.mY2 <= vCoastYMax)) &&
                            ((vLine.mX1 >= vCoastXMin) || (vLine.mX2 >= vCoastXMin)) &&
                            ((vLine.mX1 <= vCoastXMax) || (vLine.mX2 <= vCoastXMax)))
                        {
                            mCellsCoastLines[vTransitionType, vCell].Add(vLine);
                        }
                    }

                    foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                    {
                        if ((vPoint.mY >= vDeepWaterYMin) && (vPoint.mY <= vDeepWaterYMax) && (vPoint.mX >= vDeepWaterXMin) && (vPoint.mX <= vDeepWaterXMax))
                        {
                            mCellsDeepWaterPoints[vTransitionType, vCell].Add(vPoint);
                        }
                    }


                    foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                    {
                        if (((vLine.mY1 >= vDeepWaterYMin) || (vLine.mY2 >= vDeepWaterYMin)) &&
                            ((vLine.mY1 <= vDeepWaterYMax) || (vLine.mY2 <= vDeepWaterYMax)) &&
                            ((vLine.mX1 >= vDeepWaterXMin) || (vLine.mX2 >= vDeepWaterXMin)) &&
                            ((vLine.mX1 <= vDeepWaterXMax) || (vLine.mX2 <= vDeepWaterXMax)))
                        {
                            mCellsDeepWaterLines[vTransitionType, vCell].Add(vLine);
                        }
                    }
                }
            }

        }


        public void ReduceLinesCount()
        {

            List<tPoint> vCoastPoints     = new List<tPoint>();
            List<tLine>  vCoastLines      = new List<tLine>();
            List<tPoint> vDeepWaterPoints = new List<tPoint>();
            List<tLine>  vDeepWaterLines  = new List<tLine>();

            Single vCoastYMin;
            Single vCoastYMax;
            Single vCoastXMin;
            Single vCoastXMax;

            Single vDeepWaterYMin;
            Single vDeepWaterYMax;
            Single vDeepWaterXMin;
            Single vDeepWaterXMax;

            tPointWithSquareDistance vCoastMinPoint;
            tPointWithSquareDistance vDeepWaterMinPoint;


            for (Int32 vTransitionType = 0; vTransitionType < (Int32)(MasksConfig.tTransitionType.eSize); vTransitionType++)
            {

                vCoastYMin = 0.0f;
                vCoastYMax = (Single)(mPixelCountInY) - 1.0f;
                vCoastXMin = 0.0f;
                vCoastXMax = (Single)(mPixelCountInX) - 1.0f;

                vDeepWaterYMin = 0.0f;
                vDeepWaterYMax = (Single)(mPixelCountInY) - 1.0f;
                vDeepWaterXMin = 0.0f;
                vDeepWaterXMax = (Single)(mPixelCountInX) - 1.0f;

                //Top Line
                for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(vX), 0.0f);

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mY < vCoastYMin)
                        {
                            vCoastYMin = vCoastMinPoint.mY;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(vX), 0.0f);

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mY < vDeepWaterYMin)
                        {
                            vDeepWaterYMin = vDeepWaterMinPoint.mY;
                        }
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                {
                    if (vPoint.mY >= vCoastYMin)
                    {
                        vCoastPoints.Add(vPoint);
                    }
                }
               

                foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                {
                    if ((vLine.mY1 >= vCoastYMin) || (vLine.mY2 >= vCoastYMin))
                    {
                        vCoastLines.Add(vLine);
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                {
                    if (vPoint.mY >= vDeepWaterYMin)
                    {
                        vDeepWaterPoints.Add(vPoint);
                    }
                }

                foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                {
                    if ((vLine.mY1 >= vDeepWaterYMin) || (vLine.mY2 >= vDeepWaterYMin))
                    {
                        vDeepWaterLines.Add(vLine);
                    }
                }

                MasksCommon.mCoastPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vCoastPoints)
                {
                    MasksCommon.mCoastPoints[vTransitionType].Add(vPoint);
                }
                vCoastPoints.Clear();

                MasksCommon.mCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in vCoastLines)
                {
                    MasksCommon.mCoastLines[vTransitionType].Add(vLine);
                }
                vCoastLines.Clear();

                MasksCommon.mDeepWaterPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vDeepWaterPoints)
                {
                    MasksCommon.mDeepWaterPoints[vTransitionType].Add(vPoint);
                }
                vDeepWaterPoints.Clear();

                MasksCommon.mDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in vDeepWaterLines)
                {
                    MasksCommon.mDeepWaterLines[vTransitionType].Add(vLine);
                }
                vDeepWaterLines.Clear();


                //Bottom Line
                for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(vX), (Single)(mPixelCountInY) - 1.0f);

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mY > vCoastYMax)
                        {
                            vCoastYMax = vCoastMinPoint.mY;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(vX), (Single)(mPixelCountInY) - 1.0f);

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mY > vDeepWaterYMax)
                        {
                            vDeepWaterYMax = vDeepWaterMinPoint.mY;
                        }
                    }
                }


                foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                {
                    if (vPoint.mY <= vCoastYMax)
                    {
                        vCoastPoints.Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                {
                    if ((vLine.mY1 <= vCoastYMax) || (vLine.mY2 <= vCoastYMax))
                    {
                        vCoastLines.Add(vLine);
                    }
                }


                foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                {
                    if (vPoint.mY <= vDeepWaterYMax)
                    {
                        vDeepWaterPoints.Add(vPoint);
                    }
                }

                foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                {
                    if ((vLine.mY1 <= vDeepWaterYMax) || (vLine.mY2 <= vDeepWaterYMax))
                    {
                        vDeepWaterLines.Add(vLine);
                    }
                }

                MasksCommon.mCoastPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vCoastPoints)
                {
                    MasksCommon.mCoastPoints[vTransitionType].Add(vPoint);
                }
                vCoastPoints.Clear();

                MasksCommon.mCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in vCoastLines)
                {
                    MasksCommon.mCoastLines[vTransitionType].Add(vLine);
                }
                vCoastLines.Clear();

                MasksCommon.mDeepWaterPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vDeepWaterPoints)
                {
                    MasksCommon.mDeepWaterPoints[vTransitionType].Add(vPoint);
                }
                vDeepWaterPoints.Clear();

                MasksCommon.mDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in vDeepWaterLines)
                {
                    MasksCommon.mDeepWaterLines[vTransitionType].Add(vLine);
                }
                vDeepWaterLines.Clear();

                //Left Line
                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, 0.0f, (Single)(vY));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mX < vCoastXMin)
                        {
                            vCoastXMin = vCoastMinPoint.mX;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, 0.0f, (Single)(vY));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mX < vDeepWaterXMin)
                        {
                            vDeepWaterXMin = vDeepWaterMinPoint.mX;
                        }
                    }
                }


                foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                {
                    if (vPoint.mX >= vCoastXMin)
                    {
                        vCoastPoints.Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                {
                    if ((vLine.mX1 >= vCoastXMin) || (vLine.mX2 >= vCoastXMin))
                    {
                        vCoastLines.Add(vLine);
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                {
                    if (vPoint.mX >= vDeepWaterXMin)
                    {
                        vDeepWaterPoints.Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                {
                    if ((vLine.mX1 >= vDeepWaterXMin) || (vLine.mX2 >= vDeepWaterXMin))
                    {
                        vDeepWaterLines.Add(vLine);
                    }
                }

                MasksCommon.mCoastPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vCoastPoints)
                {
                    MasksCommon.mCoastPoints[vTransitionType].Add(vPoint);
                }
                vCoastPoints.Clear();

                MasksCommon.mCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in vCoastLines)
                {
                    MasksCommon.mCoastLines[vTransitionType].Add(vLine);
                }
                vCoastLines.Clear();

                MasksCommon.mDeepWaterPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vDeepWaterPoints)
                {
                    MasksCommon.mDeepWaterPoints[vTransitionType].Add(vPoint);
                }
                vDeepWaterPoints.Clear();

                MasksCommon.mDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in vDeepWaterLines)
                {
                    MasksCommon.mDeepWaterLines[vTransitionType].Add(vLine);
                }
                vDeepWaterLines.Clear();

                //Right Line
                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vCoastMinPoint = GetCoastMinPoint(vTransitionType, (Single)(mPixelCountInX) - 1.0f, (Single)(vY));

                    if (vCoastMinPoint.mValid)
                    {
                        if (vCoastMinPoint.mX > vCoastXMax)
                        {
                            vCoastXMax = vCoastMinPoint.mX;
                        }
                    }

                    vDeepWaterMinPoint = GetDeepWaterMinPoint(vTransitionType, (Single)(mPixelCountInX) - 1.0f, (Single)(vY));

                    if (vDeepWaterMinPoint.mValid)
                    {
                        if (vDeepWaterMinPoint.mX > vDeepWaterXMax)
                        {
                            vDeepWaterXMax = vDeepWaterMinPoint.mX;
                        }
                    }
                }


                foreach (tPoint vPoint in MasksCommon.mCoastPoints[vTransitionType])
                {
                    if (vPoint.mX <= vCoastXMax)
                    {
                        vCoastPoints.Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mCoastLines[vTransitionType])
                {
                    if ((vLine.mX1 <= vCoastXMax) || (vLine.mX2 <= vCoastXMax))
                    {
                        vCoastLines.Add(vLine);
                    }
                }

                foreach (tPoint vPoint in MasksCommon.mDeepWaterPoints[vTransitionType])
                {
                    if (vPoint.mX <= vDeepWaterXMax)
                    {
                        vDeepWaterPoints.Add(vPoint);
                    }
                }


                foreach (tLine vLine in MasksCommon.mDeepWaterLines[vTransitionType])
                {
                    if ((vLine.mX1 <= vDeepWaterXMax) || (vLine.mX2 <= vDeepWaterXMax))
                    {
                        vDeepWaterLines.Add(vLine);
                    }
                }

                MasksCommon.mCoastPoints[vTransitionType].Clear();
                MasksCommon.mSliceCoastPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vCoastPoints)
                {
                    MasksCommon.mCoastPoints[vTransitionType].Add(vPoint);
                    MasksCommon.mSliceCoastPoints[vTransitionType].Add(vPoint);
                }
                vCoastPoints.Clear();

                MasksCommon.mCoastLines[vTransitionType].Clear();
                MasksCommon.mSliceCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in vCoastLines)
                {
                    MasksCommon.mCoastLines[vTransitionType].Add(vLine);
                    MasksCommon.mSliceCoastLines[vTransitionType].Add(vLine);
                }
                vCoastLines.Clear();

                MasksCommon.mDeepWaterPoints[vTransitionType].Clear();
                MasksCommon.mSliceDeepWaterPoints[vTransitionType].Clear();
                foreach (tPoint vPoint in vDeepWaterPoints)
                {
                    MasksCommon.mDeepWaterPoints[vTransitionType].Add(vPoint);
                    MasksCommon.mSliceDeepWaterPoints[vTransitionType].Add(vPoint);
                }
                vDeepWaterPoints.Clear();

                MasksCommon.mDeepWaterLines[vTransitionType].Clear();
                MasksCommon.mSliceDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in vDeepWaterLines)
                {
                    MasksCommon.mDeepWaterLines[vTransitionType].Add(vLine);
                    MasksCommon.mSliceDeepWaterLines[vTransitionType].Add(vLine);
                }
                vDeepWaterLines.Clear();


                // Prepare Lines For Cut Check
                MasksCommon.mForCutCheckCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in MasksCommon.mOriginalCoastLines[vTransitionType])
                {
                    //Cut checks happens with foregin line types so we check with the ranges from the other type
                    //That check here should also handle lines crossing the box with both points outside
                    if (((vLine.mY1 >= vDeepWaterYMin) || (vLine.mY2 >= vDeepWaterYMin)) &&
                        ((vLine.mY1 <= vDeepWaterYMax) || (vLine.mY2 <= vDeepWaterYMax)) &&
                        ((vLine.mX1 >= vDeepWaterXMin) || (vLine.mX2 >= vDeepWaterXMin)) &&
                        ((vLine.mX1 <= vDeepWaterXMax) || (vLine.mX2 <= vDeepWaterXMax)))
                    {
                        MasksCommon.mForCutCheckCoastLines[vTransitionType].Add(vLine);
                    }
                }

                /* older code
                MasksCommon.mForCutCheckCoastLines[vTransitionType].Clear();
                foreach (tLine vLine in MasksCommon.mOriginalCoastLines[vTransitionType])
                {
                    //Cut checks happens with foregin line types so we check with the ranges from the other type
                    if (((vLine.mX1 <= vDeepWaterXMax) && (vLine.mX1 >= vDeepWaterXMin) && (vLine.mY1 <= vDeepWaterYMax) && (vLine.mY1 >= vDeepWaterYMin)) ||
                        ((vLine.mX2 <= vDeepWaterXMax) && (vLine.mX2 >= vDeepWaterXMin) && (vLine.mY2 <= vDeepWaterYMax) && (vLine.mY2 >= vDeepWaterYMin)))
                    {
                        //Attribute last one point of line is within the Boc so WeakReference add
                        MasksCommon.mForCutCheckCoastLines[vTransitionType].Add(vLine);
                    }
                    else
                    {
                        //Line end points are outside the box but the line still can cross the box
                        //We check if corssing one of the 2 diagonals. Is so it has to be added.
                        Int32 vCut = IsVectorCuttingAnotherVector(vLine, vDeepWaterXMin,vDeepWaterYMin, vDeepWaterXMax, vDeepWaterYMax);
                        if (vCut == 0)
                        {
                            Int32 vCut2 = IsVectorCuttingAnotherVector(vLine, vDeepWaterXMin, vDeepWaterYMax, vDeepWaterXMax, vDeepWaterYMin);
                            if (vCut2 != 0)
                            {
                                //Unknown or critical so add
                                MasksCommon.mForCutCheckCoastLines[vTransitionType].Add(vLine);
                            }  //else we let the vector away..not touching our box in any way
                        }
                        else
                        {
                            //Unknown or critical so add
                            MasksCommon.mForCutCheckCoastLines[vTransitionType].Add(vLine);
                        }
                    }
                }
                */
                
                MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in MasksCommon.mOriginalDeepWaterLines[vTransitionType])
                {
                    //Cut checks happens with foregin line types so we check with the ranges from the other type
                    //That check here should also handle lines crossing the box with both points outside
                    if (((vLine.mY1 >= vCoastYMin) || (vLine.mY2 >= vCoastYMin)) &&
                        ((vLine.mY1 <= vCoastYMax) || (vLine.mY2 <= vCoastYMax)) &&
                        ((vLine.mX1 >= vCoastXMin) || (vLine.mX2 >= vCoastXMin)) &&
                        ((vLine.mX1 <= vCoastXMax) || (vLine.mX2 <= vCoastXMax)))
                    {
                        MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Add(vLine);
                    }
                }

                /* older code
                MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Clear();
                foreach (tLine vLine in MasksCommon.mOriginalDeepWaterLines[vTransitionType])
                {

                    //Cut checks happens with foregin line types so we check with the ranges from the other type
                    if (((vLine.mX1 <= vCoastXMax) && (vLine.mX1 >= vCoastXMin) && (vLine.mY1 <= vCoastYMax) && (vLine.mY1 >= vCoastYMin)) ||
                        ((vLine.mX2 <= vCoastXMax) && (vLine.mX2 >= vCoastXMin) && (vLine.mY2 <= vCoastYMax) && (vLine.mY2 >= vCoastYMin)))
                    {
                        //Attribute last one point of line is within the Boc so WeakReference add
                        MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Add(vLine);
                    }
                    else
                    {
                        //Line end points are outside the box but the line still can cross the box
                        //We check if corssing one of the 2 diagonals. Is so it has to be added.
                        Int32 vCut = IsVectorCuttingAnotherVector(vLine, vCoastXMin, vCoastYMin, vCoastXMax, vCoastYMax);
                        if (vCut == 0)
                        {
                            Int32 vCut2 = IsVectorCuttingAnotherVector(vLine, vCoastXMin, vCoastYMax, vCoastXMax, vCoastYMin);
                            if (vCut2 != 0)
                            {
                                //Unknown or critical so add
                                MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Add(vLine);
                            }  //else we let the vector away..not touching our box in any way
                        }
                        else
                        {
                            //Unknown or critical so add
                            MasksCommon.mForCutCheckDeepWaterLines[vTransitionType].Add(vLine);
                        }
                    }
                }
                */

            }
        }


        public void CommandVectorFloodFill(Int32 iX, Int32 iY, tWaterRegionType iWaterRegionType)
        {
            UInt32 vFillColor = cVectorCommandColorValue;

            switch (iWaterRegionType)
            {
                case tWaterRegionType.eLand:
                    {
                        vFillColor = cLandCommandColorValue << 8; //Shift it to Green
                        break;
                    }
                case tWaterRegionType.eWater:
                    {
                        vFillColor = cWaterCommandColorValue << 8;
                        break;
                    }
                case tWaterRegionType.eTransition:
                    {
                        vFillColor = cTransitionCommandColorValue << 8;
                        break;
                    }
                default:
                    {
                        vFillColor = cVectorCommandColorValue << 8; 
                        break;
                    }
            }
            if (iWaterRegionType != tWaterRegionType.eUndetected)
            {
                if (MasksConfig.mLandWaterDetetcionForEverySinglePixel)
                {
                    SetDestinationPixel24and32BitBitmapFormat(iX, iY, vFillColor); //instead flood fill
                }
                else
                {
                    FloodFill(iX, iY, vFillColor);
                }
            }
        }


        // This is a 4-Way FloodFill that uses mainly Loop to fill Up and Down and Recursion very Sparse only to avoid CallStack overflow which is to expect with big picture. 
        // It should do the job and fill everything correct
        // However it showed to be more complexe than I original thought and some additional code that rarly come into play here I didn't check for correct function anymore (costs lot of time) and so there is a chance it doesnt fill indeed 100%
        // Note that this is no problem for us since we go through the full Bitmap checking for every free pixel in a loop outside that fuction anyway and missing parts would become filled latest then.
        public void FloodFill(Int32 iXStartPos, Int32 iYStartPos, UInt32 iFillColor)
        {

            UInt32 vValueBuffer;

            Int32 vXStart = 0;
            Int32 vXStop  = 0;

            Int32 vStarLineXStart = 0;
            Int32 vStarLineXStop  = 0;

            Int32 vConX;
            Int32 vConY;

            Int32 vNextY;
            Int32 vNextXStart;

            Boolean vPartSign;
            Boolean vBackTrackFlag;

            Boolean vBorderFound;

            vBorderFound = false;

            vNextXStart = iXStartPos;

            //--- Loop Upward --- (Upward in Y)

            for (vConY = iYStartPos; vConY >= 0; vConY--)
            {

                //go left
                for (vConX = vNextXStart; vConX >= 0; vConX--)
                {
                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                    if (vValueBuffer != 0x00000000)
                    {
                        vBorderFound = true;
                        vXStart = vConX + 1;
                        break; //stop;
                    }
                }
                if (!vBorderFound)
                {
                    vXStart = 0;
                }

                vBorderFound = false;

                //go right
                for (vConX = vXStart; vConX < mPixelCountInX; vConX++)
                {
                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                    if (vValueBuffer != 0x00000000)
                    {
                        vBorderFound = true;
                        vXStop = vConX - 1;  //vStart is never 0 so it's a safe operation to subtract 1
                        vConX = mPixelCountInX; //stop;
                    }
                    else
                    {
                        SetDestinationPixel24and32BitBitmapFormat(vConX, vConY, iFillColor);
                    }
                }
                if (!vBorderFound)
                {
                    vXStop = mPixelCountInX-1;
                }

                //KeepStart Line to free it on the downsearch again
                if (vConY == iYStartPos)
                {
                    vStarLineXStart = vXStart;
                    vStarLineXStop  = vXStop;
                }


                //go Up and Search
                if (vConY > 0)
                {
                    vNextY = vConY - 1;

                    vBorderFound = false;
                    //Search  X Start-Stop Range                  

                    for (vConX = vXStart; vConX <= vXStop; vConX++)
                    {
                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                        vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                        if (vValueBuffer == 0x00000000)
                        {
                            vBorderFound = true;
                            vNextXStart = vConX;
                            break;  //stop;
                        }
                    }
                    if (!vBorderFound)
                    {
                        break; //stop; //Exit UpsearchLoop
                    }
                    else
                    {
                        //Check for Further Parts.
                        // a= actual line in loop, N = next line in loop, R = Further Parts Recursion call   
                        //      [N][N][N][N]   [R][R][R][R][R]   [R][R][R]
                        //   [A][A][A][A][A][A][A][A][A][A][A][A][A]
                        vPartSign      = true;
                        vBackTrackFlag = true;
                        //Search  X Start-Stop Range
                        for (vConX = vNextXStart; vConX <= vXStop; vConX++)
                        {
                            vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                            vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                            if (vPartSign)
                            {
                                if (vValueBuffer != 0x00000000)
                                {
                                    vPartSign      = false;
                                    vBackTrackFlag = false;
                                }
                            }
                            else
                            {
                                if (vValueBuffer == 0x00000000)
                                {
                                    vPartSign = true;
                                    FloodFill(vConX, vNextY, iFillColor); //Now enter Recursion
                                }
                            }
                        }
                        
                        //Bounce Back
                        // a= actual line in loop, N = next line in loop, R = Bounce Back Recursion call  (right side) 
                        //      [N][N][N][N][N][N][N][N][N][N][N]
                        //   [A][A][A][A]   [R][R][R]  [R][R]
                        if (vBackTrackFlag)
                        {
                            //Back Tracking right Side
                            vPartSign = false;
                            for (vConX = vXStop + 1; vConX < mPixelCountInX; vConX++)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                                vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                if (vValueBuffer == 0x00000000)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                    if (vPartSign)
                                    {
                                        if (vValueBuffer != 0x00000000)
                                        {
                                            vPartSign = false;
                                        }
                                    }
                                    else
                                    {
                                        if (vValueBuffer == 0x00000000)
                                        {
                                            vPartSign = true;
                                            FloodFill(vConX, vConY, iFillColor); //Now enter BackTrack Recursion
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        // a= actual line in loop, N = next line in loop, R = Bounce Back Recursion call (left side) 
                        //      [N][N][N][N][N][N][N][N][N][N][N]
                        //    [R][R][R]  [R][R]   [A][A][A][A]
                        if ((vNextXStart == vXStart) && (vXStart > 0))
                        {
                            //Back Tracking Left Side
                            vPartSign = false;
                            for (vConX = vXStart - 1; vConX >= 0; vConX--)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                                vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                if (vValueBuffer == 0x00000000)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                    if (vPartSign)
                                    {
                                        if (vValueBuffer != 0x00000000)
                                        {
                                            vPartSign = false;
                                        }
                                    }
                                    else
                                    {
                                        if (vValueBuffer == 0x00000000)
                                        {
                                            vPartSign = true;
                                            FloodFill(vConX, vConY, iFillColor); //Now enter BackTrack Recursion
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //-------

                    }

                }
                else
                {
                    break; //stop; //Exit UpsearchLoop
                }

            }

            vBorderFound = false;
            vNextXStart = iXStartPos;

            //--- Loop  Downward --- (simular to above just Downward in Y)

            //First free the StartLine again. 
            //The StartLine was marked to block any recursion deadlock.
            //We have to search from the start line again because we have to enter the normal NextLine backtrack check's downward this tim  from there else that is mission and skiped to overnext          
            for (vConX = vStarLineXStart; vConX <= vStarLineXStop; vConX++)
            {
                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, iYStartPos);
                vValueBuffer &= 0xFFFF00FF; //GreenChannel is command channel
                SetDestinationPixel24and32BitBitmapFormat(vConX, iYStartPos, vValueBuffer); //Fill with Free Color Green = 0
            }

            for (vConY = iYStartPos; vConY < mPixelCountInY; vConY++)
            {

                //go left
                for (vConX = vNextXStart; vConX >= 0; vConX--)
                {
                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                    if (vValueBuffer != 0x00000000)
                    {
                        vBorderFound = true;
                        vXStart = vConX + 1;
                        break; //stop;
                    }
                }
                if (!vBorderFound)
                {
                    vXStart = 0;
                }

                vBorderFound = false;

                //go right
                for (vConX = vXStart; vConX < mPixelCountInX; vConX++)
                {
                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                    if (vValueBuffer != 0x00000000)
                    {
                        vBorderFound = true;
                        vXStop = vConX - 1;  //vStart is never 0 so it's a safe operation to subtract 1
                        break;  //stop;
                    }
                    else
                    {
                        SetDestinationPixel24and32BitBitmapFormat(vConX, vConY, iFillColor); //This time we fill
                    }
                }
                if (!vBorderFound)
                {
                    vXStop = mPixelCountInX-1;
                }

                //go Down and Search
                if (vConY < (mPixelCountInY-1))
                {
                    vNextY = vConY + 1;

                    vBorderFound = false;
                    //Search  X Start-Stop Range
                    for (vConX = vXStart; vConX <= vXStop; vConX++)
                    {
                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                        vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                        if (vValueBuffer == 0x00000000)
                        {
                            vBorderFound = true;
                            vNextXStart = vConX;
                            break; //stop;
                        }
                    }
                    if (!vBorderFound)
                    {
                        break; //stop; //Exit UpsearchLoop
                    }
                    else
                    {
                        //Check for Further Parts.
                        // a= actual line in loop, N = next line in loop, R = Further Parts Recursion call   
                        //   [A][A][A][A][A][A][A][A][A][A][A][A][A]
                        //      [N][N][N][N]   [R][R][R][R][R]   [R][R][R]
                        vPartSign = true;
                        vBackTrackFlag = true;
                        //Search  X Start-Stop Range
                        for (vConX = vNextXStart; vConX <= vXStop; vConX++)
                        {
                            vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                            vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                            if (vPartSign)
                            {
                                if (vValueBuffer != 0x00000000)
                                {
                                    vPartSign      = false;
                                    vBackTrackFlag = false;
                                }
                            }
                            else
                            {
                                if (vValueBuffer == 0x00000000)
                                {
                                    vPartSign = true;
                                    FloodFill(vConX, vNextY, iFillColor); //Now enter Recursion
                                }
                            }
                        }
                        //Bounce Back
                        // a= actual line in loop, N = next line in loop, R = Bounce Back Recursion call  (right side) 
                        //   [A][A][A][A]   [R][R][R]  [R][R]       
                        //      [N][N][N][N][N][N][N][N][N][N][N]
                        if (vBackTrackFlag)
                        {
                            //Back Tracking right Side
                            vPartSign = false;
                            for (vConX = vXStop + 1; vConX < mPixelCountInX; vConX++)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                                vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                if (vValueBuffer == 0x00000000)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                    if (vPartSign)
                                    {
                                        if (vValueBuffer != 0x00000000)
                                        {
                                            vPartSign = false;
                                        }
                                    }
                                    else
                                    {
                                        if (vValueBuffer == 0x00000000)
                                        {
                                            vPartSign = true;
                                            FloodFill(vConX, vConY, iFillColor); //Now enter BackTrack Recursion
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        // a= actual line in loop, N = next line in loop, R = Bounce Back Recursion call (left side) 
                        //    [R][R][R]  [R][R]   [A][A][A][A]
                        //      [N][N][N][N][N][N][N][N][N][N][N]
                        if ((vNextXStart == vXStart) && (vXStart > 0))
                        {
                            //Back Tracking Left Side
                            vPartSign = false;
                            for (vConX = vXStart - 1; vConX >= 0; vConX--)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vNextY);
                                vValueBuffer &= 0x0000FF00; //GreenChannel is command channel
                                if (vValueBuffer == 0x00000000)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY);
                                    vValueBuffer &= 0x0000FF00; //GreenChannel is command channel

                                    if (vPartSign)
                                    {
                                        if (vValueBuffer != 0x00000000)
                                        {
                                            vPartSign = false;
                                        }
                                    }
                                    else
                                    {
                                        if (vValueBuffer == 0x00000000)
                                        {
                                            vPartSign = true;
                                            FloodFill(vConX, vConY, iFillColor); //Now enter BackTrack Recursion
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //-------                       
                    }

                }
                else
                {
                    break; //stop; //Exit UpsearchLoop
                }
            }

        }

        private tXYCoord CoordToPixel(double lat, double longi)
        {
            tXYCoord tempCoord;
            tempCoord.mX = longi;
            tempCoord.mY = lat;
            tXYCoord pixel = ConvertXYLatLongToPixel(tempCoord);
            pixel.mX -= 0.5f;
            pixel.mY -= 0.5f;

            return pixel;
        }

        private PointF[] CoordsToPixelRect(double startLat, double stopLat, double startLong, double stopLong)
        {
            PointF[] ret = new PointF[4];

            tXYCoord pixel = CoordToPixel(startLat, startLong);
            ret[0] = new PointF((float)pixel.mX, (float)pixel.mY);
            pixel = CoordToPixel(startLat, stopLong);
            ret[1] = new PointF((float)pixel.mX, (float)pixel.mY);
            pixel = CoordToPixel(stopLat, stopLong);
            ret[2] = new PointF((float)pixel.mX, (float)pixel.mY);
            pixel = CoordToPixel(stopLat, startLong);
            ret[3] = new PointF((float)pixel.mX, (float)pixel.mY);

            return ret;
        }

        private enum BlendGradientStartStopMode
        {
            WhiteToBlack,
            BlackToWhite
        }

        private void Blend(Graphics g, PointF[] rect, LinearGradientMode lgMode, BlendGradientStartStopMode bgMode)
        {
            float width = rect[1].X - rect[0].X;
            float height = rect[3].Y - rect[0].Y;

            RectangleF r = new RectangleF(rect[0].X, rect[0].Y, width, height);
            LinearGradientBrush b = null;
            if (bgMode == BlendGradientStartStopMode.BlackToWhite)
            {
                b = new LinearGradientBrush(
                    r,
                    Color.Black,
                    Color.White,
                    lgMode
                );
            }
            else
            {
                b = new LinearGradientBrush(
                    r,
                    Color.White,
                    Color.Black,
                    lgMode
                );
            }

            g.FillRectangle(b, r);
        }

        public static double[][] ImageTo2DByteArray(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            byte[] bytes = new byte[height * data.Stride];
            try
            {
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            double[][] result = new double[height][];
            for (int y = 0; y < height; ++y)
            {
                result[y] = new double[width];
                for (int x = 0; x < width; ++x)
                {
                    int offset = y * data.Stride + x * 3;
                    result[y][x] = (double)((bytes[offset + 0] + bytes[offset + 1] + bytes[offset + 2]) / 3);
                }
            }

            return result;
        }

        public struct ColorARGB
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;

            public ColorARGB(Color color)
            {
                A = color.A;
                R = color.R;
                G = color.G;
                B = color.B;
            }

            public ColorARGB(byte a, byte r, byte g, byte b)
            {
                A = a;
                R = r;
                G = g;
                B = b;
            }

            public Color ToColor()
            {
                return Color.FromArgb(A, R, G, B);
            }
        }

        private unsafe Bitmap ToBitmap(double[][] rawImage)
        {
            int width = rawImage[0].Length;
            int height = rawImage.Length;

            Bitmap Image = new Bitmap(width, height);
            BitmapData bitmapData = Image.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb
            );
            ColorARGB* startingPosition = (ColorARGB*)bitmapData.Scan0;


            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    double color = rawImage[i][j];
                    byte rgb = (byte)color;

                    ColorARGB* position = startingPosition + j + i * width;
                    position->A = 255;
                    position->R = rgb;
                    position->G = rgb;
                    position->B = rgb;
                }

            Image.UnlockBits(bitmapData);
            return Image;
        }

        // this is a port from Ortho4XP. I've no clue how it works, but it applies a nice smooth transition to coasts...
        private Bitmap ApplyMaskWidth(Bitmap origImg, Bitmap maskWidthImg)
        {
            double[][] maskWidthImgBytes = ImageTo2DByteArray(maskWidthImg);
            double[][] origImgBytes = ImageTo2DByteArray(origImg);

            int blurWidth = (int)((MasksConfig.mAreaPixelCountInX / (MasksConfig.mAreaSECornerLongitude - MasksConfig.mAreaNWCornerLongitude)) * MasksConfig.mMasksWidth);
            if (blurWidth < 2)
            {
                return origImg;
            }

            double[] kernel = new double[2 * blurWidth - 1];
            for (int i = blurWidth - 1, idx = blurWidth; i > 0; i--, idx++)
            {
                kernel[idx] = i;
            }
            for (int i = 0; i < 2 * blurWidth - 1; i++)
            {
                kernel[i] = (i + 1) / Math.Pow(blurWidth, 2);
            }

            for (int i = 0; i < maskWidthImgBytes.Length; i++)
            {
                maskWidthImgBytes[i] = CommonFunctions.Convolve(maskWidthImgBytes[i], kernel, "same");
            }

            maskWidthImgBytes = CommonFunctions.Transpose(maskWidthImgBytes);

            for (int i = 0; i < maskWidthImgBytes.Length; i++)
            {
                maskWidthImgBytes[i] = CommonFunctions.Convolve(maskWidthImgBytes[i], kernel, "same");
            }

            maskWidthImgBytes = CommonFunctions.Transpose(maskWidthImgBytes);

            for (int i = 0; i < maskWidthImgBytes.Length; i++)
            {
                for (int j = 0; j < maskWidthImgBytes[0].Length; j++)
                {
                    double min = 2 * (maskWidthImgBytes[i][j] < 127 ? maskWidthImgBytes[i][j] : 127);
                    double temp = origImgBytes[i][j] > 0 ? 255 : 0;

                    origImgBytes[i][j] = temp > min ? temp : min;
                }
            }

            return ToBitmap(origImgBytes);
        }

        public Bitmap createWaterMaskBitmap()
        {
            var tris = ReadAllMeshFiles();
            Bitmap bmp = new Bitmap(MasksConfig.mAreaPixelCountInX, MasksConfig.mAreaPixelCountInY, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bmp);
            SolidBrush b = new SolidBrush(Color.Black);
            g.FillRectangle(Brushes.White, 0, 0, bmp.Width, bmp.Height);

            // borders
            double NWLat = MasksConfig.mAreaNWCornerLatitude;
            double NWLon = MasksConfig.mAreaNWCornerLongitude;
            double SELat = MasksConfig.mAreaSECornerLatitude;
            double SELon = MasksConfig.mAreaSECornerLongitude;
            double pixWidth = MasksConfig.mBlendBorderDistance;
            double LON_BLEND_WIDTH = ((SELon - NWLon) / MasksConfig.mAreaPixelCountInX) * pixWidth;
            double LAT_BLEND_WIDTH = ((NWLat - SELat) / MasksConfig.mAreaPixelCountInY) * pixWidth;
            if (MasksConfig.mBlendNorthBorder)
            {
                Blend(g, CoordsToPixelRect(NWLat, NWLat - LAT_BLEND_WIDTH, NWLon, SELon), LinearGradientMode.Vertical, BlendGradientStartStopMode.BlackToWhite);
            }
            if (MasksConfig.mBlendEastBorder)
            {
                Blend(g, CoordsToPixelRect(NWLat, SELat, SELon - LON_BLEND_WIDTH, SELon), LinearGradientMode.Horizontal, BlendGradientStartStopMode.WhiteToBlack);
            }
            if (MasksConfig.mBlendSouthBorder)
            {
                Blend(g, CoordsToPixelRect(SELat + LAT_BLEND_WIDTH, SELat, NWLon, SELon), LinearGradientMode.Vertical, BlendGradientStartStopMode.WhiteToBlack);
            }
            if (MasksConfig.mBlendWestBorder)
            {
                Blend(g, CoordsToPixelRect(NWLat, SELat, NWLon, NWLon + LON_BLEND_WIDTH), LinearGradientMode.Horizontal, BlendGradientStartStopMode.BlackToWhite);
            }

            foreach (var tri in tris)
            {
                PointF[] convertedTri = new PointF[3];
                for (int i = 0; i < convertedTri.Length; i++)
                {
                    PointF toConvert = tri[i];
                    tXYCoord pixel = CoordToPixel(toConvert.Y, toConvert.X);
                    convertedTri[i] = new PointF((float)pixel.mX, (float)pixel.mY);
                }

                g.FillPolygon(b, convertedTri);
            }

            MemoryStream memoryStream = new MemoryStream();
            bmp.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            Bitmap streamBitmap = (Bitmap)Bitmap.FromStream(memoryStream);
            bmp = ApplyMaskWidth(bmp, streamBitmap);

            return bmp;
        }
        public void FloodFillWaterLandTransition(Int32 iTransitionType, FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            Int32  vX = 0;
            Int32  vY = 0;
            Int32  vPixelInQuadIndex = 0;
            Int32  vUInt32IndexModal96 = 0;
            UInt32 vValue = 0;

            tWaterRegionType vWaterRegionType = tWaterRegionType.eLand;

            Int32 vPixelCounterForDisplay = 0;

            //FloodFill Regions With Commands
            for (vY = 0; vY < mPixelCountInY; vY++)
            {
                vPixelCounterForDisplay++;

                if (((vPixelCounterForDisplay % 100) == 0) || (vPixelCounterForDisplay == mPixelCountInY))
                {
                    switch (iTransitionType)
                    {
                        case (Int32)(MasksConfig.tTransitionType.eWaterTransition):
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Water  flood ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                                break;
                            }
                        case (Int32)(MasksConfig.tTransitionType.eWater2Transition):
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Water2 flood ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                                break;
                            }
                        case (Int32)(MasksConfig.tTransitionType.eBlendTransition):
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Blend flood ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                                break;
                            }
                        default:
                            {
                                iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Unknown flood ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                                break;
                            }
                    }
                }

                vPixelInQuadIndex = 0;
                vUInt32IndexModal96 = 0;

                for (vX = 0; vX < (mPixelCountInX); vX++)
                {

                    //Is Pixel Still uncolored / Free?

                    if (mDestination32BitBitmapMode)
                    {
                        vValue = mAreaBitmapArray[vY, vX];
                    }
                    else
                    {
                        vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                    }

                    vValue = vValue & 0x0000FF00;

                    if (vValue == 0)
                    {
                        //Yes it is free
                        //For Debug of a specific land water detection bug
                        //if (((vX >= 442) && (vX <= 446)) && (vY ==0))
                        //{
                        //    Thread.Sleep(1);
                        //}
                        vWaterRegionType = CalculateWaterTransitionRegionType(iTransitionType, (Single)(vX), (Single)(vY));
                        CommandVectorFloodFill(vX, vY, vWaterRegionType);
                    }

                    //for flood test comment this out so it does only start on X = 0 position
                    vPixelInQuadIndex++;
                    if (vPixelInQuadIndex >= 4)
                    {
                        vPixelInQuadIndex = 0;
                        vUInt32IndexModal96 += 3;
                    }

                }
            }
        }

        public void ClearCommandArray()
        {
            for (Int32 vY = 0; vY < mPixelCountInY; vY++)
            {
                for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                {
                    mWorkCommandArray[vY, vX] = 0;
                }
            }
        }

        public void CopyTransitionPolygonResultToWorkCommandArray(Int32 iTransitionType, FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            DoCopyResultToWorkCommandArray(iTransitionType, iFSEarthMasksInternalInterface, true); //true = Handle water Only
        }

        public void CopyResultToWorkCommandArray(Int32 iTransitionType, FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            DoCopyResultToWorkCommandArray(iTransitionType, iFSEarthMasksInternalInterface, false); //false = handle everything
        }

        protected void DoCopyResultToWorkCommandArray(Int32 iTransitionType, FSEarthMasksInternalInterface iFSEarthMasksInternalInterface, Boolean iHandleWaterOnly)
        {
            try
            {
                Int32 vPixelInQuadIndex = 0;
                Int32 vUInt32IndexModal96 = 0;
                UInt32 vValue = 0;

                Byte vLandCommandCommand       = 0x00;
                Byte vTransitionCommandCommand = 0x00;
                Byte vWaterCommandCommand      = 0x00;
                Byte vMaskCommandCommand       = 0xFF;

                switch (iTransitionType)
                {
                    case ((Int32)(MasksConfig.tTransitionType.eWaterTransition)):
                        {
                            vLandCommandCommand       = (Byte)(tWorkCommandCommand.eWaterTransitionLand);
                            vTransitionCommandCommand = (Byte)(tWorkCommandCommand.eWaterTransitionTransition);
                            vWaterCommandCommand      = (Byte)(tWorkCommandCommand.eWaterTransitionWater);
                            vMaskCommandCommand       = 0xFC;
                            break;
                        }
                    case ((Int32)(MasksConfig.tTransitionType.eWater2Transition)):
                        {
                            vLandCommandCommand       = (Byte)(tWorkCommandCommand.eWater2TransitionLand);
                            vTransitionCommandCommand = (Byte)(tWorkCommandCommand.eWater2TransitionTransition);
                            vWaterCommandCommand      = (Byte)(tWorkCommandCommand.eWater2TransitionWater);
                            vMaskCommandCommand       = 0xF3;
                            break;
                        }
                    case ((Int32)(MasksConfig.tTransitionType.eBlendTransition)):
                        {
                            vLandCommandCommand       = (Byte)(tWorkCommandCommand.eBlendTransitionSolid);
                            vTransitionCommandCommand = (Byte)(tWorkCommandCommand.eBlendTransitionTransition);
                            vWaterCommandCommand      = (Byte)(tWorkCommandCommand.eBlendTransitionFullyTransparent);
                            vMaskCommandCommand       = 0xCF;
                            break;
                        }
                    default:
                        {
                            //do nothing
                            break;
                        }
                }

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {
                        //Check Pixel Commando
                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                        }
                        vValue = (vValue & 0x0000FF00) >> 8;


                        switch (vValue)
                        {
                            case cLandCommandColorValue:
                                {
                                    if (!iHandleWaterOnly)
                                    {
                                        mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                        mWorkCommandArray[vY, vX] |= vLandCommandCommand;
                                    }
                                    break;
                                }
                            case cWaterCommandColorValue:
                                {
                                    mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                    mWorkCommandArray[vY, vX] |= vWaterCommandCommand;
                                    break;
                                }
                            case cTransitionCommandColorValue:
                                {
                                    if (!iHandleWaterOnly)
                                    {
                                        mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                        mWorkCommandArray[vY, vX] |= vTransitionCommandCommand;
                                    }
                                    break;
                                }
                            case cVectorCommandColorValue:
                                {
                                    if (!iHandleWaterOnly)
                                    {
                                        mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                        mWorkCommandArray[vY, vX] |= vTransitionCommandCommand;
                                    }
                                    break;
                                }
                            default:
                                {
                                    if (!iHandleWaterOnly)
                                    {
                                        //new this could happen In case we have really an eUndecided and not filled by flood.
                                        //the chances that thsi happens should be zero but it's not garanted. Amatter of circumstances. -> Vector size, Bitmapsize and Signle calc percission .the epsilon.
                                        //if so lets calculate it as transition that should be best (alternative set it to land)
                                        mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                        mWorkCommandArray[vY, vX] |= vTransitionCommandCommand;
                                    }
                                    break;
                                }
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
            catch
            {
                //ops
                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Code or Memory error catch in: CopyResultToWorkCommandArray");
                Thread.Sleep(3000);
            }
        }


        public void CopyPoolPolygonsResultToWorkCommandArray(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {
            try
            {
                Int32 vPixelInQuadIndex = 0;
                Int32 vUInt32IndexModal96 = 0;
                UInt32 vValue = 0;

                Byte vWaterCommandCommand = (Byte)(tWorkCommandCommand.eFixWater);
                Byte vLandCommandCommand  = (Byte)(tWorkCommandCommand.eFixLand);
                Byte vBlendCommandCommand = (Byte)(tWorkCommandCommand.eFixBlend);
                Byte vMaskCommandCommand  = 0x3F;
   

                for (Int32 vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (Int32 vX = 0; vX < mPixelCountInX; vX++)
                    {
                        //Check Pixel Commando
                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                        }
                        vValue = (vValue & 0x0000FF00) >> 8;


                        switch (vValue)
                        {
                            case cPoolWaterCommandColorValue:
                                {
                                    mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                    mWorkCommandArray[vY, vX] |= vWaterCommandCommand;
                                    break;
                                }
                            case cPoolLandCommandColorValue:
                                {
                                    mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                    mWorkCommandArray[vY, vX] |= vLandCommandCommand;
                                    break;
                                }
                            case cPoolBlendCommandColorValue:
                                {
                                    mWorkCommandArray[vY, vX] &= vMaskCommandCommand;
                                    mWorkCommandArray[vY, vX] |= vBlendCommandCommand;
                                    break;
                                }
                            default:
                                {
                                   //nothing to do
                                    break;
                                }
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
            catch
            {
                //ops
                iFSEarthMasksInternalInterface.SetStatusFromFriendThread("Code or Memory error catch in: CopyResultToWorkCommandArray");
                Thread.Sleep(3000);
            }
        }

        public void CreateWaterInWorkMaskBitmap(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            if (mWorkMemoryAllocated)
            {

                PrepareTrippleSTransitionTables();

                //MasksCommon.mCoastLines.Clear(); //test
                //MasksCommon.mDeepWaterLines.Clear(); //test

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                Byte vCmdValue = 0;
                Byte vSubCmdValue = 0;

                UInt32 vReflectionValue = 0;
                UInt32 vTransparencyValue = 0;

                Single vReflectionFactor = 0.0f;
                Single vTransparencyFactor = 0.0f;

                Single vSquareDist1 = 0.0f;
                Single vSquareDist2 = 1.0f;

                Int32 vY = 0;
                Int32 vX = 0;

                UInt32 vWaterOneStep = (UInt32)((1 << (8 - MasksConfig.mWaterResolutionBitsCount)));
                UInt32 vWaterStepAddition = vWaterOneStep - 1; //cosmetic to have the brightest Value
                Int32 vWaterStepsInResolution = (1 << MasksConfig.mWaterResolutionBitsCount) - 1;
                Single vWaterStepWidth = 1.0f / vWaterStepsInResolution;

                UInt32 vLandOneStep = (UInt32)((1 << (8 - MasksConfig.mBlendResolutionBitsCount)));
                UInt32 vLandStepAddition = vLandOneStep - 1; //cosmetic to have the brightest Value
                Int32 vLandStepsInResolution = (1 << MasksConfig.mBlendResolutionBitsCount) - 1;
                Single vLandStepWidth = 1.0f / vLandStepsInResolution;

                Int32 vPixelCounterForDisplay = 0;
                Int32 vRowCountForSlice = -1;

                //Now do The Water and Land Mask
                for (vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelCounterForDisplay++;

                    vRowCountForSlice++;
                    if (cUseSliceMethode)
                    {
                        if ((vRowCountForSlice % cYSliceSpace) == 0)
                        {
                            SliceReduceLinesCount(0, vRowCountForSlice, mPixelCountInX - 1, vRowCountForSlice + cYSliceSpace - 1);
                            if (cUseCellsMethode)
                            {
                                CellsReduceLinesCount(vRowCountForSlice, vRowCountForSlice + cYSliceSpace - 1);
                            }
                        }
                    }

                    if (((vPixelCounterForDisplay % 100) == 0) || (vPixelCounterForDisplay == mPixelCountInY))
                    {
                        iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Calculate Water ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                    }

                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (vX = 0; vX < (mPixelCountInX); vX++)
                    {
                        //Land Conditions
                        vReflectionFactor = 0.0f;
                        vTransparencyFactor = 0.0f;

                        //Check Pixel Commando    
                        vCmdValue = mWorkCommandArray[vY, vX];

                        vSubCmdValue = vCmdValue;
                        vSubCmdValue &= 0xC0;

                        if (vSubCmdValue == 0x00)
                        {
                            //Transitions

                            //Water
                            vSubCmdValue = vCmdValue;
                            vSubCmdValue &= 0x03;

                            switch (vSubCmdValue)
                            {
                                case (Byte)(tWorkCommandCommand.eWaterTransitionWater):
                                    {
                                        vTransparencyFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWaterTransition), (UInt32)(MasksConfig.tTransitionSubType.eTransparency)];
                                        vReflectionFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWaterTransition), (UInt32)(MasksConfig.tTransitionSubType.eReflection)];
                                        break;
                                    }
                                case (Byte)(tWorkCommandCommand.eWaterTransitionTransition):
                                    {
                                        CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                        vTransparencyFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eTransparency), vSquareDist1, vSquareDist2);
                                        vReflectionFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWaterTransition), (Int32)(MasksConfig.tTransitionSubType.eReflection), vSquareDist1, vSquareDist2);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            //WaterTwo
                            vSubCmdValue = vCmdValue;
                            vSubCmdValue &= 0x0C;

                            switch (vSubCmdValue)
                            {
                                case (Byte)(tWorkCommandCommand.eWater2TransitionWater):
                                    {
                                        Single vTempTransparencyFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWater2Transition), (UInt32)(MasksConfig.tTransitionSubType.eTransparency)];
                                        Single vTempReflectionFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eWater2Transition), (UInt32)(MasksConfig.tTransitionSubType.eReflection)];
                                        if (vTempTransparencyFactor > vTransparencyFactor)
                                        {
                                            vTransparencyFactor = vTempTransparencyFactor;
                                        }
                                        if (vTempReflectionFactor > vReflectionFactor)
                                        {
                                            vReflectionFactor = vTempReflectionFactor;
                                        }
                                        break;
                                    }
                                case (Byte)(tWorkCommandCommand.eWater2TransitionTransition):
                                    {
                                        CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                        Single vTempTransparencyFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eTransparency), vSquareDist1, vSquareDist2);
                                        Single vTempReflectionFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eWater2Transition), (Int32)(MasksConfig.tTransitionSubType.eReflection), vSquareDist1, vSquareDist2);
                                        if (vTempTransparencyFactor > vTransparencyFactor)
                                        {
                                            vTransparencyFactor = vTempTransparencyFactor;
                                        }
                                        if (vTempReflectionFactor > vReflectionFactor)
                                        {
                                            vReflectionFactor = vTempReflectionFactor;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }


                            //Blend
                            vSubCmdValue = vCmdValue;
                            vSubCmdValue &= 0x30;

                            switch (vSubCmdValue)
                            {
                                case (Byte)(tWorkCommandCommand.eBlendTransitionFullyTransparent):
                                    {
                                        Single vTempTransparencyFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eBlendTransition), (UInt32)(MasksConfig.tTransitionSubType.eTransparency)];
                                        Single vTempReflectionFactor = mTrippleSTransitionExitValues[(UInt32)(MasksConfig.tTransitionType.eBlendTransition), (UInt32)(MasksConfig.tTransitionSubType.eReflection)];
                                        if (vTempTransparencyFactor > vTransparencyFactor)
                                        {
                                            vTransparencyFactor = vTempTransparencyFactor;
                                        }
                                        if (vTempReflectionFactor > vReflectionFactor)
                                        {
                                            vReflectionFactor = vTempReflectionFactor;
                                        }
                                        break;
                                    }
                                case (Byte)(tWorkCommandCommand.eBlendTransitionTransition):
                                    {
                                        CalculateTransitionSquares((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Single)(vX), (Single)(vY), cUseSliceMethode, cUseCellsMethode, ref vSquareDist1, ref vSquareDist2);
                                        Single vTempTransparencyFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eTransparency), vSquareDist1, vSquareDist2);
                                        Single vTempReflectionFactor = GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input((Int32)(MasksConfig.tTransitionType.eBlendTransition), (Int32)(MasksConfig.tTransitionSubType.eReflection), vSquareDist1, vSquareDist2);
                                        if (vTempTransparencyFactor > vTransparencyFactor)
                                        {
                                            vTransparencyFactor = vTempTransparencyFactor;
                                        }
                                        if (vTempReflectionFactor > vReflectionFactor)
                                        {
                                            vReflectionFactor = vTempReflectionFactor;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            //Pools Pools overwrite everything!
                            switch (vSubCmdValue)
                            {
                                case (Byte)(tWorkCommandCommand.eFixWater):
                                    {
                                        vTransparencyFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eWaterPool)].mTransparency);
                                        vReflectionFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eWaterPool)].mReflection);
                                        break;
                                    }
                                case (Byte)(tWorkCommandCommand.eFixLand):
                                    {
                                        vTransparencyFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eLandPool)].mTransparency);
                                        vReflectionFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eLandPool)].mReflection);
                                        break;
                                    }
                                case (Byte)(tWorkCommandCommand.eFixBlend):
                                    {
                                        vTransparencyFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eBlendPool)].mTransparency);
                                        vReflectionFactor = (Single)(MasksConfig.mPoolsParameters[(UInt32)(MasksConfig.tPoolType.eBlendPool)].mReflection);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }
                        }

                        vReflectionValue = (UInt32)(vReflectionFactor * 255.0f);
                        vTransparencyValue = (UInt32)(vTransparencyFactor * 255.0f);

                        if ((vReflectionValue < 255) && (vReflectionValue > 0))
                        {
                            //Dithering
                            Single vWaterPartFactor = vReflectionFactor / vWaterStepWidth;
                            UInt32 vBaseStepNr = (UInt32)(vWaterPartFactor); //No Convert.UInt32 here convert rounds the number, cast not!!
                            Single vWaterStepFraction = vWaterPartFactor - (Single)(vBaseStepNr);
                            Single vRandom = (Single)(mRandomGenerator.NextDouble());
                            if (vRandom <= vWaterStepFraction)
                            {
                                //Next Higher
                                vReflectionValue = (vBaseStepNr + 1) * vWaterOneStep + vWaterStepAddition;
                            }
                            else
                            {
                                //Next Lower
                                vReflectionValue = vBaseStepNr * vWaterOneStep + vWaterStepAddition;
                            }
                        }


                        if ((vTransparencyValue < 255) && (vTransparencyValue > 0))
                        {
                            //Dithering
                            Single vLandPartFactor = vTransparencyFactor / vLandStepWidth;
                            UInt32 vBaseStepNr = (UInt32)(vLandPartFactor); //No Convert.UInt32 here convert rounds the number, cast not!!
                            Single vLandStepFraction = vLandPartFactor - (Single)(vBaseStepNr);
                            Single vRandom = (Single)(mRandomGenerator.NextDouble());
                            if (vRandom <= vLandStepFraction)
                            {
                                //Next Higher
                                vTransparencyValue = (vBaseStepNr + 1) * vLandOneStep + vLandStepAddition;
                            }
                            else
                            {
                                //Next Lower
                                vTransparencyValue = vBaseStepNr * vLandOneStep + vLandStepAddition;
                            }
                        }

                        if (mDestination32BitBitmapMode)
                        {
                            vValue = mAreaBitmapArray[vY, vX];
                            vValue = ((vValue & 0xFF00FF00) | vReflectionValue) | (vTransparencyValue << 16);
                            mAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                            vValue = ((vValue & 0xFF00FF00) | vReflectionValue) | (vTransparencyValue << 16);
                            SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public void CreateFS2004WaterInAreaBitmap(Bitmap mask)
        {
            Size s1 = mAreaBitmap.Size;
            Size s2 = mask.Size;
            if (s1 != s2)
            {
                return;
            }

            for (int y = 0; y < s1.Height; y++)
            {
                for (int x = 0; x < s1.Width; x++)
                {
                    Color c1 = mAreaBitmap.GetPixel(x, y);
                    Color c2 = mask.GetPixel(x, y);
                    mAreaBitmap.SetPixel(x, y, Color.FromArgb((int)(255 * c2.GetBrightness()), c1));
                }
            }
        }

        public void CreateFS2004WaterInWorkMaskBitmap(FSEarthMasksInternalInterface iFSEarthMasksInternalInterface)
        {

            if ((mWorkMemoryAllocated) && (mDestination32BitBitmapMode))
            {
 

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                Int32 vX;
                Int32 vY;

                Boolean vIsWater = false;

                Int32 vPixelCounterForDisplay = 0;

                //Now do The Water and Land Mask
                for (vY = 0; vY < mPixelCountInY; vY++)
                {
                    vPixelCounterForDisplay++;


                    if (((vPixelCounterForDisplay % 100) == 0) || (vPixelCounterForDisplay == mPixelCountInY))
                    {
                        iFSEarthMasksInternalInterface.SetStatusFromFriendThread(" Process FS2004 Water ...   row " + Convert.ToString(vPixelCounterForDisplay) + " of " + Convert.ToString(mPixelCountInY));
                    }

                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (vX = 0; vX < (mPixelCountInX); vX++)
                    {
                        //Land Conditions
                        vIsWater = IsWaterOrWaterTransition(vX,vY);

                        if (mDestination32BitBitmapMode)
                        {
                            if (vIsWater)
                            {
                                vValue = mAreaBitmapArray[vY, vX];
                                vValue = vValue & 0x00FFFFFF; //SetWater
                                mAreaBitmapArray[vY, vX] = vValue;
                            }
                            else
                            {
                                if (!MasksConfig.mMergeWaterForFS2004) //Set Land Only when not merge
                                {
                                    vValue = mAreaBitmapArray[vY, vX];
                                    vValue = (vValue & 0x00FFFFFF) | (0xFF000000); //Set LAnd
                                    mAreaBitmapArray[vY, vX] = vValue;
                                }
                            }
                        }
                        else
                        {
                            if (vIsWater)
                            {
                                vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                                vValue = vValue & 0x00FFFFFF; //SetWater
                                SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                            }
                            else
                            {
                                if (!MasksConfig.mMergeWaterForFS2004) //Set Land Only when not merge
                                {
                                    vValue = GetSourcePixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY);
                                    vValue = (vValue & 0x00FFFFFF) | (0xFF000000); //Set LAnd
                                    SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                                }
                            }
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
            }
        }


        public Boolean IsWaterOrWaterTransition(Int32 iX, Int32 iY)
        {
            Boolean vIsWater = false;

            Byte vCmdValue = 0;
            Byte vSubCmdValue = 0;

            //Check Pixel Commando    
            vCmdValue = mWorkCommandArray[iY, iX];

            vSubCmdValue = vCmdValue;
            vSubCmdValue &= 0xC0;

            if (vSubCmdValue == 0x00)
            {
                //Transitions

                //Water
                vSubCmdValue = vCmdValue;
                vSubCmdValue &= 0x03;

                switch (vSubCmdValue)
                {
                    case (Byte)(tWorkCommandCommand.eWaterTransitionWater):
                        {
                            vIsWater = true;
                            break;
                        }
                    case (Byte)(tWorkCommandCommand.eWaterTransitionTransition):
                        {
                            vIsWater = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                //WaterTwo
                vSubCmdValue = vCmdValue;
                vSubCmdValue &= 0x0C;

                switch (vSubCmdValue)
                {
                    case (Byte)(tWorkCommandCommand.eWater2TransitionWater):
                        {
                            vIsWater = true;
                            break;
                        }
                    case (Byte)(tWorkCommandCommand.eWater2TransitionTransition):
                        {
                            vIsWater = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            else
            {
                //Pools Pools overwrite everything!
                switch (vSubCmdValue)
                {
                    case (Byte)(tWorkCommandCommand.eFixWater):
                        {
                            vIsWater = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            return vIsWater;
        }



        // Non used function
        public void StreetsReconTheXway()
        {
            UInt32 vValueBuffer;
            
            Int32 vXStart;
            Int32 vXStop;
            Int32 vXSpot;

            Int32 vConX;
            Int32 vConY;
            Int32 vConX2;
            Int32 vConY2;

            if (mWorkMemoryAllocated)
            {
                for (vConY = 0; vConY < mPixelCountInY; vConY++)
                {
                    for (vConX = 0; vConX < mPixelCountInX; vConX++)
                    {
                        vValueBuffer = GetSourcePixel24BitBitmapFormat(vConX, vConY);
                        vValueBuffer &= 0x00FFFFFF;
                        if (vValueBuffer == 0x00FFFFFF)
                        {
                            vXStart = vConX;
                            vXStop = mPixelCountInX - 1;

                            SetDestinationPixel24BitBitmapFormat(vConX, vConY, 0x00FF8000);

                            for (vConX2 = vConX + 1; vConX2 < mPixelCountInX; vConX2++)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY);
                                vValueBuffer &= 0x00FFFFFF;
                                if (vValueBuffer == 0x00FFFFFF)
                                {
                                    SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY, 0x00FF8000);
                                }
                                else
                                {
                                    vXStop = vConX2;
                                    break; //stop
                                }

                            }
                            for (vConY2 = vConY + 1; vConY2 < mPixelCountInY; vConY2++)
                            {
                                vXSpot = mPixelCountInX;
                                for (vConX2 = vXStart; vConX2 <= vXStop; vConX2++)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                    vValueBuffer &= 0x00FFFFFF;
                                    if (vValueBuffer == 0x00FFFFFF)
                                    {
                                        vXSpot = vConX2;
                                        SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        break; //stop
                                    }

                                }
                                if (vXSpot < mPixelCountInX)
                                {
                                    for (vConX2 = vXSpot - 1; vConX2 > 0; vConX2--)
                                    {
                                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                        vValueBuffer &= 0x00FFFFFF;
                                        if (vValueBuffer == 0x00FFFFFF)
                                        {
                                            vXStart = vConX2;
                                            SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        }
                                        else
                                        {
                                            break; //stop
                                        }
                                    }
                                    for (vConX2 = vXSpot + 1; vConX2 < mPixelCountInX; vConX2++)
                                    {
                                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                        vValueBuffer &= 0x00FFFFFF;
                                        if (vValueBuffer == 0x00FFFFFF)
                                        {
                                            vXStop = vConX2;
                                            SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        }
                                        else
                                        {
                                            break; //stop
                                        }
                                    }
                                }
                                else
                                {
                                    break; //stop
                                }

                            }
                        }
                    }
                }
            }
        }


        // Non used function
        public void StreetsReconTheYway()
        {
            UInt32 vValueBuffer;
            Int32 vYStart;
            Int32 vYStop;
            Int32 vYSpot;

            Int32 vConY;
            Int32 vConX;
            Int32 vConY2;
            Int32 vConX2;

            if (mWorkMemoryAllocated)
            {

                for (vConX = 0; vConX < mPixelCountInX; vConX++)
                {
                    for (vConY = 0; vConY < mPixelCountInY; vConY++)
                    {
                        vValueBuffer = GetSourcePixel24BitBitmapFormat(vConX, vConY);
                        vValueBuffer &= 0x00FFFFFF;
                        if (vValueBuffer == 0x00FFFFFF)
                        {
                            vYStart = vConY;
                            vYStop = mPixelCountInY - 1;
                            SetDestinationPixel24BitBitmapFormat(vConX, vConY, 0x00FF8000);
                            for (vConY2 = vConY + 1; vConY2 < mPixelCountInY; vConY2++)
                            {
                                vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX, vConY2);
                                vValueBuffer &= 0x00FFFFFF;
                                if (vValueBuffer == 0x00FFFFFF)
                                {
                                    SetDestinationPixel24and32BitBitmapFormat(vConX, vConY2, 0x00FF8000);
                                }
                                else
                                {
                                    vYStop = vConY2;
                                    break; //stop
                                }

                            }
                            for (vConX2 = vConX + 1; vConX2 < mPixelCountInX; vConX2++)
                            {
                                vYSpot = mPixelCountInY;
                                for (vConY2 = vYStart; vConY2 <= vYStop; vConY2++)
                                {
                                    vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                    vValueBuffer &= 0x00FFFFFF;
                                    if (vValueBuffer == 0x00FFFFFF)
                                    {
                                        vYSpot = vConY2;
                                        SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        break; //stop
                                    }

                                }
                                if (vYSpot < mPixelCountInY)
                                {
                                    for (vConY2 = vYSpot - 1; vConY2 > 0; vConY2--)
                                    {
                                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                        vValueBuffer &= 0x00FFFFFF;
                                        if (vValueBuffer == 0x00FFFFFF)
                                        {
                                            vYStart = vConY2;
                                            SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        }
                                        else
                                        {
                                            break; //stop
                                        }
                                    }
                                    for (vConY2 = vYSpot + 1; vConY2 < mPixelCountInY; vConY2++)
                                    {
                                        vValueBuffer = GetSourcePixel24and32BitBitmapFormat(vConX2, vConY2);
                                        vValueBuffer &= 0x00FFFFFF;
                                        if (vValueBuffer == 0x00FFFFFF)
                                        {
                                            vYStop = vConY2;
                                            SetDestinationPixel24and32BitBitmapFormat(vConX2, vConY2, 0x00FF8000);
                                        }
                                        else
                                        {
                                            break; //stop
                                        }
                                    }
                                }
                                else
                                {
                                    break; //stop
                                }

                            }
                        }
                    }
                }
            }

        }


        public void ARGBBitmapTest()
        {
           Bitmap    m32AreaBitmap;                               
           UInt32[,] m32AreaBitmapArray;                         
           Graphics  m32AreaGraphics;                            
           GCHandle  m32GCHandle;                              
 
           m32AreaBitmap = new Bitmap(4, 4);  
           m32AreaGraphics = Graphics.FromImage(m32AreaBitmap);
           m32AreaBitmapArray = new UInt32[4, 4];
           m32GCHandle = GCHandle.Alloc(m32AreaBitmapArray);
                  
           try
           {

                m32GCHandle.Free();
                m32AreaBitmapArray = new UInt32[256, 256];  //Bitmap in Memory is [Y,X]. It does not work the other way!

                m32GCHandle = GCHandle.Alloc(m32AreaBitmapArray, GCHandleType.Pinned);
                IntPtr vPointer = Marshal.UnsafeAddrOfPinnedArrayElement(m32AreaBitmapArray, 0);
                m32AreaBitmap = new Bitmap(256, 256, 256 << 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb, vPointer);
                m32AreaGraphics = Graphics.FromImage(m32AreaBitmap);

                m32AreaBitmapArray[0,0] = 0x100000FF;
                m32AreaBitmapArray[0,1] = 0x1000FF00;
                m32AreaBitmapArray[0,2] = 0x10FF0000;
                m32AreaBitmapArray[0,3] = 0x10000000;

                m32AreaBitmapArray[0,4] = 0x330000FF;
                m32AreaBitmapArray[0,5] = 0x3300FF00;
                m32AreaBitmapArray[0,6] = 0x33FF0000;
                m32AreaBitmapArray[0,7] = 0x33000000;

                m32AreaBitmapArray[0,8] = 0x770000FF;
                m32AreaBitmapArray[0,9] = 0x7700FF00;
                m32AreaBitmapArray[0,10] = 0x77FF0000;
                m32AreaBitmapArray[0,11] = 0x77000000;

                //DoSomething
                m32AreaBitmap.Save("D:\\FSEarthTiles\\work\\ARGBTEST.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                m32AreaGraphics.Dispose();
                m32AreaBitmap.Dispose();
                m32GCHandle.Free();
                m32AreaBitmapArray = new UInt32[4, 4];
           }
           catch
           {
               Thread.Sleep(10); //For Debug only
           }

        }



        public void PrepareTrippleSTransitionTables()
        {

            Int32 vSampleNr;
            Double vX;
            Double vYSimple;
            Double vXMediate;
            Double vYDirect;

            for (Int32 vTrippleSType = 0; vTrippleSType < (Int32)(MasksConfig.tTransitionType.eSize); vTrippleSType++)
            {
                for (Int32 vTrippleSSubType = 0; vTrippleSSubType < (Int32)(MasksConfig.tTransitionSubType.eSize); vTrippleSSubType++)
                {
                    PrepareTrippleSTransitionConstants(vTrippleSType, vTrippleSSubType);

                    for (vSampleNr = 0; vSampleNr < cWaterTransitionTableSize; vSampleNr++)
                    {
                        vX = (Double)(vSampleNr) / (cWaterTransitionTableSize - 1); //vX is here once a linear once a square relation
                        vYSimple = TransitionTrippleSFunction(vTrippleSType, vTrippleSSubType, vX);

                        vXMediate = ConvertSquareRelationToLinearRelation(vX);
                        vYDirect = TransitionTrippleSFunction(vTrippleSType, vTrippleSSubType, vXMediate);

                        mTrippleSTableDistanceDirect[vTrippleSType, vTrippleSSubType, vSampleNr] = (Single)(vYSimple);
                        mTrippleSTableSquareDirect[vTrippleSType,   vTrippleSSubType, vSampleNr] = (Single)(vYDirect);

                        //Create Tables Without EntryFunction (TransitionS and ExitS streched to the range 0.0 .. 1.0)
                        Double vX1 = MasksConfig.mTrippleSFunctions[vTrippleSType, vTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x;
                        Double vXStreched = (1.0 - vX1) * vX + vX1;
                        Double vYStreched = TransitionTrippleSFunction(vTrippleSType, vTrippleSSubType, vXStreched);

                        Double vXMediateStreched = (1.0 - vX1) * vXMediate + vX1;
                        Double vYMediateStreched = TransitionTrippleSFunction(vTrippleSType, vTrippleSSubType, vXMediateStreched);

                        mTrippleSTableDistanceDirectStreched[vTrippleSType, vTrippleSSubType, vSampleNr] = (Single)(vYStreched);
                        mTrippleSTableSquareDirectStreched[vTrippleSType, vTrippleSSubType, vSampleNr] = (Single)(vYMediateStreched);


                    }
                }
            }

            //For Debug print out the function in a file
            /*
            for (Int32 vTrippleSType = 0; vTrippleSType < (Int32)(MasksConfig.tTransitionType.eSize); vTrippleSType++)
            {
                for (Int32 vTrippleSSubType = 0; vTrippleSSubType < (Int32)(MasksConfig.tTransitionSubType.eSize); vTrippleSSubType++)
                {
                    StreamWriter myStream;
                    myStream = new StreamWriter("D:\\FSEarthTiles\\worktest\\TrippleSTransitionCheckTable" + Convert.ToString(vTrippleSType) + Convert.ToString(vTrippleSSubType) + ".txt");
                    if (myStream != null)
                    {
                        for (vSampleNr = 0; vSampleNr < cWaterTransitionTableSize; vSampleNr++)
                        {

                            vX = (Single)(vSampleNr) / (cWaterTransitionTableSize - 1); //vX is here once a linear once a square relation

                            Single vXSquareRelation = (Single)(vX * vX / ((1.0 - vX) * (1.0 - vX) + vX * vX));
                            Single vYCheckBack = GetYofTrippleSTransitionFunctionTableWithSquareRelationInput(vTrippleSType, vTrippleSSubType, vXSquareRelation);

                            myStream.WriteLine(Convert.ToString(vX) + "     " + Convert.ToString(mTrippleSTableDistanceDirect[vTrippleSType, vTrippleSSubType, vSampleNr]) + "     " + Convert.ToString(mTrippleSTableSquareDirect[vTrippleSType, vTrippleSSubType, vSampleNr]) + "     " + vYCheckBack);
                        }
                        myStream.Close();
                    }
                }
            }
            */
        }

        protected void PrepareTrippleSTransitionConstants(Int32 iTrippleSType, Int32 iTrippleSSubType)
        {
            mTrippleSTransitionExitValues[iTrippleSType, iTrippleSSubType] = 1.0f;
            mTrippleSTransitionActive[iTrippleSType, iTrippleSSubType]     = false;

            if (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x == 1.0f)
            {
                mTrippleSTransitionExitValues[iTrippleSType, iTrippleSSubType] = (Single)(MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y);
            }

            if (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction)
            {
                mTrippleSTransitionExitValues[iTrippleSType, iTrippleSSubType] = 1.0f - mTrippleSTransitionExitValues[iTrippleSType, iTrippleSSubType];
            }

            if ((MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y  != 0.0f) || 
                (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y  != 0.0f))
            {
                mTrippleSTransitionActive[iTrippleSType, iTrippleSSubType] = true;
            }

            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy = 0.0;

            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitionx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony = 0.0;

            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExitx = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity = 0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity = 0.0;

            // General Linear Mapping Between two Points P1 and P2 
            // ax = 1 / (Px2  - Px1);
            // bx = -a * Px1;
            // ay = 1 / (Py2  - Py1);
            // by = -ay * Py1;

            if (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx = 1.0 / MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryx = 0.0;
            }
            if (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy = 1.0 / MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy = 0.0;
            }
            if ((MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x) != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx = 1.0 / (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitionx = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx * MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitionx = -MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x;
            }

            if ((MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y) != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony = 1.0 / (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony * MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony = -MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y;
            }
            if ((1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x) != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx = 1.0 / (1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExitx = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx * MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExitx = -MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x;

            }
            if ((1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y) != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity = 1.0 / (1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity * MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity = -MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2y;
            }


            // The S function first half, linear middle part and Second half          

            //We have to transform the Slopes to the local System at P1 and P2 because the Slopes are valid for the Full System
            Double vSlopeP1 = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1Slope;
            Double vSlopeP2 = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2Slope;

            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastFirstHalf       =  0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastSecondHalf      =  0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionFirstHalf  =  0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionSecondHalf =  0.0;
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterFirstHalf   =  0.0;      
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterSecondHalf  =  0.0;

            if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastSecondHalf = vSlopeP1 * mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastFirstHalf  = 0.0;
            }
            if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionFirstHalf  = vSlopeP1 * mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionSecondHalf = vSlopeP2 * mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx;
            }
            if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterFirstHalf = vSlopeP2 * mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterSecondHalf = 0.0;
            }

    
            // With First Half  y = a*x^n + b*x
            // And  Second Half y = d*x^m + e*x
            //
            // The derivations at point x1 respective x2 are:
            //                  y1' = n*a*x1^(n-1) + b
            //                  y2' = m*d*x2^(m-1) + e
            //   
            // and  middle part Slope = (1 - y1 -y2) / ( 1 - x1 - x2) 
            //
            // we can form the two equations  y1' == Slope and y2' == Slope
            //
            // That have this solution:
            //
            //          x1^(1-n) * ( (b-e)*x2 + m * (e*x2 - b*x2 + b - 1) )
            // a -> + ---------------------------------------------------------
            //                 n * m * (x1 + x2 -1) - m*x1 - n*x2
            //
            //
            //          x2^(1-m) * ( (e-b)*x1 + n * (b*x1 - e*x1 + e - 1) )
            // d -> + ---------------------------------------------------------
            //                n * m * (x1 + x2 -1) - m*x1 - n*x2
            //
            // Note that x1 = LinearSlopeBegin 
            //      and  x2 = (1- LinearSlopeEnd) !
            //
            //because we have both functions ancored on (0,0) for this calculation.
            //note that the formula for a and d is the same if you exchange the letters and x1 and x2

            Double vx1 = 0.0;
            Double vx2 = 0.0;
            Double va  = 0.0;
            Double vb  = 0.0;
            Double vd  = 0.0;
            Double ve  = 0.0;
            Double vn  = 0.0;
            Double vm  = 0.0;
            Double vNenner = 0.0;
            Double vFactor = 0.0;
            Double vy1 = 0.0;
            Double vy2 = 0.0;

            //Coast S
            vx1 = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeBegin;
            vx2 = 1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeEnd;           
            vb  = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastFirstHalf;
            ve  = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastSecondHalf;
            vn  = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionFirstHalfOrder;
            vm  = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionSecondHalfOrder;

            vNenner =  vn * vm * (vx1 + vx2 -1.0) - vm*vx1 - vn*vx2;
            vFactor = 0.0;

            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf  = Math.Pow(vx1,(1.0-vn)) * ( (vb-ve)*vx2 + vm * (ve*vx2 - vb*vx2 + vb - 1.0) );
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf = Math.Pow(vx2, (1.0 - vm)) * ((ve - vb) * vx1 + vn * (vb * vx1 - ve * vx1 + ve - 1.0));
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf  *= vFactor;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf *= vFactor;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf = 0.0;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf = 0.0;
            }

            va = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf;
            vd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf;

            vy1 = va * Math.Pow(vx1, vn) + vb * vx1;
            vy2 = vd * Math.Pow(vx2, vm) + ve * vx2;

            vNenner =  1.0 - vx1 - vx2;
            vFactor  = 0.0;
            
            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeCoastMiddle = (1.0 - vy1 - vy2); 
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeCoastMiddle *= vFactor; 
            }
            else
            {
                //we could set it zero because not used.. or set it to the true value which is the derivation at x1 of the FirstHalffunction (or the dev at x2 o fteh second half) 
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeCoastMiddle = vn*va*Math.Pow(vx1,(vn-1.0)) + vb; 
            }


            //Transition S
            vx1 = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeBegin;
            vx2 = 1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeEnd;
            vb = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionFirstHalf;
            ve = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionSecondHalf;
            vn = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionFirstHalfOrder;
            vm = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionSecondHalfOrder;

            vNenner = vn * vm * (vx1 + vx2 - 1.0) - vm * vx1 - vn * vx2;
            vFactor = 0.0;

            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf = Math.Pow(vx1, (1.0 - vn)) * ((vb - ve) * vx2 + vm * (ve * vx2 - vb * vx2 + vb - 1.0));
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf = Math.Pow(vx2, (1.0 - vm)) * ((ve - vb) * vx1 + vn * (vb * vx1 - ve * vx1 + ve - 1.0));
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf *= vFactor;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf *= vFactor;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf = 0.0;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf = 0.0;
            }

            va = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf;
            vd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf;

            vy1 = va * Math.Pow(vx1, vn) + vb * vx1;
            vy2 = vd * Math.Pow(vx2, vm) + ve * vx2;

            vNenner = 1.0 - vx1 - vx2;
            vFactor = 0.0;

            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeTransitionMiddle = (1.0 - vy1 - vy2);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeTransitionMiddle *= vFactor;
            }
            else
            {
                //we could set it zero because not used.. or set it to the true value which is the derivation at x1 of the FirstHalffunction (or the dev at x2 o fteh second half) 
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeTransitionMiddle = vn * va * Math.Pow(vx1, (vn - 1.0)) + vb;
            }


            //DeepWater S
            vx1 = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeBegin;
            vx2 = 1.0 - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeEnd;
            vb = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterFirstHalf;
            ve = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterSecondHalf;
            vn = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionFirstHalfOrder;
            vm = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionSecondHalfOrder;

            vNenner = vn * vm * (vx1 + vx2 - 1.0) - vm * vx1 - vn * vx2;
            vFactor = 0.0;

            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf = Math.Pow(vx1, (1.0 - vn)) * ((vb - ve) * vx2 + vm * (ve * vx2 - vb * vx2 + vb - 1.0));
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf = Math.Pow(vx2, (1.0 - vm)) * ((ve - vb) * vx1 + vn * (vb * vx1 - ve * vx1 + ve - 1.0));
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf *= vFactor;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf *= vFactor;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf = 0.0;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf = 0.0;
            }

            va = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf;
            vd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf;

            vy1 = va * Math.Pow(vx1, vn) + vb * vx1;
            vy2 = vd * Math.Pow(vx2, vm) + ve * vx2;

            vNenner = 1.0 - vx1 - vx2;
            vFactor = 0.0;

            if (vNenner != 0.0)
            {
                vFactor = 1.0 / vNenner;
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeDeepWaterMiddle = (1.0 - vy1 - vy2);
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeDeepWaterMiddle *= vFactor;
            }
            else
            {
                //we could set it zero because not used.. or set it to the true value which is the derivation at x1 of the FirstHalffunction (or the dev at x2 o fteh second half) 
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeDeepWaterMiddle = vn * va * Math.Pow(vx1, (vn - 1.0)) + vb;
            }

            Double vDistanceLimit = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionDistanceLimit;
            if (MasksConfig.AreLatLongParametersInConfigOK())
            {
                //Yes we have coords so its a KML so the input is meter
                Double vPixelAngleY = (MasksConfig.mAreaNWCornerLatitude - MasksConfig.mAreaSECornerLatitude) / (Double)(MasksConfig.mAreaPixelCountInY);
                Double vPixelAngleX = (MasksConfig.mAreaSECornerLongitude - MasksConfig.mAreaNWCornerLongitude) / (Double)(MasksConfig.mAreaPixelCountInX);
                if ((vPixelAngleY > 0.0) && (vPixelAngleX > 0.0))
                {
                     const Double cEarthCircumference  = 40075000;                            //[meter]
                     const Double cAngelDistance = cEarthCircumference / 360.0;               //[meter/Grad]
                     Double vPixelDistance = (vPixelAngleX * cAngelDistance + vPixelAngleY * cAngelDistance) / 2.0; //[meter]
                     vDistanceLimit = vDistanceLimit / vPixelDistance;                        //new unit Pixel
                }
            }

            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimit = (Single)(vDistanceLimit);
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitSquare = (Single)(vDistanceLimit * vDistanceLimit);
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceDistancePoint1Square = (Single)(MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x * MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x);
            mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitPoint1y = (Single)(MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1y);
            if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceDistancePoint1Square != 0.0)
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitMaxSquare = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitSquare / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceDistancePoint1Square;
            }
            else
            {
                mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitMaxSquare = 1.0f;
            }
            //For debugging
            //mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitPoint1y = 1.0f; //So we see it
        }

        // xLinearRelation = SquareRelationToLinearRelationConvertion(ixSquareRealtion);
        protected Double ConvertSquareRelationToLinearRelation(Double ixSquareRealtion)
        {
            Double vYIn  = ixSquareRealtion; //ixSquareRelation is the Y of the function y = x^2 / ((1-x)^2 + x^2);
            Double vXOut = 0.0;

            //we calculate the inverse of the function y = x^2 / (x^2 + (1-x)^2);
            //which solves to:
            //
            //       y +/- sqrt(y*(1-y))
            //   x =   -------------------
            //             2*y -1
            //
            // intersting, only the negative solution is of value for us and creates x in the range of 0.0 to 1.0 
            // this inverse function has a pole at 0.5

            if (vYIn == 0.5)
            {
                vXOut = 0.5;
            }
            else
            {
                vXOut = (vYIn - Math.Sqrt(vYIn * (1.0 - vYIn))) / (2.0 * vYIn - 1.0);
            }
            return vXOut;
        }


        // y = TrippleSFunction(ix);
        protected Double TransitionTrippleSFunction(Int32 iTrippleSType, Int32 iTrippleSSubType, Double ix)
        {

            Double vYOut = 0.0;
            Double vYStrobe = 0.0;

            Double vXCoast = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx * ix + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryx;
            Double vXTransition = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx * ix + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitionx;
            Double vXDeepWater = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx * ix + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExitx;

            Double vYCoastFirstHalf = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf * Math.Pow(vXCoast, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastFirstHalf * vXCoast;
            Double vYCoastSecondHalf = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf * Math.Pow((1.0 - vXCoast), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionSecondHalfOrder) - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastSecondHalf * (1.0-vXCoast) +1.0;

            Double vYTransitionFirstHalf = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf * Math.Pow(vXTransition, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionFirstHalf * vXTransition;
            Double vYTransitionSecondHalf = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf * Math.Pow((1.0 - vXTransition), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionSecondHalfOrder) - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionSecondHalf * (1.0-vXTransition) + 1.0;

            Double vYDeepWaterFirstHalf = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf * Math.Pow(vXDeepWater, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterFirstHalf * vXDeepWater;
            Double vYDeepWaterSecondHalf = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf * Math.Pow((1.0 - vXDeepWater), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionSecondHalfOrder) - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterSecondHalf * (1.0-vXDeepWater) + 1.0;

            if (ix < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x)   //[0.0 .. x1[ (excluding x1)
            {
                if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy != 0.0)
                {
                    if (vXCoast < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeBegin)
                    {
                        vYStrobe = vYCoastFirstHalf;
                    }
                    else if (vXCoast < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeEnd)
                    {
                        Double vXCoastFirstHalfEnd = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeBegin;
                        Double vYCoastFirstHalfEnd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf * Math.Pow(vXCoastFirstHalfEnd, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBCoastFirstHalf * vXCoastFirstHalfEnd;
                        vYStrobe = vYCoastFirstHalfEnd + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeCoastMiddle * (vXCoast - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionLinearSlopeBegin);
                    }
                    else
                    {
                        vYStrobe = vYCoastSecondHalf;
                    }
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy;
                }
            }
            else if (ix <= MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x) //[x1 .. x2] (including x1 and x2)
            {
                if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony != 0.0)
                {
                    if (vXTransition < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeBegin)
                    {
                        vYStrobe = vYTransitionFirstHalf;
                    }
                    else if (vXTransition < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeEnd)
                    {
                        Double vXTransitionFirstHalfEnd = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeBegin;
                        Double vYTransitionFirstHalfEnd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf * Math.Pow(vXTransitionFirstHalfEnd, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBTransitionFirstHalf * vXTransitionFirstHalfEnd;
                        vYStrobe = vYTransitionFirstHalfEnd + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeTransitionMiddle * (vXTransition - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionLinearSlopeBegin);
                    }
                    else
                    {
                        vYStrobe = vYTransitionSecondHalf;
                    }
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony;
                }
            }
            else   //]x2 .. 1.0] (excluding x2)
            {
                if (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity != 0.0)
                {
                    if (vXDeepWater < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeBegin)
                    {
                        vYStrobe = vYDeepWaterFirstHalf;
                    }
                    else if (vXDeepWater < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeEnd)
                    {
                        Double vXDeepWaterFirstHalfEnd = MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeBegin;
                        Double vYDeepWaterFirstHalfEnd = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf * Math.Pow(vXDeepWaterFirstHalfEnd, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionFirstHalfOrder) + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSBDeepWaterFirstHalf * vXDeepWaterFirstHalfEnd;
                        vYStrobe = vYDeepWaterFirstHalfEnd + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSSlopeDeepWaterMiddle * (vXDeepWater - MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionLinearSlopeBegin);
                    }
                    else
                    {
                        vYStrobe = vYDeepWaterSecondHalf;
                    }
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut = -mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity;
                }
            }

            if (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction)
            {
                vYOut = 1.0 - vYOut;
            }
            return vYOut;
        }


        /* older code keept for checking back the smoothing
        // y = TrippleSFunction(ix);
        protected Double TransitionTrippleSFunction(Int32 iTrippleSType, Int32 iTrippleSSubType, Double ix)
        {

           Double vYOut    = 0.0;
           Double vYStrobe = 0.0;

           Double vXCoast = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryx * ix + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryx;
           Double vXTransition   = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitionx * ix  + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitionx;
           Double vXDeepWater    = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExitx  * ix  + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExitx;
   
           Double vYCoastFirstHalf  =   mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastFirstHalf    * Math.Pow(vXCoast, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionFirstHalfOrder);
           Double vYCoastSecondHalf = - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSACoastSecondHalf  * Math.Pow((1.0 - vXCoast), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mEntrySFunctionSecondHalfOrder) + 1.0;
 
           Double vYCoastWightFirstHalf  = Math.Pow((1.0-vXCoast),MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);
           Double vYCoastWightSecondHalf = Math.Pow(vXCoast,MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);

           Double vYTransitionFirstHalf  =   mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionFirstHalf    * Math.Pow(vXTransition, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionFirstHalfOrder);
           Double vYTransitionSecondHalf = - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSATransitionSecondHalf  * Math.Pow((1.0 - vXTransition), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mTransitionSFunctionSecondHalfOrder) + 1.0;
 
           Double vYTransitionWightFirstHalf  = Math.Pow((1.0-vXTransition),MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);
           Double vYTransitionWightSecondHalf = Math.Pow(vXTransition,MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);

           Double vYDeepWaterFirstHalf  =   mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterFirstHalf    * Math.Pow(vXDeepWater, MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionFirstHalfOrder);
           Double vYDeepWaterSecondHalf = - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mSADeepWaterSecondHalf  * Math.Pow((1.0 - vXDeepWater), MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mExitSFunctionSecondHalfOrder) + 1.0;
 
           Double vYDeepWaterWightFirstHalf  = Math.Pow((1.0-vXDeepWater),MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);
           Double vYDeepWaterWightSecondHalf = Math.Pow(vXDeepWater,MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mFlipFunction);

   
            if (ix < MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x)   //[0.0 .. x1[ (excluding x1)
            {
                if (((vYCoastWightFirstHalf + vYCoastWightSecondHalf) != 0.0) && (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy != 0.0))
                {
                    vYStrobe = (vYCoastWightFirstHalf * vYCoastFirstHalf + vYCoastWightSecondHalf * vYCoastSecondHalf) / (vYCoastWightFirstHalf + vYCoastWightSecondHalf);
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAEntryy; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut =  - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBEntryy;
                }
            }
            else if (ix <= MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint2x) //[x1 .. x2] (including x1 and x2)
            {
                if (((vYTransitionWightFirstHalf + vYTransitionWightSecondHalf) != 0.0) && (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony != 0.0))
                {
                    vYStrobe = (vYTransitionWightFirstHalf * vYTransitionFirstHalf + vYTransitionWightSecondHalf * vYTransitionSecondHalf) / (vYTransitionWightFirstHalf + vYTransitionWightSecondHalf);
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapATransitiony; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut =  - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBTransitiony;
                }
            }
            else   //]x2 .. 1.0] (excluding x2)
            {
                if (((vYDeepWaterWightFirstHalf + vYDeepWaterWightSecondHalf) != 0.0) && (mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity != 0.0))
                {
                    vYStrobe = (vYDeepWaterWightFirstHalf * vYDeepWaterFirstHalf + vYDeepWaterWightSecondHalf * vYDeepWaterSecondHalf) / (vYDeepWaterWightFirstHalf + vYDeepWaterWightSecondHalf);
                    vYOut = (vYStrobe - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity) / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapAExity; // the inverse function of y' = ay*y+by, we want to have y
                }
                else
                {
                    vYOut =  - mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mLinearMapBExity;
                }
            }

            return vYOut;
        }
        */

        protected Single GetYofTrippleSTransitionFunctionTableWithSquare1AndSquare2Input(Int32 iTrippleSType, Int32 iTrippleSSubType, Single ixSquare1, Single ixSquare2)
        {
            if (mTrippleSTransitionActive[iTrippleSType, iTrippleSSubType])
            {
                if ((!MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mUseEntrySFunctionDistanceLimit) ||
                    (MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mSFunctionToSFunctionConnectionPoint1x == 0.0))
                {
                    //Normal
                    Single vSquareRelation = 0.0f;
                    if ((ixSquare1 + ixSquare2) != 0.0)
                    {
                        vSquareRelation = ixSquare1 / (ixSquare1 + ixSquare2);
                    }
                    Single vY = GetYofTrippleSTransitionFunctionTableWithSquareRelationInput(iTrippleSType, iTrippleSSubType, vSquareRelation);
                    return vY;
                }
                else if ((MasksConfig.mTrippleSFunctions[iTrippleSType, iTrippleSSubType].mStretchTransitionSAndExitSFunctionToFillAnyGap) &&
                         (ixSquare1 > mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitSquare))
                {
                    //Streched
                    //b^2 = c^2 + a^2 - 2ac where a=TheDistance Limit, b=The Distance1 - that limit und c = the original distance
                    //with b^2 = vNewSquare1 and c^2 = ixSquare1 follows:
                    //unfortunatly it seems we can not avoid a Square here.. :/  The only one thought.
                    Single vNewSquare1 = ixSquare1 + mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitSquare - 2.0f * mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimit * (Single)(Math.Sqrt(ixSquare1));
                    
                    Single vSquareRelation = 0.0f;
                    if ((vNewSquare1 + ixSquare2) != 0.0)
                    {
                        vSquareRelation = vNewSquare1 / (vNewSquare1 + ixSquare2);
                    }

                    Single vX = (cWaterTransitionTableSize - 1) * vSquareRelation;
                    Int32 vX1Int = (Int32)(vX); //Cut down
                    Int32 vX2Int = vX1Int + 1;
                    Single vXFraction = vX - (Single)(vX1Int);

                    if (vX1Int < 0)
                    {
                        vX1Int = 0;
                    }
                    if (vX1Int > ((Int32)cWaterTransitionTableSize - 1))
                    {
                        vX1Int = ((Int32)cWaterTransitionTableSize - 1);
                    }
                    if (vX2Int < 0)
                    {
                        vX2Int = 0;
                    }
                    if (vX2Int > ((Int32)cWaterTransitionTableSize - 1))
                    {
                        vX2Int = ((Int32)cWaterTransitionTableSize - 1);
                    }

                    Single vValue1 = mTrippleSTableSquareDirectStreched[iTrippleSType, iTrippleSSubType, vX1Int];
                    Single vValue2 = mTrippleSTableSquareDirectStreched[iTrippleSType, iTrippleSSubType, vX2Int];
                    Single vY = (1.0f - vXFraction) * vValue1 + vXFraction * vValue2;

                    return vY;
                }
                else if (ixSquare1 > mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitSquare) 
                {
                    Single vSquareRelation = 0.0f;
                    if ((ixSquare1 + ixSquare2) != 0.0)
                    {
                        vSquareRelation = ixSquare1 / (ixSquare1 + ixSquare2);
                    }
                    if (vSquareRelation < mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceDistancePoint1Square)
                    {
                        //constant part with Distance limit not streched (store value in the arre fit to Flip function)
                        Single vY = mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitPoint1y;
                        return vY;
                    }
                    else
                    {
                        //Normal
                        Single vY = GetYofTrippleSTransitionFunctionTableWithSquareRelationInput(iTrippleSType, iTrippleSSubType, vSquareRelation);
                        return vY;
                    }
                }
                else
                {
                    // Entry with Distance Limit... ..fix maximum
                    Single vSquareRelation = ixSquare1 / mTrippleSTransitionConstants[iTrippleSType, iTrippleSSubType].mEntryDistanceLimitMaxSquare;
                    Single vY = GetYofTrippleSTransitionFunctionTableWithSquareRelationInput(iTrippleSType, iTrippleSSubType, vSquareRelation);
                    return vY;
                }
            }
            else
            {
                return 0.0f;
            }
        }

        protected Single GetYofTrippleSTransitionFunctionTableWithSquareRelationInput(Int32 iTrippleSType, Int32 iTrippleSSubType, Single ixSquareRelation)
        {
            if (mTrippleSTransitionActive[iTrippleSType, iTrippleSSubType])
            {
                Single vX = (cWaterTransitionTableSize - 1) * ixSquareRelation;
                Int32 vX1Int = (Int32)(vX); //Cut down
                Int32 vX2Int = vX1Int + 1;
                Single vXFraction = vX - (Single)(vX1Int);

                if (vX1Int < 0)
                {
                    vX1Int = 0;
                }
                if (vX1Int > ((Int32)cWaterTransitionTableSize - 1))
                {
                    vX1Int = ((Int32)cWaterTransitionTableSize - 1);
                }
                if (vX2Int < 0)
                {
                    vX2Int = 0;
                }
                if (vX2Int > ((Int32)cWaterTransitionTableSize - 1))
                {
                    vX2Int = ((Int32)cWaterTransitionTableSize - 1);
                }

                Single vValue1 = mTrippleSTableSquareDirect[iTrippleSType, iTrippleSSubType, vX1Int];
                Single vValue2 = mTrippleSTableSquareDirect[iTrippleSType, iTrippleSSubType, vX2Int];
                Single vY = (1.0f - vXFraction) * vValue1 + vXFraction * vValue2;

                return vY;
            }
            else
            {
                return 0.0f;
            }
        }


        protected Single GetYofTrippleSTransitionFunctionTableWithLinearRelationInput(Int32 iTrippleSType, Int32 iTrippleSSubType, Single ixLinear)
        {
            if (mTrippleSTransitionActive[iTrippleSType, iTrippleSSubType])
            {
                Single vX = (cWaterTransitionTableSize - 1) * ixLinear;
                Int32 vX1Int = (Int32)(vX); //Cut down
                Int32 vX2Int = vX1Int + 1;
                Single vXFraction = vX - (Single)(vX1Int);

                if (vX1Int < 0)
                {
                    vX1Int = 0;
                }
                if (vX1Int > ((Int32)cWaterTransitionTableSize - 1))
                {
                    vX1Int = ((Int32)cWaterTransitionTableSize - 1);
                }
                if (vX2Int < 0)
                {
                    vX2Int = 0;
                }
                if (vX2Int > ((Int32)cWaterTransitionTableSize - 1))
                {
                    vX2Int = ((Int32)cWaterTransitionTableSize - 1);
                }

                Single vValue1 = mTrippleSTableDistanceDirect[iTrippleSType, iTrippleSSubType, vX1Int];
                Single vValue2 = mTrippleSTableDistanceDirect[iTrippleSType, iTrippleSSubType, vX2Int];
                Single vY = (1.0f - vXFraction) * vValue1 + vXFraction * vValue2;

                return vY;
            }
            else
            {
                return 0.0f;
            }
        }


        public void CreateAndSaveTransitionPlotGraphicBitmap()
        {
            try
            {
                if ((MasksConfig.mTransitionPlotGraphicSizeFactor >= 0.5f) && (MasksConfig.mTransitionPlotGraphicSizeFactor <= 10.0f))
                {

                    Int32 vPixelCountInX = Convert.ToInt32(MasksConfig.mTransitionPlotGraphicSizeFactor * 1024.0f);
                    Int32 vPixelCountInY = Convert.ToInt32(MasksConfig.mTransitionPlotGraphicSizeFactor * 704.0f);

                    Bitmap vTransitionPlotBitmap = new Bitmap(vPixelCountInX, vPixelCountInY);
                    Graphics vTransitionGraphics = Graphics.FromImage(vTransitionPlotBitmap);

                    PrepareTrippleSTransitionTables();

                    Single vXSpaceing = 20.0f;
                    Single vYSpaceing = 20.0f;

                    Single vSpaceX = ((Single)(vPixelCountInX) - 4.0f * vXSpaceing) / 3.0f;
                    Single vSpaceY = ((Single)(vPixelCountInY) - 3.0f * vYSpaceing) / 2.0f;

                    Single vWaterPlotXStart = vXSpaceing;
                    Single vWaterPlotXStop = vSpaceX + vXSpaceing;
                    Single vWater2PlotXStart = vSpaceX + 2.0f * vXSpaceing;
                    Single vWater2PlotXStop = 2.0f * vSpaceX + 2.0f * vXSpaceing;
                    Single vBlendPlotXStart = 2.0f * vSpaceX + 3.0f * vXSpaceing;
                    Single vBlendPlotXStop = 3.0f * vSpaceX + 3.0f * vXSpaceing;
                    Single vPlotYStop1 = vYSpaceing;
                    Single vPlotYStart1 = vSpaceY + vYSpaceing;
                    Single vPlotYStop2 = vSpaceY + 2.0f * vYSpaceing;
                    Single vPlotYStart2 = 2.0f * vSpaceY + 2.0f * vYSpaceing;

                    Pen vWaterPen = new Pen(Color.FromArgb(255, 0, 0, 128), 3.0f);
                    Pen vWater2Pen = new Pen(Color.FromArgb(255, 0, 128, 128), 3.0f);
                    Pen vBlendPen = new Pen(Color.FromArgb(255, 128, 0, 0), 3.0f);
                    SolidBrush vWhiteBackGroud = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
                    SolidBrush vPlotBrush = new SolidBrush(Color.FromArgb(255, 220, 220, 230));

                    vTransitionGraphics.FillRectangle(vWhiteBackGroud, 0.0f, 0.0f, (Single)(vPixelCountInX), (Single)(vPixelCountInY));
                    vTransitionGraphics.FillRectangle(vPlotBrush, vWaterPlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.FillRectangle(vPlotBrush, vWater2PlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.FillRectangle(vPlotBrush, vBlendPlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vWaterPen, vWaterPlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vWater2Pen, vWater2PlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vBlendPen, vBlendPlotXStart - 5.0f, vPlotYStop1 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);

                    vTransitionGraphics.FillRectangle(vPlotBrush, vWaterPlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.FillRectangle(vPlotBrush, vWater2PlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.FillRectangle(vPlotBrush, vBlendPlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vWaterPen, vWaterPlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vWater2Pen, vWater2PlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);
                    vTransitionGraphics.DrawRectangle(vBlendPen, vBlendPlotXStart - 5.0f, vPlotYStop2 - 5.0f, vSpaceX + 10.0f, vSpaceY + 10.0f);

                    Pen vTransparencyPen = new Pen(Color.FromArgb(192, 100, 150, 34), 3.0f);
                    Pen vReflectionPen = new Pen(Color.FromArgb(192, 160, 80, 89), 3.0f);
                    Pen vLightnessPen = new Pen(Color.FromArgb(192, 33, 50, 190), 3.0f);
                    Pen vColoringPen = new Pen(Color.FromArgb(192, 188, 33, 210), 3.0f);
                    Pen vSharpTransparencyPen = new Pen(Color.FromArgb(255, 100, 150, 34), 1.0f);
                    Pen vSharpReflectionPen = new Pen(Color.FromArgb(255, 160, 80, 89), 1.0f);
                    Pen vSharpLightnessPen = new Pen(Color.FromArgb(255, 33, 50, 190), 1.0f);
                    Pen vSharpColoringPen = new Pen(Color.FromArgb(255, 188, 33, 210), 1.0f);
                    Pen vSharpBlackPen = new Pen(Color.FromArgb(255, 0, 0, 0), 1.0f);

                    Pen vUsedPlotPenPen = vTransparencyPen;
                    Pen vSharpUsedPlotPenPen = vSharpTransparencyPen;

                    Int32 vFontSize = 12;
                    
                    if (MasksConfig.mTransitionPlotGraphicSizeFactor < 1.0)
                    {
                        vFontSize = 8;
                    }

                    Font vFont = new Font("Arial", vFontSize);
                    SolidBrush vFontTransparencyBrush = new SolidBrush(Color.FromArgb(192, 100, 150, 34));
                    SolidBrush vFontReflectionBrush = new SolidBrush(Color.FromArgb(192, 160, 80, 89));
                    SolidBrush vFontLightnessBrush = new SolidBrush(Color.FromArgb(192, 33, 50, 190));
                    SolidBrush vFontColoringBrush = new SolidBrush(Color.FromArgb(192, 188, 33, 210));

                    String vPlotString = "";
                    String vPlot2String = "";

                    Single vGraphicXStart = 0.0f;
                    Single vGraphicYStart = 0.0f;

                    Boolean vContinue = false;

                    for (Int32 vTrippleSType = 0; vTrippleSType < (Int32)(MasksConfig.tTransitionType.eSize); vTrippleSType++)
                    {
                        vContinue = false;
                        vPlotString = "";

                        if (vTrippleSType == (Int32)(MasksConfig.tTransitionType.eWaterTransition))
                        {
                            vPlotString += "Water";
                            vGraphicXStart = vWaterPlotXStart;
                            vContinue = true;
                        }
                        else if (vTrippleSType == (Int32)(MasksConfig.tTransitionType.eWater2Transition))
                        {
                            vPlotString += "WaterTwo";
                            vGraphicXStart = vWater2PlotXStart;
                            vContinue = true;
                        }
                        else if (vTrippleSType == (Int32)(MasksConfig.tTransitionType.eBlendTransition))
                        {
                            vPlotString += "Blend";
                            vGraphicXStart = vBlendPlotXStart;
                            vContinue = true;
                        }

                        if (vContinue)
                        {
                            for (Int32 vTrippleSSubType = 0; vTrippleSSubType < (Int32)(MasksConfig.tTransitionSubType.eSize); vTrippleSSubType++)
                            {
                                vContinue = false;
                                vPlot2String = vPlotString;

                                if (vTrippleSSubType == (Int32)(MasksConfig.tTransitionSubType.eTransparency))
                                {
                                    vPlot2String += " Transparency";
                                    vGraphicYStart = vPlotYStart1;
                                    vUsedPlotPenPen = vTransparencyPen;
                                    vSharpUsedPlotPenPen = vSharpTransparencyPen;
                                    vTransitionGraphics.DrawString(vPlot2String, vFont, vFontTransparencyBrush, new PointF(vGraphicXStart + 5.0f, vGraphicYStart - vSpaceY + 10.0f));
                                    vContinue = true;
                                }
                                else if (vTrippleSSubType == (Int32)(MasksConfig.tTransitionSubType.eReflection))
                                {
                                    vPlot2String += " Reflection";
                                    vGraphicYStart = vPlotYStart1;
                                    vUsedPlotPenPen = vReflectionPen;
                                    vSharpUsedPlotPenPen = vSharpReflectionPen;
                                    vTransitionGraphics.DrawString(vPlot2String, vFont, vFontReflectionBrush, new PointF(vGraphicXStart + 5.0f, vGraphicYStart - vSpaceY + 30.0f));
                                    vContinue = true;
                                }
                                else if (vTrippleSSubType == (Int32)(MasksConfig.tTransitionSubType.eLightness))
                                {
                                    vPlot2String += " Lightness";
                                    vGraphicYStart = vPlotYStart2;
                                    vUsedPlotPenPen = vLightnessPen;
                                    vSharpUsedPlotPenPen = vSharpLightnessPen;
                                    vTransitionGraphics.DrawString(vPlot2String, vFont, vFontLightnessBrush, new PointF(vGraphicXStart + 5.0f, vGraphicYStart - vSpaceY + 10.0f));
                                    vContinue = true;
                                }
                                else if (vTrippleSSubType == (Int32)(MasksConfig.tTransitionSubType.eColoring))
                                {
                                    vPlot2String += " Coloring";
                                    vGraphicYStart = vPlotYStart2;
                                    vUsedPlotPenPen = vColoringPen;
                                    vSharpUsedPlotPenPen = vSharpColoringPen;
                                    vTransitionGraphics.DrawString(vPlot2String, vFont, vFontColoringBrush, new PointF(vGraphicXStart + 5.0f, vGraphicYStart - vSpaceY + 30.0f));

                                    vContinue = true;
                                }

                                if (vContinue)
                                {
                                    Int32 vXPixels = Convert.ToInt32(vSpaceX);

                                    Single vLastX = 0.0f;
                                    Single vLastY = 0.0f;
                                    Single vNewX = 0.0f;
                                    Single vNewY = 0.0f;

                                    for (Int32 vX = 0; vX <= vXPixels; vX++)
                                    {
                                        Single vLinearRelation = (Single)(vX) / vSpaceX;
                                        if (vLinearRelation > 1.0f)
                                        {
                                            vLinearRelation = 1.0f;
                                        }
                                        Single vValue = GetYofTrippleSTransitionFunctionTableWithLinearRelationInput(vTrippleSType, vTrippleSSubType, vLinearRelation);
                                        Single vYPos = vValue * vSpaceY;

                                        vNewX = vGraphicXStart + (Single)(vX);
                                        vNewY = vGraphicYStart - vYPos;

                                        if (vX != 0)
                                        {
                                            vTransitionGraphics.DrawLine(vUsedPlotPenPen, vLastX, vLastY, vNewX, vNewY);
                                            vTransitionGraphics.DrawLine(vSharpUsedPlotPenPen, vLastX, vLastY, vNewX, vNewY);
                                            //vTransitionGraphics.DrawLine(vSharpBlackPen, vLastX, vLastY, vNewX, vNewY);
                                        }

                                        vLastX = vNewX;
                                        vLastY = vNewY;
                                    }

                                }
                            }
                        }
                    }

                    DirectoryInfo vPath = Directory.GetParent(MasksConfig.mAreaSourceBitmapFile);
                    vTransitionPlotBitmap.Save(vPath.FullName + "\\TransitionPlotGraphicBitmapFile.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

                    vTransitionGraphics.Dispose();
                    vTransitionPlotBitmap.Dispose();
                }

            }
            catch
            {
                //mostlikely Bitmap assign went wrong ...out of memory (or save went wrong)
                //ignore this error
            }
        }

        public void CreateCommandGraphicBitmap()
        {
            if (mWorkMemoryAllocated)
            {

                Int32 vPixelInQuadIndex;
                Int32 vUInt32IndexModal96;
                UInt32 vValue;

                UInt32 vRed   = 0;
                UInt32 vGreen = 0;
                UInt32 vBlue  = 0;

                Byte vCmdValue    = 0;
                Byte vSubCmdValue = 0;

                Int32 vY = 0;
                Int32 vX = 0;

                for (vY = 0; vY < mPixelCountInY; vY++)
                {

                    vPixelInQuadIndex = 0;
                    vUInt32IndexModal96 = 0;

                    for (vX = 0; vX < (mPixelCountInX); vX++)
                    {

                        //We don't use Alfa Channel here, drawing with Alfa channel causes inprecision in placement
                        vRed   = 50;
                        vGreen = 128; //Default make Land
                        vBlue  = 0;

                        //Check Pixel Commando    
                        vCmdValue = mWorkCommandArray[vY, vX];
                        vSubCmdValue = vCmdValue;
                        vSubCmdValue &= 0x03;

                        switch (vSubCmdValue)
                        {
                            case (Byte)(tWorkCommandCommand.eWaterTransitionWater):
                                {
                                    vGreen = 0;
                                    vBlue  = 200;
                                    break;
                                }
                            case (Byte)(tWorkCommandCommand.eWaterTransitionTransition):
                                {
                                    vGreen = 110;
                                    vBlue  = 150;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }

                        vCmdValue    = mWorkCommandArray[vY, vX];
                        vSubCmdValue = vCmdValue;
                        vSubCmdValue &= 0x0C;

                        switch (vSubCmdValue)
                        {
                            case (Byte)(tWorkCommandCommand.eWater2TransitionWater):
                                {
                                    vRed    = (vRed + 60)  >> 1;
                                    vGreen  = (vGreen + 80) >> 1;
                                    vBlue   = (vBlue + 240) >> 1;
                                    break;
                                }
                            case (Byte)(tWorkCommandCommand.eWater2TransitionTransition):
                                {
                                    vRed   = (vRed + 60)   >> 1;
                                    vGreen = (vGreen + 80) >> 1;
                                    vBlue  = (vBlue + 180) >> 1;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }

                        vCmdValue = mWorkCommandArray[vY, vX];
                        vSubCmdValue = vCmdValue;
                        vSubCmdValue &= 0x30;

                        switch (vSubCmdValue)
                        {
                            case (Byte)(tWorkCommandCommand.eBlendTransitionFullyTransparent):
                                {
                                    vRed = (vRed + 250) >> 1;
                                    vGreen = (vGreen + 40) >> 1;
                                    vBlue = (vBlue + 20) >> 1;
                                    break;
                                }
                            case (Byte)(tWorkCommandCommand.eBlendTransitionTransition):
                                {
                                    vRed = (vRed + 150) >> 1;
                                    vGreen = (vGreen + 50) >> 1;
                                    vBlue = (vBlue + 20) >> 1;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }


                        vCmdValue = mWorkCommandArray[vY, vX];
                        vSubCmdValue = vCmdValue;
                        vSubCmdValue &= 0xC0;

                        switch (vSubCmdValue)
                        {
                            case (Byte)(tWorkCommandCommand.eFixWater):
                                {
                                    vRed = 0;
                                    vGreen = 0;
                                    vBlue = 200;
                                    break;
                                }
                            case (Byte)(tWorkCommandCommand.eFixLand):
                                {
                                    vRed   = 0;
                                    vGreen = 200;
                                    vBlue  = 0;
                                    break;
                                }
                            case (Byte)(tWorkCommandCommand.eFixBlend):
                                {
                                    vRed   = 200;
                                    vGreen = 0;
                                    vBlue  = 0;
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }

                        vValue = 0xFF000000 | (vRed << 16) | (vGreen << 8) | (vBlue);
                        if (mDestination32BitBitmapMode)
                        {
                            mAreaBitmapArray[vY, vX] = vValue;
                        }
                        else
                        {
                            SetDestinationPixel24BitBitmapFormat(vUInt32IndexModal96, vPixelInQuadIndex, vY, vValue);
                        }

                        vPixelInQuadIndex++;
                        if (vPixelInQuadIndex >= 4)
                        {
                            vPixelInQuadIndex = 0;
                            vUInt32IndexModal96 += 3;
                        }
                    }
                }
                if (MasksConfig.mShowVectorsInCommandGraphicBitmap)
                {
                    DrawUserVisibleVectorsIntoWorkBitmap();
                }
            }
        }

        public void SaveCommandGraphicBitmap()
        {
            try
            {
                DirectoryInfo vPath = Directory.GetParent(MasksConfig.mAreaSourceBitmapFile);

                mAreaBitmap.Save(vPath.FullName + "\\CommandGraphicBitmap.bmp", System.Drawing.Imaging.ImageFormat.Bmp);
            }
            catch
            {
                //Save went wrong..ignore
            }
            MasksCommon.CollectGarbage();
        }


        public tLine CutLineOnBitmap(tLine iLine)
        {
            tLine vLine = iLine;

            try
            {
                if (!mPixelInfoInited)
                {
                    return vLine;
                }
                if ((vLine.mX1 < 0.0f) && (vLine.mX2 < 0.0f))
                {
                    //Complete outside
                    return vLine;
                }
                if ((vLine.mY1 < 0.0f) && (vLine.mY2 < 0.0f))
                {
                    //Complete outside
                    return vLine;
                }

                Single vXBorder = (Single)(mAreaBitmap.Width - 1);
                Single vYBorder = (Single)(mAreaBitmap.Height - 1);

                if ((vLine.mX1 > vXBorder) && (vLine.mX2 > vXBorder))
                {
                    //Complete outside
                    return vLine;
                }

                if ((vLine.mY1 > vYBorder) && (vLine.mY2 > vYBorder))
                {
                    //Complete outside
                    return vLine;
                }

                if ((vLine.mX1 >= 0.0f) && (vLine.mX1 <= vXBorder) &&
                    (vLine.mX2 >= 0.0f) && (vLine.mX2 <= vXBorder) &&
                    (vLine.mY1 >= 0.0f) && (vLine.mY1 <= vYBorder) &&
                    (vLine.mY2 >= 0.0f) && (vLine.mY2 <= vYBorder))
                {
                    //Complete inside
                    return vLine;
                }

                Single vDeltaX = vLine.mX2 - vLine.mX1;
                Single vDeltaY = vLine.mY2 - vLine.mY1;

                if ((vDeltaX != 0.0f) || (vDeltaY != 0.0f))
                {
                    if ((vDeltaX * vDeltaX) >= (vDeltaY * vDeltaY))
                    {
                        // X dominant -line

                        //X1 Cut left border?
                        if (vLine.mX1 < 0.0f)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY1 = (0.0f - vLine.mX1) * (vDeltaY / vDeltaX) + vLine.mY1;
                                vLine.mX1 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X1 Cut right border?
                        if (vLine.mX1 > vXBorder)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY1 = (vXBorder - vLine.mX1) * (vDeltaY / vDeltaX) + vLine.mY1;
                                vLine.mX1 = vXBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X2 Cut left border?
                        if (vLine.mX2 < 0.0f)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY2 = (0.0f - vLine.mX2) * (-vDeltaY / -vDeltaX) + vLine.mY2;
                                vLine.mX2 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X2 Cut right border?
                        if (vLine.mX2 > vXBorder)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY2 = (vXBorder - vLine.mX2) * (-vDeltaY / -vDeltaX) + vLine.mY2;
                                vLine.mX2 = vXBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }

                        //Y1 Cut Top border?
                        if (vLine.mY1 < 0.0f)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX1 = (0.0f - vLine.mY1) * (vDeltaX / vDeltaY) + vLine.mX1;
                                vLine.mY1 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y1 Cut bottom border?
                        if (vLine.mY1 > vYBorder)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX1 = (vYBorder - vLine.mY1) * (vDeltaX / vDeltaY) + vLine.mX1;
                                vLine.mY1 = vYBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y2 Cut Top border?
                        if (vLine.mY2 < 0.0f)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX2 = (0.0f - vLine.mY2) * (-vDeltaX / -vDeltaY) + vLine.mX2;
                                vLine.mY2 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y2 Cut bottom border?
                        if (vLine.mY2 > vYBorder)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX2 = (vYBorder - vLine.mY2) * (-vDeltaX / -vDeltaY) + vLine.mX2;
                                vLine.mY2 = vYBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }

                    }
                    else
                    {
                        // y dominant line
                        // same code as X dominant just cutting Y first.

                        //Y1 Cut Top border?
                        if (vLine.mY1 < 0.0f)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX1 = (0.0f - vLine.mY1) * (vDeltaX / vDeltaY) + vLine.mX1;
                                vLine.mY1 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y1 Cut bottom border?
                        if (vLine.mY1 > vYBorder)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX1 = (vYBorder - vLine.mY1) * (vDeltaX / vDeltaY) + vLine.mX1;
                                vLine.mY1 = vYBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y2 Cut Top border?
                        if (vLine.mY2 < 0.0f)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX2 = (0.0f - vLine.mY2) * (-vDeltaX / -vDeltaY) + vLine.mX2;
                                vLine.mY2 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //Y2 Cut bottom border?
                        if (vLine.mY2 > vYBorder)
                        {
                            //Yes
                            if (vDeltaY != 0.0f)
                            {
                                vLine.mX2 = (vYBorder - vLine.mY2) * (-vDeltaX / -vDeltaY) + vLine.mX2;
                                vLine.mY2 = vYBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }

                        //X1 Cut left border?
                        if (vLine.mX1 < 0.0f)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY1 = (0.0f - vLine.mX1) * (vDeltaY / vDeltaX) + vLine.mY1;
                                vLine.mX1 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X1 Cut right border?
                        if (vLine.mX1 > vXBorder)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY1 = (vXBorder - vLine.mX1) * (vDeltaY / vDeltaX) + vLine.mY1;
                                vLine.mX1 = vXBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X2 Cut left border?
                        if (vLine.mX2 < 0.0f)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY2 = (0.0f - vLine.mX2) * (-vDeltaY / -vDeltaX) + vLine.mY2;
                                vLine.mX2 = 0.0f;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }
                        //X2 Cut right border?
                        if (vLine.mX2 > vXBorder)
                        {
                            //Yes
                            if (vDeltaX != 0.0f)
                            {
                                vLine.mY2 = (vXBorder - vLine.mX2) * (-vDeltaY / -vDeltaX) + vLine.mY2;
                                vLine.mX2 = vXBorder;
                                vDeltaX = vLine.mX2 - vLine.mX1;
                                vDeltaY = vLine.mY2 - vLine.mY1;
                            }
                        }

                    }
                    return vLine;
                }
                else
                {
                    //shouldn't happen thought
                    return vLine;
                }
            }
            catch
            {
                //Ops!
                //shouldn't happen thought
                return vLine;
            }
        }

        public PointF[] CutPolygonOnBitmap(PointF[] iPolygon)
        {
            List<PointF> vCuttedPolygonPointFList = new List<PointF>();

            if (iPolygon.Length>=1)
            {
              tLine vLine;
              vLine.mX1 = 0.0f;
              vLine.mX2 = 0.0f;
              vLine.mY1 = 0.0f;
              vLine.mX2 = 0.0f;
              vLine.mDo = 0.0f;
              vLine.mDx = 0.0f;
              vLine.mDy = 0.0f;
              vLine.mUo = 0.0f;
              vLine.mUx = 0.0f;
              vLine.mUy = 0.0f;
              PointF vStartPoint = iPolygon[0];
              PointF vPoint1     = iPolygon[0];
               
              vCuttedPolygonPointFList.Add(new PointF(vStartPoint.X,vStartPoint.Y));

              for (Int32 vIndex = 1; vIndex <= iPolygon.Length; vIndex++)
              {
                  vLine.mX1 = vPoint1.X;
                  vLine.mY1 = vPoint1.Y;

                  if (vIndex < iPolygon.Length)
                  {
                      vLine.mX2 = iPolygon[vIndex].X;
                      vLine.mY2 = iPolygon[vIndex].Y;
                  }
                  else
                  {
                      vLine.mX2 = vStartPoint.X;
                      vLine.mY2 = vStartPoint.Y;
                  }
                  tLine vCutLine = CutLineOnBitmap(vLine);
                  if ((vLine.mX1 != vCutLine.mX1) ||
                      (vLine.mY1 != vCutLine.mY1))
                  {
                      vCuttedPolygonPointFList.Add(new PointF(vCutLine.mX1, vCutLine.mY1));
                  }
                  if ((vLine.mX2 != vCutLine.mX2) ||
                      (vLine.mY2 != vCutLine.mY2))
                  {
                      vCuttedPolygonPointFList.Add(new PointF(vCutLine.mX2, vCutLine.mY2));
                  }

                  if (vIndex < iPolygon.Length)
                  {
                      vCuttedPolygonPointFList.Add(iPolygon[vIndex]);
                      vPoint1 = iPolygon[vIndex];
                  }
                  else
                  {
                      //Don't add Startpoint. 
                  }
              }

              PointF[] vCutPolygon = new PointF[vCuttedPolygonPointFList.Count];

              Int32 vCount = 0;

              foreach (PointF vPoint in vCuttedPolygonPointFList)
              {
                  vCutPolygon[vCount] = vPoint;
                  vCount++;
              }

              return vCutPolygon;
            }
            else
            {
                return iPolygon;
            }

        }

        public Int32 GetPixelCountInX()
        {
            if (mPixelInfoInited)
            {
                return mPixelCountInX;
            }
            else
            {
                return 0;
            }
        }

        public Int32 GetPixelCountInY()
        {
            if (mPixelInfoInited)
            {
                return mPixelCountInY;
            }
            else
            {
                return 0;
            }
        }

        public Boolean IsPixelInfoInited()
        {
          return mPixelInfoInited;
        }

    }
}
