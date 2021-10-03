using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using FSEarthTilesInternalDLL;
using CSScriptLibrary;   //Third party library
using System.Reflection; //for Assembly class

namespace FSEarthTilesDLL
{
    class EarthScriptsHandler
    {
        static Boolean           mCSTileCodeingScriptLoaded = false;
        static TileCodeingScript mTileCodeingScript = new TileCodeingScript();

        static Boolean                    mCSAreaInfoFileCreationScriptLoaded = false;
        static AreaInfoFileCreationScript mAreaInfoFileCreationScript = new AreaInfoFileCreationScript();
         
        static Boolean                    mCSCustomizedProcessesScriptLoaded = false;
        static CustomizedProcessesScript  mCustomizedProcessesScript = new CustomizedProcessesScript();


        static AsmHelper mAsmTileCodeingScriptHelper;
        static Object    mCSTileCodeingScriptObject;

        static AsmHelper mAsmAreaInfoFileCreationScriptHelper;
        static Object    mCSAreaInfoFileCreationScriptObject;

        static AsmHelper mAsmCustomizedProcessesScriptHelper;
        static Object    mCSCustomizedProcessesScriptObject;

        static private Object mMethodeCallLock = new Object();

        static List<String> mTempFilesToDelete = new List<String>();

        public EarthScriptsHandler()
        {
        }

        static public void CleanUp()
        {
            try
            {
                FreeObjects();

                EarthCommon.CollectGarbage();

                //unfortunatly that above does not unload the assembly 
                //It's impossible to unload an assemply in the main ApplDomain I read.  But they have to be in the main ApplDomain that the common data shareing works. 
                //The assembly obligate temp files can not be deleted within the prog because the windows system protectes them as long as the application is running
                //Therefore we have to fire a clean up program for that job that is deleting the files when FSET appl died.
                if (mTempFilesToDelete.Count > 0)
                {
                    String vArgument = "";

                    foreach (String vFileToDelete in mTempFilesToDelete)
                    {
                        vArgument = vArgument + "\"" + vFileToDelete + "\" ";
                    }

                    mTempFilesToDelete.Clear();

                    System.Diagnostics.Process proc = new System.Diagnostics.Process();

                    proc.StartInfo.FileName  = EarthConfig.mStartExeFolder + "\\" + "FSETScriptsTempFilesCleanUp.exe";
                    proc.StartInfo.Arguments = vArgument;
                    proc.Start();
                }

            }
            catch
            {
                //ignore error
            }
        }

        static private void FreeObjects()
        {
           
            mCSTileCodeingScriptObject          = null;
            mCSAreaInfoFileCreationScriptObject = null;
            mCSCustomizedProcessesScriptObject  = null;

            if (mAsmTileCodeingScriptHelper != null)
            {
                mAsmTileCodeingScriptHelper.Dispose();
                mAsmTileCodeingScriptHelper = null;
            }
            if (mAsmAreaInfoFileCreationScriptHelper != null)
            {
                mAsmAreaInfoFileCreationScriptHelper.Dispose();
                mAsmAreaInfoFileCreationScriptHelper = null;
            }
            if (mAsmCustomizedProcessesScriptHelper != null)
            {
                mAsmCustomizedProcessesScriptHelper.Dispose();
                mAsmCustomizedProcessesScriptHelper = null;
            }

        }


        //Will be called from FSEarthTilesForm on First Main Tick Event
        static public void TryInstallTileCodeingScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            lock (mMethodeCallLock)
            {
                if (EarthConfig.mUseCSharpScripts)
                {
                    if (!mCSTileCodeingScriptLoaded)
                    {
                        String vScriptFileNameAndPath = iScriptPath + "\\" + "TileCodeingScript.cs";

                        if (File.Exists(vScriptFileNameAndPath))
                        {
                            try
                            {
                                List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                                if (vScript.Count > 0)
                                {
                                    mAsmTileCodeingScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                    mCSTileCodeingScriptObject = GetCSTileCodeingScriptObject();
                                    mCSTileCodeingScriptLoaded = true;
                                }
                            }
                            catch (System.Exception e)
                            {

                                //LoadingWentWrong
                                String vError = e.ToString();
                                MessageBox.Show(vError, "Warning! Failed To Install C#Script: TileCodeingScript.cs! FSET continues with internal default routines.");
                            }
                        }
                    }
                }
            }
        }

        static public void TryInstallAreaInfoFileCreationScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (EarthConfig.mUseCSharpScripts)
            {
                if (!mCSAreaInfoFileCreationScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "AreaInfoFileCreationScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmAreaInfoFileCreationScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mCSAreaInfoFileCreationScriptObject = GetCSAreaInfoFileCreationScriptObject();
                                mCSAreaInfoFileCreationScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: AreaInfoFileCreationScript.cs! FSET continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallCustomizedProcessesScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (EarthConfig.mUseCSharpScripts)
            {
                if (!mCSCustomizedProcessesScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "CustomizedProcessesScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmCustomizedProcessesScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mCSCustomizedProcessesScriptObject = GetCSCustomizedProcessesScriptObject();
                                mCSCustomizedProcessesScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {
                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: CustomizedProcessesScript.cs! FSET continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static List<String> ReadInScript(String iScriptFilenameAndPath)
        {
           StreamReader vStream;        
           String vString;      
           List<String> vList = new List<String>();

           vStream = new StreamReader(iScriptFilenameAndPath);

           if (vStream != null)
           {
               //Read first Line
               vString = vStream.ReadLine();
               while (vString != null)
               {
                   vList.Add(vString);
                   //Read next Line
                   vString = vStream.ReadLine();
               }
               vStream.Close();
           }
           return vList;
        }


        static AsmHelper GetCSScriptAsmHelper(List<String> iScript, String iCSScriptLibraryDLLDirectory)
        {
            String vScriptInOneLine = "";

            foreach (String vString in iScript)
            {
                vScriptInOneLine += vString + Environment.NewLine;
            }

            CSScript.GlobalSettings.SearchDirs += ";" + iCSScriptLibraryDLLDirectory;
            CSScript.AssemblyResolvingEnabled = true;

            Assembly vAssembly = CSScript.LoadCode(vScriptInOneLine, null, false); //false = create no debug info (.pdb) file. Not needed and spares us to program a more inteleigent clean up/file delete.
            mTempFilesToDelete.Add(vAssembly.Location);
            AsmHelper helper = new AsmHelper(vAssembly);
           
            //old: AsmHelper helper = new AsmHelper(CSScript.LoadCode(vScriptInOneLine, null, true));

            return helper;
        }

        static Object GetCSTileCodeingScriptObject()
        {
            Object vTileCodeingScriptObject = mAsmTileCodeingScriptHelper.CreateObject("FSEarthTilesDLL.TileCodeingScript");
            return vTileCodeingScriptObject;
        }

        static Object GetCSAreaInfoFileCreationScriptObject()
        {
            Object vAreaInfoFileCreationScriptObject = mAsmAreaInfoFileCreationScriptHelper.CreateObject("FSEarthTilesDLL.AreaInfoFileCreationScript");
            return vAreaInfoFileCreationScriptObject;
        }


        static Object GetCSCustomizedProcessesScriptObject()
        {
            Object vCustomizedProcessesScriptObject = mAsmCustomizedProcessesScriptHelper.CreateObject("FSEarthTilesDLL.CustomizedProcessesScript");
            return vCustomizedProcessesScriptObject;
        }

        //Used from MainForm,Web,Engines
        static public String MapAreaCoordToTileCode(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iAreaCodeLevel, String iUseCode)
        {
            lock (mMethodeCallLock)
            {
                String vTileCode = "";
                try
                {
                    if (mCSTileCodeingScriptLoaded)
                    {
                        vTileCode = (String)(mAsmTileCodeingScriptHelper.InvokeInst(mCSTileCodeingScriptObject, "MapAreaCoordToTileCode", iAreaCodeX, iAreaCodeY, iAreaCodeLevel, iUseCode));
                    }
                    else
                    {
                        vTileCode = mTileCodeingScript.MapAreaCoordToTileCode(iAreaCodeX, iAreaCodeY, iAreaCodeLevel, iUseCode);
                    }

                }
                catch (System.Exception e)
                {
                    //Call went wrong
                    String vError = e.ToString();
                    MessageBox.Show(vError, "Script Error in TileCodeingScript.cs,  Method MapAreaCoordToTileCode ");
                    Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
                }

                return vTileCode;
            }
        }

        //Used from MainForm
        static public void SaveAreaInfo(EarthArea iEarthArea, String iAreaFileNameMiddlePart)
        {
            try
            {
                if (mCSAreaInfoFileCreationScriptLoaded)
                {
                    mAsmAreaInfoFileCreationScriptHelper.InvokeInst(mCSAreaInfoFileCreationScriptObject, "SaveAreaInfo", iEarthArea, iAreaFileNameMiddlePart);
                }
                else
                {
                    mAreaInfoFileCreationScript.SaveAreaInfo(iEarthArea, iAreaFileNameMiddlePart);
                }

            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in AreaInfoFileCreationScript.cs,  Method SaveAreaInfo ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoOnStartButtonEvent(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoOnStartButtonEvent", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoOnStartButtonEvent(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoOnStartButtonEvent ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoOnAbortButtonEvent(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoOnAbortButtonEvent", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoOnAbortButtonEvent(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoOnAbortButtonEvent ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoWhenEverthingIsDone(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoWhenEverthingIsDone", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoWhenEverthingIsDone(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoWhenEverthingIsDone ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoOnFSEarthTilesClose(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoOnFSEarthTilesClose", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoOnFSEarthTilesClose(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoOnFSEarthTilesClose ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoBeforeDownload(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoBeforeDownload", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoBeforeDownload(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoBeforeDownload ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoBeforeResampleing(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoBeforeResampleing", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoBeforeResampleing(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoBeforeResampleing ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoAfterDownload(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoAfterDownload", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoAfterDownload(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoAfterDownload ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoBeforeFSEarthMasks(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoBeforeFSEarthMasks", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoBeforeFSEarthMasks(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoBeforeFSEarthMasks ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoAfterFSEarthMasks(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoAfterFSEarthMasks", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoAfterFSEarthMasks(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoAfterFSEarthMasks ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoBeforeFSCompilation(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoBeforeFSCompilation", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoBeforeFSCompilation(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoBeforeFSCompilation ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void DoAfterFSCompilation(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            try
            {
                if (mCSCustomizedProcessesScriptLoaded)
                {
                    mAsmCustomizedProcessesScriptHelper.InvokeInst(mCSCustomizedProcessesScriptObject, "DoAfterFSCompilation", iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
                else
                {
                    mCustomizedProcessesScript.DoAfterFSCompilation(iEarthArea, iAreaFileNameMiddlePart, iEarthMultiArea, iAreaInfo, iAreaNr, iDownloadedTilesTotal, iMultiAreaMode);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in CustomizedProcessesScript.cs,  Method DoAfterFSCompilation ");
                Thread.Sleep(2000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        //---------Additional not Scripted Stuff--------:
        //Creates a Web Address for a Tile used from Main and Web (Possible to do in Script) used for WebBrowser
        public static String CreateWebAddress(Int64 iAreaCodeX, Int64 iAreaCodeY, Int64 iLevel, Int32 iService)
        {

            String vTileCode = "";
            String vFullTileAddress = "";
            String vServiceUserAgent = "";
            String vServiceStringBegin;
            String vServiceStringEnd;

            if (EarthConfig.layServiceMode)
            {
                LayProvider lp = EarthConfig.layProviders[EarthConfig.layServiceSelected];
                int variationIdx = lp.GetRandomVariationIdx();
                vFullTileAddress = lp.getURL(variationIdx, iAreaCodeX, iAreaCodeY, EarthMath.cLevel0CodeDeep - iLevel);
            }
            else
            {
                Random vServerVariationRandom = new Random();
                Double vRandomNumber = vServerVariationRandom.NextDouble();
                Int32 vServerVariantionSelection = 0;

                if (vRandomNumber > 0.25)
                {
                    vServerVariantionSelection = 1;
                }
                if (vRandomNumber > 0.50)
                {
                    vServerVariantionSelection = 2;
                }
                if (vRandomNumber > 0.75)
                {
                    vServerVariantionSelection = 3;
                }

                vTileCode = MapAreaCoordToTileCode(iAreaCodeX, iAreaCodeY, iLevel, EarthConfig.mServiceCodeing[iService - 1]);
                switch (vServerVariantionSelection)
                {
                    case 0: vServiceStringBegin = EarthConfig.mServiceUrlBegin0[iService - 1]; break;
                    case 1: vServiceStringBegin = EarthConfig.mServiceUrlBegin1[iService - 1]; break;
                    case 2: vServiceStringBegin = EarthConfig.mServiceUrlBegin2[iService - 1]; break;
                    case 3: vServiceStringBegin = EarthConfig.mServiceUrlBegin3[iService - 1]; break;
                    default: vServiceStringBegin = EarthConfig.mServiceUrlBegin0[iService - 1]; break;
                }
                vServiceStringEnd = EarthConfig.mServiceUrlEnd[iService - 1];
                vServiceUserAgent = EarthConfig.mServiceCodeing[iService - 1];
                vFullTileAddress = vServiceStringBegin + vTileCode + vServiceStringEnd;
            }

            return vFullTileAddress;
        }

        //No where used (Possible to do in Script)
        public static String MapCoordToTileCode(Double iLongitude, Double iLatitude, Int64 iLevel, String iUseCode)
        {
            Int64 vAreaCodeX = EarthMath.GetAreaCodeX(iLongitude, iLevel);
            Int64 vAreaCodeY = EarthMath.GetAreaCodeY(iLatitude, iLevel);
            String vResultCode = MapAreaCoordToTileCode(vAreaCodeX, vAreaCodeY, iLevel, iUseCode);

            return vResultCode;
        }

    }
}
