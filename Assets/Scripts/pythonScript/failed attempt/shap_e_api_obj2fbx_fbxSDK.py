from flask import Flask, jsonify, request
from gradio_client import Client
import trimesh, random
import fbx


client = Client("https://hysts-shap-e.hf.space/")

def object_generate(prompt):
	seed = 0 #random.randint(0, 2147483647)
	result = client.predict(prompt,	# str  in 'Prompt' Textbox component
							seed,	# int | float (numeric value between 0 and 2147483647) in 'Seed' Slider component
							20,	# int | float (numeric value between 1 and 20) in 'Guidance scale' Slider component
							64,	# int | float (numeric value between 1 and 100) in 'Number of inference steps' Slider component
                            fn_index=3)
	return result	


def generate_list_of_objects(objects_list, saving_path):
	n = 0
	for name in objects_list:
		result = object_generate(name)
		mesh = trimesh.load(result)
		#saving_path = "/Users/wangyutong/ProjectDev/immersive_project/try/objects/"
		filePath = saving_path[:-1] + ' obj/' + name + '.obj'
		mesh.export(file_type="obj", file_obj= filePath)
		n += 1

		# convert obj file to fbx file, codes reference: https://stackoverflow.com/questions/34132474/convert-obj-to-fbx-with-python-fbx-sdk
		# Create an SDK manager
		manager = fbx.FbxManager.Create()
		# Create a scene
		scene = fbx.FbxScene.Create(manager, "")
		# Create an importer object                                                                                                  
		importer = fbx.FbxImporter.Create(manager, "")
		# Path to the .obj file
		milfalcon = filePath
		# Specify the path and name of the file to be imported                                                                            
		importstat = importer.Initialize(milfalcon, -1)
		importstat = importer.Import(scene)
		# Create an exporter object                                                                                                  
		exporter = fbx.FbxExporter.Create(manager, "")
		save_path = saving_path + name + '.fbx'
		# Specify the path and name of the file to be imported                                                                            
		exportstat = exporter.Initialize(save_path, -1)
		exportstat = exporter.Export(scene)

	return n

app = Flask(__name__)

@app.route("/shap_e/status", methods = ["GET"])
def status():
	return jsonify(status = "ok")

@app.route("/shap_e/user_input", methods = ['POST'])
def getUserPrompt():
    prompt = request.json
    objects = prompt['objects']
    path_s = prompt['saving_path']
    saving_path = path_s+ "/" if path_s[-1]!="/" else path_s
    objects_list = objects.split(",")
    n = generate_list_of_objects(objects_list, saving_path)
    return  {'amount of objects generated':n}


if __name__ == '__main__':
    app.run(port = 7777)
