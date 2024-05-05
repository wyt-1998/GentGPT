using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class shap_e_btn : MonoBehaviour
{
    [SerializeField] private string objects;
    [SerializeField] private string saving_path;

    //TMP_InputField outputArea;
    private Button Btn;

    private void Start()
    {
        //outputArea = GameObject.Find("OutputArea").GetComponent<TMP_InputField>();
        GameObject.Find("Btn").GetComponent<Button>().onClick.AddListener(Post_method);
    }

    private void Post_method() => StartCoroutine(PostData(objects, saving_path));

    private IEnumerator PostData(string objects, string saving_path)
    {
        //outputArea.text = "Loading...";
        string url = "http://127.0.0.1:7777/shap_e/user_input";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] sending_text = System.Text.Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new sendRequest
                {
                    Objects = objects,
                    Saving_path = saving_path
                }));

            request.uploadHandler = new UploadHandlerRaw(sending_text);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            //outputArea.text = request.downloadHandler.text;
        }
    }
}