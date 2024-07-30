
from . import binjo_utils

import numpy as np

class ModelBIN_VtxSeg:
    HEADER_SIZE = 0x18

    # it's not guaranteed that there is a proper VTX count inside this segment,
    # so pass over the one from the BIN Header segment instead
    def __init__(self):
        self.valid = False
        
    # it's not guaranteed that there is a proper VTX count inside this segment,
    # so pass over the one from the BIN Header segment instead
    def populate_from_data(self, file_data, file_offset, bin_header_vtx_cnt=0):
        if file_offset == 0:
            print("No Vertex Segment")
            self.valid = False
            return

        self.file_offset = file_offset
        self.file_offset_data = file_offset + ModelBIN_VtxSeg.HEADER_SIZE
        # parsing properties
        self.min_x =        binjo_utils.read_bytes(file_data, file_offset + 0x00, 2, type="signed")
        self.min_y =        binjo_utils.read_bytes(file_data, file_offset + 0x02, 2, type="signed")
        self.min_z =        binjo_utils.read_bytes(file_data, file_offset + 0x04, 2, type="signed")
        self.max_x =        binjo_utils.read_bytes(file_data, file_offset + 0x06, 2, type="signed")
        self.max_y =        binjo_utils.read_bytes(file_data, file_offset + 0x08, 2, type="signed")
        self.max_z =        binjo_utils.read_bytes(file_data, file_offset + 0x0A, 2, type="signed")
        self.center_x =     binjo_utils.read_bytes(file_data, file_offset + 0x0C, 2, type="signed")
        self.center_y =     binjo_utils.read_bytes(file_data, file_offset + 0x0E, 2, type="signed")
        self.center_z =     binjo_utils.read_bytes(file_data, file_offset + 0x10, 2, type="signed")
        self.local_norm =   binjo_utils.read_bytes(file_data, file_offset + 0x12, 2, type="signed")
        self.vtx_cnt =      binjo_utils.read_bytes(file_data, file_offset + 0x14, 2)
        self.global_norm =  binjo_utils.read_bytes(file_data, file_offset + 0x16, 2, type="signed")

        # if the vtx_cnt parsed from the VTX-Seg Header is different from the one from the BIN-Header,
        # assume this is a mistake and give a warning on the console window
        if (self.vtx_cnt != bin_header_vtx_cnt != 0):
            print(f"parsed vtx-cnt from VTX-Seg ({self.vtx_cnt}) is different from BIN-Header vtx-cnt ({bin_header_vtx_cnt}) !")
        # always assume the header count is more accurate !! (if it exists)
        if (bin_header_vtx_cnt != 0):
            self.vtx_cnt = bin_header_vtx_cnt

        self.vtx_list = []
        for idx in range(0, self.vtx_cnt):
            file_offset_vtx = self.file_offset_data + (idx * ModelBIN_VtxElem.SIZE)
            vtx = ModelBIN_VtxElem.build_from_binary_data(file_data, file_offset_vtx)
            self.vtx_list.append(vtx)

        print(f"parsed {self.vtx_cnt} vertices.")
        self.valid = True
        return

    def populate_from_vtx_list(self, vtx_list):
        # take on the supplied vtx list
        self.vtx_list = vtx_list
        self.vtx_cnt = len(vtx_list)
        # infer minmax coords and center coords from the list
        x_coords = [vtx.x for vtx in vtx_list]
        y_coords = [vtx.y for vtx in vtx_list]
        z_coords = [vtx.z for vtx in vtx_list]
        self.min_x = int(np.min(x_coords))
        self.min_y = int(np.min(y_coords))
        self.min_z = int(np.min(z_coords))
        self.max_x = int(np.max(x_coords))
        self.max_y = int(np.max(y_coords))
        self.max_z = int(np.max(z_coords))
        self.center_x = int((self.min_x + self.max_x) / 2)
        self.center_y = int((self.min_y + self.max_y) / 2)
        self.center_z = int((self.min_z + self.max_z) / 2)
        # aswell as the maximum local (distance to center) and global (distance to origin) norms
        center = ModelBIN_VtxElem()
        center.x, center.y, center.z = self.center_x, self.center_y, self.center_z
        origin = ModelBIN_VtxElem()
        origin.x, origin.y, origin.z = 0, 0, 0
        local_norms = [vtx.distance_to(center) for vtx in vtx_list]
        global_norms = [vtx.distance_to(origin) for vtx in vtx_list]
        self.local_norm = int(np.max(local_norms))
        self.global_norm = int(np.max(global_norms))
        # and donezo
        self.valid = True

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.min_x, 2)
        output += binjo_utils.int_to_bytes(self.min_y, 2)
        output += binjo_utils.int_to_bytes(self.min_z, 2)
        output += binjo_utils.int_to_bytes(self.max_x, 2)
        output += binjo_utils.int_to_bytes(self.max_y, 2)
        output += binjo_utils.int_to_bytes(self.max_z, 2)
        output += binjo_utils.int_to_bytes(self.center_x, 2)
        output += binjo_utils.int_to_bytes(self.center_y, 2)
        output += binjo_utils.int_to_bytes(self.center_z, 2)
        output += binjo_utils.int_to_bytes(self.local_norm, 2)
        output += binjo_utils.int_to_bytes(self.vtx_cnt, 2)
        output += binjo_utils.int_to_bytes(self.global_norm, 2)
        for vtx in self.vtx_list:
            output += vtx.get_bytes()
        return output





class ModelBIN_VtxElem:
    SIZE = 0x10

    def __init__(self):
        # actual Coords
        self.x = 0
        self.y = 0
        self.z = 0
        # UV Tex Coords
        self.u = 0
        self.v = 0
        # RGBA Vtx-Shading
        self.r = 0xFF
        self.g = 0xFF
        self.b = 0xFF
        self.a = 0xFF
        # intrinsics
        self.transformed_U = 0.0
        self.transformed_V = 0.0

    def build_from_binary_data(file_data, file_offset):
        vtx = ModelBIN_VtxElem()
        vtx.x = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2, type="signed")
        vtx.y = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2, type="signed")
        vtx.z = binjo_utils.read_bytes(file_data, file_offset + 0x04, 2, type="signed")
        vtx.u = binjo_utils.read_bytes(file_data, file_offset + 0x08, 2, type="signed")
        vtx.v = binjo_utils.read_bytes(file_data, file_offset + 0x0A, 2, type="signed")
        vtx.r = binjo_utils.read_bytes(file_data, file_offset + 0x0C, 1)
        vtx.g = binjo_utils.read_bytes(file_data, file_offset + 0x0D, 1)
        vtx.b = binjo_utils.read_bytes(file_data, file_offset + 0x0E, 1)
        vtx.a = binjo_utils.read_bytes(file_data, file_offset + 0x0F, 1)
        # print(f"v {vtx.x:+5d}, {vtx.y:+5d}, {vtx.z:+5d}")
        return vtx

    def build_from_model_data(x, y, z, r, g, b, a, u_transf, v_transf):
        vtx = ModelBIN_VtxElem()
        vtx.x = x
        vtx.y = y
        vtx.z = z
        vtx.r = r
        vtx.g = g
        vtx.b = b
        vtx.a = a
        vtx.transformed_U = u_transf
        vtx.transformed_V = v_transf
        return vtx

    

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.x, 2)
        output += binjo_utils.int_to_bytes(self.y, 2)
        output += binjo_utils.int_to_bytes(self.z, 2)
        output += binjo_utils.int_to_bytes(0x0000, 2)
        output += binjo_utils.int_to_bytes(self.u, 2)
        output += binjo_utils.int_to_bytes(self.v, 2)
        output += binjo_utils.int_to_bytes(self.r, 1)
        output += binjo_utils.int_to_bytes(self.g, 1)
        output += binjo_utils.int_to_bytes(self.b, 1)
        output += binjo_utils.int_to_bytes(self.a, 1)
        return output

    def distance_to(self, other):
        dx = self.x - other.x
        dy = self.y - other.y
        dz = self.z - other.z
        return np.sqrt(dx*dx + dy*dy + dz*dz)

    # translate BKs uint8 UV coords into Blenders float [0.0 - 1.0] UV coords
    def calc_transformed_UVs(self, tile_descriptor):
        if (tile_descriptor is not None and tile_descriptor.tex_idx is not None):
            self.transformed_U = ((self.u / 64.0) + tile_descriptor.S_shift + 0.5) / tile_descriptor.tex_width
            self.transformed_V = ((self.v / 64.0) + tile_descriptor.T_shift + 0.5) / tile_descriptor.tex_height
        else:
            self.transformed_U = ((self.u / 64.0) + 0 + 0.5) / 32.0
            self.transformed_V = ((self.v / 64.0) + 0 + 0.5) / 32.0

    def reverse_UV_transforms(self, w_factor, h_factor):
        self.u = int(64.0 * ((self.transformed_U * w_factor) - 0.5))
        self.v = int(64.0 * ((self.transformed_V * h_factor) - 0.5))

    def clone(self):
        return self.__class__(
            self.x, self.y, self.z,
            self.u, self.v,
            self.r, self.g, self.b, self.a,
            self.transformed_U, self.transformed_V
        )

    def __eq__(self, other):
        if not isinstance(other, ModelBIN_VtxElem):
            return False
        return (
            self.x == other.x and \
            self.y == other.y and \
            self.z == other.z
        )




