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
            { 5, "Animation Segment" },
            { 6, "GeoLayout Segment" },
        };

        public bool file_loaded = false;
        public string loaded_bin_name;

        public BIN_Header bin_header = new BIN_Header();
        public Texture_Segment tex_seg = new Texture_Segment();
        public Vertex_Segment vtx_seg = new Vertex_Segment();
        public void parse_BIN()
        {
            this.file_loaded = false;

            this.content = System.IO.File.ReadAllBytes(this.loaded_bin_name);
            bin_header.populate(this.content);
            tex_seg.populate(this.content, (int)bin_header.tex_offset);
            vtx_seg.binheader_vtx_cnt = bin_header.vtx_cnt;
            vtx_seg.populate(this.content, (int)bin_header.vtx_offset);

            this.file_loaded = true;
        }
        public void overwrite_img_data(int index, byte[] replacement)
        {
            Tex_Data d = this.tex_seg.data[index];
            File_Handler.write_data(this.content, (int)d.file_offset, replacement);
        }
        public void save_BIN()
        {
            if (this.loaded_bin_name.Contains("_copy") == false)
            {
                this.loaded_bin_name = this.loaded_bin_name.Replace(".bin", "_copy.bin");
            }
            System.IO.File.WriteAllBytes(this.loaded_bin_name, this.content);
            System.Console.WriteLine(String.Format("Overwriting File {0}...", this.loaded_bin_name));
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

            this.loaded_bin_name = OFD.FileName;
            this.parse_BIN();
        }
        public void export_image_of_element(int index, bool choose_name)
        {
            if (index < 0 || index >= tex_seg.tex_cnt)
            {
                Console.WriteLine("Received invalid index for Image Export.");
                return;
            }
            string chosen_filename = Path.Combine(Directory.GetCurrentDirectory(), String.Format("tex_{0:0000}.png", index));
            if (choose_name == true)
            {
                SaveFileDialog SFD = new SaveFileDialog();
                SFD.InitialDirectory = File_Handler.get_basedir_or_assets();
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
    }
}
