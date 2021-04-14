using System;
using System.Collections.Generic;
using System.Text;

namespace FSEarthTilesInternalDLL
{
    public class AreaInfoFIFOQueue
    {
       public AreaInfoFIFOQueue(Int32 iSize)
        {
            mAreaInfoStorage = new AreaInfo[iSize];
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

        public int GetLastEntryIndex()
        {


            return mSize-1;


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

        public AreaInfo GetNextAreaInfoClone()  //Read And Remove
        {
           return new AreaInfo(GetNextAreaInfoReference());
        }

        public AreaInfo GetNextAreaInfoReference()  //Read And Remove
        {
            AreaInfo vAreaInfo = new AreaInfo();
            if (!IsEmpty())
            {
                vAreaInfo = mAreaInfoStorage[mReadIndex];
                IncreaseReadIndex();
            }
            return vAreaInfo;
        }


        public void AddAreaInfo(AreaInfo iAreaInfo)        //We always clone on add
        {
           if (!IsFull())
           {
             mAreaInfoStorage[mWriteIndex] = new AreaInfo(iAreaInfo);
             IncreaseWriteIndex();
           }
        }

        public void AddAreaInfoRandomPosition(AreaInfo iAreaInfo)        //We always clone on add
        {
            if (!IsFull())
            {
                Int32 vRandom = mReadIndex + mRandomGenerator.Next(0, mEntries + 1); //a Randomnumber between 0 and mEntries
                if (vRandom >= mSize)
                {
                    vRandom -= mSize;
                }
                AreaInfo vAreaInfo = mAreaInfoStorage[vRandom];
                mAreaInfoStorage[mWriteIndex] = vAreaInfo;
                mAreaInfoStorage[vRandom]     = new AreaInfo(iAreaInfo);
                IncreaseWriteIndex();
            }
        }
   

        public void AddAreaInfoNoDoubles(AreaInfo iAreaInfo) //We always clone on add
        {
            if (!IsFull())
            {
                Boolean vAleradyExist = false;
                Int32 vLocalReadIndex = mReadIndex;
                for (Int32 vCon = 1; vCon <= mEntries; vCon++)
                {
                    if (AreaInfo.Equals(iAreaInfo,mAreaInfoStorage[vLocalReadIndex]))
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
                    mAreaInfoStorage[mWriteIndex] = new AreaInfo(iAreaInfo);
                    IncreaseWriteIndex();
                }
            }
        }


        public void AddAreaInfoNoDoublesRandomPosition(AreaInfo iAreaInfo) //We always clone on add
        {
            if (!IsFull())
            {
                Boolean vAleradyExist = false;
                Int32 vLocalReadIndex = mReadIndex;
                for (Int32 vCon = 1; vCon <= mEntries; vCon++)
                {
                    if (AreaInfo.Equals(iAreaInfo, mAreaInfoStorage[vLocalReadIndex]))
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
                    AreaInfo vAreaInfo = mAreaInfoStorage[vRandom];
                    mAreaInfoStorage[mWriteIndex] = vAreaInfo;
                    mAreaInfoStorage[vRandom] = new AreaInfo(iAreaInfo);
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
        public AreaInfo[] mAreaInfoStorage;
        private Int32 mSize;
        private Int32 mEntries;
        private Int32 mWriteIndex;
        private Int32 mReadIndex;
        private Random mRandomGenerator;
    }
}
