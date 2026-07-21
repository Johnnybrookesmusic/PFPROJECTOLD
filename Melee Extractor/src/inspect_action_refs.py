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

    name = raw.split(
        b"\x00",
        1
    )[0].decode(
        "ascii",
        errors="ignore"
    )


    if "ACTION" in name:

        print("====================")
        print("ACTION:", name)
        print("OFFSET:", key)
        print("SIZE:", obj.Length)

        print("\nREFERENCES:")

        try:

            subrefs = obj.References

            for r in subrefs.Keys:
                print(
                    " ->",
                    r
                )

        except Exception as e:
            print(
                "No refs:",
                e
            )


        print("\nSUB STRUCTS:")

        try:

            subs = obj.GetSubStructs()

            for s in subs:
                print(
                    "SUB:",
                    s.Length
                )

        except Exception as e:
            print(
                "No subs:",
                e
            )


        count += 1


        if count >= 10:
            break