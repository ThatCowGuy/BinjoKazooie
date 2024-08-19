
from . import binjo_utils
from . binjo_dicts import Dicts
import os
import sys
import bpy

class ModelBIN_TexSeg:
    HEADER_SIZE = 0x08

    def __init__(self):
        self.valid = False

    def populate_from_data(self, data, file_offset):
        if file_offset == 0:
            print("No Texture Segment")
            self.valid = False
            return

        # locators
        self.file_offset = file_offset
        self.file_offset_meta = file_offset + ModelBIN_TexSeg.HEADER_SIZE

        # parsed properties
        # === 0x00 ===============================
        self.data_size  = binjo_utils.read_bytes(data, file_offset + 0x00, 4)
        self.tex_cnt    = binjo_utils.read_bytes(data, file_offset + 0x04, 2)
        self.unk_1      = binjo_utils.read_bytes(data, file_offset + 0x06, 2)

        # computing properties
        self.meta_data_size = (self.tex_cnt * ModelBIN_TexElem.META_SIZE)
        self.full_header_size = ModelBIN_TexSeg.HEADER_SIZE + self.meta_data_size
        self.file_offset_data = file_offset + self.full_header_size

        # now get all the tex elements; first, grab all the data offsets though
        img_data_offsets = []
        for idx in range(0, self.tex_cnt):
            file_offset_meta = file_offset + ModelBIN_TexSeg.HEADER_SIZE + (idx * ModelBIN_TexElem.META_SIZE)
            img_data_offsets.append(binjo_utils.read_bytes(data, file_offset_meta + 0x00, 4))
        # the final entry is slightly "fake" because its just the end of all img data, but I need this for size-calc
        img_data_offsets.append(self.data_size - self.full_header_size)
        self.tex_elements = []
        for idx in range(0, self.tex_cnt):
            # some precalculated values for element extraction
            file_offset_meta = file_offset + ModelBIN_TexSeg.HEADER_SIZE + (idx * ModelBIN_TexElem.META_SIZE)
            file_offset_data = self.file_offset_data + img_data_offsets[idx]
            img_data_size = (img_data_offsets[idx+1] - img_data_offsets[idx])
            # now create the tex element and append it to our list
            tex = ModelBIN_TexElem()
            tex.build_from_data(data, file_offset_meta, file_offset_data, img_data_size)
            self.tex_elements.append(tex)

        print(f"parsed {self.tex_cnt} image files.")
        self.valid = True
        return

    def populate_from_elements(self, tex_list):
        self.file_offset = 0x0038
        self.file_offset_meta = self.file_offset + ModelBIN_TexSeg.HEADER_SIZE
        
        self.tex_cnt = 0
        self.data_size = 0
        self.tex_elements = []
        for tex in tex_list:
            tex.datasection_offset_data = self.data_size
            tex.file_offset_meta = self.file_offset_meta + (self.tex_cnt * ModelBIN_TexElem.META_SIZE)
            self.tex_cnt += 1
            self.tex_elements.append(tex)
            self.data_size += tex.data_size
        
        # for the data size, dont forget to add the meta element sizes too !
        self.data_size += ModelBIN_TexSeg.HEADER_SIZE
        self.data_size += (self.tex_cnt * ModelBIN_TexElem.META_SIZE)

        self.meta_data_size = (self.tex_cnt * ModelBIN_TexElem.META_SIZE)
        self.full_header_size = ModelBIN_TexSeg.HEADER_SIZE + self.meta_data_size
        self.file_offset_data = self.file_offset + self.full_header_size

        self.unk_1 = 0
        self.valid = True

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.data_size, 4)
        output += binjo_utils.int_to_bytes(self.tex_cnt, 2)
        output += binjo_utils.int_to_bytes(0x00, 2)
        for tex in self.tex_elements:
            output += tex.get_bytes_meta()
        for tex in self.tex_elements:
            output += tex.get_bytes_data()
        return output
    
    # figure out which tex corresponds to a given datasection offset
    def get_tex_ID_from_datasection_offset(self, datasection_offset_data):
        for idx, tex in enumerate(self.tex_elements):
            if (tex.datasection_offset_data == datasection_offset_data):
                return idx
        return -1




class ModelBIN_TexElem:
    # not calling this just "SIZE" because the element itself also contains the (disjunct) data..
    META_SIZE = 0x10

    # input the file_offset to the meta element
    def __init__(self):
        pass

    def __eq__(self, other):
        # first compare some light meta data
        if (self.tex_type != other.tex_type):
            return False
        if (self.width != other.width):
            return False
        if (self.height != other.height):
            return False
        # then compare the actual pixel data
        if (self.color_pixels != other.color_pixels):
            return False
        return True
        
    def build_from_data(self, data, file_offset_meta, file_offset_data, img_data_size):
        # parsed properties (META elements)
        # === 0x00 ===============================
        self.datasection_offset_data = binjo_utils.read_bytes(data, file_offset_meta + 0x00, 4)
        self.tex_type                = binjo_utils.read_bytes(data, file_offset_meta + 0x04, 2)
        self.unk_1                   = binjo_utils.read_bytes(data, file_offset_meta + 0x06, 2)
        self.width                   = binjo_utils.read_bytes(data, file_offset_meta + 0x08, 1)
        self.height                  = binjo_utils.read_bytes(data, file_offset_meta + 0x09, 1)
        self.unk_2                   = binjo_utils.read_bytes(data, file_offset_meta + 0x0A, 2)
        self.unk_3                   = binjo_utils.read_bytes(data, file_offset_meta + 0x0C, 4)

        # locators
        self.file_offset_meta = file_offset_meta
        self.file_offset_data = file_offset_data
        
        # computed properties
        self.pixel_total = self.width * self.height

        #self.section_offset_data = self.full_header_size + self.datasection_offset_data
        #self.section_offset_meta = 0
        #self.section_offset_data = 0

        expected_texel_size = ((self.width * self.height) * Dicts.TEXEL_FMT_BITSIZE[self.tex_type]) // 8
        expected_palet_size = Dicts.TEX_TYPE_PALETTE_SIZE[self.tex_type]
        # at this point, Im checking 2 things:
        # 1) BB erronously declares some CI8 textures as being CI4 in the headers. This results
        #    in a palette-size difference of exactly 0x1E0 bytes, and double the expected texel-size
        if (img_data_size == (expected_palet_size + 0x1E0 + int(expected_texel_size * 2.0))):
            print(f"Tex {binjo_utils.to_decal_hex(self.datasection_offset_data, 4)} erronously declared as CI4 in Header instead of CI8; Changing tex_type.")
            self.tex_type = Dicts.TEX_TYPES["CI8"]
        # 2) If a texture contains mipmaps, its data-size (minus the palette) will be exactly
        #    1.5x larger, regardless of its dimension.
        if (img_data_size == (expected_palet_size + int(expected_texel_size * 1.5))):
            print(f"Tex {binjo_utils.to_decal_hex(self.datasection_offset_data, 4)} is mipmapped; ignoring for now...")

        # additional parsed properties (DATA elements)
        # === 0x00 ===============================
        self.data_size = img_data_size
        self.img_data = binjo_utils.get_bytes(data, file_offset_data, self.data_size)

        self.contains_transparency = binjo_utils.check_IMG_data_for_transparency(self.img_data, self.tex_type)
        self.palette, self.color_pixels = binjo_utils.convert_img_data_to_pixels(
            self.img_data,
            self.tex_type,
            self.width, self.height
        )

        self.Blender_IMG = bpy.data.images.new("tmp", width=self.width, height=self.height)
        self.Blender_IMG.file_format = 'PNG'
        # Blenders bpy.data.images expects the RGBA values to range inbetween (0.0, 1.0) instead of (0, 255)
        blender_pixels = [float(val / 255.0) for val in self.color_pixels.flatten()]
        self.Blender_IMG.pixels = blender_pixels

        

    def build_from_IMG(IMG):
        tex = ModelBIN_TexElem()
        # dimensions
        tex.width, tex.height = IMG.size[0], IMG.size[1]
        tex.pixel_total = (tex.width * tex.height)
        # type + data
        tex.tex_type = Dicts.TEX_TYPES["CI4"]
        tex.image_formatted_data, tex.color_pixels = binjo_utils.convert_RGBA32_IMG_to_bytes(IMG, tex.tex_type)
        tex.contains_transparency = binjo_utils.check_IMG_data_for_transparency(tex.image_formatted_data, tex.tex_type)
        tex.data_size = len(tex.image_formatted_data)
        return tex

    def get_bytes_meta(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.datasection_offset_data, 4)
        output += binjo_utils.int_to_bytes(self.tex_type, 2)
        output += binjo_utils.int_to_bytes(0x00, 2)
        output += binjo_utils.int_to_bytes(self.width, 1)
        output += binjo_utils.int_to_bytes(self.height, 1)
        output += binjo_utils.int_to_bytes(0x00, 2)
        output += binjo_utils.int_to_bytes(0x00, 4)
        return output

    def get_bytes_data(self):
        return bytearray(self.image_formatted_data)
        



    def export_as_file(self, path):
        self.IMG.filepath_raw = path + f"pic_{binjo_utils.to_decal_hex(self.datasection_offset_data, 2)}.png"
        self.IMG.file_format = 'PNG'
        self.IMG.save()

    def __str__(self):
        return (
            f'Tex_Meta(\n'
            f'    datasection_offset_data = {binjo_utils.to_decal_hex(self.datasection_offset_data, 4)},\n'
            f'    tex_type                = {binjo_utils.to_decal_hex(self.tex_type, 2)},\n'
            f'    unk_1                   = {binjo_utils.to_decal_hex(self.unk_1, 2)},\n'
            f'    width                   = {binjo_utils.to_decal_hex(self.width, 1)},\n'
            f'    height                  = {binjo_utils.to_decal_hex(self.height, 1)},\n'
            f'    unk_2                   = {binjo_utils.to_decal_hex(self.unk_2, 2)},\n'
            f'    unk_3                   = {binjo_utils.to_decal_hex(self.unk_3, 4)}\n'
            f')'
        )

    def export_as_binary(self, filename):
        with open(filename, mode="wb") as output:
            output.write(self.img_data)
