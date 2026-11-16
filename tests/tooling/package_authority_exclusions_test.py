"""Historical authority exclusions must never cover maintained consumer projects."""

from __future__ import annotations

import unittest
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]
ARCHIVED_PREFIX = '_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/'
APPROVED_PLATFORM_PREFIX = '_bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/'
APPROVED_PLATFORM_PROJECTS = {
    APPROVED_PLATFORM_PREFIX + path for path in (
        'coordinator-final-dependencies/platform/catalog/20261116T134852Z-668b8b128a6b4ff098a9ce224c0649ed/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj',
        'coordinator-final-dependencies/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj',
        'platform-corrected/platform/catalog/20261116T123849Z-724c847ff7be495aacb4b30f5f9942c3/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj',
        'platform-corrected/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj',
        'platform-final-correction/platform/catalog/20261116T124513Z-deb728e7995344b9a55088ff50964492/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj',
        'platform-final-correction/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj',
    )
}


class PackageAuthorityExclusionTests(unittest.TestCase):
    def test_only_exact_archived_projects_are_excluded(self) -> None:
        entries = [line.strip() for line in (ROOT / 'eng/package-authority-exclusions.txt').read_text().splitlines()
                   if line.strip() and not line.lstrip().startswith('#')]
        self.assertEqual(len(entries), len(set(entries)))
        self.assertTrue(entries)
        self.assertEqual(APPROVED_PLATFORM_PROJECTS,
                         {entry for entry in entries if not entry.startswith(ARCHIVED_PREFIX)})
        for entry in entries:
            with self.subTest(entry=entry):
                self.assertTrue(entry.startswith(ARCHIVED_PREFIX) or entry in APPROVED_PLATFORM_PROJECTS)
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
