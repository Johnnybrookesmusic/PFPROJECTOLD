import clr
import os
import System

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
clr.AddReference(os.path.join(BASE_DIR, "HSDRaw.dll"))
from HSDRaw import HSDRawFile

flags = (
    System.Reflection.BindingFlags.Public |
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Instance |
    System.Reflection.BindingFlags.Static
)

for DAT in ["PlFx.dat", "PlFc.dat", "PlCo.dat", "PlFxAJ.dat"]:
    if not os.path.exists(DAT):
        print("MISSING", DAT)
        continue

    print()
    print("====================")
    print(DAT)
    print("====================")

    f = HSDRawFile(DAT)

    print("-- Roots --")
    for i, root in enumerate(f.Roots):
        print(f"  [{i}] name={root.Name!r}  type={root.Data.GetType().FullName if root.Data is not None else None}")

    # Some HSD files also have "reference nodes" separate from data roots
    # (external symbol references) - check if HSDRawFile exposes any.
    rn_prop = f.GetType().GetProperty("References", flags)
    if rn_prop is not None:
        try:
            refnodes = rn_prop.GetValue(f, None)
            print("-- Reference nodes --")
            for i, rn in enumerate(refnodes):
                print(f"  [{i}] name={rn.Name!r}  type={rn.Data.GetType().FullName if rn.Data is not None else None}")
        except Exception as e:
            print("References property errored:", e)

print()
print("====================")
print("DONE")
print("====================")