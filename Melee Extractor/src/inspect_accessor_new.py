import clr

clr.AddReference("HSDRaw")

import HSDRaw


print("====================")
print("HSDAccessOR.New")
print("====================")


cls = clr.GetClrType(
    HSDRaw.HSDAccessor
)


for method in cls.GetMethods():

    if str(method.Name) == "New":

        print()
        print(method)

        print()

        for param in method.GetParameters():

            print(
                param.Name,
                ":",
                param.ParameterType
            )