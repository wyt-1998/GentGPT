from flask import Flask, jsonify, request
from gradio_client import Client
import trimesh, random
import os, datetime
from threading import Thread
import fnmatch

# blender directory on Windows platform
#if os.name == 'nt':
#	 bl_path = input("directory where Blender is installed: ")

client = Client("https://hysts-shap-e.hf.space/")

def object_generate(prompt):
	print("Generating the following object from Shap-e:{}".format(prompt))
	seed = random.randint(0, 2147483647)
	result = client.predict(prompt,	# str  in 'Prompt' Textbox component
							seed,	# int | float (numeric value between 0 and 2147483647) in 'Seed' Slider component
							20,	# int | float (numeric value between 1 and 20) in 'Guidance scale' Slider component
							64,	# int | float (numeric value between 1 and 100) in 'Number of inference steps' Slider component
							fn_index=3)
	return result	


n=0
def generate_list_of_objects(objects_list, saving_path, is_override=False,is_new = False, num_gen=1):
    for name in objects_list:
        #first check how many file exist already
        file_count = 0
        #checking how many existing .fbx file in the folder
        if (os.path.exists(saving_path[:-1] + '/' + name + '/')):
            print(saving_path[:-1] + '/' + name + '/')
            file_count = len(fnmatch.filter(os.listdir(saving_path[:-1] + '/' + name), '*.fbx'))
        print("file count = ",file_count)
        print(str(is_new) + "NEW MODE")
        if (is_new):
            num_start_file = file_count+1
            print("is_new is True so the number_start_file = ",num_start_file)
        else:
            num_start_file = 1
            print("is_new is Off so the number_start_file = ",num_start_file)
        # generate 
        for file_no in range(num_start_file, num_gen+num_start_file):
            print("Loop : File number : ",file_no)
            file_name = name + '_' + str(file_no)
            path_ = saving_path[:-1] + '/PLY Files/' + file_name + '.ply'

            if (os.path.exists(path_) and not is_override):
                print("{}.ply already exist".format(file_name))
                break
            else:
                print(path_, " does not exist yet")
            result = object_generate(name)
            mesh = trimesh.load(result)
            #saving_path = "/Users/wangyutong/ProjectDev/immersive_project/try/objects/"

            mesh.export(file_type="ply", file_obj= path_)
            # convert ply file to fbx file
            os.environ["THE_PATH"] = saving_path
            os.environ["OBJ_NAME"] = name
            os.environ["PLY_MODEL_NAME"] = file_name
            

            print(saving_path," ",name," ",file_name)

            #check if fbxfile exist or not
            path_fbx = saving_path[:-1] + '/' + name + '/' + file_name + '.fbx'
            if (os.path.exists(path_fbx) and not is_override):
                print("{}.fbx already exist".format(file_name))
                break

            try:
                #platform: Mac
                if os.name == 'posix':
                    bl_path ='/Applications/Blender.app/Contents/MacOS/Blender'
                    os.system('{} -b --python ply2fbx_MyApi.py'.format(bl_path))
                    print("{}.fbx has been generated".format(file_name))
            
                #platform: Windows
                if os.name == 'nt':
                    os.system('cmd /c "C:\\Program Files\\Blender Foundation\\Blender 3.5\\blender" -b --python ply2fbx_MyApi.py') 
                    print("{}.fbx has been generated".format(file_name))
            except:
                print('could not execute cmd')
        global n        
        n += 1
    return n

app = Flask(__name__)

@app.route("/shap_e/status", methods = ["GET"])
def status():
    return jsonify(status = "ok")

@app.route("/shap_e/progress", methods = ['GET'])
def progress():
    return jsonify(data = str(n))
    
@app.route("/shap_e/user_input", methods = ['POST'])
def getUserPrompt():
    use_thread = True
    prompt = request.json
    objects = prompt['objects']
    path_s = prompt['saving_path']
    is_override = prompt['is_override']
    is_new = prompt['is_new']
    num_gen = prompt['num_gen']
    saving_path = path_s+ "/" if path_s[-1]!="/" else path_s
    objects_list = objects.split(",")
    # threading
    if use_thread:
        n = len(objects_list)//3 if len(objects_list)%3==0 else len(objects_list)//3+1
        objects_1 = objects_list[:n]
        objects_2 = objects_list[n:n*2-1] if len(objects_list)%3!=0 else objects_list[n:n*2]
        objects_3 = objects_list[n*2-1:] if len(objects_list)%3!=0 else objects_list[n*2:]
        thread_1 = Thread(target= generate_list_of_objects, args=(objects_1, saving_path, is_override, is_new, num_gen))
        thread_2 = Thread(target= generate_list_of_objects, args=(objects_2, saving_path, is_override, is_new, num_gen))
        #start multiple threads
        thread_1.start()
        thread_2.start()
        generate_list_of_objects(objects_3, saving_path, is_override, is_new, num_gen)
    #   obj = generate_list_of_objects(objects_list, saving_path)
        print('each of ' + str(num_gen) + ' unique objects generated', len(objects_list))
        return {'each of ' + str(num_gen) + ' unique objects generated': len(objects_list)}
    else:
        obj = generate_list_of_objects(objects_list, saving_path, is_override, is_new, num_gen)
        print('each of ' + str(num_gen) + ' unique objects generated', obj)
        return {'each of ' + str(num_gen) + ' unique objects generated': obj}

if __name__ == '__main__':
    app.run(port = 7777)
