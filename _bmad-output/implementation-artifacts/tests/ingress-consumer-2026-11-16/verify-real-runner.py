"""Exercise the skipped real-runner compatibility case using an explicitly packaged Release assembly."""
import importlib.util
import os
from pathlib import Path
import shutil
import subprocess
import unittest
root = Path.cwd()
repo = Path(os.environ['TMPDIR']) / 'repo'
repo.mkdir()
subprocess.run(['rsync', '-a', '--exclude=references', '--exclude=.git', '--exclude=bin', '--exclude=obj',
    '--exclude=_bmad', '--exclude=_bmad-output', '--exclude=artifacts', '--exclude=node_modules', '--exclude=.agents',
    '--exclude=.codex', '--exclude=.claude', str(root) + '/', str(repo) + '/'], check=True)
(repo / 'references').symlink_to(root / 'references', target_is_directory=True)
project = repo / 'test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj'
subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '-m:1', '-nr:false', '-p:MinVerVersionOverride=1.0.0'], check=True)
# The existing test/gate locates bin/Debug. This explicit harness alias exercises the package-built runner,
# without claiming a Debug/source build or altering production gate/test code.
(project.parent / 'bin/Debug').symlink_to('Release', target_is_directory=True)
suite = unittest.TestSuite()
for story, class_name in (("52", "Story52RealRunnerCompatibilityTests"), ("53", "Story53RealRunnerCompatibilityTests")):
    spec = importlib.util.spec_from_file_location("real_runner_" + story, root / ("tests/tooling/story_" + story + "_verifier_test.py"))
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
    m.ROOT = repo
    m.SCRIPT = root / ("eng/verify-story-" + story[0] + "." + story[1] + ".ps1")
    suite.addTests(unittest.defaultTestLoader.loadTestsFromTestCase(getattr(m, class_name)))
result = unittest.TextTestRunner(verbosity=2).run(suite)
raise SystemExit(0 if result.wasSuccessful() and not result.skipped else 1)
