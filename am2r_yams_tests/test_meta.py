import re
from pathlib import Path
from importlib.metadata import version
from am2r_yams import load_wrapper, YamsException
import pytest
from unittest.mock import patch
from pythonnet import load, unload


@pytest.fixture(scope="session", autouse=True)
def _mock_load_unload():
    with patch("am2r_yams.wrapper._load_cs_environment") as mocked_load:
        load("coreclr")
        import clr

        clr.AddReference("YAMS-LIB")
        from YAMS_LIB import Patcher as CSharp_Patcher

        yield
        unload()


def test_correct_versions():
    cs_version = ""
    with load_wrapper() as w:
        cs_version = w.get_csharp_version()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version


def test_throw_correct_exception():
    with pytest.raises(Exception) as excinfo:
        with load_wrapper() as w:
            import System

            raise System.Exception("Dummy Exception")

    assert excinfo.type is System.Exception
    assert "Dummy Exception" == str(excinfo.value)
