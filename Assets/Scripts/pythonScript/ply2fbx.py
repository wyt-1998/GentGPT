import bpy, os

#clear all things in scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()

#receive data from other script
saving_path = os.environ.get("THE_PATH")
objName = os.environ.get("OBJ_NAME")
modelName = os.environ.get("PLY_MODEL_NAME")

#import ply file
ply_p = saving_path[:-1] + '/PLY Files/' + modelName + '.ply'
bpy.ops.import_mesh.ply(filepath=ply_p)
bpy.ops.object.select_all(action='SELECT')

#rotate model
#bpy.ops.transform.rotate(value=-1.5708, orient_axis='X', orient_type='GLOBAL')
#bpy.ops.transform.rotate(value=-1.5708, orient_axis='Z', orient_type='GLOBAL')

#export as fbx file
if (not os.path.exists(saving_path + objName)):
	os.mkdir(saving_path + objName)
fbx_p = saving_path + objName + "/" + modelName + '.fbx'
bpy.ops.export_scene.fbx(filepath=fbx_p, use_selection=True)

