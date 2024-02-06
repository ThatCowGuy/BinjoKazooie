using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
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



        public void populate(byte[] file_data, int file_offset)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No Collision Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint)file_offset;

            // parsing properties
            // === 0x00 ===============================
            this.command_cnt = File_Handler.read_int(file_data, file_offset + 0x00);
            this.padding = File_Handler.read_int(file_data, file_offset + 0x04);

            this.command_list = new DisplayList_Command[this.command_cnt];
            for (int i = 0; i < this.command_cnt; i++)
            {
                DisplayList_Command cmd = new DisplayList_Command();
                int file_offset_cmd = (int)(this.file_offset + 0x08 + (i * 0x08));

                // parsing properties
                // === 0x00 ===============================
                cmd.command_byte = File_Handler.read_char(file_data, file_offset_cmd + 0x00);
                cmd.command_name = DisplayList_Segment.F3DEX_Command_Names[cmd.command_byte];

                uint tmp;
                switch (cmd.command_name)
                {
                    case ("G_TEXTURE"):
                        tmp = File_Handler.read_char(file_data, file_offset_cmd + 0x02);
                        // maximum number of mipmaps
                        cmd.parameters[0] = ((tmp & 0b00111000) >> 3);
                        // affected tile descripter index
                        cmd.parameters[1] = ((tmp & 0b00000111) >> 0);
                        // enable tile descriptor
                        cmd.parameters[2] = File_Handler.read_char(file_data, file_offset_cmd + 0x03);
                        // S Axis scale factor (horizontal)
                        cmd.parameters[3] = File_Handler.read_short(file_data, file_offset_cmd + 0x04);
                        // T Axis scale factor (vertical)
                        cmd.parameters[4] = File_Handler.read_short(file_data, file_offset_cmd + 0x06);
                        break;

                    case ("G_SETTIMG"):
                        tmp = File_Handler.read_char(file_data, file_offset_cmd + 0x01);
                        // color storage format
                        cmd.parameters[0] = ((tmp & 0b11100000) >> 5);
                        // color storage bit-size
                        cmd.parameters[1] = ((tmp & 0b00011000) >> 3);
                        // data segment num
                        cmd.parameters[2] = File_Handler.read_char(file_data, file_offset_cmd + 0x04);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[3] = File_Handler.read_int(file_data, file_offset_cmd + 0x04) & 0x00FFFFF;
                        break;

                    case ("G_SETTILE"):
                        tmp = File_Handler.read_int(file_data, file_offset_cmd + 0x00);
                        // color storage format
                        cmd.parameters[0] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_1110_0000__0000_0000_0000_0000);
                        // color storage bit-size
                        cmd.parameters[1] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0001_1000__0000_0000_0000_0000);
                        // num of 64bit vals per row
                        cmd.parameters[2] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0011__1111_1110_0000_0000);
                        // TMEM offset of texture
                        cmd.parameters[3] = File_Handler.apply_bitmask(tmp, 0b_0000_0000_0000_0000__0000_0001_1111_1111);

                        tmp = File_Handler.read_int(file_data, file_offset_cmd + 0x04);
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
                        cmd.parameters[0] = File_Handler.read_char(file_data, file_offset_cmd + 0x04);
                        // quadrupled color count (-1)
                        cmd.parameters[1] = (File_Handler.read_int(file_data, file_offset_cmd + 0x04) & 0x00FFF000) >> 0xC;
                        cmd.parameters[1] = (cmd.parameters[1] / 4) + 1;
                        break;

                    case ("G_DL"):
                        // remember return address (which identifies that this is not the end)
                        cmd.parameters[0] = File_Handler.read_char(file_data, file_offset_cmd + 0x01);
                        // data segment num
                        cmd.parameters[1] = File_Handler.read_char(file_data, file_offset_cmd + 0x04);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[2] = File_Handler.read_int(file_data, file_offset_cmd + 0x04) & 0x00FFFFF;
                        break;

                    case ("G_VTX"):
                        // vertex buffer target location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x01) / 2);
                        // vertex count to load + size of vertex data
                        tmp = File_Handler.read_short(file_data, file_offset_cmd + 0x02);
                        // vertex count
                        cmd.parameters[1] = ((tmp & 0xFC00) >> 10);
                        // vertex sotrage size
                        cmd.parameters[2] = ((tmp & 0x03FF) >> 0);
                        // data segment num
                        cmd.parameters[3] = File_Handler.read_char(file_data, file_offset_cmd + 0x04);
                        // data offset (removing the leading byte, because thats caught in the param before)
                        cmd.parameters[4] = File_Handler.read_int(file_data, file_offset_cmd + 0x04) & 0x00FFFFF;
                        break;

                    case ("G_TRI1"):
                        // vertex buffer tri_B_v1 location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x05) / 2);
                        // vertex buffer tri_B_v2 location (doubled)
                        cmd.parameters[1] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x06) / 2);
                        // vertex buffer tri_B_v3 location (doubled)
                        cmd.parameters[2] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x07) / 2);
                        break;

                    case ("G_TRI2"):
                        // vertex buffer tri_A_v1 location (doubled)
                        cmd.parameters[0] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x01) / 2);
                        // vertex buffer tri_A_v2 location (doubled)
                        cmd.parameters[1] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x02) / 2);
                        // vertex buffer tri_A_v3 location (doubled)
                        cmd.parameters[2] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x03) / 2);
                        //
                        // vertex buffer tri_B_v1 location (doubled)
                        cmd.parameters[3] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x05) / 2);
                        // vertex buffer tri_B_v2 location (doubled)
                        cmd.parameters[4] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x06) / 2);
                        // vertex buffer tri_B_v3 location (doubled)
                        cmd.parameters[5] = (uint)(File_Handler.read_char(file_data, file_offset_cmd + 0x07) / 2);
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