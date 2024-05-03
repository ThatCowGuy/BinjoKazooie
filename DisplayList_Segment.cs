using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    // this class is supposed to catch ALL information of a triangle of any type.
    // this sort of catch-all case will make sorting and comparing easier later
    public class FullTriangle : IEquatable<FullTriangle>, IComparable<FullTriangle>
    {
        // parsed properties
        public ushort index_1;
        public ushort index_2;
        public ushort index_3;
        public ushort max_index;
        public ushort min_index;
        public Vtx_Elem vtx_1;
        public Vtx_Elem vtx_2;
        public Vtx_Elem vtx_3;
        public ushort unk_1;
        public ushort floor_type;
        public ushort sound_type;

        public bool collidable;
        public bool visible;

        // inferred properties
        public short assigned_tex_ID = -1;
        public Tex_Data assigned_tex_data;
        public Tex_Meta assigned_tex_meta;
        public FX_Elem assigned_FX;

        public bool Equals(FullTriangle other)
        {
            // compare some simpler attributes first
            if (this.floor_type != other.floor_type) return false;
            if (this.sound_type != other.sound_type) return false;
            if (this.assigned_tex_ID != other.assigned_tex_ID) return false;

            // for my intents and purpose, every cyclic permutation of vtx orders
            // is also making up the same tri, so I need to check them all
            if ( // 123 - 123
                this.vtx_1.Equals(other.vtx_1) &&
                this.vtx_2.Equals(other.vtx_2) &&
                this.vtx_3.Equals(other.vtx_3)
            ) return true;
            if ( // 123 - 231
                this.vtx_1.Equals(other.vtx_2) &&
                this.vtx_2.Equals(other.vtx_3) &&
                this.vtx_3.Equals(other.vtx_1)
            ) return true;
            if ( // 123 - 312
                this.vtx_1.Equals(other.vtx_3) &&
                this.vtx_2.Equals(other.vtx_1) &&
                this.vtx_3.Equals(other.vtx_2)
            ) return true;
            // first tri doesnt need to be permuted, because the resulting checks
            // are already covered by permuting the other tri.
            return false;
        }
        public override bool Equals(Object other)
        {
            return this.Equals((FullTriangle) other);
        }
        public int CompareTo(FullTriangle other)
        {
            // first sort by tex (no tex has tex_ID == -1)
            if (this.assigned_tex_ID > other.assigned_tex_ID) return +1;
            if (this.assigned_tex_ID < other.assigned_tex_ID) return -1;
            // then sort by floor type
            if (this.floor_type > other.floor_type) return +1;
            if (this.floor_type < other.floor_type) return -1;
            // and then sound
            if (this.sound_type > other.sound_type) return +1;
            if (this.sound_type < other.sound_type) return -1;

            // if no difference was found (for whatever reason) we return equality
            return 0;
        }
        public String print()
        {
            return String.Format("TEX-ID:{0}; Coll:{1}", this.assigned_tex_ID, this.floor_type);
        }
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x0C];
            // VTX Indices
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.index_1, 2), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.index_2, 2), bytes, 0x02);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.index_3, 2), bytes, 0x04);
            // Padding
            File_Handler.write_bytes_to_buffer(new byte[2] { 0x00, 0x00 }, bytes, 0x06);
            // Flags
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.floor_type, 2), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.sound_type, 2), bytes, 0x0A);
            return bytes;
        }

        /*/=====================================================
         * Thanks to Unalive for documenting these Flags
        =====================================================/*/
        public Dictionary<int, string> COLLISION_FLAGS = new Dictionary<int, string>
        {
            { 0x0, "" },
            { 0x1, "" },
            { 0x2, "" },
            { 0x3, "Water" },
            { 0x4, "Trottable" },
            { 0x5, "Disc-Rim" },
            { 0x6, "Un-Trottable" },
            { 0x7, "" },
            { 0x8, "~ footstep sfx" },
            { 0x9, "~ footstep sfx" },
            { 0xA, "~ footstep sfx" },
            { 0xB, "~ footstep sfx" },
            { 0xC, "~ footstep sfx" },
            { 0xD, "Damage Floor" },
            { 0xE, "~ Damage func" },
            { 0xF, "~ Damage func" },
        };
        public Dictionary<int, string> SOUND_FLAGS = new Dictionary<int, string>
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

    }
    public class DisplayList_Command
    {
        public DisplayList_Command(ulong encoding)
        {
            this.command_byte = (byte) (encoding >> 56);
            this.command_name = Dicts.F3DEX_CMD_NAMES[this.command_byte];

            this.raw_content = new uint[]
            {
                (uint) ((encoding >> 32) & 0xFFFFFFFF),
                (uint) ((encoding >> 0)  & 0xFFFFFFFF)
            };
            if (encoding != 0x00)
                this.infer_parameters();
        }
        // this is C#'s shorthand for a constructor that calls another constructor...
        public DisplayList_Command() : this(0x00) { }

        public byte command_byte;
        public String command_name;

        public uint[] parameters = new uint[16];

        public uint[] raw_content = new uint[2];

        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x08];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.raw_content[0], 4), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.raw_content[1], 4), bytes, 0x04);
            return bytes;
        }

        public static ulong G_CLEARGEOMETRYMODE(uint flags)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_CLEARGEOMETRYMODE"], 56, 8);
            cmd |= MathHelpers.shift_cut(flags, 0, 32);
            return cmd;
        }
        public static ulong G_SETGEOMETRYMODE(uint flags)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SETGEOMETRYMODE"], 56, 8);
            cmd |= MathHelpers.shift_cut(flags, 0, 32);
            return cmd;
        }
        public static ulong G_TEXTURE(int mipmap_cnt, int descr, Boolean activate, uint scaling_S, uint scaling_T)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_TEXTURE"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) mipmap_cnt, 43, 3);
            cmd |= MathHelpers.shift_cut((ulong) descr, 40, 3);
            cmd |= MathHelpers.shift_cut((ulong) (activate ? 1 : 0), 32, 8); // technically only 1 bit, but the entire byte is reserved
            cmd |= MathHelpers.shift_cut((ulong) scaling_S, 16, 16);
            cmd |= MathHelpers.shift_cut((ulong) scaling_T, 0, 16);
            return cmd;
        }
        public static ulong G_SETTIMG(String format, int bitsize, uint seg_address)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SETTIMG"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) Dicts.SETTILE_COLFORM[format], 53, 3);
            ulong bitsize_transformed = (ulong) Math.Log(bitsize / 4, 2);
            cmd |= MathHelpers.shift_cut(bitsize_transformed, 51, 2);
            ulong addr_transformed = (ulong)((Dicts.INTERNAL_SEG_NAMES_REV["Tex"] << 24) + seg_address);
            cmd |= MathHelpers.shift_cut(addr_transformed, 0, 32);
            return cmd;
        }
        // TMEM for palettes should appearently be 0x0100; for pixel data its 0x0000
        // pal is palette index if parallel-usage is utilized
        // NOTE: S and T are in reverse in the encoding...
        public static ulong G_SETTILE(
            String format, int bitsize, uint width, uint TMEM, int descr, int pal,
            Boolean clamp_S, Boolean mirror_S, int wrap_S, int shift_S,
            Boolean clamp_T, Boolean mirror_T, int wrap_T, int shift_T
        )
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SETTILE"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) Dicts.SETTILE_COLFORM[format], 53, 3);
            ulong bitsize_transformed = (ulong) Math.Log(bitsize / 4, 2);
            cmd |= MathHelpers.shift_cut(bitsize_transformed, 51, 2);
            ulong num64 = (ulong) ((width * bitsize) / 64);
            cmd |= MathHelpers.shift_cut(num64, 41, 9); // there is a bit of padding infront of this, so bit #50 is unused
            cmd |= MathHelpers.shift_cut((ulong) TMEM, 32, 9);
            cmd |= MathHelpers.shift_cut((ulong) descr, 24, 3);
            cmd |= MathHelpers.shift_cut((ulong) pal, 20, 4);
            // T axis
            cmd |= MathHelpers.shift_cut((ulong) (clamp_T ? 1 : 0), 19, 1);
            cmd |= MathHelpers.shift_cut((ulong) (mirror_T ? 1 : 0), 18, 1);
            cmd |= MathHelpers.shift_cut((ulong) wrap_T, 14, 4);
            cmd |= MathHelpers.shift_cut((ulong) shift_T, 10, 4);
            // S axis
            cmd |= MathHelpers.shift_cut((ulong) (clamp_S ? 1 : 0), 9, 1);
            cmd |= MathHelpers.shift_cut((ulong) (mirror_S ? 1 : 0), 8, 1);
            cmd |= MathHelpers.shift_cut((ulong) wrap_S, 4, 4);
            cmd |= MathHelpers.shift_cut((ulong) shift_S, 0, 4);
            return cmd;
        }
        // UL = Upper Left corner of the Texture
        public static ulong G_SETTILESIZE(uint ULx, uint ULy, int descr, uint width, uint height)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SETTILESIZE"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) ULx, 44, 12); // 3 nibbles
            cmd |= MathHelpers.shift_cut((ulong) ULy, 32, 12); // 3 nibbles
            cmd |= MathHelpers.shift_cut((ulong) descr, 24, 8);
            ulong W_transformed = (ulong) (4 * (width - 1));
            ulong H_transformed = (ulong) (4 * (height - 1));
            cmd |= MathHelpers.shift_cut(W_transformed, 12, 12); // 3 nibbles
            cmd |= MathHelpers.shift_cut(H_transformed, 0, 12); // 3 nibbles
            return cmd;
        }
        public static ulong G_LOADTLUT(int descr, uint color_cnt)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_LOADTLUT"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) descr, 24, 8);
            ulong cc_transformed = (ulong) (4 * (color_cnt - 1));
            cmd |= MathHelpers.shift_cut(cc_transformed, 12, 12); // 3 nibbles
            return cmd;
        }
        // the target string is telling which sort of flags are supposed to be changed
        // and is translated to a shift value (and technically to a bitlen val too)
        // modebits is the new bits to set there
        public static ulong G_SetOtherMode_H(String target, uint bitlen, uint modebits)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SetOtherMode_H"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) Dicts.OTHERMODE_H_MDSFT[target], 40, 8);
            cmd |= MathHelpers.shift_cut((ulong) bitlen, 32, 8);
            cmd |= MathHelpers.shift_cut((ulong) modebits, 0, 32);
            return cmd;
        }
        public static ulong G_LOADBLOCK(uint ULx, uint ULy, int descr, uint width, uint height, uint texel_bitsize)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_LOADBLOCK"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) ULx, 44, 12); // 3 nibbles
            cmd |= MathHelpers.shift_cut((ulong) ULy, 32, 12); // 3 nibbles
            cmd |= MathHelpers.shift_cut((ulong) descr, 24, 8);
            // NOTE: dont forget to substract -1; idk if this has a technical reason, but interestingly
            //       the resolution 64/64 is 0x40/0x40 which has 0x1000 texels, but the texel_cnt slot
            //       in this command can only take 12 bits, or 3 nibbles (0x???). So maybe this -1 is meant
            //       to get 64/64 resolutions to work with 12 bits, as 0x1000 - 1 = 0xFFF...
            ulong texel_cnt = (ulong) ((width * height) - 1);
            cmd |= MathHelpers.shift_cut(texel_cnt, 12, 12); // 3 nibbles
            ulong DXT = MathHelpers.calc_DXT(width, texel_bitsize);
            cmd |= MathHelpers.shift_cut(DXT, 0, 12); // 3 nibbles
            return cmd;
        }
        public static ulong G_RDPPIPESYNC()
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_RDPPIPESYNC"], 56, 8);
            return cmd;
        }
        // Im copping out hard here; The Game appears to only use a couple of distinct
        // parameter sets for this command, so instead of going through the struggle of
        // properly building it, I will derive the to-be-used static parameter set from
        // a heavily reduced set of input parameters
        public static ulong G_SETCOMBINE()
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_SETCOMBINE"], 56, 8);
            cmd |= MathHelpers.shift_cut(0x00129804, 32, 24);
            cmd |= MathHelpers.shift_cut(0x3F15FFFF, 0,  32);
            return cmd;
        }
        // remember to always call the setup-DL thats statically stored in seg-3:
        // G_DL(false, "Mode", 0x20);
        public static ulong G_DL(Boolean final, String segment, int seg_address)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_DL"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) (final ? 1 : 0), 48, 8);
            cmd |= MathHelpers.shift_cut((ulong) Dicts.INTERNAL_SEG_NAMES_REV[segment], 24, 8);
            cmd |= MathHelpers.shift_cut((ulong) seg_address, 0, 24);
            return cmd;
        }
        // NOTE: G_VTX loads a continuous array of vertices into the buffer
        public static ulong G_VTX(uint buffer_target, uint vtx_cnt, uint vtx_start_id)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_VTX"], 56, 8);
            cmd |= MathHelpers.shift_cut((ulong) (2 * buffer_target), 48, 8); // reminder that the buffer id is doubled in encoding
            cmd |= MathHelpers.shift_cut((ulong) vtx_cnt, 42, 6);
            ulong mem_size = (ulong) ((vtx_cnt * 0x10) - 1); // dunno why, but the F3DEX wants that -1 here
            cmd |= MathHelpers.shift_cut(mem_size, 32, 10);
            cmd |= MathHelpers.shift_cut((ulong) Dicts.INTERNAL_SEG_NAMES_REV["VTX"], 24, 8);
            ulong seg_address = (ulong) (vtx_start_id * 0x10);
            cmd |= MathHelpers.shift_cut(seg_address, 0, 24);
            return cmd;
        }
        // NOTE: all the IDs here and in TRI2 are stored doubled-ly
        public static ulong G_TRI1(uint id_A1, uint id_A2, uint id_A3)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_TRI1"], 56, 8);
            // A is stored in the back
            cmd |= MathHelpers.shift_cut((id_A1 * 2), 16, 8);
            cmd |= MathHelpers.shift_cut((id_A2 * 2), 8,  8);
            cmd |= MathHelpers.shift_cut((id_A3 * 2), 0,  8);
            return cmd;
        }
        public static ulong G_TRI2(uint id_A1, uint id_A2, uint id_A3, uint id_B1, uint id_B2, uint id_B3)
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_TRI2"], 56, 8);
            // A is stored in the FRONT
            cmd |= MathHelpers.shift_cut((id_A1 * 2), 48, 8);
            cmd |= MathHelpers.shift_cut((id_A2 * 2), 40, 8);
            cmd |= MathHelpers.shift_cut((id_A3 * 2), 32, 8);
            // B is stored in the back
            cmd |= MathHelpers.shift_cut((id_B1 * 2), 16, 8);
            cmd |= MathHelpers.shift_cut((id_B2 * 2), 8,  8);
            cmd |= MathHelpers.shift_cut((id_B3 * 2), 0,  8);
            return cmd;
        }
        public static ulong G_ENDDL()
        {
            ulong cmd = 0x00;
            cmd |= MathHelpers.shift_cut(Dicts.F3DEX_CMD_NAMES_REV["G_ENDDL"], 56, 8);
            return cmd;
        }

        //    |     v48     v32|     v16     v0|
        //    |  v56     v40   |  v24     v8   |
        // 0x | 00 00   00 00  | 00 00   00 00 |

        // this function uses the command byte and the raw encoding to infer all the associated parameters of the command
        public void infer_parameters()
        {
            this.parameters = new uint[16];
            switch (this.command_name)
            {
                case ("G_SETCOMBINE"):
                    break;

                case ("G_LOADBLOCK"):
                    // UL corner S coord
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1111_1111__1111_0000_0000_0000);
                    // UL corner T coord
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_1111_1111_1111);
                    // tile descriptor
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // Texel count - 1
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_0000_0000_0000);
                    this.parameters[3] = (this.parameters[3] + 1);
                    // DXT (this is a really messy one:
                    // "dxt is an unsigned fixed-point 1.11 [11 digit mantissa] number"
                    // "dxt is the RECIPROCAL of the number of 64-bit chunks it takes to get a row of texture"
                    // an example: Take a 32x32 px Tex with 16b colors;
                    // -> a row of that Tex takes 32x16b = 512b
                    // -> so it needs (512b/64b) = 8 chunks of 64b to create a row
                    // -> the reciprocal is 1/8, which in binary is 0.001_0000_0000 = 0x100
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_1111_1111_1111);
                    break;

                case ("G_SETTILESIZE"):
                    // UL corner S coord
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1111_1111__1111_0000_0000_0000);
                    // UL corner T coord
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_1111_1111_1111);
                    // tile descriptor
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // (Width - 1) * 4
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_0000_0000_0000);
                    this.parameters[3] = (this.parameters[3] / 4) + 1;
                    // (Height - 1) * 4
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_1111_1111_1111);
                    this.parameters[4] = (this.parameters[4] / 4) + 1;
                    break;

                // 0E 02 = set TLUT (texel lookup table) color format
                //       0000 0000 = no TLUT at all (tex formats RGBA16/32 + IA8)
                //       0000 8000 = TLUT type = RGBA16 (tex formats CI4/8)
                case ("G_SetOtherMode_H"):
                    // shift
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__1111_1111_0000_0000);
                    // num affected bits
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0000_1111_1111);
                    // new mode-bits
                    this.parameters[2] = this.raw_content[1];
                    break;

                case ("G_G_SETGEOMETRYMODE"):
                    // RSP flags to enable
                    this.parameters[0] = this.raw_content[1];
                    break;
                case ("G_G_CLEARGEOMETRYMODE"):
                    // RSP flags to disable
                    this.parameters[0] = this.raw_content[1];
                    break;

                case ("G_TEXTURE"):
                    // maximum number of mipmaps
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0011_1000_0000_0000);
                    // affected tile descripter index
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0111_0000_0000);
                    // enable tile descriptor
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0000_1111_1111);
                    // S Axis scale factor (horizontal)
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_1111_1111__0000_0000_0000_0000);
                    // T Axis scale factor (vertical)
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__1111_1111_1111_1111);
                    break;

                case ("G_SETTIMG"):
                    // color storage format
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1110_0000__0000_0000_0000_0000);
                    // color storage bit-size
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0001_1000__0000_0000_0000_0000);
                    // data segment num
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // data offset (removing the leading byte, because thats caught in the param before)
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_1111_1111_1111);
                    break;

                case ("G_SETTILE"):
                    // color storage format
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1110_0000__0000_0000_0000_0000);
                    // color storage bit-size
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0001_1000__0000_0000_0000_0000);
                    // num of 64bit vals per row
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0011__1111_1110_0000_0000);
                    // TMEM offset of texture
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0001_1111_1111);
                    // target tile descriptor
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0111_0000_0000__0000_0000_0000_0000);
                    // corresponding palette
                    this.parameters[5] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_0000__0000_0000_0000_0000);
                    // T clamp
                    this.parameters[6] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_1000__0000_0000_0000_0000);
                    // T mirror
                    this.parameters[7] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0100__0000_0000_0000_0000);
                    // T wrap (this one made me decide to split this command into 2 ints, rather than 4 shorts...)
                    this.parameters[8] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0011__1100_0000_0000_0000);
                    // T shift
                    this.parameters[9] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0011_1100_0000_0000);
                    // S clamp
                    this.parameters[10] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0010_0000_0000);
                    // S mirror
                    this.parameters[11] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0001_0000_0000);
                    // S wrap
                    this.parameters[12] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0000_1111_0000);
                    // S shift
                    this.parameters[13] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0000_0000_1111);
                    break;

                case ("G_LOADTLUT"):
                    // affected tile descripter index
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // quadrupled color count (-1)
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_0000_0000_0000);
                    this.parameters[1] = (this.parameters[1] / 4) + 1;
                    break;

                case ("G_DL"):
                    // remember return address (which identifies that this is not the end)
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1111_1111__0000_0000_0000_0000);
                    // data segment num
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // data offset (removing the leading byte, because thats caught in the param before)
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_1111_1111_1111);
                    break;

                case ("G_VTX"):
                    // vertex buffer target location (doubled)
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1111_1111__0000_0000_0000_0000);
                    this.parameters[0] = (this.parameters[0] / 2);
                    // vertex count
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__1111_1100_0000_0000);
                    // vertex sotrage size
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0011_1111_1111);
                    // data segment num
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_1111_1111_0000_0000__0000_0000_0000_0000);
                    // data offset (removing the leading byte, because thats caught in the param before)
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__1111_1111_1111_1111);
                    break;

                case ("G_TRI1"):
                    // vertex buffer tri_B_v1 location (doubled)
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__0000_0000_0000_0000);
                    this.parameters[0] = (this.parameters[0] / 2);
                    // vertex buffer tri_B_v2 location (doubled)
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__1111_1111_0000_0000);
                    this.parameters[1] = (this.parameters[1] / 2);
                    // vertex buffer tri_B_v3 location (doubled)
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0000_1111_1111);
                    this.parameters[2] = (this.parameters[2] / 2);
                    break;

                case ("G_TRI2"):
                    // vertex buffer tri_A_v1 location (doubled)
                    this.parameters[0] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_1111_1111__0000_0000_0000_0000);
                    this.parameters[0] = (this.parameters[0] / 2);
                    // vertex buffer tri_A_v2 location (doubled)
                    this.parameters[1] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__1111_1111_0000_0000);
                    this.parameters[1] = (this.parameters[1] / 2);
                    // vertex buffer tri_A_v3 location (doubled)
                    this.parameters[2] = File_Handler.apply_bitmask(this.raw_content[0], 0b_0000_0000_0000_0000__0000_0000_1111_1111);
                    this.parameters[2] = (this.parameters[2] / 2);

                    // vertex buffer tri_B_v1 location (doubled)
                    this.parameters[3] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_1111_1111__0000_0000_0000_0000);
                    this.parameters[3] = (this.parameters[3] / 2);
                    // vertex buffer tri_B_v2 location (doubled)
                    this.parameters[4] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__1111_1111_0000_0000);
                    this.parameters[4] = (this.parameters[4] / 2);
                    // vertex buffer tri_B_v3 location (doubled)
                    this.parameters[5] = File_Handler.apply_bitmask(this.raw_content[1], 0b_0000_0000_0000_0000__0000_0000_1111_1111);
                    this.parameters[5] = (this.parameters[5] / 2);
                    break;

                case ("G_ENDDL"):
                case ("G_RDPLOADSYNC"):
                case ("G_RDPPIPESYNC"):
                case ("G_RDPTILESYNC"):
                case ("G_RDPFULLSYNC"):
                    // no additional params
                    break;
            }
        }



        public static String get_affected_flags(uint value)
        {
            String flagnames = "";
            foreach (int key in Dicts.RSP_GEOMODE_FLAGS_REV.Keys)
            {
                if ((value & key) > 0)
                    // NOTE: Im cutting out the G_ here because its redundant
                    flagnames += Dicts.RSP_GEOMODE_FLAGS_REV[key].Substring(2) + ", ";
            }
            return flagnames;
        }
    }

    public class Tile_Descriptor
    {
        public ushort color_storage_format;
        public ushort color_storage_bitsize;
        public ushort bitvals_per_row;
        public ushort TMEM_tex_offset;

        public ushort corresponding_palette;
        public Boolean T_clamp;
        public Boolean T_mirror;
        public double T_wrap;
        public double T_shift;
        public Boolean S_clamp;
        public Boolean S_mirror;
        public double S_wrap;
        public double S_shift;

        public ushort assigned_tex_ID;
        public Tex_Meta assigned_tex_meta;
        public Tex_Data assigned_tex_data;
    }

    public class DisplayList_Segment
    {
        public bool valid = false;


        // parsed properties
        // === 0x00 ===============================
        public uint command_cnt;
        public uint padding;

        public DisplayList_Command[] command_list;
        public int[] simulated_vtx_buffer;

        // locators
        public uint file_offset;

        // inferred properties
        public uint visual_vtx_count;
        public uint visual_tri_count;
        public List<Vtx_Elem> visual_vtx_list;
        public List<Tri_Elem> visual_tri_list;

        public String export_TRI_IDs_to_Base64()
        {
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.visual_tri_list.Count; i++)
            {
                Tri_Elem vis_tri = this.visual_tri_list[i];
                raw_data.AddRange(BitConverter.GetBytes((ushort) vis_tri.index_1));
                raw_data.AddRange(BitConverter.GetBytes((ushort) vis_tri.index_2));
                raw_data.AddRange(BitConverter.GetBytes((ushort) vis_tri.index_3));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
        }
        public String export_VTX_Coords_to_Base64()
        {
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.visual_vtx_list.Count; i++)
            {
                Vtx_Elem vtx = this.visual_vtx_list[i];
                raw_data.AddRange(BitConverter.GetBytes((Single) vtx.x));
                raw_data.AddRange(BitConverter.GetBytes((Single) vtx.y));
                raw_data.AddRange(BitConverter.GetBytes((Single) vtx.z));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
        }
        public String export_UV_Coords_to_Base64()
        {
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.visual_vtx_list.Count; i++)
            {
                Vtx_Elem vtx = this.visual_vtx_list[i];
                raw_data.AddRange(BitConverter.GetBytes(vtx.transformed_U));
                raw_data.AddRange(BitConverter.GetBytes(vtx.transformed_V));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
        }
        public void populate(byte[] file_data, int file_offset, Texture_Segment tex_seg, Vertex_Segment vtx_seg, List<FullTriangle> full_tri_list)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No DisplayList Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint)file_offset;

            this.visual_vtx_list = new List<Vtx_Elem>();
            this.visual_tri_list = new List<Tri_Elem>();
            this.visual_vtx_count = 0;
            this.visual_tri_count = 0;

            // storing the vtx IDs as loaded by the DLs
            int[] simulated_vtx_buffer = new int[0x20];
            Vtx_Elem vis_vtx;
            Tri_Elem vis_tri;
            FullTriangle full_tri;
            Tile_Descriptor[] tile_descriptor = new Tile_Descriptor[]
            {
                new Tile_Descriptor(),
                new Tile_Descriptor(),
                new Tile_Descriptor()
            };

            // parsing properties
            // === 0x00 ===============================
            this.command_cnt = File_Handler.read_int(file_data, file_offset + 0x00, false);
            this.padding = File_Handler.read_int(file_data, file_offset + 0x04, false);

            this.command_list = new DisplayList_Command[this.command_cnt];
            for (int i = 0; i < this.command_cnt; i++)
            {
                DisplayList_Command cmd = new DisplayList_Command();
                int file_offset_cmd = (int)(this.file_offset + 0x08 + (i * 0x08));

                cmd.raw_content[0] = File_Handler.read_int(file_data, file_offset_cmd + 0x00, false);
                cmd.raw_content[1] = File_Handler.read_int(file_data, file_offset_cmd + 0x04, false);

                // parsing properties
                // === 0x00 ===============================
                cmd.command_byte = File_Handler.read_char(file_data, file_offset_cmd + 0x00, false);
                cmd.command_name = Dicts.F3DEX_CMD_NAMES[cmd.command_byte];
                cmd.infer_parameters();

                uint tmp;
                // and additionally, Im running over the command switch again, to simulate the descriptors
                // to build up the parsed full-tri list
                switch (cmd.command_name)
                {
                    case ("G_SETCOMBINE"):
                    case ("G_LOADBLOCK"):
                    case ("G_SETTILESIZE"):
                    case ("G_SetOtherMode_H"):
                    case ("G_G_SETGEOMETRYMODE"):
                    case ("G_G_CLEARGEOMETRYMODE"):
                    case ("G_TEXTURE"):
                    case ("G_SETTILE"):
                    case ("G_LOADTLUT"):
                    case ("G_DL"):
                    case ("G_ENDDL"):
                    case ("G_RDPLOADSYNC"):
                    case ("G_RDPPIPESYNC"):
                    case ("G_RDPTILESYNC"):
                    case ("G_RDPFULLSYNC"):
                        break;

                    case ("G_SETTIMG"):
                        // find the tex that corresponds to this address (and only update the descriptor if it was actual data)
                        if (tex_seg.get_tex_ID_from_datasection_offset(cmd.parameters[3]) != 0xFFFF)
                        {
                            tile_descriptor[0].assigned_tex_ID = (ushort) tex_seg.get_tex_ID_from_datasection_offset(cmd.parameters[3]);
                            tile_descriptor[0].assigned_tex_data = tex_seg.data[tile_descriptor[0].assigned_tex_ID];
                            tile_descriptor[0].assigned_tex_meta = tex_seg.meta[tile_descriptor[0].assigned_tex_ID];
                        }
                        break;

                    case ("G_VTX"):
                        uint vtx_cnt = cmd.parameters[1];
                        // since this is the actual offset, I'll calculate the vtx ID manually
                        uint offset = (cmd.parameters[4] / 0x10);
                        // write the corresponding vtx into the simulated buffer
                        for (int k = 0; k < vtx_cnt; k++)
                        {
                            simulated_vtx_buffer[cmd.parameters[0] + k] = (int) (offset + k);
                        }
                        break;

                    case ("G_TRI1"):
                        // first, check if the tri also exists as a collision tri
                        full_tri = new FullTriangle();
                        full_tri.index_1 = (ushort) simulated_vtx_buffer[cmd.parameters[0]];
                        full_tri.index_2 = (ushort) simulated_vtx_buffer[cmd.parameters[1]];
                        full_tri.index_3 = (ushort) simulated_vtx_buffer[cmd.parameters[2]];
                        full_tri.vtx_1 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[0]]].Clone();
                        full_tri.vtx_2 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[1]]].Clone();
                        full_tri.vtx_3 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[2]]].Clone();
                        if (full_tri_list.Contains(full_tri) == true)
                        {
                            // if we found one, we can work with that
                            full_tri = full_tri_list.Find(item => full_tri.Equals(item));
                            // do some inferrments without adding it to the list AGAIN
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                        }
                        else
                        {
                            // otherwise, we need to build a new one
                            full_tri.index_1 = (ushort) ((full_tri_list.Count * 3) + 0);
                            full_tri.index_2 = (ushort) ((full_tri_list.Count * 3) + 1);
                            full_tri.index_3 = (ushort) ((full_tri_list.Count * 3) + 2);
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                            // and ADD it to the list !
                            full_tri_list.Add(full_tri);
                        }
                        break;

                    case ("G_TRI2"):
                        // first, check if the tri also exists as a collision tri
                        full_tri = new FullTriangle();
                        full_tri.index_1 = (ushort) simulated_vtx_buffer[cmd.parameters[0]];
                        full_tri.index_2 = (ushort) simulated_vtx_buffer[cmd.parameters[1]];
                        full_tri.index_3 = (ushort) simulated_vtx_buffer[cmd.parameters[2]];
                        full_tri.vtx_1 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[0]]].Clone();
                        full_tri.vtx_2 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[1]]].Clone();
                        full_tri.vtx_3 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[2]]].Clone();
                        if (full_tri_list.Contains(full_tri) == true)
                        {
                            // if we found one, we can work with that
                            full_tri = full_tri_list.Find(item => full_tri.Equals(item));
                            // do some inferrments without adding it to the list AGAIN
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                        }
                        else
                        {
                            // otherwise, we need to build a new one
                            full_tri.index_1 = (ushort) ((full_tri_list.Count * 3) + 0);
                            full_tri.index_2 = (ushort) ((full_tri_list.Count * 3) + 1);
                            full_tri.index_3 = (ushort) ((full_tri_list.Count * 3) + 2);
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                            // and ADD it to the list !
                            full_tri_list.Add(full_tri);
                        }
                        // REPEAT FOR TRI-2
                        full_tri = new FullTriangle();
                        full_tri.index_1 = (ushort) simulated_vtx_buffer[cmd.parameters[3]];
                        full_tri.index_2 = (ushort) simulated_vtx_buffer[cmd.parameters[4]];
                        full_tri.index_3 = (ushort) simulated_vtx_buffer[cmd.parameters[5]];
                        full_tri.vtx_1 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[3]]].Clone();
                        full_tri.vtx_2 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[4]]].Clone();
                        full_tri.vtx_3 = (Vtx_Elem) vtx_seg.vtx_list[simulated_vtx_buffer[cmd.parameters[5]]].Clone();
                        if (full_tri_list.Contains(full_tri) == true)
                        {
                            // if we found one, we can work with that
                            full_tri = full_tri_list.Find(item => full_tri.Equals(item));
                            // do some inferrments without adding it to the list AGAIN
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                        }
                        else
                        {
                            // otherwise, we need to build a new one
                            full_tri.index_1 = (ushort) ((full_tri_list.Count * 3) + 0);
                            full_tri.index_2 = (ushort) ((full_tri_list.Count * 3) + 1);
                            full_tri.index_3 = (ushort) ((full_tri_list.Count * 3) + 2);
                            full_tri.vtx_1.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_2.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.vtx_3.calc_transformed_UVs(tile_descriptor[0]);
                            full_tri.assigned_tex_ID = (short) tile_descriptor[0].assigned_tex_ID;
                            full_tri.assigned_tex_meta = tile_descriptor[0].assigned_tex_meta;
                            full_tri.assigned_tex_data = tile_descriptor[0].assigned_tex_data;
                            // and ADD it to the list !
                            full_tri_list.Add(full_tri);
                        }
                        break;
                }
                this.command_list[i] = cmd;
            }
        }

        public List<string[]> get_content()
        {
            List<string[]> content = new List<string[]>();
            if (this.valid == false)
            {
                content.Add(new string[] { "No Data" });
                return content;
            }

            content.Add(new string[] {
                "File Offset",
                File_Handler.uint_to_string(this.file_offset, 0xFFFFFFFF),
                "",
                "from File Start"
            });
            content.Add(new string[] {
                "Command Count",
                File_Handler.uint_to_string(this.command_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.command_cnt, 10),
                ""
            });

            return content;
        }

        public List<string[]> get_cmd_content(int id)
        {
            DisplayList_Command cmd = this.command_list[id];
            List<string[]> content = new List<string[]>();

            // building the info string at the end.. ughhh
            String details = "";
            switch (cmd.command_name)
            {
                case ("G_SETCOMBINE"):
                    // FC 12 7F FF FF FF F8 38 : Standard usage for solid RGBA textures
                    // FC 12 18 24 FF 33 FF FF : Standard usage for alpha RGBA textures
                    // FC269804 1F14FFFF -- G_SETCOMBINE
                    // FC129804 3F15FFFF -- G_SETCOMBINE
                    details += File_Handler.uint_to_string(cmd.raw_content[0], 0xFFFFFFFF) + " ";
                    details += File_Handler.uint_to_string(cmd.raw_content[1], 0xFFFFFFFF) + " (raw hex)";
                    break;

                case ("G_LOADBLOCK"):
                    details += String.Format("UL=({0};{1}), ",
                        File_Handler.uint_to_string(cmd.parameters[0], 0xFFFF),
                        File_Handler.uint_to_string(cmd.parameters[1], 0xFFFF)
                    );
                    details += String.Format("descr=#{0}, ", cmd.parameters[2]);
                    details += String.Format("texelCNT={0}, ", File_Handler.uint_to_string(cmd.parameters[3], 0xFFFF));
                    details += String.Format("DXT={0}", File_Handler.uint_to_string(cmd.parameters[4], 0xFFFF));
                    break;

                case ("G_SETTILESIZE"):
                    details += String.Format("UL=({0};{1}), ",
                        File_Handler.uint_to_string(cmd.parameters[0], 0xFFFF),
                        File_Handler.uint_to_string(cmd.parameters[1], 0xFFFF)
                    );
                    details += String.Format("descr=#{0}, ", cmd.parameters[2]);
                    details += String.Format("dim={0}x{1} px",
                        File_Handler.uint_to_string(cmd.parameters[3], 10),
                        File_Handler.uint_to_string(cmd.parameters[4], 10)
                    );
                    break;

                case ("G_SETTILE"):
                    details += String.Format("fmt={0}, ", Dicts.SETTILE_COLFORM_REV[(int) cmd.parameters[0]]);
                    details += String.Format("siz={0}b, ", (4 * Math.Pow(2, cmd.parameters[1])));
                    details += String.Format("64num={0}, ", cmd.parameters[2]);
                    details += String.Format("TexTMEM@{0}, ", File_Handler.uint_to_string(cmd.parameters[3], 0xFFFF));

                    details += String.Format("descr=#{0}, ", cmd.parameters[4]);
                    details += String.Format("pal={0}, ", cmd.parameters[5]);

                    details += String.Format("T:{0},{1},{2},{3}; ",
                        cmd.parameters[6],
                        cmd.parameters[7],
                        File_Handler.uint_to_string(cmd.parameters[8], 0xF),
                        File_Handler.uint_to_string(cmd.parameters[9], 0xF)
                    );
                    details += String.Format("S:{0},{1},{2},{3}",
                        cmd.parameters[10],
                        cmd.parameters[11],
                        File_Handler.uint_to_string(cmd.parameters[12], 0xF),
                        File_Handler.uint_to_string(cmd.parameters[13], 0xF)
                    );
                    break;

                // 0E 02 = set TLUT (texel lookup table) color format
                //       0000 0000 = no TLUT at all (tex formats RGBA16/32 + IA8)
                //       0000 8000 = TLUT type = RGBA16 (tex formats CI4/8)
                case ("G_SetOtherMode_H"):
                    // the shft is basically determining what we are touching (the bit_cnt is neglible for information):
                    // NOTE: Im cutting out the G_MDSFT_ part because its redundant here
                    details += String.Format("shft={0}, ", Dicts.OTHERMODE_H_MDSFT_REV[(int) cmd.parameters[0]].Substring(8));
                    //details += String.Format("bit_cnt={0}, ", cmd.parameters[1]);
                    details += String.Format("bits={0}", File_Handler.uint_to_string(cmd.parameters[2], 0xFFFFFFFF));
                    break;

                case ("G_G_SETGEOMETRYMODE"):
                    // RSP flags to enable
                    details += DisplayList_Command.get_affected_flags(cmd.parameters[0]);
                    break;
                case ("G_G_CLEARGEOMETRYMODE"):
                    // RSP flags to disable
                    details += DisplayList_Command.get_affected_flags(cmd.parameters[0]);
                    break;

                case ("G_TEXTURE"):
                    details += String.Format("m={0}, ", cmd.parameters[0]);
                    details += String.Format("descr=#{0}, ", cmd.parameters[1]);
                    details += String.Format("y/n={0}, ", cmd.parameters[2]);
                    details += "Ssc=" + File_Handler.uint_to_string(cmd.parameters[3], 0xFFFF) + ", ";
                    details += "Tsc=" + File_Handler.uint_to_string(cmd.parameters[4], 0xFFFF);
                    break;

                case ("G_SETTIMG"):
                    details += "type=";
                    switch (cmd.parameters[0])
                    {
                        case (0x00):
                            details += "RGBA"; break;
                        case (0x01):
                            details += "YUV"; break;
                        case (0x02):
                            details += "CI"; break;
                        case (0x03):
                            details += "IA"; break;
                        case (0x04):
                            details += "I"; break;
                        default:
                            details += "??"; break;
                    }
                    details += ", ";
                    details += String.Format("size={0} b, ", (4 * Math.Pow(2, cmd.parameters[1])));
                    details += String.Format("seg={0}, ", Dicts.INTERNAL_SEG_NAMES[(int) cmd.parameters[2]]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[3], 0xFFFFFFFF);
                    break;

                case ("G_LOADTLUT"):
                    details += String.Format("descr=#{0}, ", cmd.parameters[0]);
                    details += String.Format("color_cnt={0}, ", cmd.parameters[1]);
                    break;

                case ("G_DL"):
                    details += String.Format("final?={0}, ", cmd.parameters[0]);
                    details += String.Format("seg={0}, ", Dicts.INTERNAL_SEG_NAMES[(int) cmd.parameters[1]]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[2], 0xFFFFFFFF);
                    break;

                case ("G_VTX"):
                    details += String.Format("buf@={0}, ", cmd.parameters[0]);
                    details += String.Format("cnt={0}, ", cmd.parameters[1]);
                    details += String.Format("siz={0} B, ", cmd.parameters[2]);
                    details += String.Format("seg={0}, ", Dicts.INTERNAL_SEG_NAMES[(int) cmd.parameters[3]]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[4], 0xFFFFFFFF);
                    break;

                case ("G_TRI1"):
                    details += "tri_B=( ";
                    details += String.Format("{0}, ", cmd.parameters[0]);
                    details += String.Format("{0}, ", cmd.parameters[1]);
                    details += String.Format("{0} ", cmd.parameters[2]);
                    details += "), ";
                    break;

                case ("G_TRI2"):
                    details += "tri_A=( ";
                    details += String.Format("{0}, ", cmd.parameters[0]);
                    details += String.Format("{0}, ", cmd.parameters[1]);
                    details += String.Format("{0} ", cmd.parameters[2]);
                    details += "), ";
                    details += "tri_B=( ";
                    details += String.Format("{0}, ", cmd.parameters[3]);
                    details += String.Format("{0}, ", cmd.parameters[4]);
                    details += String.Format("{0} ", cmd.parameters[5]);
                    details += ")";
                    break;

                case ("G_ENDDL"):
                    details = "ENDING";
                    break;
                case ("G_RDPLOADSYNC"):
                    details = "Waiting for Texture load...";
                    break;
                case ("G_RDPPIPESYNC"):
                    details = "Waiting for RDP Primitive Rendering...";
                    break;
                case ("G_RDPTILESYNC"):
                    details = "Waiting for RDP Rendering...";
                    break;
                case ("G_RDPFULLSYNC"):
                    details = "Waiting for RDP entirely...";
                    break;
            }

            content.Add(new string[] {
                File_Handler.uint_to_string(cmd.command_byte, 0xFF),
                cmd.command_name,
                details
            });

            return content;
        }
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x08];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(this.command_cnt, 4), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes(0x0000000000, 4), bytes, 0x04);

            foreach (DisplayList_Command cmd in this.command_list)
            {
                bytes = File_Handler.concat_arrays(bytes, cmd.get_bytes());
            }
            return bytes;
        }
    }
}