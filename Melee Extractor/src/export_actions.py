import json
import clr
import os

print("""
====================
ACTION DATA EXPORT
====================
""")

base = os.path.dirname(__file__)

clr.AddReference(
    os.path.join(base, "HSDRaw.dll")
)

from HSDRaw.Melee.Pl import *
from HSDRaw import *

path = os.path.join(base, "PlFx.dat")

print("Loading:")
print(path)

root = HSDRawFile.Read(path)


# Find all SBM_FighterSubAction objects
actions = []

def scan(obj):

    if obj is None:
        return

    try:
        name = obj.Name

        if name and "ACTION" in str(name):
            actions.append({
                "name": str(name),
                "animation_offset": obj.AnimationOffset,
                "animation_size": obj.AnimationSize,
                "flags": obj.Flags
            })

    except:
        pass


# walk file objects
for obj in root.Roots:
    try:
        scan(obj)
    except:
        pass


print()
print("====================")
print("FOUND")
print(len(actions))
print("ACTIONS")
print("====================")


with open(
    "fox_actions_full.json",
    "w",
    encoding="utf8"
) as f:

    json.dump(
        actions,
        f,
        indent=4
    )


print()
print("WROTE fox_actions_full.json")