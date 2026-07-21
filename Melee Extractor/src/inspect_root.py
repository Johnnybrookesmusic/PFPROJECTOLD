from dat_loader import DatLoader
import os


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)

root = loader.file.Roots[0]


print("====================")
print("ROOT TYPE")
print("====================")

print(type(root))


print()
print("====================")
print("ROOT DIR")
print("====================")

print(dir(root))