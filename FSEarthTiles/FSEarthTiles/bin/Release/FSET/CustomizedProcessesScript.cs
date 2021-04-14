using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FSEarthTilesInternalDLL;

namespace FSEarthTilesDLL
{
    public class CustomizedProcessesScript
    {

        //The following Methodes will be called by FSEarthTiles:

        //DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady
        //DoOnStartButtonEvent
        //DoOnAbortButtonEvent
        //DoWhenEverthingIsDone
        //DoOnFSEarthTilesClose
        //DoBeforeDownload
        //DoBeforeResampleing
        //DoAfterDownload
        //DoBeforeFSEarthMasks
        //DoAfterFSEarthMasks
        //DoBeforeFSCompilation
        //DoAfterFSCompilation

        //Note: 
        //If you start applications from events that are called from FSET's Main Thread the FSET-GUI will frezze. Avoid doeing this.

        //The here passed input parameters are all copies or clones of internal object. There are Input Information only.
        public void DoOneTimeOnlyWhenFSEarthTilesIsFiredUpAndReady(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoOnStartButtonEvent(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoOnAbortButtonEvent(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoWhenEverthingIsDone(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoOnFSEarthTilesClose(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoBeforeDownload(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's Main Thread)
        }

        public void DoBeforeResampleing(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's After-Download-Processing Thread) 

            //Download is finish but the Texture is still not undistorted or resampled and not stored as file.
            //You have no access to texture datas at this timepoint
        }

        public void DoAfterDownload(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
           //(Is called from FSET's After-Download-Processing Thread)

           //From this event on the Texture is downloaded resampled and the Area Bitmap File created and the Memory freed
           //So this is the best timepoint to start additional Applictaions

           //JustAnExampleOfAnApplicationStart();                        //Starts Notepad with the Readme.txt

           //AnotherExampleOfAnApplicationStart(iAreaFileNameMiddlePart); //Starts MsPaint with our texture as parameter
                                                                         //Attention! MsPaint isn't doing well with large bitmaps (memory useage and crashes on modify and save)
        }

        public void DoBeforeFSEarthMasks(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's After-Download-Processing Thread)
        }

        public void DoAfterFSEarthMasks(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's After-Download-Processing Thread)
        }

        public void DoBeforeFSCompilation(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's After-Download-Processing Thread)
        }

        public void DoAfterFSCompilation(EarthArea iEarthArea, String iAreaFileNameMiddlePart, EarthMultiArea iEarthMultiArea, AreaInfo iAreaInfo, Int64 iAreaNr, Int64 iDownloadedTilesTotal, Boolean iMultiAreaMode)
        {
            //(Is called from FSET's After-Download-Processing Thread)
        }

        protected void JustAnExampleOfAnApplicationStart()
        {
            try
            {

                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName  = "notepad.exe";
                proc.StartInfo.Arguments = "\"" + EarthConfig.mStartExeFolder + "\\" + "Readme.txt" + "\"";

                proc.Start();
                proc.WaitForExit();
                if (!proc.HasExited)
                {
                    proc.Kill();
                }

            }
            catch (System.Exception e)
            {
               String vError = e.ToString();
               MessageBox.Show(vError, "Could not open Notepad! What to do now?");
               Thread.Sleep(2000); //give user some time after a MsgBox ok click to react so in case of endless loop errors
            }
        }

        protected void AnotherExampleOfAnApplicationStart(String iAreaFileNameMiddlePart)
        {
            try
            {

                System.Diagnostics.Process proc = new System.Diagnostics.Process();

                proc.StartInfo.FileName = "mspaint.exe";
                proc.StartInfo.Arguments = "\"" + EarthConfig.mWorkFolder + "\\" + "Area" + iAreaFileNameMiddlePart + ".bmp" + "\"";
                proc.Start();
                proc.WaitForExit();
                if (!proc.HasExited)
                {
                    proc.Kill();
                }

            }
            catch (System.Exception e)
            {
                String vError = e.ToString();
                MessageBox.Show(vError, "Could not open MsPaint! What to do now?");
                Thread.Sleep(2000); //give user some time after a MsgBox ok click to react so in case of endless loop errors
            }
        }

    }
}
