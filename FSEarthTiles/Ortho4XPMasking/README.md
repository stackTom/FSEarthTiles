# Generate mesh data using Ortho4XP API

This folder contains createMesh.py. This file can be compiled into an exe using pyinstaller as follows:

`pyinstaller --clean -F -p src createMesh.py`.

I have compiled it on a Windows 10 64-bit machine (build 19042) using python 3.6.4. The last supported python for Windows XP was 3.4.3. So, XP user's can't run water masking unless an old pyinstaller and python is used to build createMesh.py

Note: The src/ folder from Ortho4XP_FSX_P3D (https://github.com/stackTom/Ortho4XP_FSX_P3D) must be present in the same folder as createMesh.py (or, just download Ortho4XP_FSX_P3D, place createMesh.py into the folder containing src/, and use pyinstaller as above).
In order to eliminate the message `"ERROR: Providers/O4_Custom_URL.py contains invalid code. The corresponding providers won't probably work."`, comment out this line
`print("ERROR: Providers/O4_Custom_URL.py contains invalid code. The corresponding providers won't probably work.")`
in `O4_Imagery_Utils.py` before compiling with pyinstaller.
