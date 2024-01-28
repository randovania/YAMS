import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Callable
from contextlib import contextmanager

# TODO: revise this to be cleaner. Following things to keep in mind here
# 1. AFAIK the DLLs must be in path before loading the CLR. test whether that's truly the case.
#    Maybe we can remove from path when we're done?
# 2. While we figured out how to deal with non system installations via the DOTNET_ROOT env var,
#    should we also provide a way to just pass a `dotnet_root` param to the wrapper which gets
#    passed to pythonnet?
yams_path = os.fspath(Path(__file__).with_name(name="yams"))
sys.path.append(yams_path)
from pythonnet import load, unload


class YamsException(Exception):
    pass


# TODO: add docstrings for methods when not in alpha and has stableish API
class Wrapper:
    def __init__(self, lib):
        self.csharp_patcher = lib

    def get_csharp_version(self) -> str:
        return self.csharp_patcher.Version

    # TODO: ADD TESTS!!!
    def patch_game(
        self,
        input_path: Path,
        output_path: Path,
        patch_data: dict,
        progress_update: Callable[[str, float], None],
    ):
        # Copy to input dir to temp dir first to do operations there
        progress_update("Copying to temporary path...", 0)
        tempdir = tempfile.TemporaryDirectory()
        shutil.copytree(input_path, tempdir.name, dirs_exist_ok=True)

        # Get data.win path. Both of these *need* to be strings, as otherwise patcher won't accept them.
        output_data_win: str = os.fspath(
            _prepare_environment_and_get_data_win_path(tempdir.name)
        )
        input_data_win: str = shutil.move(output_data_win, output_data_win + "_orig")
        input_data_win_path = Path(input_data_win)

        # Temp write patch_data into json file for yams later
        progress_update("Creating json file...", 0.3)
        json_file: str = os.fspath(
            input_data_win_path.parent.joinpath("yams-data.json")
        )
        with open(json_file, "w+") as f:
            f.write(json.dumps(patch_data, indent=2))

        # Wrapper to play rando easier on flatpak
        tempdir_Path = Path(tempdir.name)
        if platform.system() == "Linux" and not tempdir_Path.joinpath("AM2R.AppImage").exists():
            wrapper_file: str = os.fspath(tempdir_Path.joinpath("start-rando.sh"))
            with open(wrapper_file, "w+") as f:
                f.write("#!/usr/bin/env bash\n")
                f.write('script_dir="$(realpath "$(dirname "${BASH_SOURCE[0]}")")"\n')
                f.write('flatpak run --command="${script_dir}/runner" io.github.am2r_community_developers.AM2RLauncher\n')
            os.chmod(wrapper_file, 0o775)

        # AM2RLauncher installations usually have a profile.xml file. For less confusion, remove it if it exists
        profile_xml_path = Path(tempdir.name).joinpath("profile.xml")
        if profile_xml_path.exists():
            profile_xml_path.unlink()

        # Patch data.win
        progress_update("Patching data file...", 0.6)
        self.csharp_patcher.Main(input_data_win, output_data_win, json_file)

        # Move temp dir to output dir and get rid of it. Also delete original data.win
        # Also delete the json if we're on a race seed.
        if not patch_data.get("configuration_identifier", {}).get("contains_spoiler", False):
            input_data_win_path.parent.joinpath("yams-data.json").unlink()
        input_data_win_path.unlink()
        progress_update("Moving to output directory...", 0.8)
        shutil.copytree(tempdir.name, output_path, dirs_exist_ok=True)
        shutil.rmtree(tempdir.name)

        progress_update("Exporting finished!", 1)


def _load_cs_environment():
    # Load dotnet runtime
    load("coreclr")
    import clr

    clr.AddReference("YAMS-LIB")

@contextmanager
def load_wrapper() -> Wrapper:
    try:
        _load_cs_environment()
        from YAMS_LIB import Patcher as CSharp_Patcher
        yield Wrapper(CSharp_Patcher)
    except Exception as e:
        raise e


def _prepare_environment_and_get_data_win_path(folder: str) -> Path:
    current_platform = platform.system()
    folderPath = Path(folder)
    if current_platform == "Windows":
        return folderPath.joinpath("data.win")

    elif current_platform == "Linux":
        # Linux can have the game packed in an AppImage. If it exists, extract it first
        # Also extraction for some reason only does it into CWD with no way to change it, so we specify it.
        appimage = folderPath.joinpath("AM2R.AppImage")
        if appimage.exists():
            subprocess.run([appimage, "--appimage-extract"], cwd=folder)
            appimage.unlink()
            # shutil doesn't support moving a directory like this, so I copy + delete
            squashfsPath = folderPath.joinpath("squashfs-root")
            shutil.copytree(squashfsPath, folder, dirs_exist_ok=True)
            shutil.rmtree(squashfsPath)
            return folderPath.joinpath("usr", "bin", "assets", "game.unx")
        else:
            return folderPath.joinpath("assets", "game.unx")

    elif current_platform == "Darwin":
        return folderPath.joinpath("AM2R.app", "Contents", "Resources", "game.ios")

    else:
        raise ValueError(f"Unknown system: {platform.system()}")
