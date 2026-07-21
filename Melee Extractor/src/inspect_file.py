from dat_loader import DatLoader
import os


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)

file = loader.file


print("====================")
print("ROOTS")
print("====================")


for r in file.Roots:

    print(r)


print()
print("====================")
print("ROOT COUNT")
print("====================")

print(
    file.Roots.Count
)


print()
print("====================")
print("REFERENCES")
print("====================")


refs = file.References

print(type(refs))

print(
    "DIR:"
)

print(
    dir(refs)
)


print()

try:
    print(
        "COUNT:",
        refs.Count
    )
except Exception as e:
    print(e)