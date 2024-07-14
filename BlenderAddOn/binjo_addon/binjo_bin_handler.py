
from . import binjo_utils
from . binjo_model_bin import ModelBIN

import os



class BINjo_ModelBIN_Handler:

    def __init__(self, filename):
        with open(filename, mode="rb") as rom_file:
            self.ROM_data = rom_file.read()
        self.model_object = None

    def load_model_file(self, model_filename):
        model_file_data = binjo_utils.extract_model(self.ROM_data, model_filename)
        if (model_file_data is None or len(model_file_data) == 0):
            print(f"Model File \"{model_filename}\" could not be loaded !")
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
