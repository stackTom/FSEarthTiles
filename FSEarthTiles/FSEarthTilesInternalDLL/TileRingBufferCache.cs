using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace FSEarthTilesInternalDLL
{
    public class TileRingBufferCache
    {

        public TileRingBufferCache(Int32 iSize)
        {
            mCacheSize = iSize;
            mTileStorage = new Tile[mCacheSize];
            mCacheIndex = 0;

            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                mTileStorage[vCon] = new Tile();
            }

        }


        public void EmptyTileCache()
        {
            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                mTileStorage[vCon].Clear();
            }

            mCacheIndex = 0;
        }

        public Boolean IsTileInCache(TileInfo iTileInfo)
        {
            Boolean vIsInCache = false;

            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                if (mTileStorage[vCon].EqualsInfo(iTileInfo))
                {
                    vIsInCache = true;
                    vCon = mCacheSize; //abort
                }
            }
            return vIsInCache;
        }


        public Tile GetTile(TileInfo iTileInfo)
        {
            Tile vTile = new Tile();

            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                if (mTileStorage[vCon].EqualsInfo(iTileInfo))
                {
                    vTile = mTileStorage[vCon].Clone();
                    vCon = mCacheSize; //abort
                }
            }
            return vTile;
        }

        public Tile GetTileReference(TileInfo iTileInfo)
        {
            Tile vTile = new Tile();

            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                if (mTileStorage[vCon].EqualsInfo(iTileInfo))
                {
                    vTile = mTileStorage[vCon];
                    vCon = mCacheSize; //abort
                }
            }
            return vTile;
        }

        public Bitmap GetTileBitmapReference(TileInfo iTileInfo)
        {
            Bitmap vBitmap = null;

            for (Int32 vCon = 0; vCon < mCacheSize; vCon++)
            {
                if (mTileStorage[vCon].EqualsInfo(iTileInfo))
                {
                    vBitmap = mTileStorage[vCon].GetBitmapReference();
                    vCon = mCacheSize; //abort
                }
            }
            return vBitmap;
        }

        public void AddTileOverwriteOldTiles(Tile iTile)
        {
            mTileStorage[mCacheIndex].Clear();
            mTileStorage[mCacheIndex] = iTile.Clone();

            mCacheIndex++;
            if (mCacheIndex >= mCacheSize)
            {
                mCacheIndex = 0;
            }
        }


        private Tile [] mTileStorage;
        private Int32 mCacheSize;
        private Int32 mCacheIndex;
    }
}
