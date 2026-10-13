#!/bin/sh
set -eu

cd "$(dirname "$0")/.."

output=ReleaseArtifacts/setup
if [ "$#" -gt 0 ]; then
    output=$1
fi

# Invoke the required compiler before spending time building the payload.
ISCC.exe --version
sh scripts/build-portable.sh "$output"

python - "$output" <<'PY'
import hashlib
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET

manifest = ET.parse("VideoCropper/Package.appxmanifest")
version = manifest.find("{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity").attrib["Version"]
output = pathlib.Path(sys.argv[1]).resolve()
payload = output / f"VideoCropper_{version}_x64"
# Native paths avoid MSYS argument conversion when passing compiler definitions.
subprocess.run([
    "ISCC.exe",
    f"-dAppVersion={version}",
    f"-dPayloadDir={payload}",
    f"-dSetupOutput={output}",
    str(pathlib.Path("installer/VideoCropper.iss").resolve()),
], check=True)
setup = output / f"VideoCropper_{version}_x64_Setup.exe"
with setup.open("rb") as stream:
    digest = hashlib.file_digest(stream, "sha256").hexdigest()
checksum = setup.with_suffix(".exe.sha256")
checksum.write_text(f"{digest}  {setup.name}\n", encoding="utf-8")
print(setup)
print(checksum)
PY
