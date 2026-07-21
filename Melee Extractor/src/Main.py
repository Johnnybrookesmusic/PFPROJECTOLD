from pathlib import Path
from hashlib import sha256
import orjson
from rich.console import Console
from rich.table import Table

console = Console()

# ===========================================
# CHANGE THIS ONLY IF YOU MOVE YOUR FOLDER
# ===========================================
MELEE_ROOT = Path(r"C:\Users\Johnn\Downloads\Melee Extractor\src\Input\Melee Deconstructed")

OUTPUT_DIR = Path("output")
OUTPUT_DIR.mkdir(exist_ok=True)

TARGET_FILES = [
    "PlCo.dat",
    "PlFx.dat",
    "PlFxAJ.dat",
    "PlFc.dat",
    "PlFcAJ.dat",
]

results = []

console.print("[bold green]Searching for target files...[/bold green]\n")

for target in TARGET_FILES:

    found = None

    for file in MELEE_ROOT.rglob(target):
        found = file
        break

    if found is None:
        console.print(f"[red]Missing:[/red] {target}")
        continue

    data = found.read_bytes()

    info = {
        "name": target,
        "path": str(found.relative_to(MELEE_ROOT)),
        "size": len(data),
        "sha256": sha256(data).hexdigest(),
        "header_hex": data[:64].hex(" "),
        "footer_hex": data[-64:].hex(" "),
    }

    results.append(info)

table = Table(title="Target Files")

table.add_column("File")
table.add_column("Size")
table.add_column("SHA256 (first 16)")
table.add_column("Status")

for item in results:
    table.add_row(
        item["name"],
        f'{item["size"]:,}',
        item["sha256"][:16],
        "FOUND"
    )

console.print(table)

report = {
    "target_count": len(results),
    "files": results
}

with open(OUTPUT_DIR / "target_report.json", "wb") as f:
    f.write(orjson.dumps(report, option=orjson.OPT_INDENT_2))

console.print("\n[green]target_report.json written successfully.[/green]")