using System.Collections;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class shap_e_api : MonoBehaviour
{
    private TMP_InputField objects;
    private TMP_InputField saving_path;
    private TMP_InputField outputArea;
    private Button sendBtn;

    private void Start()
    {
        objects = GameObject.Find("Objects").GetComponent<TMP_InputField>();
        saving_path = GameObject.Find("Path").GetComponent<TMP_InputField>();
        saving_path.text = Application.dataPath + "/Models";
        outputArea = GameObject.Find("OutputArea").GetComponent<TMP_InputField>();

        if (!AssetDatabase.IsValidFolder("Assets/Models"))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Models/PLY Files"))
        {
            AssetDatabase.CreateFolder("Assets/Models", "PLY Files");
        }

        GameObject.Find("SendBtn").GetComponent<Button>().onClick.AddListener(Post_method);
    }

    private void Post_method() => StartCoroutine(PostData(objects, saving_path));

    private IEnumerator PostData(TMP_InputField objects, TMP_InputField saving_path)
    {
        outputArea.text = "Loading...";
        string url = "http://127.0.0.1:7777/shap_e/user_input";
        //string url = "https://my-app-shape.nw.r.appspot.com/shap_e/user_input"; //public api
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] sending_text = System.Text.Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new sendRequest
                {
                    Objects = objects.text,
                    Saving_path = saving_path.text
                }));

            request.uploadHandler = new UploadHandlerRaw(sending_text);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            outputArea.text = request.downloadHandler.text;
        }
    }
}