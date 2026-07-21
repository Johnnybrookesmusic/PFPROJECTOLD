import clr

clr.AddReference("HSDRaw")

from HSDRaw.Melee import *

path="PlFx.dat"

print("Loading:")
print(path)

root = HSDRaw.Melee.MeleeDataLoader.Load(path)

fox = root.FighterData["Fox"]

print("====================")
print(type(fox))
print("====================")

for x in dir(fox):
    if not x.startswith("_"):
        try:
            value=getattr(fox,x)
            print(x,"=",type(value))
        except:
            pass