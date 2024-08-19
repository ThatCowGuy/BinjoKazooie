
import zlib
import numpy as np
# from PIL import Image 
import sys

from . import binjo_model_LU
from . binjo_dicts import Dicts

from timeit import default_timer as timer



def report_time(running_timer, msg):
    print(f">>> ({timer() - running_timer:.3f}s) -- {msg}.")
    return timer()

# if any of the VTXs UVs are misaligned / too far away from 0, realign all of the
# VTX within the given list (always assume it's a triplet because model should be triangulated)
def realign_vtx_UVs(vtx_triplet, tex_w, tex_h):
    # higher bound
    while (np.max([vtx_triplet[0].u, vtx_triplet[1].u, vtx_triplet[2].u]) > 0x8000):
        for vtx in vtx_triplet:
            vtx.u -= (64 * tex_w)
    while (np.max([vtx_triplet[0].v, vtx_triplet[1].v, vtx_triplet[2].v]) > 0x8000):
        for vtx in vtx_triplet:
            vtx.v -= (64 * tex_h)
    # lower bound
    while (np.min([vtx_triplet[0].u, vtx_triplet[1].u, vtx_triplet[2].u]) < -0x7FFF):
        for vtx in vtx_triplet:
            vtx.u += (64 * tex_w)
    while (np.min([vtx_triplet[0].v, vtx_triplet[1].v, vtx_triplet[2].v]) < -0x7FFF):
        for vtx in vtx_triplet:
            vtx.v += (64 * tex_h)
    # if either UV is NOW bigger than 0x8000 again, the UV face is bigger than 0xFFFF in total !
    if (
        np.max([vtx_triplet[0].u, vtx_triplet[1].u, vtx_triplet[2].u]) > 0x8000 or \
        np.max([vtx_triplet[0].v, vtx_triplet[1].v, vtx_triplet[2].v]) > 0x8000
    ):
        print(f"A Face has way too big UVs: {[f'XYZ={(vtx.x, vtx.y, vtx.z)} UV={(vtx.u, vtx.v)}' for vtx in vtx_triplet]} !")
        return -1
    return 0

def to_decal_hex(val, dig, prefix="0x"):
    if dig == 8:
        return f"{prefix}{val:016X}"
    if dig == 4:
        return f"{prefix}{val:08X}"
    if dig == 2:
        return f"{prefix}{val:04X}"
    if dig == 1:
        return f"{prefix}{val:02X}"

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

def get_2s_complement(value, byte_cnt):
    if (value < 0):
        value = int(np.power(2.0, (byte_cnt * 8)) - abs(value))
    return value

def int_to_bytes(val, cnt, endianness="big"):
    val = get_2s_complement(val, cnt)
    return val.to_bytes(cnt, byteorder=endianness)
    
def concat_bytes(src, dst):
    if type(dst) is not list:
        dst = [dst]
    for byte in dst:
        src.append(byte)
    return src

def apply_bitmask(value, mask):
    # apply mask
    tmp_val = (value & mask)
    # and shift the value according to the masks position
    # (== r-shifting both until the lowest mask-bit is set)
    while (mask & 0b01 == 0):
        tmp_val = tmp_val >> 1
        mask = mask >> 1
    return tmp_val

# cut VALUE in the shape of BIT_LEN and shift it by BIT_OFFSET to the left
def shift_cut(value, bit_offset, bit_len):
    value = int(value) & int(np.power(2, bit_len) - 1)
    return (value << bit_offset)

# DXT is a binary 12b fractional number with 11b for the mantissa
# I return it as an int though, because its not neccessary to ever
# interpret it as a float; We only ever need to write the raw data
def calc_DXT(width, bit_size):
    # DXT (this is a really messy one:
    # "dxt is an unsigned fixed-point 1.11 [11 digit mantissa] number"
    # "dxt is the RECIPROCAL of the number of 64-bit chunks it takes to get a row of texture"
    # an example: Take a 32x32 px Tex with 16b colors;
    # -> a row of that Tex takes 32x16b = 512b
    # -> so it needs (512b/64b) = 8 chunks of 64b to create a row
    # -> the reciprocal is 1/8, which in binary is 0.001_0000_0000 = 0x100
    #
    # since bitsize is divisble by 4, and width is divisible by 16,
    # bits_per_row has to be divisible by 64; So this is lossless
    bits_per_row = (width * bit_size)
    chunks_per_row = (bits_per_row / 64.0)
    # furthermore, this should result in a power of 2, say 16 eg.
    # 16 = 0b10000, with only 1 bit set. The corresponding DXT should ALSO
    # only set 1 bit: the 1/16th bit! So we can simply calculate the log2
    # of the chunks_per_row result to get the set bit, and build the DXT
    # in the correct binary encoding with that knowledge:
    set_bit = int(np.log2(chunks_per_row))
    DXT = (0b01 << (11 - set_bit))
    # Console.WriteLine(String.Format("{0}", File_Handler.uint_to_string((uint) set_bit, 0b1))])
    # Console.WriteLine(String.Format("{0}, {1}, {2}", width, bitsize, File_Handler.uint_to_string((uint) DXT, 0b1))])
    return DXT


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
    # to see if this is ran from blenders API or not, just use a try-catch block that will throw if bpy is inexistent
    try:
        if ("bpy" not in sys.modules):
            import bpy
        IMG = bpy.data.images.new("tmp", width=w, height=h)
        IMG.pixels = pixel_data
        return IMG
    except:
        print("BPY doesn't exist; You need to run this from Blender")
        return None


    
# https://n64squid.com/homebrew/n64-sdk/textures/image-formats/
def convert_img_data_to_pixels(bin_data, tex_type, w, h):
    if (tex_type == Dicts.TEX_TYPES["CI4"]): # C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
        
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

    if (tex_type == Dicts.TEX_TYPES["CI8"]): # C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        
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

    if (tex_type == Dicts.TEX_TYPES["RGBA16"]): # RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel

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

    if (tex_type == Dicts.TEX_TYPES["RGBA32"]): # RGBA32 or RGBA8888 without a palette; pixels stored as a 32bit texel

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

    if (tex_type == Dicts.TEX_TYPES["IA8"]): # IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha

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





class ColorPixel:
    def __init__(self, r, g, b, a, occurences=1):
        # map alpha to all-or-nothing
        if (a < (255 / 2)):
            self.r, self.g, self.b, self.a = 0xFF, 0xFF, 0xFF, 0x00
        else:
            self.r, self.g, self.b, self.a = round(r), round(g), round(b), 0xFF
        self.occurences = occurences
    def __eq__(self, other):
        return (
            self.r == other.r and \
            self.g == other.g and \
            self.b == other.b and \
            self.a == other.a
        )
    def color_distance(self, other):
        dr = self.r - other.r
        dg = self.g - other.g
        db = self.b - other.b
        da = self.a - other.a
        return np.floor(np.sqrt(dr*dr + dg*dg + db*db + da*da))

    def convert_8888_to_5551(self):
        r = (self.r >> 3) & 0b11111
        g = (self.g >> 3) & 0b11111
        b = (self.b >> 3) & 0b11111
        a = 1 if self.a > (255 / 2) else 0
        return (r << 11) + (g << 6) + (b << 1) + (a << 0)

# From an input IMG, build a palette containing (color_cnt) colors, which best
# represent the IMG. The most occuring colors will be selected at first, and then
# close matches will be filtered out iteratively until the palette is acceptable;
# finally, pull in more colors and try to merge them into the existing palette
# until they stop mattering
def approx_palette_by_most_used_with_diversity(IMG_color_pixels, color_cnt, diversity_threshold=3):
    # first create a unique palette from the color-pixels, counting their occurences
    palette = []
    for cpx in IMG_color_pixels:
        # collect unique colors, and count their occurences
        if (cpx not in palette):
            palette.append(cpx)
        else:
            palette[palette.index(cpx)].occurences += 1

    # sort by occurences (high occurences == important color)
    palette = sorted(palette, key=lambda color: color.occurences, reverse=True)
    
    # now build the reduced palette by using the top (color_cnt) colors
    reduced_palette = []
    while (len(reduced_palette) < color_cnt and len(palette) > 0):
        reduced_palette.append(palette.pop(0))
    while (len(reduced_palette) < color_cnt):
        reduced_palette.append(ColorPixel(0, 0, 0, 0))
        
    # now check if the reduced palette contains colors that are less diverse than our
    # diversity_threshold argument, merge them and pull in another color instead
    while (len(palette) > 0):
        # find the worst diversity match (ie. the closest 2 colors)
        worst_diversity_match = 1e+10
        match_A = None
        match_B = None
        for idx_A in range(0, len(reduced_palette)):
            for idx_B in range(idx_A + 1, len(reduced_palette)):
                diversity = reduced_palette[idx_A].color_distance(reduced_palette[idx_B])
                if (diversity == 0):
                    continue
                if (diversity < worst_diversity_match):
                    worst_diversity_match = diversity
                    match_A = reduced_palette[idx_A]
                    match_B = reduced_palette[idx_B]
        # if the worst diversity is acceptable, we can stop
        if (worst_diversity_match > diversity_threshold):
            break
        
        # otherwise, merge the matches into a new color
        combined_occurences = (match_A.occurences + match_B.occurences)
        merger_alpha = match_A.a if (match_A.occurences > match_B.occurences) else match_B.a
        merged_color = ColorPixel(
            ((match_A.r * match_A.occurences) + (match_B.r * match_B.occurences)) / combined_occurences,
            ((match_A.g * match_A.occurences) + (match_B.g * match_B.occurences)) / combined_occurences,
            ((match_A.b * match_A.occurences) + (match_B.b * match_B.occurences)) / combined_occurences,
            merger_alpha,
            occurences=combined_occurences
        )
        # remove the matches
        reduced_palette.remove(match_A)
        reduced_palette.remove(match_B)
        # add the merger, and pull in another new color from the remaining palette
        reduced_palette.append(merged_color)
        reduced_palette.append(palette.pop(0))
        
    # second pass: get the next best colors (until they stop mattering) and merge them in aswell
    while (len(palette) > 0):
        # get the next best color
        next_best_color = palette.pop(0)
        # if the color is really meaningless (< 0.3% usage), stop
        if (next_best_color.occurences < (len(IMG_color_pixels) * 0.3 / 100.0)):
            break

        # find the worst diversity match (ie. the closest 2 colors)
        worst_diversity_match = 1e+10
        match_B = None
        for idx_B in range(0, len(reduced_palette)):
            diversity = next_best_color.color_distance(reduced_palette[idx_B])
            if (diversity == 0):
                continue
            if (diversity < worst_diversity_match):
                worst_diversity_match = diversity
                match_B = reduced_palette[idx_B]
        
        # in this pass, definetely merge the colors
        combined_occurences = (next_best_color.occurences + match_B.occurences)
        merger_alpha = next_best_color.a if (next_best_color.occurences > match_B.occurences) else match_B.a
        merged_color = ColorPixel(
            ((next_best_color.r * next_best_color.occurences) + (match_B.r * match_B.occurences)) / combined_occurences,
            ((next_best_color.g * next_best_color.occurences) + (match_B.g * match_B.occurences)) / combined_occurences,
            ((next_best_color.b * next_best_color.occurences) + (match_B.b * match_B.occurences)) / combined_occurences,
            merger_alpha,
            occurences=combined_occurences
        )
        # replace the original with the merger
        reduced_palette.remove(match_B)
        reduced_palette.append(merged_color)
    
    # and we are finally done !
    return reduced_palette

# from an array of color-pixels and a palette, grab the palette-indices
# that best approximate the IMG itself
def convert_IMG_pixels_into_palette_indices(IMG_color_pixels, palette):
    indices = []
    for color in IMG_color_pixels:
        # find the best fitting color from the palette
        closest_diversity_match = 1e10
        closest_idx = -1
        for idx in range(0, len(palette)):
            diversity = color.color_distance(palette[idx])
            # in this case, a diversity of 0 is a perfect match !
            if (diversity == 0):
                closest_idx = idx
                break
            if (diversity < closest_diversity_match):
                closest_diversity_match = diversity
                closest_idx = idx
        indices.append(closest_idx)
    return indices



def check_IMG_data_for_transparency(IMG_data, tex_type):
    # in both CI formats, it suffices to check the palette for an alpha entry;
    # all colors are RGBA8888 in CI palettes, and we only need to check the A component
    if (tex_type == "CI4"):
        for i in range(0, 0x10):
            if (IMG_data[int((i*4) + 3)] < 0xFF):
                return True
        return False
    if (tex_type == "CI8"):
        for i in range(0, 0x100):
            if (IMG_data[int((i*4) + 3)] < 0xFF):
                return True
        return False
    # unsupported tex_types...
    return False

def convert_RGBA32_IMG_to_bytes(IMG, tex_type):

    if (tex_type == Dicts.TEX_TYPES["CI4"]): # C4 or CI4; 16 RGBA5551-colors, pixels are encoded per row as 4bit IDs
        # print("Converting IMG to CI4 palette + indices...")

        # round every color value to 5-bit 
        byte_pixels = [((round(255 * val) >> 3) << 3) for val in IMG.pixels]
        color_pixels = []
        for px in range(len(byte_pixels) // 4):
            # unroll the next 4 values into RGBA
            r, g, b, a = byte_pixels[(4 * px) + 0 : (4 * px) + 4]
            cpx = ColorPixel(r, g, b, a)
            color_pixels.append(cpx)

        palette = approx_palette_by_most_used_with_diversity(color_pixels, 0x10, diversity_threshold=3)
        indices = convert_IMG_pixels_into_palette_indices(color_pixels, palette)
        # convert palette and indices into a bytearray
        data = bytearray()
        for color in palette:
            data += int_to_bytes(int(color.convert_8888_to_5551()), 2)
        for idx in range(0, (len(indices) // 2)):
            concat_index = ((indices[2*idx + 0] & 0x0F) << 4) + ((indices[2*idx + 1] & 0x0F) << 0)
            data += int_to_bytes(concat_index, 1)

        color_pixels = [palette[idx] for idx in indices]
        pixels = []
        [pixels.extend([cpx.r, cpx.g, cpx.b, cpx.a]) for cpx in color_pixels]
        return data, pixels

    if (tex_type == Dicts.TEX_TYPES["CI8"]): # C8 or CI8; 256 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        # print("Converting IMG to CI8 palette + indices...")
        
        # round every color value to 5-bit 
        byte_pixels = [((round(255 * val) >> 3) << 3) for val in IMG.pixels]
        color_pixels = []
        for px in range(len(byte_pixels) // 4):
            # unroll the next 4 values into RGBA
            r, g, b, a = byte_pixels[(4 * px) + 0 : (4 * px) + 4]
            cpx = ColorPixel(r, g, b, a)
            color_pixels.append(cpx)

        palette = approx_palette_by_most_used_with_diversity(color_pixels, 0x100, diversity_threshold=3)
        indices = convert_IMG_pixels_into_palette_indices(color_pixels, palette)
        # convert palette and indices into a bytearray
        data = bytearray()
        for color in palette:
            data += int_to_bytes(int(color.convert_8888_to_5551()), 2)
        for idx in range(0, len(indices)):
            data += int_to_bytes(indices[idx], 1)

        color_pixels = [palette[idx] for idx in indices]
        pixels = []
        [pixels.extend([cpx.r, cpx.g, cpx.b, cpx.a]) for cpx in color_pixels]
        return data, pixels
    
    print("Unknown tex type in convert_IMG_to_palette_and_pixels() !")
    return None, None



# determine if a given tri is intersecting a cube volume through SAT (Separating Axis Theorem)
# https://dyn4j.org/2010/01/sat/
# NOTE: Tris and Cubes are always convex; Cubes are always axis-aligned and rasterized
def tri_intersects_cube(tri, cube):
    # put the vertex coords into np arrays for speed
    A = np.array([tri.vtx_1.x, tri.vtx_1.y, tri.vtx_1.z])
    B = np.array([tri.vtx_2.x, tri.vtx_2.y, tri.vtx_2.z])
    C = np.array([tri.vtx_3.x, tri.vtx_3.y, tri.vtx_3.z])

    # shift both bodies so that the cube's center sits at origin
    # (the cube doesnt actually need to be shifted, bc it's relative)
    A = A - cube.center
    B = B - cube.center
    C = C - cube.center
    # obviously dont need to recalc tri-sidelengths a,b,c and tri-norm n because those are all relative to cube-sides A,B,C

    # now we can check all 13 SA's; Starting with the 3 cube normals, because those are the softest computationally
    # NOTE: because the cube normals are always the carthesian unit vectors, I know the results here a priori, so
    # I can skip ahead in the projection-calculation; example for cube nx:
    # pA = np.dot(tri.A, cube_nx) == tri.A.x
    # pB = np.dot(tri.B, cube_nx) == tri.B.x
    # pC = np.dot(tri.C, cube_nx) == tri.C.x
    #
    # NOTE: the projected extent of the cube is always L if projected along a cube normal
    #       --> cube_extent = cube_L
    if (
        # nx
        -1.0 * np.max([A[0], B[0], C[0]]) > cube.scale or \
        +1.0 * np.min([A[0], B[0], C[0]]) > cube.scale or \
        # ny
        -1.0 * np.max([A[1], B[1], C[1]]) > cube.scale or \
        +1.0 * np.min([A[1], B[1], C[1]]) > cube.scale or \
        # nz
        -1.0 * np.max([A[2], B[2], C[2]]) > cube.scale or \
        +1.0 * np.min([A[2], B[2], C[2]]) > cube.scale
    ):
        print("ni SAT axis trigger")
        return False
        
    # now, find the 9 cross-product sepperation-axes and check those
    sepperation_axes = [None] * 9
    # I can skip calculating these properly, because I know nx,ny,nz of the cube a priori
    # nx, ny, nz cross tri edge a
    sepperation_axes[0] = np.array([0,     -A[2], +A[1]])
    sepperation_axes[1] = np.array([+A[2], 0,     -A[0]])
    sepperation_axes[2] = np.array([-A[1], +A[0], 0    ])
    # nx, ny, nz cross tri edge b
    sepperation_axes[3] = np.array([0,     -B[2], +B[1]])
    sepperation_axes[4] = np.array([+B[2], 0,     -B[0]])
    sepperation_axes[5] = np.array([-B[1], +B[0], 0    ])
    # nx, ny, nz cross tri edge c
    sepperation_axes[6] = np.array([0,     -C[2], +C[1]])
    sepperation_axes[7] = np.array([+C[2], 0,     -C[0]])
    sepperation_axes[8] = np.array([-C[1], +C[0], 0    ])

    # now project the vertices A,B,C of the tri onto the SAs, and compare to the projected cube extent
    for sep_axis in sepperation_axes:
        pA = np.dot(A, sep_axis)
        pB = np.dot(B, sep_axis)
        pC = np.dot(C, sep_axis)
        # the projected extent of the cube can be simplified a lot too:
        cube_extent = cube.scale * np.sum(abs(sep_axis))

        # and repeat the check from earlier:
        if (
            -1.0 * np.max([pA, pB, pC]) > cube_extent or \
            +1.0 * np.min([pA, pB, pC]) > cube_extent
        ):
            print("cross-prod SAT axis trigger")
            return False

    # if none of the 13 SA's triggered, the bodies don't overlap on any of the SA's, and according to the SAT, they therefor dont intersect
    return True



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
    if (model_start_address == model_end_address):
        print(f"Model Filename \"{filename}\" doesn't contain any Data !")
        print(f"Cancelling...")
        return None

    print(f"Model Filename:\t\t{filename}")
    print(f"Pointer-Table:\t\t{to_decal_hex(PT_Address, 4)}")
    print(f"Model Start Adress:\t{to_decal_hex(model_start_address, 4)}")
    print(f"Model End Adress:\t{to_decal_hex(model_end_address, 4)}")

    compression_ident = read_bytes(data, (model_start_address + 0), 2)
    if (compression_ident != 0x1172):
        print(f"The Data at {to_decal_hex(model_start_address, 4)} does not seem to contain Model Data !")
        print(f" -- read: {to_decal_hex(compression_ident, 2)}")
        return None

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