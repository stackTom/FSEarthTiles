# Generate mesh data using Ortho4XP API

This folder contains createMesh.py. This file can be compiled into an exe using pyinstaller as follows:

1) First, the src/ folder from Ortho4XP_FSX_P3D (https://github.com/stackTom/Ortho4XP_FSX_P3D) must be present in the same folder as createMesh.py (or, just download Ortho4XP_FSX_P3D, place createMesh.py into the folder containing src/, and use pyinstaller as above).
In order to eliminate the message `"ERROR: Providers/O4_Custom_URL.py contains invalid code. The corresponding providers won't probably work."`, comment out this line
`print("ERROR: Providers/O4_Custom_URL.py contains invalid code. The corresponding providers won't probably work.")`
in `O4_Imagery_Utils.py` before compiling with pyinstaller.

2) Then, run:
`pyinstaller --clean -F --runtime-tmpdir "./Pyinstaller_temp/" -p src createMesh.py`.

3) Then, copy spatialindex-64.dll and spatialindex_c-64.dll (from rtree python module) into the folder where the new executable is:
`cp /c/Users/fery2/AppData/Local/Programs/Python/Python36/Lib/site-packages/rtree/lib/spatialindex* .`

4) If the executable crashes with errors like `OSError: could not find or load spatialindex_c-64.dll`, then follow these instructions: https://stackoverflow.com/questions/64398516/pyinstaller-exe-oserror-could-not-find-or-load-spatialindex-c-64-dll
(Basically, find the `createMesh.spec` file, which should be in the same directory as createMesh.py. Add this import to it: `from PyInstaller.utils.hooks import collect_dynamic_libs`. Then, change the line that says `binaries=[]` to `binaries=collect_dynamic_libs("rtree")`. A sample `createMesh.spec` file is provided for reference, but it is recommended to use the one produced by pyinstaller and edit it with the lines just mentioned. After doing this, run `pyinstaller createMesh.spec`.

Note: I have compiled it on a Windows 10 64-bit machine (build 19043) using python 3.6.4. The last supported python for Windows XP was 3.4.3. So, XP user's can't run water masking unless an old pyinstaller and python is used to build createMesh.py
