using System;
using System.Collections.Generic;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FSEarthMasksForm(iApplicationStartArguments));
        }
    }
}