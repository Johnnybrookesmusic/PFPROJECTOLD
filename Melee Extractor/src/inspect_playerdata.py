import clr

clr.AddReference("HSDRaw")

from HSDRaw.Melee.Pl import SBM_PlayerData

print("====================")
print("SBM_PlayerData")
print("====================")

for x in dir(SBM_PlayerData):
    if not x.startswith("_"):
        print(x)