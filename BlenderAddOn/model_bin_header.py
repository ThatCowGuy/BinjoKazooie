
import BinjoUtils

class ModelBIN_Header:

    # python class constructor basically also serves as my member declaration...
    def __init__(self, bin_data=None):
        if (bin_data == None):
            print("initializing empty ModelBIN_Header OBJ...")
            self.valid = False
            return        
        # parsed properties
        # === 0x00 ===============================
        self.start_identifier = BinjoUtils.read_bytes(bin_data, 0x00, 4)
        self.geo_offset = BinjoUtils.read_bytes(bin_data, 0x04, 4)
        self.tex_offset = BinjoUtils.read_bytes(bin_data, 0x08, 2)
        self.geo_type = BinjoUtils.read_bytes(bin_data, 0x0A, 2)
        self.DL_offset = BinjoUtils.read_bytes(bin_data, 0x0C, 4)
        # === 0x10 ===============================
        self.vtx_offset = BinjoUtils.read_bytes(bin_data, 0x10, 4)
        self.unk_1 = BinjoUtils.read_bytes(bin_data, 0x14, 4)
        self.bone_offset = BinjoUtils.read_bytes(bin_data, 0x18, 4)
        self.coll_offset = BinjoUtils.read_bytes(bin_data, 0x1C, 4)
        # === 0x20 ===============================
        self.FX_END = BinjoUtils.read_bytes(bin_data, 0x20, 4)
        self.FX_offset = BinjoUtils.read_bytes(bin_data, 0x24, 4)
        self.unk_2 = BinjoUtils.read_bytes(bin_data, 0x28, 4)
        self.anim_tex_offset = BinjoUtils.read_bytes(bin_data, 0x2C, 4)
        # === 0x30 ===============================
        self.tri_cnt = BinjoUtils.read_bytes(bin_data, 0x30, 2)
        self.vtx_cnt = BinjoUtils.read_bytes(bin_data, 0x32, 2)
        self.unk_3 = BinjoUtils.read_bytes(bin_data, 0x34, 4)
        # PARSING COMPLETE
        self.valid = True
        print(self)
    
    def __str__(self):
        return (
            f'BIN_Header(\n'
            f'    valid             = {self.valid},\n'
            f'    start_identifier  = {BinjoUtils.to_decal_hex(self.start_identifier, 4)},\n'
            f'    geo_offset        = {BinjoUtils.to_decal_hex(self.geo_offset, 4)},\n'
            f'    tex_offset        = {BinjoUtils.to_decal_hex(self.tex_offset, 2)},\n'
            f'    geo_type          = {BinjoUtils.to_decal_hex(self.geo_type, 2)},\n'
            f'    DL_offset         = {BinjoUtils.to_decal_hex(self.DL_offset, 4)},\n'
            f'    vtx_offset        = {BinjoUtils.to_decal_hex(self.vtx_offset, 4)},\n'
            f'    unk_1             = {BinjoUtils.to_decal_hex(self.unk_1, 4)},\n'
            f'    bone_offset       = {BinjoUtils.to_decal_hex(self.bone_offset, 4)},\n'
            f'    coll_offset       = {BinjoUtils.to_decal_hex(self.coll_offset, 4)},\n'
            f'    FX_END            = {BinjoUtils.to_decal_hex(self.FX_END, 4)},\n'
            f'    FX_offset         = {BinjoUtils.to_decal_hex(self.FX_offset, 4)},\n'
            f'    unk_2             = {BinjoUtils.to_decal_hex(self.unk_2, 4)},\n'
            f'    anim_tex_offset   = {BinjoUtils.to_decal_hex(self.anim_tex_offset, 4)},\n'
            f'    tri_cnt           = {BinjoUtils.to_decal_hex(self.tri_cnt, 2)},\n'
            f'    vtx_cnt           = {BinjoUtils.to_decal_hex(self.vtx_cnt, 2)},\n'
            f'    unk_3             = {BinjoUtils.to_decal_hex(self.unk_3, 4)}\n'
            f')'
        )