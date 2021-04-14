using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace FSEarthTilesInternalDLL
{

    public class Tile
    {
        public Tile()
        {
            mGoodBitmap          = false;
            mBitmapValid         = false;
            mTileInfo            = new TileInfo();
        }

        public Tile(TileInfo iTileInfo)
        {
            mGoodBitmap  = false;
            mBitmapValid = false;
            mTileInfo    = new TileInfo(iTileInfo);
        }

        public Tile(Tile iTile)
        {
            if (iTile.mBitmapValid)
            {
                mTileBitmap = (Bitmap)iTile.mTileBitmap.Clone();
            }
            mGoodBitmap  =  iTile.mGoodBitmap;
            mBitmapValid =  iTile.mBitmapValid;
            mTileInfo    =  new TileInfo(iTile.mTileInfo);
        }

        public Tile(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iLevel, Int32 iService,Boolean iSkipTile)
        {
            mGoodBitmap  = false;
            mBitmapValid = false;
            mTileInfo    = new TileInfo(iAreaCodeX, iAreaCodeY, iLevel, iService,iSkipTile);
        }

        public Tile Clone()
        {
            return new Tile(this);
        }


        public void Clear()
        {
            FreeBitmap();
            mGoodBitmap = false;
            mBitmapValid = false;
            mTileInfo = new TileInfo();
        }

        public Boolean Equals(Tile iTile)
        {
            Boolean vIsEqual = false;

            if ((mGoodBitmap  == iTile.mGoodBitmap) &&
                (mBitmapValid == iTile.mBitmapValid) &&
                (mTileInfo.Equals(iTile.mTileInfo)))
            {
                vIsEqual = true;
            }
            return vIsEqual;
        }

        public Boolean EqualsInfo(TileInfo iTileInfo)
        {
            Boolean vIsEqual = false;

            if (mTileInfo.Equals(iTileInfo))
            {
                vIsEqual = true;
            }
            return vIsEqual;
        }

        static public Boolean Equals(Tile iTile1, Tile iTile2)
        {
            return iTile1.Equals(iTile2);
        }

        public void StoreBitmap(Bitmap iTileBitmap) //We clone on store
        {
            FreeBitmap();
            mTileBitmap = (Bitmap)iTileBitmap.Clone();
            mBitmapValid = true;
        }

        public void FreeBitmap()
        {
            if (mBitmapValid)
            {
                mTileBitmap.Dispose();
                mGoodBitmap  = false;
                mBitmapValid = false;
            }
        }

        public Boolean IsGoodBitmap()
        {
           return mGoodBitmap;
        }

        public Boolean IsValidBitmap()
        {
          return mBitmapValid;
        }

        public void MarkAsGoodBitmap()
        {
           mGoodBitmap = true;
        }

        public void MarkAsBadBitmap()
        {
           mGoodBitmap = false;
        }

        public Bitmap GetBitmapReference()
        {
            return mTileBitmap;
        }

        public Bitmap GetBitmapClone()
        {
            return (Bitmap)mTileBitmap.Clone();
        }


        public Boolean IsSuspicious()
        {
            Boolean vIsSuspicious = false;

            if (mGoodBitmap && mBitmapValid)
            {
                vIsSuspicious = true;

                Color vRef = mTileBitmap.GetPixel(255, 255);

                for (Int32 vX = 240; vX < 256; vX++)
                {
                    if (vRef != mTileBitmap.GetPixel(vX, 255))
                    {
                        vIsSuspicious = false;
                        break;
                    }
                }

                if (vIsSuspicious)
                {
                    //Picture has a white ending
                    vIsSuspicious = false;

                    for (Int32 vX = 240; vX < 256; vX++)
                    {
                        if (vRef != mTileBitmap.GetPixel(vX, 0))
                        {
                            //Start has something, End has nothing -> Broken Tile (messed)
                            vIsSuspicious = true;
                            break;
                        }
                    }

                    if (!vIsSuspicious)
                    {
                        //Picture has a white ending And a white beginning
                        //now we check middle parts and rest
                        vIsSuspicious = true;

                        for (Int32 vX = 120; vX < 136; vX++)
                        {
                            if (vRef != mTileBitmap.GetPixel(vX, 128))
                            {
                                vIsSuspicious = false;
                                break;
                            }
                        }

                        if (vIsSuspicious)
                        {
                            for (Int32 vX = 0; vX < 255; vX++)
                            {
                                if (vRef != mTileBitmap.GetPixel(vX, 128))
                                {
                                    vIsSuspicious = false;
                                    break;
                                }
                            }
                        }

                        if (vIsSuspicious)
                        {
                            for (Int32 vX = 0; vX < 255; vX++)
                            {
                                if (vRef != mTileBitmap.GetPixel(vX, 64))
                                {
                                    vIsSuspicious = false;
                                    break;
                                }
                            }
                        }

                        if (vIsSuspicious)
                        {
                            for (Int32 vX = 0; vX < 255; vX++)
                            {
                                if (vRef != mTileBitmap.GetPixel(vX, 192))
                                {
                                    vIsSuspicious = false;
                                    break;
                                }
                            }
                        }

                        if (vIsSuspicious)
                        {
                            for (Int32 vY = 0; vY < 255; vY += 8)
                            {
                                for (Int32 vX = 0; vX < 255; vX++)
                                {
                                    if (vRef != mTileBitmap.GetPixel(vX, vY))
                                    {
                                        vIsSuspicious = false;
                                        break;
                                    }
                                }
                            }
                        }

                        //If it is still Suspicious than it is probabily  a unique colored tile and this is suspecious

                    }
                }

            }

            return vIsSuspicious;
        }


        //Public Datas
        public TileInfo mTileInfo;

        //Private Datas
        private Bitmap  mTileBitmap;
        private Boolean mGoodBitmap;  //true = Bitmap is ok and downloaded, false means download failed -> NoTileFound Bitmap or LastValidBitmap.
        private Boolean mBitmapValid; //true =  Class contains a valid Bitmap. false = Class contains no valid Bitmap;
    }
}
