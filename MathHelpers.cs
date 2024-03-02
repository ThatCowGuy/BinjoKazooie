using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;



namespace Binjo
{
    public static class MathHelpers
    {
        // these 3 methods are counting bits from the left
        public static Boolean get_bit(uint bitfield, int bitfield_len, int bit_ID)
        {
            return ((bitfield & (uint) (0b1 << (bitfield_len - 1 - bit_ID))) > 0);
        }
        public static uint set_bit(uint bitfield, int bitfield_len, int bit_ID)
        {
            return (bitfield |= (uint) (0b1 << (bitfield_len - 1 - bit_ID)));
        }
        public static uint set_bits(uint bitfield, int bitfield_len, int[] bit_ID_arr)
        {
            for (int i = 0; i < bit_ID_arr.Length; i++)
                bitfield = set_bit(bitfield, bitfield_len, bit_ID_arr[i]);
            return bitfield;
        }


        // INPUT is first cut off at BITLEN (leftsided),
        // then shifted left by BITOFFSET (so 0s on the right)
        // to make building commands easier
        public static ulong shift_cut(ulong input, int bitoffset, int bitlen)
        {
            input = (input & (ulong) (Math.Pow(2, bitlen) - 1));
            return (input << bitoffset);
        }
        // DXT is a binary 12b fractional number with 11b for the mantissa
        // I return it as a ulong though, because its not neccessary to ever
        // interpret it as a float; We only ever need to write the raw data
        public static ulong calc_DXT(uint width, uint bitsize)
        {
            ulong bits_per_row = (ulong) (width * bitsize);
            // DXT (this is a really messy one:
            // "dxt is an unsigned fixed-point 1.11 [11 digit mantissa] number"
            // "dxt is the RECIPROCAL of the number of 64-bit chunks it takes to get a row of texture"
            // an example: Take a 32x32 px Tex with 16b colors;
            // -> a row of that Tex takes 32x16b = 512b
            // -> so it needs (512b/64b) = 8 chunks of 64b to create a row
            // -> the reciprocal is 1/8, which in binary is 0.001_0000_0000 = 0x100

            // since bitsize is divisble by 4, and width is divisible by 16,
            // bits_per_row has to be divisible by 64; So this is lossless
            ulong chunks_per_row = (ulong) (bits_per_row / 64.0);
            // furthermore, this should result in a power of 2, say 16 eg.
            // 16 = 0b10000, with only 1 bit set. The corresponding DXT should ALSO
            // only set 1 bit: the 1/16th bit! So we can simply calculate the log2
            // of the chunks_per_row result to get the set bit, and build the DXT
            // in the correct binary encoding with that knowledge:
            int set_bit = (int) Math.Log(chunks_per_row, 2);
            ulong DXT = (ulong) (0b1 << (11 - set_bit));
            // Console.WriteLine(String.Format("{0}", File_Handler.uint_to_string((uint) set_bit, 0b1)));
            // Console.WriteLine(String.Format("{0}, {1}, {2}", width, bitsize, File_Handler.uint_to_string((uint) DXT, 0b1)));
            return DXT;
        }

        public static int get_max(int[] arr)
        {
            return arr.Max();
        }
        public static int get_min(int[] arr)
        {
            return arr.Min();
        }
        // public static String convert_to_Base64()
        public static double color_distance(byte R1, byte G1, byte B1, byte R2, byte G2, byte B2)
        {
            double sq_dist = Math.Pow(R2 - R1, 2) + Math.Pow(G2 - G1, 2) + Math.Pow(B2 - B1, 2);
            return Math.Sqrt(sq_dist);
        }
        public static double L2_distance(short x1, short y1, short z1, short x2, short y2, short z2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dz = z2 - z1;
            double sq_dist = dx * dx + dy * dy + dz * dz;
            return Math.Sqrt(sq_dist);
        }
        public static double L2_distance(short x2, short y2, short z2)
        {
            return L2_distance(0, 0, 0, x2, y2, z2);
        }
        public static double color_distance(ColorPixel A, ColorPixel B)
        {
            double sq_dist = Math.Pow(A.r - B.r, 2) + Math.Pow(A.g - B.g, 2) + Math.Pow(A.b - B.b, 2);
            sq_dist += Math.Pow(A.a - B.a, 2); 
            return Math.Sqrt(sq_dist);
        }

        public class ColorPixel : IEquatable<ColorPixel>
        {
            public ColorPixel (int r, int g, int b, int a)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                // NOTE: since this constructor is only used for RGBA5551 colors, this is fine
                this.a = (a > 0 ? 1 : 0); 
                // fully transparent pixels have irrelevant colors
                if (this.a == 0)
                {
                    this.r = 0;
                    this.g = 0;
                    this.b = 0;
                }
            }
            public override bool Equals(Object obj)
            {
                return this.Equals(obj as ColorPixel);
            }
            public bool Equals(ColorPixel other)
            {
                if (this.r != other.r) return false;
                if (this.g != other.g) return false;
                if (this.b != other.b) return false;
                if (this.a != other.a) return false;
                return true;
            }
            public double r;
            public double g;
            public double b;
            public double a;
            public int count = 0;
            public double impact = 0;
            public ColorPixel closest_neighbor;
            public double closest_dist = 0;
            public double second_closest_dist = 0;

            public void print()
            {
                Console.WriteLine("{0}, {1}, {2}, {3} ({4})", r, g, b, a, count);
            }
        }
        public static byte[] convert_bitmap_to_bytes(Bitmap img, int tex_type)
        {
            int w = img.Width;
            int h = img.Height;

            // flip the parsed image because.. ig C# reaons
            Bitmap img_clone = new Bitmap(img);
            img_clone.RotateFlip(RotateFlipType.RotateNoneFlipY);

            switch (tex_type)
            {
                case (0x01): // C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
                {
                    Console.WriteLine("Converting CI4 Bitmap to Bytes...");
                    // parse the image data in a way that remembers every color
                    List<ColorPixel> palette = new List<ColorPixel>();
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);
                            // converting the image color data into RGBA5551 at this point already
                            // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                            //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                            ColorPixel cpx = new ColorPixel((byte) (px.R >> 3), (byte) (px.G >> 3), (byte) (px.B >> 3), (byte) (px.A >> 3));

                            // only add new colors
                            if (palette.Contains(cpx) == true)
                            {
                                ColorPixel twin = palette.Find(tmp => tmp.Equals(cpx) == true);
                                twin.count++;
                            }
                            else palette.Add(cpx);
                        }
                    }
                    if (palette.Count > 16)
                    {
                        Console.WriteLine("Too many Colors in extracted Palette (CI4 max is 32)");
                        return null;
                    }
                    byte[] data = new byte[0x20 + (w * h / 2)];
                    int pal_id = 0;
                    // NOTE: implicitly creating "black" colors for missing ones because it doesnt matter
                    foreach (ColorPixel pal_col in palette)
                    {
                        int col_val = ((int)pal_col.r << 0xB) + ((int)pal_col.g << 0x6) + ((int)pal_col.b << 0x1) + ((int)pal_col.a);
                        data[(pal_id * 2) + 0] = (byte)(col_val >> 8);
                        data[(pal_id * 2) + 1] = (byte)(col_val >> 0);
                        pal_id++;
                    }
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);
                            // converting the image color data into RGBA5551 at this point already
                            // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                            //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                            ColorPixel cpx = new ColorPixel((byte) (px.R >> 3), (byte) (px.G >> 3), (byte) (px.B >> 3), (byte) (px.A >> 3));

                            if (palette.Contains(cpx) == true)
                            {
                                double best_dist = 0xFFFFFF;
                                ColorPixel best_col = null;
                                foreach (ColorPixel pal_col in palette)
                                {
                                    if (color_distance(cpx, pal_col) < best_dist)
                                    {
                                        best_dist = color_distance(cpx, pal_col);
                                        best_col = pal_col;
                                    }
                                }
                                int px_pal_id = palette.FindIndex(tmp => tmp.Equals(best_col) == true);
                                if (px_index % 2 == 0)
                                    data[0x20 + (px_index / 2)] += (byte)(px_pal_id << 4);
                                else
                                    data[0x20 + (px_index / 2)] += (byte)(px_pal_id);
                            }
                        }
                    }
                    return data;
                }
                case (0x02): // C8 or CI8; 32 RGB555-colors, pixels are encoded per row as 8bit IDs
                {
                    Console.WriteLine("Converting CI8 Bitmap to Bytes...");
                    // parse the image data in a way that remembers every color
                    List<ColorPixel> palette = new List<ColorPixel>();
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);
                            // converting the image color data into RGBA5551 at this point already
                            // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                            //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                            ColorPixel cpx = new ColorPixel((byte) (px.R >> 3), (byte) (px.G >> 3), (byte) (px.B >> 3), (byte) (px.A >> 3));

                            // only add new colors
                            if (palette.Contains(cpx) == true)
                            {
                                ColorPixel twin = palette.Find(tmp => tmp.Equals(cpx) == true);
                                twin.count++;
                            }
                            else palette.Add(cpx);
                        }
                    }
                    if (palette.Count > 256)
                    {
                        Console.WriteLine("Too many Colors in extracted Palette (CI8 max is 256)");
                        return null;
                    }
                    byte[] data = new byte[0x200 + (w * h)];
                    int pal_id = 0;
                    // NOTE: implicitly creating "black" colors for missing ones because it doesnt matter
                    foreach (ColorPixel pal_col in palette)
                    {
                        int col_val = ((int)pal_col.r << 0xB) + ((int)pal_col.g << 0x6) + ((int)pal_col.b << 0x1) + ((int) pal_col.a); ;
                        data[(pal_id * 2) + 0] = (byte)(col_val >> 8);
                        data[(pal_id * 2) + 1] = (byte)(col_val >> 0);
                        pal_id++;
                    }
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);
                            // converting the image color data into RGBA5551 at this point already
                            // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                            //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                            ColorPixel cpx = new ColorPixel((byte)(px.R >> 3), (byte)(px.G >> 3), (byte)(px.B >> 3), (byte) (px.A >> 3));

                            if (palette.Contains(cpx) == true)
                            {
                                double best_dist = 0xFFFFFF;
                                ColorPixel best_col = null;
                                foreach (ColorPixel pal_col in palette)
                                {
                                    if (color_distance(cpx, pal_col) < best_dist)
                                    {
                                        best_dist = color_distance(cpx, pal_col);
                                        best_col = pal_col;
                                    }
                                }
                                int px_pal_id = palette.FindIndex(tmp => tmp.Equals(best_col) == true);
                                data[0x200 + (px_index)] = (byte)(px_pal_id);
                            }
                        }
                    }
                    return data;
                }
                case (0x04): // RGBA16 or RGB555A1 without a palette; pixels stored as a 16bit texel
                {
                    Console.WriteLine("Converting RGBA16 Bitmap to Bytes...");
                    byte[] data = new byte[(w * h * 2)];
                    // parse the image data
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);

                            // convert to RGB555A1 first
                            uint color_value = 0;
                            color_value += ((uint)px.R >> 3) << 0xB;
                            color_value += ((uint)px.G >> 3) << 0x6;
                            color_value += ((uint)px.B >> 3) << 0x1;
                            color_value += (uint)((uint)px.A > 0 ? 0b1 : 0b0);

                            data[(px_index * 2) + 0] = (byte)((color_value >> 8) & 0xFF);
                            data[(px_index * 2) + 1] = (byte)((color_value >> 0) & 0xFF);
                        }
                    }
                    return data;
                }
                case (0x08): // RGBA32 or RGB888A8 without a palette; pixels stored as a 32bit texel
                {
                    Console.WriteLine("Converting RGBA32 Bitmap to Bytes...");
                    byte[] data = new byte[(w * h * 4)];
                    // parse the image data
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);

                            data[(px_index * 4) + 0] = px.R;
                            data[(px_index * 4) + 1] = px.G;
                            data[(px_index * 4) + 2] = px.B;
                            data[(px_index * 4) + 3] = px.A;
                        }
                    }
                    return data;
                }
                case (0x10): // IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha;
                {
                    Console.WriteLine("Converting IA8 Bitmap to Bytes...");
                    byte[] data = new byte[(w * h)];
                    // parse the image data
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int px_index = (y * w) + x;
                            Color px = img_clone.GetPixel(x, y);

                            // in IA8, every color-value = intensity already, so only read one
                            // and convert that to a nibble. Then add alpha.
                            data[px_index] += (byte)((((uint)px.G & 0b11110000) >> 0) & 0b11110000);
                            data[px_index] += (byte)((((uint)px.A & 0b11110000) >> 4) & 0b00001111);
                        }
                    }
                    return data;
                }
                default: // UNKNOWN !";
                {
                    Console.WriteLine("Unknown Encoding Format received");
                    return null;
                }
            }
            return null;
        }


        public static Bitmap convert_image_to_IA8(Bitmap original)
        {
            int w = original.Width;
            int h = original.Height;
            // reduce the image
            Bitmap converted_img = new Bitmap(original);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color px = original.GetPixel(x, y);

                    // using the RGB -> Digital Luma conversion here
                    byte intensity = (byte)((px.R * 0.299) + (px.G * 0.587) + (px.B * 0.114));
                    byte alpha = px.A;

                    converted_img.SetPixel(
                        x, y, Color.FromArgb(
                        alpha,
                        intensity,
                        intensity,
                        intensity
                    ));

                }
            }
            return converted_img;
        }
        public static Bitmap convert_image_to_RGB5551(Bitmap original)
        {
            int w = original.Width;
            int h = original.Height;
            // reduce the image
            Bitmap converted_img = new Bitmap(original);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color px = original.GetPixel(x, y);

                    // converting to RGB555
                    converted_img.SetPixel(
                        x, y, Color.FromArgb(
                        (byte)(px.A > 0 ? 0xFF : 0x00), // dont do ((a >> 7) << 7) because that only returns 0x00 (=0%) or 0x80 (=50%)
                        (byte)((uint)px.R >> 3) << 3,
                        (byte)((uint)px.G >> 3) << 3,
                        (byte)((uint)px.B >> 3) << 3
                    ));
                }
            }
            return converted_img;
        }
        public static Bitmap convert_image_to_RGB5551_with_palette(Bitmap original, List<ColorPixel> palette)
        {
            int w = original.Width;
            int h = original.Height;
            // reduce the image
            Bitmap converted_img = new Bitmap(original);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color px = original.GetPixel(x, y);
                    // converting the image color data into RGBA5551 at this point already
                    // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                    //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                    ColorPixel cpx = new ColorPixel((byte) (px.R >> 3), (byte) (px.G >> 3), (byte) (px.B >> 3), (byte) (px.A >> 3));

                    double best_distance = 1e10;
                    ColorPixel best_color = null;
                    foreach (ColorPixel pal_col in palette)
                    {
                        if (color_distance(cpx, pal_col) < best_distance)
                        {
                            best_distance = color_distance(cpx, pal_col);
                            best_color = pal_col;
                        }
                    }
                    converted_img.SetPixel(
                        x, y, Color.FromArgb(
                        (int)best_color.a * 0xFF, // dont do (a << 7) because that only returns 0x00 (=0%) or 0x80 (=50%)
                        (int)best_color.r << 3,
                        (int)best_color.g << 3,
                        (int)best_color.b << 3
                    ));
                }
            }
            return converted_img;
        }

        public static List<ColorPixel> approx_palette_by_most_used_with_diversity(Bitmap original, int col_cnt, double thresh)
        {
            int w = original.Width;
            int h = original.Height;
            // parse the image data in a way that remembers every color
            List<ColorPixel> palette = new List<ColorPixel>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int px_index = (y * w) + x;
                    Color px = original.GetPixel(x, y);
                    // converting the image color data into RGBA5551 at this point already
                    // NOTE: Im using 5 bits of Alpha here to give it some weight; the constructor will convert any alpha > 0
                    //       to 1 afterwards, which essentially maps all alphas > 0b0000_1000 to 1
                    ColorPixel cpx = new ColorPixel((byte) (px.R >> 3), (byte) (px.G >> 3), (byte) (px.B >> 3), (byte) (px.A >> 3));
                    cpx.count = 1;

                    // only add new colors
                    if (palette.Contains(cpx) == true)
                    {
                        ColorPixel twin = palette.Find(tmp => tmp.Equals(cpx) == true);
                        twin.count++;
                    }
                    else palette.Add(cpx);
                }
            }
            // reduce the palette
            palette = palette.OrderByDescending(c => c.count).ToList();
            List<ColorPixel> reduced_palette = new List<ColorPixel>();
            for (int i = 0; i < col_cnt && palette.Count > 0; i++)
            {
                reduced_palette.Add(palette.ElementAt(0));
                palette.RemoveAt(0);
            }
            // first pass: merge colors in the top-used if they are really close
            while (palette.Count > 0)
            {
                double lowest_diversity_match = 1e10;
                ColorPixel matchA = null;
                ColorPixel matchB = null;
                foreach (ColorPixel a in reduced_palette)
                {
                    foreach (ColorPixel b in reduced_palette)
                    {
                        double diversity = color_distance(a, b);
                        if (diversity != 0 && diversity < lowest_diversity_match)
                        {
                            lowest_diversity_match = diversity;
                            matchA = a;
                            matchB = b;
                        }
                    }
                }
                if (lowest_diversity_match > thresh) break;

                reduced_palette.Remove(matchA);
                reduced_palette.Remove(matchB);
                double added_counts = (matchA.count + matchB.count);
                ColorPixel merged = new ColorPixel(
                    (int) ((matchA.r * matchA.count + matchB.r * matchB.count) / added_counts),
                    (int) ((matchA.g * matchA.count + matchB.g * matchB.count) / added_counts),
                    (int) ((matchA.b * matchA.count + matchB.b * matchB.count) / added_counts),
                    (int) (0xFF * Math.Max(matchA.a, matchB.a))
                );
                merged.count = (int)added_counts;
                reduced_palette.Add(palette.ElementAt(0));
                reduced_palette.Add(merged);
                palette.RemoveAt(0);
            }
            // second pass: get the next best colors (until they stop mattering) and merge them in aswell
            while (palette.Count > 0)
            {
                // get the next best color
                ColorPixel next_best = palette.ElementAt(0);
                // if the color is really meaningless (< 0.5% usage), stop
                if (next_best.count < (w * h) / 500.0) break;

                double lowest_diversity_match = 1e10;
                ColorPixel matchA = null;
                ColorPixel matchB = next_best;
                foreach (ColorPixel a in reduced_palette)
                {
                    double diversity = color_distance(a, next_best);
                    if (diversity != 0 && diversity < lowest_diversity_match)
                    {
                        lowest_diversity_match = diversity;
                        matchA = a;
                    }
                }

                palette.RemoveAt(0);
                reduced_palette.Remove(matchA);
                double added_counts = (matchA.count + matchB.count);
                ColorPixel merged = new ColorPixel(
                    (int) ((matchA.r * matchA.count + matchB.r * matchB.count) / added_counts),
                    (int) ((matchA.g * matchA.count + matchB.g * matchB.count) / added_counts),
                    (int) ((matchA.b * matchA.count + matchB.b * matchB.count) / added_counts),
                    (int) (0xFF * Math.Max(matchA.a, matchB.a))
                );
                merged.count = (int) added_counts;
                reduced_palette.Add(merged);
            }
            return reduced_palette;
        }

        public static Bitmap approx_by_KMeans(Bitmap original, int iterations)
        {
            int w = original.Width;
            int h = original.Height;
            int col_cnt = 16;
            // randomly initiate the color palette
            byte[] color_palette = new byte[col_cnt * 3];
            Random rng = new Random();
            for (int i = 0; i < col_cnt; i++)
            {
                color_palette[(i * 3) + 0] = (byte)rng.Next(0b00000, 0b11111);
                color_palette[(i * 3) + 1] = (byte)rng.Next(0b00000, 0b11111);
                color_palette[(i * 3) + 2] = (byte)rng.Next(0b00000, 0b11111);
            }
            // then parse the image data (+ an index for clustering)
            byte[] pixel_data = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int px_index = (y * w) + x;
                    Color px = original.GetPixel(x, y);
                    // converting the image color data into RGB555 at this point already
                    // also swapping from RGB -> BGR
                    pixel_data[(px_index * 4) + 2] = (byte)(px.R >> 3);
                    pixel_data[(px_index * 4) + 1] = (byte)(px.G >> 3);
                    pixel_data[(px_index * 4) + 0] = (byte)(px.B >> 3);
                    pixel_data[(px_index * 4) + 3] = (byte)0;
                }
            }
            // and start KMeans algorithm
            for (int k = 0; k < iterations; k++)
            {
                // figure out which cluster every pixel belongs to
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int px_index = (y * w) + x;
                        double closest_color_dist = 1e10;
                        for (int i = 0; i < col_cnt; i++)
                        {
                            double dist = color_distance(
                                pixel_data[(px_index * 4) + 0], pixel_data[(px_index * 4) + 1], pixel_data[(px_index * 4) + 2],
                                color_palette[(i * 3) + 0], color_palette[(i * 3) + 1], color_palette[(i * 3) + 2]
                            );
                            if (dist < closest_color_dist)
                            {
                                closest_color_dist = dist;
                                // also store the new best index
                                pixel_data[(px_index * 4) + 3] = (byte)i;
                            }
                        }
                    }
                }
                // now calculate the new palette as the average of the cluster pixels
                // this has an extra slot to remember how many colors are in this cluster
                double[] new_color_palette = new double[col_cnt * 4];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int px_index = (y * w) + x;
                        int cluster_ID = pixel_data[(px_index * 4) + 3];

                        // count this pixel towards the cluster
                        new_color_palette[(cluster_ID * 4) + 3] += 1;
                        // and sum its colors up
                        new_color_palette[(cluster_ID * 4) + 0] += pixel_data[(px_index * 4) + 0];
                        new_color_palette[(cluster_ID * 4) + 1] += pixel_data[(px_index * 4) + 1];
                        new_color_palette[(cluster_ID * 4) + 2] += pixel_data[(px_index * 4) + 2];
                    }
                }
                // finally, average the sums and update the color palette
                for (int i = 0; i < col_cnt; i++)
                {
                    color_palette[(i * 3) + 0] = (byte)(new_color_palette[(i * 4) + 0] / new_color_palette[(i * 4) + 3]);
                    color_palette[(i * 3) + 1] = (byte)(new_color_palette[(i * 4) + 1] / new_color_palette[(i * 4) + 3]);
                    color_palette[(i * 3) + 2] = (byte)(new_color_palette[(i * 4) + 2] / new_color_palette[(i * 4) + 3]);
                }
            }
            // now we can replace the image pixels with the closest color from the new palette
            byte[] new_pixel_data = new byte[w * h * 3];
            // figure out which cluster every pixel belongs to
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int px_index = (y * w) + x;
                    double closest_color_dist = 1e10;
                    int closest_color_ID = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        double dist = color_distance(
                            pixel_data[(px_index * 4) + 0], pixel_data[(px_index * 4) + 1], pixel_data[(px_index * 4) + 2],
                            color_palette[(i * 3) + 0], color_palette[(i * 3) + 1], color_palette[(i * 3) + 2]
                        );
                        if (dist < closest_color_dist)
                        {
                            closest_color_dist = dist;
                            // also store the new best index
                            closest_color_ID = (byte)i;
                        }
                    }
                    // and this time, replace the color within the pixel
                    // (and convert it to a full pixel for the Bitmap Display)
                    new_pixel_data[(px_index * 3) + 0] = (byte)(((double)color_palette[(closest_color_ID * 3) + 0] / 0b11111) * 0xFF);
                    new_pixel_data[(px_index * 3) + 1] = (byte)(((double)color_palette[(closest_color_ID * 3) + 1] / 0b11111) * 0xFF);
                    new_pixel_data[(px_index * 3) + 2] = (byte)(((double)color_palette[(closest_color_ID * 3) + 2] / 0b11111) * 0xFF);
                }
            }
            return new Bitmap(
                (int)w, (int)h, (int)(w * 3),
                System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                GCHandle.Alloc(new_pixel_data, GCHandleType.Pinned).AddrOfPinnedObject()
            );
        }
    }
}
