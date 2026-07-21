import json
import os

from dat_loader import DatLoader


dat = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(dat)


root = loader.find_root(
    "ftDataFox"
)


if root is None:
    raise Exception(
        "ftDataFox not found"
    )


player = root.Data


print()
print("====================")
print("PLAYER DATA TYPE")
print("====================")

print(
    player.GetType()
)


print()
print("====================")
print("ATTRIBUTES")
print("====================")


attributes = player.Attributes


print(
    attributes.GetType()
)


data = loader.inspect_object(
    attributes,
    0,
    5
)


with open(
    "fox_attributes.json",
    "w"
) as f:

    json.dump(
        data,
        f,
        indent=4,
        default=str
    )


print()
print(
    "Exported fox_attributes.json"
)