import json
import struct


INPUT = "attributes2_bytes.json"


# Known Attributes2 structure
# Names will be filled as we identify them
ATTRIBUTES2 = {

    0x00: "Unknown0x00",
    0x04: "Unknown0x04",
    0x08: "Unknown0x08",
    0x0C: "Unknown0x0C",

    0x10: "Unknown0x10",
    0x14: "Unknown0x14",
    0x18: "Unknown0x18",
    0x1C: "Unknown0x1C",

    0x20: "Unknown0x20",
    0x24: "Unknown0x24",
    0x28: "Unknown0x28",
    0x2C: "Unknown0x2C",

    0x30: "Unknown0x30",
    0x34: "Unknown0x34",
    0x38: "Unknown0x38",
    0x3C: "Unknown0x3C",

    0x40: "Unknown0x40",
    0x44: "Unknown0x44",
    0x48: "Unknown0x48",
    0x4C: "Unknown0x4C",

    0x50: "Unknown0x50",
    0x54: "Unknown0x54",
    0x58: "Unknown0x58",
    0x5C: "Unknown0x5C",

    0x60: "Unknown0x60",
    0x64: "Unknown0x64",
    0x68: "Unknown0x68",
    0x6C: "Unknown0x6C",

    0x70: "Unknown0x70",
    0x74: "Unknown0x74",
    0x78: "Unknown0x78",
    0x7C: "Unknown0x7C",

    0x80: "Unknown0x80",
    0x84: "Unknown0x84",
    0x88: "Unknown0x88",
    0x8C: "Unknown0x8C",

    0x90: "Unknown0x90",
    0x94: "Unknown0x94",
    0x98: "Unknown0x98",
    0x9C: "Unknown0x9C",

    0xA0: "Unknown0xA0",
    0xA4: "Unknown0xA4",
    0xA8: "Unknown0xA8",
    0xAC: "Unknown0xAC",

    0xB0: "Unknown0xB0",
    0xB4: "Unknown0xB4",
    0xB8: "Unknown0xB8",
    0xBC: "Unknown0xBC",

    0xC0: "Unknown0xC0",
    0xC4: "Unknown0xC4",
    0xC8: "Unknown0xC8",
    0xCC: "Unknown0xCC",

    0xD0: "Unknown0xD0",
}



def main():

    with open(INPUT) as f:
        data=json.load(f)["bytes"]


    output={}


    for offset,name in ATTRIBUTES2.items():

        if offset+4 > len(data):
            break

        raw=data[offset:offset+4]

        value=struct.unpack(">f",bytes(raw))[0]


        output[name]={
            "offset":hex(offset),
            "float":value,
            "hex":" ".join(
                f"{x:02X}" for x in raw
            )
        }


    with open(
        "fox_attributes2_decoded.json",
        "w"
    ) as f:

        json.dump(
            output,
            f,
            indent=4
        )


    print(
        "Exported fox_attributes2_decoded.json"
    )



if __name__=="__main__":
    main()