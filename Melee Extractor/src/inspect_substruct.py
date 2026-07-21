import os

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


for key in refs.Keys:

    obj = refs[key]

    raw = bytes(
        obj.GetBytes(
            0,
            obj.Length
        )
    )


    if raw.startswith(b"PlyFox5K_Share_ACTION"):

        print("====================")
        print("ACTION")
        print(raw.split(b"\x00")[0].decode())
        print("OFFSET:", key)
        print("SIZE:", obj.Length)


        subs = obj.GetSubStructs()

        print("SUB COUNT:", len(subs))


        for s in subs:

            print(
                "SUB TYPE:",
                s.GetType()
            )

            print(
                "SUB SIZE:",
                s.Length
            )


            data = bytes(
                s.GetBytes(
                    0,
                    s.Length
                )
            )

            print(
                data.hex(" ")
            )

        break