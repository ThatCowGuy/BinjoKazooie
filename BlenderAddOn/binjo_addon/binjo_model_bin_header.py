
from . import binjo_utils

class ModelBIN_Header:

    # python class constructor basically also serves as my member declaration...
    def __init__(self, bin_data=None):
        # === 0x00 ========================================================
        self.start_identifier   = 0x0000_000B
        self.geo_offset         = 0
        self.tex_offset         = 0x0038
        self.geo_type           = 0x0002 # tri-linear as default
        self.DL_offset          = 0
        # === 0x10 ========================================================
        self.vtx_offset         = 0
        self.unk_1              = 0
        self.bone_offset        = 0
        self.coll_offset        = 0
        # === 0x20 ========================================================
        self.FX_END             = 0
        self.FX_offset          = 0
        self.unk_2              = 0
        self.anim_tex_offset    = 0
        # === 0x30 ========================================================
        self.tri_cnt            = 0
        self.vtx_cnt            = 0
        self.unk_3              = 0x42C8_0000

        self.valid = True
        if (bin_data == None):
            return        
            
        # parsed properties
        # === 0x00 ========================================================
        self.start_identifier   = binjo_utils.read_bytes(bin_data, 0x00, 4)
        self.geo_offset         = binjo_utils.read_bytes(bin_data, 0x04, 4)
        self.tex_offset         = binjo_utils.read_bytes(bin_data, 0x08, 2)
        self.geo_type           = binjo_utils.read_bytes(bin_data, 0x0A, 2)
        self.DL_offset          = binjo_utils.read_bytes(bin_data, 0x0C, 4)
        # === 0x10 ========================================================
        self.vtx_offset         = binjo_utils.read_bytes(bin_data, 0x10, 4)
        self.unk_1              = binjo_utils.read_bytes(bin_data, 0x14, 4)
        self.bone_offset        = binjo_utils.read_bytes(bin_data, 0x18, 4)
        self.coll_offset        = binjo_utils.read_bytes(bin_data, 0x1C, 4)
        # === 0x20 ========================================================
        self.FX_END             = binjo_utils.read_bytes(bin_data, 0x20, 4)
        self.FX_offset          = binjo_utils.read_bytes(bin_data, 0x24, 4)
        self.unk_2              = binjo_utils.read_bytes(bin_data, 0x28, 4)
        self.anim_tex_offset    = binjo_utils.read_bytes(bin_data, 0x2C, 4)
        # === 0x30 ========================================================
        self.tri_cnt            = binjo_utils.read_bytes(bin_data, 0x30, 2)
        self.vtx_cnt            = binjo_utils.read_bytes(bin_data, 0x32, 2)
        self.unk_3              = binjo_utils.read_bytes(bin_data, 0x34, 4)
        # PARSING COMPLETE
        self.valid = True
        print(self)

    def get_bytes(self):
        output = bytearray()
        # === 0x00 ========================================================
        output += binjo_utils.int_to_bytes(self.start_identifier, 4)
        output += binjo_utils.int_to_bytes(self.geo_offset, 4)
        output += binjo_utils.int_to_bytes(self.tex_offset, 2)
        output += binjo_utils.int_to_bytes(self.geo_type, 2)
        output += binjo_utils.int_to_bytes(self.DL_offset, 4)
        # === 0x10 ========================================================
        output += binjo_utils.int_to_bytes(self.vtx_offset, 4)
        output += binjo_utils.int_to_bytes(self.unk_1, 4)
        output += binjo_utils.int_to_bytes(self.bone_offset, 4)
        output += binjo_utils.int_to_bytes(self.coll_offset, 4)
        # === 0x20 ========================================================
        output += binjo_utils.int_to_bytes(self.FX_END, 4)
        output += binjo_utils.int_to_bytes(self.FX_offset, 4)
        output += binjo_utils.int_to_bytes(self.unk_2, 4)
        output += binjo_utils.int_to_bytes(self.anim_tex_offset, 4)
        # === 0x30 ========================================================
        output += binjo_utils.int_to_bytes(self.tri_cnt, 2)
        output += binjo_utils.int_to_bytes(self.vtx_cnt, 2)
        output += binjo_utils.int_to_bytes(self.unk_3, 4)
        return output
    
    def __str__(self):
        return (
            f'BIN_Header(\n'
            f'    start_identifier  = {binjo_utils.to_decal_hex(self.start_identifier, 4)},\n'
            f'    geo_offset        = {binjo_utils.to_decal_hex(self.geo_offset, 4)},\n'
            f'    tex_offset        = {binjo_utils.to_decal_hex(self.tex_offset, 2)},\n'
            f'    geo_type          = {binjo_utils.to_decal_hex(self.geo_type, 2)},\n'
            f'    DL_offset         = {binjo_utils.to_decal_hex(self.DL_offset, 4)},\n'
            f'    vtx_offset        = {binjo_utils.to_decal_hex(self.vtx_offset, 4)},\n'
            f'    unk_1             = {binjo_utils.to_decal_hex(self.unk_1, 4)},\n'
            f'    bone_offset       = {binjo_utils.to_decal_hex(self.bone_offset, 4)},\n'
            f'    coll_offset       = {binjo_utils.to_decal_hex(self.coll_offset, 4)},\n'
            f'    FX_END            = {binjo_utils.to_decal_hex(self.FX_END, 4)},\n'
            f'    FX_offset         = {binjo_utils.to_decal_hex(self.FX_offset, 4)},\n'
            f'    unk_2             = {binjo_utils.to_decal_hex(self.unk_2, 4)},\n'
            f'    anim_tex_offset   = {binjo_utils.to_decal_hex(self.anim_tex_offset, 4)},\n'
            f'    tri_cnt           = {binjo_utils.to_decal_hex(self.tri_cnt, 2)},\n'
            f'    vtx_cnt           = {binjo_utils.to_decal_hex(self.vtx_cnt, 2)},\n'
            f'    unk_3             = {binjo_utils.to_decal_hex(self.unk_3, 4)}\n'
            f')'
        )