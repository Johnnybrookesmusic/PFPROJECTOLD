import json
import struct


with open("attributes2_bytes.json") as f:
    data=json.load(f)["bytes"]


def f32(offset):
    return struct.unpack(
        ">f",
        bytes(data[offset:offset+4])
    )[0]


def u32(offset):
    return int.from_bytes(
        bytes(data[offset:offset+4]),
        "big"
    )


output={}


for offset in range(0,len(data),4):

    value=u32(offset)
    fl=f32(offset)


    output[f"0x{offset:03X}"]={

        "uint32":value,

        "float":fl,

        "hex":
        " ".join(
            f"{x:02X}"
            for x in data[offset:offset+4]
        )
    }


with open(
    "attributes2_mixed.json",
    "w"
) as f:

    json.dump(
        output,
        f,
        indent=4
    )


print(
    "Exported attributes2_mixed.json"
)