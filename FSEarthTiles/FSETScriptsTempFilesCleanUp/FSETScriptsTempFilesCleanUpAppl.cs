using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace FSETScriptsTempFilesCleanUp
{
    static class FSETScriptsTempFilesCleanUpAppl
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(String[] iApplicationStartArguments)
        {
            if (iApplicationStartArguments.Length > 0)
            {
                Thread.Sleep(500); //Give the the calling application time to close to free the reservated files.
                for (Int32 vCon = 1; vCon <= 20; vCon++)
                {
                    Boolean vAllFilesDeleted = true;

                    foreach (String vFile in iApplicationStartArguments)
                    {
                        try
                        {
                            FileAttributes attr = File.GetAttributes(vFile);

                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            {
                                Directory.Delete(vFile, true);
                            }
                            else
                            {
                                File.Delete(vFile);
                            }
                        }
                        catch
                        {
                            vAllFilesDeleted = false;
                        }
                    }

                    if (vAllFilesDeleted)
                    {
                        break;
                    }
                    Thread.Sleep(500);
                }
            }
            else
            {
                MessageBox.Show("This Application is called by FSEarthTiles and FSEarthMasks as a invicible Helper to delete the temporary files (Assemblies) left from the C# Script Compiler leftover in your temporary user system folder.", "FSET Scripts Temporary Files Clean Up");
            }
        }
    }
}