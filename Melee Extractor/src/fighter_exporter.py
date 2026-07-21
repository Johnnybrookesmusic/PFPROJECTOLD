class FighterExporter:

    def __init__(self, loader):
        self.loader = loader


    def find_player_data(self):

        for root in self.loader.roots:
            if "SBM_PlayerData" in root.Data.GetType().Name:
                return root.Data

        return None


    def export(self):

        fighter = self.find_player_data()

        struct = fighter._s

        output = {
            "fighter": fighter.GetType().Name,
            "references": []
        }


        for key in struct.References.Keys:

            child = struct.References[key]

            entry = {
                "offset": int(key),
                "length": child.Length,
                "floats": [],
                "ints": []
            }


            # scan first 20 float values
            for i in range(0, min(child.Length // 4, 30)):

                try:
                    entry["floats"].append(
                        child.GetFloat(i * 4)
                    )

                except:
                    pass


            # scan first 20 ints
            for i in range(0, min(child.Length // 4, 30)):

                try:
                    entry["ints"].append(
                        child.GetInt32(i * 4)
                    )

                except:
                    pass


            output["references"].append(entry)


        return output