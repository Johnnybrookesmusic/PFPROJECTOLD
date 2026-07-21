import json
import sys

from dat_loader import DatLoader
from fighter_exporter import FighterExporter

loader = DatLoader(sys.argv[1])

exporter = FighterExporter(loader)

data = exporter.export()

with open("output.json", "w") as f:

    json.dump(data, f, indent=4)

print("Done.")