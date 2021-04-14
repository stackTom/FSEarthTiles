using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public class TileInfoFIFOQueue
    {

        public TileInfoFIFOQueue(Int32 iSize)
        {
            mTileInfoStorage = new TileInfo[iSize];
            mSize       = iSize;
            mEntries    = 0;
            mWriteIndex = 0;
            mReadIndex  = 0;
            mRandomGenerator = new Random();
        }

        public Boolean IsFull()
        {
           Boolean vIsFull = (mEntries>=mSize);
           return  vIsFull;
        }

        public  Boolean IsEmpty()
        {
           Boolean vIsEmpty = (mEntries<=0);
           return  vIsEmpty;
        }

        public void DoEmpty()
        {
            mEntries    = 0;
            mWriteIndex = 0;
            mReadIndex  = 0;
        }


        public Int32 GetEntriesCount()
        {
            return mEntries;
        }

        public Int32 GetFreeSpace()
        {
            Int32  vFreeSpace = mSize - mEntries;
            return vFreeSpace;
        }

        public TileInfo GetNextTileInfoClone()  //Read And Remove
        {
           return new TileInfo(GetNextTileInfoReference());
        }

        public TileInfo GetNextTileInfoReference()  //Read And Remove
        {
            TileInfo vTileInfo = new TileInfo();
            if (!IsEmpty())
            {
                vTileInfo = mTileInfoStorage[mReadIndex];
                IncreaseReadIndex();
            }
            return vTileInfo;
        }


        public void AddTileInfo(TileInfo iTileInfo)        //We always clone on add
        {
           if (!IsFull())
           {
             mTileInfoStorage[mWriteIndex] = new TileInfo(iTileInfo);
             IncreaseWriteIndex();
           }
        }

        public void AddTileInfoRandomPosition(TileInfo iTileInfo)        //We always clone on add
        {
            if (!IsFull())
            {
                //ok I coded lazy and dangerous. The methode was only meant to be used with a fresh and empty allocated FIFOQueue,
                //thanks to fly-a-lot for pointing out that I have to use the ReadIndex to make the methode work for every case
                Int32 vRandom = mReadIndex + mRandomGenerator.Next(0, mEntries + 1); //a Randomnumber between 0 and mEntries
                if (vRandom >= mSize)                                                //(If Entries = 2 than we have 3 valid indexpositions to fill the value in: 0,1,2)
                {
                    vRandom -= mSize;
                }
                TileInfo vTileInfo = mTileInfoStorage[vRandom]; //this is just for better debug sure I can assign it directly also
                mTileInfoStorage[mWriteIndex] = vTileInfo;
                mTileInfoStorage[vRandom]     = new TileInfo(iTileInfo);
                IncreaseWriteIndex();
            }
        }
   

        public void AddTileInfoNoDoubles(TileInfo iTileInfo) //We always clone on add
        {
            if (!IsFull())
            {
                Boolean vAleradyExist = false;
                Int32 vLocalReadIndex = mReadIndex;
                for (Int32 vCon = 1; vCon <= mEntries; vCon++)
                {
                    if (TileInfo.Equals(iTileInfo,mTileInfoStorage[vLocalReadIndex]))
                   {
                       vAleradyExist = true;
                       vCon          = mEntries; //Abort
                   }
                   vLocalReadIndex++;
                   if (vLocalReadIndex >= mSize)
                   {
                       vLocalReadIndex = 0;
                   }
                }
                if (!vAleradyExist)
                {
                    mTileInfoStorage[mWriteIndex] = new TileInfo(iTileInfo);
                    IncreaseWriteIndex();
                }
            }
        }


        public void AddTileInfoNoDoublesRandomPosition(TileInfo iTileInfo) //We always clone on add
        {
            if (!IsFull())
            {
                Boolean vAleradyExist = false;
                Int32 vLocalReadIndex = mReadIndex;
                for (Int32 vCon = 1; vCon <= mEntries; vCon++)
                {
                    if (TileInfo.Equals(iTileInfo, mTileInfoStorage[vLocalReadIndex]))
                    {
                        vAleradyExist = true;
                        vCon = mEntries; //Abort
                    }
                    vLocalReadIndex++;
                    if (vLocalReadIndex >= mSize)
                    {
                        vLocalReadIndex = 0;
                    }
                }
                if (!vAleradyExist)
                {
                    Int32 vRandom = mReadIndex + mRandomGenerator.Next(0, mEntries + 1); //a Randomnumber between 0 and mEntries
                    if (vRandom >= mSize)                                               
                    {
                        vRandom -= mSize;
                    }
                    TileInfo vTileInfo = mTileInfoStorage[vRandom];
                    mTileInfoStorage[mWriteIndex] = vTileInfo;
                    mTileInfoStorage[vRandom] = new TileInfo(iTileInfo);
                    IncreaseWriteIndex();
                }
            }
        }


        //Private
        private void  IncreaseReadIndex()
        {
           mReadIndex++;
           if (mReadIndex>=mSize)
           {
              mReadIndex = 0;
           }
            mEntries--;
        }
       

        private void  IncreaseWriteIndex()
        {
           mWriteIndex++;
           if (mWriteIndex>=mSize)
           {
              mWriteIndex = 0;
           }
            mEntries++;
        }


        //Datas
        private TileInfo[] mTileInfoStorage;
        private Int32 mSize;
        private Int32 mEntries;
        private Int32 mWriteIndex;
        private Int32 mReadIndex;
        private Random mRandomGenerator;

    }



    public class DownloadTiles
    {

        public List<TileInfo> mTileInfo=new List<TileInfo>();


        public Int64 TotalTiles2Download()
        {

            Int64 TotalTiles = 0;

            for (int i = 0; i < this.mTileInfo.Count; i++)
            {
                if (this.mTileInfo[i].mSkipTile == false)
                {
                    TotalTiles++;
                }
            }
            return TotalTiles;


        }


    }
}
