Setup 

	Conda
	1. Create environment (conda create -n ENV_NAME)
	2. Activate environment (conda activate ENV_NAME)
	3. Install PiP (conda install pip)
	4. Navigate to the pythonScript folder (cd YOUR_PATH\gengpt\Assets\Scripts\pythonScript)
	5. Install requirements (pip install -r requirements.txt)
	6. (Optional) Run Shap-e locally using CUDA (see section below)

	Using CUDA (Requires Nividia GPU)
	1. Install CUDA 11.8, Restart PC (https://developer.nvidia.com/cuda-11-8-0-download-archive)
	2. Uninstall Torch from requirements (pip uninstall torch torchvision torchaudio)
	2. Install Torch with CUDA (pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118)
	3. Install shap-e (git clone https://github.com/openai/shap-e.git, cd shap-e, pip install -e .)
	
Run

	1. Navigate to the pythonScript folder (cd YOUR_PATH\gengpt\Assets\Scripts\pythonScript)
	1. Activate environment (conda activate ENV_NAME)
	2. Run API (python MyApi_ply2fbx_blender.py)

ref:
https://pytorch.org/