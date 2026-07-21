from dat_loader import DatLoader
import os


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)


print("====================")
print("DATALOADER")
print("====================")

print(dir(loader))