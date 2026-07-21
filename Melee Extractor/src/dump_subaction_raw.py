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


struct = table.GetType().GetField("_s").GetValue(table)


refs = struct.References


print("====================")
print("RAW SUBACTION DUMP")
print("====================")

print("COUNT:", refs.Count)


output = []


count = 0


for key in refs.Keys:

    if count >= 10:
        break

    count += 1

    value = refs[key]


    print()
    print("--------------------")
    print("KEY:", key)
    print("SIZE:", value.Length)


    data = value.GetBytes(
        0,
        value.Length
    )


    hexdata = " ".join(
        "{:02X}".format(x)
        for x in data
    )


    print(hexdata)


    output.append(
        {
            "key": int(key),
            "size": int(value.Length),
            "hex": hexdata
        }
    )


with open(
    "subaction_raw_dump.json",
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