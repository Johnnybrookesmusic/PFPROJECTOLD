import json


with open(
    "fox_action_raw.json",
    "r"
) as f:
    actions = json.load(f)


for action in actions[:10]:

    print("\n====================")
    print(action["id"])
    print(action["name"])
    print("SIZE:", action["size"])
    print("====================")


    data = action["bytes"]


    for i in range(0, len(data), 4):

        chunk = data[i:i+4]

        print(
            f"{i:02X}:",
            " ".join(
                f"{x:02X}"
                for x in chunk
            )
        )