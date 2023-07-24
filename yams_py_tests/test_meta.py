import re
from pathlib import Path
from importlib.metadata import version
from yams_py import Patcher


def test_correct_versions():
    cs_version = Patcher.get_lib_version()
    python_version = version("yams_py")

    assert cs_version != ""
    assert cs_version == python_version
