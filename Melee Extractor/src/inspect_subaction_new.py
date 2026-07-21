import os
import struct
import json

from dat_loader import DatLoader


DAT_FILE = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)


loader = DatLoader(DAT_FILE)

root = loader.find_root(
    "ftDataFox"
)


table = root.Data.SubActionTable


field = table.GetType().GetField(
    "_s"
)


accessor_struct = field.GetValue(
    table
)


refs = accessor_struct.References


print("====================")
print("CUSTOM SUBACTION DECODER")
print("====================")

print(
    "TOTAL:",
    refs.Count
)


output = {}


for key in refs.Keys:

    struct_obj = refs[key]

    length = struct_obj.Length


    raw = bytes(
        struct_obj.GetBytes(
            0,
            length
        )
    )


    print()
    print("--------------------")
    print(
        "KEY:",
        key
    )

    print(
        "SIZE:",
        length
    )


    # extract ascii name
    name_bytes = raw.split(
        b"\x00",
        1
    )[0]


    try:
        name = name_bytes.decode(
            "ascii"
        )
    except:
        name = ""


    print(
        "NAME:",
        name
    )


    entry = {
        "name": name,
        "size": length,
        "raw": raw.hex(" ")
    }


    # try to decode trailing ints
    if length >= 12:

        try:

            values = []

            for i in range(
                0,
                length,
                4
            ):

                values.append(
                    struct.unpack(
                        ">I",
                        raw[i:i+4]
                    )[0]
                )


            entry["uint32"] = values


        except:
            pass


    output[str(key)] = entry



with open(
    "fox_subactions_decoded.json",
    "w"
) as f:

    json.dump(
        output,
        f,
        indent=4
    )


print()
print("====================")
print("DONE")
print("====================")