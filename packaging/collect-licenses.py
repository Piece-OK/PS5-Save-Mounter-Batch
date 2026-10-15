#!/usr/bin/env python3
"""Record resolved package versions and preserve embedded upstream notices."""
import json
import re
import shutil
import sys
from pathlib import Path
from xml.etree import ElementTree as ET


def collect(assets_path: Path, destination: Path) -> list[dict]:
    assets = json.loads(assets_path.read_text())
    destination.mkdir(parents=True, exist_ok=True)
    packages = []
    for identifier, item in sorted(assets["libraries"].items()):
        if item.get("type") != "package":
            continue
        name, version = identifier.rsplit("/", 1)
        package = next((Path(folder) / item["path"] for folder in assets["packageFolders"]
                        if (Path(folder) / item["path"]).is_dir()), None)
        if package is None:
            raise FileNotFoundError(f"Restored package missing: {identifier}")
        nuspec = next(package.glob("*.nuspec"))
        xml = ET.parse(nuspec).getroot()
        metadata = next(e for e in xml if e.tag.rsplit("}", 1)[-1] == "metadata")
        fields = {e.tag.rsplit("}", 1)[-1]: e for e in metadata}
        license_element = fields.get("license")
        license_value = license_element.text if license_element is not None else None
        target = destination / name / version
        target.mkdir(parents=True, exist_ok=True)
        shutil.copy2(nuspec, target / nuspec.name)
        copied = []
        explicit = package / license_value if license_element is not None and license_element.get("type") == "file" and license_value else None
        for file in package.rglob("*"):
            if not file.is_file() or file.is_symlink():
                continue
            if file != explicit and not re.search(r"(^|[-_.])(licen[sc]e|copying|notice|copyright|third.party)", file.name, re.I):
                continue
            if file.stat().st_size > 4 * 1024 * 1024:
                continue
            relative = file.relative_to(package)
            output = target / relative
            output.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(file, output)
            copied.append(relative.as_posix())
        packages.append({"id": name, "version": version, "license": license_value,
                         "licenseType": license_element.get("type") if license_element is not None else None,
                         "notices": sorted(copied)})
    (destination / "packages.json").write_text(json.dumps(packages, indent=2) + "\n")
    return packages


if __name__ == "__main__":
    packages = collect(Path(sys.argv[1]), Path(sys.argv[2]))
    print(f"Recorded {len(packages)} resolved package versions and embedded notices")
