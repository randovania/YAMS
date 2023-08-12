import re
from pathlib import Path
from importlib.metadata import version
from am2r_yams import load_wrapper, YamsException
import pytest
from multiprocessing import Queue, Process, set_start_method


# Every yams call needs to be run in seperate process, as otherwise
# pythonnet gets unloaded multiple times and we segfault due to that
def setup_module():
    print("i was run!!!")
    set_start_method('spawn')


def test_correct_versions():
    def get_cs_version(queue) -> str:
        with load_wrapper() as w:
            queue.put(w.get_csharp_version())

    cs_version = ""
    queue = Queue()
    p = Process(target=get_cs_version, args=(queue,))
    p.run()
    cs_version = queue.get()
    python_version = version("am2r_yams")

    assert cs_version != ""
    assert cs_version == python_version


def test_throw_correct_exception():

    class RaisingProcess(Process):
        def run(self):
            try:
                self._run()
            except Exception as e:
                raise e
        def _run(self):
            if self._target:
                self._target(*self._args, **self._kwargs)


    def throw_exception():
        with load_wrapper() as w:
            import System

            raise System.Exception("Dummy Exception")

    with pytest.raises(Exception) as excinfo:
        p = RaisingProcess(target=throw_exception)
        p.run()
    assert excinfo.type is YamsException
    assert "Dummy Exception" == str(excinfo.value)

