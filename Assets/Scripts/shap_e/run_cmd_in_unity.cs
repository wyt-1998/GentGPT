using System.Diagnostics;
using UnityEngine;

public class run_cmd_in_unity : MonoBehaviour
{
    [SerializeField] private string fullPathToPython3Executable = "C:\\Users\\In\\miniconda3\\envs\\shape\\python.exe";

    private string pythonScriptPath = "/Scripts/pythonScript/shap_e_api_ply2fbx_blender.py";

    // Start is called before the first frame update
    private void Start()
    {
        string projectPath = Application.dataPath;

        UnityEngine.Debug.Log("python " + projectPath + pythonScriptPath);
        Run_cmd(fullPathToPython3Executable, "-u " + projectPath + pythonScriptPath);
    }

    private void Run_cmd(string cmd, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            },
            EnableRaisingEvents = true
        };

        process.ErrorDataReceived += Process_OutputDataReceived;
        process.OutputDataReceived += Process_OutputDataReceived;

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();
        //Console.Read();
    }

    private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        UnityEngine.Debug.Log(e.Data);
    }
}