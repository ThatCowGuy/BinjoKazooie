using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BK_BIN_Analyzer
{
    public class Vtx_Elem : IEquatable<Vtx_Elem>
    {
        public short x;
        public short y;
        public short z;
        public short padding;
        public short u;
        public short v;
        public Single transformed_U;
        public Single transformed_V;
        public byte r;
        public byte g;
        public byte b;
        public byte a;
        public void calc_transformed_UVs(Tile_Descriptor tiledes)
        {
            if (tiledes.assigned_tex_meta == null || tiledes.assigned_tex_data == null)
            {
                this.transformed_U = (Single) ((this.u / 64.0) + 0 + 0.5) / 32.0f;
                this.transformed_V = (Single) ((this.v / 64.0) + 0 + 0.5) / 32.0f;
            }
            else
            {
                this.transformed_U = (Single) ((this.u / 64.0) + tiledes.S_shift + 0.5) / tiledes.assigned_tex_meta.width;
                this.transformed_V = (Single) ((this.v / 64.0) + tiledes.T_shift + 0.5) / tiledes.assigned_tex_meta.height;
            }
            // ATTENTION !! Im flipping the V coord here bx images are stored upside down
            //              but Im exporting them right-side up
            this.transformed_V = -1 * this.transformed_V;
        }
        // NOTE: this ignores the tiledescriptors for now... so no shifting yet (maybe unneccessary ?)
        public void reverse_UV_transforms(uint w_factor, uint h_factor)
        {
            // ATTENTION !! Im undoing the flipping here again
            this.u = (short) (64.0 * ((this.transformed_U * w_factor) - 0.5));
            this.v = (short) (64.0 * ((-1 * this.transformed_V * h_factor) - 0.5));
        }
        public Object Clone()
        {
            return this.MemberwiseClone();
        }

        public bool Equals(Vtx_Elem other)
        {
            if (this.x != other.x) return false;
            if (this.y != other.y) return false;
            if (this.z != other.z) return false;
            return true;
        }
        public override bool Equals(Object other)
        {
            return this.Equals((Vtx_Elem) other);
        }

        public String print()
        {
            return String.Format("{0}, {1}, {2}", this.x, this.y, this.z);
        }
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x10];
            // XYZ Coords
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.x, 2), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.y, 2), bytes, 0x02);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.z, 2), bytes, 0x04);
            // Padding
            File_Handler.write_bytes_to_buffer(new byte[2]{ 0x00, 0x00 }, bytes, 0x06);
            // UVs
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.u, 2), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.v, 2), bytes, 0x0A);
            // RGBA V-Shades
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.r, 1), bytes, 0x0B);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.g, 1), bytes, 0x0C);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.b, 1), bytes, 0x0D);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.a, 1), bytes, 0x0E);
            return bytes;
        }
    }
    public class Vertex_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public short min_x;
        public short min_y;
        public short min_z;
        public short max_x;
        public short max_y;
        public short max_z;
        public short center_x;
        public short center_y;
        // === 0x10 ===============================
        public short center_z;
        public short local_norm;
        public ushort vtx_count; // for whatever reason; also wrong sometimes
        public short global_norm;

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
                System.Console.WriteLine("No Vertex Segment");
                this.valid = false;
                return;
            }
            this.valid = true;

            this.file_offset = (uint)file_offset;
            this.file_offset_data = (uint)file_offset + 0x18;

            // parsing properties
            // === 0x00 ===============================
            this.min_x = (short)File_Handler.read_short(file_data, file_offset + 0x00, false);
            this.min_y = (short)File_Handler.read_short(file_data, file_offset + 0x02, false);
            this.min_z = (short)File_Handler.read_short(file_data, file_offset + 0x04, false);
            this.max_x = (short)File_Handler.read_short(file_data, file_offset + 0x06, false);
            this.max_y = (short)File_Handler.read_short(file_data, file_offset + 0x08, false);
            this.max_z = (short)File_Handler.read_short(file_data, file_offset + 0x0A, false);
            this.center_x = (short)File_Handler.read_short(file_data, file_offset + 0x0C, false);
            this.center_y = (short)File_Handler.read_short(file_data, file_offset + 0x0E, false);
            // === 0x10 ===============================
            this.center_z = (short)File_Handler.read_short(file_data, file_offset + 0x10, false);
            this.local_norm = (short)File_Handler.read_short(file_data, file_offset + 0x12, false);
            this.vtx_count = File_Handler.read_short(file_data, file_offset + 0x14, false);
            this.global_norm = (short) File_Handler.read_short(file_data, file_offset + 0x16, false);

            int max_dist_ori = 0;

            this.vtx_list = new Vtx_Elem[this.binheader_vtx_cnt];
            for (int i = 0; i < this.binheader_vtx_cnt; i++)
            {
                Vtx_Elem v = new Vtx_Elem();
                int file_offset_vtx = (int)(this.file_offset_data + (i * 0x10));

                // parsing properties
                // === 0x00 ===============================
                v.x = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x00, false);
                v.y = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x02, false);
                v.z = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x04, false);
                v.padding = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x06, false);
                v.u = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x08, false);
                v.v = (short)File_Handler.read_short(file_data, file_offset_vtx + 0x0A, false);
                v.r = File_Handler.read_char(file_data, file_offset_vtx + 0x0C, false);
                v.g = File_Handler.read_char(file_data, file_offset_vtx + 0x0D, false);
                v.b = File_Handler.read_char(file_data, file_offset_vtx + 0x0E, false);
                v.a = File_Handler.read_char(file_data, file_offset_vtx + 0x0F, false);

                int dist = (int)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
                max_dist_ori = (dist > max_dist_ori) ? dist : max_dist_ori;

                vtx_list[i] = v;
            }
            Console.WriteLine(File_Handler.uint_to_string(max_dist_ori, 0xFFFF));
        }

        public void infer_vtx_data_for_full_tris(List<FullTriangle> full_tri_list)
        {
            for (int i = 0; i < full_tri_list.Count; i++)
            {
                FullTriangle full_tri = full_tri_list[i];
                full_tri.vtx_1 = (Vtx_Elem) this.vtx_list[full_tri.index_1].Clone();
                full_tri.vtx_2 = (Vtx_Elem) this.vtx_list[full_tri.index_2].Clone();
                full_tri.vtx_3 = (Vtx_Elem) this.vtx_list[full_tri.index_3].Clone();
            }
        }

        public String export_IDs_to_Base64()
        {
            // NOTE: I am stupid
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.binheader_vtx_cnt; i++)
            {
                raw_data.AddRange(BitConverter.GetBytes((ushort)i));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
        }
        public String export_VTX_Coords_to_Base64()
        {
            List<Byte> raw_data = new List<Byte>();
            for (int i = 0; i < this.binheader_vtx_cnt; i++)
            {
                Vtx_Elem vtx = this.vtx_list[i];
                raw_data.AddRange(BitConverter.GetBytes((Single)vtx.x));
                raw_data.AddRange(BitConverter.GetBytes((Single)vtx.y));
                raw_data.AddRange(BitConverter.GetBytes((Single)vtx.z));
            }
            return System.Convert.ToBase64String(raw_data.ToArray());
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
                "Minimum X",
                File_Handler.uint_to_string(this.min_x, 0xFFFF),
                File_Handler.uint_to_string(this.min_x, 10),
                ""
            });
            content.Add(new string[] {
                "Minimum Y",
                File_Handler.uint_to_string(this.min_y, 0xFFFF),
                File_Handler.uint_to_string(this.min_y, 10),
                ""
            });
            content.Add(new string[] {
                "Minimum Z",
                File_Handler.uint_to_string(this.min_z, 0xFFFF),
                File_Handler.uint_to_string(this.min_z, 10),
                ""
            });
            content.Add(new string[] {
                "Maximum X",
                File_Handler.uint_to_string(this.max_x, 0xFFFF),
                File_Handler.uint_to_string(this.max_x, 10),
                String.Format("Extent = {0} units", (this.max_x - this.min_x))
            });
            content.Add(new string[] {
                "Maximum Y",
                File_Handler.uint_to_string(this.max_y, 0xFFFF),
                File_Handler.uint_to_string(this.max_y, 10),
                String.Format("Extent = {0} units", (this.max_y - this.min_y))
            });
            content.Add(new string[] {
                "Maximum Z",
                File_Handler.uint_to_string(this.max_z, 0xFFFF),
                File_Handler.uint_to_string(this.max_z, 10),
                String.Format("Extent = {0} units", (this.max_z - this.min_z))
            });
            content.Add(new string[] {
                "Center X",
                File_Handler.uint_to_string(this.center_x, 0xFFFF),
                File_Handler.uint_to_string(this.center_x, 10),
                ""
            });
            content.Add(new string[] {
                "Center Y",
                File_Handler.uint_to_string(this.center_y, 0xFFFF),
                File_Handler.uint_to_string(this.center_y, 10)
            });
            content.Add(new string[] {
                "Center Z",
                File_Handler.uint_to_string(this.center_z, 0xFFFF),
                File_Handler.uint_to_string(this.center_z, 10)
            });
            content.Add(new string[] {
                "Local Norm",
                File_Handler.uint_to_string(this.local_norm, 0xFFFF),
                File_Handler.uint_to_string(this.local_norm, 10),
                "largest distance of any vtx to center"
            });
            content.Add(new string[] {
                "VTX count",
                File_Handler.uint_to_string(this.vtx_count, 0xFFFF),
                File_Handler.uint_to_string(this.vtx_count, 10)
            });
            content.Add(new string[] {
                "Global Norm",
                File_Handler.uint_to_string(this.global_norm, 0xFFFF),
                File_Handler.uint_to_string(this.global_norm, 10),
                "largest distance of any vtx to origin"
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
