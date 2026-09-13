using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
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
            // FSET parses and formats numbers all over the place (ini files, coordinates, scenproc scripts, ...)
            // assuming '.' as the decimal separator. Force the invariant culture on this thread and on every
            // thread created from now on, so that a comma-decimal Windows locale (fr-FR, es-AR, de-DE, ...)
            // doesn't corrupt them. See https://github.com/stackTom/FSEarthTiles/issues/9
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            FSEarthTilesForm      vFSEarthTilesForm      = new FSEarthTilesForm(iApplicationStartArguments, null, null);
            FSEarthTilesInterface vFSEarthTilesInterface = vFSEarthTilesForm;

            Application.Run(vFSEarthTilesForm);
        }
    }
}