
class Dicts:
    tex_types = {
        "CI4":      0x01, # C4 or CI4; 16 RGB5551-colors, pixels are encoded per row as 4bit IDs
        "CI8":      0x02, # C8 or CI8; 32 RGBA5551-colors, pixels are encoded per row as 8bit IDs
        "RGBA16":   0x04, # RGBA16 or RGBA5551 without a palette; pixels stored as a 16bit texel
        "RGBA32":   0x08, # RGBA32 or RGBA8888 without a palette; pixels stored as a 32bit texel
        "IA8":      0x10, # IA8 - each byte is a pixel; a nibble of intensity and a nibble of alpha
    }