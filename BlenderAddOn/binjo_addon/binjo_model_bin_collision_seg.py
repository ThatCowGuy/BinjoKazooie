
import numpy as np
import re

from . import binjo_utils
from . binjo_dicts import Dicts

class ModelBIN_ColSeg:
    HEADER_SIZE = 0x18

    def __init__(self):
        self.valid = False

    def populate_from_collision_tri_list(self, tri_list, cube_scale=1000):
        x_coords = []
        y_coords = []
        z_coords = []
        for tri in tri_list:
            x_coords.extend([tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x])
            y_coords.extend([tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y])
            z_coords.extend([tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z])

        # using floor() for both min and max, because I consider the cubes to start in the lower-left corner
        self.geo_cube_scale = cube_scale
        self.min_geo_cube_x = int(np.floor(np.min(x_coords) / cube_scale))
        self.min_geo_cube_y = int(np.floor(np.min(y_coords) / cube_scale))
        self.min_geo_cube_z = int(np.floor(np.min(z_coords) / cube_scale))
        self.max_geo_cube_x = int(np.floor(np.max(x_coords) / cube_scale))
        self.max_geo_cube_y = int(np.floor(np.max(y_coords) / cube_scale))
        self.max_geo_cube_z = int(np.floor(np.max(z_coords) / cube_scale))
        # the strides determine how many indices we need to jump if we jump along another axis than the x axis
        x_count = (self.max_geo_cube_x - self.min_geo_cube_x + 1)
        y_count = (self.max_geo_cube_y - self.min_geo_cube_y + 1)
        z_count = (self.max_geo_cube_z - self.min_geo_cube_z + 1)
        self.stride_y = x_count
        self.stride_z = (x_count * y_count)
        # trivial
        self.geo_cube_cnt = (x_count * y_count * z_count)
        
        self.geo_cube_list = []
        # initiating the cubes in z-y-x order, so that x has the lowest stride (of 1)
        for z_id in range(self.min_geo_cube_z, self.max_geo_cube_z + 1):
            for y_id in range(self.min_geo_cube_y, self.max_geo_cube_y + 1):
                for x_id in range(self.min_geo_cube_x, self.max_geo_cube_x + 1):
                    self.geo_cube_list.append(ModelBIN_GeoCubeElem(x_id=x_id, y_id=y_id, z_id=z_id, cube_scale=cube_scale))

        # now comes the hard part: sort EVERY collision tri into EVERY intersected geocube...
        self.tri_cnt = 0
        for tri in tri_list:
            # find the bounding cube IDs for the tri; using these to drastically limit the search space
            # of possible containing cube candidates in the next step
            x_coords = [tri.vtx_1.x, tri.vtx_2.x, tri.vtx_3.x]
            y_coords = [tri.vtx_1.y, tri.vtx_2.y, tri.vtx_3.y]
            z_coords = [tri.vtx_1.z, tri.vtx_2.z, tri.vtx_3.z]
            tri_min_geo_cube_x = int(np.floor(np.min(x_coords) / cube_scale))
            tri_min_geo_cube_y = int(np.floor(np.min(y_coords) / cube_scale))
            tri_min_geo_cube_z = int(np.floor(np.min(z_coords) / cube_scale))
            tri_max_geo_cube_x = int(np.floor(np.max(x_coords) / cube_scale))
            tri_max_geo_cube_y = int(np.floor(np.max(y_coords) / cube_scale))
            tri_max_geo_cube_z = int(np.floor(np.max(z_coords) / cube_scale))
            
            for z_id in range(tri_min_geo_cube_z, tri_max_geo_cube_z + 1):
                enum_z_id = (z_id - self.min_geo_cube_z)

                for y_id in range(tri_min_geo_cube_y, tri_max_geo_cube_y + 1):
                    enum_y_id = (y_id - self.min_geo_cube_y)

                    for x_id in range(tri_min_geo_cube_x, tri_max_geo_cube_x + 1):
                        enum_x_id = (x_id - self.min_geo_cube_x)
                        cube_id = (enum_x_id + (enum_y_id * self.stride_y) + (enum_z_id * self.stride_z))

                        if (binjo_utils.tri_intersects_cube(tri, self.geo_cube_list[cube_id]) == True):
                            self.geo_cube_list[cube_id].intersecting_tri_list.append(tri)
                            self.tri_cnt += 1

        # now all the tris are signed in into their respective lists;
        # next, write all the tris into a long list (with duplicates) to index into, and set the starting indices
        self.tri_list = []
        listed_tris = 0
        for cube in self.geo_cube_list:
            # set starting ID to current count
            cube.starting_tri_ID = listed_tris
            cube.tri_cnt = len(cube.intersecting_tri_list)

            for tri in cube.intersecting_tri_list:
                self.tri_list.append(tri)
                listed_tris += 1

        if (self.tri_cnt != listed_tris):
            print(f"Something went wrong within ColSeg() building; listed_tri_cnt != assigned tri_cnt")
            self.valid = False
            return
        self.valid = True
        return


    def populate_from_data(self, file_data, file_offset):
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
            cube = ModelBIN_GeoCubeElem()
            cube.populate_from_data(file_data, file_offset_geo_cube)
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
        unique_percentage = (100.0 * self.unique_tri_cnt / self.tri_cnt)
        print(f"{self.unique_tri_cnt} ({unique_percentage:.2f}%) of those tris were unique;")
        print(f" ==> Blender Model tri-cnt reduced by {100.0 - unique_percentage:.2f}%")
        self.valid = True
        return

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.min_geo_cube_x, 2)
        output += binjo_utils.int_to_bytes(self.min_geo_cube_y, 2)
        output += binjo_utils.int_to_bytes(self.min_geo_cube_z, 2)
        output += binjo_utils.int_to_bytes(self.max_geo_cube_x, 2)
        output += binjo_utils.int_to_bytes(self.max_geo_cube_y, 2)
        output += binjo_utils.int_to_bytes(self.max_geo_cube_z, 2)
        output += binjo_utils.int_to_bytes(self.stride_y, 2)
        output += binjo_utils.int_to_bytes(self.stride_z, 2)
        output += binjo_utils.int_to_bytes(self.geo_cube_cnt, 2)
        output += binjo_utils.int_to_bytes(self.geo_cube_scale, 2)
        output += binjo_utils.int_to_bytes(self.tri_cnt, 2)
        output += binjo_utils.int_to_bytes(0x0000, 2)
        for cube in self.geo_cube_list:
            output += cube.get_bytes()
        for tri in self.tri_list:
            output += tri.get_bytes()
        return output

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

    def get_colltype_from_mat_name(mat_name):
        if ("NOCOLL" in mat_name):
            return None
        # try getting a regex match from the material name (should be an attribute later)
        match = re.search(rf".*_.*(0x[0-9,A-F]+)", mat_name)
        if (match == None):
            print(f"Couldn't parse coll_type from Material {mat_name}")
            return None
        # group(1) is actually the first group, because group(0) is reserved for the full-match...
        return int(match.group(1), 0x10)

    def get_SFX_from_mat_name(mat_name):
        coll_type = ModelBIN_ColSeg.get_colltype_from_mat_name(mat_name)
        if (coll_type is None):
            return 0

        SFX_val = binjo_utils.apply_bitmask(coll_type, 0b00000000_00000000_00001111_00000000)
        return SFX_val


    def get_colltype_from_mat(mat):
        if (mat["Collision_Disabled"] == True):
            return None
            
        coll_type = 0x0000_0000

        # add all the set flags to the coll_type
        for key in mat["Collision_Flags"].keys():
            if (key == "SFX Value"):
                print(mat["Collision_Flags"][key])
                continue
            if (mat["Collision_Flags"][key] == True):
                coll_type += Dicts.COLLISION_FLAGS[key]
        # as well as the SFX value
        coll_type += (mat["Collision_SFX"] << 8)
        return coll_type

    def get_collision_flag_dict(initial_value=0x0000_0000):
        coll_dict = {}
        if (initial_value is None):
            initial_value = 0x0000_0000
        for key in Dicts.COLLISION_FLAGS.keys():
            coll_dict[key] = bool(initial_value & Dicts.COLLISION_FLAGS[key])
        return coll_dict


class ModelBIN_GeoCubeElem:
    SIZE = 0x04

    def __init__(self, x_id=0, y_id=0, z_id=0, cube_scale=1000):
        self.scale = cube_scale
        self.center_x = cube_scale * (x_id + 0.5)
        self.center_y = cube_scale * (y_id + 0.5)
        self.center_z = cube_scale * (z_id + 0.5)
        self.center = np.array([self.center_x, self.center_y, self.center_z])
        self.intersecting_tri_list = []

    def populate_from_data(self, file_data, file_offset):
        # parsing properties
        self.starting_tri_ID    = binjo_utils.read_bytes(file_data, file_offset + 0x00, 2)
        self.tri_cnt            = binjo_utils.read_bytes(file_data, file_offset + 0x02, 2)
        return

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.starting_tri_ID, 2)
        output += binjo_utils.int_to_bytes(self.tri_cnt, 2)
        return output




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

    def get_bytes(self):
        output = bytearray()
        output += binjo_utils.int_to_bytes(self.index_1, 2)
        output += binjo_utils.int_to_bytes(self.index_2, 2)
        output += binjo_utils.int_to_bytes(self.index_3, 2)
        output += binjo_utils.int_to_bytes(0x0000, 2)
        output += binjo_utils.int_to_bytes(self.collision_type, 4)
        return output

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