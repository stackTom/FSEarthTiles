FS Earth Tiles command line arguments (original defined by steffen)
=====================================

The following command line arguments are supported:

--lat dd mm ss h
--lon ddd mm ss h

    Center latitude and longitude. (d)dd are degrees, mm are minutes, ss are
    seconds, and h is the hemisphere (N/S or E/W). All parameters are
    mandatory and have to appear in the format shown. Other formats are not
    supported.

--north dd mm ss h
--west ddd mm ss h
--south dd mm ss h
--east ddd mm ss h

    Bounding box latitudes and longitudes. Format as above.


--width f
--height f

    The width and height of the area to be fetched. n is a floating point
    number.

--zoom n

    The zoom level to be fetched. n is a decimal number between 0 and 8.

--snap {Off|Tiles|LOD13|Pixel}

    Area snap mode. Specify one of the four possibilities.


--compile

    The scenery compiler will start automatically after fetching. Intended for
    use with the --fetch switch: If --compile is missing during a --fetch run,
    the area will be only fetched, and has to be compiled manually later.

--fetch

    Causes FS Earth Tiles to enter batch mode. The area indicated will be
    fetched (and compiled if requested to), and FS Earth Tiles will exit when
    finished, without any user interaction. Might be usefull for automatically
    fetching larger areas.

The above arguments are not case sensitive and may appear in any order, as
long as the format of the individual arguments appears as shown. Spaces
between argument names and parameters are mandatory.


Example:

FSEarthTiles.exe --lat 48 21 12 n --lon 11 47 12 E --width 3.8 --height 2.0 --zoom 1 --fetch

Will fetch and compile an area of 3.8 x 2.0 nm centered at 48° 21.2' N,
11° 47.2' E at a resolution of about 1 m/pixel. This is basically the airport
of Munich, Germany (EDDM). Once finished, FS Earth Tiles will quit.



Expansion for v0.9 and later (by HB-100)
---------------------------------------

--AutoStartDownload     (sets the Start Download Flag. FSET will start Download as soon as it is started up.) 
--AutoExitApplication   (sets the Application Exit Flag. FSET will auto exit after a Download.)

You also have this two parameters in the FSEarthTiles.ini file.

Also new:
You can pass an FSEarthTiles Config file like FSEarthConfig.ini or PartialFSEarthTiles.ini as single argument
You can pass an KML File like AreaKML.kml file as single argument. 

 (But note can not pass both or multiple files or other command line parameters at the same time, only one file)


note:  You can also Drag and Drop .ini and .kml files on FSET when running even multiple files although that makes not so much sense usually


