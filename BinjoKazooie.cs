using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace BK_BIN_Analyzer
{
    public partial class BinjoKazooie : Form
    {
        public BIN_Handler handler = new BIN_Handler();

        public BinjoKazooie()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // why isnt this default ??
            this.checkBox1.Visible = false;
            this.Width = 585;
            update_display();
        }

        public void set_bounds_for_numericUpDown2()
        {
            this.numericUpDown2.Minimum = 0;
            this.numericUpDown2.Value = 0;
            switch (this.numericUpDown1.Value)
            {
                case (1):
                    this.numericUpDown2.Maximum = this.handler.tex_seg.tex_cnt - 1;
                    break;
                default:
                    this.numericUpDown2.Maximum = 0;
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.handler.select_BIN();

            this.numericUpDown1.Value = 0;
            this.numericUpDown1.Minimum = 0;
            this.numericUpDown1.Maximum = this.handler.SEGMENT_NAMES.Count - 1;

            this.set_bounds_for_numericUpDown2();

            this.update_display();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            //ControlPaint.DrawBorder(e.Graphics, this.panel1.ClientRectangle, Color.White, ButtonBorderStyle.Solid);
        }
        private void panel2_Paint_1(object sender, PaintEventArgs e)
        {
            //ControlPaint.DrawBorder(e.Graphics, this.panel2.ClientRectangle, Color.White, ButtonBorderStyle.Solid);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        public const int DGV_HEADER_HEIGHT = 24;
        public const int DGV_ROW_HEIGHT = 22;
        public const int DGV_OVERHANG = 10;

        public void setup_DGV(DataGridView DGV)
        {
            // reset the grid contents
            DGV.Rows.Clear();
            DGV.Columns.Clear();
            // fix a couple sizes
            DGV.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            DGV.ColumnHeadersHeight = DGV_HEADER_HEIGHT;
        }
        public void finish_up_DGV(DataGridView DGV)
        {
            DGV.Height = (DGV.RowCount * DGV_ROW_HEIGHT) + DGV_HEADER_HEIGHT + DGV_OVERHANG;
            colorize_DGV(DGV);
            DGV.ClearSelection();
        }
        public System.Drawing.Point get_bottom_of_DGV_1()
        {
            return new System.Drawing.Point(
                this.panel1.Location.X,
                this.panel1.Location.Y + this.dataGridView1.Location.Y + this.dataGridView1.Height + 28
            );
        }
        public void colorize_DGV(DataGridView DGV)
        {
            // colors go brrrrrrr
            DGV.EnableHeadersVisualStyles = false;
            Color head_color = Color.FromArgb(255, 255, 192, 128);
            DGV.ColumnHeadersDefaultCellStyle.BackColor = head_color;
            Color cell_color = Color.FromArgb(255, 255, 224, 192);
            for (int col = 0; col < DGV.ColumnCount; col++)
                DGV.Columns[col].DefaultCellStyle.BackColor = cell_color;
        }
        public void disable_button(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.Enabled = false;
        }
        public void enable_button(Button btn)
        {
            btn.FlatStyle = FlatStyle.Standard;
            btn.Enabled = true;
        }

        // this is where most the GUI shenanigans happen. but to be honest, Im only
        // writing this comment to find this function easier...
        public void update_display()
        {
            disable_button(button2);
            disable_button(button4);

            string seg_name = this.handler.SEGMENT_NAMES[(int)numericUpDown1.Value];
            label2.Text = (seg_name);
            label2.Update();
            label3.Text = String.Format("Loaded BIN = {0}", this.handler.loaded_bin_name);
            label3.Update();

            this.panel2.Visible = false;
            this.panel3.Visible = false;
            this.panel4.Visible = false;

            if (this.handler.file_loaded == true)
            {
                convert_replacement_to_fitting();

                setup_DGV(dataGridView1);
                setup_DGV(dataGridView2);
                setup_DGV(dataGridView3);
                setup_DGV(dataGridView4);

                // add descriptors to grid 1
                dataGridView1.Columns.Add("0", "Description");
                dataGridView1.Columns.Add("1", "Hex Value");
                dataGridView1.Columns.Add("2", "Interpretation");
                dataGridView1.Columns.Add("3", "Notes");
                dataGridView1.Columns[0].Width = 120;
                dataGridView1.Columns[1].Width = 75;
                dataGridView1.Columns[2].Width = 90;

                if (seg_name == "BIN Header")
                {
                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.bin_header.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);
                }
                if (seg_name == "Texture Segment")
                {
                    this.panel2.Visible = true;

                    enable_button(button2);
                    this.numericUpDown2.Visible = true;

                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.tex_seg.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);
                    this.panel2.Location = get_bottom_of_DGV_1();

                    // add descriptors to grid 2
                    dataGridView2.Columns.Add("0", "Description");
                    dataGridView2.Columns.Add("1", "Hex Value");
                    dataGridView2.Columns.Add("2", "Interpretation");
                    dataGridView2.Columns[0].Width = 120;
                    dataGridView2.Columns[1].Width = 75;

                    // Sanity check to see if index is valid
                    if (this.handler.tex_seg.tex_cnt > 0 && this.numericUpDown2.Value < this.handler.tex_seg.tex_cnt)
                    {
                        foreach (string[] element in handler.tex_seg.get_content_of_element((int) this.numericUpDown2.Value))
                            dataGridView2.Rows.Add(element);

                        Tex_Meta m = this.handler.tex_seg.meta[(int)this.numericUpDown2.Value];
                        int display_w = 128;
                        int display_h = 128;
                        double wm_ratio = ((double)m.width / (double)m.height);
                        if (wm_ratio > 1.0) display_h = (int)(display_h / wm_ratio);
                        if (wm_ratio < 1.0) display_w = (int)(display_w * wm_ratio);
                        pictureBox1.Image = new Bitmap(
                            handler.tex_seg.data[(int) this.numericUpDown2.Value].img_rep,
                            display_w, display_h
                        );
                        pictureBox1.Update();

                        if (this.replacement_cvt != null)
                        {
                            enable_button(button4);
                        }
                    }
                    finish_up_DGV(dataGridView2);
                }
                if (seg_name == "Bone Segment")
                {
                    foreach (string[] element in handler.bone_seg.get_content())
                        dataGridView1.Rows.Add(element);
                    finish_up_DGV(dataGridView1);

                    this.panel4.Visible = true;
                    this.panel4.Location = get_bottom_of_DGV_1();

                    // add descriptors to grid 3
                    dataGridView4.Columns.Add("0", "ID");
                    dataGridView4.Columns[0].DividerWidth = 3;
                    dataGridView4.Columns.Add("1", "X");
                    dataGridView4.Columns.Add("2", "Y");
                    dataGridView4.Columns.Add("3", "Z");
                    dataGridView4.Columns[3].DividerWidth = 3;
                    dataGridView4.Columns.Add("4", "Internal Bone ID");
                    dataGridView4.Columns.Add("5", "Parent Bone ID");
                    dataGridView4.Columns[0].Width = 75;
                    dataGridView4.Columns[1].Width = 75;
                    dataGridView4.Columns[2].Width = 75;
                    dataGridView4.Columns[3].Width = 75;

                    if (this.checkBox2.Checked == true)
                    {
                        for (int id = 0; id < handler.bone_seg.bone_cnt; id++)
                        {
                            foreach (string[] element in handler.bone_seg.get_bone_content(id))
                                dataGridView4.Rows.Add(element);
                        }
                        // dataGridView4 is a special one:
                        // should be scrollable, so we dont use the standard method with data
                        colorize_DGV(dataGridView4);
                        dataGridView4.ClearSelection();
                        dataGridView4.Height = 230;
                    }
                    else
                    {
                        finish_up_DGV(dataGridView4);
                    }
                    panel4.Height = 32 + dataGridView4.Height + 16;
                }
                if (seg_name == "Vertex Segment")
                {
                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.vtx_seg.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);

                    this.panel3.Visible = true;
                    this.panel3.Location = get_bottom_of_DGV_1();

                    // add descriptors to grid 3
                    dataGridView3.Columns.Add("0", "ID");
                    dataGridView3.Columns[0].DividerWidth = 3;
                    dataGridView3.Columns.Add("1", "X");
                    dataGridView3.Columns.Add("2", "Y");
                    dataGridView3.Columns.Add("3", "Z");
                    dataGridView3.Columns[3].DividerWidth = 3;
                    dataGridView3.Columns.Add("4", "U");
                    dataGridView3.Columns.Add("5", "V");
                    dataGridView3.Columns[5].DividerWidth = 3;
                    dataGridView3.Columns.Add("6", "R");
                    dataGridView3.Columns.Add("7", "G");
                    dataGridView3.Columns.Add("8", "B");
                    dataGridView3.Columns[8].DividerWidth = 3;
                    dataGridView3.Columns.Add("9", "A");

                    if (this.checkBox1.Checked == true)
                    {
                        for (int id = 0; id < handler.vtx_seg.binheader_vtx_cnt; id++)
                        {
                            foreach (string[] element in handler.vtx_seg.get_vtx_content(id))
                                dataGridView3.Rows.Add(element);
                        }
                        // dataGridView3 is a special one:
                        // should be scrollable, so we dont use the standard method with data
                        colorize_DGV(dataGridView3);
                        dataGridView3.ClearSelection();
                        dataGridView3.Height = 320;
                    }
                    else
                    {
                        finish_up_DGV(dataGridView3);
                    }
                    panel3.Height = 32 + dataGridView3.Height + 16;

                }
                panel1.Height = 32 + dataGridView1.Height + 16;
            }
        }


        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            // reset this one to avoid shenanigans
            this.set_bounds_for_numericUpDown2();
            this.numericUpDown2.Value = this.numericUpDown2.Minimum;

            string seg_name = this.handler.SEGMENT_NAMES[(int)numericUpDown1.Value];
            if (seg_name == "Vertex Segment")
            {
                this.checkBox1.Visible = true;
                // this one is off by default, because the header might be of more interest
                this.checkBox1.Checked = false;
            }
            else if (seg_name == "Bone Segment")
            {
                // make this one depend on the amount of bone data to show
                this.checkBox2.Checked = (handler.bone_seg.bone_cnt > 32) ? false : true;
            }
            else
            {
                this.checkBox1.Visible = false;
            }

            this.update_display();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            this.update_display();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        public void export_image(Bitmap img)
        {
            string chosen_filename = Path.Combine(Directory.GetCurrentDirectory(), String.Format("converted.png"));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                System.Console.WriteLine(String.Format("Saving Image File {0}...", chosen_filename));
                img.Save(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            export_image(this.replacement_cvt);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            this.handler.export_image_of_element((int) this.numericUpDown2.Value, true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.handler.tex_seg.tex_cnt; i++)
                this.handler.export_image_of_element(i, false);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (this.replacement_ori == null || this.replacement_cvt == null)
            {
                System.Console.WriteLine(String.Format("No Replacement IMG loaded; Cancelled."));
                return;
            }
            this.convert_replacement_to_fitting();
            int tex_type = this.handler.tex_seg.meta[(int)this.numericUpDown2.Value].tex_type;
            byte[] replacement_data = MathHelpers.convert_bitmap_to_bytes(this.replacement_cvt, tex_type);
            Console.WriteLine(tex_type);
            Console.WriteLine(replacement_data.Length);
            this.handler.overwrite_img_data((int) this.numericUpDown2.Value, replacement_data);
            this.handler.save_BIN();
            this.handler.parse_BIN();
            this.update_display();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.update_display();
        }
        public Bitmap replacement_ori;
        public Bitmap replacement_cvt;
        public List<MathHelpers.ColorPixel> replacement_palette;
        public void convert_replacement_to_fitting()
        {
            if (this.replacement_ori == null)
            {
                Console.WriteLine("Nothing to convert yet.");
                return;
            }

            int new_w = 128;
            int new_h = 128;
            int display_w = 128;
            int display_h = 128;
            if (this.handler.file_loaded == false || this.handler.tex_seg.tex_cnt < 1)
            {
                // not doing any conversion
                this.replacement_cvt = new Bitmap(this.replacement_ori);
            }
            else
            {
                Tex_Meta m = this.handler.tex_seg.meta[(int)this.numericUpDown2.Value];
                new_w = m.width;
                new_h = m.height;
                double wm_ratio = ((double)new_w / (double)new_h);
                // the display version is scaled up (always. but keep the ratio)
                if (wm_ratio > 1.0) display_h = (int)(display_h / wm_ratio);
                if (wm_ratio < 1.0) display_w = (int)(display_w * wm_ratio);

                switch (m.tex_type)
                {
                    case (0x01): // C4 or CI4; 16 RGB555-colors, pixels are encoded per row as 4bit IDs
                    { 
                        int col_cnt = 16;
                        this.replacement_palette = MathHelpers.approx_palette_by_most_used_with_diversity(
                            new Bitmap(replacement_ori, new_w, new_h),
                            col_cnt, (int)this.numericUpDown3.Value
                        );
                        this.replacement_cvt = MathHelpers.convert_image_to_RGB555_with_palette(
                            new Bitmap(replacement_ori, new_w, new_h),
                            this.replacement_palette
                        );
                        break;
                    }
                    case (0x02): // C8 or CI8; 32 RGB555-colors, pixels are encoded per row as 8bit IDs
                    {
                        int col_cnt = 32;
                        this.replacement_palette = MathHelpers.approx_palette_by_most_used_with_diversity(
                            new Bitmap(replacement_ori, new_w, new_h),
                            col_cnt, (int)this.numericUpDown3.Value
                        );
                        this.replacement_cvt = MathHelpers.convert_image_to_RGB555_with_palette(
                            new Bitmap(replacement_ori, new_w, new_h),
                            this.replacement_palette
                        );
                        break;
                    }
                    case (0x04): // RGBA16 or RGB555A1 without a palette; pixels stored as a 16bit texel
                    {
                        this.replacement_cvt = MathHelpers.convert_image_to_RGB555(
                            new Bitmap(replacement_ori, new_w, new_h)
                        );
                        break;
                    }
                    case (0x08): // RGBA32 or RGB888A8 without a palette; pixels stored as a 32bit texel
                    {
                        this.replacement_cvt = new Bitmap(this.replacement_ori, new_w, new_h);
                        break;
                    }
                    case (0x10): // IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha;
                    {
                        this.replacement_cvt = MathHelpers.convert_image_to_IA8(
                            new Bitmap(replacement_ori, new_w, new_h)
                        );
                        break;
                    }
                    default: // UNKNOWN !";
                        break;
                }
            }
            this.pictureBox2.Image = new Bitmap(this.replacement_cvt, display_w, display_h);
            this.pictureBox2.Update();
        }
        private void button3_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                System.Console.WriteLine(String.Format("Loading File {0}...", OFD.FileName));
            }
            else
            {
                System.Console.WriteLine(String.Format("Cancelled."));
                return;
            }
            using (var tmp_img = new Bitmap(OFD.FileName))
            {
                this.replacement_ori = new Bitmap(tmp_img);
            }
            this.convert_replacement_to_fitting();
            this.update_display();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            convert_replacement_to_fitting();
            this.update_display();
        }



        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            update_display();
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            update_display();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            Process.Start("https://hack64.net/wiki/doku.php?id=banjo_kazooie");
        }
        private void button8_Click(object sender, EventArgs e)
        {
            Process.Start("https://docs.google.com/document/d/1wcETwmo98Xfn_MUZ58qS6XAY_ikr592Bz5cl27hTr14/edit");
        }
    }
}
