import os

from dat_loader import DatLoader



dat = os.path.join(
    os.path.dirname(__file__),
    "PlFx.dat"
)



loader = DatLoader(dat)



loader.export_player_data(
    "ftDataFox"
)