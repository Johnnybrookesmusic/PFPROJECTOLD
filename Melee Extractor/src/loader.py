import clr
import os
import json
import System


BASE_DIR = os.path.dirname(
    os.path.abspath(__file__)
)


clr.AddReference(
    os.path.join(BASE_DIR,"HSDRaw.dll")
)


from HSDRaw import HSDRawFile



DAT = "PlFx.dat"



print()
print("Loading:")
print(DAT)



file = HSDRawFile(DAT)



print()
print("====================")
print("ROOTS")
print("====================")


for i,root in enumerate(file.Roots):

    print(
        i,
        str(root.Name),
        str(root.GetType())
    )



# choose first player root

root = file.Roots[0]


print()
print("====================")
print("SELECTED ROOT")
print("====================")

print(root.Name)
print(root.GetType())



data = root.Data


print()
print("====================")
print("DATA TYPE")
print("====================")

print(
    data.GetType()
)



print()
print("====================")
print("DATA MEMBERS")
print("====================")



flags = (
    System.Reflection.BindingFlags.Public |
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Instance
)



for p in data.GetType().GetProperties(flags):

    print(
        p.Name
    )



for f in data.GetType().GetFields(flags):

    print(
        f.Name
    )



print()
print("====================")
print("DONE")
print("====================")