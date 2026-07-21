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


count = 0


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
        print(raw.split(b"\x00")[0].decode())
        print("OFFSET:", key)
        print("SIZE:", obj.Length)


        print()
        print("REFERENCES COUNT:")

        try:
            print(obj.References.Count)
        except:
            print("NO COUNT")


        try:
            for r in obj.References.Keys:
                print(
                    "REF:",
                    r,
                    obj.References[r].GetType()
                )

        except Exception as e:
            print(
                "REF ERROR:",
                e
            )


        print()
        print("RAW:")
        print(raw.hex(" "))


        count += 1

        if count == 5:
            break