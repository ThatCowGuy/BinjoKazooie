
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
            tri = ModelBIN_TriElem()
            tri.build_from_binary_data(file_data, file_offset_tri)
            self.tri_list.append(tri)
            if (tri not in self.unique_tri_list):
                self.unique_tri_list.append(tri)
                self.unique_tri_cnt += 1

        print(f"parsed {self.tri_cnt} collision tris within {self.geo_cube_cnt} cubes.")
        print(f"{self.unique_tri_cnt} ({(100.0 * self.unique_tri_cnt / self.tri_cnt):.2f}%) of those tris are unique.")
        self.valid = True
        return

    # link the VTX objects in the given VTX-list to the TRI objects in our tri-list via their indices
    def link_vertex_objects_for_all_tris(self, vtx_list):
        if (self.valid == False):
            print("Cannot link vertices - No Collision Segment")
            return
        # I'm deliberately NOT using the func within ModelBIN_TriElem to avoid passing the list around so much
        for tri in self.tri_list:
            tri.vtx_1 = vtx_list[tri.index_1]
            tri.vtx_2 = vtx_list[tri.index_2]
            tri.vtx_3 = vtx_list[tri.index_3]




class ModelBIN_GeoCubeElem:
    SIZE = 0x04

    def __init__(self, file_data, file_offset):
        # parsing properties
        self.starting_tri_ID    = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2)
        self.tri_cnt            = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2)
        return




class ModelBIN_TriElem:
    # NOTE: this is strictly the size of a tri in the binary collision segment !
    SIZE = 0x0C

    # NOTE: the existance of coll_type determines if this tri is collidable;
    #       the existance of tex_id determines if this tri is visible
    def __init__(self):
        self.vtx_1 = None
        self.vtx_2 = None
        self.vtx_3 = None
        
    def build_from_binary_data(self, file_data, file_offset):
        # parsing properties
        self.index_1        = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2)
        self.index_2        = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2)
        self.index_3        = binjo_utils.read_bytes(file_data, file_offset + 0x04, 2)
        self.unk_1          = binjo_utils.read_bytes(file_data, file_offset + 0x06, 2)
        self.collision_type = binjo_utils.read_bytes(file_data, file_offset + 0x08, 4)
        self.tex_idx        = None
        self.visible        = False
        return

    def build_from_parameters(self, idx1, idx2, idx3, coll_type=None, tex_id=None):
        # parsing properties
        self.index_1        = idx1
        self.index_2        = idx2
        self.index_3        = idx3
        self.unk_1          = 0x00
        self.collision_type = coll_type
        self.tex_idx        = tex_id
        self.visible        = (tex_id != None)
        return

    # link the VTX objects in the given VTX-list to the TRI objects in our tri-list via their indices
    def link_vertex_objects(self, vtx_list):
        self.vtx_1 = vtx_list[self.index_1]
        self.vtx_2 = vtx_list[self.index_2]
        self.vtx_3 = vtx_list[self.index_3]

    def compare_only_indices(self, other):
        if not isinstance(other, ModelBIN_TriElem):
            return False
        # checking all cyclic permutations because they are essentially identical
        # non-cyclic permutations have a different normal-vector though !
        if (
            self.index_1 == other.index_1 and \
            self.index_2 == other.index_2 and \
            self.index_3 == other.index_3
        ):
            return True
        if (
            self.index_1 == other.index_2 and \
            self.index_2 == other.index_3 and \
            self.index_3 == other.index_1
        ):
            return True
        if (
            self.index_1 == other.index_3 and \
            self.index_2 == other.index_1 and \
            self.index_3 == other.index_2
        ):
            return True
        return False

    # built-in equals() method; used to evaluate (A == B) expressions
    def __eq__(self, other):
        if not isinstance(other, ModelBIN_TriElem):
            return False
        if self.collision_type != other.collision_type:
            return False
        if self.tex_idx != other.tex_idx:
            return False
        # and finally compare the indices
        return (self.compare_only_indices(other))

    # built-in less_then() method; used to evaluate (A < B) expressions
    def __lt__(self, other):
        if (self.tex_idx > other.tex_idx):
            return False
        if (self.collision_type > other.collision_type):
            return False
        return True