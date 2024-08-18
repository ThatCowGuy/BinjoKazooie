

import os
from timeit import default_timer as timer

from . binjo_model_bin import ModelBIN
from . binjo_bin_handler import BINjo_ModelBIN_Handler
from . binjo_dicts import Dicts

from . binjo_model_bin_vertex_seg import ModelBIN_VtxSeg, ModelBIN_VtxElem
from . binjo_model_bin_collision_seg import ModelBIN_ColSeg, ModelBIN_TriElem
from . binjo_model_bin_texture_seg import ModelBIN_TexSeg, ModelBIN_TexElem
from . binjo_model_bin_displaylist_seg import ModelBIN_DLSeg, DisplayList_Command
from . binjo_model_bin_geolayout_seg import ModelBIN_GeoSeg, ModelBIN_GeoCommandChain

import bpy
from bpy_extras.io_utils import ImportHelper
from bpy.app.handlers import persistent
# https://docs.blender.org/api/current/bpy_types_enum_items/operator_return_items.html
# https://docs.blender.org/api/current/bpy_types_enum_items/wm_report_items.html#rna-enum-wm-report-items
# http://www.network-science.de/ascii/

bl_info = {
    "name": "BINjo-Kazooie",
    "blender": (3, 4, 1),
    "category": "Object",
}
bin_handler = None
version_num = "0.1.2"



#===========================================================================================================
#    __  __            __        __           ______                     __   _                    
#   / / / /____   ____/ /____ _ / /_ ___     / ____/__  __ ____   _____ / /_ (_)____   ____   _____
#  / / / // __ \ / __  // __ `// __// _ \   / /_   / / / // __ \ / ___// __// // __ \ / __ \ / ___/
# / /_/ // /_/ // /_/ // /_/ // /_ /  __/  / __/  / /_/ // / / // /__ / /_ / // /_/ // / / /(__  ) 
# \____// .___/ \__,_/ \__,_/ \__/ \___/  /_/     \__,_//_/ /_/ \___/ \__//_/ \____//_/ /_//____/  
#      /_/  
#===========================================================================================================

def highlight_invis_changed(self, context):
    # and check for object and active-mat existence
    if (context.active_object is None):
        return
    target_object = context.active_object
    color_attribute = target_object.data.color_attributes["import_Color"]
    for face in target_object.data.polygons:
        if ("INVIS" in target_object.data.materials[face.material_index].name):
            # if the toggle is active
            if (context.scene.binjo_props.highlight_invis == True):
                # pure collision tris will be drawn in magenta
                color_attribute.data[face.loop_indices[0]].color = (1.0, 0, 1.0, 1.0)
                color_attribute.data[face.loop_indices[1]].color = (1.0, 0, 1.0, 1.0)
                color_attribute.data[face.loop_indices[2]].color = (1.0, 0, 1.0, 1.0)
            else:
                # otherwise make them gray and fully transparent
                color_attribute.data[face.loop_indices[0]].color = (0.7, 0.7, 0.7, 0.0)
                color_attribute.data[face.loop_indices[1]].color = (0.7, 0.7, 0.7, 0.0)
                color_attribute.data[face.loop_indices[2]].color = (0.7, 0.7, 0.7, 0.0)

disable_collision_update_function = False
def collision_checkboxes_changed(self, context):
    # only update the materials collision dict, if this wasnt disabled
    global disable_collision_update_function
    if (disable_collision_update_function == False):
        # and check for object and active-mat existence
        if (context.active_object is not None):
            mat = context.active_object.active_material
            if (mat is not None):
                for idx, key in enumerate(mat["Collision_Flags"].keys()):
                    mat["Collision_Flags"][key] = bool(context.scene.binjo_props.collision_checkboxes[idx])

def collision_disabled_changed(self, context):
    # only update the materials collision dict, if this wasnt disabled
    global disable_collision_update_function
    if (disable_collision_update_function == False):
        # and check for object and active-mat existence
        if (context.active_object is not None):
            mat = context.active_object.active_material
            if (mat is not None):
                mat["Collision_Disabled"] = bool(context.scene.binjo_props.collision_disabled[0])

def collision_SFX_changed(self, context):
    # only update the materials collision dict, if this wasnt disabled
    global disable_collision_update_function
    if (disable_collision_update_function == False):
        # and check for object and active-mat existence
        if (context.active_object is not None):
            mat = context.active_object.active_material
            if (mat is not None):
                mat["Collision_SFX"] = Dicts.COLLISION_SFX[context.scene.binjo_props.SFX_value_enum]


@persistent
def general_update_function(scene):
    context = bpy.context

    # this update has to be hidden, to not trigger infinite-loops
    global disable_collision_update_function
    disable_collision_update_function = True

    if (context.active_object is not None):
        mat = context.active_object.active_material
        if (mat is not None):
            # update all the flags
            for idx, key in enumerate(mat["Collision_Flags"].keys()):
                context.scene.binjo_props.collision_checkboxes[idx] = bool(mat["Collision_Flags"][key])
            # as well as the collision-enabled state
            context.scene.binjo_props.collision_disabled[0] = bool(mat["Collision_Disabled"])
            # and the collision SFX
            context.scene.binjo_props.SFX_value_enum = Dicts.COLLISION_SFX_REV[mat["Collision_SFX"]]

    disable_collision_update_function = False



#===========================================================================================================
#     ____   ____ __  __            ____                                   __   _            
#    / __ ) / __ \\ \/ /           / __ \ _____ ____   ____   ___   _____ / /_ (_)___   _____
#   / __  |/ /_/ / \  /  ______   / /_/ // ___// __ \ / __ \ / _ \ / ___// __// // _ \ / ___/
#  / /_/ // ____/  / /  /_____/  / ____// /   / /_/ // /_/ //  __// /   / /_ / //  __/(__  ) 
# /_____//_/      /_/           /_/    /_/    \____// .___/ \___//_/    \__//_/ \___//____/  
#                                                  /_/                                    
#===========================================================================================================

# Properties are data elements that show up in the GUI Panel
class BINJO_Properties(bpy.types.PropertyGroup):
    rom_path: bpy.props.StringProperty(
        name="",
        description="Path to ROM",
        default="",
        maxlen=1024,
        subtype='FILE_PATH'
    )
    export_path: bpy.props.StringProperty(
        name="",
        description="Path to Store Exports",
        default="",
        maxlen=1024,
        subtype='DIR_PATH'
    )
    model_filename: bpy.props.StringProperty(
        name="",
        description="Internal Model Filename",
        default="",
        maxlen=1024,
        subtype='NONE'
    )
    force_model_A : bpy.props.BoolProperty(
        name="Force only Model-A",
        description="Force everything to export into a singular Model-BIN.",
        default = False
    )
    highlight_invis : bpy.props.BoolProperty(
        name="Highlight INVIS Mats",
        description="Highlight all the INVIS Materials in Magenta; Those are usually Collision only.",
        default = False,
        update = highlight_invis_changed
    )
    collision_disabled : bpy.props.BoolVectorProperty(
        name="Collision Disabled",
        description="Materials with disabled collision will not be part of the Collision-Model at all; They're strictly visual-only.",
        size=1,
        default = (False,) * 1,
        update = collision_disabled_changed
    )
    collision_checkboxes : bpy.props.BoolVectorProperty(
        name="Collision Flags",
        description="Set the Collision Flags of the Selected Material.",
        size=len(Dicts.COLLISION_FLAGS.keys()),
        default = (False,) * len(Dicts.COLLISION_FLAGS.keys()),
        update = collision_checkboxes_changed
    )
    show_all_coll_flags : bpy.props.BoolProperty(
        name="Show all Coll Flags",
        description="Show ALL Collision Flags, including unknown ones and guesses.",
        default = False
    )
    model_filename_enum : bpy.props.EnumProperty(
        name="Model File Name Enum",
        description="Internal Model Filename Enum",
        default="(0x01) TTC - Treasure Trove Cove",
        items = [
            ("(0x00) Unknown 01", "(0x00) Unknown 01", ""),
            ("(0x01) TTC - Treasure Trove Cove", "(0x01) TTC - Treasure Trove Cove", ""),
            ("(0x02) TTC - Treasure Trove Cove B", "(0x02) TTC - Treasure Trove Cove B", ""),
            ("(0x03) TTC - Crab Shell A", "(0x03) TTC - Crab Shell A", ""),
            ("(0x04) TTC - Crab Shell B", "(0x04) TTC - Crab Shell B", ""),
            ("(0x05) TTC - Pirate Ship A", "(0x05) TTC - Pirate Ship A", ""),
            ("(0x06) TTC - Pirate Ship B", "(0x06) TTC - Pirate Ship B", ""),
            ("(0x07) TTC - Sand Castle A", "(0x07) TTC - Sand Castle A", ""),
            ("(0x08) TTC - Sand Castle B", "(0x08) TTC - Sand Castle B", ""),
            ("(0x09) TTC - Sharkfood Island A", "(0x09) TTC - Sharkfood Island A", ""),
            ("(0x0A) GV - Gobi's Valley A", "(0x0A) GV - Gobi's Valley A", ""),
            ("(0x0B) GV - Gobi's Valley B", "(0x0B) GV - Gobi's Valley B", ""),
            ("(0x0C) GV - Match Game", "(0x0C) GV - Match Game", ""),
            ("(0x0D) Unknown 02", "(0x0D) Unknown 02", ""),
            ("(0x0E) GV - Maze A", "(0x0E) GV - Maze A", ""),
            ("(0x0F) GV - Maze B", "(0x0F) GV - Maze B", ""),
            ("(0x10) GV - Water Pyramid A", "(0x10) GV - Water Pyramid A", ""),
            ("(0x11) GV - Water Pyramid B", "(0x11) GV - Water Pyramid B", ""),
            ("(0x12) GV - Rupee's House", "(0x12) GV - Rupee's House", ""),
            ("(0x13) GV - Inside the Sphinx", "(0x13) GV - Inside the Sphinx", ""),
            ("(0x14) GV - Blue Egg Chamber A", "(0x14) GV - Blue Egg Chamber A", ""),
            ("(0x15) MMM - Mad Monster Mansion A", "(0x15) MMM - Mad Monster Mansion A", ""),
            ("(0x16) MMM - Mad Monster Mansion B", "(0x16) MMM - Mad Monster Mansion B", ""),
            ("(0x17) MMM - Drainpipe A", "(0x17) MMM - Drainpipe A", ""),
            ("(0x18) MMM - Cellar A", "(0x18) MMM - Cellar A", ""),
            ("(0x19) MMM - Secret Church Room A", "(0x19) MMM - Secret Church Room A", ""),
            ("(0x1A) MMM - Secret Church Room B", "(0x1A) MMM - Secret Church Room B", ""),
            ("(0x1B) MMM - Dining Room A", "(0x1B) MMM - Dining Room A", ""),
            ("(0x1C) MMM - Church A", "(0x1C) MMM - Church A", ""),
            ("(0x1D) MMM - Church B", "(0x1D) MMM - Church B", ""),
            ("(0x1E) MMM - Tumbler's Shed", "(0x1E) MMM - Tumbler's Shed", ""),
            ("(0x1F) MMM - Egg Room A", "(0x1F) MMM - Egg Room A", ""),
            ("(0x20) MMM - Egg Room B", "(0x20) MMM - Egg Room B", ""),
            ("(0x21) MMM - Note Room A", "(0x21) MMM - Note Room A", ""),
            ("(0x22) MMM - Note Room B", "(0x22) MMM - Note Room B", ""),
            ("(0x23) MMM - Feather Room A", "(0x23) MMM - Feather Room A", ""),
            ("(0x24) MMM - Feather Room B", "(0x24) MMM - Feather Room B", ""),
            ("(0x25) MMM - Bathroom A", "(0x25) MMM - Bathroom A", ""),
            ("(0x26) MMM - Bathroom B", "(0x26) MMM - Bathroom B", ""),
            ("(0x27) MMM - Bedroom A", "(0x27) MMM - Bedroom A", ""),
            ("(0x28) MMM - Bedroom B", "(0x28) MMM - Bedroom B", ""),
            ("(0x29) MMM - Gold Feather Room A", "(0x29) MMM - Gold Feather Room A", ""),
            ("(0x2A) MMM - Gold Feather Room B", "(0x2A) MMM - Gold Feather Room B", ""),
            ("(0x2B) MMM - Well A", "(0x2B) MMM - Well A", ""),
            ("(0x2C) MMM - Well B", "(0x2C) MMM - Well B", ""),
            ("(0x2D) MMM - Drainpipe B", "(0x2D) MMM - Drainpipe B", ""),
            ("(0x2E) MMM - Septic Tank A", "(0x2E) MMM - Septic Tank A", ""),
            ("(0x2F) MMM - Septic Tank B", "(0x2F) MMM - Septic Tank B", ""),
            ("(0x30) MMM - Dining Room B", "(0x30) MMM - Dining Room B", ""),
            ("(0x31) MMM - Cellar B", "(0x31) MMM - Cellar B", ""),
            ("(0x32) ??? Dark Room", "(0x32) ??? Dark Room", ""),
            ("(0x33) CS - Intro A", "(0x33) CS - Intro A", ""),
            ("(0x34) CS - Ncube A", "(0x34) CS - Ncube A", ""),
            ("(0x35) CS - Grunty's Final Words A", "(0x35) CS - Grunty's Final Words A", ""),
            ("(0x36) CS - Ncube B", "(0x36) CS - Ncube B", ""),
            ("(0x37) CS - Intro B", "(0x37) CS - Intro B", ""),
            ("(0x38) CS - Banjo's House A", "(0x38) CS - Banjo's House A", ""),
            ("(0x39) CS - Grunty's Final Words B", "(0x39) CS - Grunty's Final Words B", ""),
            ("(0x3A) CS - Banjo's House B", "(0x3A) CS - Banjo's House B", ""),
            ("(0x3B) CS - Beach Ending B", "(0x3B) CS - Beach Ending B", ""),
            ("(0x3C) CS - With Falling twords Ground A", "(0x3C) CS - With Falling twords Ground A", ""),
            ("(0x3D) Unknown 03", "(0x3D) Unknown 03", ""),
            ("(0x3E) CS - Floor 5 B", "(0x3E) CS - Floor 5 B", ""),
            ("(0x3F) CS - Beach Ending A", "(0x3F) CS - Beach Ending A", ""),
            ("(0x40) MM - Mumbo's Mountain A", "(0x40) MM - Mumbo's Mountain A", ""),
            ("(0x41) MM - Mumbo's Mountain B", "(0x41) MM - Mumbo's Mountain B", ""),
            ("(0x42) MM - Termite Hill A", "(0x42) MM - Termite Hill A", ""),
            ("(0x43) MM - Termite Hill B", "(0x43) MM - Termite Hill B", ""),
            ("(0x44) Mumbo's Skull", "(0x44) Mumbo's Skull", ""),
            ("(0x45) Unknown 04", "(0x45) Unknown 04", ""),
            ("(0x46) RBB - Rusty Bucket Bay A", "(0x46) RBB - Rusty Bucket Bay A", ""),
            ("(0x47) RBB - Rusty Bucket Bay B", "(0x47) RBB - Rusty Bucket Bay B", ""),
            ("(0x48) RBB - Machine Room A", "(0x48) RBB - Machine Room A", ""),
            ("(0x49) RBB - Machine Room B", "(0x48) RBB - Machine Room B", ""),
            ("(0x4A) RBB - Big Fish Warehouse A", "(0x4A) RBB - Big Fish Warehouse A", ""),
            ("(0x4B) RBB - Big Fish Warehouse B", "(0x4B) RBB - Big Fish Warehouse B", ""),
            ("(0x4C) RBB - Boat Room A", "(0x4C) RBB - Boat Room A", ""),
            ("(0x4D) RBB - Boat Room B", "(0x4D) RBB - Boat Room B", ""),
            ("(0x4E) RBB - Container 1 A", "(0x4E) RBB - Container 1 A", ""),
            ("(0x4F) RBB - Container 2 A", "(0x4F) RBB - Container 2 A", ""),
            ("(0x50) RBB - Container 3 A", "(0x50) RBB - Container 3 A", ""),
            ("(0x51) RBB - Captain's Cabin A", "(0x51) RBB - Captain's Cabin A", ""),
            ("(0x52) RBB - Captain's Cabin B", "(0x52) RBB - Captain's Cabin B", ""),
            ("(0x53) RBB - Sea Grublin's Cabin A", "(0x53) RBB - Sea Grublin's Cabin A", ""),
            ("(0x54) RBB - Boss Boom Box Room A", "(0x54) RBB - Boss Boom Box Room A", ""),
            ("(0x55) RBB - Boss Boom Box Room B", "(0x55) RBB - Boss Boom Box Room B", ""),
            ("(0x56) RBB - Boss Boom Box Room (2) A", "(0x56) RBB - Boss Boom Box Room (2) A", ""),
            ("(0x57) RBB - Boss Boom Box Room (2) B", "(0x57) RBB - Boss Boom Box Room (2) B", ""),
            ("(0x58) RBB - Navigation Room A", "(0x58) RBB - Navigation Room A", ""),
            ("(0x59) RBB - Boom Box Room (Pipe) A", "(0x59) RBB - Boom Box Room (Pipe) A", ""),
            ("(0x5A) RBB - Boom Box Room (Pipe) B", "(0x5A) RBB - Boom Box Room (Pipe) B", ""),
            ("(0x5B) RBB - Kitchen A", "(0x5B) RBB - Kitchen A", ""),
            ("(0x5C) RBB - Kitchen B", "(0x5C) RBB - Kitchen B", ""),
            ("(0x5D) RBB - Anchor Room A", "(0x5D) RBB - Anchor Room A", ""),
            ("(0x5E) RBB - Anchor Room B", "(0x5E) RBB - Anchor Room B", ""),
            ("(0x5F) RBB - Navigation Room B", "(0x5F) RBB - Navigation Room B", ""),
            ("(0x60) FP - Freezeezy Peak A", "(0x60) FP - Freezeezy Peak A", ""),
            ("(0x61) FP - Freezeezy Peak B", "(0x61) FP - Freezeezy Peak B", ""),
            ("(0x62) FP - Igloo A", "(0x62) FP - Igloo A", ""),
            ("(0x63) FP - Christmas Tree A", "(0x63) FP - Christmas Tree A", ""),
            ("(0x64) FP - Wozza's Cave A", "(0x64) FP - Wozza's Cave A", ""),
            ("(0x65) FP - Wozza's Cave B", "(0x65) FP - Wozza's Cave B", ""),
            ("(0x66) FP - Igloo B", "(0x66) FP - Igloo B", ""),
            ("(0x67) SM - Spiral Mountain A", "(0x67) SM - Spiral Mountain A", ""),
            ("(0x68) SM - Spiral Mountain B", "(0x68) SM - Spiral Mountain B", ""),
            ("(0x69) BGS - Bubblegloop Swamp A", "(0x69) BGS - Bubblegloop Swamp A", ""),
            ("(0x6A) BGS - Bubblegloop Swamp B", "(0x6A) BGS - Bubblegloop Swamp B", ""),
            ("(0x6B) BGS - Mr. Vile A", "(0x6B) BGS - Mr. Vile A", ""),
            ("(0x6C) BGS - Tiptup Quior A", "(0x6C) BGS - Tiptup Quior A", ""),
            ("(0x6D) BGS - Tiptup Quior B", "(0x6D) BGS - Tiptup Quior B", ""),
            ("(0x6E) ?? - Test Map A", "(0x6E) ?? - Test Map A", ""),
            ("(0x6F) ?? - Test Map B", "(0x6F) ?? - Test Map B", ""),
            ("(0x70) CCW - Click Clock Woods A", "(0x70) CCW - Click Clock Woods A", ""),
            ("(0x71) CCW - Spring A", "(0x71) CCW - Spring A", ""),
            ("(0x72) CCW - Summer A", "(0x72) CCW - Summer A", ""),
            ("(0x73) CCW - Autumn A", "(0x73) CCW - Autumn A", ""),
            ("(0x74) CCW - Winter A", "(0x74) CCW - Winter A", ""),
            ("(0x75) CCW - Wasp Hive A", "(0x75) CCW - Wasp Hive A", ""),
            ("(0x76) CCW - Nabnut's House A", "(0x76) CCW - Nabnut's House A", ""),
            ("(0x77) CCW - Whiplash Room A", "(0x77) CCW - Whiplash Room A", ""),
            ("(0x78) CCW - Nabnut's Attic 1", "(0x78) CCW - Nabnut's Attic 1", ""),
            ("(0x79) CCW - Nabnut's Attic 2 A", "(0x79) CCW - Nabnut's Attic 2 A", ""),
            ("(0x7A) CCW - Nabnut's Attic 2 B", "(0x7A) CCW - Nabnut's Attic 2 B", ""),
            ("(0x7B) CCW - Click Clock Woods B", "(0x7B) CCW - Click Clock Woods B", ""),
            ("(0x7C) CCW - Spring B", "(0x7C) CCW - Spring B", ""),
            ("(0x7D) CCW - Summer B", "(0x7D) CCW - Summer B", ""),
            ("(0x7E) CCW - Autumn B", "(0x7E) CCW - Autumn B", ""),
            ("(0x7F) CCW - Winter B", "(0x7F) CCW - Winter B", ""),
            ("(0x80) GL - Quiz Room", "(0x80) GL - Quiz Room", ""),
            ("(0x81) Unknown 05", "(0x81) Unknown 05", ""),
            ("(0x82) Unknown 06", "(0x82) Unknown 06", ""),
            ("(0x83) Unknown 07", "(0x83) Unknown 07", ""),
            ("(0x84) Unknown 08", "(0x84) Unknown 08", ""),
            ("(0x85) CC - Clanker's Cavern A", "(0x85) CC - Clanker's Cavern A", ""),
            ("(0x86) Clanker's Cavern B", "(0x86) Clanker's Cavern B", ""),
            ("(0x87) CC - Inside Clanker Witch Switch A", "(0x87) CC - Inside Clanker Witch Switch A", ""),
            ("(0x88) CC - Inside Clanker A", "(0x88) CC - Inside Clanker A", ""),
            ("(0x89) CC - Inside Clanker B", "(0x89) CC - Inside Clanker B", ""),
            ("(0x8A) CC - Inside Clanker Gold Feathers A", "(0x8A) CC - Inside Clanker Gold Feathers A", ""),
            ("(0x8B) GL - Floor 1 A", "(0x8B) GL - Floor 1 A", ""),
            ("(0x8C) GL - Floor 2 A", "(0x8C) GL - Floor 2 A", ""),
            ("(0x8D) GL - Floor 3 A", "(0x8D) GL - Floor 3 A", ""),
            ("(0x8E) GL - Floor 3 - Pipe Room A", "(0x8E) GL - Floor 3 - Pipe Room A", ""),
            ("(0x8F) GL - Floor 3 - TTC Entrance A", "(0x8F) GL - Floor 3 - TTC Entrance A", ""),
            ("(0x90) GL - Floor 5 A", "(0x90) GL - Floor 5 A", ""),
            ("(0x91) GL - Floor 6 A", "(0x91) GL - Floor 6 A", ""),
            ("(0x92) GL - Floor 6 B", "(0x92) GL - Floor 6 B", ""),
            ("(0x93) GL - Floor 3 - CC Entrance A", "(0x93) GL - Floor 3 - CC Entrance A", ""),
            ("(0x94) GL - Boss A", "(0x94) GL - Boss A", ""),
            ("(0x95) GL - Lava Room", "(0x95) GL - Lava Room", ""),
            ("(0x96) GL - Floor 6 - MMM Entrance A", "(0x96) GL - Floor 6 - MMM Entrance A", ""),
            ("(0x97) GL - Floor 6 - Coffin Room A", "(0x97) GL - Floor 6 - Coffin Room A", ""),
            ("(0x98) GL - Floor 4 A", "(0x98) GL - Floor 4 A", ""),
            ("(0x99) GL - Floor 4 - BGS Entrance A", "(0x99) GL - Floor 4 - BGS Entrance A", ""),
            ("(0x9A) GL - Floor 7 A", "(0x9A) GL - Floor 7 A", ""),
            ("(0x9B) GL - Floor 7 - RBB Entrance A", "(0x9B) GL - Floor 7 - RBB Entrance A", ""),
            ("(0x9C) GL - Floor 7 - MMM Puzzle A", "(0x9C) GL - Floor 7 - MMM Puzzle A", ""),
            ("(0x9D) GL - Floor 9 A", "(0x9D) GL - Floor 9 A", ""),
            ("(0x9E) GL - Floor 8 - Path to Quiz A", "(0x9E) GL - Floor 8 - Path to Quiz A", ""),
            ("(0x9F) GL - Floor 3 - CC Entrance B", "(0x9F) GL - Floor 3 - CC Entrance B", ""),
            ("(0xA0) GL - Floor 7 B", "(0xA0) GL - Floor 7 B", ""),
            ("(0xA1) GL - Floor 7 - RBB Entrance B", "(0xA1) GL - Floor 7 - RBB Entrance B", ""),
            ("(0xA2) GL - Floor 7 - MMM Puzzle B", "(0xA2) GL - Floor 7 - MMM Puzzle B", ""),
            ("(0xA3) GL - Floor 1 B", "(0xA3) GL - Floor 1 B", ""),
            ("(0xA4) GL - Floor 2 B", "(0xA4) GL - Floor 2 B", ""),
            ("(0xA5) GL - Floor 3 - Pipe Room B", "(0xA5) GL - Floor 3 - Pipe Room B", ""),
            ("(0xA6) GL - Floor 4 B", "(0xA6) GL - Floor 4 B", ""),
            ("(0xA7) GL - First Cutscene Inside", "(0xA7) GL - First Cutscene Inside", ""),
            ("(0xA8) GL - Floor 4 - TTC Entrance B", "(0xA8) GL - Floor 4 - TTC Entrance B", ""),
            ("(0xA9) GL - Floor 3 - BGS Entrance B", "(0xA9) GL - Floor 3 - BGS Entrance B", ""),
            ("(0xAA) GL - Floor 3 B", "(0xAA) GL - Floor 3 B", ""),
            ("(0xAB) GL - Floor 9 B", "(0xAB) GL - Floor 9 B", ""),
            ("(0xAC) GL - Floor 8 - Path to Quiz B", "(0xAC) GL - Floor 8 - Path to Quiz B", ""),
            ("(0xAD) GL - Boss B", "(0xAD) GL - Boss B", "")
        ]
    )
    SFX_value_enum : bpy.props.EnumProperty(
        name="SFX Value",
        description="SFX Value Enum to determine Surface Sound",
        default="Normal",
        items = [(key, key, "") for key in Dicts.COLLISION_SFX.keys()],
        update = collision_SFX_changed
    )
    


#===========================================================================================================
#     ____   ____ __  __            ____                       __     
#    / __ ) / __ \\ \/ /           / __ \ ____ _ ____   ___   / /_____
#   / __  |/ /_/ / \  /  ______   / /_/ // __ `// __ \ / _ \ / // ___/
#  / /_/ // ____/  / /  /_____/  / ____// /_/ // / / //  __// /(__  ) 
# /_____//_/      /_/           /_/     \__,_//_/ /_/ \___//_//____/  
#                                                             
#===========================================================================================================

# PT elements are GUI Panels to collect and arrange Features + Props
class BINJO_PT_main_panel(bpy.types.Panel):
    """ GUI Panel for stuff """
    bl_label = "BINjo Control Panel"        # Panel Headline
    bl_space_type = "VIEW_3D"               # Editting View under which to find the Panel
    bl_region_type = "UI"                   #
    bl_category = 'Tool'                    # Which Tab the Panel is located under
    bl_options = {'HEADER_LAYOUT_EXPAND'}   #
    
    def draw(self, context):
        layout = self.layout

        # import from ROM
        row = layout.row()
        row.label(text="Source ROM :")
        row = layout.row()
        row.prop(context.scene.binjo_props, "rom_path", text="")

        row = layout.row()
        row.label(text="Targetted Map :")
        row = layout.row()
        row.prop(context.scene.binjo_props, "model_filename_enum", text="")

        row = layout.row()
        row.operator("conversion.from_rom")
        row = layout.row()
        row.operator("conversion.from_bin")

        # import from BIN
        layout.split()
        layout.split()
        # row = layout.row()
        # row.label(text="Exporting :")
        row = layout.row()
        row.label(text="Set Export Path :")
        row = layout.row()
        row.prop(context.scene.binjo_props, "export_path", text="")

        row = layout.row()
        row.operator("conversion.to_bin")
        row = layout.row()
        row.prop(context.scene.binjo_props, "force_model_A")
        # row = layout.row()
        # row.operator("conversion.dump_images")
        
        layout.split()
        layout.split()
        row = layout.row()
        row.label(text="Tooling :")

        # control elements
        row = layout.row()
        row.operator("material.create_mat")
        row = layout.row()
        row.operator("material.change_mat_img")
        row = layout.row()
        row.operator("object.convert_materials")

        row = layout.row()
        row.prop(context.scene.binjo_props, "highlight_invis")


# PT elements are GUI Panels to collect and arrange Features + Props
class BINJO_PT_material_panel(bpy.types.Panel):
    bl_label = "BINjo Tools"
    bl_space_type = "PROPERTIES"
    bl_region_type = "WINDOW"
    bl_context = 'material'
    bl_options = {"HIDE_HEADER"} # this forces the panel to the top in the stack as a side-effect

    def draw(self, context):
        # update_collision_dict_hidden(context)
        layout = self.layout

        box = layout.box()
        
        inner_box = box.box()
        head_row = inner_box.row()
        head_row.label(text=f"BINjo Material Collision Editor")

        row = box.row()
        row.prop(context.scene.binjo_props, "show_all_coll_flags", text="Show all Collision Flags")
        
        mat = None
        if (context.active_object is not None):
            mat = context.active_object.active_material

            if (mat is None):
                row = box.row()
                row.label(text="No Material is selected.")
                return
                
            if (mat.get("BINjo_Version", None) is None):
                row = box.row()
                row.label(text="Selected Material is not a BINjo Mat.")
                return

            if (mat is not None):

                row = box.row()
                row.prop(context.scene.binjo_props, "collision_disabled", index=0, text="Collision disabled entirely")

                sfx_row = box.row()
                sfx_row.prop(context.scene.binjo_props, "SFX_value_enum", text="Sound Effect")

                element_row = box.row()
                element_columns = (element_row.column(), element_row.column())
                
                # determine how many rows will be needed for the display
                if (context.scene.binjo_props.show_all_coll_flags == True):
                    # the +1 is basically like calling ceil() except without calling it
                    display_row_cnt = ((len(Dicts.COLLISION_FLAGS.keys()) + 1) // 2)
                if (context.scene.binjo_props.show_all_coll_flags == False):
                    display_row_cnt = 5

                displayed_elements = 0
                for idx, key in enumerate(mat["Collision_Flags"].keys()):
                    # if the toggle to show all flags is OFF, skip those that should be skipped
                    if ((context.scene.binjo_props.show_all_coll_flags == False) and ("UNK" in key or "(" in key)):
                        continue
                    # if the element is the SFX value, skip it (handled further up in sfx_row)
                    if (key == "SFX Value"):
                        continue
                    # draw the element
                    element_columns[displayed_elements // display_row_cnt].prop(
                        context.scene.binjo_props, "collision_checkboxes",
                        index=idx, text=key
                    )
                    displayed_elements += 1

        if (context.scene.binjo_props.show_all_coll_flags == True):
            row = box.row()
            if (mat is not None):
                row.label(text=f"Selected Material: {mat.name}")
            else:
                row.label(text="No Material Selected")



#===========================================================================================================
#     ______                           __ 
#    / ____/_  __ ____   ____   _____ / /_
#   / __/  | |/_// __ \ / __ \ / ___// __/
#  / /___ _>  < / /_/ // /_/ // /   / /_  
# /_____//_/|_|/ .___/ \____//_/    \__/  
#             /_/    
#===========================================================================================================

class BINJO_OT_export_to_BIN(bpy.types.Operator):
    """Export the model to a BIN File"""
    bl_idname = "conversion.to_bin"
    bl_label = "Export to BIN"
    bl_options = {'REGISTER'}

    def execute(self, context):                 # execute() is called when running the operator.
        export_timer_start = timer()
        export_timer = timer()
        scene = context.scene

        if (os.path.isdir(context.scene.binjo_props.export_path) == False):
            self.report({'ERROR'}, f"Export Path is not set to a viable Directory !")
            return {'CANCELLED'}
        if (os.access(context.scene.binjo_props.export_path, (os.R_OK & os.W_OK)) == False):
            self.report({'ERROR'}, f"Incorrect Permissions for Export Path Directory !")
            return {'CANCELLED'}

        global bin_handler
        new_ModelBin_A = ModelBIN()
        new_ModelBin_B = ModelBIN()

        # grab the targetted object (NOTE: should grab every object later... ugh)
        if (context.active_object is None):
            self.report({'ERROR'}, f"No Object selected to be exported !")
            return {'CANCELLED'}

        target_object = context.active_object
        color_attribute = target_object.data.color_attributes["import_Color"]
        uv_layer = target_object.data.uv_layers["import_UV"]
        # remember current mode, and set it to OBJECT for the time being
        original_mode = target_object.mode
        bpy.ops.object.mode_set(mode='OBJECT')



        print("Converting Material-Textures into TexSeg Data + Building TexSeg...")
        # first, create a list that tracks actually used materials within the model to avoid unneccessary exports
        loaded_materials = target_object.data.materials
        loaded_mat_cnt = len(loaded_materials)
        material_is_used = [False] * loaded_mat_cnt
        for face in target_object.data.polygons:
            # filter out materials that are not BINjo-related
            binjo_version = loaded_materials[face.material_index].get("BINjo_Version", None)
            if (binjo_version is None):
                continue
            # otherwise mark the material as being used
            material_is_used[face.material_index] = True

        tex_list = []
        material_tex_index_dict = {}
        # Create a BK Texture for every used Material (if it has an Image assigned) and create a Dict
        for idx, mat in enumerate(loaded_materials):
            # skip it, if it's not actually being used
            if (material_is_used[idx] == False):
                continue
            # materials that dont rock a texture get (-1)
            if (mat.node_tree.nodes["TEX"].image == None):
                material_tex_index_dict[mat.name] = -1
                continue
            # create a tex object from the image data linked to this material
            # this hurts a LOOOT...
            tex = ModelBIN_TexElem.build_from_IMG(mat.node_tree.nodes["TEX"].image)
            # and add it to our list if it is new
            if (tex not in tex_list):
                tex_list.append(tex)
            # finally, add the material to our dictionary to find the tex-index easily later
            material_tex_index_dict[mat.name] = tex_list.index(tex)
        # this is also kinda stupid for A/B Model split but can be fixed later
        new_ModelBin_A.TexSeg.populate_from_elements(tex_list)
        new_ModelBin_B.TexSeg.populate_from_elements(tex_list)
        
        print(f"({timer() - export_timer:.3f}s) -- Done.")
        export_timer = timer()



        print(f"Extracting granular Model Information + Building VtxSeg, DLSeg, ColSeg...")
        # sort every face into its own sub-list, to sepperate them by Material
        # (this makes building the DLs easier)
        polygon_list_list = [ [] for __ in range(loaded_mat_cnt)]

        for face in target_object.data.polygons:
            # catch if the user tries to convert a non-triangulated model
            if (len(face.vertices) > 3):
                self.report({'ERROR'}, f"Some Face in your Mesh is not triangular (vertex-count: {len(face.vertices)}) !")
                bpy.ops.object.mode_set(mode=original_mode)
                return { 'CANCELLED' }
            # completely ignoring loose geometry (vtx_cnt < 3)
            if (len(face.vertices) < 3):
                continue
            # otherwise we are good
            polygon_list_list[face.material_index].append(face)
            
        extracted_vertices_A = []
        extracted_vertices_B = []
        DL_command_list_A = []
        DL_command_list_B = []
        collision_tris_A = []
        collision_tris_B = []

        # now we can iterate over the sorted lists
        for polygon_list in polygon_list_list:
            # if the list is empty, it features an unused material
            if (len(polygon_list) == 0):
                continue
            rep_poly = polygon_list[0]

            # filter out materials that are not actually being used
            # (Non-Binjo Materials will also be fitlered out here as a side-effect)
            if (material_is_used[rep_poly.material_index] == False):
                continue

            # figure out which mat is assigned to this list
            assigned_mat = target_object.data.materials[rep_poly.material_index]
            
            # and gather the collision-type from it
            coll_type = ModelBIN_ColSeg.get_colltype_from_mat(assigned_mat)

            # as well as the tex_id through the material-dict from before
            tex_id = material_tex_index_dict[assigned_mat.name]
            tex_contains_transparency = False
            if (tex_id >= 0):
                # ATTENTION: this will 100% break when I clean up the A/B split some more...
                tex = new_ModelBin_A.TexSeg.tex_elements[tex_id]
                tex_contains_transparency = tex.contains_transparency
                # this is stupid, but good enough for now
                setup_commands_A = ModelBIN_DLSeg.build_setup_commands(tex, mode=0)
                setup_commands_B = ModelBIN_DLSeg.build_setup_commands(tex, mode=0)
                DL_command_list_A.extend(setup_commands_A)
                DL_command_list_B.extend(setup_commands_B)

            # this list will hold just a couple of same-tex tris, so I can bunch them
            # up and send them to the DL one big VTX-load + TRI-N command-chunk
            buffered_tris_A = []
            buffered_tris_B = []
            for polygon in polygon_list:

                vtx_triplet = []
                for (vertex_idx, loop_idx) in zip(polygon.vertices, polygon.loop_indices):
                    # get the XYZ coord containers, RGBA shade containers and UV coord containers
                    coords = target_object.data.vertices[vertex_idx].co
                    rgba   = color_attribute.data[loop_idx].color
                    uvs    = uv_layer.data[loop_idx].uv
                    # and extract the individual values (and correct the coordinate system)
                    x, y, z = [round(coord) for coord in coords]
                    x, y, z = x, z, -y
                    r, g, b, a = [round(255 * channel) for channel in rgba]
                    u_transf, v_transf = uvs.x, uvs.y
                    # to build a vertex from them
                    vtx = ModelBIN_VtxElem.build_from_model_data(x, y, z, r, g, b, a, u_transf, v_transf)
                    if (tex_id >= 0):
                        vtx.reverse_UV_transforms(tex.width, tex.height)
                    vtx_triplet.append(vtx)

                face_contains_transparency = False
                for vtx in vtx_triplet:
                    if (vtx.a < 0xFF):
                        face_contains_transparency = True
                        break
                
                # try to realign the UVs if they extend too far 
                success = binjo_utils.realign_vtx_UVs(vtx_triplet, tex.width, tex.height)
                if (success != 0):
                    self.report({'ERROR'}, f"UVs of a Face are extending too much !")
                    return {'CANCELLED'}

                if (
                    context.scene.binjo_props.force_model_A == True or \
                    (tex_contains_transparency == False and face_contains_transparency == False)
                ):
                    # add the triplet to the list of extracted verts
                    extracted_vertices_A.extend(vtx_triplet)

                    # then build a tri from the newest 3 vertices
                    tri = ModelBIN_TriElem()
                    vtx_cnt_A = len(extracted_vertices_A)
                    tri.build_from_parameters((vtx_cnt_A - 3), (vtx_cnt_A - 2), (vtx_cnt_A - 1), coll_type=coll_type, tex_id=tex_id)
                    tri.vtx_1 = vtx_triplet[0]
                    tri.vtx_2 = vtx_triplet[1]
                    tri.vtx_3 = vtx_triplet[2]

                    # if the previously determined coll-type is not None, add it to the collision tris
                    if (coll_type is not None):
                        collision_tris_A.append(tri)
                    # and if it has a valid tex_id, create the aforementioned DL command chunk for the buffered tris
                    if (tex_id >= 0):
                        buffered_tris_A.append(tri)
                        # if we reached 10 buffered tris, we dump them into a tri-drawing chunk and flush it
                        # (the DL VTX-Buffer can hold 0x20==32 verts; 10 tris have 30 verts)
                        if (len(buffered_tris_A) == 10): 
                            DL_command_list_A.extend(ModelBIN_DLSeg.build_tri_drawing_commands(buffered_tris_A))
                            buffered_tris_A = []
                else:
                    # add the triplet to the list of extracted verts
                    extracted_vertices_B.extend(vtx_triplet)

                    # then build a tri from the newest 3 vertices
                    tri = ModelBIN_TriElem()
                    vtx_cnt_B = len(extracted_vertices_B)
                    tri.build_from_parameters((vtx_cnt_B - 3), (vtx_cnt_B - 2), (vtx_cnt_B - 1), coll_type=coll_type, tex_id=tex_id)
                    tri.vtx_1 = vtx_triplet[0]
                    tri.vtx_2 = vtx_triplet[1]
                    tri.vtx_3 = vtx_triplet[2]

                    # if the previously determined coll-type is not None, add it to the collision tris
                    if (coll_type is not None):
                        collision_tris_B.append(tri)
                    # and if it has a valid tex_id, create the aforementioned DL command chunk for the buffered tris
                    if (tex_id >= 0):
                        buffered_tris_B.append(tri)
                        # if we reached 10 buffered tris, we dump them into a tri-drawing chunk and flush it
                        # (the DL VTX-Buffer can hold 0x20==32 verts; 10 tris have 30 verts)
                        if (len(buffered_tris_B) == 10): 
                            DL_command_list_B.extend(ModelBIN_DLSeg.build_tri_drawing_commands(buffered_tris_B))
                            buffered_tris_B = []

            # now the polygon loop is over; check if some buffered tris are left over
            if (tex_id >= 0 and len(buffered_tris_A) > 0):
                DL_command_list_A.extend(ModelBIN_DLSeg.build_tri_drawing_commands(buffered_tris_A))
            if (tex_id >= 0 and len(buffered_tris_B) > 0):
                DL_command_list_B.extend(ModelBIN_DLSeg.build_tri_drawing_commands(buffered_tris_B))
        
        # use the count of extracted A-model VTXs to determine if we need a A-Model at all
        # (this is moreso just a sanity check incase some User tries something silly like creating an only-alpha map...)
        if (len(extracted_vertices_A) > 0):
            # build the VTX-Seg from the extracted vertices
            new_ModelBin_A.VtxSeg.populate_from_vtx_list(extracted_vertices_A)
            # + the Collision-Seg from the collected collision tris
            new_ModelBin_A.ColSeg.populate_from_collision_tri_list(collision_tris_A)
            # + the DL-Seg from the constructed DL-command list
            new_ModelBin_A.DLSeg.populate_from_command_list(DL_command_list_A)
            # we can also build the GeoLayout Segment in this very crude default way...
            new_ModelBin_A.GeoSeg.build_from_minmax(
                min_x=new_ModelBin_A.VtxSeg.min_x, 
                min_y=new_ModelBin_A.VtxSeg.min_y,
                min_z=new_ModelBin_A.VtxSeg.min_z,
                max_x=new_ModelBin_A.VtxSeg.max_x,
                max_y=new_ModelBin_A.VtxSeg.max_y,
                max_z=new_ModelBin_A.VtxSeg.max_z
            )
            # and export Model-A
            new_ModelBin_A.export_to_BIN(filename=f"{context.scene.binjo_props.export_path}/test_A.bin")

        # use the count of extracted B-model VTXs to determine if we need a B-Model at all
        if (len(extracted_vertices_B) > 0):
            new_ModelBin_B.VtxSeg.populate_from_vtx_list(extracted_vertices_B)
            new_ModelBin_B.ColSeg.populate_from_collision_tri_list(collision_tris_B)
            new_ModelBin_B.DLSeg.populate_from_command_list(DL_command_list_B)
            new_ModelBin_B.GeoSeg.build_from_minmax(
                min_x=new_ModelBin_B.VtxSeg.min_x, 
                min_y=new_ModelBin_B.VtxSeg.min_y,
                min_z=new_ModelBin_B.VtxSeg.min_z,
                max_x=new_ModelBin_B.VtxSeg.max_x,
                max_y=new_ModelBin_B.VtxSeg.max_y,
                max_z=new_ModelBin_B.VtxSeg.max_z
            )
            new_ModelBin_B.export_to_BIN(filename=f"{context.scene.binjo_props.export_path}/test_B.bin")

        print(f"({timer() - export_timer:.3f}s) -- Done.")
        export_timer = timer()

        # reset object to original mode, export the collected data to BIN
        bpy.ops.object.mode_set(mode=original_mode)
        print(f"FULL TIME: {timer() - export_timer_start:.3f}s")
        return { 'FINISHED' }



#===========================================================================================================
#     ____                                __ 
#    /  _/____ ___   ____   ____   _____ / /_
#    / / / __ `__ \ / __ \ / __ \ / ___// __/
#  _/ / / / / / / // /_/ // /_/ // /   / /_  
# /___//_/ /_/ /_// .___/ \____//_/    \__/  
#                /_/  
#===========================================================================================================


class BINJO_OT_create_model_from_bin_handler(bpy.types.Operator):
    # this OP is hidden - used by the others
    bl_label = ""
    bl_idname = "conversion.from_bin_handler"
    bl_options = {'REGISTER'}

    def execute(self, context):       
        global bin_handler
        scene = context.scene
        export_timer_start = timer()
        export_timer = timer()

        print("Creating new Object...")
        # setting up a new mesh for the scene
        aaa = bpy.data.meshes.new("import_Mesh").name
        imported_mesh = bpy.data.meshes[aaa]
        imported_object = bpy.data.objects.new("import_Object", imported_mesh)
        
        vertices    = bin_handler.model_object.vertex_coord_list
        edges       = []
        faces       = bin_handler.model_object.face_idx_list
        imported_mesh.from_pydata(vertices, edges, faces)

        # create over-arching layer/attribute elements
        import_UV = imported_object.data.uv_layers.new(name="import_UV")
        color_attribute = imported_mesh.attributes.new(name='import_Color', domain='CORNER', type='BYTE_COLOR')

        # now create actual materials from the mat-names
        for binjo_mat in bin_handler.model_object.mat_list:

            mat = bpy.data.materials.new(binjo_mat.name)
            set_mat_to_default(mat)
            # assign the parsed Tex after defaulting the mat
            tex_node = mat.node_tree.nodes["TEX"]
            tex_node.image = binjo_mat.Blender_IMG
            if (tex_node.image is not None):

                if (os.path.isdir(context.scene.binjo_props.export_path) == False):
                    self.report({'WARNING'}, f"Export Path is not set to a viable Directory - Not saving tmp Images...")
                elif (os.access(context.scene.binjo_props.export_path, (os.R_OK & os.W_OK)) == False):
                    self.report({'WARNING'}, f"Incorrect Permissions for Export Path Directory !")
                else:
                    tex_node.image.filepath_raw = f"{context.scene.binjo_props.export_path}/{tex_node.image.name}"
                    tex_node.image.save()
            # also parse the collision properties and assign them correctly after defaulting
            mat["Collision_Disabled"] = bool("NOCOLL" in mat.name)
            mat["Collision_Flags"] = ModelBIN_ColSeg.get_collision_flag_dict(
                initial_value=ModelBIN_ColSeg.get_colltype_from_mat_name(mat.name)
            )
            mat["Collision_SFX"] = ModelBIN_ColSeg.get_SFX_from_mat_name(mat.name)

            # and add it to the mat-list
            imported_object.data.materials.append(mat)

        loop_ids = []
        for (face, tri) in zip(imported_mesh.polygons, bin_handler.model_object.complete_tri_list):
            # set material index of the face according to the data within tri
            face.material_index = tri.mat_index
            # and set the UV coords of the face through the loop indices
            import_UV.data[face.loop_indices[0]].uv = (tri.vtx_1.transformed_U, tri.vtx_1.transformed_V)
            import_UV.data[face.loop_indices[1]].uv = (tri.vtx_2.transformed_U, tri.vtx_2.transformed_V)
            import_UV.data[face.loop_indices[2]].uv = (tri.vtx_3.transformed_U, tri.vtx_3.transformed_V)
            
            # aswell as the RGBA shades
            if ("INVIS" in imported_object.data.materials[face.material_index].name):
                # if the toggle is active
                if (context.scene.binjo_props.highlight_invis == True):
                    # pure collision tris will be drawn in magenta
                    color_attribute.data[face.loop_indices[0]].color = (1.0, 0, 1.0, 1.0)
                    color_attribute.data[face.loop_indices[1]].color = (1.0, 0, 1.0, 1.0)
                    color_attribute.data[face.loop_indices[2]].color = (1.0, 0, 1.0, 1.0)
                else:
                    # otherwise make them gray and fully transparent
                    color_attribute.data[face.loop_indices[0]].color = (0.7, 0.7, 0.7, 0.0)
                    color_attribute.data[face.loop_indices[1]].color = (0.7, 0.7, 0.7, 0.0)
                    color_attribute.data[face.loop_indices[2]].color = (0.7, 0.7, 0.7, 0.0)
            else:
                # others get their vertex RGBA values assigned (regardless of textured or not)
                color_attribute.data[face.loop_indices[0]].color = (tri.vtx_1.r/255, tri.vtx_1.g/255, tri.vtx_1.b/255, tri.vtx_1.a/255)
                color_attribute.data[face.loop_indices[1]].color = (tri.vtx_2.r/255, tri.vtx_2.g/255, tri.vtx_2.b/255, tri.vtx_2.a/255)
                color_attribute.data[face.loop_indices[2]].color = (tri.vtx_3.r/255, tri.vtx_3.g/255, tri.vtx_3.b/255, tri.vtx_3.a/255)

        scene.collection.objects.link(imported_object)

        # just some names to check if neccessary
        print([e.name for e in bpy.data.materials[0].node_tree.nodes["Principled BSDF"].inputs])
        print(f"({timer() - export_timer:.3f}s) -- Done.")
        export_timer = timer()

        return { 'FINISHED' }



# init the bin-handler with data from ROM, grab a BIN from that ROM and convert it to a model
class BINJO_OT_import_from_ROM(bpy.types.Operator):
    """Import a model from a selected ROM"""    # Use this as a tooltip for menu items and buttons.
    bl_idname = "conversion.from_rom"           # Unique identifier for buttons and menu items to reference.
    bl_label = "Import from ROM"                # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}           # Enable undo for the operator.

    def execute(self, context):                 # execute() is called when running the operator.
        global bin_handler
        scene = context.scene

        if (bin_handler is None or bin_handler.ROM_name != scene.binjo_props.rom_path):
            bin_handler = BINjo_ModelBIN_Handler(rom_filename=scene.binjo_props.rom_path)
        bin_handler.load_model_file_from_ROM(scene.binjo_props.model_filename_enum)

        if (bin_handler.model_object is None):
            self.report({'ERROR'}, f"No Model-Object could be pulled from the ROM !")
            return {'CANCELLED'}
        
        bpy.ops.conversion.from_bin_handler()
        return {'FINISHED'}



# init the bin-handler without data, and convert an external BIN to a model
class BINJO_OT_import_from_BIN(bpy.types.Operator, ImportHelper):
    """Import a model from a selected BIN directly"""
    bl_idname = "conversion.from_bin"
    bl_label = "Import from BIN"
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        global bin_handler
        scene = context.scene

        if (bin_handler is None):
            bin_handler = BINjo_ModelBIN_Handler(rom_filename=None)
        bin_handler.load_model_file_from_BIN(self.filepath)

        if (bin_handler.model_object is None):
            self.report({'ERROR'}, f"No Model-Object could be pulled from the ROM !")
            return {'CANCELLED'}
        
        bpy.ops.conversion.from_bin_handler()
        return {'FINISHED'}




#===========================================================================================================
#     ____   ____ __  __            ____                             __                    
#    / __ ) / __ \\ \/ /           / __ \ ____   ___   _____ ____ _ / /_ ____   _____ _____
#   / __  |/ /_/ / \  /  ______   / / / // __ \ / _ \ / ___// __ `// __// __ \ / ___// ___/
#  / /_/ // ____/  / /  /_____/  / /_/ // /_/ //  __// /   / /_/ // /_ / /_/ // /   (__  ) 
# /_____//_/      /_/            \____// .___/ \___//_/    \__,_/ \__/ \____//_/   /____/  
#                                     /_/           
#===========================================================================================================

class BINJO_OT_dump_images(bpy.types.Operator):
    """Dump all the currently loaded Image Objects"""
    bl_idname = "conversion.dump_images"
    bl_label = "Dump Images"
    bl_options = {'REGISTER'}

    def execute(self, context):
        path = context.scene.binjo_props.export_path
        if (path == ""):
            return { 'CANCELLED' }
        if (os.isdir(path) == False):
            return { 'CANCELLED' }
        global bin_handler
        if (bin_handler is None):
            return { 'CANCELLED' }
        bin_handler.dump_image_files_to(path=path)
        return {'FINISHED'}
        


class BINJO_OT_convert_all_mats_to_binjo(bpy.types.Operator):
    """Convert ALL Materials of the selected Object into BINjo Default ones"""
    bl_idname = "object.convert_materials"
    bl_label = "Convert all Materials"
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        # check for object and active-mat existence
        target_object = context.active_object
        if (target_object is None):
            self.report({'ERROR'}, f"No Object selected !")
            return {'CANCELLED'}
                
        # create over-arching layer/attribute elements

        # if there is a color attr already, keep it and rename it for consistency
        if (len(target_object.data.color_attributes) > 0):
            target_object.data.color_attributes[0].name = "import_Color"
            color_attribute = target_object.data.color_attributes[0]
        # otherwise, create a new one
        else:
            color_attribute = target_object.data.color_attributes.new(name='import_Color', domain='CORNER', type='BYTE_COLOR')
            for idx in range(0, len(color_attribute.data)):
                color_attribute.data[idx].color = (1.0, 1.0, 1.0, 1.0)
        
        # same for UVs
        if (len(target_object.data.uv_layers) > 0):
            target_object.data.uv_layers[0].name = "import_UV"
            import_UV = target_object.data.uv_layers[0]
        else:
            import_UV = imported_object.data.uv_layers.new(name="import_UV")
        
        for mat in target_object.data.materials:
            # only convert mats that dont match the current binjo version
            if (mat.get("BINjo_Version", None) != version_num):
                set_mat_to_default(mat)

        return {'FINISHED'}

class BINJO_OT_change_mat_img(bpy.types.Operator, ImportHelper):
    """Change the Image of the currently selected Material"""
    bl_idname = "material.change_mat_img"
    bl_label = "Change Material Image"
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        # check for object and active-mat existence
        if (context.active_object is None):
            self.report({'ERROR'}, f"No Object selected !")
            return {'CANCELLED'}
        mat = context.active_object.active_material
        if (mat is None):
            self.report({'ERROR'}, f"No Material selected !")
            return {'CANCELLED'}
            
        mat.node_tree.nodes["TEX"].image = bpy.data.images.load(self.filepath)
        mat.node_tree.nodes["TEX"].image.filepath_raw = f"{self.filepath}"

        print(self.filepath)
        return {'FINISHED'}

def set_mat_to_default(mat):
    # first, retain (potential) old images, and remove old nodes
    # pulled from BBMat4.1
    old_image = None
    if (mat.use_nodes == True):
        for old_node in mat.node_tree.nodes:
            if old_node.type == "TEX_IMAGE":
                old_image = old_node.image
                break
        for old_node in mat.node_tree.nodes:
            # keep these 2 intact (also keeps BSDF settings that arent defaulted)
            if (old_node.name == "Principled BSDF" or old_node.name == "Material Output"):
                continue
            mat.node_tree.nodes.remove(old_node)
        
    # setting internal parameters within the mat
    mat.use_nodes = True
    mat.blend_method = "HASHED" # "HASHED" == Dithered Transparency
    mat.shadow_method = "NONE"
    mat.use_backface_culling = True
    # setting exposed parameters within the mat
    mat.node_tree.nodes["Principled BSDF"].inputs["Specular"].default_value = 0
            
    # texture node (NOTE that this will also assign "None" if the mat doesnt have an image)
    tex_node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex_node.name = "TEX"
    tex_node.location = [-600, +300]
    # using the old_image (it may be None, but that's fine)
    tex_node.image = old_image
        
    # color node (RGB+A)
    color_node = mat.node_tree.nodes.new("ShaderNodeVertexColor")
    color_node.name = "RGBA"
    new_x = (tex_node.location[0] + tex_node.width - color_node.width)
    color_node.location = (new_x, 0)
    color_node.layer_name = "import_Color" # this name is what's connecting the node to the attribute

    # mixer-node (texture * RGB)                  
    mix_node_1 = mat.node_tree.nodes.new("ShaderNodeMixRGB")
    mix_node_1.blend_type = "MULTIPLY"
    mix_node_1.location = (-275, +300)
    mix_node_1.inputs["Fac"].default_value = 1.0

    # link tex and color nodes to mixer
    mat.node_tree.links.new(tex_node.outputs["Color"], mix_node_1.inputs["Color1"])
    mat.node_tree.links.new(color_node.outputs["Color"], mix_node_1.inputs["Color2"])
    # link mixer to base-color input in main-material node
    mat.node_tree.links.new(mix_node_1.outputs["Color"], mat.node_tree.nodes[0].inputs["Base Color"])
    
    # and link color node's alpha output to mat alpha input
    mat.node_tree.links.new(color_node.outputs["Alpha"], mat.node_tree.nodes[0].inputs["Alpha"])

    mat["Collision_Disabled"] = False
    mat["Collision_Flags"] = ModelBIN_ColSeg.get_collision_flag_dict(0x0000_0000)
    mat["Collision_Flags"]["Use Default SFXs"] = True
    mat["Collision_SFX"] = Dicts.COLLISION_SFX["Normal"]
    mat["BINjo_Version"] = version_num



class BINJO_OT_create_mat(bpy.types.Operator, ImportHelper):
    """Create a new BINjo Material"""
    bl_idname = "material.create_mat"
    bl_label = "Create new Material"
    bl_options = {'REGISTER'}

    def execute(self, context):
        # check for object and active-mat existence
        target_object = context.active_object
        if (target_object is None):
            self.report({'ERROR'}, f"No Object selected !")
            return {'CANCELLED'}

        # if there is a color attr already, keep it and rename it for consistency
        if (len(target_object.data.color_attributes) > 0):
            target_object.data.color_attributes[0].name = "import_Color"
            color_attribute = target_object.data.color_attributes[0]
        # otherwise, create a new one
        else:
            color_attribute = target_object.data.color_attributes.new(name='import_Color', domain='CORNER', type='BYTE_COLOR')
            for idx in range(0, len(color_attribute.data)):
                color_attribute.data[idx].color = (1.0, 1.0, 1.0, 1.0)
        
        # same for UVs
        if (len(target_object.data.uv_layers) > 0):
            target_object.data.uv_layers[0].name = "import_UV"
            import_UV = target_object.data.uv_layers[0]
        else:
            import_UV = imported_object.data.uv_layers.new(name="import_UV")

        mat = bpy.data.materials.new("new_mat")
        set_mat_to_default(mat)
        # assign the loaded Tex after defaulting the mat
        mat.node_tree.nodes["TEX"].image = bpy.data.images.load(self.filepath)
        mat.node_tree.nodes["TEX"].image.filepath_raw = self.filepath

        # and add it to the mat-list
        target_object.data.materials.append(mat)
        return {'FINISHED'}


# class list to abstract / loopify the reg() und unreg() funcs
classes = [
    BINJO_Properties,
    BINJO_PT_main_panel,
    BINJO_PT_material_panel,
    BINJO_OT_create_model_from_bin_handler,
    BINJO_OT_import_from_ROM,
    BINJO_OT_import_from_BIN,
    BINJO_OT_export_to_BIN,
    BINJO_OT_dump_images,
    BINJO_OT_change_mat_img,
    BINJO_OT_create_mat,
    BINJO_OT_convert_all_mats_to_binjo
]

def register():
    for entry in classes:
        try:
            bpy.utils.register_class(entry)
        except ValueError:
            bpy.utils.unregister_class(entry)
            bpy.utils.register_class(entry)
    # and create a props object
    bpy.types.Scene.binjo_props = bpy.props.PointerProperty(type=BINJO_Properties)
    bpy.app.handlers.depsgraph_update_pre.append(general_update_function)

def unregister():
    for entry in reversed(classes):
        try:
            bpy.utils.unregister_class(entry)
        except ValueError:
            pass
    # and delete the props object
    del bpy.types.Scene.binjo_props
    bpy.app.handlers.depsgraph_update_pre.remove(general_update_function)

# This allows you to run the script directly from Blender's Text editor
# to test the add-on without having to install it.
if __name__ == "__main__":
    register()