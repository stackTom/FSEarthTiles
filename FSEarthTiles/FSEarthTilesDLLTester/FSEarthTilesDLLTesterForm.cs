using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.IO;
using System.Windows.Forms;

using FSEarthTilesDLL;
//css_ref FSEarthTilesInternalDLL.dll; // CSscript directives. Only this way this application can be run as C# Script also. Funny that the GUI with C# Script looks different.

namespace FSEarthTilesDLLTester
{
    public partial class FSEarthTilesDLLTesterForm : Form
    {
        FSEarthTilesForm      mFSEarthTiles;
        FSEarthTilesInterface mFSEarthTilesInterface;


        public FSEarthTilesDLLTesterForm()
        {
            InitializeComponent();
        }

        private void StartFSEarthTiles_Click(object sender, EventArgs e)
        {
            String vFSEarthTilesApplicationFolder = null;
            String vThisApplicationsFolder = Application.StartupPath;
            
            if (!File.Exists(vThisApplicationsFolder + "\\" + "FSEarthTiles.ini"))
            {
                //if no FSEarthTiles.ini file exists we asume FSET is not installed in the same directory as your applictaion
                //therefore we explicit have to set the FSET's folder where all this stuff is at home and pass it to FSET DLL
                vFSEarthTilesApplicationFolder = "D:\\CSharp\\FSEarthTiles\\FSEarthTiles\\bin\\Debug";
            }


            if (mFSEarthTiles == null)
            {
                mFSEarthTiles = new FSEarthTilesForm(null, null, vFSEarthTilesApplicationFolder); //If you pass null as FSET's-Exe folder then FSET with all it's stuff and your appl have to be in the same directory.
                mFSEarthTilesInterface = mFSEarthTiles;
                mFSEarthTiles.Show();
            }
            else
            {
                if (!mFSEarthTiles.Created)
                {
                    mFSEarthTiles.Dispose();
                    mFSEarthTiles = null;
                    mFSEarthTilesInterface = null;

                    mFSEarthTiles = new FSEarthTilesForm(null, null, vFSEarthTilesApplicationFolder);
                    mFSEarthTilesInterface = mFSEarthTiles;
                    mFSEarthTiles.Show();
                }
            }
        }

        private void StartDownLoadButton_Click(object sender, EventArgs e)
        {
            if (mFSEarthTiles != null)
            {
                List<String> vList = new List<String>();
                vList.Add("[FSEarthTiles]");
                vList.Add("");
                vList.Add("DownloadResolution       = 3");

                mFSEarthTilesInterface.ProcessHotPlugInConfigList(vList);

                mFSEarthTilesInterface.SetArea(44.6, 8.7, 44.5, 8.8); //iNWLatitude, iNWLongitude, iSELatitude, iSELongitude

                mFSEarthTilesInterface.Start();
            }

        }

        private void AbortDownloadButton_Click(object sender, EventArgs e)
        {
            if (mFSEarthTiles != null)
            {
                mFSEarthTilesInterface.Abort();
            }
        }

        private void StopFSEarthTilesButton_Click(object sender, EventArgs e)
        {
            if (mFSEarthTiles != null)
            {
                mFSEarthTiles.Close();
                mFSEarthTiles.Dispose();
                mFSEarthTiles = null;
                mFSEarthTilesInterface = null;
            }
        }

        private void FSEarthTilesDLLTesterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopFSEarthTilesButton_Click(null, null);
        }
    }
}