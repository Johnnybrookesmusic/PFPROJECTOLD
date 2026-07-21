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


for key in refs.Keys:

    obj = refs[key]

    size = obj.Length

    raw = bytes(
        obj.GetBytes(
            0,
            size
        )
    )


    name = raw.split(
        b"\x00",
        1
    )[0].decode(
        "ascii",
        errors="ignore"
    )


    if (
        name.startswith("PlyFox")
        and "ACTION" in name
    ):

        actions.append(
            {
                "id": key // 24,
                "offset": key,
                "name": name,
                "size": size,
                "bytes": list(raw)
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


print(
    "Exported",
    len(actions),
    "actions"
)