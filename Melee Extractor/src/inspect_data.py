from dat_loader import DatLoader

print("""
Loading:
C:\\Users\\Johnn\\Downloads\\Melee Extractor\\src\\PlFx.dat
""")

path = r"C:\Users\Johnn\Downloads\Melee Extractor\src\PlFx.dat"

loader = DatLoader(path)

root = loader.find_root()

data = root.Data

print("====================")
print("PLAYER DATA")
print("====================")
print(type(data))

print("====================")
print("SUBACTION TABLE")
print("====================")

table = data.SubActionTable

print("TYPE:")
print(type(table))

print("====================")
print("DIR:")
print("====================")

print(dir(table))

print("====================")
print("VALUE:")
print("====================")

print(table)

print("====================")
print("TRIMMED SIZE:")
print("====================")

try:
    print(table.TrimmedSize)
except Exception as e:
    print("ERROR:", e)

print("====================")
print("END")
print("====================")