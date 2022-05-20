# -*- mode: python ; coding: utf-8 -*-


from PyInstaller.utils.hooks import collect_dynamic_libs
block_cipher = None


a = Analysis(['createMesh.py'],
             pathex=['src', 'G:\\FSEarthTiles\\FSEarthTiles\\Ortho4XPMasking'],
             binaries=collect_dynamic_libs("rtree"),
             datas=[],
             hiddenimports=[],
             hookspath=[],
             runtime_hooks=[],
             excludes=[],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher,
             noarchive=False)
pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          a.binaries,
          a.zipfiles,
          a.datas,
          [],
          name='createMesh',
          debug=False,
          bootloader_ignore_signals=False,
          strip=False,
          upx=True,
          upx_exclude=[],
          runtime_tmpdir='./Pyinstaller_temp/',
          console=True )
