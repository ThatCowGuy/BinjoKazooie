import numpy as np

class Vtx_Elem:
    # default constructor
    def __init__(self):
        self.x = np.int16(0)
        self.y = np.int16(0)
        self.z = np.int16(0)
        self.padding = np.int16(0)
        self.u = np.int16(0)
        self.v = np.int16(0)
        self.transformed_U = np.float32(0.0)
        self.transformed_V = np.float32(0.0)
        self.r = np.uint8(0xFF)
        self.g = np.uint8(0xFF)
        self.b = np.uint8(0xFF)
        self.a = np.uint8(0xFF)

    def calc_transformed_UVs(self, tiledes):
        if tiledes.assigned_tex_meta is None or tiledes.assigned_tex_data is None:
            self.transformed_U = ((self.u / 64.0) + 0 + 0.5) / 32.0
            self.transformed_V = ((self.v / 64.0) + 0 + 0.5) / 32.0
        else:
            self.transformed_U = ((self.u / 64.0) + tiledes.S_shift + 0.5) / tiledes.assigned_tex_meta.width
            self.transformed_V = ((self.v / 64.0) + tiledes.T_shift + 0.5) / tiledes.assigned_tex_meta.height
        
        # ATTENTION !! I'm flipping the V coord here because images are stored upside down
        # but I'm exporting them right-side up
        self.transformed_V = -1 * self.transformed_V

    # NOTE: this ignores the tile descriptors for now... so no shifting yet (maybe unnecessary?)
    def reverse_UV_transforms(self, w_factor, h_factor):
        # ATTENTION !! I'm undoing the flipping here again
        self.u = np.int16(64.0 * ((+1 * self.transformed_U * w_factor) - 0.5))
        self.v = np.int16(64.0 * ((-1 * self.transformed_V * h_factor) - 0.5))

    def get_bytes(self):
        bytes_array = bytearray(16)
        # XYZ Coords
        bytes_array[0:2] = self.x.tobytes()
        bytes_array[2:4] = self.y.tobytes()
        bytes_array[4:6] = self.z.tobytes()
        # Padding
        bytes_array[6:8] = (0).to_bytes(2, byteorder='little')
        # UVs
        bytes_array[8:10] = self.u.tobytes()
        bytes_array[10:12] = self.v.tobytes()
        # RGBA V-Shades
        bytes_array[12] = self.r
        bytes_array[13] = self.g
        bytes_array[14] = self.b
        bytes_array[15] = self.a
        
        return bytes(bytes_array)

    # default func that's used for comparisons
    def __eq__(self, other):
        if not isinstance(other, Vtx_Elem):
            return False
        if self.x != other.x:
            return False
        if self.y != other.y:
            return False
        if self.z != other.z:
            return False
        return True

    def clone(self):
        return self.__class__(self)

    # default func that's used for self-representation, ie. print(vtx)
    def __repr__(self):
        return f"Vtx_Elem(x={self.x}, y={self.y}, z={self.z}, r={self.r}, g={self.g}, b={self.b}, a={self.a})"

    def print(self):
        return f"{self.x}, {self.y}, {self.z}"
        