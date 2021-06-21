:: NOTE: user needs to manually copy over fs9, fsx, p3d resample.exe and imagetool.exe for fs9

:: copy base files
xcopy /I .\FSEarthTiles\bin\Release\FSET .\FSET

:: copy over new FSET binaries
copy .\FSEarthTiles\bin\Release\CSScriptLibrary.dll .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTiles.exe .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTiles.exe.config .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTiles.pdb .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTilesDLL.dll .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTilesDLL.pdb .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTilesInternalDLL.dll .\FSET
copy .\FSEarthTiles\bin\Release\FSEarthTilesInternalDLL.pdb .\FSET

:: copy over new FSEarthMasks binaries
copy .\FSEarthMasks\bin\Release\CSScriptLibrary.dll .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasks.exe .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasks.exe.config .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasks.pdb .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksDLL.dll .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksDLL.pdb .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.dll .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.pdb .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.dll .\FSET
copy .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.pdb .\FSET

:: copy over createMesh.exe and it's needed files
xcopy /I .\Ortho4XPMasking\Utils .\FSET\Utils
copy .\Ortho4XPMasking\createMesh.exe .\FSET
copy .\Ortho4XPMasking\spatialindex_c-64.dll .\FSET
copy .\Ortho4XPMasking\spatialindex-64.dll .\FSET

:: copy over the Scenproc Scripts
xcopy /I .\Scenproc_scripts .\FSET\Scenproc_scripts

:: copy over the ini's
copy .\Ini\FSEarthMasks.ini .\FSET
copy .\Ini\FSEarthTiles.ini .\FSET

:: copy over some needed scripts (more to be added in the future if the default scripts are updated)
copy .\FSEarthTilesDLL\AreaInfoFileCreationScript.cs .\FSET
copy .\FSEarthTilesDLL\TileCodeingScript.cs .\FSET

:: create zip file
powershell Compress-Archive .\FSET\* FSET.zip

:: Now, user needs to manually copy over fs9, fsx, p3d resample.exe and imagetool.exe for fs9
