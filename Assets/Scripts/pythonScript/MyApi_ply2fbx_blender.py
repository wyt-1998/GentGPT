from flask import Flask, jsonify, request
from gradio_client import Client
import trimesh, random
import os, datetime
import torch
from threading import Thread
import fnmatch

local=False
shape=True

try:
    from shap_e.diffusion.sample import sample_latents
    from shap_e.diffusion.gaussian_diffusion import diffusion_from_config
    from shap_e.models.download import load_model, load_config
    from shap_e.util.notebooks import create_pan_cameras, decode_latent_images, gif_widget
    from shap_e.util.notebooks import decode_latent_mesh
except ModuleNotFoundError:
    shpae=False
    pass

# blender directory on Windows platform
#if os.name == 'nt':
#    bl_path = input("directory where Blender is installed: ")


if torch.cuda.is_available() and shape:
    local=True
    device = torch.device('cuda')
    print("Using CUDA")
    
    #optimize
    torch.backends.cudnn.benchmark = True
    torch.pin_memory = True
    torch.no_grad()
    
    xm = load_model('transmitter', device=device)
    model = load_model('text300M', device=device)
    diffusion = diffusion_from_config(load_config('diffusion'))

    render_mode = 'nerf' # you can change this to 'stf'
    size = 64 # this is the size of the renders; higher values take longer to render.

    cameras = create_pan_cameras(size, device)
else:
    print("CUDA not available, fallback to online API")
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

def text_to_mesh(prompt: str, saving_path, file_name,
                 #seed = 0,
                 guidance_scale = 20.0, #20 is the best guidance scale according to the paper
                 batch_size = 1):
    #batch_size = 1 #amount of 3d object
    #torch.manual_seed(seed) #set seed

    latents = sample_latents(batch_size=batch_size,
                             model=model,
                             diffusion=diffusion,
                             guidance_scale=guidance_scale,
                             model_kwargs=dict(texts=[prompt] * batch_size),
                             progress=True,
                             clip_denoised=True,
                             use_fp16=True,
                             use_karras=True,
                             karras_steps=64,
                             sigma_min=1e-3,
                             sigma_max=160,
                             s_churn=0,)
    
    for i, latent in enumerate(latents):
        name = prompt + str(i+1) if batch_size > 1 else prompt
        f = open(saving_path[:-1] + '/PLY Files/' + file_name + '.ply', 'wb')
        decode_latent_mesh(xm, latent).tri_mesh().write_ply(f)

def generate_list_of_objects(objects_list, saving_path, is_override=False,is_new = False, num_gen=1):
    n=0
    for name in objects_list:
        print("count =", os.environ["COUNT"])
        
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
                
            if (local==True):
                text_to_mesh(name, saving_path, file_name)
            else:
                result = object_generate(name)
                mesh = trimesh.load(result)
                mesh.export(file_type="ply", file_obj= path_)
            #saving_path = "/Users/wangyutong/ProjectDev/immersive_project/try/objects/"

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
                blender_file = ""
                if local:
                    blender_file = "ply2fbx_MyApi"
                else:
                    blender_file = "ply2fbx"
                #platform: Mac
                if os.name == 'posix':
                    bl_path ='/Applications/Blender.app/Contents/MacOS/Blender'
                    os.system('{} -b --python {}.py'.format(bl_path, blender_file))
                    print("{}.fbx has been generated".format(file_name))
            
                #platform: Windows
                if os.name == 'nt':
                    #os.system('cmd /c "C:\\Program Files\\Blender Foundation\\Blender 3.2\\blender" -b --python {}.py'.format(blender_file))
                    os.system('cmd /c "C:\\Program Files\\Blender Foundation\\Blender 3.5\\blender" -b --python {}.py'.format(blender_file))
                    print("{}.fbx has been generated".format(file_name))
            except:
                print('could not execute cmd')      
        os.environ["COUNT"] = str( int(os.environ["COUNT"])+ 1)
        n+=1
    return n

app = Flask(__name__)

@app.route("/shap_e/status", methods = ["GET"])
def status():
    return jsonify(status = "ok")

@app.route("/shap_e/progress", methods = ['GET'])
def progress():
    #if (os.environ.get["COUNT"] == None):
    #    return jsonify(data = str(0))
    #else:
    return jsonify(data = str(os.environ["COUNT"]))
    
@app.route("/shap_e/user_input", methods = ['POST'])
def getUserPrompt():
    os.environ["COUNT"] = "0"
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
