import json
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


if root is None:
    raise Exception(
        "Fox data not found"
    )


attributes = root.Data.Attributes


output = {}


for prop in attributes.GetType().GetProperties():

    try:

        name = str(prop.Name)

        value = prop.GetValue(
            attributes,
            None
        )

        if isinstance(
            value,
            (
                int,
                float,
                str,
                bool
            )
        ):
            output[name] = value

    except:
        pass



with open(
    "fox_physics.json",
    "w"
) as f:

    json.dump(
        output,
        f,
        indent=4
    )


print(
    "Exported fox_physics.json"
)