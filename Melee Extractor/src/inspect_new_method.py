import clr

clr.AddReference("HSDRaw")

import HSDRaw


cls = clr.GetClrType(
    HSDRaw.Melee.Pl.SBM_FighterSubAction
)


print("====================")
print("SBM_FighterSubAction.New")
print("====================")


for method in cls.GetMethods():

    if str(method.Name) == "New":

        print()
        print(method)

        print()

        for parameter in method.GetParameters():

            print(
                parameter.Name,
                ":",
                parameter.ParameterType
            )