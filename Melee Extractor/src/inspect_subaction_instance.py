import os
import clr

from dat_loader import DatLoader


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)

root = loader.find_root(
    "ftDataFox"
)

if root is None:
    raise Exception("Fox not found")


player = root.Data


print("====================")
print("SUBACTION TABLE")
print("====================")


table = player.SubActionTable


if table is None:
    print("NONE")
    exit()


print("TYPE:")
print(table.GetType())


print()
print("MEMBERS")
print("====================")

for m in table.GetType().GetMembers():
    print(m.Name)