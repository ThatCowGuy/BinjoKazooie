
bl_info = {
    "name": "BINjo-Kazooie",
    "blender": (3, 4, 1),
    "category": "Object",
}

import bpy
import pathlib

import BINjo_Vtx_Elem.py

class BINJO_PT_Panel(bpy.types.Panel):
    """ GUI Panel for stuff """
    bl_label = "BINjo Tools"                # Panel Headline
    bl_idname = "BINJO_PT_Panel"
    bl_space_type = "VIEW_3D"               # Editting View under which to find the Panel
    bl_region_type = "UI"                   #
    bl_category = 'Tool'                    # Which Tab the Panel is located under
    bl_options = {'HEADER_LAYOUT_EXPAND'}   #
    
    def draw(self, context):
        layout = self.layout
        row = layout.row()
        row.operator("object.move_x")
        row = layout.row()
        row.operator("object.move_xa")
        row = layout.row()
        row.operator("object.export_binjo")

class ObjectMoveX(bpy.types.Operator):
    """My Object Moving Script"""      # Use this as a tooltip for menu items and buttons.
    bl_idname = "object.move_x"        # Unique identifier for buttons and menu items to reference.
    bl_label = "Move X by One"         # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}  # Enable undo for the operator.

    def execute(self, context):        # execute() is called when running the operator.
        # The original script
        scene = context.scene
        print("aloha")
        for obj in scene.objects:
            obj.location.x += 1.0
        return {'FINISHED'}            # Lets Blender know the operator finished successfully.
class ObjectMoveXANTI(bpy.types.Operator):
    """My Object Moving Script"""      # Use this as a tooltip for menu items and buttons.
    bl_idname = "object.move_xa"        # Unique identifier for buttons and menu items to reference.
    bl_label = "Move Xa by One"         # Display name in the interface.
    bl_options = {'REGISTER', 'UNDO'}  # Enable undo for the operator.

    def execute(self, context):        # execute() is called when running the operator.
        # The original script
        scene = context.scene
        for obj in scene.objects:
            obj.location.x -= 1.0
        return {'FINISHED'}            # Lets Blender know the operator finished successfully.


class ExportToBIN(bpy.types.Operator):
    """Exports Model to BINjo"""      # Use this as a tooltip for menu items and buttons.
    bl_idname = "object.export_binjo" # Unique identifier for buttons and menu items to reference.
    bl_label = "Export BINjo"         # Display name in the interface.
    bl_options = {'REGISTER'}         # DONT Enable undo for the operator.

    def execute(self, context):
        filepath = pathlib.Path.home() / "source" / "repos" / "BinjoKazooie" / "BlenderAddOn" / "test3.txt"
        print(filepath)
        outfile = open(filepath, "w")
        # https://docs.blender.org/api/current/bpy.types.Scene.html#bpy.types.Scene
        scene = context.scene
        for obj in scene.objects:
            mesh = obj.data
            # https://docs.blender.org/api/current/bpy.types.MeshVertex.html#bpy.types.MeshVertex
            for vtx in mesh.vertices:
                outfile.write(f"v {vtx.co.x} {vtx.co.y} {vtx.co.z}\n")
            # https://docs.blender.org/api/current/bpy.types.MeshPolygon.html#bpy.types.MeshPolygon
            for face in mesh.polygons:
                outfile.write(f"f ")
                for id in face.vertices:
                    outfile.write(f"{id} ")
                outfile.write("\n")
        outfile.close()
        # UV
        # https://blender.stackexchange.com/questions/30677/get-set-coordinates-for-uv-vertices-using-python

        return {'FINISHED'} 

# this somehow registers the funcs to be available to the Panel ?
def menu_func(self, context):
    self.layout.operator(ExportToBIN.bl_idname)
    self.layout.operator(ObjectMoveX.bl_idname)
    self.layout.operator(ObjectMoveXANTI.bl_idname)

def register():
    bpy.utils.register_class(BINJO_PT_Panel)
    bpy.utils.register_class(ExportToBIN)
    bpy.utils.register_class(ObjectMoveX)
    bpy.utils.register_class(ObjectMoveXANTI)

def unregister():
    bpy.utils.unregister_class(BINJO_PT_Panel)
    bpy.utils.unregister_class(ExportToBIN)
    bpy.utils.unregister_class(ObjectMoveX)
    bpy.utils.unregister_class(ObjectMoveXANTI)


# This allows you to run the script directly from Blender's Text editor
# to test the add-on without having to install it.
if __name__ == "__main__":
    register()