import re
from pathlib import Path
from importlib.metadata import version
from am2r_yams import load_wrapper, YamsException
import pytest
import multiprocessing
from concurrent.futures import ProcessPoolExecutor

def _get_cs_version(pipe) -> str:
        with load_wrapper() as w:
            pipe.send(w.get_csharp_version())


def test_correct_versions():
    cs_version = ""
    receiving_pipe, output_pipe = multiprocessing.Pipe(True)
    with ProcessPoolExecutor(max_workers=1) as executor:
        future = executor.submit(_get_cs_version, output_pipe)
        future.result()
    cs_version = receiving_pipe.recv()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version

def _throw_exception():
    with load_wrapper() as w:
        import System

        raise System.Exception("Dummy Exception")

def test_throw_correct_exception():
    with pytest.raises(Exception) as excinfo:
        with ProcessPoolExecutor(max_workers=1) as executor:
            future = executor.submit(_throw_exception)
            future.result()
    assert excinfo.type is YamsException
    assert "Dummy Exception" == str(excinfo.value)
