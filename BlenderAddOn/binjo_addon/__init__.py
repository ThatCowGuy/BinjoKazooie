
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
        bin_handler.load_model_file("TTC - Treasure Trove Cove")

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