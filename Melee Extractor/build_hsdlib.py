import subprocess
import pathlib

ROOT = pathlib.Path(__file__).parent.parent

subprocess.run(
    [
        "dotnet",
        "build",
        str(ROOT / "HSDLib.sln"),
        "-c",
        "Release"
    ],
    check=True
)

print("Finished.")