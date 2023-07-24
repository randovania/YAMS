import re
from pathlib import Path

def test_correct_versions():
    cs_version_regex = re.compile(r"<Version>(.*)<\/Version>")
    python_version_regex = re.compile(r"version = (.*)")
    cs_file = ""
    python_file = ""

    root_path = Path(__file__).parent.parent

    with open(root_path.joinpath("YAMS-LIB", "YAMS-LIB.csproj")) as f:
        cs_file = f.read()

    with open(root_path.joinpath("setup.cfg")) as f:
        python_file = f.read()

    cs_match = cs_version_regex.search(cs_file)
    python_match = python_version_regex.search("version = 0.0.1")

    assert cs_match is not None and python_match is not None
    assert cs_match.group(1) == python_match.group(1)
