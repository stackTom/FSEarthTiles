using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FSEarthTilesDLL;

namespace FSEarthTiles
{
    static class FSEarthTilesAppl
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(String[] iApplicationStartArguments)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FSEarthTilesForm      vFSEarthTilesForm      = new FSEarthTilesForm(iApplicationStartArguments, null, null);
            FSEarthTilesInterface vFSEarthTilesInterface = vFSEarthTilesForm;

            Application.Run(vFSEarthTilesForm);
        }
    }
}