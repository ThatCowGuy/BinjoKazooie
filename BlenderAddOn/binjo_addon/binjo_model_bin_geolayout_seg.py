
from . import binjo_utils
from . binjo_dicts import Dicts


class ModelBIN_GeoCommand:
    def __init__(self):
        pass

class ModelBIN_GeoCommandChain:
    def __init__(self):
        pass

    def build_default(self, min_x, min_y, min_z, max_x, max_y, max_z):
        self.entries = []

        self.entries.append(Dicts.GEO_CMD_NAMES["DRAW_DISTANCE"])
        self.entries.append(0x00000028) # full length of the chain (10 entries à 4B = 40 B = 0x28 B)
        self.entries.append((binjo_utils.get_2s_complement(min_x, 2) << 16) + binjo_utils.get_2s_complement(min_y, 2))
        self.entries.append((binjo_utils.get_2s_complement(min_z, 2) << 16) + binjo_utils.get_2s_complement(max_x, 2))
        self.entries.append((binjo_utils.get_2s_complement(max_y, 2) << 16) + binjo_utils.get_2s_complement(max_z, 2))
        self.entries.append(0x001808D3)

        self.entries.append(Dicts.GEO_CMD_NAMES["LOAD_DL"])
        self.entries.append(0x00000000) # 0x00 == final command of the chain
        self.entries.append(0x00000000) # this contains the offset
        self.entries.append(0x00000000) # just padding ?

    def get_bytes(self):
        output = bytearray()
        for entry in self.entries:
            output += binjo_utils.int_to_bytes(entry, 4)
        return output



class ModelBIN_GeoSeg:

    # python class constructor basically also serves as my member declaration...
    def __init__(self):
        self.valid = False

    def build_from_minmax(self, min_x, min_y, min_z, max_x, max_y, max_z):
        self.command_chains = []

        chain = ModelBIN_GeoCommandChain()
        chain.build_default(
            min_x=min_x, min_y=min_y, min_z=min_z,
            max_x=max_x, max_y=max_y, max_z=max_z
        )
        self.command_chains.append(chain)

        self.valid = True

    def get_bytes(self):
        output = bytearray()
        for chain in self.command_chains:
            output += chain.get_bytes()
        return output