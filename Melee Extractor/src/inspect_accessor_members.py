import os

from dat_loader import DatLoader


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)

root = loader.find_root(
    "ftDataFox"
)


table = root.Data.SubActionTable


print("====================")
print("SUBACTION TABLE")
print("====================")

print(
    table.GetType()
)


print()
print("====================")
print("MEMBERS")
print("====================")


for prop in table.GetType().GetProperties():

    print(
        "PROPERTY:",
        prop.Name,
        ":",
        prop.PropertyType
    )


for method in table.GetType().GetMethods():

    print(
        "METHOD:",
        method
    )