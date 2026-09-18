"""Executable contract between Dependabot prefixes and commitlint policy."""

from __future__ import annotations

import re
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DEPENDABOT = ROOT / ".github" / "dependabot.yml"
COMMITLINT = ROOT / "node_modules" / ".bin" / "commitlint"


class DependencyCommitContractTests(unittest.TestCase):
    """Require every configured Dependabot subject prefix to pass commitlint."""

    def test_dependabot_prefixes_pass_commitlint(self) -> None:
        prefixes = re.findall(
            r'^\s*prefix:\s*"([^"]+)"\s*$',
            DEPENDABOT.read_text(encoding="utf-8"),
            flags=re.MULTILINE,
        )
        self.assertEqual(prefixes, ["chore(deps)", "chore(deps)", "ci(deps)"])

        for prefix in prefixes:
            with self.subTest(prefix=prefix):
                result = subprocess.run(
                    [str(COMMITLINT)],
                    cwd=ROOT,
                    input=f"{prefix}: update dependencies\n",
                    capture_output=True,
                    text=True,
                )
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
