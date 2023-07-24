import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

# TODO: revise this.
yams_path = os.fspath(Path(__file__).with_name(name="yams"))
sys.path.append(yams_path)
from pythonnet import load
load("coreclr")
import clr
clr.AddReference("YAMS-LIB")
from YAMS_LIB import Patcher


def patch_game(input_path, output_path, patch_data):
    # Copy to input dir to temp dir first to do operations there
    progress_update("Copying to temporary path...", -1)
    tempdir = tempfile.TemporaryDirectory()
    shutil.copytree(input_path, tempdir.name, dirs_exist_ok=True)

    # Get data.win path. Both of these *need* to be strings, as otherwise patcher won't accept them.
    output_data_win_path: str = os.fspath(self._get_data_win_path(tempdir.name))
    input_data_win_path: str = shutil.move(output_data_win_path, output_data_win_path + "_orig")

    # Temp write patch_data into json file for yams later
    progress_update("Creating json file...", -1)
    json_file = tempfile.NamedTemporaryFile(mode='w+', delete=False)
    json_file.write(json.dumps(patch_data))
    json_file.close()

    # AM2RLauncher installations usually have a profile.xml file. For less confusion, remove it if it exists
    if Path.exists(Path(tempdir.name).joinpath("profile.xml")):
        Path.unlink(Path(tempdir.name).joinpath("profile.xml"))

    # TODO: this is where we'd do some customization options like music shuffler or samus palettes

    # Patch data.win
    progress_update("Patching data file...", -1)
    Patcher.Main(input_data_win_path, output_data_win_path, json_file.name)

    # Move temp dir to output dir and get rid of it. Also delete original data.win
    Path.unlink(Path(input_data_win_path))
    progress_update("Moving to output directory...", -1)
    shutil.copytree(tempdir.name, output_path, dirs_exist_ok=True)
    shutil.rmtree(tempdir.name)


def _get_data_win_path(self, folder: str) -> Path:
    current_platform = platform.system()
    if current_platform == "Windows":
        return Path(folder).joinpath("data.win")

    elif current_platform == "Linux":
        # Linux can have the game packed in an AppImage. If it exists, extract it first
        # Also extraction for some reason only does it into CWD, so we temporarily change it
        appimage = Path(folder).joinpath("AM2R.AppImage")
        if Path.exists(appimage):
            cwd = Path.cwd()
            os.chdir(folder)
            subprocess.run([appimage, "--appimage-extract"])
            os.chdir(cwd)
            Path.unlink(appimage)
            # shutil doesn't support moving a directory like this, so I copy + delete
            shutil.copytree(Path(folder).joinpath("squashfs-root"), folder, dirs_exist_ok=True)
            shutil.rmtree(Path(folder).joinpath("squashfs-root"))
            return Path(folder).joinpath("usr", "bin","assets", "game.unx")
        else:
            return Path(folder).joinpath("assets","game.unx")

    elif current_platform == "Darwin":
        return Path(folder).joinpath("AM2R.app","Contents","Resources","game.ios")

    else:
        raise ValueError(f"Unknown system: {platform.system()}")
