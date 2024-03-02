using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Binjo
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
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x0C];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.index_1, 2), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.index_2, 2), bytes, 0x02);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.index_3, 2), bytes, 0x04);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_1, 2), bytes, 0x06);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.floor_type, 2), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.sound_type, 2), bytes, 0x0A);
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

        public Tri_Elem(FullTriangle full_tri)
        {
            this.index_1 = full_tri.index_1;
            this.index_2 = full_tri.index_2;
            this.index_3 = full_tri.index_3;
            this.unk_1 = full_tri.unk_1;
            this.floor_type = full_tri.floor_type;
            this.sound_type = full_tri.sound_type;
        }
        public Tri_Elem()
        {
            this.index_1 = 0;
            this.index_2 = 0;
            this.index_3 = 0;
            this.unk_1 = 0;
            this.floor_type = 0;
            this.sound_type = 0;
        }
    }
    public class Geo_Cube_Elem
    {
        public static int MEM_SIZE = 0x04;

        public ushort starting_tri_ID;
        public ushort tri_cnt;

        public List<Tri_Elem> coll_tri_list = new List<Tri_Elem>();

        public Geo_Cube_Elem()
        {
            this.starting_tri_ID = 0;
            this.tri_cnt = 0;
            this.coll_tri_list = new List<Tri_Elem>();
        }

        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x04];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.starting_tri_ID, 2), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tri_cnt, 2), bytes, 0x02);
            return bytes;
        }
    }
    public class Collision_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public short min_geo_cube_x;
        public short min_geo_cube_y;
        public short min_geo_cube_z;
        public short max_geo_cube_x;
        public short max_geo_cube_y;
        public short max_geo_cube_z;
        public short stride_y;
        public short stride_z;
        // === 0x10 ===============================
        public ushort geo_cube_cnt;
        public ushort geo_cube_scale;
        public ushort tri_cnt;
        public ushort padding;

        public Geo_Cube_Elem[] geo_cube_list;
        public Tri_Elem[] tri_list;
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x18];
            // === 0x00 ===============================
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.min_geo_cube_x, 2), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.min_geo_cube_y, 2), bytes, 0x02);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.min_geo_cube_z, 2), bytes, 0x04);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.max_geo_cube_x, 2), bytes, 0x06);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.max_geo_cube_y, 2), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.max_geo_cube_z, 2), bytes, 0x0A);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.stride_y, 2), bytes, 0x0C);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.stride_z, 2), bytes, 0x0E);
            // === 0x10 ===============================
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.geo_cube_cnt, 2), bytes, 0x10);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.geo_cube_scale, 2), bytes, 0x12);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tri_cnt, 2), bytes, 0x14);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.padding, 2), bytes, 0x16);

            // then we append all the geocubes as byte arrays
            for (int id = 0; id < this.geo_cube_cnt; id++)
            {
                Geo_Cube_Elem cube = this.geo_cube_list[id];
                bytes = File_Handler.concat_arrays(bytes, cube.get_bytes());
            }
            // and all the collision tris
            for (int id = 0; id < this.tri_cnt; id++)
            {
                Tri_Elem tri = this.tri_list[id];
                bytes = File_Handler.concat_arrays(bytes, tri.get_bytes());
            }
            return bytes;
        }

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
            this.min_geo_cube_x = (short) File_Handler.read_short(file_data, file_offset + 0x00, false);
            this.min_geo_cube_y = (short) File_Handler.read_short(file_data, file_offset + 0x02, false);
            this.min_geo_cube_z = (short) File_Handler.read_short(file_data, file_offset + 0x04, false);
            this.max_geo_cube_x = (short) File_Handler.read_short(file_data, file_offset + 0x06, false);
            this.max_geo_cube_y = (short) File_Handler.read_short(file_data, file_offset + 0x08, false);
            this.max_geo_cube_z = (short) File_Handler.read_short(file_data, file_offset + 0x0A, false);
            this.stride_y = (short) File_Handler.read_short(file_data, file_offset + 0x0C, false);
            this.stride_z = (short) File_Handler.read_short(file_data, file_offset + 0x0E, false);
            // === 0x10 ===============================
            this.geo_cube_cnt = File_Handler.read_short(file_data, file_offset + 0x10, false);
            this.geo_cube_scale = File_Handler.read_short(file_data, file_offset + 0x12, false);
            this.tri_cnt = File_Handler.read_short(file_data, file_offset + 0x14, false);
            this.padding = File_Handler.read_short(file_data, file_offset + 0x16, false);

            // calculated properties
            this.file_offset_data = (uint)(file_offset + 0x18 + (this.geo_cube_cnt * Geo_Cube_Elem.MEM_SIZE));

            this.geo_cube_list = new Geo_Cube_Elem[this.geo_cube_cnt];
            for (int i = 0; i < this.geo_cube_cnt; i++)
            {
                Geo_Cube_Elem geo_cube = new Geo_Cube_Elem();
                int file_offset_geo_cube = (int)(this.file_offset + 0x18 + (i * Geo_Cube_Elem.MEM_SIZE));

                // parsing properties
                // === 0x00 ===============================
                geo_cube.starting_tri_ID = File_Handler.read_short(file_data, file_offset_geo_cube + 0x00, false);
                geo_cube.tri_cnt = File_Handler.read_short(file_data, file_offset_geo_cube + 0x02, false);

                this.geo_cube_list[i] = geo_cube;
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
                "Geo Cube Minima",
                String.Format("{0}, {1}, {2}", 
                    File_Handler.uint_to_string(this.min_geo_cube_x, 0xFFFF),
                    File_Handler.uint_to_string(this.min_geo_cube_y, 0xFFFF),
                    File_Handler.uint_to_string(this.min_geo_cube_z, 0xFFFF)
                ),
                String.Format("{0}, {1}, {2}",
                    File_Handler.uint_to_string(this.min_geo_cube_x, 10),
                    File_Handler.uint_to_string(this.min_geo_cube_y, 10),
                    File_Handler.uint_to_string(this.min_geo_cube_z, 10)
                ),
                "inclusive range"
            });
            content.Add(new string[] {
                "Geo Cube Maxima",
                String.Format("{0}, {1}, {2}",
                    File_Handler.uint_to_string(this.max_geo_cube_x, 0xFFFF),
                    File_Handler.uint_to_string(this.max_geo_cube_y, 0xFFFF),
                    File_Handler.uint_to_string(this.max_geo_cube_z, 0xFFFF)
                ),
                String.Format("{0}, {1}, {2}",
                    File_Handler.uint_to_string(this.max_geo_cube_x, 10),
                    File_Handler.uint_to_string(this.max_geo_cube_y, 10),
                    File_Handler.uint_to_string(this.max_geo_cube_z, 10)
                ),
                "inclusive range"
            });
            content.Add(new string[] {
                "Y Stride",
                String.Format("{0}",
                    File_Handler.uint_to_string(this.stride_y, 0xFFFF)
                ),
                String.Format("{0}",
                    File_Handler.uint_to_string(this.stride_y, 10)
                ),
                "Amount of GeoCubes per X-Row"
            });
            content.Add(new string[] {
                "Z Stride",
                String.Format("{0}",
                    File_Handler.uint_to_string(this.stride_z, 0xFFFF)
                ),
                String.Format("{0}",
                    File_Handler.uint_to_string(this.stride_z, 10)
                ),
                "Amount of GeoCubes per XY-Layer"
            });
            content.Add(new string[] {
                "Geo Cube Count",
                File_Handler.uint_to_string(this.geo_cube_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.geo_cube_cnt, 10),
                "Full amount of GeoCubes"
            });
            content.Add(new string[] {
                "Geo Cube Scale",
                File_Handler.uint_to_string(this.geo_cube_scale, 0xFFFF),
                File_Handler.uint_to_string(this.geo_cube_scale, 10),
                ""
            });
            content.Add(new string[] {
                "Coll Tri Count",
                File_Handler.uint_to_string(this.tri_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.tri_cnt, 10),
                "may contain duplicates"
            });

            return content;
        }
    }
}
