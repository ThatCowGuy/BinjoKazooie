using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    public class BIN_Header
    {
        // parsed properties
        // === 0x00 ===============================
        public uint start_identifier;
        public uint geo_offset;
        public ushort tex_offset;
        public ushort geo_type;
        public uint DL_offset;
        // === 0x10 ===============================
        public uint vtx_offset;
        public uint unk_1;
        public uint bone_offset;
        public uint coll_offset;
        // === 0x20 ===============================
        public uint FX_END;
        public uint FX_offset;
        public uint unk_2; // this is similarly valued as unk_1
        public uint bone_tex_offset;
        // === 0x30 ===============================
        public ushort tri_cnt;
        public ushort vtx_cnt;
        public uint unk_3;

        public void populate(byte[] file_data)
        {
            // parsed properties
            // === 0x00 ===============================
            // this.start_identifier = 0x0000000B; // CONST
            this.start_identifier = File_Handler.read_int(file_data, 0x00);
            this.geo_offset = File_Handler.read_int(file_data, 0x04);
            this.tex_offset = File_Handler.read_short(file_data, 0x08);
            this.geo_type = File_Handler.read_short(file_data, 0x0A);
            this.DL_offset = File_Handler.read_int(file_data, 0x0C);
            // === 0x10 ===============================
            this.vtx_offset = File_Handler.read_int(file_data, 0x10);
            this.unk_1 = File_Handler.read_int(file_data, 0x14);
            this.bone_offset = File_Handler.read_int(file_data, 0x18);
            this.coll_offset = File_Handler.read_int(file_data, 0x1C);
            // === 0x20 ===============================
            this.FX_END = File_Handler.read_int(file_data, 0x20);
            this.FX_offset = File_Handler.read_int(file_data, 0x24);
            this.unk_2 = File_Handler.read_int(file_data, 0x28);
            this.bone_tex_offset = File_Handler.read_int(file_data, 0x2C);
            // === 0x30 ===============================
            this.tri_cnt = File_Handler.read_short(file_data, 0x30);
            this.vtx_cnt = File_Handler.read_short(file_data, 0x32);
            this.unk_3 = File_Handler.read_int(file_data, 0x34);
        }

        public List<string[]> get_content()
        {
            List<string[]> content = new List<string[]>();

            content.Add(new string[] {
                "BIN Start Identifier",
                File_Handler.uint_to_string(this.start_identifier, 0xFFFFFFFF),
                File_Handler.uint_to_string(this.start_identifier, 10),
                "Constant: 0x0000_000B"
            });
            content.Add(new string[] {
                "GeoLayout Offset",
                File_Handler.uint_to_string(this.geo_offset, 0xFFFF),
                File_Handler.uint_to_string(this.geo_offset, 10),
                ""
            });
            content.Add(new string[] {
                "Tex Offset",
                File_Handler.uint_to_string(this.tex_offset, 0xFFFF),
                File_Handler.uint_to_string(this.tex_offset, 10),
                "Basically always 0x0038"
            });
            string named_geo_type = "";
            switch (this.geo_type)
            {
                case 0x00:
                named_geo_type = "normal";
                break;
                case 0x02:
                named_geo_type = "tri-linear";
                break;
                case 0x04:
                named_geo_type = "env-map";
                break;
                default:
                named_geo_type = "unknown";
                break;
            }
            content.Add(new string[] {
                "GeoType",
                File_Handler.uint_to_string(this.geo_type, 0xFFFF),
                named_geo_type
            });
            content.Add(new string[] {
                "DL Offset",
                File_Handler.uint_to_string(this.DL_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "VTX Offset",
                File_Handler.uint_to_string(this.vtx_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_1, 0xFFFFFFFF),
                this.unk_1.ToString()
            });
            content.Add(new string[] {
                "Bone Offset",
                File_Handler.uint_to_string(this.bone_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "Collision Offset",
                File_Handler.uint_to_string(this.coll_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "FX END",
                File_Handler.uint_to_string(this.FX_END, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "FX START",
                File_Handler.uint_to_string(this.FX_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_2, 0xFFFFFFFF),
                this.unk_2.ToString()
            });
            content.Add(new string[] {
                "bone.Tex Offset",
                File_Handler.uint_to_string(this.bone_tex_offset, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "Tri Count",
                File_Handler.uint_to_string(this.tri_cnt, 0xFFFF),
                this.tri_cnt.ToString()
            });
            content.Add(new string[] {
                "VTX Count",
                File_Handler.uint_to_string(this.vtx_cnt, 0xFFFF),
                this.vtx_cnt.ToString()
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_3, 0xFFFFFFFF),
                this.unk_3.ToString()
            });
            return content;
        }
    }
}
