from dat_loader import DatLoader

path = r"C:\Users\Johnn\Downloads\Melee Extractor\src\PlFXAJ.dat"

loader = DatLoader(path)

root = loader.file.Roots[0]

figa = root.Data

print("====================")
print("FIGATREE")
print("====================")

print(type(figa))

print("====================")
print("DIR")
print("====================")

print(dir(figa))

print("====================")
print("FIELDS")
print("====================")

for name in dir(figa):
    if not name.startswith("_"):
        try:
            value = getattr(figa, name)
            print(name, "=", value)
        except:
            pass