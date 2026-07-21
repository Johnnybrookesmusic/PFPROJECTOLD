import clr

clr.AddReference("HSDRaw")

import HSDRaw


print("====================")
print("SBM_FighterSubAction")
print("====================")


cls = HSDRaw.Melee.Pl.SBM_FighterSubAction


print()
print("PYTHON TYPE:")
print(cls)


print()
print("====================")
print("CLR TYPE")
print("====================")


clr_type = clr.GetClrType(cls)


print(
    clr_type
)


print()
print("====================")
print("FIELDS")
print("====================")


for field in clr_type.GetFields():

    print(
        field.Name,
        ":",
        field.FieldType
    )


print()
print("====================")
print("PROPERTIES")
print("====================")


for prop in clr_type.GetProperties():

    print(
        prop.Name,
        ":",
        prop.PropertyType
    )


print()
print("====================")
print("METHODS")
print("====================")


for method in clr_type.GetMethods():

    print(
        method.Name
    )