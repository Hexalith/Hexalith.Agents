"""Stage cumulative owned source for context-free review without touching Git."""

import datetime
import difflib
import hashlib
import json
from pathlib import Path
import sys
import tempfile


def sha(value):
    return hashlib.sha256(value).hexdigest()


manifest_path = Path(sys.argv[1]).resolve()
manifest = json.loads(manifest_path.read_text())
workspace = Path(__file__).resolve().parents[4]
baselines = {}
for capture in manifest["inputs"]:
    path = Path(capture["path"])
    if sha(path.read_bytes()) != capture["sha256"]:
        raise ValueError(f"Capture changed: {path}")
    data = json.loads(path.read_text())
    rows = data if isinstance(data, list) else data.get("files", data.get("sources"))
    for row in rows:
        if "repository" in row:
            key = row["repository"], row["relativePath"]
        else:
            source = Path(row["path"])
            relative = source.relative_to(workspace.parent) if source.is_absolute() else source
            key = relative.parts[0], str(Path(*relative.parts[1:]))
        if key in baselines:
            continue
        supplied = row.get("beforeCopy") or row.get("copy")
        copied = Path(supplied) if supplied else path.parent / "before" / key[0] / key[1]
        if not copied.is_absolute():
            copied = path.parent / copied
        before = copied.read_bytes() if copied.is_file() else b""
        expected = row.get("beforeSha256") or row.get("sha256")
        if expected and sha(before) != expected:
            raise ValueError(f"Baseline changed: {key}")
        baselines[key] = before

patches = []
sections = 0
for row in manifest["files"]:
    key = row["repository"], row["relativePath"]
    before = baselines[key]
    current = Path(row["reviewCopy"]).read_bytes()
    if sha(before) != row["beforeSha256"] or sha(current) != row["currentSha256"]:
        raise ValueError(f"Frozen source mismatch: {key}")
    source = Path(row["sourcePath"])
    actual = source.read_bytes() if source.is_file() else b""
    if sha(actual) != row["currentSha256"]:
        raise ValueError(f"Live source drift: {source}")
    normalize = lambda value: value.decode("utf-8-sig").replace("\r\n", "\n").splitlines(keepends=True)
    patch = list(difflib.unified_diff(normalize(before), normalize(current),
        fromfile=str(source) if row["existed"] else "/dev/null",
        tofile=str(source) if source.exists() else "/dev/null"))
    if patch:
        sections += 1
        patches.extend(patch)

with tempfile.NamedTemporaryFile(prefix="hexalith-owner-source-review-", suffix=".diff", mode="w", delete=False) as stream:
    stream.writelines(patches)
    target = Path(stream.name)
report = {
    "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "manifest": str(manifest_path), "manifestSha256": sha(manifest_path.read_bytes()),
    "diffFile": str(target), "diffSha256": sha(target.read_bytes()),
    "diffBytes": target.stat().st_size, "sections": sections,
    "scope": "Cumulative earliest explicit before-capture to frozen current owned source, including new files; absolute source headers; CRLF normalized only for review readability. No staging or Git mutations.",
}
output = manifest_path.parent / "root-staged-diff.json"
if output.exists():
    raise ValueError("Fresh staging metadata is required")
output.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps(report))
