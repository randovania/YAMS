import re
from pathlib import Path
from importlib.metadata import version
from am2r_yams import load_wrapper, YamsException
import pytest
import multiprocessing


def test_correct_versions():
    cs_version = ""
    with load_wrapper() as w:
        cs_version = w.get_csharp_version()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version


def t_test_throw_correct_exception():
    def _throw_exception():
        with load_wrapper() as w:
            import System

            raise System.Exception("Dummy Exception")

    with pytest.raises(Exception) as excinfo:
        # Needs to be run in seperate process, as otherwise
        # pythonnet gets unloaded multiple times and we segfault due to that
        multiprocessing.set_start_method('fork')
        p = multiprocessing.Process(target=_throw_exception)
        p.start()
    assert "Dummy Exception" == str(excinfo.value)
