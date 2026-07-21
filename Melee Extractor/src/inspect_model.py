import loader


DAT_PATH = "PlFx.dat"


dat = loader.DatLoader(DAT_PATH)

player = dat.file.Roots[0].Data

root = player.ShadowModel


print("========================")
print("JOBJ TREE")
print("========================")


def walk_jobj(jobj, depth=0):

    if jobj is None:
        return

    indent = "  " * depth

    print()
    print(indent + "JOBJ")
    print(indent, "TX:", jobj.TX)
    print(indent, "TY:", jobj.TY)
    print(indent, "TZ:", jobj.TZ)

    print(indent, "DOBJ:", type(jobj.Dobj))


    if jobj.Dobj:

        dobj=jobj.Dobj

        while dobj:

            print()
            print(indent+"  DOBJ")
            print(indent+"  ",type(dobj))

            print(indent+"  Attributes:")

            for x in dir(dobj):

                if not x.startswith("_"):

                    print(indent+"   ",x)


            try:

                print(
                    indent+"  POBJ:",
                    type(dobj.Pobj)
                )

            except:
                pass


            dobj=dobj.Next


    if jobj.Child:
        walk_jobj(
            jobj.Child,
            depth+1
        )


    if jobj.Next:
        walk_jobj(
            jobj.Next,
            depth
        )



walk_jobj(root)


print()
print("========================")
print("DONE")
print("========================")