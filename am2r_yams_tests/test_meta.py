import re
from pathlib import Path
from importlib.metadata import version
import am2r_yams

def test_correct_versions():
    cs_version = ""
    with am2r_yams.load_wrapper() as w:
        cs_version = w.get_csharp_version()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version
