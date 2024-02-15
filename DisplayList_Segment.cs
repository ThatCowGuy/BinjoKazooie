using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    // this class is supposed to catch ALL information of a triangle of any type.
    // this sort of catch-all case will make sorting and comparing easier later
    public class FullTriangle : IEquatable<FullTriangle>, IComparable<FullTriangle>
    {
        // parsed properties
        public ushort index_1;
        public ushort index_2;
        public ushort index_3;
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
        public byte command_byte;
        public String command_name;

        public uint[] parameters = new uint[16];
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

        public static Dictionary<byte, string> F3DEX_Command_Names = new Dictionary<byte, string>()
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

                // parsing properties
                // === 0x00 ===============================
                cmd.command_byte = File_Handler.read_char(file_data, file_offset_cmd + 0x00, false);
                cmd.command_name = DisplayList_Segment.F3DEX_Command_Names[cmd.command_byte];

                uint tmp;
                switch (cmd.command_name)
                {
                    case ("G_TEXTURE"):
                        tmp = File_Handler.read_char(file_data, file_offset_cmd + 0x02, false);
                        // maximum number of mipmaps
                        cmd.parameters[0] = ((tmp & 0b00111000) >> 3);
                        // affected tile descripter index
                        cmd.parameters[1] = ((tmp & 0b00000111) >> 0);
                        // enable tile descriptor
                        cmd.parameters[2] = File_Handler.read_char(file_data, file_offset_cmd + 0x03, false);
                        // S Axis scale factor (horizontal)
                        cmd.parameters[3] = File_Handler.read_short(file_data, file_offset_cmd + 0x04, false);
                        // T Axis scale factor (vertical)
                        cmd.parameters[4] = File_Handler.read_short(file_data, file_offset_cmd + 0x06, false);
                        break;

                    case ("G_SETTIMG"):
                        tmp = File_Handler.read_char(file_data, file_offset_cmd + 0x01, false);
                        // color storage format
                        cmd.parameters[0] = ((tmp & 0b11100000) >> 5);
                        // color storage bit-size
                        cmd.parameters[1] = ((tmp & 0b00011000) >> 3);
                        // data segment num
                        cmd.parameters[2] = File_Handler.read_char(file_data, file_offset_cmd + 0x04, false);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[3] = File_Handler.read_int(file_data, file_offset_cmd + 0x04, false) & 0x00FFFFF;
                        //================================
                        ///// SIMULATE DESCRIPTORS ///////
                        //================================
                        // find the tex that corresponds to this address (and only update the descriptor if it was actual data)
                        if (tex_seg.get_tex_ID_from_datasection_offset(cmd.parameters[3]) != 0xFFFF)
                        {
                            tile_descriptor[0].assigned_tex_ID = (ushort) tex_seg.get_tex_ID_from_datasection_offset(cmd.parameters[3]);
                            tile_descriptor[0].assigned_tex_data = tex_seg.data[tile_descriptor[0].assigned_tex_ID];
                            tile_descriptor[0].assigned_tex_meta = tex_seg.meta[tile_descriptor[0].assigned_tex_ID];
                        }
                        break;

                    case ("G_SETTILE"):
                        tmp = File_Handler.read_int(file_data, file_offset_cmd + 0x00, false);
                        // color storage format
                        cmd.parameters[0] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_1110_0000__0000_0000_0000_0000);
                        // color storage bit-size
                        cmd.parameters[1] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0001_1000__0000_0000_0000_0000);
                        // num of 64bit vals per row
                        cmd.parameters[2] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0011__1111_1110_0000_0000);
                        // TMEM offset of texture
                        cmd.parameters[3] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0001_1111_1111);

                        tmp = File_Handler.read_int(file_data, file_offset_cmd + 0x04, false);
                        // target tile descriptor
                        cmd.parameters[4] = File_Handler.apply_bitmask(tmp, 0b_0000_0111_0000_0000__0000_0000_0000_0000);
                        // corresponding palette
                        cmd.parameters[5] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_1111_0000__0000_0000_0000_0000);
                        // T clamp
                        cmd.parameters[6] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_1000__0000_0000_0000_0000);
                        // T mirror
                        cmd.parameters[7] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0100__0000_0000_0000_0000);
                        // T wrap (this one made me decide to split this command into 2 ints, rather than 4 shorts...)
                        cmd.parameters[8] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0011__1100_0000_0000_0000);
                        // T shift
                        cmd.parameters[9] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0011_1100_0000_0000);
                        // S clamp
                        cmd.parameters[10] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0010_0000_0000);
                        // S mirror
                        cmd.parameters[11] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0001_0000_0000);
                        // S wrap
                        cmd.parameters[12] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0000_1111_0000);
                        // S shift
                        cmd.parameters[13] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0000_0000_1111);
                        break;

                    case ("G_LOADTLUT"):
                        // affected tile descripter index
                        cmd.parameters[0] = File_Handler.read_char(file_data, file_offset_cmd + 0x04, false);
                        // quadrupled color count (-1)
                        cmd.parameters[1] = (File_Handler.read_int(file_data, file_offset_cmd + 0x04, false) & 0x00FFF000) >> 0xC;
                        cmd.parameters[1] = (cmd.parameters[1] / 4) + 1;
                        break;

                    case ("G_DL"):
                        // remember return address (which identifies that this is not the end)
                        cmd.parameters[0] = File_Handler.read_char(file_data, file_offset_cmd + 0x01, false);
                        // data segment num
                        cmd.parameters[1] = File_Handler.read_char(file_data, file_offset_cmd + 0x04, false);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[2] = File_Handler.read_int(file_data, file_offset_cmd + 0x04, false) & 0x00FFFFF;
                        break;

                    case ("G_VTX"):
                        // vertex buffer target location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x01, false) / 2);
                        // vertex count to load + size of vertex data
                        tmp = File_Handler.read_short(file_data, file_offset_cmd + 0x02, false);
                        // vertex count
                        cmd.parameters[1] = ((tmp & 0xFC00) >> 10);
                        // vertex sotrage size
                        cmd.parameters[2] = ((tmp & 0x03FF) >> 0);
                        // data segment num
                        cmd.parameters[3] = File_Handler.read_char(file_data, file_offset_cmd + 0x04, false);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[4] = File_Handler.read_int(file_data, file_offset_cmd + 0x04, false) & 0x00FFFFF;
                        //================================
                        ///// SIMULATE DESCRIPTORS ///////
                        //================================
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
                        // vertex buffer tri_B_v1 location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x05, false) / 2);
                        // vertex buffer tri_B_v2 location (doubled)
                        cmd.parameters[1] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x06, false) / 2);
                        // vertex buffer tri_B_v3 location (doubled)
                        cmd.parameters[2] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x07, false) / 2);

                        //================================
                        ///// SIMULATE DESCRIPTORS ///////
                        //================================
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
                        // vertex buffer tri_A_v1 location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x01, false) / 2);
                        // vertex buffer tri_A_v2 location (doubled)
                        cmd.parameters[1] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x02, false) / 2);
                        // vertex buffer tri_A_v3 location (doubled)
                        cmd.parameters[2] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x03, false) / 2);
                        //
                        // vertex buffer tri_B_v1 location (doubled)
                        cmd.parameters[3] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x05, false) / 2);
                        // vertex buffer tri_B_v2 location (doubled)
                        cmd.parameters[4] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x06, false) / 2);
                        // vertex buffer tri_B_v3 location (doubled)
                        cmd.parameters[5] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x07, false) / 2);

                        //================================
                        ///// SIMULATE DESCRIPTORS ///////
                        //================================
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

                    case ("G_ENDDL"):
                        // no additional params
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
                    details += String.Format("seg={0}, ", cmd.parameters[2]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[3], 0xFFFFFFFF);
                    break;

                case ("G_LOADTLUT"):
                    details += String.Format("descr=#{0}, ", cmd.parameters[0]);
                    details += String.Format("color_cnt={0}, ", cmd.parameters[1]);
                    break;

                case ("G_DL"):
                    details += String.Format("final?={0}, ", cmd.parameters[0]);
                    details += String.Format("seg={0}, ", cmd.parameters[1]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[2], 0xFFFFFFFF);
                    break;

                case ("G_VTX"):
                    details += String.Format("buf@={0}, ", cmd.parameters[0]);
                    details += String.Format("cnt={0}, ", cmd.parameters[1]);
                    details += String.Format("siz={0} B, ", cmd.parameters[2]);
                    details += String.Format("seg={0}, ", cmd.parameters[3]);
                    details += "@ " + File_Handler.uint_to_string(cmd.parameters[4], 0xFFFFFFFF);
                    break;

                case ("G_TRI1"):
                    details += "tri_B=(";
                    details += String.Format(" {0}, ", cmd.parameters[0]);
                    details += String.Format("{0}, ", cmd.parameters[1]);
                    details += String.Format("{0} ", cmd.parameters[2]);
                    details += "), ";
                    break;

                case ("G_TRI2"):
                    details += "tri_A=(";
                    details += String.Format(" {0}, ", cmd.parameters[0]);
                    details += String.Format("{0}, ", cmd.parameters[1]);
                    details += String.Format("{0} ", cmd.parameters[2]);
                    details += "), ";
                    details += "tri_B=(";
                    details += String.Format(" {0}, ", cmd.parameters[3]);
                    details += String.Format("{0}, ", cmd.parameters[4]);
                    details += String.Format("{0} ", cmd.parameters[5]);
                    details += ")";
                    break;

                case ("G_ENDDL"):
                    details = "ENDING";
                    break;
            }

            content.Add(new string[] {
                File_Handler.uint_to_string(cmd.command_byte, 0xFF),
                cmd.command_name,
                details
            });

            return content;
        }

    }
}