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

        public List<FullTriangle> full_tri_list = new List<FullTriangle>();

        public void parse_BIN()
        {
            this.file_loaded = false;
            this.full_tri_list = new List<FullTriangle>();

            this.content = System.IO.File.ReadAllBytes(this.loaded_bin_path);
            bin_header.populate(this.content);
            tex_seg.populate(this.content, (int)bin_header.tex_offset);
            vtx_seg.binheader_vtx_cnt = bin_header.vtx_cnt;
            vtx_seg.populate(this.content, (int)bin_header.vtx_offset);
            bone_seg.populate(this.content, (int)bin_header.bone_offset);
            coll_seg.populate(this.content, (int)bin_header.coll_offset);

            // start of the full tri list by adding every collision tri
            // and inferring the correct associated vertices
            this.full_tri_list = coll_seg.export_tris_as_full();
            vtx_seg.infer_vtx_data_for_full_tris(this.full_tri_list);

            DL_seg.populate(this.content, (int)bin_header.DL_offset, this.tex_seg, this.vtx_seg, this.full_tri_list); // this guy needs handles for several inferrations
            FX_seg.populate(this.content, (int)bin_header.FX_offset);
            FXEND_seg.populate(this.content, (int)bin_header.FX_END);
            animtex_seg.populate(this.content, (int)bin_header.anim_tex_offset);

            // now we can sort the full tri list
            this.full_tri_list.Sort();

            /*
            string output_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("testing.gltf"));
            this.write_gltf_model(output_filename);
            string input_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("testing_blender_out.gltf"));
            this.gltf_to_full_tri_list(input_filename);
            */
            this.file_loaded = true;
        }
        public void export_gltf_model()
        {
            string chosen_filename = Path.Combine(File_Handler.get_basedir_or_exports(), String.Format("{0}.gltf", this.loaded_bin_name));

            SaveFileDialog SFD = new SaveFileDialog();
            SFD.InitialDirectory = File_Handler.get_basedir_or_exports();
            SFD.FileName = chosen_filename;
            if (SFD.ShowDialog() == DialogResult.OK)
            {
                chosen_filename = SFD.FileName;
                File_Handler.remembered_exports_path = Path.GetDirectoryName(chosen_filename);
                System.Console.WriteLine(String.Format("Saving Object File {0}...", chosen_filename));

                write_gltf_model(chosen_filename);
                return;
            }
            System.Console.WriteLine("Cancelled");
            return;
        }
        public List<FullTriangle> gltf_to_full_tri_list(String filepath)
        {
            String input_name = filepath;
            String json_content = File.ReadAllText(input_name);
            GLTF_Handler gltf_input = JsonSerializer.Deserialize<GLTF_Handler>(json_content);

            List<FullTriangle> imported_list = new List<FullTriangle>();

            uint counter = 0;
            foreach (GLTF_BufferInternal buff in gltf_input.buffers)
            {
                Console.WriteLine(String.Format("=== Reading Buffer: {0}", counter));
                Console.WriteLine("Len: " + buff.byteLength);
                String base64buffer = buff.uri.Replace(GLTF_Handler.URI_PREFIX, "");
                buff.content = Convert.FromBase64String(base64buffer);
                counter++;
            }
            counter = 0;
            foreach (GLTF_BufferView view in gltf_input.bufferViews)
            {
                Console.WriteLine(String.Format("=== Reading View: {0}", counter));
                Console.WriteLine("Off: " + view.byteOffset);
                Console.WriteLine("Len: " + view.byteLength);
                Console.WriteLine("Buf: " + view.buffer);
                Console.WriteLine("Targ:" + GLTF_Handler.get_target_type(view.target));

                view.linked_content = new byte[view.byteLength];
                Array.Copy(
                    gltf_input.buffers.ElementAt((int) view.buffer).content,
                    view.byteOffset,
                    view.linked_content,
                    0,
                    view.byteLength
                );
                counter++;
            }
            counter = 0;
            foreach (GLTF_Accessor acc in gltf_input.accessors)
            {
                Console.WriteLine(String.Format("=== Reading Accessor: {0}", counter));
                Console.WriteLine("View: " + acc.bufferView);
                Console.WriteLine("Type: " + acc.type);
                Console.WriteLine("CT: " + GLTF_Handler.get_component_type(acc.componentType));
                Console.WriteLine("cnt: " + acc.count);
                acc.compLength = (GLTF_Handler.get_component_group_size(acc.type) * GLTF_Handler.get_component_type_size(acc.componentType));
                acc.byteLength = (acc.compLength * acc.count);
                Console.WriteLine("Len: " + acc.byteLength);

                acc.linked_content = new byte[acc.byteLength];
                Array.Copy(
                    gltf_input.bufferViews.ElementAt((int) acc.bufferView).linked_content,
                    acc.byteOffset,
                    acc.linked_content,
                    0,
                    acc.byteLength
                );
                counter++;
            }
            counter = 0;
            List<FullTriangle> parsed_tris = new List<FullTriangle>();
            foreach (GLTF_Mesh mesh in gltf_input.meshes)
            {
                Console.WriteLine(String.Format("=== Reading Mesh: {0}", counter));
                foreach (GLTF_Primitive primitive in mesh.primitives)
                {
                    int pos_ID;
                    int idx_ID;
                    int uv_ID;
                    GLTF_Accessor pos_acc;
                    GLTF_Accessor idx_acc;
                    GLTF_Accessor uv_acc;

                    Console.WriteLine(String.Format("Attr:"));
                    Console.WriteLine(String.Format("POSITION: {0}", primitive.attributes["POSITION"]));
                    pos_ID = (int) primitive.attributes["POSITION"];
                    pos_acc = gltf_input.accessors[pos_ID];
                    idx_ID = (int) primitive.indices;
                    idx_acc = gltf_input.accessors[idx_ID];
                    primitive.collidable = true;

                    if (primitive.attributes.ContainsKey("TEXCOORD_0"))
                    {
                        Console.WriteLine(String.Format("TEXCOORD_0: {0}", primitive.attributes["TEXCOORD_0"]));
                        uv_ID = (int) primitive.attributes["TEXCOORD_0"];
                        uv_acc = gltf_input.accessors[uv_ID];
                        primitive.visible = true;
                    }
                    gltf_input.parse_tris_from_primitive(parsed_tris, primitive);
                }
                counter++;
            }
            return imported_list;
        }
        public void write_gltf_model(String filepath)
        {
            GLTF_Handler gltf = new GLTF_Handler();

            GLTF_Mesh tmp_mesh = new GLTF_Mesh();
            tmp_mesh.name = String.Format("Mesh Full");
            gltf.meshes.Add(tmp_mesh);

            GLTF_Node tmp_node = new GLTF_Node();
            tmp_node.name = String.Format("Node Full");
            tmp_node.mesh = 0;
            gltf.nodes.Add(tmp_node);

            List<Byte> raw_data_vtx_xyz = new List<Byte>();
            List<Byte> raw_data_vtx_uv = new List<Byte>();
            List<Byte> raw_data_tri = new List<Byte>();
            uint parsed_tri_types = 0;

            uint written_verts = 0;
            raw_data_vtx_xyz.Clear();
            raw_data_vtx_uv.Clear();
            raw_data_tri.Clear();
            for (int i = 0; i < this.full_tri_list.Count; i++)
            {
                FullTriangle full_tri = this.full_tri_list[i];

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
                if ((i == full_tri_list.Count - 1) || (full_tri.CompareTo(this.full_tri_list[i + 1]) != 0))
                {
                    // building the VTX ID buffer + all correspondences
                    GLTF_BufferInternal buffer = new GLTF_BufferInternal();
                    buffer.name = String.Format("VTX-ID Buffer #{0}", parsed_tri_types);
                    buffer.content = raw_data_tri.ToArray();
                    buffer.byteLength = (uint) buffer.content.Length;
                    buffer.uri = GLTF_Handler.URI_PREFIX + Convert.ToBase64String(buffer.content);
                    gltf.buffers.Add(buffer);

                    GLTF_BufferView tmp_view = new GLTF_BufferView();
                    tmp_view.name = String.Format("VTX-ID BufferView #{0}", parsed_tri_types);
                    tmp_view.buffer = (uint) (gltf.buffers.Count - 1);
                    tmp_view.byteOffset = 0;
                    tmp_view.byteLength = buffer.byteLength;
                    tmp_view.target = GLTF_Handler.TARGET_TYPES["INDEX"];
                    gltf.bufferViews.Add(tmp_view);

                    GLTF_Accessor tmp_accessor = new GLTF_Accessor();
                    tmp_accessor.name = String.Format("VTX-ID Accessor #{0}", parsed_tri_types);
                    uint vtx_ids_accessor_ID = (uint) (gltf.bufferViews.Count - 1);
                    tmp_accessor.bufferView = vtx_ids_accessor_ID;
                    tmp_accessor.byteOffset = 0;
                    tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["USHORT"];
                    tmp_accessor.count = written_verts;
                    tmp_accessor.type = "SCALAR";
                    gltf.accessors.Add(tmp_accessor);

                    // building the VTX Coords buffer + all correspondences
                    buffer = new GLTF_BufferInternal();
                    buffer.name = String.Format("VTX-Coords Buffer #{0}", parsed_tri_types);
                    buffer.content = raw_data_vtx_xyz.ToArray();
                    buffer.byteLength = (uint) buffer.content.Length;
                    buffer.uri = GLTF_Handler.URI_PREFIX + Convert.ToBase64String(buffer.content);
                    gltf.buffers.Add(buffer);

                    tmp_view = new GLTF_BufferView();
                    tmp_view.name = String.Format("VTX-Coords BufferView #{0}", parsed_tri_types);
                    tmp_view.buffer = (uint) (gltf.buffers.Count - 1);
                    tmp_view.byteOffset = 0;
                    tmp_view.byteLength = buffer.byteLength;
                    tmp_view.target = GLTF_Handler.TARGET_TYPES["VERTEX"];
                    gltf.bufferViews.Add(tmp_view);

                    tmp_accessor = new GLTF_Accessor();
                    tmp_accessor.name = String.Format("VTX-Coords Accessor #{0}", parsed_tri_types);
                    uint vtx_coords_accessor_ID = (uint) (gltf.bufferViews.Count - 1);
                    tmp_accessor.bufferView = vtx_coords_accessor_ID;
                    tmp_accessor.byteOffset = 0;
                    tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["FLOAT"];
                    tmp_accessor.count = written_verts; // 1 VEC3 per vert
                    tmp_accessor.type = "VEC3";
                    gltf.accessors.Add(tmp_accessor);

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
                        gltf.buffers.Add(buffer);

                        tmp_view = new GLTF_BufferView();
                        tmp_view.name = String.Format("VTX-UV BufferView #{0}", parsed_tri_types);
                        tmp_view.buffer = (uint) (gltf.buffers.Count - 1);
                        tmp_view.byteOffset = 0;
                        tmp_view.byteLength = buffer.byteLength;
                        tmp_view.target = GLTF_Handler.TARGET_TYPES["VERTEX"];
                        gltf.bufferViews.Add(tmp_view);

                        tmp_accessor = new GLTF_Accessor();
                        tmp_accessor.name = String.Format("VTX-UV Accessor #{0}", parsed_tri_types);
                        vtx_uv_accessor_ID = (uint) (gltf.bufferViews.Count - 1);
                        tmp_accessor.bufferView = vtx_uv_accessor_ID;
                        tmp_accessor.byteOffset = 0;
                        tmp_accessor.componentType = GLTF_Handler.COMPONENT_TYPES["FLOAT"];
                        tmp_accessor.count = written_verts; // 1 VEC2 per vert
                        tmp_accessor.type = "VEC2";
                        gltf.accessors.Add(tmp_accessor);
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
                        gltf.images.Add(tmp_img);

                        GLTF_Texture tmp_tex = new GLTF_Texture();
                        tmp_tex.source = (uint) (gltf.materials.Count);
                        gltf.textures.Add(tmp_tex);

                        GLTF_baseColorTexture tmp_bCT = new GLTF_baseColorTexture();
                        tmp_bCT.index = (uint) (gltf.materials.Count);
                        GLTF_pbrMetallicRoughness tmp_pbr = new GLTF_pbrMetallicRoughness();
                        tmp_pbr.baseColorTexture = tmp_bCT;
                        GLTF_Material tmp_mat = new GLTF_Material();
                        tmp_mat.name = String.Format("mat_t{0}_c{1}_s{2}",
                            (uint) (gltf.materials.Count),
                            File_Handler.uint_to_string(full_tri.floor_type, 0xFFFF),
                            File_Handler.uint_to_string(full_tri.sound_type, 0xFFFF)
                        );
                        tmp_mat.pbrMetallicRoughness = tmp_pbr;
                        gltf.materials.Add(tmp_mat);
;                       
                        // both of these are nullable
                        tmp_prim.attributes.Add("TEXCOORD_0", vtx_uv_accessor_ID);
                        tmp_prim.material = (gltf.materials.Count - 1);
                    }
                    gltf.meshes[0].primitives.Add(tmp_prim);

                    parsed_tri_types += 1;
                    // and start over !
                    written_verts = 0;
                    raw_data_vtx_xyz.Clear();
                    raw_data_vtx_uv.Clear();
                    raw_data_tri.Clear();
                }
            }

            using (StreamWriter output_gltf = new StreamWriter(filepath))
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                output_gltf.WriteLine(JsonSerializer.Serialize(gltf, options));
            }
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
