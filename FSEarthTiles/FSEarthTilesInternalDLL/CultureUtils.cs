using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace FSEarthTilesInternalDLL
{
    // FSET parses and formats numbers all over the place (ini files, coordinates, scenproc scripts, Overpass
    // bounding boxes, ...) assuming '.' as the decimal separator. On a comma-decimal Windows locale
    // (fr-FR, es-AR, de-DE, ...) that produced "26,5" in URLs and FormatException / "Value was either too large
    // or too small for a Decimal" when parsing. See https://github.com/stackTom/FSEarthTiles/issues/9
    //
    // We still target .NET 4.0 to keep supporting Windows XP, so CultureInfo.DefaultThreadCurrentCulture (4.5+)
    // is not available at compile time. Instead every thread entry point in FSET calls ForceInvariantCulture()
    // itself, and the exe entry points additionally set DefaultThreadCurrentCulture via reflection when the
    // runtime happens to be 4.5 or newer.
    public static class CultureUtils
    {
        // Call at the top of every thread entry point (ThreadStart targets, lambdas passed to new Thread(...)).
        public static void ForceInvariantCulture()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        // Call once from Main() before any other thread is created.
        public static void ForceInvariantCultureProcessWide()
        {
            ForceInvariantCulture();
            try
            {
                PropertyInfo vDefaultThreadCurrentCulture = typeof(CultureInfo).GetProperty("DefaultThreadCurrentCulture", BindingFlags.Public | BindingFlags.Static);
                if (vDefaultThreadCurrentCulture != null)
                {
                    vDefaultThreadCurrentCulture.SetValue(null, CultureInfo.InvariantCulture, null);
                }
            }
            catch
            {
                // .NET 4.0 runtime (Windows XP): not available, the per-thread calls cover us.
            }
        }
    }
}