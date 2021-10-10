:: NOTE: user needs to manually copy over fs9, fsx, p3d resample.exe and imagetool.exe for fs9

:: rebuild solution in release mode
MSBuild.exe FSEarthTiles.sln /p:Configuration=Release /p:Platform="Any CPU" /t:rebuild

:: copy base files
xcopy /I .\FSEarthTiles\bin\Release\FSET .\FSET

:: copy over documentation
xcopy /I .\Docs\ .\FSET\Docs

:: copy over new FSET binaries
copy /Y .\FSEarthTiles\bin\Release\CSScriptLibrary.dll .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTiles.exe .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTiles.exe.config .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTiles.pdb .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTilesDLL.dll .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTilesDLL.pdb .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTilesInternalDLL.dll .\FSET
copy /Y .\FSEarthTiles\bin\Release\FSEarthTilesInternalDLL.pdb .\FSET

:: copy over new FSEarthMasks binaries
copy /Y .\FSEarthMasks\bin\Release\CSScriptLibrary.dll .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasks.exe .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasks.exe.config .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasks.pdb .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksDLL.dll .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksDLL.pdb .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.dll .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.pdb .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.dll .\FSET
copy /Y .\FSEarthMasks\bin\Release\FSEarthMasksInternalDLL.pdb .\FSET

:: copy over ALGLib.dll
copy /Y .\ALGLib\bin\Release\ALGLib.dll .\FSET

:: copy over createMesh.exe and it's needed files
xcopy /I .\Ortho4XPMasking\Utils .\FSET\Utils
copy /Y .\Ortho4XPMasking\createMesh.exe .\FSET
copy /Y .\Ortho4XPMasking\spatialindex_c-64.dll .\FSET
copy /Y .\Ortho4XPMasking\spatialindex-64.dll .\FSET

:: copy over the Scenproc Scripts
xcopy /I .\Scenproc_scripts .\FSET\Scenproc_scripts

:: copy over the ini's
copy /Y .\Ini\FSEarthMasks.ini .\FSET
copy /Y .\Ini\FSEarthTiles.ini .\FSET

:: copy over the providers
xcopy /I /s .\Providers .\FSET\Providers

:: copy over some needed scripts (more to be added in the future if the default scripts are updated)
copy /Y .\FSEarthTilesDLL\AreaInfoFileCreationScript.cs .\FSET
copy /Y .\FSEarthTilesDLL\TileCodeingScript.cs .\FSET

:: create zip file
powershell Compress-Archive .\FSET\* FSET.zip

:: remove .\FSET temp folder, since we only care about the .zip for release
rmdir /s /q .\FSET

:: Now, user needs to manually copy over fs9, fsx, p3d resample.exe and imagetool.exe for fs9
