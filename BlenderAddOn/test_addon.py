
import binjo_addon



bin_handler = binjo_addon.BINjo_ModelBIN_Handler("./banjo.us.v10.z64")
# bin_handler.load_model_file("RBB - Big Fish Warehouse A")
bin_handler.load_model_file("(0x01) TTC - Treasure Trove Cove")

print("creating new object")
vertices    = bin_handler.model_object.vertex_coord_list
edges       = bin_handler.model_object.edge_idx_list
faces       = bin_handler.model_object.face_idx_list