import warnings
warnings.simplefilter(action="ignore", category=FutureWarning)
import os
import sys

# needed by exe to find all the files to "compile"
Ortho4XP_dir = "."
sys.path.append(os.path.join(Ortho4XP_dir, "src"))

# set up working folders
import O4_File_Names as FNAMES
import O4_UI_Utils as UI

FNAMES.Ortho4XP_dir = Ortho4XP_dir
UI.Ortho4XP_dir = Ortho4XP_dir # put log in Ortho4XP_dir, as UI uses a different value for Ortho4XP_dir
FNAMES.Utils_dir = os.path.join(FNAMES.Ortho4XP_dir, "Utils")
FNAMES.OSM_dir = os.path.join(sys.argv[3], "OSM_data")
FNAMES.Elevation_dir = os.path.join(sys.argv[3], "Elevation_data")
FNAMES.Tile_dir = os.path.join(sys.argv[3], "Tiles", sys.argv[4])

import O4_Vector_Map as VMAP
import O4_Config_Utils as CFG
import O4_Mesh_Utils as MESH

def main(argv):
    tile = CFG.Tile(int(argv[1]), int(argv[2]), FNAMES.Tile_dir)
    VMAP.build_poly_file(tile)
    MESH.build_mesh(tile)

if __name__ == "__main__":
    main(sys.argv)
