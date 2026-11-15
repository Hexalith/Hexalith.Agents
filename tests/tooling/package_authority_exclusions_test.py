"""Historical authority exclusions must never cover maintained consumer projects."""

from __future__ import annotations

import unittest
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]


class PackageAuthorityExclusionTests(unittest.TestCase):
    def test_only_exact_archived_projects_are_excluded(self) -> None:
        entries = [line.strip() for line in (ROOT / 'eng/package-authority-exclusions.txt').read_text().splitlines()
                   if line.strip() and not line.lstrip().startswith('#')]
        self.assertEqual(len(entries), len(set(entries)))
        self.assertTrue(entries)
        for entry in entries:
            with self.subTest(entry=entry):
                self.assertTrue(entry.startswith('_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/'))
                self.assertTrue(entry.endswith('.csproj'))
                self.assertNotRegex(entry, r'[?*]|(^|/)\.\.(/|$)')
                self.assertTrue((ROOT / entry).is_file())

        solution = ElementTree.parse(ROOT / 'Hexalith.Agents.slnx')
        active = {project.attrib['Path'] for project in solution.iter('Project')}
        self.assertEqual(len(active), 12)
        self.assertFalse(active.intersection(entries))
        for project in active:
            with self.subTest(project=project):
                self.assertNotIn('_bmad-output', (ROOT / project).read_text())


if __name__ == '__main__':
    unittest.main()
