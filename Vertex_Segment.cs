using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    public class Vtx_Elem
    {
        public short x;
        public short y;
        public short z;
        public short padding;
        public short u;
        public short v;
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }
    public class Vertex_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public short neg_draw_dist_x;
        public short neg_draw_dist_y;
        public short neg_draw_dist_z;
        public short pos_draw_dist_x;
        public short pos_draw_dist_y;
        public short pos_draw_dist_z;
        public short obj_range_A;
        public short obj_range_B;
        // === 0x10 ===============================
        public short coll_range_other;
        public short coll_range_banjo;
        public ushort vtx_cnt_doubled; // for whatever reason; also wrong sometimes

        public Vtx_Elem[] vtx_list;

        // calculated properties
        public ushort binheader_vtx_cnt;

        // locators
        public uint file_offset;
        public uint file_offset_data;


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
            this.file_offset_data = (uint)file_offset + 0x18;

            // parsing properties
            // === 0x00 ===============================
            this.neg_draw_dist_x = (short)File_Handler.read_short(file_data, file_offset + 0x00);
            this.neg_draw_dist_y = (short)File_Handler.read_short(file_data, file_offset + 0x02);
            this.neg_draw_dist_z = (short)File_Handler.read_short(file_data, file_offset + 0x04);
            this.pos_draw_dist_x = (short)File_Handler.read_short(file_data, file_offset + 0x06);
            this.pos_draw_dist_y = (short)File_Handler.read_short(file_data, file_offset + 0x08);
            this.pos_draw_dist_z = (short)File_Handler.read_short(file_data, file_offset + 0x0A);
            this.obj_range_A = (short)File_Handler.read_short(file_data, file_offset + 0x0C);
            this.obj_range_B = (short)File_Handler.read_short(file_data, file_offset + 0x0E);
            // === 0x10 ===============================
            this.coll_range_other = (short)File_Handler.read_short(file_data, file_offset + 0x10);
            this.coll_range_banjo = (short)File_Handler.read_short(file_data, file_offset + 0x12);
            // NOTE: this value seems to be incorrect sometimes; use BIN Header one
            this.vtx_cnt_doubled = File_Handler.read_short(file_data, file_offset + 0x14);

            this.vtx_list = new Vtx_Elem[this.binheader_vtx_cnt];
            for (int i = 0; i < this.binheader_vtx_cnt; i++)
            {
                Vtx_Elem v = new Vtx_Elem();
                int file_offset_vtx = (int)(this.file_offset_data + (i * 0x10));

                // parsing properties
                // === 0x00 ===============================
                v.x = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x00);
                v.y = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x02);
                v.z = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x04);
                v.padding = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x06);
                v.u = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x08);
                v.v = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x0A);
                v.r = File_Handler.read_char(file_data, file_offset_vtx + 0x0C);
                v.g = File_Handler.read_char(file_data, file_offset_vtx + 0x0D);
                v.b = File_Handler.read_char(file_data, file_offset_vtx + 0x0E);
                v.a = File_Handler.read_char(file_data, file_offset_vtx + 0x0F);

                vtx_list[i] = v;
            }
        }

        public List<string[]> get_vtx_content(int id)
        {
            Vtx_Elem vtx = this.vtx_list[id];
            List<string[]> content = new List<string[]>();

            content.Add(new string[] {
                File_Handler.uint_to_string(id, 0xFFFF),
                File_Handler.uint_to_string(vtx.x, 0xFFFF),
                File_Handler.uint_to_string(vtx.y, 0xFFFF),
                File_Handler.uint_to_string(vtx.z, 0xFFFF),

                File_Handler.uint_to_string(vtx.u, 0xFFFF),
                File_Handler.uint_to_string(vtx.v, 0xFFFF),

                File_Handler.uint_to_string(vtx.r, 0xFF),
                File_Handler.uint_to_string(vtx.g, 0xFF),
                File_Handler.uint_to_string(vtx.b, 0xFF),
                File_Handler.uint_to_string(vtx.a, 0xFF),
            });

            return content;
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
                "Draw Distance -X",
                File_Handler.uint_to_string(this.neg_draw_dist_x, 0xFFFF),
                File_Handler.uint_to_string(this.neg_draw_dist_x, 10),
                ""
            });
            content.Add(new string[] {
                "Draw Distance -Y",
                File_Handler.uint_to_string(this.neg_draw_dist_y, 0xFFFF),
                File_Handler.uint_to_string(this.neg_draw_dist_y, 10),
                ""
            });
            content.Add(new string[] {
                "Draw Distance -Z",
                File_Handler.uint_to_string(this.neg_draw_dist_z, 0xFFFF),
                File_Handler.uint_to_string(this.neg_draw_dist_z, 10),
                ""
            });
            content.Add(new string[] {
                "Draw Distance +X",
                File_Handler.uint_to_string(this.pos_draw_dist_x, 0xFFFF),
                File_Handler.uint_to_string(this.pos_draw_dist_x, 10),
                String.Format("Extent = {0} units", (this.pos_draw_dist_x - this.neg_draw_dist_x))
            });
            content.Add(new string[] {
                "Draw Distance +Y",
                File_Handler.uint_to_string(this.pos_draw_dist_y, 0xFFFF),
                File_Handler.uint_to_string(this.pos_draw_dist_y, 10),
                String.Format("Extent = {0} units", (this.pos_draw_dist_y - this.neg_draw_dist_y))
            });
            content.Add(new string[] {
                "Draw Distance +Z",
                File_Handler.uint_to_string(this.pos_draw_dist_z, 0xFFFF),
                File_Handler.uint_to_string(this.pos_draw_dist_z, 10),
                String.Format("Extent = {0} units", (this.pos_draw_dist_z - this.neg_draw_dist_z))
            });
            content.Add(new string[] {
                "Object Range A (?)",
                File_Handler.uint_to_string(this.obj_range_A, 0xFFFF),
                File_Handler.uint_to_string(this.obj_range_A, 10),
                ""
            });
            content.Add(new string[] {
                "Object Range B (?)",
                File_Handler.uint_to_string(this.obj_range_B, 0xFFFF),
                File_Handler.uint_to_string(this.obj_range_B, 10),
                String.Format("Extent = {0} units", (this.obj_range_B - this.obj_range_A))
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.coll_range_other, 0xFFFF),
                File_Handler.uint_to_string(this.coll_range_other, 10),
                "may be Enemy related"
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.coll_range_banjo, 0xFFFF),
                File_Handler.uint_to_string(this.coll_range_banjo, 10),
                "may be Banjo related"
            });
            content.Add(new string[] {
                "Doubled VTX count",
                File_Handler.uint_to_string(this.vtx_cnt_doubled, 0xFFFF),
                File_Handler.uint_to_string(this.vtx_cnt_doubled, 10),
                String.Format("{0} vertices", (this.vtx_cnt_doubled / 2))
            });
            content.Add(new string[] {
                "VTX cnt (BIN Header)",
                File_Handler.uint_to_string(this.binheader_vtx_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.binheader_vtx_cnt, 10),
                String.Format("seems to be more reliable")
            });
            return content;
        }
    }
}
