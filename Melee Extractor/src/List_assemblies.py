import os

print("====================")
print("DLL FILES")
print("====================")

for f in os.listdir("."):
    if f.lower().endswith(".dll"):
        print(f)