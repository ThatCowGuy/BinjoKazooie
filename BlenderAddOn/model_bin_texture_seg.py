
import BinjoUtils
import os
from PIL import Image 

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
        self.data_size = BinjoUtils.read_bytes(data, file_offset + 0x00, 4)
        self.tex_cnt = BinjoUtils.read_bytes(data, file_offset + 0x04, 2)
        self.unk_1 = BinjoUtils.read_bytes(data, file_offset + 0x06, 2)

        # computing properties
        self.meta_data_size = (self.tex_cnt * ModelBIN_TexElem.META_ELEMENT_SIZE)
        self.full_header_size = ModelBIN_TexSeg.HEADER_SIZE + self.meta_data_size
        self.file_offset_data = file_offset + self.full_header_size

        # now get all the tex elements; first, grab all the data offsets though
        img_data_offsets = []
        for idx in range(0, self.tex_cnt):
            file_offset_meta = file_offset + ModelBIN_TexSeg.HEADER_SIZE + (idx * ModelBIN_TexElem.META_ELEMENT_SIZE)
            img_data_offsets.append(BinjoUtils.read_bytes(data, file_offset_meta + 0x00, 4))
        # the final entry is slightly "fake" because its just the end of all img data, but I need this for size-calc
        img_data_offsets.append(self.data_size)

        self.tex_elements = []
        for idx in range(0, self.tex_cnt):
            # some precalculated values for element extraction
            file_offset_meta = file_offset + ModelBIN_TexSeg.HEADER_SIZE + (idx * ModelBIN_TexElem.META_ELEMENT_SIZE)
            file_offset_data = img_data_offsets[idx]
            img_data_size = (img_data_offsets[idx+1] - img_data_offsets[idx])
            # now create the tex element and append it to our list
            tex = ModelBIN_TexElem(data, file_offset_meta, file_offset_data, img_data_size)
            # tex_filename = f"image_{idx:02d}.tex.bin"
            # tex.export_as_binary(tex_filename)
            # cmd = f"./BINjoTextureConverter.exe {tex_filename}"
            # print(cmd)
            # os.system(cmd)
            self.tex_elements.append(tex)

        # data
        self.valid = True

    def 

    def create_IMG_from_bytes(byte_arr):
        # https://pillow.readthedocs.io/en/stable/handbook/concepts.html#concept-modes
        # Mode RGBA should catch all needs; expecting 4x8 bit pixels
        IMG = Image.frombytes("RGBA", (32, 32), byte_arr)
        IMG.save("test.png")



class ModelBIN_TexElem:
    META_ELEMENT_SIZE = 0x10

    # input the file_offset to the meta element
    def __init__(self, data, file_offset_meta, file_offset_data, img_data_size):
        # parsed properties (META elements)
        # === 0x00 ===============================
        self.datasection_offset_data = BinjoUtils.read_bytes(data, file_offset_meta + 0x00, 4)
        self.tex_type                = BinjoUtils.read_bytes(data, file_offset_meta + 0x04, 2)
        self.unk_1                   = BinjoUtils.read_bytes(data, file_offset_meta + 0x06, 2)
        self.width                   = BinjoUtils.read_bytes(data, file_offset_meta + 0x08, 1)
        self.height                  = BinjoUtils.read_bytes(data, file_offset_meta + 0x09, 1)
        self.unk_2                   = BinjoUtils.read_bytes(data, file_offset_meta + 0x0A, 2)
        self.unk_3                   = BinjoUtils.read_bytes(data, file_offset_meta + 0x0C, 4)

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
        self.img_data = BinjoUtils.get_bytes(data, file_offset_data, self.data_size)
        print(self)

    def __str__(self):
        return (
            f'Tex_Meta(\n'
            f'    datasection_offset_data = {BinjoUtils.to_decal_hex(self.datasection_offset_data, 4)},\n'
            f'    tex_type                = {BinjoUtils.to_decal_hex(self.tex_type, 2)},\n'
            f'    unk_1                   = {BinjoUtils.to_decal_hex(self.unk_1, 2)},\n'
            f'    width                   = {BinjoUtils.to_decal_hex(self.width, 1)},\n'
            f'    height                  = {BinjoUtils.to_decal_hex(self.height, 1)},\n'
            f'    unk_2                   = {BinjoUtils.to_decal_hex(self.unk_2, 2)},\n'
            f'    unk_3                   = {BinjoUtils.to_decal_hex(self.unk_3, 4)}\n'
            f')'
        )

    def export_as_binary(self, filename):
        with open(filename, mode="wb") as output:
            output.write(self.img_data)

    def convert_binary_to_pixels(self):
        