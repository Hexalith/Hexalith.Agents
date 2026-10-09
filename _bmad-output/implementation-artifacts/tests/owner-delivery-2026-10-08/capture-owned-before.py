"""Capture an explicit owner unit before edits; never infer ownership from Git state."""

import datetime
import hashlib
import json
from pathlib import Path
import sys


evidence = Path(__file__).resolve().parent
workspace = evidence.parents[3]
output = Path(sys.argv[1]).resolve()
output.mkdir(parents=True, exist_ok=False)
rows = []
for supplied in sys.argv[2:]:
    relative = Path(supplied)
    if relative.is_absolute() or ".." in relative.parts or len(relative.parts) < 2:
        raise ValueError("Use explicit repository/path relative to the sibling repository parent")
    repository = relative.parts[0]
    if repository not in {"agents", "parties", "eventstore", "conversations", "platform"}:
        raise ValueError("Unsupported owner repository")
    path = workspace.parent / relative
    existed = path.is_file()
    data = path.read_bytes() if existed else b""
    before = output / "before" / relative
    if existed:
        before.parent.mkdir(parents=True, exist_ok=True)
        before.write_bytes(data)
    rows.append({"repository": repository, "relativePath": str(Path(*relative.parts[1:])),
                 "existed": existed, "sha256": hashlib.sha256(data).hexdigest(),
                 "beforeCopy": str(before) if existed else None})
if not rows:
    raise ValueError("Explicit source/test paths are required")
capture = output / "before-hashes.json"
capture.write_text(json.dumps({"capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
                               "scope": "Explicit before-edit owner preservation capture", "files": rows}, indent=2) + "\n")
print(json.dumps({"capture": str(capture), "paths": len(rows)}))
