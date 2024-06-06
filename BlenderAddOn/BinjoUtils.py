
import zlib
import model_LU



def to_decal_hex(val, dig):
    if dig == 4:
        return f"0x{val:08X}"
    if dig == 2:
        return f"0x{val:04X}"
    if dig == 1:
        return f"0x{val:02X}"

def read_bytes(data, offset, cnt):
    result = 0x00
    for i in range(0, cnt):
        result *= 0x100
        result += data[offset + i]
    return result

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



# files only start at this offset within the
extra_file_offset = 0x10CD0

def extract_model(data, filename):
    PT_Address = model_LU.map_model_lookup[filename][1]
    ROM_address = read_bytes(data, PT_Address, 4) + extra_file_offset
    print(f"Model Filename:\t\t{filename}")
    print(f"Pointer-Table:\t\t{to_decal_hex(PT_Address, 4)}")
    print(f"ROM Adress:\t\t{to_decal_hex(ROM_address, 4)}")

    compression_ident = read_bytes(data, (ROM_address + 0), 2)
    if (compression_ident != 0x1172):
        print(f"The Data at {to_decal_hex(ROM_address, 4)} does not seem to contain Model Data !")
        print(f" -- read: {to_decal_hex(compression_ident, 2)}")
        return
    uncompressed_size = read_bytes(data, (ROM_address + 2), 4)
    print(f"4B Compression-Header:\t{to_decal_hex(uncompressed_size, 4)}")

    model_file = bytearray()
    model_file += int_to_bytes(compression_ident, 2)

    read_size = 0
    while(True):
        val = read_bytes(data, (ROM_address + 0x02 + read_size), 2)
        # check if we've reached the end
        if (val == 0x1172):
            print(f"Found start of another Model File at {to_decal_hex((ROM_address + 0x02 + read_size), 4)} - Done.")
            break
        model_file += int_to_bytes(val, 2)
        read_size += 2
    # now there is still some trailing padding (0xAA) at the end, which we wanna cut
    while(model_file[-1] == 0xAA):
        model_file.pop(-1)

    # with open("test.compr.bin", mode="wb") as output:
    #     output.write(model_file)
    model_file = decompress(model_file)
    with open("test.bin", mode="wb") as output:
        output.write(model_file)

    return model_file