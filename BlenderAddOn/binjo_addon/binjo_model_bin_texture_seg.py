
from . import binjo_utils
import os

class ModelBIN_TexSeg:
    HEADER_SIZE = 0x08

    def __init__(self, data, file_offset):
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
            tex = ModelBIN_TexElem(data, file_offset_meta, file_offset_data, img_data_size)
            self.tex_elements.append(tex)

        print(f"parsed {self.tex_cnt} image files.")
        self.valid = True
        return
    
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
    def __init__(self, data, file_offset_meta, file_offset_data, img_data_size):
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

        # additional parsed properties (DATA elements)
        # === 0x00 ===============================
        self.data_size = img_data_size
        self.img_data = binjo_utils.get_bytes(data, file_offset_data, self.data_size)
        self.palette, self.pixel_data = binjo_utils.convert_img_data_to_pixels(
            self.img_data,
            self.tex_type,
            self.width, self.height
        )

        # to see if this is ran from blenders API or not, just use a try-catch block that will throw if bpy is inexistent
        try:
            import bpy
            self.IMG = bpy.data.images.new("tmp", width=self.width, height=self.height)
            # Blenders bpy.data.images expects the RGBA values to range inbetween (0.0, 1.0) instead of (0, 255)
            pixel_data_floats = [float(val / 255.0) for val in self.pixel_data.flatten()]
            self.IMG.pixels = pixel_data_floats
            self.IMG.file_format = 'PNG'
            self.is_blender = True
        except:
            from PIL import Image
            self.IMG = Image.frombytes("RGBA", (self.width, self.height), self.pixel_data.flatten())
            self.IMG.save(f"exports/pic_{binjo_utils.to_decal_hex(self.datasection_offset_data, 4)}.png")
            self.is_blender = False

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
