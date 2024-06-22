
from . import binjo_utils
from . binjo_model_bin_header import ModelBIN_Header
from . binjo_model_bin_texture_seg import ModelBIN_TexSeg
from . binjo_model_bin_vertex_seg import ModelBIN_VtxSeg
from . binjo_model_bin_collision_seg import ModelBIN_ColSeg

class ModelBIN:
    # Header                done
    # Texture               done
    # Vertex                done
    # Bone
    # Collision             done
    # DisplayList
    # Effects
    # FX_END
    # Animated Textures
    # GeoLayout

    def __init__(self, bin_data):
        self.Header = ModelBIN_Header(bin_data)
        self.TexSeg = ModelBIN_TexSeg(bin_data, self.Header.tex_offset)
        self.VtxSeg = ModelBIN_VtxSeg(bin_data, self.Header.vtx_offset, vtx_cnt=self.Header.vtx_cnt)

        self.ColSeg = ModelBIN_ColSeg(bin_data, self.Header.coll_offset)
        


    # arrange the model data into lists that Blender can convert into a mesh
    def arrange_mesh_data(self):
        self.vertex_coord_list = []
        self.vertex_shade_list = []
        scale_factor = 1.0
        for vtx in self.VtxSeg.vtx_list:
            # NOTE: Im swapping Y and Z in here, and flipping Z afterwards,
            #       because BK uses a different coord system than blender
            self.vertex_coord_list.append((
                + vtx.x * scale_factor,
                - vtx.z * scale_factor, # swapped and flipped
                + vtx.y * scale_factor  # swapped
            ))

        self.face_idx_list = []
        self.edge_idx_list = []
        for tri in self.ColSeg.tri_list:
            self.face_idx_list.append((tri.index_1, tri.index_2, tri.index_3))
            self.edge_idx_list.append((tri.index_1, tri.index_2))
            self.edge_idx_list.append((tri.index_2, tri.index_3))
            self.edge_idx_list.append((tri.index_3, tri.index_1))
        return