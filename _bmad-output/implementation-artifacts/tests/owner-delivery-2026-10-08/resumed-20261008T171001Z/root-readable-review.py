"""Produce line-ending-normalized review and sequential inspection deltas from exact frozen copies."""

import difflib
import hashlib
import json
from pathlib import Path
import sys


def sha(value):
    return hashlib.sha256(value).hexdigest()


def lines(value):
    return value.decode("utf-8-sig").replace("\r\n", "\n").splitlines(keepends=True)


manifest_path = Path(sys.argv[1]).resolve()
manifest = json.loads(manifest_path.read_text())
previous_path = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else None
previous = json.loads(previous_path.read_text()) if previous_path else None
previous_files = {(item["repository"], item["relativePath"]): item for item in previous["files"]} if previous else {}
baselines = {}
for capture in manifest["inputs"]:
    path = Path(capture["path"])
    content = json.loads(path.read_text())
    captured = content if isinstance(content, list) else content.get("files", content.get("sources"))
    for original in captured:
        item = dict(original)
        if "repository" not in item:
            source = Path(item["path"])
            relative = source.relative_to(Path.cwd().parent) if source.is_absolute() else source
            item["repository"], item["relativePath"] = relative.parts[0], str(Path(*relative.parts[1:]))
        key = item["repository"], item["relativePath"]
        if key in baselines:
            continue
        copied = item.get("beforeCopy") or item.get("copy")
        copied = Path(copied) if copied else path.parent / "before" / key[0] / key[1]
        if not copied.is_absolute():
            copied = path.parent / copied
        data = copied.read_bytes() if copied.is_file() else b""
        expected = item.get("beforeSha256") or item.get("sha256")
        if expected and sha(data) != expected:
            raise ValueError(f"Baseline hash mismatch: {key}")
        baselines[key] = data

all_diff, delta_diff, index = [], [], []
for item in manifest["files"]:
    key = item["repository"], item["relativePath"]
    current = Path(item["reviewCopy"]).read_bytes()
    if sha(current) != item["currentSha256"]:
        raise ValueError(f"Current frozen-copy hash mismatch: {key}")
    before = baselines[key]
    if sha(before) != item["beforeSha256"]:
        raise ValueError(f"Original baseline hash mismatch: {key}")
    all_patch = list(difflib.unified_diff(lines(before), lines(current), fromfile=f"a/{key[0]}/{key[1]}", tofile=f"b/{key[0]}/{key[1]}"))
    all_diff.extend(all_patch)
    earlier = previous_files.get(key)
    prior = Path(earlier["reviewCopy"]).read_bytes() if earlier else before
    if earlier and sha(prior) != earlier["currentSha256"]:
        raise ValueError(f"Earlier frozen-copy hash mismatch: {key}")
    patch = list(difflib.unified_diff(lines(prior), lines(current), fromfile=f"prior/{key[0]}/{key[1]}", tofile=f"current/{key[0]}/{key[1]}"))
    delta_diff.extend(patch)
    index.append({"repository": key[0], "relativePath": key[1], "currentSha256": sha(current), "unifiedLines": len(all_patch), "deltaLines": len(patch), "priorBasis": "previous frozen copy" if earlier else "original preserved baseline"})

directory = manifest_path.parent
readable = directory / "owned-current-readable.diff"
delta = directory / "root-inspection-delta.diff"
if any(path.exists() for path in (readable, delta, directory / "root-readable-review-index.json")):
    raise ValueError("Fresh readable-review artifacts are required")
readable.write_text("".join(all_diff))
delta.write_text("".join(delta_diff))
report = {"manifest": str(manifest_path), "manifestSha256": sha(manifest_path.read_bytes()), "previousManifest": str(previous_path) if previous_path else None, "normalization": "CRLF to LF only for readability; original frozen source and raw unified diff remain authoritative.", "readableDiff": str(readable), "readableSha256": sha(readable.read_bytes()), "readableLines": len(all_diff), "deltaDiff": str(delta), "deltaSha256": sha(delta.read_bytes()), "deltaLines": len(delta_diff), "files": index}
(directory / "root-readable-review-index.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({key: value for key, value in report.items() if key != "files"}))
