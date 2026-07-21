import os
import clr
import json


BASE_DIR = os.path.dirname(
    os.path.abspath(__file__)
)


DLL = os.path.join(
    BASE_DIR,
    "HSDRaw.dll"
)


clr.AddReference(DLL)


from HSDRaw import HSDRawFile



def read_struct_bytes(struct_obj):

    size = struct_obj.Length


    output = []


    for i in range(size):

        output.append(
            int(
                struct_obj.GetByte(i)
            )
        )


    return output



def main():


    dat=os.path.join(
        BASE_DIR,
        "PlFx.dat"
    )


    print()
    print("Loading:")
    print(dat)


    file=HSDRawFile(dat)


    root=None


    for r in file.Roots:

        if str(r.Name)=="ftDataFox":

            root=r
            break



    print(
        "Found",
        root.Name
    )


    accessor=root.Data.Attributes2


    struct_obj=accessor._s


    print()
    print("======================")
    print("HSD STRUCT")
    print("======================")


    print(
        "Length:",
        struct_obj.Length
    )


    raw=read_struct_bytes(
        struct_obj
    )


    output={

        "length":len(raw),

        "hex":
            " ".join(
                f"{x:02X}"
                for x in raw
            ),

        "bytes":raw
    }



    with open(
        "attributes2_bytes.json",
        "w"
    ) as f:

        json.dump(
            output,
            f,
            indent=4
        )


    print()
    print(
        "Exported attributes2_bytes.json"
    )



if __name__=="__main__":

    main()