
import BinjoUtils
from model_bin import ModelBIN



ROM_list = [
    "banjo.us.v10.z64",
    "banjo.us.v10.z64.ext.z64",
]

if __name__ == '__main__':
    
    ROM_filename = ROM_list[1]
    print(f"Reading in ROM \"{ROM_filename}\"...")
    with open(ROM_filename, mode="rb") as rom_file:
        ROM = rom_file.read()

    filename = "RBB - Big Fish Warehouse A"
    model_file = BinjoUtils.extract_model(ROM, filename)

    model_object = ModelBIN(model_file)
