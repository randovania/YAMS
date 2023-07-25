import re
from pathlib import Path
from importlib.metadata import version
from am2r_yams.Patcher import Patcher


def test_correct_versions():
    cs_version = ""
    with Patcher() as p:
        cs_version = p.get_lib_version()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version
