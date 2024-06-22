
from . import binjo_utils
from . binjo_model_bin import ModelBIN

import os


ROM_list = [
    "banjo.us.v10.z64",
    "banjo.us.v10.z64.ext.z64",
]

class BINjo_ModelBIN_Handler:

    def __init__(self, filename):
        with open(filename, mode="rb") as rom_file:
            self.ROM_data = rom_file.read()

        model_filename = "RBB - Big Fish Warehouse A"
        model_file_data = binjo_utils.extract_model(self.ROM_data, model_filename)

        self.model_object = ModelBIN(model_file_data)



if __name__ == '__main__':
    
    ROM_filename = ROM_list[1]
    print(f"Reading in ROM \"{ROM_filename}\"...")
    with open(ROM_filename, mode="rb") as rom_file:
        ROM = rom_file.read()

    filename = "RBB - Big Fish Warehouse A"
    model_file = binjo_utils.extract_model(ROM, filename)

    model_object = ModelBIN(model_file)
