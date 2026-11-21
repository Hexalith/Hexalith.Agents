"""Create and inspect isolated consumers without launching processes."""
from __future__ import annotations
import hashlib,json,ntpath,os,shutil,re,textwrap,zipfile
import base64,ctypes,contextlib,sys
from collections import Counter
from pathlib import Path
from urllib.parse import unquote,urlparse
from xml.etree import ElementTree
from xml.sax.saxutils import quoteattr
ROOT = Path(__file__).resolve().parents[1]

def package_identity(path: Path) -> tuple[str, str]:
    """Return package id and version from one archive."""
    with zipfile.ZipFile(path) as archive:
        nuspec = next(name for name in archive.namelist() if name.endswith(".nuspec"))
        root = ElementTree.fromstring(archive.read(nuspec))
        values = {element.tag.rsplit("}", 1)[-1]: (element.text or "").strip() for element in root.iter()}
        return values["id"], values["version"]


def json_strings(value: object):
    """Yield every JSON key and string value so path disclosures cannot hide in either."""
    if isinstance(value, dict):
        for key, item in value.items():
            yield str(key)
            yield from json_strings(item)
    elif isinstance(value, list):
        for item in value:
            yield from json_strings(item)
    elif isinstance(value, str):
        yield value


def normalize_disclosure_path(value: str) -> str:
    """Normalize URI escaping, Windows separators/case, and existing symlink components."""
    decoded = unquote(value.strip())
    if decoded.casefold().startswith("file:"):
        parsed = urlparse(decoded)
        decoded = parsed.path or parsed.netloc
    decoded = decoded.replace("\\", "/")
    if re.match(r"^[A-Za-z]:/", decoded):
        return ntpath.normcase(ntpath.normpath(decoded.replace("/", "\\"))).replace("\\", "/")
    try:
        return str(Path(decoded).resolve(strict=False)).replace("\\", "/").casefold()
    except (OSError, ValueError):
        return os.path.normcase(os.path.normpath(decoded)).replace("\\", "/").casefold()


def active_import_paths(expanded: str) -> list[str]:
    """Read MSBuild's active import filename comment lines, without extension filtering."""
    return [line.strip() for line in expanded.splitlines()
        if Path(line.strip()).is_absolute() and Path(line.strip()).is_file()]


def archive_file_inventory(archive: zipfile.ZipFile) -> set[str]:
    """Match NuGet's redundant slash normalization without admitting ambiguous/traversing members."""
    members = set()
    for item in archive.infolist():
        if item.is_dir():
            continue
        name = item.filename
        components = name.split("/")
        if name.startswith("/") or "\\" in name or any(part in (".", "..") for part in components):
            raise ValueError("unsafe package archive member for executable provenance")
        normalized = "/".join(part for part in components if part)
        if not normalized or normalized in members:
            raise ValueError("ambiguous normalized package archive member for executable provenance")
        members.add(normalized)
    return members


def executable_asset_receipt(assets: dict, packages: Path, analyzers: list[str], imports: list[str], consumer_root: Path | None = None, observation: dict | None = None) -> dict:
    """Separate selected compiler/import inputs from merely available package archive members."""
    owned = []
    available = []
    for identity, library in sorted(assets["libraries"].items()):
        if library.get("type") != "package":
            continue
        package_id, version = identity.rsplit("/", 1)
        package_root = (packages / library["path"]).resolve()
        archive_path = package_root / (package_id.casefold() + "." + version + ".nupkg")
        if not archive_path.is_file():
            raise ValueError(f"package archive is missing for executable provenance: {identity}")
        with zipfile.ZipFile(archive_path) as archive:
            members = archive_file_inventory(archive)
        owned.append((package_root, package_id, version, set(library.get("files", [])), members))
        for name in library.get("files", []):
            if name.startswith(("analyzers/", "build/", "buildTransitive/", "buildMultiTargeting/")) or name.casefold().endswith((".dll", ".exe", ".so", ".dylib")):
                path = package_root / name
                if path.is_file():
                    available.append({"package": package_id, "version": version, "path": name,
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    selected, sdk, sdk_imports, generated, sdk_loads, generated_loads, sdk_native, system_native = [], [], [], [], [], [], [], []
    dotnet_root = Path(shutil.which("dotnet") or "dotnet").resolve().parent
    if observation is None:
        raise ValueError("consumer executable load observation is required")
    kinds = (("analyzer", analyzers), ("import", imports), ("assembly", list(observation["loads"])), ("native", list(observation["nativeLoads"])))
    for kind, paths in kinds:
        for value in sorted(set(paths)):
            path = Path(value).resolve()
            if not path.is_file():
                raise ValueError(f"selected {kind} does not exist: {path}")
            digest = hashlib.sha256(path.read_bytes()).hexdigest()
            if kind in ("assembly", "native") and observation["loads" if kind == "assembly" else "nativeLoads"][str(path)] != digest:
                raise ValueError(f"observed executable bytes changed: {path}")
            for package_root, package_id, version, library_files, archive_files in owned:
                if path.is_relative_to(package_root):
                    relative = path.relative_to(package_root).as_posix()
                    if relative not in library_files or relative not in archive_files:
                        raise ValueError(f"unowned executable consumer asset absent from exact archive/library inventory: {path}")
                    selected.append({"kind": kind, "package": package_id, "version": version,
                        "path": path.relative_to(package_root).as_posix(),
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
                    break
            else:
                if any(path.is_relative_to(dotnet_root / area) for area in ("sdk", "packs", "sdk-manifests", "shared", "host")):
                    entry = {"path": path.relative_to(dotnet_root).as_posix(),
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
                    (sdk if kind == "analyzer" else sdk_imports if kind == "import" else sdk_loads if kind == "assembly" else sdk_native).append(entry)
                elif kind == "native" and any(path.is_relative_to(Path(root).resolve()) for root in ("/lib", "/lib64", "/usr/lib")):
                    system_native.append({"path": str(path), "sha256": digest})
                elif kind == "assembly" and path == Path(observation["observer"]):
                    generated_loads.append({"kind": "observer", "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
                elif (kind == "import" and consumer_root is not None
                        and ((path.parent == (consumer_root / "obj").resolve()
                              and path.name.endswith((".csproj.nuget.g.props", ".csproj.nuget.g.targets")))
                             or path == (consumer_root / "Directory.Packages.props").resolve()
                             or path == (consumer_root / "PackageConsumer.csproj").resolve()
                             or path == (consumer_root / "Consumer.csproj").resolve()
                             or (path.parent == consumer_root.resolve()
                                 and path.name in ("Directory.Build.props", "Directory.Build.targets")
                                 and path.read_bytes() == b"<Project />\n"))):
                    generated.append({"path": path.relative_to(consumer_root).as_posix()})
                else:
                    raise ValueError(f"unowned executable consumer asset: {path}")
    key = lambda entry: json.dumps(entry, sort_keys=True)
    return {"selected": sorted(selected, key=key), "available": sorted(available, key=key),
            "sdkAnalyzers": sorted(sdk, key=key), "sdkImports": sorted(sdk_imports, key=key),
            "generatedImports": sorted(generated, key=key),
            "sdkAssemblies": sorted(sdk_loads, key=key), "generatedAssemblies": generated_loads,
            "observedProcesses": observation["processes"], "sdkNativeLibraries": sorted(sdk_native, key=key),
            "systemNativeLibraries": sorted(system_native, key=key),
            "systemProcessObservers": [observation["systemProcessObserver"]] if "systemProcessObserver" in observation else [],
            "generatedExecutables": [{"kind": "kernel-process-observer", "sha256": observation["processObserverSha256"]}]
                if "processObserverSha256" in observation else []}


AGENTS_PACKAGE_IDS = frozenset(package_id.casefold() for package_id in (
    "Hexalith.Agents", "Hexalith.Agents.Client", "Hexalith.Agents.Contracts",
    "Hexalith.Agents.EventStore", "Hexalith.Agents.Testing", "Hexalith.Agents.UI"))


def policy_selection(selected: list[dict], produced_version: str | None = None) -> list[dict]:
    """Bind UI imports to the validated release version and Builds-catalog package versions to one placeholder,
    while keeping every kind, package, path and byte fingerprint exact."""
    result = []
    for entry in selected:
        value = dict(entry)
        if produced_version is not None and value.get("package") == "Hexalith.Agents.UI" and value.get("version") == produced_version:
            value["version"] = "$produced"
        elif "version" in value and str(value.get("package", "")).casefold() not in AGENTS_PACKAGE_IDS:
            value["version"] = "$catalog"
        result.append(value)
    return result


def enforce_asset_policy(receipts: list[dict], policy: dict, produced_version: str | None = None) -> None:
    """Fail closed on unreviewed selected package code, regardless of nuspec exclusion metadata."""
    actual = sorted({json.dumps(entry, sort_keys=True) for receipt in receipts
                     for entry in policy_selection(receipt["selected"], produced_version)})
    expected = sorted(json.dumps(entry, sort_keys=True) for entry in policy.get("selected", []))
    per_consumer = {receipt["consumer"]: policy_selection(receipt["selected"], produced_version) for receipt in receipts}
    if policy.get("schemaVersion") != 3 or actual != expected or per_consumer != policy.get("consumers"):
        raise ValueError("consumer executable asset baseline changed; review package/version/path/SHA-256 "
            f"before updating eng/consumer-executable-assets.json; added={sorted(set(actual)-set(expected))}; "
            f"removed={sorted(set(expected)-set(actual))}")


def prepare_consumer(directory: Path, package_directory: Path, identities: list[tuple[str, str]],
                     all_identities: list[tuple[str, str]]) -> None:
    """Write package-only project inputs, isolated from parent repository imports."""
    directory.mkdir(parents=True, exist_ok=True)
    (directory / "Directory.Build.props").write_text("<Project />\n")
    (directory / "Directory.Build.targets").write_text("<Project />\n")
    (directory / ".editorconfig").write_text("root = true\n")
    shutil.copyfile(ROOT / "global.json", directory / "global.json")
    package_versions = "\n".join(
        f'    <PackageVersion Include="{package_id}" Version="{version}" />'
        for package_id, version in all_identities
    )
    package_references = "\n".join(
        f'    <PackageReference Include="{package_id}" />' for package_id, _ in identities
    )
    (directory / "Directory.Packages.props").write_text(
        "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n"
        "  </PropertyGroup>\n  <ItemGroup>\n"
        f"{package_versions}\n  </ItemGroup>\n</Project>\n",
        encoding="utf-8",
    )
    project = directory / "PackageConsumer.csproj"
    project.write_text(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net11.0</TargetFramework>\n"
        "    <Nullable>enable</Nullable>\n    <ImplicitUsings>enable</ImplicitUsings>\n"
        "    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n  <ItemGroup>\n"
        f"{package_references}\n  </ItemGroup>\n</Project>\n",
        encoding="utf-8",
    )
    probes = {
        "Hexalith.Agents": "Hexalith.Agents.AgentsAssemblyMarker",
        "Hexalith.Agents.Client": "Hexalith.Agents.Client.AgentsClientAssemblyMarker",
        "Hexalith.Agents.Contracts": "Hexalith.Agents.Contracts.Agent.AgentLifecycleStatus",
        "Hexalith.Agents.EventStore": "Hexalith.Agents.EventStore.AgentsEventStoreServiceCollectionExtensions",
        "Hexalith.Agents.Testing": "Hexalith.Agents.Testing.AgentsTestingAssemblyMarker",
        "Hexalith.Agents.UI": "Hexalith.Agents.UI.AgentsUIAssemblyMarker",
    }
    types = ",".join(f"typeof({probes[package_id]})" for package_id, _ in identities)
    (directory / "ConsumerProbe.cs").write_text(
        "namespace Hexalith.Agents.PackageConsumer; public static class ConsumerProbe { "
        f"public static System.Type[] PublicPackageTypes {{ get; }} = [{types}];" + " }", encoding="utf-8")
    config = directory / "NuGet.config"
    local_patterns = "\n".join(
        f'                  <package pattern="{package_id}" />' for package_id, _ in all_identities
    )
    config.write_text(
        textwrap.dedent(f"""\
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="agents" value={quoteattr(str(package_directory))} />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
          </packageSources>
          <packageSourceMapping>
            <packageSource key="agents">
    {local_patterns}
            </packageSource>
            <packageSource key="nuget.org"><package pattern="*" /></packageSource>
          </packageSourceMapping>
        </configuration>
        """),
        encoding="utf-8",
    )



def validate_consumer_assets(directory: Path, package_directory: Path, identities: list[tuple[str, str]],
                             all_identities: list[tuple[str, str]]) -> None:
    """Reject project references, foreign local feeds and repository source disclosures."""
    packages = directory / "packages"
    assets_path = directory / "obj" / "project.assets.json"
    assets_text = assets_path.read_text(encoding="utf-8")
    assets = json.loads(assets_text)
    project_libraries = sorted(
        name for name, value in assets["libraries"].items() if value.get("type") == "project"
    )
    if project_libraries:
        raise ValueError(f"isolated consumer resolved repository projects: {project_libraries}")

    expected_agent_libraries = {f"{package_id}/{version}".casefold() for package_id, version in identities}
    actual_agent_libraries = {
        name.casefold()
        for name, value in assets["libraries"].items()
        if value.get("type") == "package" and name.casefold().startswith("hexalith.agents/")
        or value.get("type") == "package" and name.casefold().startswith("hexalith.agents.")
    }
    if not expected_agent_libraries <= actual_agent_libraries or not actual_agent_libraries <= {
        f"{package_id}/{version}".casefold() for package_id, version in all_identities
    }:
        raise ValueError(
            "isolated consumer Agents package identity mismatch; "
            f"missing={sorted(expected_agent_libraries - actual_agent_libraries)}; "
            f"unexpected={sorted(actual_agent_libraries - expected_agent_libraries)}"
        )

    expected_source = normalize_disclosure_path(str(package_directory))
    for package_id, version in all_identities:
        if f"{package_id}/{version}".casefold() not in actual_agent_libraries:
            continue
        metadata_path = packages / package_id.casefold() / version / ".nupkg.metadata"
        if not metadata_path.is_file():
            raise ValueError(f"missing NuGet source metadata for local package {package_id}/{version}")
        source = json.loads(metadata_path.read_text(encoding="utf-8")).get("source", "")
        if normalize_disclosure_path(source) != expected_source:
            raise ValueError(
                f"{package_id}/{version} did not resolve unambiguously from the local Agents feed: {source!r}"
            )

    disclosed_source_roots = [
        normalize_disclosure_path(str(ROOT / "src")),
        normalize_disclosure_path(str(ROOT / "test")),
        normalize_disclosure_path(str(ROOT / "references")),
    ]
    disclosed = sorted(
        {
            candidate
            for value in json_strings(assets)
            for candidate in [normalize_disclosure_path(value)]
            if any(candidate == root or candidate.startswith(f"{root}/") for root in disclosed_source_roots)
        }
    )
    if disclosed:
        raise ValueError(f"isolated consumer assets disclose repository source paths: {disclosed}")


def consumer_receipt(directory: Path, label: str, observation: dict) -> dict:
    """Qualify the exact observed build through the shared schema-3 ownership policy."""
    assets = json.loads((directory / "obj/project.assets.json").read_text())
    return {"consumer": label, **executable_asset_receipt(assets, directory / "packages",
        observation["analyzers"], observation["imports"], directory, observation)}

def check_consumer_prerequisites(require_compiler: bool = True) -> None:
    """Refuse unsupported qualification before restore, builds or package code can run."""
    if not sys.platform.startswith("linux"):
        raise ValueError("consumer executable load observation requires the Linux dynamic-loader trace")
    if not hasattr(ctypes.CDLL(None), "gnu_get_libc_version"):
        raise ValueError("consumer qualification requires glibc loader diagnostics and Linux ptrace/inotify")
    libc = ctypes.CDLL(None, use_errno=True)
    descriptor = libc.inotify_init1(os.O_NONBLOCK | os.O_CLOEXEC)
    if descriptor < 0:
        raise ValueError("consumer qualification requires permitted Linux inotify mutation tracing")
    os.close(descriptor)
    if require_compiler and shutil.which("cc") is None:
        raise ValueError("consumer qualification requires a native C compiler (cc) for independent ptrace observation")

@contextlib.contextmanager
def immutable_package_inputs(packages: Path):
    """Refuse every package write/rename, including an atomic restoration of the original bytes."""
    libc = ctypes.CDLL(None, use_errno=True)
    descriptor = libc.inotify_init1(os.O_NONBLOCK | os.O_CLOEXEC)
    if descriptor < 0:
        raise OSError(ctypes.get_errno(), "consumer qualification requires Linux inotify")
    try:
        for directory in [packages, *packages.rglob("*")]:
            # Access/open events are harmless. Every write, replacement, membership or metadata mutation refuses.
            if libc.inotify_add_watch(descriptor, os.fsencode(directory), 0x00000FCE) < 0:
                raise OSError(ctypes.get_errno(), "cannot watch complete consumer package inputs")
        yield
        try:
            changes = os.read(descriptor, 1024 * 1024)
        except BlockingIOError:
            changes = b""
        if changes:
            raise ValueError("consumer package input mutation occurred during observation")
    finally:
        os.close(descriptor)

def read_load_observation(traces: Path, observer: Path) -> dict:
    """Require completed MSBuild and compiler traces and retain exact loaded bytes."""
    files = sorted(traces.glob("*.loads"))
    loads, processes = {}, []
    for trace in files:
        rows = []
        for line in trace.read_text(encoding="utf-8").splitlines():
            kind, encoded, digest = line.split("\t")
            rows.append((kind, base64.b64decode(encoded, validate=True).decode("utf-8"), digest))
        if not rows or rows[0][0] != "start" or sum(row[0] == "complete" for row in rows) != 1:
            raise ValueError("consumer executable load observation is incomplete")
        processes.append(Path(rows[0][1]).name)
        for kind, value, digest in rows[1:]:
            if kind == "complete":
                continue
            if kind != "assembly":
                raise ValueError(f"unattributed executable load: {kind} {value}")
            path = Path(value).resolve()
            if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != digest:
                raise ValueError(f"observed executable bytes changed: {path}")
            loads[str(path)] = digest
    if not any(Path(path).name.casefold() == "microsoft.build.dll" for path in loads) or not any(name.casefold() == "csc.dll" for name in processes):
        raise ValueError("consumer executable load observation is missing MSBuild or the fresh compiler")
    if str(observer.resolve()) not in loads:
        raise ValueError("consumer executable load observer was not loaded")
    native_files = sorted(traces.glob("native.*"))
    if {path.name.split(".")[-1] for path in native_files} != {path.stem for path in files}:
        raise ValueError("native load observation is missing or contains an unobserved executable process")
    process_log = traces / "processes.log"
    if not process_log.is_file():
        raise ValueError("independent process execution observation is missing")
    rows = process_log.read_text().splitlines()
    if not rows or rows[-1] != "complete":
        raise ValueError("independent process execution observation is incomplete")
    created = {row.split("\t")[1] for row in rows if row.startswith("created\t")}
    executed = Counter(row.split("\t")[1] for row in rows if row.startswith("exec\t"))
    if created != set(executed) or created != {path.stem for path in files} or any(count != 1 for count in executed.values()):
        raise ValueError("independent process observation found unobserved child execution")
    native = {}
    for trace in native_files:
        paths = re.findall(r"calling init: ([^\r\n]+)", trace.read_text(encoding="utf-8"))
        if not paths:
            raise ValueError("native executable load observation is missing")
        for value in paths:
            path = Path(value.strip()).resolve()
            if not path.is_file():
                raise ValueError(f"observed native executable is missing: {path}")
            native[str(path)] = hashlib.sha256(path.read_bytes()).hexdigest()
    return {"loads": loads, "nativeLoads": native, "processes": sorted(processes), "observer": str(observer.resolve())}


def check_hook_prerequisites() -> None:
    """Require a system process tracer before packing/restoring/building in the product hook."""
    check_consumer_prerequisites(require_compiler=False)
    if shutil.which("strace") is None:
        raise ValueError("package hook qualification requires system strace with --kill-on-exit, fork/exec and environment tracing support")


def prepare_hook_observer(directory: Path) -> None:
    """Write the same approved managed observer without launching an opaque compiler wrapper."""
    check_hook_prerequisites()
    observer = directory / "obj/load-observer"
    observer.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(ROOT / "scripts/StartupHook.cs", observer / "StartupHook.cs")
    (observer / "Observer.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<TargetFramework>net11.0</TargetFramework><Nullable>enable</Nullable>'
        '<UseSharedCompilation>false</UseSharedCompilation><NuGetAudit>true</NuGetAudit>'
        '</PropertyGroup></Project>')


@contextlib.contextmanager
def hook_observation_environment(directory: Path):
    """Instrument fixed literal hook compiler calls without changing executable/source resolution."""
    traces = directory / "obj/assembly-loads"
    traces.mkdir()
    previous_hooks = os.environ.get("DOTNET_STARTUP_HOOKS")
    previous_trace = os.environ.get("AGENTS_CONSUMER_LOAD_TRACE")
    previous_packages = os.environ.get("NUGET_PACKAGES")
    previous_server = os.environ.get("DOTNET_CLI_USE_MSBUILD_SERVER")
    previous_nodes = os.environ.get("MSBUILDDISABLENODEREUSE")
    previous_debug = os.environ.get("LD_DEBUG")
    previous_debug_output = os.environ.get("LD_DEBUG_OUTPUT")
    os.environ["DOTNET_STARTUP_HOOKS"] = os.pathsep.join(filter(None, [previous_hooks, str((directory / "obj/load-observer/bin/Release/net11.0/Observer.dll").resolve())]))
    os.environ["AGENTS_CONSUMER_LOAD_TRACE"] = str(traces.resolve())
    os.environ["NUGET_PACKAGES"] = str((directory / "packages").resolve())
    os.environ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0"
    os.environ["MSBUILDDISABLENODEREUSE"] = "1"
    # strace enables LD_DEBUG only for the tracee. Absolute output survives a
    # compiler child changing its working directory without tracing strace itself.
    os.environ.pop("LD_DEBUG", None)
    os.environ["LD_DEBUG_OUTPUT"] = str((traces / "native").resolve())
    try:
        yield
    finally:
        if previous_hooks is None: os.environ.pop("DOTNET_STARTUP_HOOKS", None)
        else: os.environ["DOTNET_STARTUP_HOOKS"] = previous_hooks
        if previous_trace is None: os.environ.pop("AGENTS_CONSUMER_LOAD_TRACE", None)
        else: os.environ["AGENTS_CONSUMER_LOAD_TRACE"] = previous_trace
        if previous_packages is None: os.environ.pop("NUGET_PACKAGES", None)
        else: os.environ["NUGET_PACKAGES"] = previous_packages
        if previous_server is None: os.environ.pop("DOTNET_CLI_USE_MSBUILD_SERVER", None)
        else: os.environ["DOTNET_CLI_USE_MSBUILD_SERVER"] = previous_server
        if previous_nodes is None: os.environ.pop("MSBUILDDISABLENODEREUSE", None)
        else: os.environ["MSBUILDDISABLENODEREUSE"] = previous_nodes
        if previous_debug is None: os.environ.pop("LD_DEBUG", None)
        else: os.environ["LD_DEBUG"] = previous_debug
        if previous_debug_output is None: os.environ.pop("LD_DEBUG_OUTPUT", None)
        else: os.environ["LD_DEBUG_OUTPUT"] = previous_debug_output


def strace_process_inventory(traces: Path) -> None:
    """Translate actual complete system-tracer fork/exec observations to the shared strict inventory."""
    phases = [sorted(traces.glob(prefix + ".*")) for prefix in ("process-preprocess", "process-build")]
    if not all(phases):
        raise ValueError("independent strace process observation is missing")
    files = [path for phase in phases for path in phase]
    created, executed, complete, tracees = set(), [], set(), set()
    for path in files:
        pid = path.name.rsplit(".", 1)[-1]
        if not pid.isdecimal():
            raise ValueError("invalid strace process identity")
        pending = {}
        for line in path.read_text().splitlines():
            if "<unfinished ...>" in line:
                name = line.split("(", 1)[0]
                if name in ("clone", "clone3", "fork", "vfork", "execve", "execveat"):
                    pending[name] = line.split("<unfinished ...>")[0]
                continue
            resumed = re.match(r"<\.\.\. (\w+) resumed>(.*)", line)
            if resumed:
                name = resumed[1]
                if name in pending:
                    line = pending.pop(name) + resumed[2]
                elif name in ("clone", "clone3", "fork", "vfork", "execve", "execveat"):
                    raise ValueError("incomplete strace process observation")
            if re.match(r"execve(?:at)?\(", line) and re.search(r"= 0$", line):
                executed.append(pid)
            child = re.match(r"(?:clone3?|fork|vfork)\(.*\)\s+= (\d+)$", line)
            if child:
                tracees.add(child[1])
                if "CLONE_THREAD" not in line:
                    created.add(child[1])
            if re.fullmatch(r"\+\+\+ (?:exited with \d+|killed by [\w ]+(?: \(core dumped\))?) \+\+\+", line):
                complete.add(pid)
        if pending:
            raise ValueError("incomplete strace process observation")
    counts = Counter(executed)
    if any(not any(path.name.rsplit(".", 1)[-1] in counts for path in phase) for phase in phases):
        raise ValueError("incomplete strace process observation")
    tracees.update(counts)
    observed = {path.name.rsplit(".", 1)[-1] for path in files}
    created.update(counts)
    if not created or tracees != observed or complete != observed or any(count != 1 for count in counts.values()):
        raise ValueError("incomplete or repeated strace process execution observation")
    rows = [*("created\t" + pid for pid in sorted(created)), *("exec\t" + pid for pid in executed), "complete"]
    (traces / "processes.log").write_text("\n".join(rows) + "\n")


def hook_observation(directory: Path, evaluation: str) -> dict:
    """Bind the actual hook compiler/loader/process traces to the common schema-2 receipt."""
    traces = directory / "obj/assembly-loads"
    strace_process_inventory(traces)
    observer = directory / "obj/load-observer/bin/Release/net11.0/Observer.dll"
    observation = read_load_observation(traces, observer)
    observation["analyzers"] = [item["Identity"] for item in json.loads(evaluation)["Items"]["Analyzer"]]
    observation["imports"] = active_import_paths((directory / "imports.xml").read_text())
    observation["systemProcessObserver"] = {"name": "strace", "sha256": hashlib.sha256(Path(shutil.which("strace")).read_bytes()).hexdigest()}
    return observation
