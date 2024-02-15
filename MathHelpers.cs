using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;



namespace BK_BIN_Analyzer
{
    public static class MathHelpers
    {
        // public static String convert_to_Base64()
        public static double color_distance(byte R1, byte G1, byte B1, byte R2, byte G2, byte B2)
        {
            double sq_dist = Math.Pow(R2 - R1, 2) + Math.Pow(G2 - G1, 2) + Math.Pow(B2 - B1, 2);
            return Math.Sqrt(sq_dist);
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
                    Console.WriteLine("Converting Bitmap to CI4...");
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
                    Console.WriteLine("Converting Bitmap to CI8...");
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
                    Console.WriteLine("Converting Bitmap to RGBA16...");
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
                    Console.WriteLine("Converting Bitmap to RGBA32...");
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
                    Console.WriteLine("Converting Bitmap to IA8...");
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

                    {
                        Console.WriteLine(px_index);
                        cpx.print();
                    }

                    // only add new colors
                    if (palette.Contains(cpx) == true)
                    {
                        ColorPixel twin = palette.Find(tmp => tmp.Equals(cpx) == true);
                        twin.count++;
                    }
                    else palette.Add(cpx);
                }
            }
            Console.WriteLine(palette.Count);
            // reduce the palette
            palette = palette.OrderByDescending(c => c.count).ToList();
            List<ColorPixel> reduced_palette = new List<ColorPixel>();
            for (int i = 0; i < col_cnt && palette.Count > 0; i++)
            {
                reduced_palette.Add(palette.ElementAt(0));

                {
                    Console.WriteLine(i);
                    palette.ElementAt(0).print();
                }
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
