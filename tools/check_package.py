"""Verify that the distributable contains exactly the current release files."""
import json
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
version = manifest["Version"]
assert f"<Version>{version}</Version>" in (root / "Archeo.csproj").read_text(encoding="utf-8")
expected = {name: root / name for name in ("manifest.json", "README.md", "CHANGELOG.md", "ROADMAP.md", "LICENSE")}
for folder in ("assets", "i18n"):
    expected.update({p.relative_to(root).as_posix(): p for p in (root / folder).rglob("*") if p.is_file()})
expected["Archeo.dll"] = root / "bin/Release/net6.0/Archeo.dll"
archive = root / "dist" / f"Archeo-{version}.zip"
with zipfile.ZipFile(archive) as release:
    assert release.testzip() is None, "ZIP integrity failure"
    actual = {name for name in release.namelist() if not name.endswith("/")}
    assert actual == {"Archeo/" + name for name in expected}, ("unexpected or missing release files", actual.symmetric_difference({"Archeo/" + name for name in expected}))
    for name, source in expected.items():
        assert release.read("Archeo/" + name) == source.read_bytes(), (name, "stale packaged file")
print(f"PASS: Archeo {version}, ZIP integrity and {len(expected)} current release files; no test harness or config included.")
