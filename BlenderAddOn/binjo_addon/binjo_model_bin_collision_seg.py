
from . import binjo_utils

class ModelBIN_ColSeg:
    HEADER_SIZE = 0x18

    # it's not guaranteed that there is a proper VTX count inside this segment,
    # so pass over the one from the BIN Header segment instead
    def __init__(self, file_data, file_offset):
        if file_offset == 0:
            print("No Collision Segment")
            self.valid = False
            return

        # parsing properties
        self.min_geo_cube_x = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2, type="signed")
        self.min_geo_cube_y = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2, type="signed")
        self.min_geo_cube_z = binjo_utils.read_bytes(file_data, file_offset + 0x04, 2, type="signed")
        self.max_geo_cube_x = binjo_utils.read_bytes(file_data, file_offset + 0x06, 2, type="signed")
        self.max_geo_cube_y = binjo_utils.read_bytes(file_data, file_offset + 0x08, 2, type="signed")
        self.max_geo_cube_z = binjo_utils.read_bytes(file_data, file_offset + 0x0A, 2, type="signed")
        self.stride_y       = binjo_utils.read_bytes(file_data, file_offset + 0x0C, 2)
        self.stride_z       = binjo_utils.read_bytes(file_data, file_offset + 0x0E, 2)
        self.geo_cube_cnt   = binjo_utils.read_bytes(file_data, file_offset + 0x10, 2)
        self.geo_cube_scale = binjo_utils.read_bytes(file_data, file_offset + 0x12, 2)
        self.tri_cnt        = binjo_utils.read_bytes(file_data, file_offset + 0x14, 2)
        self.unk_1          = binjo_utils.read_bytes(file_data, file_offset + 0x16, 2)

        # calculated properties
        self.unique_tri_cnt = 0
    
        self.file_offset        = file_offset
        self.file_offset_cubes  = file_offset + ModelBIN_ColSeg.HEADER_SIZE
        self.file_offset_tris   = file_offset + ModelBIN_ColSeg.HEADER_SIZE + (self.geo_cube_cnt * ModelBIN_GeoCubeElem.SIZE)

        self.geo_cube_list = []
        for idx in range(0, self.geo_cube_cnt):
            file_offset_geo_cube = self.file_offset_cubes + (idx * ModelBIN_GeoCubeElem.SIZE)
            cube = ModelBIN_GeoCubeElem(file_data, file_offset_geo_cube)
            self.geo_cube_list.append(cube)

        self.tri_list = []
        self.unique_tri_list = []
        for idx in range(0, self.tri_cnt):
            file_offset_tri = self.file_offset_tris + (idx * ModelBIN_TriElem.SIZE)
            tri = ModelBIN_TriElem(file_data, file_offset_tri)
            self.tri_list.append(tri)
            if (tri not in self.unique_tri_list):
                self.unique_tri_list.append(tri)
                self.unique_tri_cnt += 1

        print(f"parsed {self.tri_cnt} collision tris within {self.geo_cube_cnt} cubes.")
        print(f"{self.unique_tri_cnt} ({(100.0 * self.unique_tri_cnt / self.tri_cnt):.2f}%) of those tris are unique.")
        self.valid = True
        return




class ModelBIN_GeoCubeElem:
    SIZE = 0x04

    def __init__(self, file_data, file_offset):
        # parsing properties
        self.starting_tri_ID    = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2)
        self.tri_cnt            = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2)
        return




class ModelBIN_TriElem:
    SIZE = 0x0C
    
    def __init__(self, file_data, file_offset):
        # parsing properties
        self.index_1        = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2)
        self.index_2        = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2)
        self.index_3        = binjo_utils.read_bytes(file_data, file_offset + 0x04, 2)
        self.unk_1          = binjo_utils.read_bytes(file_data, file_offset + 0x06, 2)
        self.collision_type = binjo_utils.read_bytes(file_data, file_offset + 0x08, 4)
        # print(f"f {self.index_1}-{self.index_2}-{self.index_3}")
        return

    def __eq__(self, other):
        if not isinstance(other, ModelBIN_TriElem):
            return False
        return (
            self.index_1 == other.index_1 and \
            self.index_2 == other.index_2 and \
            self.index_3 == other.index_3 and \
            self.collision_type == other.collision_type
        )