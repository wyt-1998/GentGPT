using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class TestWeb : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(Test());
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private IEnumerator Test()
    {
        using (UnityWebRequest progressRequest = UnityWebRequest.Get("http://127.0.0.1:7777/shap_e/progress"))
        {
            progressRequest.disposeUploadHandlerOnDispose = true;
            progressRequest.disposeDownloadHandlerOnDispose = true;
            progressRequest.disposeCertificateHandlerOnDispose = true;
            progressRequest.SetRequestHeader("Content-Type", "application/json");
            progressRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            yield return progressRequest.SendWebRequest();
            Debug.Log(progressRequest.downloadHandler.data);
            string response = System.Text.Encoding.UTF8.GetString(progressRequest.downloadHandler.data);
            getResponse test = JsonConvert.DeserializeObject<getResponse>(response);
            Debug.Log(test.Data);

            //var test = new getResponse { Data = response };
            //Debug.Log(test.Data.ToString());
            //var result = int.Parse(progressRequest.downloadHandler.text) / 10;
            //var progress = Mathf.Clamp01(result);
        }
    }
}