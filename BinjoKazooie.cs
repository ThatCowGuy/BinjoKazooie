﻿using System;
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

namespace Binjo
{
    public partial class BinjoKazooie : Form
    {
        public BIN_Handler handler = new BIN_Handler();
        public static String version = "v1.1.0";

        public Boolean texture_converter_active = false;

        public BinjoKazooie()
        {
            InitializeComponent();
            this.Text = String.Format("BINjo Kazooie  -  a BIN Modification Tool   ( {0} )", BinjoKazooie.version);
            this.DoubleBuffered = true; // why isnt this default ??
            this.checkBox1.Visible = false;
            this.Width = 585;
            this.segName_comboBox.SelectedIndex = 0;
            this.Converter_Format_ComBox.SelectedIndex = 0;
            this.Converter_Width_ComBox.SelectedIndex = this.Converter_Width_ComBox.FindString("64");
            this.Converter_Height_ComBox.SelectedIndex = this.Converter_Height_ComBox.FindString("64");
            update_display();
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        public void set_bounds_for_numericUpDown2()
        {
            this.numericUpDown2.Minimum = 0;
            this.numericUpDown2.Value = 0;
            switch (this.segName_comboBox.Text)
            {
                case ("Texture Segment"):
                    this.numericUpDown2.Maximum = this.handler.tex_seg.tex_cnt - 1;
                    break;
                default:
                    break;
            }
        }

        private void load_bin_button_Click(object sender, EventArgs e)
        {
            this.handler.load_BIN();

            this.texture_converter_active = false;
            this.set_bounds_for_numericUpDown2();

            this.update_display();
        }
        private void export_BIN_button_Click(object sender, EventArgs e)
        {
            this.handler.save_BIN();
        }
        private void load_GLTF_button_Click(object sender, EventArgs e)
        {
            this.handler.load_GLTF();

            this.texture_converter_active = false;
            this.set_bounds_for_numericUpDown2();

            update_display();
        }
        private void export_GLTF_button_Click(object sender, EventArgs e)
        {
            this.handler.save_GLTF();
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

        private void button1_Click_1(object sender, EventArgs e)
        {
            this.texture_converter_active = true;
            this.update_display();
        }

        // this is where most the GUI shenanigans happen. but to be honest, Im only
        // writing this comment to find this function easier...
        public void update_display()
        {
            // create both the replacement-suitable and custom-conversion IMGs + GUI updates
            convert_replacement_to_fitting();
            convert_converter_to_fitting();

            if (this.converter_cvt != null)
            {
                uint cvt_w = (uint) Int32.Parse(this.Converter_Width_ComBox.Text);
                uint cvt_h = (uint) Int32.Parse(this.Converter_Height_ComBox.Text);
                string cvt_tex_type = this.Converter_Format_ComBox.Text;
                uint cvt_data_size = Texture_Segment.get_datasize(cvt_w, cvt_h, cvt_tex_type);
                Converter_TexDataSize_Label.Text = String.Format("Converted Tex DataSize: 0x{0}", File_Handler.uint_to_string(cvt_data_size, 0xFFFFFFFF));
                Converter_TexDataSize_Label.Update();
            }

            disable_button(button2);
            disable_button(button4);

            string seg_name = segName_comboBox.Text;
            label3.Text = String.Format("Loaded BIN = {0}", this.handler.loaded_bin_path);
            label3.Update();

            // the default for panel1 is to be visible
            this.panel1.Visible = true;

            this.panel2.Visible = false;
            this.panel3.Visible = false;
            this.panel4.Visible = false;
            this.panel5.Visible = false;
            this.panel6.Visible = false;
            this.panel7.Visible = false;
            this.panel8.Visible = false; // Texture Converter Panel

            if (this.texture_converter_active == true)
            {
                this.panel8.Visible = true;

                // effectively replace panel 1 with this one
                this.panel1.Visible = false;
                this.panel8.Location = this.panel1.Location;
            }
            else if (this.handler.file_loaded == true)
            {
                convert_replacement_to_fitting();

                setup_DGV(dataGridView1);
                setup_DGV(dataGridView2);
                setup_DGV(dataGridView3);
                setup_DGV(dataGridView4);
                setup_DGV(dataGridView5);
                setup_DGV(dataGridView6);
                setup_DGV(dataGridView7);

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
                    foreach (string[] element in handler.bin_header.get_content())
                        dataGridView1.Rows.Add(element);

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

                        Tex_Meta m = this.handler.tex_seg.meta[(int) this.numericUpDown2.Value];
                        int display_w = 128;
                        int display_h = 128;
                        double wm_ratio = ((double) m.width / (double) m.height);
                        if (wm_ratio > 1.0) display_h = (int) (display_h / wm_ratio);
                        if (wm_ratio < 1.0) display_w = (int) (display_w * wm_ratio);
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
                if (seg_name == "DisplayList Segment")
                {
                    this.panel6.Visible = true;

                    // this header only has 1 meaningful entry anyways, so keep it visible
                    foreach (string[] element in handler.DL_seg.get_content())
                        dataGridView1.Rows.Add(element);
                    finish_up_DGV(dataGridView1);
                    this.panel6.Location = get_bottom_of_DGV_1();

                    // add descriptors to grid 2
                    dataGridView6.Columns.Add("0", "code");
                    dataGridView6.Columns.Add("1", "name");
                    dataGridView6.Columns.Add("2", "Interpretation");
                    dataGridView6.Columns[0].Width = 50;
                    dataGridView6.Columns[1].Width = 160;

                    if (this.checkBox3.Checked == true)
                    {
                        for (int id = 0; id < handler.DL_seg.command_cnt; id++)
                        {
                            foreach (string[] element in handler.DL_seg.get_cmd_content(id))
                                dataGridView6.Rows.Add(element);
                        }
                        // dataGridView6 is a special one:
                        // should be scrollable, so we dont use the standard method with data
                        colorize_DGV(dataGridView6);
                        dataGridView6.ClearSelection();
                        dataGridView6.Height = 330;
                    }
                    else
                    {
                        finish_up_DGV(dataGridView6);
                    }
                    panel6.Height = 32 + dataGridView6.Height + 16;
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
                if (seg_name == "Collision Segment")
                {
                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.coll_seg.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);

                    this.panel5.Visible = true;
                    this.panel5.Location = get_bottom_of_DGV_1();

                    // some extra resizing for the hex vals here
                    dataGridView1.Columns[1].Width = 110;
                    dataGridView1.Columns[2].Width = 70;

                    // add descriptors to grid 5
                    dataGridView5.Columns.Add("0", "TRI-ID");
                    dataGridView5.Columns[0].DividerWidth = 3;
                    dataGridView5.Columns.Add("1", "VTX-ID 1");
                    dataGridView5.Columns.Add("2", "VTX-ID 2");
                    dataGridView5.Columns.Add("3", "VTX-ID 3");
                    dataGridView5.Columns[3].DividerWidth = 3;
                    dataGridView5.Columns.Add("4", "Floor Type");
                    dataGridView5.Columns.Add("5", "Sound Type");
                    dataGridView5.Columns[0].Width = 70;
                    dataGridView5.Columns[1].Width = 70;
                    dataGridView5.Columns[2].Width = 70;
                    dataGridView5.Columns[3].Width = 70;

                    if (this.checkBox1.Checked == true)
                    {
                        for (int id = 0; id < handler.coll_seg.tri_cnt; id++)
                        {
                            foreach (string[] element in handler.coll_seg.get_tri_content(id))
                                dataGridView5.Rows.Add(element);
                        }
                        // dataGridView5 is a special one:
                        // should be scrollable, so we dont use the standard method with data
                        colorize_DGV(dataGridView5);
                        dataGridView5.ClearSelection();
                        dataGridView5.Height = 320;
                    }
                    else
                    {
                        finish_up_DGV(dataGridView5);
                    }
                    panel5.Height = 32 + dataGridView5.Height + 16;
                }
                if (seg_name == "Effects Segment")
                {
                    foreach (string[] element in handler.FX_seg.get_content())
                        dataGridView1.Rows.Add(element);

                    finish_up_DGV(dataGridView1);
                }
                if (seg_name == "Effects-End Segment")
                {
                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.FXEND_seg.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);
                }
                if (seg_name == "Animated Texture Segment")
                {
                    if (this.checkBox1.Checked == false)
                    {
                        foreach (string[] element in handler.animtex_seg.get_content())
                            dataGridView1.Rows.Add(element);
                    }
                    finish_up_DGV(dataGridView1);
                }
                if (seg_name == "GeoLayout Segment")
                {
                    foreach (string[] element in handler.geo_seg.get_content())
                        dataGridView1.Rows.Add(element);

                    finish_up_DGV(dataGridView1);

                    this.panel7.Visible = true;
                    this.panel7.Location = get_bottom_of_DGV_1();

                    // add descriptors to grid 7
                    dataGridView7.Columns.Add("0", "GeoLayout Command Chain");

                    foreach (string[] element in handler.geo_seg.get_content_of_elements())
                        dataGridView7.Rows.Add(element);

                    // dataGridView7 is a special one:
                    // should be scrollable, so we dont use the standard method with data
                    colorize_DGV(dataGridView7);
                    dataGridView7.ClearSelection();
                    dataGridView7.Height = 350;
                    panel7.Height = 32 + dataGridView7.Height + 16;
                }
                panel1.Height = 32 + dataGridView1.Height + 16;
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            this.update_display();
        }

        private void segName_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // reset this one to avoid shenanigans
            this.set_bounds_for_numericUpDown2();
            this.numericUpDown2.Value = this.numericUpDown2.Minimum;
            this.checkBox1.Visible = false;

            string seg_name = this.segName_comboBox.Text;
            if (seg_name == "DisplayList Segment")
            {
                this.checkBox1.Visible = true;
                // make this one depend on the amount of DL data to show
                this.checkBox3.Checked = (handler.DL_seg.command_cnt > 32) ? false : true;
            }
            if (seg_name == "Vertex Segment")
            {
                this.checkBox1.Visible = true;
                // this one is off by default, because the header might be of more interest
                this.checkBox1.Checked = false;
            }
            if (seg_name == "Bone Segment")
            {
                this.checkBox1.Visible = true;
                // make this one depend on the amount of bone data to show
                this.checkBox2.Checked = (handler.bone_seg.bone_cnt > 32) ? false : true;
            }
            if (seg_name == "Collision Segment")
            {
                this.checkBox1.Visible = true;
                // make this one depend on the amount of triangle data to show
                this.checkBox2.Checked = (handler.coll_seg.tri_cnt > 32) ? false : true;
            }
            this.update_display();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        public void export_image(Bitmap img)
        {
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("converted.png"));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
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
        private void Converter_Export_Btn_Click(object sender, EventArgs e)
        {
            export_image(this.converter_cvt);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Hello");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.handler.export_image_of_element((int) this.numericUpDown2.Value, true);
        }
        private void button9_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < this.handler.tex_seg.tex_cnt; i++)
                this.handler.export_image_of_element(i, false);
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

            int tex_ID = (int) this.numericUpDown2.Value;
            int tex_type = this.handler.tex_seg.meta[tex_ID].tex_type;
            uint tex_w = this.handler.tex_seg.meta[tex_ID].width;
            uint tex_h = this.handler.tex_seg.meta[tex_ID].height;

            byte[] replacement_data = MathHelpers.convert_bitmap_to_bytes(this.replacement_cvt, tex_type);

            this.handler.tex_seg.data[tex_ID].data = replacement_data;
            this.handler.tex_seg.data[tex_ID].img_rep = Texture_Segment.parse_img_data(replacement_data, (uint) tex_type, tex_w, tex_h);
            this.handler.overwrite_img_data((int) this.numericUpDown2.Value, replacement_data);

            this.update_display();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.update_display();
        }

        // replacement_ori is the currently loaded IMG;
        // replacement_cvt is the converted IMG suitable for tex replacement
        // converter_cvt is the converted IMG within the tex-converter
        public Bitmap replacement_ori;
        public Bitmap replacement_cvt;
        public Bitmap converter_cvt;

        public List<MathHelpers.ColorPixel> replacement_palette;
        public List<MathHelpers.ColorPixel> converter_palette;
        public void convert_replacement_to_fitting()
        {
            if (this.replacement_ori == null)
            {
                return;
            }

            int new_w = 128;
            int new_h = 128;
            int display_w = 128;
            int display_h = 128;
            if (this.handler.file_loaded == false || this.handler.tex_seg.tex_cnt < 1)
            {
                // not doing any conversion
                Console.WriteLine("Nothing to convert to yet.");
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

                this.replacement_cvt = Texture_Segment.convert_to_fit(this.replacement_ori, new_w, new_h, m.tex_type, (int) this.numericUpDown3.Value);
            }
            this.pictureBox2.Image = new Bitmap(this.replacement_cvt, display_w, display_h);
            this.pictureBox2.Update();
        }
        public void convert_converter_to_fitting()
        {
            if (this.replacement_ori == null)
            {
                return;
            }

            int new_w = Int32.Parse(this.Converter_Width_ComBox.Text);
            int new_h = Int32.Parse(this.Converter_Height_ComBox.Text);
            int new_tex_type = Texture_Segment.TEX_TYPES[this.Converter_Format_ComBox.Text];
            double wm_ratio = ((double) new_w / (double) new_h);
            int display_w = 128;
            int display_h = 128;
            // the display version is scaled up (always. but keep the ratio)
            if (wm_ratio > 1.0) display_h = (int) (display_h / wm_ratio);
            if (wm_ratio < 1.0) display_w = (int) (display_w * wm_ratio);

            this.converter_cvt = Texture_Segment.convert_to_fit(this.replacement_ori, new_w, new_h, new_tex_type, (int) this.Converter_Diversity_NumUpDown.Value);

            this.Converter_Output_ImgBox.Image = new Bitmap(this.converter_cvt, display_w, display_h);
            this.Converter_Output_ImgBox.Update();
        }
        private void button3_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                File_Handler.remembered_assets_path = Path.GetDirectoryName(OFD.FileName);
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
        private void Converter_Load_Btn_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            if (OFD.ShowDialog() == DialogResult.OK)
            {
                File_Handler.remembered_assets_path = Path.GetDirectoryName(OFD.FileName);
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
                this.Converter_Input_ImgBox.Image = new Bitmap(this.replacement_ori, 128, 128);
                this.Converter_Input_ImgBox.Update();
            }
            convert_converter_to_fitting();
            this.update_display();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            convert_converter_to_fitting();
            this.update_display();
        }

        private void Converter_Format_ComBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            convert_converter_to_fitting();
            this.update_display();
        }
        private void Converter_Width_ComBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            convert_converter_to_fitting();
            this.update_display();
        }
        private void Converter_Height_ComBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            convert_converter_to_fitting();
            this.update_display();
        }

        private void Converter_Diversity_NumUpDown_ValueChanged(object sender, EventArgs e)
        {
            convert_converter_to_fitting();
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

        private void dataGridView3_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
        }

        private void dataGridView3_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // Console.WriteLine(dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
        }

        private void checkBox2_CheckedChanged_1(object sender, EventArgs e)
        {
            update_display();
        }
        private void checkBox3_CheckedChanged_1(object sender, EventArgs e)
        {
            update_display();
        }

        private void export_Coll_OBJ_button_Click(object sender, EventArgs e)
        {
            this.handler.export_collision_model();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            this.handler.export_displaylist_model();
        }
        private void export_DL_TXT_button_Click(object sender, EventArgs e)
        {
            this.handler.export_displaylist_text();
        }
        private void export_GeoL_TXT_button_Click(object sender, EventArgs e)
        {
            this.handler.export_geolayout_text();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void panel8_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
