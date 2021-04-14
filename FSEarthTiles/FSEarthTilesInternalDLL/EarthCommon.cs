using System;
using System.Collections.Generic;
using System.Text;

//-------------------------------------------------------------------------------------
// 
//  shared definitions and methodes for the whole project
// 
//-------------------------------------------------------------------------------------

namespace FSEarthTilesInternalDLL
{
    public static class EarthCommon //didn't want to make a calss but somehow id doesn't compile without (end of my C# knowledge lol!
    {
        public static Boolean StringCompare(String iString1, String iString2)
        {
            Boolean vIsEqual = String.Equals(iString1, iString2, StringComparison.CurrentCultureIgnoreCase);
            return vIsEqual;
        }

        public static void CollectGarbage()
        {
           GC.Collect();
           GC.WaitForPendingFinalizers();  
        }


        public static Boolean StringContains(String iWhole, String iPart)
        {
            //different to Contains we want IgnoreCase
            Int32 vIndex = iWhole.IndexOf(iPart, StringComparison.CurrentCultureIgnoreCase);
            if (vIndex >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


    }
}



