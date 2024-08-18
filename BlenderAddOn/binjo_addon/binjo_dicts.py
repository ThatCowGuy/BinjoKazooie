
class Dicts:
    TEX_TYPES = {
        "CI4":      0x01, # C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
        "CI8":      0x02, # C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        "RGBA16":   0x04, # RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel
        "RGBA32":   0x08, # RGBA32 or RGBA8888 without a palette; pixels stored as a 32bit texel
        "IA8":      0x10, # IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha
    }

    TEXEL_FMT_BITSIZE = {
        "CI4": 4,           "CI4 (5551)": 4,            0x01: 4,
        "CI8": 8,           "CI8 (5551)": 8,            0x02: 8,
        "RGBA16": 16,       "RGBA16 (5551)": 16,        0x04: 16,
        "RGBA32": 32,       "RGBA32 (8888)": 32,        0x08: 32,
        "IA8": 8,           "IA8 (44)": 8,              0x10: 8
    }

    TEX_TYPE_PALETTE_SIZE = {
        "CI4": 0x20,        "CI4 (5551)": 0x20,         0x01: 0x20,
        "CI8": 0x0200,      "CI8 (5551)": 0x0200,       0x02: 0x0200,
        "RGBA16": 0x00,     "RGBA16 (5551)": 0x00,      0x04: 0x00,
        "RGBA32": 0x00,     "RGBA32 (8888)": 0x00,      0x08: 0x00,
        "IA8": 0x00,        "IA8 (44)": 0x00,           0x10: 0x00
    }

    F3DEX_CMD_NAMES = {
        "G_SPNOOP"           : 0x00,
        "G_MTX"              : 0x01,
        "G_MOVEMEM"          : 0x03,
        "G_VTX"              : 0x04,
        "G_DL"               : 0x06,
        "G_LOAD_UCODE"       : 0xAF,
        "G_BRANCH_Z"         : 0xB0,
        "G_TRI2"             : 0xB1,
        "G_MODIFYVTX"        : 0xB2,
        "G_RDPHALF_2"        : 0xB3,
        "G_QUAD"             : 0xB5,
        "G_CLEARGEOMETRYMODE": 0xB6,
        "G_SETGEOMETRYMODE"  : 0xB7,
        "G_ENDDL"            : 0xB8,
        "G_SetOtherMode_L"   : 0xB9,
        "G_SetOtherMode_H"   : 0xBA,
        "G_TEXTURE"          : 0xBB,
        "G_MOVEWORD"         : 0xBC,
        "G_POPMTX"           : 0xBD,
        "G_CULLDL"           : 0xBE,
        "G_TRI1"             : 0xBF,
        "G_NOOP"             : 0xC0,
        "G_TEXRECT"          : 0xE4,
        "G_TEXRECTFLIP"      : 0xE5,
        "G_RDPLOADSYNC"      : 0xE6,
        "G_RDPPIPESYNC"      : 0xE7,
        "G_RDPTILESYNC"      : 0xE8,
        "G_RDPFULLSYNC"      : 0xE9,
        "G_SETKEYGB"         : 0xEA,
        "G_SETKEYR"          : 0xEB,
        "G_SETCONVERT"       : 0xEC,
        "G_SETSCISSOR"       : 0xED,
        "G_SETPRIMDEPTH"     : 0xEE,
        "G_RDPSetOtherMode"  : 0xEF,
        "G_LOADTLUT"         : 0xF0,
        "G_SETTILESIZE"      : 0xF2,
        "G_LOADBLOCK"        : 0xF3,
        "G_LOADTILE"         : 0xF4,
        "G_SETTILE"          : 0xF5,
        "G_FILLRECT"         : 0xF6,
        "G_SETFILLCOLOR"     : 0xF7,
        "G_SETFOGCOLOR"      : 0xF8,
        "G_SETBLENDCOLOR"    : 0xF9,
        "G_SETPRIMCOLOR"     : 0xFA,
        "G_SETENVCOLOR"      : 0xFB,
        "G_SETCOMBINE"       : 0xFC,
        "G_SETTIMG"          : 0xFD,
        "G_SETZIMG"          : 0xFE,
        "G_SETCIMG"          : 0xFF
    }
    F3DEX_CMD_NAMES_REV = {
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

    OTHERMODE_H_MDSFT = {
        "G_MDSFT_BLENDMASK":   0x00,
        "G_MDSFT_ALPHADITHER": 0x04,
        "G_MDSFT_RGBDITHER":   0x06,
        "G_MDSFT_COMBKEY":     0x08,
        "G_MDSFT_TEXTCONV":    0x09,
        "G_MDSFT_TEXTFILT":    0x0C,
        "G_MDSFT_TEXTLUT":     0x0E,
        "G_MDSFT_TEXTLOD":     0x10,
        "G_MDSFT_TEXTDETAIL":  0x11,
        "G_MDSFT_TEXTPERSP":   0x13,
        "G_MDSFT_CYCLETYPE":   0x14,
        "G_MDSFT_COLORDITHER": 0x16,
        "G_MDSFT_PIPELINE":    0x17,
    }
    OTHERMODE_H_MDSFT_REV = {
        0x00: "G_MDSFT_BLENDMASK",
        0x04: "G_MDSFT_ALPHADITHER",
        0x06: "G_MDSFT_RGBDITHER",
        0x08: "G_MDSFT_COMBKEY",
        0x09: "G_MDSFT_TEXTCONV",
        0x0C: "G_MDSFT_TEXTFILT",
        0x0E: "G_MDSFT_TEXTLUT",
        0x10: "G_MDSFT_TEXTLOD",
        0x11: "G_MDSFT_TEXTDETAIL",
        0x13: "G_MDSFT_TEXTPERSP",
        0x14: "G_MDSFT_CYCLETYPE",
        0x16: "G_MDSFT_COLORDITHER",
        0x17: "G_MDSFT_PIPELINE",
    }

    SETTILE_COLFORMAT = {
        "RGBA": 0b000,
        "YUV":  0b001,
        "CI":   0b010,
        "IA":   0b011,
        "I":    0b100,
    }
    SETTILE_COLFORMAT_REV = {
        0b000: "RGBA",
        0b001: "YUV",
        0b010: "CI",
        0b011: "IA",
        0b100: "I",
    }

    INTERNAL_SEG_NAMES = {
        "VTX":      0x01,
        "Tex":      0x02,
        "Mode":     0x03,
        "Virt":     0x04,
        # ...
        "DLs":      0x09,
        # ...
        "AnTex":    0x0F,
    }
    INTERNAL_SEG_NAMES_REV = {
        0x01:  "VTX",
        0x02:  "Tex",
        0x03:  "Mode",
        0x04:  "Virt",
        # ...
        0x09:  "DLs",
        # ...
        0x0B:  "AnTex",
        0x0C:  "AnTex",
        0x0D:  "AnTex",
        0x0E:  "AnTex",
        0x0F:  "AnTex",
    }

    COLLISION_FLAGS = {
        "UNK_00":               0b00000000_00000000_00000000_00000001,
        "UNK_01":               0b00000000_00000000_00000000_00000010,
        "UNK_02":               0b00000000_00000000_00000000_00000100,
        "UNK_03":               0b00000000_00000000_00000000_00001000,
        "Trottable Slope":      0b00000000_00000000_00000000_00010000,
        "UNK_05":               0b00000000_00000000_00000000_00100000,
        "Untrottable Slope":    0b00000000_00000000_00000000_01000000,
        "UNK_07":               0b00000000_00000000_00000000_10000000,
        # "SFX Value":          0b00000000_00000000_00001111_00000000, # 4b Integer that references a list of SFXs
        "UNK_0C":               0b00000000_00000000_00010000_00000000,
        "Damage":               0b00000000_00000000_00100000_00000000,
        "(0E) DMG rel. 1":      0b00000000_00000000_01000000_00000000, # used by damage func "core2/code_16010.c/func_8029D968"
        "(0F) DMG rel. 2":      0b00000000_00000000_10000000_00000000, # used by damage func "core2/code_16010.c/func_8029D968"
        "Double Sided":         0b00000000_00000001_00000000_00000000,
        "Water":                0b00000000_00000010_00000000_00000000,
        "UNK_12":               0b00000000_00000100_00000000_00000000,
        "UNK_13":               0b00000000_00001000_00000000_00000000,
        "UNK_14":               0b00000000_00010000_00000000_00000000,
        "Reflective":           0b00000000_00100000_00000000_00000000,
        "Non-Impeding":         0b00000000_01000000_00000000_00000000,
        "(17) TTC Brdwlk rel":  0b00000000_10000000_00000000_00000000,
        "(18) SFX related":     0b00000001_00000000_00000000_00000000,
        "(19) FF related":      0b00000010_00000000_00000000_00000000,
        "(1A) Snarebear":       0b00000100_00000000_00000000_00000000,
        "Script Target":        0b00001000_00000000_00000000_00000000,
        "UNK_1C":               0b00010000_00000000_00000000_00000000,
        "UNK_1D":               0b00100000_00000000_00000000_00000000,
        "(1E) Jinjo St. rel.":  0b01000000_00000000_00000000_00000000,
        "Use Default SFXs":     0b10000000_00000000_00000000_00000000,
    }
    COLLISION_FLAG_DESCRIPTIONS = {
        "UNK_00":               "",
        "UNK_01":               "",
        "UNK_02":               "",
        "UNK_03":               "",
        "Trottable Slope":      "Makes Banjo slip after 1s; Kazooie can move freely with TalonTrot",
        "UNK_05":               "",
        "Untrottable Slope":    "Makes BK slip after 1s regardless of Movestate; Transformations are unaffected",
        "UNK_07":               "",
        # "SFX Value":            "A 4b Integer Value used to index into a Table of SFXs", # 4b Integer that references a list of SFXs
        "UNK_0C":               "",
        "Damage":               "BK take Damage on this Surface in intervals; Makes you move slowly aswell (Map constrained !)",
        "(0E) DMG rel. 1":      "used by the Damaging-Func in some fashion", # used by damage func "core2/code_16010.c/func_8029D968"
        "(0F) DMG rel. 2":      "used by the Damaging-Func in some fashion", # used by damage func "core2/code_16010.c/func_8029D968"
        "Double Sided":         "Can collide with this Surface from both Sides; Doesn't affect Drawing of Surface !",
        "Water":                "Lets BK swim in and underneath; Transformations are unaffected",
        "UNK_12":               "",
        "UNK_13":               "",
        "UNK_14":               "",
        "Reflective":           "Reflective Surfaces use this; unknown why it's part of Collision",
        "Non-Impeding":         "Can pass through this surface freely, but the Collision is still detected (eg. Tall Grass)",
        "(17) TTC Brdwlk rel":  "TTC Boardwalk uses this; unknown purpose",
        "(18) SFX related":     "Has something to do with SFXs (maybe)",
        "(19) FF related":      "Furnace Fun uses this; unknown purpose",
        "(1A) Snarebear":       "Snearbears use this; unknown purpose",
        "Script Target":        "Map-Event Scripts target this Surface (eg. Sandcastle Floor, RBB Moving Paths)",
        "UNK_1C":               "",
        "UNK_1D":               "",
        "(1E) Jinjo St. rel.":  "Jinjo Statues use this; unknown purpose",
        "Use Default SFXs":     "If this is set, the SFX Value indexes into the Default Table; If not, use a different local Table",
    }
    COLLISION_SFX = {
        "Normal"    : 0b0000,
        "Metal"     : 0b0001,
        "Hollow"    : 0b0010,
        "Stone"     : 0b0011,
        "Wood"      : 0b0100,
        "Snow"      : 0b0101, # AND slippy !
        "Rustling"  : 0b0110,
        "Mud"       : 0b0111,
        "Sand"      : 0b1000,
        "Slushy"    : 0b1001,
        "unused_0A" : 0b1010,
        "unused_0B" : 0b1011,
        "unused_0C" : 0b1100,
        "unused_0D" : 0b1101,
        "unused_0E" : 0b1110,
        "unused_0F" : 0b1111
    }
    COLLISION_SFX_REV = {
        0b0000: "Normal",
        0b0001: "Metal",
        0b0010: "Hollow",
        0b0011: "Stone",
        0b0100: "Wood",
        0b0101: "Snow",
        0b0110: "Rustling",
        0b0111: "Mud",
        0b1000: "Sand",
        0b1001: "Slushy",
        0b1010: "unused_0A",
        0b1011: "unused_0B",
        0b1100: "unused_0C",
        0b1101: "unused_0D",
        0b1110: "unused_0E",
        0b1111: "unused_0F"
    }

    RSP_GEOMODE_FLAGS = {
        "G_ZBUFFER":            0x00000001,
        "G_SHADE":              0x00000004,

        # F3DEX-1, not 2
        "G_SHADING_SMOOTH":     0x00000200,

        "G_CULL_FRONT":         0x00001000,
        "G_CULL_BACK":          0x00002000,
        "G_CULL_BOTH":          0x00003000,

        "G_FOG":                0x00010000,
        "G_LIGHTING":           0x00020000,
        "G_TEXTURE_GEN":        0x00040000,
        "G_TEXTURE_GEN_LINEAR": 0x00080000,
        "G_LOD":                0x00100000,
        "G_CLIPPING":           0x00800000,
    }

    GEO_CMD_NAMES = {
        "SORT":             0x01,
        "BONE":             0x02,
        "LOAD_DL":          0x03, # just starts running a DL (offset is defined in int[2])
        "UNKNOWN_04":       0x04,
        "SKINNING":         0x05,
        "BRANCH":           0x06,
        "UNKNOWN_07":       0x07,
        "LOD":              0x08,
        "UNKNOWN_09":       0x09,
        "REFERENCE_POINT":  0x0A,
        "UNKNOWN_0B":       0x0B,
        "SELECTOR":         0x0C, # for model-swapping: int[2] holds short[0] child_count, short[1] selector index, then list of children
        # 0000000C 00000060 00040001 00000020 00000030 00000040 00000050 00000000 00000003...
        #                   ^ 4 children
        #                       ^ selector ID = 1
        #                            ^ 1st child: offset 0x20 bytes to next geolayout command
        #                                     ^ 2nd child: offset 0x30 bytes to next geolayout command
        "DRAW_DISTANCE":    0x0D,
        "UNKNOWN_0E":       0x0E,
        "UNKNOWN_0F":       0x0F,
    }
    GEO_CMD_NAMES_REV = {
        0x01: "SORT",
        0x02: "BONE",
        0x03: "LOAD_DL",
        0x04: "UNKNOWN_04",
        0x05: "SKINNING",
        0x06: "BRANCH",
        0x07: "UNKNOWN_07",
        0x08: "LOD",
        0x09: "UNKNOWN_09",
        0x0A: "REFERENCE_POINT",
        0x0B: "UNKNOWN_0B",
        0x0C: "SELECTOR",
        0x0D: "DRAW_DISTANCE",
        0x0E: "UNKNOWN_0E",
        0x0F: "UNKNOWN_0F",
    }
