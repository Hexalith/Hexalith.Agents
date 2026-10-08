"""Freeze explicitly captured owner paths for review; never infer ownership from Git HEAD."""

import datetime
import difflib
import hashlib
import json
from pathlib import Path
import sys


def sha(data):
    return hashlib.sha256(data).hexdigest()


evidence = Path(__file__).resolve().parent
workspace = evidence.parents[3]
output = Path(sys.argv[1]).resolve()
output.mkdir(parents=True, exist_ok=False)
owned = {}
inputs = []
for filename in sys.argv[2:]:
    metadata = Path(filename).resolve()
    content = json.loads(metadata.read_text())
    inputs.append({"path": str(metadata), "sha256": sha(metadata.read_bytes())})
    if isinstance(content, list):
        captured_files = content
    else:
        captured_files = content.get("files", content.get("sources"))
    if captured_files is None:
        raise ValueError(f"Not an explicit owner baseline capture: {metadata}")
    for captured_item in captured_files:
        item = dict(captured_item)
        if "repository" not in item or "relativePath" not in item:
            source = Path(item["path"])
            relative = source.relative_to(workspace.parent) if source.is_absolute() else source
            item["repository"] = relative.parts[0]
            item["relativePath"] = str(Path(*relative.parts[1:]))
        if "exists" in item:
            item["existed"] = item["exists"]
        key = (item["repository"], item["relativePath"])
        if key in owned:
            owned[key]["captureInputs"].append(str(metadata))
            continue
        supplied = item.get("beforeCopy") or item.get("copy")
        copied = Path(supplied) if supplied else None
        if copied and not copied.is_absolute():
            copied = metadata.parent / copied
        existed = item.get("existed", not item.get("newFile", False))
        if existed and copied is None:
            candidate = metadata.parent / "before" / item["repository"] / item["relativePath"]
            if candidate.is_file():
                copied = candidate
        before = copied.read_bytes() if copied and copied.exists() else b""
        expected = item.get("beforeSha256") or item.get("sha256")
        if existed and (copied is None or not copied.exists()):
            raise ValueError(f"Missing preserved baseline: {key}")
        if expected and sha(before) != expected:
            raise ValueError(f"Preserved baseline hash mismatch: {key}")
        owned[key] = {"before": before, "existed": existed, "captureInputs": [str(metadata)]}

diff = []
files = []
for (repository, relative), captured in sorted(owned.items()):
    primary = workspace.parent / repository / relative
    current = primary.read_bytes() if primary.exists() else b""
    preserved = output / "source" / repository / relative
    preserved.parent.mkdir(parents=True, exist_ok=True)
    preserved.write_bytes(current)
    before = captured.pop("before")
    diff.extend(difflib.unified_diff(
        before.decode("utf-8-sig").splitlines(keepends=True),
        current.decode("utf-8-sig").splitlines(keepends=True),
        fromfile=f"a/{repository}/{relative}" if captured["existed"] else "/dev/null",
        tofile=f"b/{repository}/{relative}" if primary.exists() else "/dev/null",
    ))
    files.append({"repository": repository, "relativePath": relative, "sourcePath": str(primary),
                  "reviewCopy": str(preserved), "beforeSha256": sha(before), "currentSha256": sha(current),
                  "changed": before != current, **captured})

diff_path = output / "owned-current.diff"
diff_path.write_text("".join(diff))
manifest = {"capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "scope": "Explicit owner-path baseline-to-current review snapshot, not a complete delivery target or Git-owned revision.",
            "limits": "Check phase ownership/source drift separately; overlapping concurrent edits are not attributed automatically.",
            "inputs": inputs, "files": files, "diffPath": str(diff_path), "diffSha256": sha(diff_path.read_bytes())}
(output / "review-input.json").write_text(json.dumps(manifest, indent=2) + "\n")
print(json.dumps({"files": len(files), "changed": sum(f["changed"] for f in files),
                  "diffPath": str(diff_path), "diffBytes": diff_path.stat().st_size,
                  "diffSha256": manifest["diffSha256"]}))
