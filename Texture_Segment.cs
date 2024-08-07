﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

// after discounting the palette size, we should be able to detect mipmaps and mulit-textures (for anim)
// by checking if the texture size is larger than what we would expect from HxWxS.
// Then we can check, if the "true size" is divisible by the texture size => multi-texture
// if it isnt, it should be a mipmap (unless one can have mipmapped multi textures, but.... ugh)

namespace Binjo
{
    public class Tex_Meta
    {
        // parsed properties
        // === 0x00 ===============================
        public uint datasection_offset_data; // for the corresponding tex data
        public ushort tex_type;
        public ushort unk_1;
        public byte width;
        public byte height;
        public ushort unk_2;
        public uint unk_3; // 00, maybe a buffer ? 
        public static int ELEMENT_SIZE = 0x10;

        // locators
        public uint file_offset;
        public uint section_offset;

        // computed properties
        public uint file_offset_data; // for the corresponding tex data
        public uint section_offset_data; // for the corresponding tex data
        public uint pixel_total;

        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x10];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.datasection_offset_data, 4), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tex_type, 2), bytes, 0x04);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_1, 2), bytes, 0x06);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.width, 1), bytes, 0x08);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.height, 1), bytes, 0x09);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_2, 2), bytes, 0x0A);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_3, 4), bytes, 0x0C);
            return bytes;
        }
    }

    public class Tex_Data
    {
        // parsed properties
        // === 0x00 ===============================
        public byte[] data;
        public uint data_size;
        public Bitmap img_rep; 

        // locators
        public uint file_offset;
        public uint section_offset;
        public uint datasection_offset;

        // computed properties
        public ushort tex_type;

        public byte[] get_bytes()
        {
            return MathHelpers.convert_bitmap_to_bytes(this.img_rep, this.tex_type);
        }
    }

    public class Texture_Segment
    {
        public bool valid = false;

        // parsed properties
        // === 0x00 ===============================
        public uint data_size;
        public ushort tex_cnt;
        public ushort unk_1;
        public static int HEADER_SIZE = 0x08;

        // locators
        public uint file_offset;
        public uint file_offset_meta;
        public uint file_offset_data;

        // computed properties
        public uint full_header_size;

        // data
        public Tex_Meta[] meta;
        public Tex_Data[] data;
        public byte[] get_bytes()
        {
            byte[] bytes = new byte[0x8];
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.data_size, 4), bytes, 0x00);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.tex_cnt, 2), bytes, 0x04);
            File_Handler.write_bytes_to_buffer(File_Handler.uint_to_bytes((uint) this.unk_1, 2), bytes, 0x06);

            for (int i = 0; i < this.tex_cnt; i++)
                bytes = File_Handler.concat_arrays(bytes, this.meta[i].get_bytes());
            for (int i = 0; i < this.tex_cnt; i++)
                bytes = File_Handler.concat_arrays(bytes, this.data[i].get_bytes());

            return bytes;
        }

        public static Dictionary<string, int> TEX_TYPES = new Dictionary<string, int>
        {
            { "IA8", 0x10 },    { "IA8 (44)", 0x10 },
            { "RGBA32", 0x08 }, { "RGBA32 (8888)", 0x08 },
            { "RGBA16", 0x04 }, { "RGBA16 (5551)", 0x04 },
            { "CI8", 0x02 },    { "CI8 (5551)", 0x02 },
            { "CI4", 0x01 },    { "CI4 (5551)", 0x01 },
        };

        public uint get_tex_ID_from_datasection_offset(uint datasection_offset)
        {
            // Console.WriteLine("looking for fileoffset " + datasection_offset);
            for (uint i = 0; i < this.tex_cnt; i++)
            {
                if (datasection_offset == this.data[i].datasection_offset)
                    return i;
            }
            // no match
            // Console.WriteLine("No Tex Match found for file_offset: " + file_offset);
            return 0xFFFF;
        }

        // get the datasize of an image (w x h) of format "format" (no mipmapping)
        public static uint get_datasize(uint w, uint h, string format)
        {
            uint texel_cnt = (w * h);
            uint texel_size = (uint) Dicts.TEXEL_FMT_BITSIZE[format];

            uint palette_size = 0;
            if (format.Contains("CI8") == true)
                // CI8 contains 2^8 colors of 16 bits (5551) each
                // 2^8 * 16 = 256 * 16 = 0x100 * 0x10 = 0x1000
                palette_size = 0x1000;
            if (format.Contains("CI4") == true)
                // CI8 contains 2^4 colors of 16 bits (5551) each
                // 2^4 * 16 = 16 * 16 = 0x10 * 0x10 = 0x100
                palette_size = 0x100;

            // return divided by 8 for bits -> byte conversion
            return (palette_size + (texel_cnt * texel_size)) / 8;
        }

        // converts an input image to a WxH sized image of the given type. color diversity is used for CI palette optimization
        public static Bitmap convert_to_fit(Bitmap input_img, int new_w, int new_h, int new_tex_type, int color_diversity)
        {
            switch (new_tex_type)
            {
                case (0x01): // C4 or CI4; 16 RGBA5551-colors, pixels are encoded per row as 4bit IDs
                {
                    int col_cnt = 0x10;
                    List<MathHelpers.ColorPixel> replacement_palette = MathHelpers.approx_palette_by_most_used_with_diversity(
                        new Bitmap(input_img, new_w, new_h),
                        col_cnt, color_diversity
                    );
                    return MathHelpers.convert_image_to_RGB5551_with_palette(
                        new Bitmap(input_img, new_w, new_h),
                        replacement_palette
                    );
                }
                case (0x02): // C8 or CI8; 256 RGBA5551-colors, pixels are encoded per row as 8bit IDs
                {
                    int col_cnt = 0x100;
                    List<MathHelpers.ColorPixel> replacement_palette = MathHelpers.approx_palette_by_most_used_with_diversity(
                        new Bitmap(input_img, new_w, new_h),
                        col_cnt, color_diversity
                    );
                    return MathHelpers.convert_image_to_RGB5551_with_palette(
                        new Bitmap(input_img, new_w, new_h),
                        replacement_palette
                    );
                }
                case (0x04): // RGBA16 or RGB555A1 without a palette; pixels stored as a 16bit texel
                {
                    return MathHelpers.convert_image_to_RGB5551(
                        new Bitmap(input_img, new_w, new_h)
                    );
                }
                case (0x08): // RGBA32 or RGB888A8 without a palette; pixels stored as a 32bit texel
                {
                    return new Bitmap(input_img, new_w, new_h);
                }
                case (0x10): // IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha;
                {
                    return MathHelpers.convert_image_to_IA8(
                        new Bitmap(input_img, new_w, new_h)
                    );
                }
                default: // UNKNOWN !";
                    Console.WriteLine("convert_to_fitting() receieved unknown tex type");
                    return null;
            }
        }
        public static Bitmap parse_img_data(byte[] data, uint tex_type, uint w, uint h)
        {
            Bitmap parsed_img = null;
            switch (tex_type)
            {
                // https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
                case (0x01): // C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
                {
                    // first parse the color palette
                    byte[] color_palette = new byte[0x10 * 4];
                    for (int i = 0; i < 0x10; i++)
                    {
                        uint color_value = File_Handler.read_short(data, (i * 2), false);
                        // RGB5551
                        color_palette[i * 4 + 0] = (byte) (((double)((color_value >> 0xB) & 0b011111) / 0b011111) * 0xFF); // R
                        color_palette[i * 4 + 1] = (byte) (((double)((color_value >> 0x6) & 0b011111) / 0b011111) * 0xFF); // G
                        color_palette[i * 4 + 2] = (byte) (((double)((color_value >> 0x1) & 0b011111) / 0b011111) * 0xFF); // B
                        // and grab alpha from final bit
                        color_palette[i * 4 + 3] = (byte) ((color_value & 0b0001) * 0xFF);
                    }
                    // then parse the image data
                    byte[] pixel_data = new byte[w * h * 4];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // calc the pixel index
                            int px_id = (int)(y * w) + x;
                            // NOTE: this grabs the full byte, but the actual ID is only one nibble of that -> split it
                            int pal_id = data[0x20 + (px_id / 2)];
                            if (px_id % 2 == 0)
                                pal_id = (pal_id >> 4) & 0b1111;
                            else
                                pal_id = (pal_id >> 0) & 0b1111;

                            // NOTE: the Bitmap constructor expects the colors to be in BGR...
                            pixel_data[(px_id * 4) + 2] = color_palette[(pal_id * 4) + 0];
                            pixel_data[(px_id * 4) + 1] = color_palette[(pal_id * 4) + 1];
                            pixel_data[(px_id * 4) + 0] = color_palette[(pal_id * 4) + 2];
                            // and alpha
                            pixel_data[(px_id * 4) + 3] = color_palette[(pal_id * 4) + 3];
                        }
                    }
                    parsed_img = new Bitmap(
                        (int) w, (int) h, (int) (w * 4),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        GCHandle.Alloc(pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
                    );
                    break;
                }
                case (0x02): // C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
                {
                    // first parse the color palette
                    byte[] color_palette = new byte[0x100 * 4];
                    for (int i = 0; i < 0x100; i++)
                    {
                        uint color_value = File_Handler.read_short(data, (i * 2), false);
                        // RGBA5551
                        color_palette[i * 4 + 0] = (byte)(((double)((color_value >> 0xB) & 0b011111) / 0b011111) * 0xFF); // R
                        color_palette[i * 4 + 1] = (byte)(((double)((color_value >> 0x6) & 0b011111) / 0b011111) * 0xFF); // G
                        color_palette[i * 4 + 2] = (byte)(((double)((color_value >> 0x1) & 0b011111) / 0b011111) * 0xFF); // B
                        // and grab alpha from final bit
                        color_palette[i * 4 + 3] = (byte) ((color_value & 0b0001) * 0xFF);
                    }
                    // then parse the image data
                    byte[] pixel_data = new byte[w * h * 4];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // calc the pixel index
                            int px_id = (int)(y * w) + x;
                            int pal_id = data[0x200 + (px_id)];

                            // NOTE: the Bitmap constructor expects the colors to be in BGR...
                            pixel_data[(px_id * 4) + 2] = color_palette[(pal_id * 4) + 0];
                            pixel_data[(px_id * 4) + 1] = color_palette[(pal_id * 4) + 1];
                            pixel_data[(px_id * 4) + 0] = color_palette[(pal_id * 4) + 2];
                            // and alpha
                            pixel_data[(px_id * 4) + 3] = color_palette[(pal_id * 4) + 3];
                        }
                    }
                    parsed_img = new Bitmap(
                        (int) w, (int) h, (int) (w * 4),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        GCHandle.Alloc(pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
                    );
                    break;
                }
                case (0x04): // RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel
                {
                    // parse the image data
                    byte[] pixel_data = new byte[w * h * 4];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // calc the pixel index
                            int px_id = (int)(y * w) + x;
                            uint color_value = File_Handler.read_short(data, (px_id * 2), false);

                            // NOTE: THIS Bitmap constructor expects the colors to be in BGRA...
                            pixel_data[(px_id * 4) + 2] = (byte)(((double)((color_value >> 0xB) & 0b011111) / 0b011111) * 0xFF);
                            pixel_data[(px_id * 4) + 1] = (byte)(((double)((color_value >> 0x6) & 0b011111) / 0b011111) * 0xFF);
                            pixel_data[(px_id * 4) + 0] = (byte)(((double)((color_value >> 0x1) & 0b011111) / 0b011111) * 0xFF);
                            // dont forget alpha !
                            pixel_data[(px_id * 4) + 3] = (byte)((color_value & 0b1) * 0xFF);
                        }
                    }
                    parsed_img = new Bitmap(
                        (int)w, (int)h, (int)(w * 4),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        GCHandle.Alloc(pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
                    );
                    break;
                }
                case (0x08): // RGBA32 or RGB888A8 without a palette; pixels stored as a 32bit texel
                {
                    // parse the image data
                    byte[] pixel_data = new byte[w * h * 4];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // calc the pixel index
                            int px_id = (int)(y * w) + x;

                            // NOTE: THIS Bitmap constructor expects the colors to be in BGRA...
                            pixel_data[(px_id * 4) + 0] = data[(px_id * 4) + 2];
                            pixel_data[(px_id * 4) + 1] = data[(px_id * 4) + 1];
                            pixel_data[(px_id * 4) + 2] = data[(px_id * 4) + 0];
                            // dont forget alpha !
                            pixel_data[(px_id * 4) + 3] = data[(px_id * 4) + 3];
                        }
                    }
                    parsed_img = new Bitmap(
                        (int)w, (int)h, (int)(w * 4),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        GCHandle.Alloc(pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
                    );
                    break;
                }
                case (0x10): // IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha;
                {
                    // parse the image data
                    byte[] pixel_data = new byte[w * h * 4];
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // calc the pixel index
                            int px_id = (int)(y * w) + x;

                            // in IA8, the first nibble is the intensity => every color-value
                            // NOTE: THIS Bitmap constructor expects the colors to be in BGRA...
                            // NOTE: This math looks pretty weird, but the 2nd summand is just to interpolate the values from
                            //       a nibble into a byte, to avoid rounding oddities
                            pixel_data[(px_id * 4) + 2] = (byte) ((data[px_id] & 0b11110000) + (data[px_id] >> 4));
                            pixel_data[(px_id * 4) + 1] = (byte) ((data[px_id] & 0b11110000) + (data[px_id] >> 4));
                            pixel_data[(px_id * 4) + 0] = (byte) ((data[px_id] & 0b11110000) + (data[px_id] >> 4));
                            // dont forget alpha !
                            pixel_data[(px_id * 4) + 3] = (byte)(((data[px_id] << 4) & 0b11110000) + (data[px_id] & 0b00001111));
                        }
                    }
                    parsed_img = new Bitmap(
                        (int)w, (int)h, (int)(w * 4),
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        GCHandle.Alloc(pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
                    );
                    break;
                }
                default:
                    //tex_type_string = "UNKNOWN !";
                    break;
            }
            // flip the parsed image because.. ig C# reaons
            parsed_img.RotateFlip(RotateFlipType.RotateNoneFlipY);
            return parsed_img;
        }

        public void populate(byte[] file_data, int file_offset)
        {
            if (file_offset == 0)
            {
                System.Console.WriteLine("No Texture Segment");
                this.valid = false;
                return;
            }
            this.valid = true;
            this.file_offset = (uint)file_offset;
            this.file_offset_meta = (uint)(file_offset + Texture_Segment.HEADER_SIZE);

            // parsing properties
            // === 0x00 ===============================
            this.data_size = File_Handler.read_int(file_data, file_offset + 0x00, false);
            this.tex_cnt = File_Handler.read_short(file_data, file_offset + 0x04, false);
            this.unk_1 = File_Handler.read_short(file_data, file_offset + 0x06, false);

            // computing properties
            this.full_header_size = (uint)(Texture_Segment.HEADER_SIZE + (this.tex_cnt * Tex_Meta.ELEMENT_SIZE));
            this.file_offset_data = (uint)file_offset + this.full_header_size;

            this.meta = new Tex_Meta[this.tex_cnt];
            this.data = new Tex_Data[this.tex_cnt];
            for (int i = 0; i < this.tex_cnt; i++)
            {
                Tex_Meta m = new Tex_Meta();
                m.file_offset = (uint)(file_offset_meta + (i * 0x10));
                m.section_offset = (uint)(i * 0x10);

                // parsing properties
                // === 0x00 ===============================
                m.datasection_offset_data = File_Handler.read_int(file_data, (int)(m.file_offset + 0x00), false);
                m.tex_type = File_Handler.read_short(file_data, (int)(m.file_offset + 0x04), false);
                m.unk_1 = File_Handler.read_short(file_data, (int)(m.file_offset + 0x06), false);
                m.width = File_Handler.read_char(file_data, (int)(m.file_offset + 0x08), false);
                m.height = File_Handler.read_char(file_data, (int)(m.file_offset + 0x09), false);
                m.unk_2 = File_Handler.read_short(file_data, (int)(m.file_offset + 0x0A), false);
                m.unk_3 = File_Handler.read_int(file_data, (int)(m.file_offset + 0x0C), false);

                // computing properties
                m.file_offset_data = file_offset_data + m.datasection_offset_data;
                m.section_offset_data = this.full_header_size + m.datasection_offset_data;
                m.pixel_total = (uint)(m.width * m.height);

                Tex_Data d = new Tex_Data();
                d.file_offset = m.file_offset_data;
                d.section_offset = m.section_offset_data;
                d.datasection_offset = m.datasection_offset_data;
                // also relay the tex type to the data element
                d.tex_type = m.tex_type;

                this.meta[i] = m;
                this.data[i] = d;
            }
            // parse the image data too (after grabbing all the meta inf)
            for (int i = 0; i < this.tex_cnt; i++)
            {
                Tex_Meta m = this.meta[i];
                Tex_Data d = this.data[i];

                // can simply take diff to next image start if not the last one
                if (i < (this.tex_cnt - 1))
                    d.data_size = this.meta[i + 1].datasection_offset_data - this.meta[i].datasection_offset_data;
                // otherwise, take diff to data-size
                else
                    d.data_size = this.data_size - this.meta[i].datasection_offset_data;

                d.data = new byte[(int) d.data_size];
                for (int b = 0; b < d.data_size; b++)
                {
                    d.data[b] = file_data[d.file_offset + b];
                }
                // and finally, parse the image data
                d.img_rep = Texture_Segment.parse_img_data(d.data, m.tex_type, m.width, m.height);
            }
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
                "Data Size",
                File_Handler.uint_to_string(this.data_size, 0xFFFFFFFF),
                File_Handler.uint_to_string(this.data_size, 10) + " Bytes",
                ""
            });
            content.Add(new string[] {
                "Texture Count",
                File_Handler.uint_to_string(this.tex_cnt, 0xFFFF),
                File_Handler.uint_to_string(this.tex_cnt, 10),
                ""
            });
            return content;
        }

        public List<string[]> get_content_of_element(int index)
        {
            List<string[]> content = new List<string[]>();

            Tex_Meta meta = this.meta[index];
            //===============================
            content.Add(new string[] {
                "File Offset (Meta)",
                File_Handler.uint_to_string(meta.file_offset, 0xFFFFFFFF),
                ""
            });
            content.Add(new string[] {
                "Seg Offset (Meta)",
                File_Handler.uint_to_string(meta.section_offset, 0xFFFFFFFF),
                ""
            });
            string tex_type_string;
            switch (meta.tex_type)
            {
                case (0x01):
                    tex_type_string = "C4 / CI4";
                    break;
                case (0x02):
                    tex_type_string = "C8 / CI8";
                    break;
                case (0x04):
                    tex_type_string = "RGBA16";
                    break;
                case (0x08):
                    tex_type_string = "RGBA32";
                    break;
                case (0x10):
                    tex_type_string = "IA8";
                    break;
                default:
                    tex_type_string = "UNKNOWN !";
                    break;
            }
            content.Add(new string[] {
                "Texture Type",
                File_Handler.uint_to_string(meta.tex_type, 0xFFFF),
                tex_type_string
            });
            content.Add(new string[] {
                "UNK_1",
                File_Handler.uint_to_string(meta.unk_1, 0xFFFF),
                File_Handler.uint_to_string(meta.unk_1, 10)
            });
            content.Add(new string[] {
                "Dimension",
                File_Handler.uint_to_string(meta.width, 0xFF) + " / " + File_Handler.uint_to_string(meta.height, 0xFF),
                "(" + File_Handler.uint_to_string(meta.width, 10) + " x " + File_Handler.uint_to_string(meta.height, 10) + ") px"
            });
            content.Add(new string[] {
                "UNK_2",
                File_Handler.uint_to_string(meta.unk_2, 0xFFFF),
                File_Handler.uint_to_string(meta.unk_2, 10)
            });
            content.Add(new string[] {
                "UNK_3",
                File_Handler.uint_to_string(meta.unk_3, 0xFFFFFFFF),
                File_Handler.uint_to_string(meta.unk_3, 10)
            });

            Tex_Data data = this.data[index];
            //===============================
            content.Add(new string[] {
                "File Offset (Data)",
                File_Handler.uint_to_string(data.file_offset, 0xFFFFFFFF),
                "",
            });
            content.Add(new string[] {
                "Seg Offset (Data)",
                File_Handler.uint_to_string(data.section_offset, 0xFFFFFFFF),
                ""
            });
            content.Add(new string[] {
                "Data Offset (Data)",
                File_Handler.uint_to_string(data.datasection_offset, 0xFFFFFFFF),
                ""
            });
            return content;
        }
    }
}
