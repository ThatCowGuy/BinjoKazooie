using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// I hate this file structually, but the var-names are predetermined by the GLTF-2 standard
// and have to be get-settable to be picked up correctly by the JSON-Serializer.

namespace BK_BIN_Analyzer
{
    public class GLTF_Asset
    {
        public String version { get; set; }
        public String generator { get; set; }
    }
    public class GLTF_Scene
    {
        public GLTF_Scene()
        {
            this.nodes = new List<uint>();
        }
        public List<uint> nodes { get; set; } // this is an array of node-IDs
    }
    public class GLTF_Node
    {
        public String name { get; set; }
        public uint mesh { get; set; } // this is the mesh index
    }
    public class GLTF_BufferExternal
    {
        public uint byte_len { get; set; }
        public String filename { get; set; }
    }
    public class GLTF_BufferInternal
    {
        public String name { get; set; }
        public uint byteLength { get; set; }
        public String uri { get; set; }

        public byte[] content;
    }
    public class GLTF_BufferView
    {
        public String name { get; set; }
        public uint buffer { get; set; }
        public uint byteOffset { get; set; }
        public uint byteLength { get; set; }
        public uint target { get; set; } // a resource that tells me what this is would be great

        public byte[] linked_content;
    }
    public class GLTF_Accessor
    {
        public String name { get; set; }
        public uint bufferView { get; set; }
        public uint byteOffset { get; set; }
        public uint componentType { get; set; }
        public String type { get; set; } // of elements
        public uint count { get; set; } // of elements present

        public byte[] linked_content;

        public uint compLength; // Accessors dont officially need this, but I feel safer keeping track
        public uint byteLength; // Accessors dont officially need this, but I feel safer keeping track
    }


    public class GLTF_Primitive
    {
        public GLTF_Primitive()
        {
            this.attributes = new Dictionary<String, uint>();
        }
        public String name { get; set; }
        public Dictionary<String, uint> attributes { get; set; }
        public uint indices { get; set; } // this is the accessor ID for whatever reason
        public Nullable<int> material { get; set; }

        public bool collidable;
        public bool visible;
    }
    public class GLTF_Mesh
    {
        public GLTF_Mesh()
        {
            this.primitives = new List<GLTF_Primitive>();
        }
        public String name { get; set; }
        // List<GLTF_PrimitiveCollision> or List<GLTF_PrimitiveVisual>
        public List<GLTF_Primitive> primitives { get; set; }
    }

    public class GLTF_Image
    {
        public GLTF_Image()
        {
            this.name = null;
            this.uri = null;
            this.bufferView = null;
            this.mimeType = null;
        }
        public String name { get; set; }

        // Images either contain an uri for an external image source
        public String uri { get; set; }
        // or they contain a mimeType and a bufferView index for an internal source
        public Nullable<int> bufferView { get; set; }
        public String mimeType { get; set; }

        public Tex_Data tex_data;
        public Tex_Meta tex_meta;
    }
    public class GLTF_Texture
    {
        public uint source { get; set; }
    }


    public class GLTF_Material
    {
        public String name { get; set; }

        public GLTF_pbrMetallicRoughness pbrMetallicRoughness { get; set; }
    }
    public class GLTF_pbrMetallicRoughness 
    {
        public GLTF_baseColorTexture baseColorTexture { get; set; }
    }
    public class GLTF_baseColorTexture
    {
        public uint index { get; set; } // texture index
    }

    public class GLTF_Handler
    {
        public void parse_tris_from_primitive(List<FullTriangle> dest, GLTF_Primitive PRIM)
        {
            GLTF_Accessor IDX = this.accessors[(int) PRIM.indices];
            GLTF_Accessor POS = this.accessors[(int) PRIM.attributes["POSITION"]];
            PRIM.collidable = true;

            GLTF_Accessor UV = null;
            if (PRIM.attributes.ContainsKey("TEXCOORD_0") == true)
            {
                UV = this.accessors[(int) PRIM.attributes["TEXCOORD_0"]];
                PRIM.visible = true;
            }

            int tri_count = dest.Count;
            // run through all the Indices
            for (int i = 0; i < IDX.count; i += 3)
            {
                // these are the indices within the linked buffer, NOT within our VTX segment
                int idx_1 = File_Handler.read_short(IDX.linked_content, ((2 * i) + 0), true);
                int idx_2 = File_Handler.read_short(IDX.linked_content, ((2 * i) + 2), true);
                int idx_3 = File_Handler.read_short(IDX.linked_content, ((2 * i) + 4), true);

                Vtx_Elem vtx_1 = new Vtx_Elem();
                vtx_1.x = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_1) + 0), true);
                vtx_1.y = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_1) + 4), true);
                vtx_1.z = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_1) + 8), true);
                Vtx_Elem vtx_2 = new Vtx_Elem();
                vtx_2.x = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_2) + 0), true);
                vtx_2.y = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_2) + 4), true);
                vtx_2.z = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_2) + 8), true);
                Vtx_Elem vtx_3 = new Vtx_Elem();
                vtx_3.x = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_3) + 0), true);
                vtx_3.y = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_3) + 4), true);
                vtx_3.z = (short) File_Handler.read_float(POS.linked_content, ((12 * idx_3) + 8), true);
                if (PRIM.visible == true)
                {
                    vtx_1.transformed_U = File_Handler.read_float(UV.linked_content, ((8 * idx_1) + 0), true);
                    vtx_1.transformed_V = File_Handler.read_float(UV.linked_content, ((8 * idx_1) + 4), true);

                    vtx_2.transformed_U = File_Handler.read_float(UV.linked_content, ((8 * idx_2) + 0), true);
                    vtx_2.transformed_V = File_Handler.read_float(UV.linked_content, ((8 * idx_2) + 4), true);

                    vtx_3.transformed_U = File_Handler.read_float(UV.linked_content, ((8 * idx_3) + 0), true);
                    vtx_3.transformed_V = File_Handler.read_float(UV.linked_content, ((8 * idx_3) + 4), true);
                }

                FullTriangle tri = new FullTriangle();
                tri.collidable = PRIM.collidable;
                tri.visible = PRIM.visible;
                // note that we need to keep track of the tri IDs within the bin ourselves..
                // NOTE: we should really check for duplicates at this point, so that we can keep the vtx seg slim;
                //       we CANNOT keep the full-tri-list slim, because we need multiple for vis + coll (at least)
                tri.index_1 = (ushort) ((tri_count * 3) + 0);
                tri.index_2 = (ushort) ((tri_count * 3) + 1);
                tri.index_3 = (ushort) ((tri_count * 3) + 2);
                tri_count += 1;
                tri.vtx_1 = vtx_1;
                tri.vtx_2 = vtx_2;
                tri.vtx_3 = vtx_3;

                // NOTE: non-collidable, visual only mats should have something like NO_COLL in their name to encode that
                if (PRIM.material != null)
                {
                    GLTF_Material material = this.materials[(int) PRIM.material];

                    // tx_cXXXX_sXXXX_fx...
                    String mat_name = material.name;
                    mat_name = "c0000_s0000";
                    String coll_encoding = System.Text.RegularExpressions.Regex.Match(mat_name, @"(?<=c)[0-9a-fA-F]{4}").Value;
                    String sound_encoding = System.Text.RegularExpressions.Regex.Match(mat_name, @"(?<=s)[0-9a-fA-F]{4}").Value;
                    tri.floor_type = (ushort) Convert.ToInt32(coll_encoding, 16);
                    tri.sound_type = (ushort) Convert.ToInt32(sound_encoding, 16);
                    tri.collidable = true;

                    // note that this is the tex ID as listed by the parsed textures from the GLTF
                    tri.assigned_tex_ID = (short) material.pbrMetallicRoughness.baseColorTexture.index;
                    tri.assigned_tex_meta = this.images[tri.assigned_tex_ID].tex_meta;
                    tri.assigned_tex_data = this.images[tri.assigned_tex_ID].tex_data;

                    // now that we have WxH meta data, we can reverse the UVs into their BK format
                    tri.vtx_1.reverse_UV_transforms(tri.assigned_tex_meta.width, tri.assigned_tex_meta.height);
                    tri.vtx_2.reverse_UV_transforms(tri.assigned_tex_meta.width, tri.assigned_tex_meta.height);
                    tri.vtx_3.reverse_UV_transforms(tri.assigned_tex_meta.width, tri.assigned_tex_meta.height);
                }

                // and finally, add the new tri
                dest.Add(tri);
            }
        }
        public static Dictionary<String, uint> TARGET_TYPES = new Dictionary<String, uint>
        {
            // digestable
            { "VERTEX", 34962 },         // vertex attributes (XYZ, UV, RGBA...)
            { "INDEX", 34963 },          // vertex IDs
            // following the specs
            { "ARRAY_BUFFER", 34962 },         // vertex attributes (XYZ, UV, RGBA...)
            { "ELEMENT_ARRAY_BUFFER", 34963 }, // vertex IDs
        };
        public static Dictionary<String, uint> COMPONENT_TYPES = new Dictionary<String, uint>
        {
            { "USHORT", 5123 },
            { "FLOAT", 5126 },
        };
        public static String get_target_type(uint target_val)
        {
            foreach (String key in GLTF_Handler.TARGET_TYPES.Keys)
            {
                if (GLTF_Handler.TARGET_TYPES[key] == target_val)
                    return key;
            }
            return "UNDEFINED";
        }
        public static String get_component_type(uint ctype)
        {
            foreach (String key in GLTF_Handler.COMPONENT_TYPES.Keys)
            {
                if (GLTF_Handler.COMPONENT_TYPES[key] == ctype)
                    return key;
            }
            return "UNDEFINED";
        }
        public static uint get_component_type_size(uint ctype)
        {
            // I would use a switch here, but C# is afraid that the Dict might contain duplicates or something
            if (ctype == GLTF_Handler.COMPONENT_TYPES["USHORT"])
                return 2;
            if (ctype == GLTF_Handler.COMPONENT_TYPES["FLOAT"])
                return 4;
            return 0;
        }
        public static uint get_component_group_size(String cgroup)
        {
            if (cgroup == "SCALAR") // i
                return 1;
            if (cgroup == "VEC2") // UV
                return 2;
            if (cgroup == "VEC3") // XYZ
                return 3;
            if (cgroup == "VEC4") // RGBA
                return 4;
            return 0;
        }
        public static String URI_PREFIX = "data:application/octet-stream;base64,";

        public GLTF_Handler()
        {
            // asset identifiers
            this.asset = new GLTF_Asset();
            this.asset.version = "2.0";
            this.asset.generator = "BINjo Kazooie v0.0.0";
            // overhead (this should always be the same)
            // scene stuff
            this.scene = 0;
            this.scenes = new List<GLTF_Scene>();
            GLTF_Scene tmp_scene = new GLTF_Scene();
            this.scenes.Add(tmp_scene); // we will always have at least 1 scene (and probably only this one)
            // node stuff
            this.nodes = new List<GLTF_Node>();

            this.images = new List<GLTF_Image>();
            this.textures = new List<GLTF_Texture>();
            this.materials = new List<GLTF_Material>();

            // visual rep
            this.meshes = new List<GLTF_Mesh>();
            // data and descriptors
            this.accessors = new List<GLTF_Accessor>();
            this.bufferViews = new List<GLTF_BufferView>();
            this.buffers = new List<GLTF_BufferInternal>();
        }
        public GLTF_Asset asset { get; set; }
        public uint scene { get; set; } // this is my own scene index; 0 for default scene
        public List<GLTF_Scene> scenes { get; set; }
        public List<GLTF_Node> nodes { get; set; }

        public List<GLTF_Image> images { get; set; }
        public List<GLTF_Texture> textures { get; set; }



        // visual rep
        public List<GLTF_Mesh> meshes { get; set; }
        public List<GLTF_Material> materials { get; set; }
        // data and descriptors
        public List<GLTF_Accessor> accessors { get; set; }
        public List<GLTF_BufferView> bufferViews { get; set; }
        public List<GLTF_BufferInternal> buffers { get; set; }
    }
}
