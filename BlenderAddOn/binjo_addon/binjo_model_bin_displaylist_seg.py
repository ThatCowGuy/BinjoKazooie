
import numpy as np

from . import binjo_utils
from . binjo_dicts import Dicts

class DisplayList_Command:

    OtherMode_SET = 0x00008000
    OtherMode_CLEAR = 0x00000000

    def __init__(self, upper=0x00, lower=0x00, full=0x00):
        if (full != 0x00):
            self.full = full
            self.upper = ((full >> 32) & 0xFFFFFFFF)
            self.lower = ((full >>  0) & 0xFFFFFFFF)
        else:
            self.full = ((upper << 32) + lower & 0xFFFFFFFF)
            self.upper = upper
            self.lower = lower
        self.command_byte = (upper >> 24)
        self.command_name = Dicts.F3DEX_CMD_NAMES_REV[self.command_byte]
        # print(self.command_name)
        self.infer_parameters()

    unimplemented_commands = []
    def infer_parameters(self):
        self.parameters = [0] * 16

        if self.command_name == "G_SETCOMBINE":
            if self.command_name not in DisplayList_Command.unimplemented_commands:
                print(f"Unimplemented (and unhandled) Command encountered: {self.command_name}")
                DisplayList_Command.unimplemented_commands.append(self.command_name)
            return

        if self.command_name == "G_LOADBLOCK":
            # UL corner S coord
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1111_1111_1111_0000_0000_0000)
            # UL corner T coord
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_1111_1111_1111)
            # tile descriptor
            self.parameters[2] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # Texel count - 1
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_0000_0000_0000) + 1
            # DXT (this is a really messy one):
            # "dxt is an unsigned fixed-point 1.11 [11 digit mantissa] number"
            # "dxt is the RECIPROCAL of the number of 64-bit chunks it takes to get a row of texture"
            # an example: Take a 32x32 px Tex with 16b colors;
            # -> a row of that Tex takes 32x16b = 512b
            # -> so it needs (512b/64b) = 8 chunks of 64b to create a row
            # -> the reciprocal is 1/8, which in binary is 0.001_0000_0000 = 0x100
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_1111_1111_1111)
            return
            
        if self.command_name == "G_SETTILESIZE":
            # UL corner S coord
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1111_1111_1111_0000_0000_0000)
            # UL corner T coord
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_1111_1111_1111)
            # tile descriptor
            self.parameters[2] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # (Width - 1) * 4
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_0000_0000_0000) // 4 + 1
            # (Height - 1) * 4
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_1111_1111_1111) // 4 + 1

        if self.command_name == "G_SetOtherMode_H":
            # shift
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_1111_1111_0000_0000)
            # num affected bits
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0000_1111_1111)
            # new mode-bits
            self.parameters[2] = self.lower
            return

        if self.command_name == "G_SETGEOMETRYMODE":
            # RSP flags to enable
            self.parameters[0] = self.lower
            return

        if self.command_name == "G_CLEARGEOMETRYMODE":
            # RSP flags to disable
            self.parameters[0] = self.lower
            return

        if self.command_name == "G_TEXTURE":
            # maximum number of mipmaps
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0011_1000_0000_0000)
            # affected tile descriptor index
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0111_0000_0000)
            # enable tile descriptor
            self.parameters[2] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0000_1111_1111)
            # S Axis scale factor (horizontal)
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_1111_1111_0000_0000_0000_0000)
            # T Axis scale factor (vertical)
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_1111_1111_1111_1111)
            return

        if self.command_name == "G_SETTIMG":
            # color storage format
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1110_0000_0000_0000_0000_0000)
            # color storage bit-size
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0001_1000_0000_0000_0000_0000)
            # data segment num
            self.parameters[2] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # data offset (removing the leading byte, because that's caught in the param before)
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_1111_1111_1111)
            return

        if self.command_name == "G_SETTILE":
            # color storage format
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1110_0000_0000_0000_0000_0000)
            # color storage bit-size
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0001_1000_0000_0000_0000_0000)
            # num of 64bit vals per row
            self.parameters[2] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0011_1111_1110_0000_0000)
            # TMEM offset of texture
            self.parameters[3] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0001_1111_1111)
            # target tile descriptor
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0111_0000_0000_0000_0000_0000_0000)
            # corresponding palette
            self.parameters[5] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_0000_0000_0000_0000_0000)
            # T clamp
            self.parameters[6] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_1000_0000_0000_0000_0000)
            # T mirror
            self.parameters[7] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0100_0000_0000_0000_0000)
            # T wrap
            self.parameters[8] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0011_1100_0000_0000_0000)
            # T shift
            self.parameters[9] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0011_1100_0000_0000)
            # S clamp
            self.parameters[10] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0010_0000_0000)
            # S mirror
            self.parameters[11] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0001_0000_0000)
            # S wrap
            self.parameters[12] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0000_1111_0000)
            # S shift
            self.parameters[13] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0000_0000_1111)
            return

        if self.command_name == "G_LOADTLUT":
            # affected tile descriptor index
            self.parameters[0] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # quadrupled color count (-1)
            self.parameters[1] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_0000_0000_0000) // 4 + 1
            return

        if self.command_name == "G_DL":
            # remember return address (which identifies that this is not the end)
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1111_1111_0000_0000_0000_0000)
            # data segment num
            self.parameters[1] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # data offset (removing the leading byte, because that's caught in the param before)
            self.parameters[2] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_1111_1111_1111)
            return

        if self.command_name == "G_VTX":
            # vertex buffer target location (doubled)
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1111_1111_0000_0000_0000_0000) // 2
            # vertex count
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_1111_1100_0000_0000)
            # vertex storage size
            self.parameters[2] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0011_1111_1111)
            # data segment num
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_1111_1111_0000_0000_0000_0000_0000_0000)
            # data offset (removing the leading byte, because that's caught in the param before)
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_1111_1111_1111_1111)
            return

        if self.command_name == "G_TRI1":
            # vertex buffer tri_B_v1 location (doubled)
            self.parameters[0] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_0000_0000_0000_0000) // 2
            # vertex buffer tri_B_v2 location (doubled)
            self.parameters[1] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_1111_1111_0000_0000) // 2
            # vertex buffer tri_B_v3 location (doubled)
            self.parameters[2] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0000_1111_1111) // 2
            return

        if self.command_name == "G_TRI2":
            # vertex buffer tri_A_v1 location (doubled)
            self.parameters[0] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_1111_1111_0000_0000_0000_0000) // 2
            # vertex buffer tri_A_v2 location (doubled)
            self.parameters[1] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_1111_1111_0000_0000) // 2
            # vertex buffer tri_A_v3 location (doubled)
            self.parameters[2] = binjo_utils.apply_bitmask(self.upper, 0b_0000_0000_0000_0000_0000_0000_1111_1111) // 2

            # vertex buffer tri_B_v1 location (doubled)
            self.parameters[3] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_1111_1111_0000_0000_0000_0000) // 2
            # vertex buffer tri_B_v2 location (doubled)
            self.parameters[4] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_1111_1111_0000_0000) // 2
            # vertex buffer tri_B_v3 location (doubled)
            self.parameters[5] = binjo_utils.apply_bitmask(self.lower, 0b_0000_0000_0000_0000_0000_0000_1111_1111) // 2
            return

        # these dont contain any parameters
        if self.command_name in {
            "G_SPNOOP",
            "G_ENDDL",
            "G_RDPLOADSYNC",
            "G_RDPPIPESYNC",
            "G_RDPTILESYNC",
            "G_RDPFULLSYNC"
        }:
            return

        if self.command_name not in DisplayList_Command.unimplemented_commands:
            print(f"Unimplemented (and unhandled) Command encountered: {self.command_name}")
            DisplayList_Command.unimplemented_commands.append(self.command_name)
        return

    def G_CLEARGEOMETRYMODE(flags):
        cmd = 0x00
        cmd = cmd | (binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_CLEARGEOMETRYMODE"], 56, 8))
        cmd = cmd | (binjo_utils.shift_cut(flags, 0, 32))
        return cmd
    
    def G_SETGEOMETRYMODE(flags):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SETGEOMETRYMODE"], 56, 8)
        cmd |= binjo_utils.shift_cut(flags, 0, 32)
        return cmd

    def G_TEXTURE(mipmap_cnt, descriptor_idx, activate, scaling_S, scaling_T):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_TEXTURE"], 56, 8)
        cmd |= binjo_utils.shift_cut(mipmap_cnt, 43, 3)
        cmd |= binjo_utils.shift_cut(descriptor_idx, 40, 3)
        cmd |= binjo_utils.shift_cut((1 if activate else 0), 32, 8)  # technically only 1 bit, but the entire byte is reserved
        cmd |= binjo_utils.shift_cut(scaling_S, 16, 16)
        cmd |= binjo_utils.shift_cut(scaling_T, 0, 16)
        return cmd

    def G_SETTIMG(color_format, bitsize, seg_address):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SETTIMG"], 56, 8)
        cmd |= binjo_utils.shift_cut(Dicts.SETTILE_COLFORMAT[color_format], 53, 3)
        bitsize_transformed = int(np.log2(bitsize / 4))
        cmd |= binjo_utils.shift_cut(bitsize_transformed, 51, 2)
        addr_transformed = (Dicts.INTERNAL_SEG_NAMES["Tex"] << 24) + seg_address
        cmd |= binjo_utils.shift_cut(addr_transformed, 0, 32)
        return cmd
    
    # The warp_ and shift_ values are interpreted as 2^N; So a wrap of 6 means show 2^6=64px
    def G_SETTILE(
        color_format, bitsize, width, TMEM, descriptor_idx, pal,
        clamp_S, mirror_S, wrap_S, shift_S,
        clamp_T, mirror_T, wrap_T, shift_T
    ):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SETTILE"], 56, 8)
        cmd |= binjo_utils.shift_cut(Dicts.SETTILE_COLFORMAT[color_format], 53, 3)
        bitsize_transformed = int(np.log2(bitsize / 4))
        cmd |= binjo_utils.shift_cut(bitsize_transformed, 51, 2)
        num64 = (width * bitsize) // 64
        cmd |= binjo_utils.shift_cut(num64, 41, 9)  # there is a bit of padding in front of this, so bit #50 is unused
        cmd |= binjo_utils.shift_cut(TMEM, 32, 9)
        cmd |= binjo_utils.shift_cut(descriptor_idx, 24, 3)
        cmd |= binjo_utils.shift_cut(pal, 20, 4)
        # T axis
        cmd |= binjo_utils.shift_cut(int(clamp_T), 19, 1)
        cmd |= binjo_utils.shift_cut(int(mirror_T), 18, 1)
        cmd |= binjo_utils.shift_cut(wrap_T, 14, 4)
        cmd |= binjo_utils.shift_cut(shift_T, 10, 4)
        # S axis
        cmd |= binjo_utils.shift_cut(int(clamp_S), 9, 1)
        cmd |= binjo_utils.shift_cut(int(mirror_S), 8, 1)
        cmd |= binjo_utils.shift_cut(wrap_S, 4, 4)
        cmd |= binjo_utils.shift_cut(shift_S, 0, 4)
        return cmd

    def G_SETTILESIZE(ULx, ULy, descriptor_idx, width, height):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SETTILESIZE"], 56, 8)
        cmd |= binjo_utils.shift_cut(ULx, 44, 12)  # 3 nibbles
        cmd |= binjo_utils.shift_cut(ULy, 32, 12)  # 3 nibbles
        cmd |= binjo_utils.shift_cut(descriptor_idx, 24, 8)
        W_transformed = 4 * (width - 1)
        H_transformed = 4 * (height - 1)
        cmd |= binjo_utils.shift_cut(W_transformed, 12, 12)  # 3 nibbles
        cmd |= binjo_utils.shift_cut(H_transformed, 0, 12)  # 3 nibbles
        return cmd

    def G_LOADTLUT(descriptor_idx, color_cnt):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_LOADTLUT"], 56, 8)
        cmd |= binjo_utils.shift_cut(descriptor_idx, 24, 8)
        cc_transformed = 4 * (color_cnt - 1)
        cmd |= binjo_utils.shift_cut(cc_transformed, 12, 12)  # 3 nibbles
        return cmd

    def G_SetOtherMode_H(target, bitlen, modebits):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SetOtherMode_H"], 56, 8)
        cmd |= binjo_utils.shift_cut(Dicts.OTHERMODE_H_MDSFT[target], 40, 8)
        cmd |= binjo_utils.shift_cut(bitlen, 32, 8)
        cmd |= binjo_utils.shift_cut(modebits, 0, 32)
        return cmd

    def G_LOADBLOCK(ULx, ULy, descriptor_idx, width, height, texel_bitsize):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_LOADBLOCK"], 56, 8)
        cmd |= binjo_utils.shift_cut(ULx, 44, 12)  # 3 nibbles
        cmd |= binjo_utils.shift_cut(ULy, 32, 12)  # 3 nibbles
        cmd |= binjo_utils.shift_cut(descriptor_idx, 24, 8)
        texel_cnt = (width * height) - 1  # Subtract 1 as per note
        cmd |= binjo_utils.shift_cut(texel_cnt, 12, 12)  # 3 nibbles
        DXT = binjo_utils.calc_DXT(width, texel_bitsize)
        cmd |= binjo_utils.shift_cut(DXT, 0, 12)  # 3 nibbles
        return cmd

    def G_RDPPIPESYNC():
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_RDPPIPESYNC"], 56, 8)
        return cmd
    
    def G_SETCOMBINE(mode=0):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_SETCOMBINE"], 56, 8)
        if (mode == 0):
            cmd |= binjo_utils.shift_cut(0x00129804, 32, 24)
            cmd |= binjo_utils.shift_cut(0x3F15FFFF, 0, 32)
        # no clue what the difference is (yet) but MMM and GV use these aswell,
        # probably for transparency drawing
        if (mode == 1):
            cmd |= binjo_utils.shift_cut(0x00269804, 32, 24)
            cmd |= binjo_utils.shift_cut(0x1F14FFFF, 0, 32)
        if (mode == 2):
            cmd |= binjo_utils.shift_cut(0x0062FE04, 32, 24)
            cmd |= binjo_utils.shift_cut(0x3F15F9FF, 0, 32)
        return cmd

    def G_DL(final, segment, seg_address):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_DL"], 56, 8)
        cmd |= binjo_utils.shift_cut((1 if final else 0), 48, 8)
        cmd |= binjo_utils.shift_cut(Dicts.INTERNAL_SEG_NAMES[segment], 24, 8)
        cmd |= binjo_utils.shift_cut(seg_address, 0, 24)
        return cmd

    def G_VTX(buffer_target, vtx_cnt, vtx_start_id):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_VTX"], 56, 8)
        cmd |= binjo_utils.shift_cut(2 * buffer_target, 48, 8)  # buffer id is doubled in encoding
        cmd |= binjo_utils.shift_cut(vtx_cnt, 42, 6)
        mem_size = (vtx_cnt * 0x10) - 1  # -1 as required by F3DEX
        cmd |= binjo_utils.shift_cut(mem_size, 32, 10)
        cmd |= binjo_utils.shift_cut(Dicts.INTERNAL_SEG_NAMES["VTX"], 24, 8)
        seg_address = vtx_start_id * 0x10
        cmd |= binjo_utils.shift_cut(seg_address, 0, 24)
        return cmd

    def G_TRI1(id_A1, id_A2, id_A3):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_TRI1"], 56, 8)
        # A is stored in the back
        cmd |= binjo_utils.shift_cut(id_A1 * 2, 16, 8)
        cmd |= binjo_utils.shift_cut(id_A2 * 2, 8,  8)
        cmd |= binjo_utils.shift_cut(id_A3 * 2, 0,  8)
        return cmd

    def G_TRI2(id_A1, id_A2, id_A3, id_B1, id_B2, id_B3):
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_TRI2"], 56, 8)
        # A is stored in the FRONT
        cmd |= binjo_utils.shift_cut(id_A1 * 2, 48, 8)
        cmd |= binjo_utils.shift_cut(id_A2 * 2, 40, 8)
        cmd |= binjo_utils.shift_cut(id_A3 * 2, 32, 8)
        # B is stored in the back
        cmd |= binjo_utils.shift_cut(id_B1 * 2, 16, 8)
        cmd |= binjo_utils.shift_cut(id_B2 * 2, 8,  8)
        cmd |= binjo_utils.shift_cut(id_B3 * 2, 0,  8)
        return cmd

    def G_ENDDL():
        cmd = 0x00
        cmd |= binjo_utils.shift_cut(Dicts.F3DEX_CMD_NAMES["G_ENDDL"], 56, 8)
        return cmd



class TileDescriptor:
    def __init__(self):
        self.color_storage_format = 0
        self.color_storage_bitsize = 0
        self.bitvals_per_row = 0
        self.TMEM_tex_offset = 0
        self.corresponding_palette = 0
        # TS coordinates are the axes that the GPU uses to index into image data
        self.T_clamp = False
        self.T_mirror = False
        self.T_wrap = 0.0
        self.T_shift = 0.0
        self.S_clamp = False
        self.S_mirror = False
        self.S_wrap = 0.0
        self.S_shift = 0.0

        self.tex_idx = None
        self.tex_width = 0
        self.tex_height = 0



class ModelBIN_DLSeg:
    HEADER_SIZE = 0x08

    def __init__(self):
        self.valid = False

    def populate_from_data(self, file_data, file_offset):
        if file_offset == 0:
            print("No DL Segment")
            self.valid = False
            return
        
        self.file_offset = file_offset

        # parsing properties
        self.command_cnt = binjo_utils.read_bytes(file_data, file_offset + 0x00, 4)

        self.command_list = []
        for idx in range(0, self.command_cnt):
            file_offset_cmd = file_offset + ModelBIN_DLSeg.HEADER_SIZE + (0x08 * idx)
            upper = binjo_utils.read_bytes(file_data, file_offset_cmd + 0x00, 4)
            lower = binjo_utils.read_bytes(file_data, file_offset_cmd + 0x04, 4)
            self.command_list.append(DisplayList_Command(upper, lower))

        self.valid = True
        return

    def populate_from_command_list(self, command_list):
        self.command_list = command_list
        # check if the list is terminated and terminate it if it wasn't
        if (self.command_list[-1].command_name != "G_ENDDL"):
            # unset the OtherMode bits...
            self.command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SetOtherMode_H(("G_MDSFT_TEXTLUT"), 2, DisplayList_Command.OtherMode_CLEAR)
            ))
            self.command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_ENDDL()
            ))
        self.command_cnt = len(self.command_list)
        self.valid = True

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.command_cnt, 4)
        output += binjo_utils.int_to_bytes(0x00000000, 4)
        for cmd in self.command_list:
            output += binjo_utils.int_to_bytes(cmd.full, 8)
        return output

    # build the command-chunk that first loads the corresponding VTXs and then draws the TRIs
    # NOTE: A maximum of 0x20 = 32 VTXs can be loaded at once !
    #       This also assumes that all VTX IDs are ordered and unique for now <-- bad !
    def build_tri_drawing_commands(tri_list):
        DL_command_chunk = []
        # the smallest index should probably be calculated as the actual min later
        smallest_index = tri_list[0].index_1
        DL_command_chunk.append(DisplayList_Command(full=
            DisplayList_Command.G_VTX(0, (3 * len(tri_list)), smallest_index)
        ))
        for tri in tri_list:
            # its guaranteed that I have an even number of tris at this point
            DL_command_chunk.append(DisplayList_Command(full=
                DisplayList_Command.G_TRI2(
                    (tri.index_1 - smallest_index),
                    (tri.index_2 - smallest_index),
                    (tri.index_3 - smallest_index),
                    (tri.index_1 - smallest_index),
                    (tri.index_2 - smallest_index),
                    (tri.index_3 - smallest_index)
                )
            ))
        return DL_command_chunk


    def build_setup_commands(tex_element, mode=0):
        command_list = []

        if (tex_element.tex_type == Dicts.TEX_TYPES["CI4"]):
            # general setup stuff
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_CLEARGEOMETRYMODE((
                    Dicts.RSP_GEOMODE_FLAGS["G_SHADE"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_SHADING_SMOOTH"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_CULL_BOTH"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_FOG"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_LIGHTING"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN_LINEAR"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_LOD"]
                ))
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SETGEOMETRYMODE((
                    Dicts.RSP_GEOMODE_FLAGS["G_SHADE"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_SHADING_SMOOTH"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_CULL_BACK"] |
                    Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN_LINEAR"]
                ))
            ))

            # palette setup stuff
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_TEXTURE(0, 0, True, 0x8000, 0x8000)
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SETTIMG("RGBA", 16, tex_element.datasection_offset_data)
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SETTILE("RGBA", 4, tex_element.width, 0x0100, 1, 0, False, False, 0, 0, False, False, 0, 0)
            ))

            # tex-data setup stuff
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_LOADTLUT(1, 16)
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SetOtherMode_H(("G_MDSFT_TEXTLUT"), 2, DisplayList_Command.OtherMode_SET)
            ))

            texel_cnt = (tex_element.width * tex_element.height)
            texel_bitsize = Dicts.TEXEL_FMT_BITSIZE["CI4"]
            S_wrap = int(np.log2(tex_element.width))
            T_wrap = int(np.log2(tex_element.height))
            # we have to outplay the TMEM restriction of loading a max of 0x400 texels at a time
            if (texel_cnt > 0x400):
                # this should use the texel bitsize of the format instead of 4
                fakeout_bitsize = (texel_bitsize * 4)
                fakeout_factor = (fakeout_bitsize / texel_bitsize)

                # this command is a really really hacky way of outplaying the TMEM restrictions...
                # normally, G_LOADBLOCK can only load a maximum of 0x400 texels at a time, but if we set the bitsize to 16
                # here before loading "0x400 texels", we juke the processor into actually loading 0x1000 texels of size 4b...
                # BUT, we have to reset this hack with an additional G_SETTILE after the G_LOADBLOCK, that fixes the fake bitsize.
                # That's also why this command works on descriptor-7 I guess.
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTIMG("CI", fakeout_bitsize, (tex_element.datasection_offset_data + 0x20)) # NOTE: +0x20 for CI4, +0x200 for CI8...
                ))
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTILE("CI", fakeout_bitsize, 0, 0x0000, 7, 0, False, False, S_wrap, 0, False, False, T_wrap, 0)
                ))
                # Note that we are providing some tempered-with params here
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_LOADBLOCK(
                        0, 0, 7,
                        (tex_element.width / fakeout_factor),
                        tex_element.height,
                        fakeout_bitsize
                    )
                ))
                # restore the correct texel bitsize afterwards
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTILE("CI", texel_bitsize, tex_element.width, 0x0000, 0, 0, False, False, S_wrap, 0, False, False, T_wrap, 0)
                ))
            # otherwise we can directly load to TMEM (and immediatly use descriptor-0)
            else:
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTIMG("CI", texel_bitsize, (tex_element.datasection_offset_data + 0x20)) # NOTE: +0x20 for CI4, +0x200 for CI8...
                ))
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTILE("CI", texel_bitsize, 0, 0x0000, 7, 0, False, False, S_wrap, 0, False, False, T_wrap, 0)
                ))
                # set up the faked LOADBLOCK; note that we reduce the percieved width by the fakeout factor
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_LOADBLOCK(
                        0, 0, 7,
                        tex_element.width, 
                        tex_element.height,
                        Dicts.TEXEL_FMT_BITSIZE["CI4"]
                    )
                ))
                command_list.append(DisplayList_Command(full=
                    DisplayList_Command.G_SETTILE("CI", texel_bitsize, tex_element.width, 0x0000, 0, 0, False, False, S_wrap, 0, False, False, T_wrap, 0)
                ))

            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SETTILESIZE(0, 0, 0, tex_element.width, tex_element.height)
            ))

            # more general setup stuff
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_RDPPIPESYNC()
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_SETCOMBINE(mode=0)
            ))
            command_list.append(DisplayList_Command(full=
                DisplayList_Command.G_DL(False, "Mode", 0x20)
            ))
            return command_list