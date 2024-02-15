using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    public class Tri_Elem
    {
        // parsed properties
        public ushort index_1;
        public ushort index_2;
        public ushort index_3;
        public ushort unk_1;
        public ushort floor_type;
        public ushort sound_type;

        // inferred properties
        public ushort assigned_tex_ID;
        public Tex_Data assigned_tex;
        public FX_Elem assigned_FX;

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
    public class Unk_Coll_Elem
    {
        public ushort unk_1;
        public ushort unk_2;
    }
    public class Collision_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public int unk_1;
        public int unk_2;
        public int unk_3;
        public int unk_4;
        // === 0x10 ===============================
        public ushort unk_cnt; // *4 to get dynamic header size
        public ushort unk_5;
        public ushort tri_cnt;
        public ushort unk_6;

        public Unk_Coll_Elem[] unk_list;
        public Tri_Elem[] tri_list;

        // locators
        public uint file_offset;
        public uint file_offset_data;

        public List<FullTriangle> export_tris_as_full()
        {
            List<FullTriangle> export = new List<FullTriangle>();
            for (int i = 0; i < this.tri_cnt; i++)
            {
                Tri_Elem tri = this.tri_list[i];
                FullTriangle full_tri = new FullTriangle();
                full_tri.index_1 = tri.index_1;
                full_tri.index_2 = tri.index_2;
                full_tri.index_3 = tri.index_3;
                full_tri.floor_type = tri.floor_type;
                full_tri.sound_type = tri.sound_type;

                export.Add(full_tri);
            }
            return export;
        }

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
            this.unk_1 = (int)File_Handler.read_int(file_data, file_offset + 0x00, false);
            this.unk_2 = (int)File_Handler.read_int(file_data, file_offset + 0x04, false);
            this.unk_3 = (int)File_Handler.read_int(file_data, file_offset + 0x08, false);
            this.unk_4 = (int)File_Handler.read_int(file_data, file_offset + 0x0C, false);
            // === 0x10 ===============================
            this.unk_cnt = File_Handler.read_short(file_data, file_offset + 0x10, false);
            this.unk_5 = File_Handler.read_short(file_data, file_offset + 0x12, false);
            this.tri_cnt = File_Handler.read_short(file_data, file_offset + 0x14, false);
            this.unk_6 = File_Handler.read_short(file_data, file_offset + 0x16, false);

            // calculated properties
            this.file_offset_data = (uint)(file_offset + 0x18 + (this.unk_cnt * 0x04));

            this.unk_list = new Unk_Coll_Elem[this.unk_cnt];
            for (int i = 0; i < this.unk_cnt; i++)
            {
                Unk_Coll_Elem unk = new Unk_Coll_Elem();
                int file_offset_unk = (int)(this.file_offset + 0x18 + (i * 0x04));

                // parsing properties
                // === 0x00 ===============================
                unk.unk_1 = File_Handler.read_short(file_data, file_offset_unk + 0x00, false);
                unk.unk_2 = File_Handler.read_short(file_data, file_offset_unk + 0x02, false);

                this.unk_list[i] = unk;
            }

            this.tri_list = new Tri_Elem[this.tri_cnt];
            for (int i = 0; i < this.tri_cnt; i++)
            {
                Tri_Elem tri = new Tri_Elem();
                int file_offset_tri = (int)(this.file_offset_data + (i * 0x0C));

                // parsing properties
                // === 0x00 ===============================
                tri.index_1 = File_Handler.read_short(file_data, file_offset_tri + 0x00, false);
                tri.index_2 = File_Handler.read_short(file_data, file_offset_tri + 0x02, false);
                tri.index_3 = File_Handler.read_short(file_data, file_offset_tri + 0x04, false);
                tri.unk_1 = File_Handler.read_short(file_data, file_offset_tri + 0x06, false);
                tri.floor_type = File_Handler.read_short(file_data, file_offset_tri + 0x08, false);
                tri.sound_type = File_Handler.read_short(file_data, file_offset_tri + 0x0A, false);

                this.tri_list[i] = tri;
            }
        }

        public String export_TRI_IDs_to_Base64()
        {
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.tri_cnt; i++)
            {
                Tri_Elem tri = this.tri_list[i];
                raw_data.AddRange(BitConverter.GetBytes((ushort) tri.index_1));
                raw_data.AddRange(BitConverter.GetBytes((ushort) tri.index_2));
                raw_data.AddRange(BitConverter.GetBytes((ushort) tri.index_3));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
        }
        public List<string[]> get_tri_content(int id)
        {
            Tri_Elem tri = this.tri_list[id];
            List<string[]> content = new List<string[]>();

            content.Add(new string[] {
                File_Handler.uint_to_string(id, 0xFFFF),
                File_Handler.uint_to_string(tri.index_1, 0xFFFF),
                File_Handler.uint_to_string(tri.index_2, 0xFFFF),
                File_Handler.uint_to_string(tri.index_3, 0xFFFF),

                File_Handler.uint_to_string(tri.floor_type, 0b1),
                File_Handler.uint_to_string(tri.sound_type, 0b1),
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
                "File Offset (Data)",
                File_Handler.uint_to_string(this.file_offset_data, 0xFFFFFFFF),
                "",
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_1, 0xFFFF),
                File_Handler.uint_to_string(this.unk_1, 10),
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_2, 0xFFFF),
                File_Handler.uint_to_string(this.unk_2, 10),
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_3, 0xFFFF),
                File_Handler.uint_to_string(this.unk_3, 10),
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_4, 0xFFFF),
                File_Handler.uint_to_string(this.unk_4, 10),
                ""
            });
            content.Add(new string[] {
                "??? Count",
                File_Handler.uint_to_string(this.unk_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.unk_cnt, 10),
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_5, 0xFFFF),
                File_Handler.uint_to_string(this.unk_5, 10),
                ""
            });
            content.Add(new string[] {
                "Tri Count",
                File_Handler.uint_to_string(this.tri_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.tri_cnt, 10),
                ""
            });
            content.Add(new string[] {
                "???",
                File_Handler.uint_to_string(this.unk_6, 0xFFFF),
                File_Handler.uint_to_string(this.unk_6, 10),
                ""
            });

            return content;
        }
    }
}
