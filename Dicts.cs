using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    public class Dicts
    {
        /*/=====================================================
         * Thanks to Unalive for documenting these Flags
        =====================================================/*/
        public static Dictionary<int, string> COLLISION_FLAGS_REV = new Dictionary<int, string>
        {
            { 0x00, "UNK_00" },
            { 0x01, "UNK_01" },
            { 0x02, "UNK_02" },
            { 0x03, "Water" },
            { 0x04, "Trottable" },
            { 0x05, "Disc-Rim" },
            { 0x06, "Un-Trottable" },
            { 0x07, "UNK_07" },
            { 0x08, "UNK_08" }, // ~ footstep sfx" },
            { 0x09, "UNK_09" }, // ~ footstep sfx" },
            { 0x0A, "UNK_0A" }, // ~ footstep sfx" },
            { 0x0B, "UNK_0B" }, // ~ footstep sfx" },
            { 0x0C, "UNK_0C" }, // ~ footstep sfx" },
            { 0x0D, "Damage" },
            { 0x0E, "UNK_0E" }, // ~ Damage func" },
            { 0x0F, "UNK_0F" }, // ~ Damage func" },
        };
        public static Dictionary<string, int> COLLISION_FLAGS = new Dictionary<string, int>
        {
            { "UNK_00",       0x00 },
            { "UNK_01",       0x01 },
            { "UNK_02",       0x02 },
            { "Water",        0x03 },
            { "Trottable",    0x04 },
            { "Disc-Rim",     0x05 },
            { "Un-Trottable", 0x06 },
            { "UNK_07",       0x07 },
            { "UNK_08",       0x08 }, // ~ footstep sfx" },
            { "UNK_09",       0x09 }, // ~ footstep sfx" },
            { "UNK_0A",       0x0A }, // ~ footstep sfx" },
            { "UNK_0B",       0x0B }, // ~ footstep sfx" },
            { "UNK_0C",       0x0C }, // ~ footstep sfx" },
            { "Damage",       0x0D },
            { "UNK_0E",       0x0E }, // ~ Damage func" },
            { "UNK_0F",       0x0F }, // ~ Damage func" },
        };

        public static Dictionary<int, string> SOUND_FLAGS_REV = new Dictionary<int, string>
        {
            { 0x0, "GV Tree Leaves" },
            { 0x1, "" },
            { 0x2, "" },
            { 0x3, "" },
            { 0x4, "" },
            { 0x5, "" },
            { 0x6, "" },
            { 0x7, "" },
            { 0x8, "Tall Grass" },
            { 0x9, "" },
            { 0xA, "" },
            { 0xB, "Metallic" },
            { 0xC, "" },
            { 0xD, "" },
            { 0xE, "" },
            { 0xF, "~ global footstep sfx" }
        };
        public static Dictionary<string, int> SOUND_FLAGS = new Dictionary<string, int>
        {
            { "GV Tree Leaves", 0x0 },
            { "UNK_01", 0x1 },
            { "UNK_02", 0x2 },
            { "UNK_03", 0x3 },
            { "UNK_04", 0x4 },
            { "UNK_05", 0x5 },
            { "UNK_06", 0x6 },
            { "UNK_07", 0x7 },
            { "Tall Grass", 0x8 },
            { "UNK_09", 0x9 },
            { "UNK_0A", 0xA },
            { "Metallic", 0xB },
            { "UNK_0C", 0xC },
            { "UNK_0D", 0xD },
            { "UNK_0E", 0xE },
            { "~ global footstep sfx", 0xF }
        };

        public static Dictionary<int, String> INTERNAL_SEG_NAMES = new Dictionary<int, string>
        {
            { 0, "MISSING" },
            { 1, "VTX" },
            { 2, "Tex" },
            { 3, "Mode" }, // static data that defines the possible rendermodes (core2/modelRender.c#L204)
            { 4, "Virt" }, // for things that require a model to be drawn to another texture
            // ...
            { 9, "DLs" },
            // ...
            { 11, "AnTex" }, // unsure if this is actually part of it
            { 12, "AnTex" },
            { 13, "AnTex" },
            { 14, "AnTex" },
            { 15, "AnTex" },
        };
        public static Dictionary<String, int> INTERNAL_SEG_NAMES_REV = new Dictionary<String, int>
        {
            { "VTX", 1 },
            { "Tex", 2 },
            { "Mode", 3 },
            { "Virt", 4 },
            // ...
            { "DLs", 9 },
            // ...
            { "AnTex", 15 },
        };
        public static Dictionary<String, int> RSP_GEOMODE_FLAGS = new Dictionary<String, int>
        {
            { "G_ZBUFFER",            0x00000001 },
            { "G_SHADE",              0x00000004 },

            // F3DEX-1, not 2
            { "G_SHADING_SMOOTH",     0x00000200 },

            { "G_CULL_FRONT",         0x00001000 },
            { "G_CULL_BACK",          0x00002000 },
            { "G_CULL_BOTH",          0x00003000 },

            { "G_FOG",                0x00010000 },
            { "G_LIGHTING",           0x00020000 },
            { "G_TEXTURE_GEN",        0x00040000 },
            { "G_TEXTURE_GEN_LINEAR", 0x00080000 },
            { "G_LOD",                0x00100000 },
            { "G_CLIPPING",           0x00800000 },
        };
        public static Dictionary<int, String> RSP_GEOMODE_FLAGS_REV = new Dictionary<int, String>
        {
            { 0x00000001, "G_ZBUFFER"            },
            { 0x00000004, "G_SHADE"              }, // *

            { 0x00000200, "G_SHADING_SMOOTH"     }, // *

            { 0x00001000, "G_CULL_FRONT"         },
            { 0x00002000, "G_CULL_BACK"          },
            { 0x00003000, "G_CULL_BOTH"          }, // *

            { 0x00010000, "G_FOG"                }, // *
            { 0x00020000, "G_LIGHTING"           }, // *
            { 0x00040000, "G_TEXTURE_GEN"        }, // *
            { 0x00080000, "G_TEXTURE_GEN_LINEAR" }, // *
            { 0x00100000, "G_LOD"                }, // *
            { 0x00800000, "G_CLIPPING"           },
        }; // * usually disabled at start of DL

        public static Dictionary<String, int> OTHERMODE_H_MDSFT = new Dictionary<String, int>
        {
            { "G_MDSFT_BLENDMASK",   0x00 },
            { "G_MDSFT_ALPHADITHER", 0x04 },
            { "G_MDSFT_RGBDITHER",   0x06 },
            { "G_MDSFT_COMBKEY",     0x08 },
            { "G_MDSFT_TEXTCONV",    0x09 },
            { "G_MDSFT_TEXTFILT",    0x0C },
            { "G_MDSFT_TEXTLUT",     0x0E },
            { "G_MDSFT_TEXTLOD",     0x10 },
            { "G_MDSFT_TEXTDETAIL",  0x11 },
            { "G_MDSFT_TEXTPERSP",   0x13 },
            { "G_MDSFT_CYCLETYPE",   0x14 },
            { "G_MDSFT_COLORDITHER", 0x16 },
            { "G_MDSFT_PIPELINE",    0x17 },
        };
        public static Dictionary<int, String> OTHERMODE_H_MDSFT_REV = new Dictionary<int, String>
        {
            { 0x00, "G_MDSFT_BLENDMASK"   },
            { 0x04, "G_MDSFT_ALPHADITHER" },
            { 0x06, "G_MDSFT_RGBDITHER"   },
            { 0x08, "G_MDSFT_COMBKEY"     },
            { 0x09, "G_MDSFT_TEXTCONV"    },
            { 0x0C, "G_MDSFT_TEXTFILT"    },
            { 0x0E, "G_MDSFT_TEXTLUT"     },
            { 0x10, "G_MDSFT_TEXTLOD"     },
            { 0x11, "G_MDSFT_TEXTDETAIL"  },
            { 0x13, "G_MDSFT_TEXTPERSP"   },
            { 0x14, "G_MDSFT_CYCLETYPE"   },
            { 0x16, "G_MDSFT_COLORDITHER" },
            { 0x17, "G_MDSFT_PIPELINE"    },
        };
        public static Dictionary<String, int> TEXEL_FMT_BITSIZE = new Dictionary<String, int>
        {
            { "RGBA32", 32 },
            { "RGBA16", 16 },
            { "CI8", 8 },
            { "CI4", 4 },
            { "IA8", 8 },
        };

        public static Dictionary<String, int> SETTILE_COLFORM = new Dictionary<String, int>
        {
            { "RGBA", 0b000 },
            { "YUV",  0b001 },
            { "CI",   0b010 },
            { "IA",   0b011 },
            { "I",    0b100 },
        };
        public static Dictionary<int, String> SETTILE_COLFORM_REV = new Dictionary<int, String>
        {
            { 0b000, "RGBA" },
            { 0b001, "YUV"  },
            { 0b010, "CI"   },
            { 0b011, "IA"   },
            { 0b100, "I"    },
        };
        public static Dictionary<byte, string> F3DEX_CMD_NAMES = new Dictionary<byte, string>()
        {
            { 0x00, "G_SPNOOP" },
            { 0x01, "G_MTX" },
            { 0x03, "G_MOVEMEM" },
            { 0x04, "G_VTX" },
            { 0x06, "G_DL" },
            { 0xAF, "G_LOAD_UCODE" },
            { 0xB0, "G_BRANCH_Z" },
            { 0xB1, "G_TRI2" },
            { 0xB2, "G_MODIFYVTX" },
            { 0xB3, "G_RDPHALF_2" },
            { 0xB5, "G_QUAD" },
            { 0xB6, "G_CLEARGEOMETRYMODE" },
            { 0xB7, "G_SETGEOMETRYMODE" },
            { 0xB8, "G_ENDDL" },
            { 0xB9, "G_SetOtherMode_L" },
            { 0xBA, "G_SetOtherMode_H" },
            { 0xBB, "G_TEXTURE" },
            { 0xBC, "G_MOVEWORD" },
            { 0xBD, "G_POPMTX" },
            { 0xBE, "G_CULLDL" },
            { 0xBF, "G_TRI1" },
            { 0xC0, "G_NOOP" },
            { 0xE4, "G_TEXRECT" },
            { 0xE5, "G_TEXRECTFLIP" },
            { 0xE6, "G_RDPLOADSYNC" },
            { 0xE7, "G_RDPPIPESYNC" },
            { 0xE8, "G_RDPTILESYNC" },
            { 0xE9, "G_RDPFULLSYNC" },
            { 0xEA, "G_SETKEYGB" },
            { 0xEB, "G_SETKEYR" },
            { 0xEC, "G_SETCONVERT" },
            { 0xED, "G_SETSCISSOR" },
            { 0xEE, "G_SETPRIMDEPTH" },
            { 0xEF, "G_RDPSetOtherMode" },
            { 0xF0, "G_LOADTLUT" },
            { 0xF2, "G_SETTILESIZE" },
            { 0xF3, "G_LOADBLOCK" },
            { 0xF4, "G_LOADTILE" },
            { 0xF5, "G_SETTILE" },
            { 0xF6, "G_FILLRECT" },
            { 0xF7, "G_SETFILLCOLOR" },
            { 0xF8, "G_SETFOGCOLOR" },
            { 0xF9, "G_SETBLENDCOLOR" },
            { 0xFA, "G_SETPRIMCOLOR" },
            { 0xFB, "G_SETENVCOLOR" },
            { 0xFC, "G_SETCOMBINE" },
            { 0xFD, "G_SETTIMG" },
            { 0xFE, "G_SETZIMG" },
            { 0xFF, "G_SETCIMG" }
        };
        // NOTE: 0x06 G_DL is also used to set the Render Mode if it is referencing Seg-3.
        //                  v seg
        //       0x06000000 03000000 - transparent ?
        //       0x06000000 03000020 - opaque ?
        //                      ^ offset
        public static Dictionary<string, byte> F3DEX_CMD_NAMES_REV = new Dictionary<string, byte>()
        {
            { "G_SPNOOP",            0x00 },
            { "G_MTX",               0x01 },
            { "G_MOVEMEM",           0x03 },
            { "G_VTX",               0x04 },
            { "G_DL",                0x06 },
            { "G_LOAD_UCODE",        0xAF },
            { "G_BRANCH_Z",          0xB0 },
            { "G_TRI2",              0xB1 },
            { "G_MODIFYVTX",         0xB2 },
            { "G_RDPHALF_2",         0xB3 },
            { "G_QUAD",              0xB5 },
            { "G_CLEARGEOMETRYMODE", 0xB6 },
            { "G_SETGEOMETRYMODE",   0xB7 },
            { "G_ENDDL",             0xB8 },
            { "G_SetOtherMode_L",    0xB9 },
            { "G_SetOtherMode_H",    0xBA },
            { "G_TEXTURE",           0xBB },
            { "G_MOVEWORD",          0xBC },
            { "G_POPMTX",            0xBD },
            { "G_CULLDL",            0xBE },
            { "G_TRI1",              0xBF },
            { "G_NOOP",              0xC0 },
            { "G_TEXRECT",           0xE4 },
            { "G_TEXRECTFLIP",       0xE5 },
            { "G_RDPLOADSYNC",       0xE6 },
            { "G_RDPPIPESYNC",       0xE7 },
            { "G_RDPTILESYNC",       0xE8 },
            { "G_RDPFULLSYNC",       0xE9 },
            { "G_SETKEYGB",          0xEA },
            { "G_SETKEYR",           0xEB },
            { "G_SETCONVERT",        0xEC },
            { "G_SETSCISSOR",        0xED },
            { "G_SETPRIMDEPTH",      0xEE },
            { "G_RDPSetOtherMode",   0xEF },
            { "G_LOADTLUT",          0xF0 },
            { "G_SETTILESIZE",       0xF2 },
            { "G_LOADBLOCK",         0xF3 },
            { "G_LOADTILE",          0xF4 },
            { "G_SETTILE",           0xF5 },
            { "G_FILLRECT",          0xF6 },
            { "G_SETFILLCOLOR",      0xF7 },
            { "G_SETFOGCOLOR",       0xF8 },
            { "G_SETBLENDCOLOR",     0xF9 },
            { "G_SETPRIMCOLOR",      0xFA },
            { "G_SETENVCOLOR",       0xFB },
            { "G_SETCOMBINE",        0xFC },
            { "G_SETTIMG",           0xFD },
            { "G_SETZIMG",           0xFE },
            { "G_SETCIMG",           0xFF }
        };

        // minimally, only 03 and 0D matter. 0C is pretty cool though.
        public static Dictionary<int, string> GEO_CMD_NAMES = new Dictionary<int, String>
        {
            { 0x01, "SORT" },
            { 0x02, "BONE" },
            { 0x03, "LOAD_DL" }, // just starts running a DL (offset is defined in int[2])
            { 0x04, "UNKNOWN_04" },
            { 0x05, "SKINNING" },
            { 0x06, "BRANCH" },
            { 0x07, "UNKNOWN_07" },
            { 0x08, "LOD" },
            { 0x09, "UNKNOWN_09" },
            { 0x0A, "REFERENCE_POINT" },
            { 0x0B, "UNKNOWN_0B" },
            { 0x0C, "SELECTOR" }, // for model-swapping: int[2] holds short[0] child_count, short[1] selector index, then list of children
            // 0000000C 00000060 00040001 00000020 00000030 00000040 00000050 00000000 00000003...
            //                   ^ 4 children
            //                       ^ sel ID = 1
            //                            ^ 1st child: offset 0x20 bytes to next geolayout command
            { 0x0D, "DRAW_DISTANCE" },
            { 0x0E, "UNKNOWN_0E" },
            { 0x0F, "UNKNOWN_0F" },
        };
        public static Dictionary<string, int> GEO_CMD_NAMES_REV = new Dictionary<String, int>
        {
            { "SORT",             0x01 },
            { "BONE",             0x02 },
            { "LOAD_DL",          0x03 },
            { "UNKNOWN_04",       0x04 },
            { "SKINNING",         0x05 },
            { "BRANCH",           0x06 },
            { "UNKNOWN_07",       0x07 },
            { "LOD",              0x08 },
            { "UNKNOWN_09",       0x09 },
            { "REFERENCE_POINT",  0x0A },
            { "UNKNOWN_0B",       0x0B },
            { "SELECTOR",         0x0C },
            { "DRAW_DISTANCE",    0x0D },
            { "UNKNOWN_0E",       0x0E },
            { "UNKNOWN_0F",       0x0F },
        };
    }
}
