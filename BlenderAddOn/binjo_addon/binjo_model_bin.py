
from . import binjo_utils
from . binjo_model_bin_header import ModelBIN_Header
from . binjo_model_bin_texture_seg import ModelBIN_TexSeg
from . binjo_model_bin_vertex_seg import ModelBIN_VtxSeg, ModelBIN_VtxElem
from . binjo_model_bin_collision_seg import ModelBIN_ColSeg, ModelBIN_TriElem
from . binjo_model_bin_displaylist_seg import ModelBIN_DLSeg, TileDescriptor
from . binjo_model_bin_geolayout_seg import ModelBIN_GeoSeg, ModelBIN_GeoCommandChain

class ModelBIN:
    # Header                done
    # Texture               done
    # Vertex                done
    # Bone
    # Collision             done
    # DisplayList           wip
    # Effects
    # FX_END
    # Animated Textures
    # GeoLayout

    def __init__(self):
        self.Header = ModelBIN_Header()
        self.TexSeg = ModelBIN_TexSeg()
        self.VtxSeg = ModelBIN_VtxSeg()
        # Bone
        self.ColSeg = ModelBIN_ColSeg()
        self.DLSeg  = ModelBIN_DLSeg()
        # FX
        # FX_END
        # AnimTex
        self.GeoSeg = ModelBIN_GeoSeg()

    def populate_from_data(self, bin_data):
        self.Header = ModelBIN_Header(bin_data)
        self.TexSeg.populate_from_data(bin_data, self.Header.tex_offset)
        self.VtxSeg.populate_from_data(bin_data, self.Header.vtx_offset, bin_header_vtx_cnt=self.Header.vtx_cnt)
        # Bone
        self.ColSeg.populate_from_data(bin_data, self.Header.coll_offset)
        self.ColSeg.link_vertex_objects_for_all_tris(self.VtxSeg.vtx_list)
        self.DLSeg.populate_from_data(bin_data, self.Header.DL_offset)
        # FX
        # FX_END
        # AnimTex
        # Geo ---- NOTE: Im currently ignoring this when building from ROM data

        self.build_complete_tri_list()

    def export_to_BIN(self, filename="default.bin"):
        output = bytearray()
        current_filesize = 0

        # write the incomplete Header (offsets are missing)
        # to determine the offsets during export
        if (self.Header.valid == True):
            output += self.Header.get_bytes()
            current_filesize = len(output)

        if (self.TexSeg.valid == True):
            self.TexSeg.file_offset = current_filesize
            self.Header.tex_offset = self.TexSeg.file_offset
            output += self.TexSeg.get_bytes()
            current_filesize = len(output)

        if (self.VtxSeg.valid == True):
            self.VtxSeg.file_offset = current_filesize
            self.Header.vtx_offset = self.VtxSeg.file_offset
            self.Header.vtx_cnt = self.VtxSeg.vtx_cnt
            output += self.VtxSeg.get_bytes()
            current_filesize = len(output)

        # Bone

        if (self.ColSeg.valid == True):
            self.ColSeg.file_offset = current_filesize
            self.Header.coll_offset = self.ColSeg.file_offset
            output += self.ColSeg.get_bytes()
            current_filesize = len(output)

        if (self.DLSeg.valid == True):
            self.DLSeg.file_offset = current_filesize
            self.Header.DL_offset = self.DLSeg.file_offset
            # self.Header.tri_cnt = self.DLSeg.DL_tri_cnt # might be neccessary at some point
            output += self.DLSeg.get_bytes()
            current_filesize = len(output)
            
        # FX
        # FX_END
        # AnimTex

        if (self.GeoSeg.valid == True):
            self.GeoSeg.file_offset = current_filesize
            self.Header.geo_offset = self.GeoSeg.file_offset
            output += self.GeoSeg.get_bytes()
            current_filesize = len(output)

        # I need to overwrite the incomplete Header
        for (idx, byte) in enumerate(self.Header.get_bytes()):
            output[idx] = byte

        with open(filename, "wb") as output_file:
            output_file.write(output)
    
    # combine the data from the Collision and DL Segments into one comprehensive list
    def build_complete_tri_list(self, TexSeg=None, ColSeg=None, DLSeg=None):
        if (TexSeg == None):
            TexSeg = self.TexSeg
        if (ColSeg == None):
            ColSeg = self.ColSeg
        if (DLSeg == None):
            DLSeg = self.DLSeg
        
        if (ColSeg.valid == True):
            # start of by grabbing all the tris from the coll segment
            self.complete_tri_list = ColSeg.unique_tri_list.copy()
        else:
            self.complete_tri_list = []

        if (DLSeg.valid == True):
            # then walk through the DLs with a TileDescriptor and a simulated VTX-Buffer to scan for visual tris;
            # the descriptor holds meta data for the GPU and handles the VTX-Buffer, which has a capacity of 32 tri-IDs
            descriptor_array = []
            for idx in range(0, 10):
                descriptor_array.append(TileDescriptor())
            active_descriptor = 0
            vertex_buffer = [0] * 0x20
            for cmd in DLSeg.command_list:

                if (cmd.command_name == "G_TEXTURE"):
                    active_descriptor = cmd.parameters[1]
                    continue

                if (cmd.command_name == "G_SETTIMG"):
                    # find the tex that corresponds to this address (and only update the descriptor if it was actual data)
                    potential_tex_idx = TexSeg.get_tex_ID_from_datasection_offset(cmd.parameters[3])
                    if (potential_tex_idx != -1):
                        descriptor_array[active_descriptor].tex_idx = potential_tex_idx
                        descriptor_array[active_descriptor].tex_width  = TexSeg.tex_elements[descriptor_array[active_descriptor].tex_idx].width
                        descriptor_array[active_descriptor].tex_height = TexSeg.tex_elements[descriptor_array[active_descriptor].tex_idx].height
                    else:
                        pass
                        # descriptor_array[active_descriptor].tex_idx = None
                        # descriptor_array[active_descriptor].tex_width  = 0
                        # descriptor_array[active_descriptor].tex_height = 0
                    continue
                
                if (cmd.command_name == "G_VTX"):
                    first_vtx_idx = (cmd.parameters[4] // 0x10)
                    vtx_load_cnt = cmd.parameters[1]
                    # write the corresponding vtx into the simulated buffer
                    buffer_offset = cmd.parameters[0]
                    for idx in range(0, vtx_load_cnt):
                        vertex_buffer[buffer_offset + idx] = (first_vtx_idx + idx)
                    continue

                if (cmd.command_name == "G_TRI1"):
                    tmp_tri = ModelBIN_TriElem()
                    tmp_tri.build_from_parameters(
                        vertex_buffer[cmd.parameters[0]],
                        vertex_buffer[cmd.parameters[1]],
                        vertex_buffer[cmd.parameters[2]]
                    )
                    self.add_and_transform_tri(tmp_tri, descriptor_array[active_descriptor])
                    continue

                if (cmd.command_name == "G_TRI2"):
                    tmp_tri = ModelBIN_TriElem()
                    tmp_tri.build_from_parameters(
                        vertex_buffer[cmd.parameters[0]],
                        vertex_buffer[cmd.parameters[1]],
                        vertex_buffer[cmd.parameters[2]]
                    )
                    self.add_and_transform_tri(tmp_tri, descriptor_array[active_descriptor])
                    tmp_tri = ModelBIN_TriElem()
                    tmp_tri.build_from_parameters(
                        vertex_buffer[cmd.parameters[3]],
                        vertex_buffer[cmd.parameters[4]],
                        vertex_buffer[cmd.parameters[5]]
                    )
                    self.add_and_transform_tri(tmp_tri, descriptor_array[active_descriptor])
                    continue

    # this func needs the entire existing-tri list aswell as the vtx-seg, so its in the collection class
    def add_and_transform_tri(self, new_tri, tile_descriptor):
        # first, check if the tri already exists in our list
        matching_tri_index = -1
        for idx, existing_tri in enumerate(self.complete_tri_list):
            if (existing_tri.compare_only_indices(new_tri) == True):
                matching_tri_index = idx
                new_tri = existing_tri
                break
        # if the tri wasnt already added, its vertex objects wont be linked yet
        if (matching_tri_index == -1):
            new_tri.link_vertex_objects(self.VtxSeg.vtx_list)
        # this is ALWAYS true if the tri was found in the DLs; Textured or not
        new_tri.visible = True
        # finally, link the tex ID and calculate the Blender-UVs with the help of the descriptor
        new_tri.tex_idx = tile_descriptor.tex_idx
        new_tri.vtx_1.calc_transformed_UVs(tile_descriptor)
        new_tri.vtx_2.calc_transformed_UVs(tile_descriptor)
        new_tri.vtx_3.calc_transformed_UVs(tile_descriptor)
        # and add the new tri if it wasnt added before
        if (matching_tri_index == -1):
            self.complete_tri_list.append(new_tri)



    # arrange the model data into lists that Blender can convert into a mesh
    def arrange_mesh_data(self):
        self.vertex_coord_list = []
        self.vertex_shade_list = []
        scale_factor = 1.0
        for vtx in self.VtxSeg.vtx_list:
            # NOTE: Im swapping Y and Z in here, and flipping Z afterwards,
            #       because BK uses a different coord system than blender
            self.vertex_coord_list.append((
                + vtx.x * scale_factor,
                - vtx.z * scale_factor, # swapped and flipped
                + vtx.y * scale_factor  # swapped
            ))

        self.face_idx_list = []
        self.edge_idx_list = []
        self.mat_list = []
        for tri in self.complete_tri_list:
            self.face_idx_list.append((tri.index_1, tri.index_2, tri.index_3))
            self.edge_idx_list.append((tri.index_1, tri.index_2))
            self.edge_idx_list.append((tri.index_2, tri.index_3))
            self.edge_idx_list.append((tri.index_3, tri.index_1))

            if (tri.visible == False):
                img_alias = "INVIS"
            if (tri.visible == True and tri.tex_idx == None):
                img_alias = "FLAT"
            if (tri.visible == True and tri.tex_idx != None):
                datasection_offset_data = self.TexSeg.tex_elements[tri.tex_idx].datasection_offset_data
                img_alias = f"{binjo_utils.to_decal_hex(datasection_offset_data, 4)}"

            coll_encoding = "NOCOLL"
            if (tri.collision_type != None):
                coll_encoding = f"{binjo_utils.to_decal_hex(tri.collision_type, 4)}"

            mat = BinjoMaterial(img_alias, coll_encoding)
            
            if mat not in self.mat_list:
                mat.link_image_object(self.TexSeg)
                self.mat_list.append(mat)
            tri.mat_index = self.mat_list.index(mat)
        return

class BinjoMaterial:
    def __init__(self, img_alias, coll_encoding):
        self.img_alias = img_alias
        self.coll_encoding = coll_encoding
        self.name = f"{img_alias}_{coll_encoding}"
    
    def link_image_object(self, TexSeg):
        if (self.img_alias == "INVIS"):
            self.Blender_IMG = None
            return
        if (self.img_alias == "FLAT"):
            self.Blender_IMG = None
            return
        tex_id = TexSeg.get_tex_ID_from_datasection_offset(int(self.img_alias, base=16))
        self.Blender_IMG = TexSeg.tex_elements[tex_id].Blender_IMG

    def __eq__(self, other):
        return (self.name == other.name)
