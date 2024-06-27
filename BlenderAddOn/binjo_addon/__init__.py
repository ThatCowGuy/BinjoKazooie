
# multi file addon workflow
# https://b3d.interplanety.org/en/creating-multifile-add-on-for-blender/ <-- looks promising
# https://blender.stackexchange.com/questions/202570/multi-files-to-addon

bl_info = {
    "name": "BINjo-Kazooie",
    "blender": (3, 4, 1),
    "category": "Object",
}

import bpy
import os

from . binjo_bin_handler import BINjo_ModelBIN_Handler

# Properties are data elements that show up in the GUI Panel
class BINJO_Properties(bpy.types.PropertyGroup):
    rom_path: bpy.props.StringProperty(
        name="",
        description="Path to ROM",
        default="",
        maxlen=1024,
        subtype='FILE_PATH'
    )
    model_filename: bpy.props.StringProperty(
        name="",
        description="Internal Model Filename",
        default="",
        maxlen=1024,
        subtype='NONE'
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
            ("(0x48) RBB - Machine Room", "(0x48) RBB - Machine Room", ""),
            ("(0x49) MISSING", "(0x49) MISSING", ""),
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
            ("(0xA8) GL - Floor 3 - BGS Entrance B", "(0xA8) GL - Floor 3 - BGS Entrance B", ""),
            ("(0xA9) GL - Floor 4 - TTC Entrance B", "(0xA9) GL - Floor 4 - TTC Entrance B", ""),
            ("(0xAA) GL - Floor 3 B", "(0xAA) GL - Floor 3 B", ""),
            ("(0xAB) GL - Floor 9 B", "(0xAB) GL - Floor 9 B", ""),
            ("(0xAC) GL - Floor 8 - Path to Quiz B", "(0xAC) GL - Floor 8 - Path to Quiz B", ""),
            ("(0xAD) GL - Boss B", "(0xAD) GL - Boss B", "")
        ]
    )

# PT elements are GUI Panels to collect and arrange Features + Props
class BINJO_PT_main_panel(bpy.types.Panel):
    """ GUI Panel for stuff """
    bl_label = "BINjo Tools"                # Panel Headline
    bl_space_type = "VIEW_3D"               # Editting View under which to find the Panel
    bl_region_type = "UI"                   #
    bl_category = 'Tool'                    # Which Tab the Panel is located under
    bl_options = {'HEADER_LAYOUT_EXPAND'}   #
    
    def draw(self, context):
        layout = self.layout
        row = layout.row()
        row.operator("import.from_rom")
        row = layout.row()
        row.prop(context.scene.binjo_props, "rom_path", text="")
        row = layout.row()
        row.prop(context.scene.binjo_props, "model_filename_enum", text="")

# def menu_func(self, context):
#     self.layout.operator(BINJO_OT_import_from_ROM.bl_idname)



# OT elements are Operators, which basically are callable Blender-Commands
class BINJO_OT_import_from_ROM(bpy.types.Operator):
    """Import a model from a selected ROM"""    # Use this as a tooltip for menu items and buttons.
    bl_idname = "import.from_rom"               # Unique identifier for buttons and menu items to reference.
    bl_label = "Import from ROM"                # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}           # Enable undo for the operator.

    def execute(self, context):                 # execute() is called when running the operator.
        scene = context.scene

        bin_handler = BINjo_ModelBIN_Handler(scene.binjo_props.rom_path)
        bin_handler.load_model_file(scene.binjo_props.model_filename_enum)

        if (bin_handler.model_object is not None):
            print("creating new object")
            # setting up a new mesh for the scene
            imported_mesh = bpy.data.meshes.new("test_mesh")
            imported_object = bpy.data.objects.new("test_object", imported_mesh)
            # imported_collection = bpy.data.collections.new("test_collection")
            # scene.collection.children.link(imported_collection)
            # imported_collection.objects.link(imported_object)
            vertices    = bin_handler.model_object.vertex_coord_list
            edges       = bin_handler.model_object.edge_idx_list
            faces       = bin_handler.model_object.face_idx_list
            imported_mesh.from_pydata(vertices, edges, faces)
            scene.collection.objects.link(imported_object)

        return {'FINISHED'}





# class list to abstract / loopify the reg() und unreg() funcs
classes = [
    BINJO_Properties,
    BINJO_PT_main_panel,
    BINJO_OT_import_from_ROM
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

def unregister():
    for entry in reversed(classes):
        try:
            bpy.utils.unregister_class(entry)
        except ValueError:
            pass
    # and delete the props object
    del bpy.types.Scene.binjo_props

# This allows you to run the script directly from Blender's Text editor
# to test the add-on without having to install it.
if __name__ == "__main__":
    register()