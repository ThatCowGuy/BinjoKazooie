using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
{
    public class BIN_Header
    {
        public bool valid = false;

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
        public uint unk_2;
        public uint anim_tex_offset;
        // === 0x30 ===============================
        public ushort tri_cnt;
        public ushort vtx_cnt;
        public uint unk_3;

        public BIN_Header()
        {
            this.start_identifier = 0x0000000B;
            this.geo_type = 0x02; // tri-linear

            this.unk_3 = 0x42C80000; // 100.0f
        }
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x38];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.start_identifier, 4), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.geo_offset, 4), bytes, 0x04);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tex_offset, 2), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.geo_type, 2), bytes, 0x0A);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.DL_offset, 4), bytes, 0x0C);
            //
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.vtx_offset, 4), bytes, 0x10);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_1, 4), bytes, 0x14);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.bone_offset, 4), bytes, 0x18);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.coll_offset, 4), bytes, 0x1C);
            //
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.FX_END, 4), bytes, 0x20);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.FX_offset, 4), bytes, 0x24);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_2, 4), bytes, 0x28);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.anim_tex_offset, 4), bytes, 0x2C);
            //
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tri_cnt, 2), bytes, 0x30);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.vtx_cnt, 2), bytes, 0x32);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_3, 4), bytes, 0x34);
            return bytes;
        }

        public void populate(byte[] file_data)
        {
            this.valid = true;

            // parsed properties
            // === 0x00 ===============================
            // this.start_identifier = 0x0000000B; // CONST
            this.start_identifier = File_Handler.read_int(file_data, 0x00, false);
            this.geo_offset = File_Handler.read_int(file_data, 0x04, false);
            this.tex_offset = File_Handler.read_short(file_data, 0x08, false);
            this.geo_type = File_Handler.read_short(file_data, 0x0A, false);
            this.DL_offset = File_Handler.read_int(file_data, 0x0C, false);
            // === 0x10 ===============================
            this.vtx_offset = File_Handler.read_int(file_data, 0x10, false);
            this.unk_1 = File_Handler.read_int(file_data, 0x14, false);
            this.bone_offset = File_Handler.read_int(file_data, 0x18, false);
            this.coll_offset = File_Handler.read_int(file_data, 0x1C, false);
            // === 0x20 ===============================
            this.FX_END = File_Handler.read_int(file_data, 0x20, false);
            this.FX_offset = File_Handler.read_int(file_data, 0x24, false);
            this.unk_2 = File_Handler.read_int(file_data, 0x28, false);
            this.anim_tex_offset = File_Handler.read_int(file_data, 0x2C, false);
            // === 0x30 ===============================
            this.tri_cnt = File_Handler.read_short(file_data, 0x30, false);
            this.vtx_cnt = File_Handler.read_short(file_data, 0x32, false);
            this.unk_3 = File_Handler.read_int(file_data, 0x34, false);
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
                "BIN Start Identifier",
                File_Handler.uint_to_string(this.start_identifier, 0xFFFFFFFF),
                File_Handler.uint_to_string(this.start_identifier, 10),
                "Constant: 0x0000_000B"
            });
            content.Add(new string[] {
                "GeoLayout Offset",
                File_Handler.uint_to_string(this.geo_offset, 0xFFFFFFFF),
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
                "??? Offset",
                File_Handler.uint_to_string(this.unk_1, 0xFFFFFFFF)
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
                "??? Offset",
                File_Handler.uint_to_string(this.unk_2, 0xFFFFFFFF)
            });
            content.Add(new string[] {
                "Anim Tex Offset",
                File_Handler.uint_to_string(this.anim_tex_offset, 0xFFFFFFFF)
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
                File_Handler.convert_to_float((int) this.unk_3).ToString("F4")
            });
            return content;
        }
    }
}
