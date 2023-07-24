import re
from pathlib import Path
from importlib.metadata import version
from yams_py import Patcher


def test_correct_versions():
    cs_version = Patcher.get_lib_version()
    python_version = version("yams-py")

    assert cs_match is not None and python_version is not None
    assert cs_match.group(1) == python_version
