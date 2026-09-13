using System;
using System.Collections.Generic;
using System.Windows.Forms;
using FSEarthTilesInternalDLL;

namespace FSEarthTilesDLLTester
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Force '.' as the decimal separator regardless of the Windows locale (see CultureUtils)
            CultureUtils.ForceInvariantCultureProcessWide();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FSEarthTilesDLLTesterForm());
        }
    }
}