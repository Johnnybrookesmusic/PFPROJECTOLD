import clr
import sys

clr.AddReference("HSDRaw")
clr.AddReference("HSDRaw.Melee")

from HSDRaw import *
from HSDRaw.Melee import *

path = "PlFx.dat"

print("Loading:")
print(path)

# USE THE SAME LOADER THAT export_physics.py USES
root = MeleeDataLoader.Load(path)

print("====================")
print("ROOT TYPE")
print("====================")
print(type(root))

print()

print("====================")
print("ROOT MEMBERS")
print("====================")

for x in dir(root):
    if not x.startswith("_"):
        try:
            value = getattr(root,x)
            print(x,"=",type(value))
        except:
            pass