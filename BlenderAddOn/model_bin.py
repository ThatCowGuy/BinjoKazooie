
import BinjoUtils
from model_bin_header import ModelBIN_Header
from model_bin_texture_seg import ModelBIN_TexSeg

class ModelBIN:
    # Header
    # Texture
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