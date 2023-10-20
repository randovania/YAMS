# Yet Another Metroid2 Shuffler (YAMS)

A patcher for providing a different randomization experience to AM2R different to what the game has built-in. It was primarily designed for [Randovania](https://github.com/randovania/randovania), but it's also usable as a standalone patcher.  
Usage:  
`./YAMS-CLI [path-to-original-data-file] [path-to-output-data-file] [path-to-json-file]`

The API/Schema for the input json file will soon be documented.

# Compilation
This project uses git submodules. So before compiling, please ensure you have cloned them (either by doing `git clone --recursive https://github.com/randovania/YAMS`, or if you have already cloned the repo, `git submodule update --init`).  
After that, you can use the standard dotnet compilation step: `dotnet build YAMS-CLI`.

# License
All code is licensed under the GNU Public License version 3. See the `LICENSE-CODE` file for full details.  
Art assets are licensed under [CC-BY-SA 4.0](https://creativecommons.org/licenses/by/4.0/). For the full list of authors and more details, please read the `Attribution.md` file located in [`YAMS-LIB/sprites/`](./YAMS-LIB/sprites/Attribution.md).
