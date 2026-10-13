#!/bin/sh
set -eu

cd "$(dirname "$0")/.."

version=$(python - <<'PY'
import xml.etree.ElementTree as ET
manifest = ET.parse("VideoCropper/Package.appxmanifest")
print(manifest.find("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity").attrib["Version"])
PY
)
output=ReleaseArtifacts/portable
if [ "$#" -gt 0 ]; then
    output=$1
fi
name="VideoCropper_${version}_x64"
mkdir -p "$output"
mkdir "$output/$name"

dotnet publish VideoCropper/VideoCropper.csproj \
    --configuration Release --runtime win-x64 --self-contained true \
    -p:Platform=x64 -p:WindowsAppSDKSelfContained=true \
    -p:WindowsPackageType=None -p:PublishProfile= \
    --output "$output/$name"

python - "$output" "$name" <<'PY'
import hashlib
import pathlib
import sys
import zipfile

output = pathlib.Path(sys.argv[1])
name = sys.argv[2]
folder = output / name
archive = output / (name + ".zip")
with zipfile.ZipFile(archive, "x", compression=zipfile.ZIP_DEFLATED) as zipped:
    for path in sorted(folder.rglob("*")):
        if path.is_file():
            zipped.write(path, path.relative_to(folder))
with archive.open("rb") as stream:
    digest = hashlib.file_digest(stream, "sha256").hexdigest()
checksum = output / (name + ".sha256")
checksum.write_text(f"{digest}  {archive.name}\n", encoding="utf-8")
print(archive.resolve())
print(checksum.resolve())
PY
