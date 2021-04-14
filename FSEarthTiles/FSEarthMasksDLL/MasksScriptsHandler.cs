using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using FSEarthMasksInternalDLL;
using CSScriptLibrary; //Third party library
using System.Reflection; //for Assembly class

namespace FSEarthMasksDLL
{
    class MasksScriptsHandler
    {
        static Boolean           mCSSpringScriptLoaded = false;
        static Boolean           mCSSummerScriptLoaded = false;
        static Boolean           mCSAutumnScriptLoaded = false;
        static Boolean           mCSWinterScriptLoaded = false;
        static Boolean           mCSHardWinterScriptLoaded = false;
        static Boolean           mCSNightScriptLoaded      = false;

        static SpringScript      mSpringScript     = new SpringScript();
        static SummerScript      mSummerScript     = new SummerScript();
        static AutumnScript      mAutumnScript     = new AutumnScript();
        static WinterScript      mWinterScript     = new WinterScript();
        static HardWinterScript  mHardWinterScript = new HardWinterScript();
        static NightScript       mNightScript      = new NightScript();

        static AsmHelper mAsmSpringScriptHelper;
        static AsmHelper mAsmSummerScriptHelper;
        static AsmHelper mAsmAutumnScriptHelper;
        static AsmHelper mAsmWinterScriptHelper;
        static AsmHelper mAsmHardWinterScriptHelper;
        static AsmHelper mAsmNightScriptHelper;

        static Object    mSpringScriptObject;
        static Object    mSummerScriptObject;
        static Object    mAutumnScriptObject;
        static Object    mWinterScriptObject;
        static Object    mHardWinterScriptObject;
        static Object    mNightScriptObject;

        static List<String> mTempFilesToDelete = new List<String>();

        public MasksScriptsHandler()
        {
        }
        static public void CleanUp()
        {
            try
            {
                FreeObjects();
 
                MasksCommon.CollectGarbage();

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

                    proc.StartInfo.FileName = MasksConfig.mStartExeFolder + "\\" + "FSETScriptsTempFilesCleanUp.exe";
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

            mSpringScriptObject = null;
            mSummerScriptObject = null;
            mAutumnScriptObject = null;
            mWinterScriptObject = null;
            mHardWinterScriptObject = null;
            mNightScriptObject = null;

            if (mAsmSpringScriptHelper != null)
            {
                mAsmSpringScriptHelper.Dispose();
                mAsmSpringScriptHelper = null;
            }
            if (mAsmSummerScriptHelper != null)
            {
                mAsmSummerScriptHelper.Dispose();
                mAsmSummerScriptHelper = null;
            }
            if (mAsmAutumnScriptHelper != null)
            {
                mAsmAutumnScriptHelper.Dispose();
                mAsmAutumnScriptHelper = null;
            }
            if (mAsmWinterScriptHelper != null)
            {
                mAsmWinterScriptHelper.Dispose();
                mAsmWinterScriptHelper = null;
            }
            if (mAsmHardWinterScriptHelper != null)
            {
                mAsmHardWinterScriptHelper.Dispose();
                mAsmHardWinterScriptHelper = null;
            }
            if (mAsmNightScriptHelper != null)
            {
                mAsmNightScriptHelper.Dispose();
                mAsmNightScriptHelper = null;
            }
        }

        //Will be called from FSEarthMasks
        static public void TryInstallSpringScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSSpringScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "SpringScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmSpringScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mSpringScriptObject = GetCSSpringScriptObject();
                                mCSSpringScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: SpringScript.cs! FSEM continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallSummerScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSSummerScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "SummerScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmSummerScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mSummerScriptObject = GetCSSummerScriptObject();
                                mCSSummerScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: SummerScript.cs! FSEM continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallAutumnScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSAutumnScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "AutumnScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmAutumnScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mAutumnScriptObject = GetCSAutumnScriptObject();
                                mCSAutumnScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: AutumnScript.cs! FSEM continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallWinterScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSWinterScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "WinterScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmWinterScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mWinterScriptObject = GetCSWinterScriptObject();
                                mCSWinterScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: WinterScript.cs! FSEM continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallHardWinterScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSHardWinterScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "HardWinterScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmHardWinterScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mHardWinterScriptObject = GetCSHardWinterScriptObject();
                                mCSHardWinterScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: HardWinterScript.cs! FSEM continues with internal default routines.");
                        }
                    }
                }
            }
        }

        static public void TryInstallNightScript(String iScriptPath, String iCSScriptLibraryDLLDirectory)
        {
            if (MasksConfig.mUseCSharpScripts)
            {
                if (!mCSNightScriptLoaded)
                {
                    String vScriptFileNameAndPath = iScriptPath + "\\" + "NightScript.cs";

                    if (File.Exists(vScriptFileNameAndPath))
                    {
                        try
                        {
                            List<String> vScript = ReadInScript(vScriptFileNameAndPath);

                            if (vScript.Count > 0)
                            {
                                mAsmNightScriptHelper = GetCSScriptAsmHelper(vScript, iCSScriptLibraryDLLDirectory);
                                mNightScriptObject = GetCSNightScriptObject();
                                mCSNightScriptLoaded = true;
                            }
                        }
                        catch (System.Exception e)
                        {

                            //LoadingWentWrong
                            String vError = e.ToString();
                            MessageBox.Show(vError, "Warning! Failed To Install C#Script: NightScript.cs! FSEM continues with internal default routines.");
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

            return helper;
        }

        static Object GetCSSpringScriptObject()
        {
            Object vSpringScriptObject = mAsmSpringScriptHelper.CreateObject("FSEarthMasksDLL.SpringScript");
            return vSpringScriptObject;
        }

        static Object GetCSSummerScriptObject()
        {
            Object vSummerScriptObject = mAsmSummerScriptHelper.CreateObject("FSEarthMasksDLL.SummerScript");
            return vSummerScriptObject;
        }

        static Object GetCSAutumnScriptObject()
        {
            Object vAutumnScriptObject = mAsmAutumnScriptHelper.CreateObject("FSEarthMasksDLL.AutumnScript");
            return vAutumnScriptObject;
        }

        static Object GetCSWinterScriptObject()
        {
            Object vWinterScriptObject = mAsmWinterScriptHelper.CreateObject("FSEarthMasksDLL.WinterScript");
            return vWinterScriptObject;
        }

        static Object GetCSHardWinterScriptObject()
        {
            Object vHardWinterScriptObject = mAsmHardWinterScriptHelper.CreateObject("FSEarthMasksDLL.HardWinterScript");
            return vHardWinterScriptObject;
        }

        static Object GetCSNightScriptObject()
        {
            Object vNightScriptObject = mAsmNightScriptHelper.CreateObject("FSEarthMasksDLL.NightScript");
            return vNightScriptObject;
        }

        static public void MakeSpring(MasksTexture iTexture)
        {
            try
            {
                if (mCSSpringScriptLoaded)
                {
                    mAsmSpringScriptHelper.InvokeInst(mSpringScriptObject, "MakeSpring", iTexture);
                }
                else
                {
                    mSpringScript.MakeSpring(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in SpringScript.cs,  Method MakeSpring ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void MakeSummer(MasksTexture iTexture)
        {
            try
            {
                if (mCSSummerScriptLoaded)
                {
                    mAsmSummerScriptHelper.InvokeInst(mSummerScriptObject, "MakeSummer", iTexture);
                }
                else
                {
                    mSummerScript.MakeSummer(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in SummerScript.cs,  Method MakeSummer ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void MakeAutumn(MasksTexture iTexture)
        {
            try
            {
                if (mCSAutumnScriptLoaded)
                {
                    mAsmAutumnScriptHelper.InvokeInst(mAutumnScriptObject, "MakeAutumn", iTexture);
                }
                else
                {
                    mAutumnScript.MakeAutumn(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in AutumnScript.cs,  Method MakeAutumn ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void MakeWinter(MasksTexture iTexture)
        {
            try
            {
                if (mCSWinterScriptLoaded)
                {
                    mAsmWinterScriptHelper.InvokeInst(mWinterScriptObject, "MakeWinter", iTexture);
                }
                else
                {
                    mWinterScript.MakeWinter(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in WinterScript.cs,  Method MakeWinter ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void MakeHardWinter(MasksTexture iTexture)
        {
            try
            {
                if (mCSHardWinterScriptLoaded)
                {
                    mAsmHardWinterScriptHelper.InvokeInst(mHardWinterScriptObject, "MakeHardWinter", iTexture);
                }
                else
                {
                    mHardWinterScript.MakeHardWinter(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in HardWinterScript.cs,  Method MakeHardWinter ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

        static public void MakeNight(MasksTexture iTexture)
        {
            try
            {
                if (mCSNightScriptLoaded)
                {
                    mAsmNightScriptHelper.InvokeInst(mNightScriptObject, "MakeNight", iTexture);
                }
                else
                {
                    mNightScript.MakeNight(iTexture);
                }
            }
            catch (System.Exception e)
            {
                //Call went wrong
                String vError = e.ToString();
                MessageBox.Show(vError, "Script Error in NightScript.cs,  Method MakeNight ");
                Thread.Sleep(3000); //Give User Enough Reaction Time to close the Appl after click away before a next Box Pops up
            }
        }

    }
}
