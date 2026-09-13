using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using FSEarthMasksDLL;

namespace FSEarthMasks
{
    static class FSEarthMasksAppl
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(String[] iApplicationStartArguments)
        {
            // Force '.' as the decimal separator on every thread regardless of the Windows locale.
            // See FSEarthTilesAppl.Main and https://github.com/stackTom/FSEarthTiles/issues/9
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FSEarthMasksForm(iApplicationStartArguments));
        }
    }
}