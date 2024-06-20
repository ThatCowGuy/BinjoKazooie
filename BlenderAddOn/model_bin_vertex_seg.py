
import BinjoUtils

class ModelBIN_VtxSeg:
    HEADER_SIZE = 0x18

    # it's not guaranteed that there is a proper VTX count inside this segment,
    # so pass over the one from the BIN Header segment instead
    def __init__(self, file_data, file_offset, vtx_cnt=0):
        if file_offset == 0:
            print("No Vertex Segment")
            self.valid = False
            return

        self.file_offset = file_offset
        self.file_offset_data = file_offset + ModelBIN_VtxSeg.HEADER_SIZE
        # parsing properties
        self.min_x =        BinjoUtils.read_bytes(file_data, file_offset + 0x00, 2, type="signed")
        self.min_y =        BinjoUtils.read_bytes(file_data, file_offset + 0x02, 2, type="signed")
        self.min_z =        BinjoUtils.read_bytes(file_data, file_offset + 0x04, 2, type="signed")
        self.max_x =        BinjoUtils.read_bytes(file_data, file_offset + 0x06, 2, type="signed")
        self.max_y =        BinjoUtils.read_bytes(file_data, file_offset + 0x08, 2, type="signed")
        self.max_z =        BinjoUtils.read_bytes(file_data, file_offset + 0x0A, 2, type="signed")
        self.center_x =     BinjoUtils.read_bytes(file_data, file_offset + 0x0C, 2, type="signed")
        self.center_y =     BinjoUtils.read_bytes(file_data, file_offset + 0x0E, 2, type="signed")
        self.center_z =     BinjoUtils.read_bytes(file_data, file_offset + 0x10, 2, type="signed")
        self.local_norm =   BinjoUtils.read_bytes(file_data, file_offset + 0x12, 2, type="signed")
        self.vtx_cnt =      BinjoUtils.read_bytes(file_data, file_offset + 0x14, 2)
        self.global_norm =  BinjoUtils.read_bytes(file_data, file_offset + 0x16, 2, type="signed")

        # calculated properties
        if (self.vtx_cnt == 0):
            self.vtx_cnt = vtx_cnt

        self.vtx_list = []
        for idx in range(0, self.vtx_cnt):
            file_offset_vtx = self.file_offset_data + (idx * ModelBIN_VtxElem.SIZE)
            vtx = ModelBIN_VtxElem.create_from_data(file_data, file_offset_vtx)
            self.vtx_list.append(vtx)

        print(f"parsed {self.vtx_cnt} vertices.")
        self.valid = True
        return




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

    def create_from_data(file_data, file_offset):
        vtx = ModelBIN_VtxElem()
        vtx.x = BinjoUtils.read_bytes(file_data, file_offset + 0x00, 2, type="signed")
        vtx.y = BinjoUtils.read_bytes(file_data, file_offset + 0x02, 2, type="signed")
        vtx.z = BinjoUtils.read_bytes(file_data, file_offset + 0x04, 2, type="signed")
        vtx.u = BinjoUtils.read_bytes(file_data, file_offset + 0x08, 2, type="signed")
        vtx.v = BinjoUtils.read_bytes(file_data, file_offset + 0x0A, 2, type="signed")
        vtx.r = BinjoUtils.read_bytes(file_data, file_offset + 0x0C, 1)
        vtx.g = BinjoUtils.read_bytes(file_data, file_offset + 0x0D, 1)
        vtx.b = BinjoUtils.read_bytes(file_data, file_offset + 0x0E, 1)
        vtx.a = BinjoUtils.read_bytes(file_data, file_offset + 0x0F, 1)
        # print(f"v {vtx.x:+5d}, {vtx.y:+5d}, {vtx.z:+5d}")
        return vtx

    # translate BKs uint8 UV coords into Blenders float [0.0 - 1.0] UV coords
    def calc_transformed_UVs(self, tile_descriptor):
        if (tile_descriptor is not None):
            self.transformed_U = ((self.u / 64.0) + tile_descriptor.S_shift + 0.5) / tile_descriptor.assigned_tex_meta.width
            self.transformed_V = ((self.v / 64.0) + tile_descriptor.T_shift + 0.5) / tile_descriptor.assigned_tex_meta.height
        else:
            self.transformed_U = ((self.u / 64.0) + 0 + 0.5) / 32.0
            self.transformed_V = ((self.v / 64.0) + 0 + 0.5) / 32.0
        # Note: Flipping the V coordinate
        self.transformed_V = -1 * self.transformed_V

    def reverse_UV_transforms(self, w_factor, h_factor):
        # Note: undoing the flipping
        self.u = int(64.0 * ((+1 * self.transformed_U * w_factor) - 0.5))
        self.v = int(64.0 * ((-1 * self.transformed_V * h_factor) - 0.5))

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



