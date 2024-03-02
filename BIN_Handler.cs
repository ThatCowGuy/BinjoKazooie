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
using System.Text.Json;


namespace Binjo
{
    public class BIN_Handler
    {
        byte[] content;


        public static Dictionary<int, string> SEGMENT_NAMES = new Dictionary<int, string>
        {
            { 0, "BIN Header" },
            { 1, "Texture Segment" },
            { 2, "DisplayList Segment" },
            { 3, "Vertex Segment" },
            { 4, "Collision Segment" },
            { 5, "Bone Segment" },
            { 6, "Effects Segment" },
            { 7, "Effects End Segment" },
            { 8, "Animated Texture Segment" },
            { 9, "GeoLayout Segment" },
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
        public Effects_Segment FX_seg = new Effects_Segment();
        public FX_END_Segment FXEND_seg = new FX_END_Segment();
        public AnimTex_Segment animtex_seg = new AnimTex_Segment();
        public GeoLayout_Segment geo_seg = new GeoLayout_Segment();

        // this is a list of fulltri-lists, where each sublist is representing one kind of tri
        // (split by material, so textures are sepperate, flags are sepperate etc)
        public List<List<FullTriangle>> tri_list_tree = new List<List<FullTriangle>>();
        // this is a list of fulltris, that will be ordered for the GLTF export; not neccessary
        // (and kinda annoying) to split that into sublists...
        public List<FullTriangle> consecutive_tri_list = new List<FullTriangle>();
        public GLTF_Handler GLTF = new GLTF_Handler();

        public void parse_BIN()
        {
            this.file_loaded = false;
            this.tri_list_tree = new List<List<FullTriangle>>();

            this.content = System.IO.File.ReadAllBytes(this.loaded_bin_path);
            bin_header.populate(this.content);
            tex_seg.populate(this.content, (int) bin_header.tex_offset);
            vtx_seg.binheader_vtx_cnt = bin_header.vtx_cnt;
            vtx_seg.populate(this.content, (int) bin_header.vtx_offset);
            bone_seg.populate(this.content, (int) bin_header.bone_offset);
            coll_seg.populate(this.content, (int) bin_header.coll_offset);

            // start of the full tri list by adding every collision tri
            // and inferring the correct associated vertices
            this.consecutive_tri_list = coll_seg.export_tris_as_full();
            vtx_seg.infer_vtx_data_for_full_tris(this.consecutive_tri_list);

            // this guy needs handles for several inferrations
            DL_seg.populate(this.content, (int) bin_header.DL_offset, this.tex_seg, this.vtx_seg, this.consecutive_tri_list);
            FX_seg.populate(this.content, (int) bin_header.FX_offset);
            FXEND_seg.populate(this.content, (int) bin_header.FX_END);
            animtex_seg.populate(this.content, (int) bin_header.anim_tex_offset);
            geo_seg.populate(this.content, (int) bin_header.geo_offset);

            this.file_loaded = true;
        }
        public void build_tex_seg()
        {
            this.tex_seg = new Texture_Segment();
            Console.WriteLine("Building Tex Segment...");

            tex_seg.data_size = 0; // summed up in the following loop
            tex_seg.tex_cnt = (ushort) this.GLTF.textures.Count;
            tex_seg.meta = new Tex_Meta[tex_seg.tex_cnt];
            tex_seg.data = new Tex_Data[tex_seg.tex_cnt];

            // NOTE: this initial offset shouldnt be hard coded
            tex_seg.file_offset = 0x00000038;
            tex_seg.file_offset_meta = (uint) (tex_seg.file_offset + Texture_Segment.HEADER_SIZE);
            tex_seg.file_offset_data = (uint) (tex_seg.file_offset_meta + (Tex_Meta.ELEMENT_SIZE * tex_seg.tex_cnt));

            for (int i = 0; i < tex_seg.tex_cnt; i++)
            {
                // most of the content within these elements is parsed during the GLTF-parsing already
                tex_seg.meta[i] = this.GLTF.images[i].tex_meta;
                tex_seg.data[i] = this.GLTF.images[i].tex_data;

                // tex_seg.meta[i].datasection_offset_data = 0;

                tex_seg.data[i].file_offset = tex_seg.file_offset_data + this.tex_seg.data_size;
                tex_seg.data[i].section_offset = (uint) (tex_seg.data[i].file_offset - tex_seg.file_offset);
                tex_seg.data[i].datasection_offset = (uint) (tex_seg.data[i].file_offset - tex_seg.file_offset_data);

                tex_seg.meta[i].section_offset = (uint) (Texture_Segment.HEADER_SIZE + (Tex_Meta.ELEMENT_SIZE * i));
                tex_seg.meta[i].file_offset = (uint) (tex_seg.file_offset + tex_seg.meta[i].section_offset);
                // internal, referring to the corresponding data
                tex_seg.meta[i].section_offset_data = tex_seg.data[i].section_offset;
                tex_seg.meta[i].datasection_offset_data = tex_seg.data[i].datasection_offset;

                tex_seg.data_size += tex_seg.data[i].data_size;
            }
            // and finally add the size of the meta elements and the header !
            tex_seg.data_size += (uint) (Texture_Segment.HEADER_SIZE + (Tex_Meta.ELEMENT_SIZE * tex_seg.tex_cnt));

            this.tex_seg.valid = true;
            Console.WriteLine("Finished Tex Segment.");
        }
         
        public void sort_tris_by_max_ID(List<FullTriangle> list)
        {
            foreach (FullTriangle tri in list)
            {
                tri.max_index = (ushort) MathHelpers.get_max(new int[]{ tri.index_1, tri.index_2, tri.index_3 });
                tri.min_index = (ushort) MathHelpers.get_min(new int[] { tri.index_1, tri.index_2, tri.index_3 });
            }
            list = list.OrderBy(o => o.max_index).ToList();
        }
        public void build_coll_seg()
        {
            this.coll_seg = new Collision_Segment();
            Console.WriteLine("Building Coll Segment...");

            this.coll_seg.geo_cube_scale = 1000;
            // NOTE: using Math.Floor to enforce rounding-down
            this.coll_seg.min_geo_cube_x = (short) Math.Floor((double) this.vtx_seg.min_x / this.coll_seg.geo_cube_scale);
            this.coll_seg.min_geo_cube_y = (short) Math.Floor((double) this.vtx_seg.min_y / this.coll_seg.geo_cube_scale);
            this.coll_seg.min_geo_cube_z = (short) Math.Floor((double) this.vtx_seg.min_z / this.coll_seg.geo_cube_scale);
            this.coll_seg.max_geo_cube_x = (short) Math.Floor((double) this.vtx_seg.max_x / this.coll_seg.geo_cube_scale);
            this.coll_seg.max_geo_cube_y = (short) Math.Floor((double) this.vtx_seg.max_y / this.coll_seg.geo_cube_scale);
            this.coll_seg.max_geo_cube_z = (short) Math.Floor((double) this.vtx_seg.max_z / this.coll_seg.geo_cube_scale);
            // NOTE: +1 because if min == max, its still 1 geo cube
            int x_extent = (this.coll_seg.max_geo_cube_x - this.coll_seg.min_geo_cube_x) + 1;
            int y_extent = (this.coll_seg.max_geo_cube_y - this.coll_seg.min_geo_cube_y) + 1;
            int z_extent = (this.coll_seg.max_geo_cube_z - this.coll_seg.min_geo_cube_z) + 1;
            this.coll_seg.stride_y     = (short)  (x_extent);
            this.coll_seg.stride_z     = (short)  (x_extent * y_extent);
            this.coll_seg.geo_cube_cnt = (ushort) (x_extent * y_extent * z_extent);

            // I dont really understand why I have to instantiate these per-element... but oh well
            this.coll_seg.geo_cube_list = new Geo_Cube_Elem[this.coll_seg.geo_cube_cnt];
            for (int i = 0; i < this.coll_seg.geo_cube_cnt; i++)
                this.coll_seg.geo_cube_list[i] = new Geo_Cube_Elem();

            // this needs to be integrated dynamically in the following loop
            this.coll_seg.tri_cnt = 0;

            foreach (List<FullTriangle> tri_list in this.tri_list_tree)
            {
                // each tri list is guaranteed to have at least 1 tri in it
                // Using that to determine what kind of flags I need to attribute
                FullTriangle rep_tri = tri_list.ElementAt(0);
                // if its uncollidable, we dont need this list...
                if (rep_tri.collidable == false) continue;

                foreach (FullTriangle tri in tri_list)
                {
                    // find the bounding cube IDs for the tri
                    int min_geo_cube_x = (short) Math.Floor((double) MathHelpers.get_min(new int[] { tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x }) / this.coll_seg.geo_cube_scale);
                    int min_geo_cube_y = (short) Math.Floor((double) MathHelpers.get_min(new int[] { tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y }) / this.coll_seg.geo_cube_scale);
                    int min_geo_cube_z = (short) Math.Floor((double) MathHelpers.get_min(new int[] { tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z }) / this.coll_seg.geo_cube_scale);
                    int max_geo_cube_x = (short) Math.Floor((double) MathHelpers.get_max(new int[] { tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x }) / this.coll_seg.geo_cube_scale);
                    int max_geo_cube_y = (short) Math.Floor((double) MathHelpers.get_max(new int[] { tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y }) / this.coll_seg.geo_cube_scale);
                    int max_geo_cube_z = (short) Math.Floor((double) MathHelpers.get_max(new int[] { tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z }) / this.coll_seg.geo_cube_scale);

                    // NOTE: this is very crude and assumes the tri is touching every cube within the bounding box
                    //       thats created from the extrema of its vertices... but at least its fast and has no
                    //       true negatives, and the amount of false positives is small-ish for small triangles
                    // NOTE: this is explicitly using <=, because the max ID should be inclusive !
                    for (int x = min_geo_cube_x; x <= max_geo_cube_x; x++)
                    {
                        int x_ID = (x - this.coll_seg.min_geo_cube_x);
                        for (int y = min_geo_cube_y; y <= max_geo_cube_y; y++)
                        {
                            int y_ID = (y - this.coll_seg.min_geo_cube_y);
                            for (int z = min_geo_cube_z; z <= max_geo_cube_z; z++)
                            {
                                int z_ID = (z - this.coll_seg.min_geo_cube_z);

                                int cube_ID = (x_ID + (y_ID * this.coll_seg.stride_y) + (z_ID * this.coll_seg.stride_z));
                                this.coll_seg.geo_cube_list[cube_ID].coll_tri_list.Add(new Tri_Elem(tri));
                                this.coll_seg.geo_cube_list[cube_ID].tri_cnt += 1;

                                this.coll_seg.tri_cnt += 1;
                            }
                        }
                    }
                }
            }
            // now all the tris are signed in to their respective lists;
            // next, write all the tris into a long list (with duplicates) to index into, and set the starting indices
            this.coll_seg.tri_list = new Tri_Elem[this.coll_seg.tri_cnt];
            int written_tris = 0;
            for (int cube = 0; cube < this.coll_seg.geo_cube_cnt; cube++)
            {
                // set starting ID to current count
                this.coll_seg.geo_cube_list[cube].starting_tri_ID = (ushort) written_tris;

                for (int id = 0; id < this.coll_seg.geo_cube_list[cube].tri_cnt; id++)
                    this.coll_seg.tri_list[written_tris + id] = this.coll_seg.geo_cube_list[cube].coll_tri_list[id];

                written_tris += this.coll_seg.geo_cube_list[cube].tri_cnt;
            }

            this.coll_seg.valid = true;
            Console.WriteLine("Finished building Coll Segment.");
        }

        public void build_DL_seg()
        {
            this.DL_seg = new DisplayList_Segment();
            Console.WriteLine("Building DL Segment...");

            List<DisplayList_Command> command_list = new List<DisplayList_Command>();
            foreach (List<FullTriangle> tri_list in this.tri_list_tree)
            {
                // each tri list is guaranteed to have at least 1 tri in it; Using that to determine what
                // kind of DLs I need to setup pre VTX-TRI section:
                FullTriangle rep_tri = tri_list.ElementAt(0);
                // if its untextured, we dont need any DLs...
                if (rep_tri.assigned_tex_ID == -1) continue;

                Tex_Meta meta = rep_tri.assigned_tex_meta;

                // general setup stuff
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_CLEARGEOMETRYMODE((uint) (
                        Dicts.RSP_GEOMODE_FLAGS["G_SHADE"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_SHADING_SMOOTH"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_CULL_BOTH"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_FOG"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_LIGHTING"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN_LINEAR"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_LOD"]
                    ))
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETGEOMETRYMODE((uint) (
                        Dicts.RSP_GEOMODE_FLAGS["G_SHADE"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_SHADING_SMOOTH"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_CULL_BACK"] |
                        Dicts.RSP_GEOMODE_FLAGS["G_TEXTURE_GEN_LINEAR"]
                    ))
                ));

                // palette setup stuff
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_TEXTURE(0, 0, true, 0x8000, 0x8000)
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETTIMG("RGBA", 16, meta.datasection_offset_data)
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETTILE("RGBA", 4, meta.width, 0x0100, 1, 0, false, false, 0, 0, false, false, 0, 0)
                ));

                // tex-data setup stuff
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_LOADTLUT(1, 16)
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SetOtherMode_H("G_MDSFT_TEXTLUT", 2, 0x00008000)
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETTIMG("CI", 16, (meta.datasection_offset_data + 0x20)) // NOTE: +0x20 for CI4, +0x200 for CI8...
                ));
                int texel_cnt = (meta.width * meta.height);
                int texel_bitsize = 4;
                // we have to outplay the TMEM restriction of loading a max of 0x400 texels at a time
                if (texel_cnt > 0x400)
                {
                    // this should use the texel bitsize of the format instead of 4
                    int fakeout_bitsize = 16;
                    int fakeout_factor = (fakeout_bitsize / texel_bitsize);

                    // this command is a really really hacky way of outplaying the TMEM restrictions...
                    // normally, G_LOADBLOCK can only load a maximum of 0x400 texels at a time, but if we set the bitsize to 16
                    // here before loading "0x400 texels", we juke the processor into actually loading 0x1000 texels of size 4b...
                    // BUT, we have to reset this hack with an additional G_SETTILE after the G_LOADBLOCK, that fixes the fake bitsize.
                    // That's also why this command works on descriptor-7 I guess.
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_SETTILE("CI", fakeout_bitsize, 0, 0x0000, 7, 0, false, false, 6, 0, false, false, 6, 0)
                    ));
                    // Note that we are providing some tempered-with params here
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_LOADBLOCK(
                            0, 0, 7,
                            (uint) (meta.width / fakeout_factor),
                            meta.height,
                            (uint) fakeout_bitsize
                        )
                    ));
                    // restore the correct texel bitsize afterwards
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_SETTILE("CI", texel_bitsize, meta.width, 0x0000, 0, 0, false, false, 6, 0, false, false, 6, 0)
                    ));
                }
                // otherwise we can directly load to TMEM (and immediatly use descriptor-0)
                else
                {
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_SETTILE("CI", texel_bitsize, 0, 0x0000, 0, 0, false, false, 6, 0, false, false, 6, 0)
                    ));
                    // set up the faked LOADBLOCK; note that we reduce the percieved width by the fakeout factor
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_LOADBLOCK(
                            0, 0, 0,
                            meta.width, 
                            meta.height,
                            (uint) Dicts.TEXEL_FMT_BITSIZE["CI4"]
                        )
                    ));
                }
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETTILESIZE(0, 0, 0, meta.width, meta.height)
                ));

                // more general setup stuff
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_RDPPIPESYNC()
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_SETCOMBINE()
                ));
                command_list.Add(new DisplayList_Command(
                    DisplayList_Command.G_DL(false, "Mode", 0x20)
                ));

                FullTriangle[] tri_arr = tri_list.ToArray();
                // NOTE: the descriptors VTX buffer can hold 32 vertices
                // NOTE: the tri lists should be sorted by maximum ID at this point
                // NOTE: the tris should all contain 3 CONSECUTIVE vertex IDs
                //       so I can load the verts of 10 tris at a time...
                int chunk_step = 10;
                int tri_ID = 0;
                for (tri_ID = 0; tri_ID < (tri_arr.Length - chunk_step); tri_ID += chunk_step)
                {
                    // find the smallest vtx index, to figure out where to start reading from
                    // NOTE: this sucks
                    FullTriangle earliest_tri = tri_list.ElementAt(tri_ID);
                    uint smallest_index = (uint) MathHelpers.get_min(new int[] { earliest_tri.index_1, earliest_tri.index_2, earliest_tri.index_3 });

                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_VTX(0, 30, smallest_index)
                    ));
                    for (int i = (tri_ID + 0); i < (tri_ID + chunk_step); i += 2)
                    {
                        // its guaranteed that I have an even number of tris at this point, because
                        // I increment by 10 each time, and only if there are >10 left
                        command_list.Add(new DisplayList_Command(
                            DisplayList_Command.G_TRI2(
                                (uint) (tri_arr[i + 0].index_1 - smallest_index),
                                (uint) (tri_arr[i + 0].index_2 - smallest_index),
                                (uint) (tri_arr[i + 0].index_3 - smallest_index),
                                (uint) (tri_arr[i + 1].index_1 - smallest_index),
                                (uint) (tri_arr[i + 1].index_2 - smallest_index),
                                (uint) (tri_arr[i + 1].index_3 - smallest_index)
                            )
                        ));
                    }
                }
                if (tri_ID < tri_arr.Length)
                {
                    // find the smallest vtx index, to figure out where to start reading from
                    // NOTE: this sucks
                    FullTriangle earliest_tri = tri_list.ElementAt(tri_ID);
                    uint smallest_index = (uint) MathHelpers.get_min(new int[] { earliest_tri.index_1, earliest_tri.index_2, earliest_tri.index_3 });

                    // now there are only 10 or less tris left
                    command_list.Add(new DisplayList_Command(
                        DisplayList_Command.G_VTX(0, (uint) (3 * (tri_arr.Length - tri_ID)), smallest_index)
                    ));
                    for (int i = (tri_ID + 0); i < tri_arr.Length; i++)
                    {
                        // playing it safe and only doing single tris for the last <=10 tris
                        command_list.Add(new DisplayList_Command(
                            DisplayList_Command.G_TRI1(
                                (uint) (tri_arr[i + 0].index_1 - smallest_index),
                                (uint) (tri_arr[i + 0].index_2 - smallest_index),
                                (uint) (tri_arr[i + 0].index_3 - smallest_index)
                            )
                        ));
                    }
                }

                // and onto the next type of tri...
            }
            // aaand sceeeneee... hopefully
            command_list.Add(new DisplayList_Command(
                DisplayList_Command.G_ENDDL()
            ));

            this.DL_seg.command_list = command_list.ToArray();
            this.DL_seg.command_cnt = (uint) command_list.Count;
            this.DL_seg.valid = true;
            Console.WriteLine("Finished DL Segment.");
        }
        public void build_geo_seg()
        {
            this.geo_seg = new GeoLayout_Segment();
            Console.WriteLine("Building GeoLayout Segment...");

            this.geo_seg.commands.Add(GeoLayout_Command.GEO_LOAD_DL(-1000, -1000, -1000, +1000, +1000, +1000));

            this.geo_seg.valid = true;
            Console.WriteLine("Finished GeoLayout Segment.");
        }
        public void build_vtx_seg()
        {
            this.vtx_seg = new Vertex_Segment();
            Console.WriteLine("Building VTX Segment...");

            vtx_seg.max_x = short.MinValue;
            vtx_seg.max_y = short.MinValue;
            vtx_seg.max_z = short.MinValue;
            vtx_seg.min_x = short.MaxValue;
            vtx_seg.min_y = short.MaxValue;
            vtx_seg.min_z = short.MaxValue;
            vtx_seg.vtx_count = 0;
            // first loop is only used to determine the extrema aswell as figuring out how many vertices we have
            foreach (List<FullTriangle> tri_list in this.tri_list_tree)
            {
                foreach (FullTriangle tri in tri_list)
                {
                    vtx_seg.max_x = (short) MathHelpers.get_max(new int[] { vtx_seg.max_x, tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x });
                    vtx_seg.max_y = (short) MathHelpers.get_max(new int[] { vtx_seg.max_y, tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y });
                    vtx_seg.max_z = (short) MathHelpers.get_max(new int[] { vtx_seg.max_z, tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z });

                    vtx_seg.min_x = (short) MathHelpers.get_min(new int[] { vtx_seg.min_x, tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x });
                    vtx_seg.min_y = (short) MathHelpers.get_min(new int[] { vtx_seg.min_y, tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y });
                    vtx_seg.min_z = (short) MathHelpers.get_min(new int[] { vtx_seg.min_z, tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z });

                    // sneaky way of finding the count: Just find the highest referenced index ! 
                    vtx_seg.vtx_count = (ushort) MathHelpers.get_max(new int[] { vtx_seg.vtx_count, tri.index_1, tri.index_2, tri.index_3 });
                }
            }
            // (and add one because 0-indexing)
            vtx_seg.vtx_count += 1;
            // NOTE: I should get rid of this, because this is only to catch the errors in BB bins
            vtx_seg.binheader_vtx_cnt = vtx_seg.vtx_count;
            vtx_seg.vtx_list = new Vtx_Elem[vtx_seg.vtx_count];

            // second loop calculates the norm-extrema and writes the vtx data to the array
            vtx_seg.center_x = (short) ((vtx_seg.max_x + vtx_seg.min_x) / 2);
            vtx_seg.center_y = (short) ((vtx_seg.max_y + vtx_seg.min_y) / 2);
            vtx_seg.center_z = (short) ((vtx_seg.max_z + vtx_seg.min_z) / 2);
            vtx_seg.local_norm = 0;
            vtx_seg.global_norm = 0;
            short local_norm;
            short global_norm;
            foreach (List<FullTriangle> tri_list in this.tri_list_tree)
            {
                foreach (FullTriangle tri in tri_list)
                {
                    // vtx_1
                    if (vtx_seg.vtx_list[tri.index_1] == null)
                    {
                        // check if the norms exceed the current extremes
                        local_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_1.x, tri.vtx_1.y, tri.vtx_1.z, vtx_seg.center_x, vtx_seg.center_y, vtx_seg.center_z
                        );
                        global_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_1.x, tri.vtx_1.y, tri.vtx_1.z, 0, 0, 0
                        );
                        vtx_seg.local_norm = (local_norm > vtx_seg.local_norm) ? local_norm : vtx_seg.local_norm;
                        vtx_seg.global_norm = (global_norm > vtx_seg.global_norm) ? global_norm : vtx_seg.global_norm;
                        // and store it
                        vtx_seg.vtx_list[tri.index_1] = (Vtx_Elem) tri.vtx_1.Clone();
                    }
                    // vtx_2
                    if (vtx_seg.vtx_list[tri.index_2] == null)
                    {
                        // check if the norms exceed the current extremes
                        local_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_2.x, tri.vtx_2.y, tri.vtx_2.z, vtx_seg.center_x, vtx_seg.center_y, vtx_seg.center_z
                        );
                        global_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_2.x, tri.vtx_2.y, tri.vtx_2.z, 0, 0, 0
                        );
                        vtx_seg.local_norm = (local_norm > vtx_seg.local_norm) ? local_norm : vtx_seg.local_norm;
                        vtx_seg.global_norm = (global_norm > vtx_seg.global_norm) ? global_norm : vtx_seg.global_norm;
                        // and store it
                        vtx_seg.vtx_list[tri.index_2] = (Vtx_Elem) tri.vtx_2.Clone();
                    }
                    // vtx_3
                    if (vtx_seg.vtx_list[tri.index_3] == null)
                    {
                        // check if the norms exceed the current extremes
                        local_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_3.x, tri.vtx_3.y, tri.vtx_3.z, vtx_seg.center_x, vtx_seg.center_y, vtx_seg.center_z
                        );
                        global_norm = (short) MathHelpers.L2_distance(
                            tri.vtx_3.x, tri.vtx_3.y, tri.vtx_3.z, 0, 0, 0
                        );
                        vtx_seg.local_norm = (local_norm > vtx_seg.local_norm) ? local_norm : vtx_seg.local_norm;
                        vtx_seg.global_norm = (global_norm > vtx_seg.global_norm) ? global_norm : vtx_seg.global_norm;
                        // and store it
                        vtx_seg.vtx_list[tri.index_3] = (Vtx_Elem) tri.vtx_3.Clone();
                    }
                }
            }
            this.vtx_seg.valid = true;
            Console.WriteLine("Finished VTX Segment.");
        }
        public void parse_gltf_additional(String filepath)
        {
            String input_name = filepath;
            String associated_folder = filepath.Substring(0, filepath.LastIndexOf('\\')) + '\\';
            String json_content = File.ReadAllText(input_name);
            this.GLTF = JsonSerializer.Deserialize<GLTF_Handler>(json_content);

            // first, parse and prepare the lowest level / plain data components
            foreach (GLTF_BufferInternal buff in this.GLTF.buffers)
            {
                // transform the actual content
                String base64buffer = buff.uri.Replace(GLTF_Handler.URI_PREFIX, "");
                buff.content = Convert.FromBase64String(base64buffer);
            }
            foreach (GLTF_BufferView view in this.GLTF.bufferViews)
            {
                // slice the referenced buffer and store the referenced data here
                view.linked_content = new byte[view.byteLength];
                Array.Copy(
                    this.GLTF.buffers.ElementAt((int) view.buffer).content,
                    view.byteOffset,
                    view.linked_content,
                    0,
                    view.byteLength
                );
            }
            foreach (GLTF_Accessor acc in this.GLTF.accessors)
            {
                // calculate some internal properties
                acc.compLength = (GLTF_Handler.get_component_group_size(acc.type) * GLTF_Handler.get_component_type_size(acc.componentType));
                acc.byteLength = (acc.compLength * acc.count);

                // slice the referenced bufferview and store the referenced data here
                acc.linked_content = new byte[acc.byteLength];
                Array.Copy(
                    this.GLTF.bufferViews.ElementAt((int) acc.bufferView).linked_content,
                    acc.byteOffset,
                    acc.linked_content,
                    0,
                    acc.byteLength
                );

                if (acc.type == "VEC4")
                {
                    /*
                    Console.WriteLine("Parsing Color Accessor");
                    Console.WriteLine(acc.componentType);
                    Console.WriteLine(acc.type);
                    Console.WriteLine(acc.byteLength);
                    Console.WriteLine(acc.count);
                    for (int b = 0; b < acc.byteLength; b+=8)
                    {
                        Console.WriteLine(String.Format("{0}{1} {2}{3} {4}{5} {6}{7}",
                            File_Handler.uint_to_string(acc.linked_content[b + 0], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 1], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 2], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 3], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 4], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 5], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 6], 0xFF),
                            File_Handler.uint_to_string(acc.linked_content[b + 7], 0xFF)
                        ));
                    }
                    */
                }
            }

            // extract all the image data
            List<Tex_Data> tex_list = new List<Tex_Data>();
            uint integrated_data_size = 0;
            uint parsed_textures = 0;
            foreach (GLTF_Texture tex in this.GLTF.textures)
            {
                GLTF_Image img = this.GLTF.images[(int) tex.source];
                // image references external image source
                if (img.uri != null)
                {
                    img.tex_data = new Tex_Data();
                    String tex_filepath = associated_folder + img.uri;
                    if (File.Exists(tex_filepath) == false)
                    {
                        Console.WriteLine(String.Format("[ERROR] File {0} specified in uri element does not exist!", tex_filepath));
                        continue;
                    }
                    img.tex_data.img_rep = new Bitmap(tex_filepath);
                }
                // image references internal source
                else if (img.bufferView != null)
                {
                    GLTF_BufferView view = this.GLTF.bufferViews[(int) img.bufferView];
                    img.tex_data = new Tex_Data();
                    using (var stream = new MemoryStream(view.linked_content))
                    {
                        // convert the image data into the corresponding Bitmap
                        img.tex_data.img_rep = new Bitmap(stream);
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] Unexpected GLTF_Image encountered; Neither uri nor bufferView present!");
                    continue;
                }
                img.tex_data.img_rep.RotateFlip(RotateFlipType.RotateNoneFlipY);

                // next, convert the image into a BK friendly format and build the Meta Information
                img.tex_meta = new Tex_Meta();
                int ori_w = img.tex_data.img_rep.Width;
                int ori_h = img.tex_data.img_rep.Height;
                double wm_ratio = ((double) ori_w / (double) ori_h);
                int scale_w = 0;
                int scale_h = 0;
                // then convert that Bitmap into a BK friendly format
                // if the image is below 16x16 pixels, we can default to RGBA32 and keep the small size (because upscaling THAT would be useless..)
                // NOTE: if the GLTF contains images that have 16 or less colors, we also shouldnt use CI8... ugh

                // NOTE: for starters, I will transform EVERYY image to CI4, to make the DL handling easier
                // (memory alignment is handled already, dw)
                /*
                if (ori_w <= 16 && ori_h <= 16)
                {
                    // keep the aspect ratio, but scale the bigger dim to 16
                    if (wm_ratio >= 1.0) { scale_w = 16; scale_h = (int) (16 / wm_ratio); }
                    if (wm_ratio <= 1.0) { scale_w = (int) (16 * wm_ratio); scale_h = 16; }
                    // using a default color diversity of 2 here
                    img.tex_data.img_rep = Texture_Segment.convert_to_fit(img.tex_data.img_rep, scale_w, scale_h, Texture_Segment.TEX_TYPES["RGBA32"], 2);
                    img.tex_meta.tex_type = (ushort) Texture_Segment.TEX_TYPES["RGBA32"];
                    img.tex_meta.width = (byte) scale_w;
                    img.tex_meta.height = (byte) scale_h;
                }
                // if the image is below 32x32 pixels, we can still default to RGBA32
                if (ori_w <= 32 && ori_h <= 32)
                {
                    // keep the aspect ratio, but scale the bigger dim to 32
                    if (wm_ratio >= 1.0) { scale_w = 32; scale_h = (int) (32 / wm_ratio); }
                    if (wm_ratio <= 1.0) { scale_w = (int) (32 * wm_ratio); scale_h = 32; }
                    // using a default color diversity of 2 here
                    img.tex_data.img_rep = Texture_Segment.convert_to_fit(img.tex_data.img_rep, scale_w, scale_h, Texture_Segment.TEX_TYPES["RGBA32"], 2);
                    img.tex_meta.tex_type = (ushort) Texture_Segment.TEX_TYPES["RGBA32"];
                    img.tex_meta.width = (byte) scale_w;
                    img.tex_meta.height = (byte) scale_h;
                }
                else // otherwise, we will use CI8 because its also nice
                {
                    // keep the aspect ratio, but scale the bigger dim to 64
                    if (wm_ratio >= 1.0) { scale_w = 64; scale_h = (int) (64 / wm_ratio); }
                    if (wm_ratio <= 1.0) { scale_w = (int) (64 * wm_ratio); scale_h = 64; }
                    img.tex_data.img_rep = Texture_Segment.convert_to_fit(img.tex_data.img_rep, scale_w, scale_h, Texture_Segment.TEX_TYPES["CI8"], 2);
                    img.tex_meta.tex_type = (ushort) Texture_Segment.TEX_TYPES["CI8"];
                    img.tex_meta.width = (byte) scale_w;
                    img.tex_meta.height = (byte) scale_h;
                }
                */
                {
                    // CI4
                    if (wm_ratio >= 1.0) { scale_w = 64; scale_h = (int) (64 / wm_ratio); }
                    if (wm_ratio <= 1.0) { scale_w = (int) (64 * wm_ratio); scale_h = 64; }
                    img.tex_data.img_rep = Texture_Segment.convert_to_fit(img.tex_data.img_rep, scale_w, scale_h, Texture_Segment.TEX_TYPES["CI4"], 2);
                    img.tex_meta.tex_type = (ushort) Texture_Segment.TEX_TYPES["CI4"];
                    img.tex_meta.width = (byte) scale_w;
                    img.tex_meta.height = (byte) scale_h;
                }

                img.tex_data.img_rep.RotateFlip(RotateFlipType.RotateNoneFlipY);
                // relay the tex type to the data element
                img.tex_data.tex_type = img.tex_meta.tex_type;

                // and finally calculate some additional information
                img.tex_data.data = MathHelpers.convert_bitmap_to_bytes(img.tex_data.img_rep, img.tex_meta.tex_type);
                img.tex_data.data_size = (uint) img.tex_data.data.Length;

                img.tex_meta.pixel_total = (uint) (scale_w * scale_h);

                Console.WriteLine(String.Format("Parsed Tex: {0}x{1} px, Type={2}", img.tex_meta.width, img.tex_meta.height, img.tex_meta.tex_type));
            }

            // now I can go through the meshes and extract all the tris that are defined in there
            this.tri_list_tree = new List<List<FullTriangle>>();
            int tri_cnt = 0;
            foreach (GLTF_Mesh mesh in this.GLTF.meshes)
            {
                foreach (GLTF_Primitive primitive in mesh.primitives)
                {
                    List<FullTriangle> next_tri_list = this.GLTF.parse_tris_from_primitive(primitive, tri_cnt);
                    tri_cnt += next_tri_list.Count();
                    sort_tris_by_max_ID(next_tri_list);
                    this.tri_list_tree.Add(next_tri_list);
                }
            }
        }
        public void write_gltf_model()
        {
            // first of all, we have to sort the tri list
            this.consecutive_tri_list.Sort();

            this.GLTF = new GLTF_Handler();

            GLTF_Mesh tmp_mesh = new GLTF_Mesh();
            tmp_mesh.name = String.Format("Mesh Full");
            this.GLTF.meshes.Add(tmp_mesh);

            GLTF_Node tmp_node = new GLTF_Node();
            tmp_node.name = String.Format("Node Full");
            tmp_node.mesh = 0;
            this.GLTF.nodes.Add(tmp_node);

            List<Byte> raw_data_vtx_xyz = new List<Byte>();
            List<Byte> raw_data_vtx_uv = new List<Byte>();
            List<Byte> raw_data_tri = new List<Byte>();
            uint parsed_tri_types = 0;

            uint written_verts = 0;
            raw_data_vtx_xyz.Clear();
            raw_data_vtx_uv.Clear();
            raw_data_tri.Clear();
            for (int i = 0; i < this.consecutive_tri_list.Count; i++)
            {
                FullTriangle full_tri = this.consecutive_tri_list[i];

                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_1.x));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_1.y));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_1.z));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_2.x));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_2.y));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_2.z));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_3.x));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_3.y));
                raw_data_vtx_xyz.AddRange(BitConverter.GetBytes((Single) full_tri.vtx_3.z));

                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_1.transformed_U));
                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_1.transformed_V));
                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_2.transformed_U));
                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_2.transformed_V));
                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_3.transformed_U));
                raw_data_vtx_uv.AddRange(BitConverter.GetBytes(full_tri.vtx_3.transformed_V));

                raw_data_tri.AddRange(BitConverter.GetBytes((ushort) (written_verts + 0)));
                raw_data_tri.AddRange(BitConverter.GetBytes((ushort) (written_verts + 1)));
                raw_data_tri.AddRange(BitConverter.GetBytes((ushort) (written_verts + 2)));
                written_verts += 3;

                // check if the next tri is different => export current collection
                // also export collection if this was the last one
                // (because we check if it is the last one, we dont need a safety check afterwards)
                if ((i == consecutive_tri_list.Count - 1) || (full_tri.CompareTo(this.consecutive_tri_list[i + 1]) != 0))
                {
                    // building the VTX ID buffer + all correspondences
                    GLTF_BufferInternal buffer = new GLTF_BufferInternal();
                    buffer.name = String.Format("VTX-ID Buffer #{0}", parsed_tri_types);
                    buffer.content = raw_data_tri.ToArray();
                    buffer.byteLength = (uint) buffer.content.Length;
                    buffer.uri = GLTF_Handler.URI_PREFIX + Convert.ToBase64String(buffer.content);
                    this.GLTF.buffers.Add(buffer);

                    GLTF_BufferView tmp_view = new GLTF_BufferView();
                    tmp_view.name = String.Format("VTX-ID BufferView #{0}", parsed_tri_types);
                    tmp_view.buffer = (uint) (this.GLTF.buffers.Count - 1);
                    tmp_view.byteOffset = 0;
                    tmp_view.byteLength = buffer.byteLength;
                    tmp_view.target = GLTF_Handler.TARGET_TYPES["INDEX"];
                    this.GLTF.bufferViews.Add(tmp_view);

                    GLTF_Accessor tmp_accessor = new GLTF_Accessor();
                    tmp_accessor.name = String.Format("VTX-ID Accessor #{0}", parsed_tri_types);
                    uint vtx_ids_accessor_ID = (uint) (this.GLTF.bufferViews.Count - 1);
                    tmp_accessor.bufferView = vtx_ids_accessor_ID;
                    tmp_accessor.byteOffset = 0;
                    tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["USHORT"];
                    tmp_accessor.count = written_verts;
                    tmp_accessor.type = "SCALAR";
                    this.GLTF.accessors.Add(tmp_accessor);

                    // building the VTX Coords buffer + all correspondences
                    buffer = new GLTF_BufferInternal();
                    buffer.name = String.Format("VTX-Coords Buffer #{0}", parsed_tri_types);
                    buffer.content = raw_data_vtx_xyz.ToArray();
                    buffer.byteLength = (uint) buffer.content.Length;
                    buffer.uri = GLTF_Handler.URI_PREFIX + Convert.ToBase64String(buffer.content);
                    this.GLTF.buffers.Add(buffer);

                    tmp_view = new GLTF_BufferView();
                    tmp_view.name = String.Format("VTX-Coords BufferView #{0}", parsed_tri_types);
                    tmp_view.buffer = (uint) (this.GLTF.buffers.Count - 1);
                    tmp_view.byteOffset = 0;
                    tmp_view.byteLength = buffer.byteLength;
                    tmp_view.target = GLTF_Handler.TARGET_TYPES["VERTEX"];
                    this.GLTF.bufferViews.Add(tmp_view);

                    tmp_accessor = new GLTF_Accessor();
                    tmp_accessor.name = String.Format("VTX-Coords Accessor #{0}", parsed_tri_types);
                    uint vtx_coords_accessor_ID = (uint) (this.GLTF.bufferViews.Count - 1);
                    tmp_accessor.bufferView = vtx_coords_accessor_ID;
                    tmp_accessor.byteOffset = 0;
                    tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["FLOAT"];
                    tmp_accessor.count = written_verts; // 1 VEC3 per vert
                    tmp_accessor.type = "VEC3";
                    this.GLTF.accessors.Add(tmp_accessor);

                    // only for textured materials
                    uint vtx_uv_accessor_ID = 0;
                    if (full_tri.assigned_tex_ID > -1)
                    {
                        // building the VTX UV buffer + all correspondences
                        buffer = new GLTF_BufferInternal();
                        buffer.name = String.Format("VTX-UV Buffer #{0}", parsed_tri_types);
                        buffer.content = raw_data_vtx_uv.ToArray();
                        buffer.byteLength = (uint) buffer.content.Length;
                        buffer.uri = GLTF_Handler.URI_PREFIX + Convert.ToBase64String(buffer.content);
                        this.GLTF.buffers.Add(buffer);

                        tmp_view = new GLTF_BufferView();
                        tmp_view.name = String.Format("VTX-UV BufferView #{0}", parsed_tri_types);
                        tmp_view.buffer = (uint) (this.GLTF.buffers.Count - 1);
                        tmp_view.byteOffset = 0;
                        tmp_view.byteLength = buffer.byteLength;
                        tmp_view.target = GLTF_Handler.TARGET_TYPES["VERTEX"];
                        this.GLTF.bufferViews.Add(tmp_view);

                        tmp_accessor = new GLTF_Accessor();
                        tmp_accessor.name = String.Format("VTX-UV Accessor #{0}", parsed_tri_types);
                        vtx_uv_accessor_ID = (uint) (this.GLTF.bufferViews.Count - 1);
                        tmp_accessor.bufferView = vtx_uv_accessor_ID;
                        tmp_accessor.byteOffset = 0;
                        tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["FLOAT"];
                        tmp_accessor.count = written_verts; // 1 VEC2 per vert
                        tmp_accessor.type = "VEC2";
                        this.GLTF.accessors.Add(tmp_accessor);
                    }

                    GLTF_Primitive tmp_prim = new GLTF_Primitive();
                    tmp_prim.name = String.Format("Primitive #{0}", parsed_tri_types);
                    tmp_prim.attributes.Add("POSITION", vtx_coords_accessor_ID);
                    tmp_prim.indices = vtx_ids_accessor_ID;
                    // if we have an assigned texture, we need to do some extra work
                    if (full_tri.assigned_tex_ID != -1)
                    {
                        GLTF_Image tmp_img = new GLTF_Image();
                        tmp_img.uri = this.get_default_texture_name(full_tri.assigned_tex_ID);
                        this.GLTF.images.Add(tmp_img);

                        GLTF_Texture tmp_tex = new GLTF_Texture();
                        tmp_tex.source = (uint) (this.GLTF.materials.Count);
                        this.GLTF.textures.Add(tmp_tex);

                        GLTF_baseColorTexture tmp_bCT = new GLTF_baseColorTexture();
                        tmp_bCT.index = (uint) (this.GLTF.materials.Count);
                        GLTF_pbrMetallicRoughness tmp_pbr = new GLTF_pbrMetallicRoughness();
                        tmp_pbr.baseColorTexture = tmp_bCT;
                        GLTF_Material tmp_mat = new GLTF_Material();
                        tmp_mat.name = String.Format("mat_t{0}_c{1}_s{2}",
                            (uint) (this.GLTF.materials.Count),
                            File_Handler.uint_to_string(full_tri.floor_type, 0xFFFF),
                            File_Handler.uint_to_string(full_tri.sound_type, 0xFFFF)
                        );
                        tmp_mat.pbrMetallicRoughness = tmp_pbr;
                        this.GLTF.materials.Add(tmp_mat);
;                       
                        // both of these are nullable
                        tmp_prim.attributes.Add("TEXCOORD_0", vtx_uv_accessor_ID);
                        tmp_prim.material = (this.GLTF.materials.Count - 1);
                    }
                    this.GLTF.meshes[0].primitives.Add(tmp_prim);

                    parsed_tri_types += 1;
                    // and start over !
                    written_verts = 0;
                    raw_data_vtx_xyz.Clear();
                    raw_data_vtx_uv.Clear();
                    raw_data_tri.Clear();
                }
            }
        }
        public void save_GLTF()
        {
            // create the GLTF structs that rep the loaded data
            this.write_gltf_model();

            // choose the bin file name to export to
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}.gltf", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving GLTF File {0}...", chosen_filename));
                // and write to GLTF
                using (StreamWriter output_gltf = new StreamWriter(chosen_filename))
                {
                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                    output_gltf.WriteLine(JsonSerializer.Serialize(this.GLTF, options));
                }
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }

        public void overwrite_img_data(int index, byte[] replacement)
        {
            Tex_Data d = this.tex_seg.data[index];
            File_Handler.write_data(this.content, (int)d.file_offset, replacement);
        }
        public void load_BIN()
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            OFD.Filter = "BIN model Files (*.bin)|*.BIN|All Files (*.*)|*.*";
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

            this.loaded_bin_path = OFD.FileName;
            int last_slash = this.loaded_bin_path.LastIndexOf("\\");
            int file_ext = this.loaded_bin_path.LastIndexOf(".");
            this.loaded_bin_name = this.loaded_bin_path.Substring((last_slash + 1), (file_ext - last_slash - 1));
            this.parse_BIN();
        }
        public void save_BIN()
        {
            // choose the bin file name to export to
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}.bin", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving BIN File {0}...", chosen_filename));
                // and write to BIN
                File.WriteAllBytes(chosen_filename, this.content);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;

            /*
            if (this.loaded_bin_path.Contains("_copy") == false)
            {
                this.loaded_bin_path = this.loaded_bin_path.Replace(".bin", "_copy.bin");
            }
            */
        }
        public void load_GLTF()
        {
            // choose the GLTF file that you want to convert
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.InitialDirectory = File_Handler.get_basedir_or_assets();
            OFD.Filter = "GLTF model Files (*.gltf)|*.GLTF|All Files (*.*)|*.*";
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
            this.file_loaded = false;

            // parsing the chosen GLTF file
            this.parse_gltf_additional(OFD.FileName);

            // Im building all the segments before appending them to the byte-stream,
            // because their dependencies are a little whacky
            this.build_tex_seg();
            this.build_vtx_seg(); // its sort of important that I build this before I build the DLs
            this.build_DL_seg();
            this.build_coll_seg();
            this.build_geo_seg();

            byte[] parsed_content = new byte[0];

            // append empty header (will be overwritten at the end again)
            this.bin_header = new BIN_Header();
            this.bin_header.vtx_cnt = this.vtx_seg.vtx_count;
            int tri_cnt = 0;
            foreach (List<FullTriangle> tri_list in this.tri_list_tree)
                tri_cnt += tri_list.Count();
            this.bin_header.tri_cnt = (ushort) tri_cnt;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.bin_header.get_bytes());

            // append tex segment
            this.tex_seg.file_offset = (uint) parsed_content.Length;
            this.bin_header.tex_offset = (ushort) this.tex_seg.file_offset;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.tex_seg.get_bytes());

            // append DL segment
            this.DL_seg.file_offset = (uint) parsed_content.Length;
            this.bin_header.DL_offset = (uint) this.DL_seg.file_offset;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.DL_seg.get_bytes());

            // append VTX segment
            this.vtx_seg.file_offset = (uint) parsed_content.Length;
            this.bin_header.vtx_offset = (uint) this.vtx_seg.file_offset;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.vtx_seg.get_bytes());

            // append Collision Segment
            this.coll_seg.file_offset = (uint) parsed_content.Length;
            this.bin_header.coll_offset = (uint) this.coll_seg.file_offset;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.coll_seg.get_bytes());

            // append Geo segment
            this.geo_seg.file_offset = (uint) parsed_content.Length;
            this.bin_header.geo_offset = (ushort) this.geo_seg.file_offset;
            parsed_content = File_Handler.concat_arrays(parsed_content, this.geo_seg.get_bytes());

            // ignoring these for now...
            this.bone_seg = new Bone_Segment();
            this.FX_seg = new Effects_Segment();
            this.FXEND_seg = new FX_END_Segment();
            this.animtex_seg = new AnimTex_Segment();

            // and finally, overwrite the header with the updated offsets
            File_Handler.write_bytes_to_buffer(this.bin_header.get_bytes(), parsed_content, 0x00);
            this.bin_header.valid = true;

            // and set the content
            this.content = parsed_content;

            // Default: cut off the ".gltf" and replace "assets" by "exports" if applicable
            this.loaded_bin_path = OFD.FileName;
            int last_slash = this.loaded_bin_path.LastIndexOf("\\");
            int file_ext = this.loaded_bin_path.LastIndexOf(".");
            this.loaded_bin_name = this.loaded_bin_path.Substring((last_slash + 1), (file_ext - last_slash - 1));

            this.file_loaded = true;
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
                    File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
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
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));
                write_displaylist_model(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        public void export_displaylist_text()
        {
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}_DL.txt", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));
                write_displaylist_text(chosen_filename);
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
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));
                write_collision_model(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        public void write_displaylist_text(string filepath)
        {
            using (StreamWriter output_txt = new StreamWriter(filepath))
            {
                uint i = 0;
                foreach (DisplayList_Command cmd in this.DL_seg.command_list)
                {
                    output_txt.WriteLine(String.Format("{0} | {1} {2} -- {3}",
                        File_Handler.uint_to_string(i, 0xFFFF),
                        File_Handler.uint_to_string(cmd.raw_content[0], 0xFFFFFFFF),
                        File_Handler.uint_to_string(cmd.raw_content[1], 0xFFFFFFFF),
                        cmd.command_name
                    ));
                    i += 0x08;
                }
            }
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
