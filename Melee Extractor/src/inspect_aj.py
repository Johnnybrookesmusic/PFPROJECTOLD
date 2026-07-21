from dat_loader import DatLoader

path = r"C:\Users\Johnn\Downloads\Melee Extractor\src\PlFXAJ.dat"

loader = DatLoader(path)

print("====================")
print("FILE")
print("====================")

print(loader.file)

print("====================")
print("ROOT COUNT")
print("====================")

print(loader.file.Roots.Count)

for r in loader.file.Roots:
    print("====================")
    print("ROOT NAME:")
    print(r.Name)
    print("TYPE:")
    print(type(r.Data))