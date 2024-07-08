
from . import binjo_utils

class DisplayList_Command:
    F3DEX_CMD_NAMES = {
        0x00: "G_SPNOOP",
        0x01: "G_MTX",
        0x03: "G_MOVEMEM",
        0x04: "G_VTX",
        0x06: "G_DL",
        0xAF: "G_LOAD_UCODE",
        0xB0: "G_BRANCH_Z",
        0xB1: "G_TRI2",
        0xB2: "G_MODIFYVTX",
        0xB3: "G_RDPHALF_2",
        0xB5: "G_QUAD",
        0xB6: "G_CLEARGEOMETRYMODE",
        0xB7: "G_SETGEOMETRYMODE",
        0xB8: "G_ENDDL",
        0xB9: "G_SetOtherMode_L",
        0xBA: "G_SetOtherMode_H",
        0xBB: "G_TEXTURE",
        0xBC: "G_MOVEWORD",
        0xBD: "G_POPMTX",
        0xBE: "G_CULLDL",
        0xBF: "G_TRI1",
        0xC0: "G_NOOP",
        0xE4: "G_TEXRECT",
        0xE5: "G_TEXRECTFLIP",
        0xE6: "G_RDPLOADSYNC",
        0xE7: "G_RDPPIPESYNC",
        0xE8: "G_RDPTILESYNC",
        0xE9: "G_RDPFULLSYNC",
        0xEA: "G_SETKEYGB",
        0xEB: "G_SETKEYR",
        0xEC: "G_SETCONVERT",
        0xED: "G_SETSCISSOR",
        0xEE: "G_SETPRIMDEPTH",
        0xEF: "G_RDPSetOtherMode",
        0xF0: "G_LOADTLUT",
        0xF2: "G_SETTILESIZE",
        0xF3: "G_LOADBLOCK",
        0xF4: "G_LOADTILE",
        0xF5: "G_SETTILE",
        0xF6: "G_FILLRECT",
        0xF7: "G_SETFILLCOLOR",
        0xF8: "G_SETFOGCOLOR",
        0xF9: "G_SETBLENDCOLOR",
        0xFA: "G_SETPRIMCOLOR",
        0xFB: "G_SETENVCOLOR",
        0xFC: "G_SETCOMBINE",
        0xFD: "G_SETTIMG",
        0xFE: "G_SETZIMG",
        0xFF: "G_SETCIMG"
    }

    def __init__(self, upper=0x00, lower=0x00):
        self.upper = upper
        self.lower = lower
        self.command_byte = (upper >> 24)
        self.command_name = DisplayList_Command.F3DEX_CMD_NAMES[self.command_byte]
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

    def __init__(self, file_data, file_offset):
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