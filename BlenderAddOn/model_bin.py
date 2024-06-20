
import BinjoUtils
from model_bin_header import ModelBIN_Header
from model_bin_texture_seg import ModelBIN_TexSeg
from model_bin_vertex_seg import ModelBIN_VtxSeg

class ModelBIN:
    # Header                done
    # Texture               done
    # Vertex
    # Bone
    # Collision
    # DisplayList
    # Effects
    # FX_END
    # Animated Textures
    # GeoLayout

    def __init__(self, bin_data):
        self.Header = ModelBIN_Header(bin_data)
        self.TexSeg = ModelBIN_TexSeg(bin_data, self.Header.tex_offset)
        self.VtxSeg = ModelBIN_VtxSeg(bin_data, self.Header.vtx_offset, vtx_cnt=self.Header.vtx_cnt)