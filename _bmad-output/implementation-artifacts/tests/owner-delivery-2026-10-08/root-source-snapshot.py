"""Hash existing source/build inputs without initializing any submodules."""

import hashlib
import json
import os
from pathlib import Path
import re
import sys


evidence = Path(__file__).resolve().parent
workspace = evidence.parents[3]
roots = [workspace.parent / name for name in ["parties", "eventstore", "conversations", "platform", "commons", "tenants"]]
paths = set(json.loads((evidence / "source-before-consistent-repeat.json").read_text()))
scan_roots = list(roots)
for root in roots:
    modules = root / ".gitmodules"
    if modules.exists():
        for relative in re.findall(r"^\s*path\s*=\s*(.+?)\s*$", modules.read_text(), re.M):
            target = root / relative
            if target.is_dir():
                scan_roots.append(target)

excluded = {".git", ".artifacts", "bin", "obj", "node_modules", ".vs", ".idea", "references", "_bmad", "_bmad-output", ".agents", ".claude", ".codex", ".aspire", "dist", "TestResults"}
extensions = {".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".ps1", ".sh", ".xml", ".resx", ".razor", ".cshtml", ".yml", ".yaml", ".config", ".css", ".js", ".ts", ".lock"}
names = {".editorconfig", ".gitattributes", ".gitmodules"}
for root in scan_roots:
    for current, directories, filenames in os.walk(root):
        directories[:] = [name for name in directories if name not in excluded]
        for name in filenames:
            path = Path(current) / name
            if path.suffix in extensions or name in names:
                paths.add(str(path.absolute()))

hashes = {}
for filename in sorted(paths):
    path = Path(filename)
    hashes[filename] = hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None
Path(sys.argv[1]).write_text(json.dumps(hashes, indent=2) + "\n")
print(json.dumps({"checkedPaths": len(hashes), "existingScanRoots": len(scan_roots), "snapshot": sys.argv[1], "scope": "Existing primary/root-declared reference source/build files only; nested references and build outputs excluded; no initialization/update."}))
