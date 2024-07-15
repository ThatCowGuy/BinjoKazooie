
class Dicts:
    TEX_TYPES = {
        "CI4":      0x01, # C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
        "CI8":      0x02, # C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        "RGBA16":   0x04, # RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel
        "RGBA32":   0x08, # RGBA32 or RGBA8888 without a palette; pixels stored as a 32bit texel
        "IA8":      0x10, # IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha
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

    TEXEL_FMT_BITSIZE = {
        "RGBA32": 32,       "RGBA32 (8888)": 32,
        "RGBA16": 16,       "RGBA16 (5551)": 16,
        "CI8": 8,           "CI8 (5551)": 8,
        "CI4": 4,           "CI4 (5551)": 4,
        "IA8": 8,           "IA8 (44)": 8
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