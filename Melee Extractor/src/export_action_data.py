import os
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


struct = table.GetType().GetField(
    "_s"
).GetValue(
    table
)


refs = struct.References


actions = []


print("====================")
print("EXPORTING ACTION STRUCT DATA")
print("====================")


for key in refs.Keys:

    obj = refs[key]

    size = obj.Length


    raw = bytes(
        obj.GetBytes(
            0,
            size
        )
    )


    name_bytes = raw.split(
        b"\x00",
        1
    )[0]


    try:
        name = name_bytes.decode("ascii")
    except:
        name = ""


    if (
        name.startswith("Ply")
        and "ACTION" in name
    ):

        print(
            key,
            name,
            "SIZE:",
            size
        )


        # first 4 bytes after the name area
        # saved for decoding later

        actions.append(
            {
                "offset": key,
                "name": name,
                "size": size,
                "raw": list(raw)
            }
        )


with open(
    "fox_action_raw.json",
    "w"
) as f:

    json.dump(
        actions,
        f,
        indent=4
    )


print()
print("====================")
print("TOTAL:")
print(len(actions))
print("====================")