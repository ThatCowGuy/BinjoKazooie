using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Diagnostics;


namespace BK_BIN_Analyzer
{
    public class BIN_Handler
    {
        byte[] content;

        public Dictionary<int, string> SEGMENT_NAMES = new Dictionary<int, string>
        {
            { 0, "BIN Header" },
            { 1, "Texture Segment" },
            { 2, "DisplayList Segment" },
            { 3, "Vertex Segment" },
            { 4, "Collision Segment" },
            { 5, "Bone Segment" },
            { 6, "GeoLayout Segment" },
        };

        public bool file_loaded = false;
        public string loaded_bin_name;
        public string loaded_bin_path;

        public BIN_Header bin_header = new BIN_Header();
        public Texture_Segment tex_seg = new Texture_Segment();
        public Vertex_Segment vtx_seg = new Vertex_Segment();
        public Bone_Segment bone_seg = new Bone_Segment();
        public Collision_Segment coll_seg = new Collision_Segment();
        public DisplayList_Segment DL_seg = new DisplayList_Segment();

        public void parse_BIN()
        {
            this.file_loaded = false;

            this.content = System.IO.File.ReadAllBytes(this.loaded_bin_path);
            bin_header.populate(this.content);
            tex_seg.populate(this.content, (int)bin_header.tex_offset);
            vtx_seg.binheader_vtx_cnt = bin_header.vtx_cnt;
            vtx_seg.populate(this.content, (int)bin_header.vtx_offset);
            bone_seg.populate(this.content, (int)bin_header.bone_offset);
            coll_seg.populate(this.content, (int)bin_header.coll_offset);
            DL_seg.populate(this.content, (int)bin_header.DL_offset);

            this.file_loaded = true;
        }
        public void overwrite_img_data(int index, byte[] replacement)
        {
            Tex_Data d = this.tex_seg.data[index];
            File_Handler.write_data(this.content, (int)d.file_offset, replacement);
        }
        public void save_BIN()
        {
            if (this.loaded_bin_path.Contains("_copy") == false)
            {
                this.loaded_bin_path = this.loaded_bin_path.Replace(".bin", "_copy.bin");
            }
            System.IO.File.WriteAllBytes(this.loaded_bin_path, this.content);
            System.Console.WriteLine(String.Format("Overwriting File {0}...", this.loaded_bin_path));
            return;
        }
        public void select_BIN()
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            OFD.Filter = "BIN model Files (*.bin)|*.BIN|All Files (*.*)|*.*";
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                System.Console.WriteLine(String.Format("Loading File {0}...", OFD.FileName));
            }
            else
            {
                System.Console.WriteLine(String.Format("Cancelled."));
                return;
            }

            this.loaded_bin_path = OFD.FileName;
            int last_slash = this.loaded_bin_path.LastIndexOf("\\");
            int file_ext = this.loaded_bin_path.LastIndexOf(".");
            this.loaded_bin_name = this.loaded_bin_path.Substring((last_slash + 1), (file_ext - last_slash - 1));
            this.parse_BIN();
        }

        public String get_default_texture_name(int index)
        {
            return String.Format("{0}_{1:0000}.png", this.loaded_bin_name, index);
        }

        public void export_image_of_element(int index, bool choose_name)
        {
            if (index < 0 || index >= tex_seg.tex_cnt)
            {
                Console.WriteLine("Received invalid index for Image Export.");
                return;
            }
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), get_default_texture_name(index));
            if (choose_name == true)
            {
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
                SFD.FileName = chosen_filename;
                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    chosen_filename = SFD.FileName;
                    System.Console.WriteLine(String.Format("Saving Image File {0}...", chosen_filename));
                    tex_seg.data[index].img_rep.Save(chosen_filename);
                    return;
                }
                System.Console.WriteLine("Cancelled");
                return;
            }
            System.Console.WriteLine(String.Format("Saving Image File {0}...", chosen_filename));
            tex_seg.data[index].img_rep.Save(chosen_filename);
        }
        public void export_displaylist_model()
        {
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}_DL.obj", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));
                write_displaylist_model(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        public void export_collision_model()
        {
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}_COLL.obj", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));
                write_collision_model(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        public void write_displaylist_model(string filepath)
        {
            // storing the vtx IDs as loaded by the DLs
            int[] simulated_vtx_buffer = new int[0x20];
            uint written_vertices = 0;
            Tile_Descriptor[] tile_descriptor = new Tile_Descriptor[]
            {
                new Tile_Descriptor(),
                new Tile_Descriptor(),
                new Tile_Descriptor()
            };

            String mtl_path = filepath.Substring(0, (filepath.Length - 4)) + ".mtl";
            using (StreamWriter output_mtl = new StreamWriter(mtl_path))
            {
                for (int i = 0; i < this.tex_seg.tex_cnt; i++)
                {
                    String default_filename = get_default_texture_name(i);

                    // using the default filename as the mtl name, assuming its in exports/
                    // and writing some default params
                    output_mtl.WriteLine("newmtl " + default_filename);
                    output_mtl.WriteLine("Ka 1.000000 1.000000 1.000000");
                    output_mtl.WriteLine("Kd 1.000000 1.000000 1.000000");
                    output_mtl.WriteLine("Ks 1.000000 1.000000 1.000000");
                    output_mtl.WriteLine("Ns 100.000000");
                    output_mtl.WriteLine("Ni 1.000000");
                    output_mtl.WriteLine("d 1.000000");
                    output_mtl.WriteLine("illum 0");
                    output_mtl.WriteLine("map_Kd " + default_filename);
                }
            }

            using (StreamWriter output_obj = new StreamWriter(filepath))
            {
                // before we do anything meaningful, we specify the MTL file
                output_obj.WriteLine("mtllib " + this.loaded_bin_name + ".mtl");

                foreach (DisplayList_Command cmd in this.DL_seg.command_list)
                {
                    switch (cmd.command_name)
                    {
                        case ("G_VTX"):
                            uint vtx_cnt = cmd.parameters[1];
                            // since this is the actual offset, I'll calculate the vtx ID manually
                            uint offset = (cmd.parameters[4] / 0x10);
                            // write the corresponding vtx into the simulated buffer
                            for (int i = 0; i < vtx_cnt; i++)
                            {
                                simulated_vtx_buffer[cmd.parameters[0] + i] = (int)(offset + i);
                            }
                            break;

                        case ("G_SETTIMG"):
                            // find the tex that corresponds to this address
                            for (int i = 0; i < this.tex_seg.tex_cnt; i++)
                            {
                                if (cmd.parameters[3] == this.tex_seg.data[i].datasection_offset)
                                {
                                    tile_descriptor[0].assigned_tex_meta = this.tex_seg.meta[i];
                                    tile_descriptor[0].assigned_tex_data = this.tex_seg.data[i];
                                    String corresponding_filename = get_default_texture_name(i);
                                    output_obj.WriteLine("usemtl " + corresponding_filename);
                                    break;
                                }
                            }
                            break;

                        case ("G_TRI1"):
                            // write the 3 vtx to the obj file  
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[0]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[1]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[2]], tile_descriptor[0]));
                            // and the corresponding tri
                            output_obj.WriteLine(String.Format("f {0}/{0} {1}/{1} {2}/{2}", (written_vertices + 1), (written_vertices + 2), (written_vertices + 3)));
                            written_vertices += 3;
                            break;

                        case ("G_TRI2"):
                            // write the 3 vtx to the obj file  
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[0]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[1]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[2]], tile_descriptor[0]));
                            // and the corresponding tri
                            output_obj.WriteLine(String.Format("f {0}/{0} {1}/{1} {2}/{2}", (written_vertices + 1), (written_vertices + 2), (written_vertices + 3)));
                            written_vertices += 3;
                            // write the 3 vtx to the obj file
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[3]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[4]], tile_descriptor[0]));
                            output_obj.WriteLine(write_vertex(simulated_vtx_buffer[cmd.parameters[5]], tile_descriptor[0]));
                            // and the corresponding tri
                            output_obj.WriteLine(String.Format("f {0}/{0} {1}/{1} {2}/{2}", (written_vertices + 1), (written_vertices + 2), (written_vertices + 3)));
                            written_vertices += 3;
                            break;
                    }
                }
            }
        }

        public String write_vertex(int id, Tile_Descriptor tiledes)
        {
            String output = "";

            Vtx_Elem vtx = this.vtx_seg.vtx_list[id];
            output += String.Format("v {0,0:F8} {1,0:F8} {2,0:F8}\n", vtx.x, vtx.y, vtx.z);

            double uscale = (1.0 / tiledes.assigned_tex_meta.width);
            double calc_u = ((vtx.u / 64.0) + tiledes.S_shift + 0.5) / tiledes.assigned_tex_meta.width;
            double vscale = (1.0 / tiledes.assigned_tex_meta.height);
            double calc_v = ((vtx.v / 64.0) + tiledes.T_shift + 0.5) / tiledes.assigned_tex_meta.height;
            output += String.Format("vt {0,0:F8} {1,0:F8}", calc_u, calc_v);
            return output;
        }

        public void write_collision_model(string filepath)
        {
            using (StreamWriter output_obj = new StreamWriter(filepath))
            {
                foreach (Vtx_Elem vtx in this.vtx_seg.vtx_list)
                {
                    output_obj.WriteLine(String.Format("v {0,0:F8} {1,0:F8} {2,0:F8}", vtx.x, vtx.y, vtx.z));
                }
                foreach (Tri_Elem tri in this.coll_seg.tri_list)
                {
                    output_obj.WriteLine(String.Format("f {0} {1} {2}", (tri.index_1 + 1), (tri.index_2 + 1), (tri.index_3 + 1)));
                }
            }
        }
    }
}
