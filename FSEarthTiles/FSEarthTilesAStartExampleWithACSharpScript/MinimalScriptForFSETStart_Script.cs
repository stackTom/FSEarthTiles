using System;
using System.Windows.Forms;

using FSEarthTilesDLL;
//css_ref FSEarthTilesInternalDLL.dll; // CSscript directive

class Script
{
  
  static FSEarthTilesForm mFSEarthTiles;

  [STAThread]
  static public void Main(string[] args)
  {
     mFSEarthTiles = new FSEarthTilesForm(null, null, null);
     mFSEarthTiles.ShowDialog();
  }

}

