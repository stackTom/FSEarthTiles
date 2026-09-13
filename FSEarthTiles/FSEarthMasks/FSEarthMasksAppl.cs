using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FSEarthMasksDLL;
using FSEarthTilesInternalDLL;

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
            // Force '.' as the decimal separator regardless of the Windows locale (see CultureUtils)
            CultureUtils.ForceInvariantCultureProcessWide();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FSEarthMasksForm(iApplicationStartArguments));
        }
    }
}