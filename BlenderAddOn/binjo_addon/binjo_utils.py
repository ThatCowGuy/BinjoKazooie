
import zlib
import numpy as np
# from PIL import Image 
import sys

from . import binjo_model_LU



def to_decal_hex(val, dig):
    if dig == 4:
        return f"0x{val:08X}"
    if dig == 2:
        return f"0x{val:04X}"
    if dig == 1:
        return f"0x{val:02X}"

def read_bytes(data, offset, cnt, type="uint"):
    result = 0x00
    for i in range(0, cnt):
        result *= 0x100
        result += data[offset + i]

    if (type == "uint" or type =="u"):
        return result
    if (type == "signed" or type == "s"):
        if (result >> ((cnt * 8) - 1) == 0b01):
            result = int(result - np.power(2.0, (cnt * 8)))
        return result

def get_bytes(data, offset, cnt):
    return data[offset:(offset+cnt)]

def int_to_bytes(val, cnt, endianness="big"):
    return val.to_bytes(cnt, byteorder=endianness)



# from decomp tools/rareunzip.py
def decompress(data):
    # the 0x1172 is not neccessary for the decompression
    if read_bytes(data[:2], 0, 2) == 0x1172:
        data = data[2:]
    # actual decompression
    decompressor = zlib.decompressobj(wbits=-15) # raw deflate bytestream
    return decompressor.decompress(data[4:])     # drop 4 byte length header


    

def create_IMG_from_bytes(pixel_data, w, h):
    # pure python via PIL
    # https://pillow.readthedocs.io/en/stable/handbook/concepts.html#concept-modes
    # blender version using bpy
    # https://blender.stackexchange.com/questions/643/is-it-possible-to-create-image-data-and-save-to-a-file-from-a-script
    # Mode RGBA should catch all needs; expecting 4x8 bit pixels
    pixel_data = pixel_data.flatten()
    
    # return Image.frombytes("RGBA", (w, h), pixel_data)


    
# https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
def convert_img_data_to_pixels(bin_data, tex_type, w, h):
    if (tex_type == 0x01): # C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
        
        # first parse the color palette
        color_palette = np.zeros((0x10, 4), dtype=np.uint8)
        for i in range(0, 0x10):
            color_value = read_bytes(bin_data, (i*2), 2)
            # RGB5551
            color_palette[i, 0] = int(((color_value >> 0xB) & 0b011111) / 0b011111 * 0xFF)  # R
            color_palette[i, 1] = int(((color_value >> 0x6) & 0b011111) / 0b011111 * 0xFF)  # G
            color_palette[i, 2] = int(((color_value >> 0x1) & 0b011111) / 0b011111 * 0xFF)  # B
            # and grab alpha from final bit
            color_palette[i, 3] = (color_value & 0b0001) * 0xFF

        # then parse the image data
        pixel_data = np.zeros((h, w, 4), dtype=np.uint8)
        for y in range(0, h):
            for x in range(0, w):
                # calc the pixel index
                px_id = (y * w) + x
                # NOTE: this grabs the full byte, but the actual ID is only one nibble of that -> split it
                pal_id = read_bytes(bin_data, (0x20 + (px_id // 2)), 1)
                if px_id % 2 == 0:
                    pal_id = (pal_id >> 4) & 0b1111
                else:
                    pal_id = (pal_id >> 0) & 0b1111
                # NOTE: the python Image constructors expect the colors to be in RGB...
                pixel_data[y, x, 0] = color_palette[pal_id, 0]
                pixel_data[y, x, 1] = color_palette[pal_id, 1]
                pixel_data[y, x, 2] = color_palette[pal_id, 2]
                pixel_data[y, x, 3] = color_palette[pal_id, 3]
        return color_palette, pixel_data

    if (tex_type == 0x02): # C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        
        # first parse the color palette
        color_palette = np.zeros((0x100, 4), dtype=np.uint8)
        for i in range(0, 0x100):
            color_value = read_bytes(bin_data, (i*2), 2)
            # RGB5551
            color_palette[i, 0] = int(((color_value >> 0xB) & 0b011111) / 0b011111 * 0xFF)  # R
            color_palette[i, 1] = int(((color_value >> 0x6) & 0b011111) / 0b011111 * 0xFF)  # G
            color_palette[i, 2] = int(((color_value >> 0x1) & 0b011111) / 0b011111 * 0xFF)  # B
            # and grab alpha from final bit
            color_palette[i, 3] = (color_value & 0b0001) * 0xFF

        # then parse the image data
        pixel_data = np.zeros((h, w, 4), dtype=np.uint8)
        for y in range(0, h):
            for x in range(0, w):
                # calc the pixel index
                px_id = (y * w) + x
                pal_id = read_bytes(bin_data, (0x200 + px_id), 1)
                # NOTE: the python Image constructors expect the colors to be in RGB...
                pixel_data[y, x, 0] = color_palette[pal_id, 0] # R
                pixel_data[y, x, 1] = color_palette[pal_id, 1] # G
                pixel_data[y, x, 2] = color_palette[pal_id, 2] # B
                pixel_data[y, x, 3] = color_palette[pal_id, 3] # A
        return color_palette, pixel_data

    if (tex_type == 0x04): # RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel

        # parse the image data
        pixel_data = np.zeros((h, w, 4), dtype=np.uint8)
        for y in range(0, h):
            for x in range(0, w):
                # calc the pixel index
                px_id = (y * w) + x
                color_value = read_bytes(bin_data, (px_id * 2), 2)
                # NOTE: the python Image constructors expect the colors to be in RGB...
                pixel_data[y, x, 0] = int(((color_value >> 0xB) & 0b011111) / 0b011111 * 0xFF)  # R
                pixel_data[y, x, 1] = int(((color_value >> 0x6) & 0b011111) / 0b011111 * 0xFF)  # G
                pixel_data[y, x, 2] = int(((color_value >> 0x1) & 0b011111) / 0b011111 * 0xFF)  # B
                pixel_data[y, x, 3] = (color_value & 0b1) * 0xFF  # A
        return None, pixel_data

    if (tex_type == 0x08): # RGBA32 or RGB8888 without a palette; pixels stored as a 32bit texel

        # parse the image data
        pixel_data = np.zeros((h, w, 4), dtype=np.uint8)
        for y in range(0, h):
            for x in range(0, w):
                # calc the pixel index
                px_id = (y * w) + x
                # NOTE: the python Image constructors expect the colors to be in RGB...
                pixel_data[y, x, 0] = int(bin_data[(px_id * 4) + 0]) # R
                pixel_data[y, x, 1] = int(bin_data[(px_id * 4) + 1]) # G
                pixel_data[y, x, 2] = int(bin_data[(px_id * 4) + 2]) # B
                pixel_data[y, x, 3] = int(bin_data[(px_id * 4) + 3]) # A
        return None, pixel_data

    if (tex_type == 0x10): # IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha

        # parse the image data
        pixel_data = np.zeros((h, w, 4), dtype=np.uint8)
        for y in range(0, h):
            for x in range(0, w):
                # calc the pixel index
                px_id = (y * w) + x
                # in IA8, the first nibble is the intensity => every color-value
                # NOTE: This math looks pretty weird, but the 2nd summand is just to interpolate the values from
                #       a nibble into a byte, to avoid rounding oddities
                intensity = int( ((bin_data[px_id] << 0) & 0b11110000) + ((bin_data[px_id] >> 4) & 0b00001111) )
                alpha     = int( ((bin_data[px_id] << 4) & 0b11110000) + ((bin_data[px_id] >> 0) & 0b00001111) )

                # NOTE: the python Image constructors expect the colors to be in RGB...
                pixel_data[y, x, 0] = intensity  # R
                pixel_data[y, x, 1] = intensity  # G
                pixel_data[y, x, 2] = intensity  # B
                pixel_data[y, x, 3] = alpha      # A
        return None, pixel_data




# files only start at this offset within the
extra_file_offset = 0x10CD0

def extract_model(data, filename):
    if (filename not in binjo_model_LU.map_model_lookup):
        print(f"Model Filename \"{filename}\" is not part of the LU in \"binjo_model_LU.py\" !")
        print(f"Cancelling extraction...")
        return None
    PT_Address = binjo_model_LU.map_model_lookup[filename][1]
    model_start_address = read_bytes(data, PT_Address + 0x00, 4) + extra_file_offset
    model_end_address   = read_bytes(data, PT_Address + 0x08, 4) + extra_file_offset
    print(f"Model Filename:\t\t{filename}")
    print(f"Pointer-Table:\t\t{to_decal_hex(PT_Address, 4)}")
    print(f"Model Start Adress:\t{to_decal_hex(model_start_address, 4)}")
    print(f"Model End Adress:\t{to_decal_hex(model_end_address, 4)}")

    compression_ident = read_bytes(data, (model_start_address + 0), 2)
    if (compression_ident != 0x1172):
        print(f"The Data at {to_decal_hex(model_start_address, 4)} does not seem to contain Model Data !")
        print(f" -- read: {to_decal_hex(compression_ident, 2)}")
        return
    uncompressed_size = read_bytes(data, (model_start_address + 2), 4)
    print(f"4B Compression-Header:\t{to_decal_hex(uncompressed_size, 4)}")

    model_file = bytearray()
    model_file += int_to_bytes(compression_ident, 2)

    read_size = 0
    while(model_start_address + 0x02 + read_size < model_end_address):
        val = read_bytes(data, (model_start_address + 0x02 + read_size), 2)
        model_file += int_to_bytes(val, 2)
        read_size += 2
    # now there is still some trailing padding (0xAA) at the end, which we wanna cut
    while(model_file[-1] == 0xAA):
        model_file.pop(-1)

    # with open("test.compr.bin", mode="wb") as output:
    #     output.write(model_file)
    model_file = decompress(model_file)
    # with open("test.bin", mode="wb") as output:
    #     output.write(model_file)

    return model_file