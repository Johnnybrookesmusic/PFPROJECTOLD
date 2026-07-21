import loader


DAT_PATH = "PlFx.dat"


print("========================")
print("HSD MODEL INSPECTOR")
print("========================")

print()
print("Loading:", DAT_PATH)


dat = loader.DatLoader(DAT_PATH)


print()
print("ROOTS")
print("------------------------")

for i, root in enumerate(dat.file.Roots):

    print(
        i,
        str(root.Name),
        type(root.Data)
    )



print()
print("========================")
print("PLAYER OBJECT")
print("========================")


player = dat.file.Roots[0].Data


print("TYPE:")
print(type(player))


print()
print("ATTRIBUTES:")
print("------------------------")

for x in dir(player):

    if not x.startswith("_"):

        print(x)



print()
print("========================")
print("SHADOW MODEL")
print("========================")


try:

    shadow = player.ShadowModel


    print(
        "TYPE:",
        type(shadow)
    )


    print()
    print("ATTRIBUTES:")
    print("------------------------")


    for x in dir(shadow):

        if not x.startswith("_"):

            print(x)



except Exception as e:

    print(
        "No ShadowModel:",
        e
    )



print()
print("========================")
print("CHILD OBJECTS")
print("========================")


def inspect_children(obj, depth=0):

    if obj is None:
        return


    if depth > 3:
        return


    prefix = "  " * depth


    print(
        prefix,
        type(obj)
    )


    for name in dir(obj):

        if name.startswith("_"):
            continue


        try:

            value = getattr(obj,name)


            if value is None:
                continue


            t=str(type(value))


            if (
                "JObj" in t
                or
                "DObj" in t
                or
                "PObj" in t
                or
                "MObj" in t
                or
                "SObj" in t
            ):

                print(
                    prefix,
                    name,
                    "=>",
                    t
                )

                inspect_children(
                    value,
                    depth+1
                )


        except:

            pass



inspect_children(
    getattr(player,"ShadowModel",None)
)


print()
print("========================")
print("DONE")
print("========================")