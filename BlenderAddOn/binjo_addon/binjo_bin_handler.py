
from . import binjo_utils
from . binjo_model_bin import ModelBIN

import os



class BINjo_ModelBIN_Handler:

    def __init__(self, rom_filename=None):
        self.ROM_name = rom_filename
        self.ROM_data = None
        self.model_object = None

        if (rom_filename is None):
            return
        with open(rom_filename, mode="rb") as rom_file:
            self.ROM_data = rom_file.read()


    # change to a different ROM
    def change_source_ROM(self, rom_filename):
        self.ROM_name = rom_filename
        self.ROM_data = None
        if (rom_filename is None):
            return
        with open(rom_filename, mode="rb") as rom_file:
            self.ROM_data = rom_file.read()


    # load a model file from a ROM via model-filename
    def load_model_file_from_ROM(self, model_filename):
        model_file_data = binjo_utils.extract_model(self.ROM_data, model_filename)
        if (model_file_data is None or len(model_file_data) == 0):
            print(f"Model File \"{model_filename}\" could not be loaded !")
            print(f"Either Binjo straight up failed on it, or its empty !")
            print(f"Cancelling Model instantiation...")
            return

        self.model_object = ModelBIN()
        self.model_object.populate_from_data(model_file_data)
        self.model_object.arrange_mesh_data()


    # load a model file from a BIN
    def load_model_file_from_BIN(self, bin_filename):
        
        with open(bin_filename, mode="rb") as bin_file:
            model_file_data = bin_file.read()
            
        if (model_file_data is None or len(model_file_data) == 0):
            print(f"Model File \"{bin_filename}\" could not be loaded !")
            print(f"Either Binjo straight up failed on it, or its empty !")
            print(f"Cancelling Model instantiation...")
            return

        self.model_object = ModelBIN()
        self.model_object.populate_from_data(model_file_data)
        self.model_object.arrange_mesh_data()

    def dump_image_files_to(self, path):
        for IMG in self.model_object.TexSeg.tex_elements:
            IMG.export_as_file(path=path)
    



if __name__ == '__main__':
    
    ROM_list = [
        "banjo.us.v10.z64",
        "banjo.us.v10.z64.ext.z64",
    ]
    
    ROM_filename = ROM_list[1]
    print(f"Reading in ROM \"{ROM_filename}\"...")
    with open(ROM_filename, mode="rb") as rom_file:
        ROM = rom_file.read()

    filename = "TTC - Treasure Trove Cove"
    model_file = binjo_utils.extract_model(ROM, filename)

    model_object = ModelBIN(model_file)
