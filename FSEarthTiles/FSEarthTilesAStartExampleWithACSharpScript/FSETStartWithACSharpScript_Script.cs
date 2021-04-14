using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;

using FSEarthTilesDLL;
//css_ref FSEarthTilesInternalDLL.dll; // CSscript directives

class Script
{

        const Boolean vStartModal = true;

        static FSEarthTilesForm mFSEarthTiles;

	[STAThread]
	static public void Main(string[] args)
	{

            String vFSEarthTilesApplicationFolder = null;
            String vThisApplicationsFolder = Application.StartupPath;
            
            if (!File.Exists(vThisApplicationsFolder + "\\" + "FSEarthTiles.ini"))
            {
                //if no FSEarthTiles.ini file exists we asume FSET is not installed in the same directory as your applictaion
                //therefore we explicit have to set the FSET's folder where all this stuff is at home and pass it to FSET DLL
                vFSEarthTilesApplicationFolder = "D:\\CSharp\\FSEarthTiles\\FSEarthTiles\\bin\\Debug";
            }

            if (vStartModal)
            {
		StartFSEarthTilesModal(vFSEarthTilesApplicationFolder);
            }
            else
	    {
		StartFSEarthTiles(vFSEarthTilesApplicationFolder);
	        for (Int32 vCon=1; vCon<=100; vCon++)
		{	
                  Application.DoEvents();
		  Thread.Sleep(50);
		}
		StopFSEarthTiles();
            }
	}


        static private void StartFSEarthTilesModal(String iFSEarthTilesApplicationFolder)
        {

            if (mFSEarthTiles == null)
            {
                mFSEarthTiles = new FSEarthTilesForm(null, null, iFSEarthTilesApplicationFolder);
		mFSEarthTiles.ShowDialog();
            }
            else
            {
		if (!mFSEarthTiles.Created)
		{ 
                  mFSEarthTiles.Dispose();
                  mFSEarthTiles = new FSEarthTilesForm(null, null, iFSEarthTilesApplicationFolder);
		  mFSEarthTiles.ShowDialog();
		}
            }
        }


        static private void StartFSEarthTiles(String iFSEarthTilesApplicationFolder)
        {

            if (mFSEarthTiles == null)
            {
                mFSEarthTiles = new FSEarthTilesForm(null, null, iFSEarthTilesApplicationFolder);
                mFSEarthTiles.Show();
            }
            else
            {
		if (!mFSEarthTiles.Created)
		{ 
                  mFSEarthTiles.Dispose();
                  mFSEarthTiles = new FSEarthTilesForm(null, null, iFSEarthTilesApplicationFolder);
                  mFSEarthTiles.Show();
		}
            }
        }

        static private void StopFSEarthTiles()
        {
            if (mFSEarthTiles != null)
            {
                mFSEarthTiles.Close();
                mFSEarthTiles.Dispose();
                mFSEarthTiles = null;
            }
        }
}

