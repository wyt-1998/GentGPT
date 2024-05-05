using System.Collections;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ShapE : MonoBehaviour
{
    private string saving_path;
    private ProceduralGen pg;
    private ProgressBar progressBar;

    private bool shapeStatus = false;
    private int ququeCount = 0;

    [SerializeField] private Slider slider;
    [SerializeField] private GameObject loadingBar;

    private void Start()
    {
        pg = GetComponent<ProceduralGen>();
        saving_path = Application.dataPath + "/Resources";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/PLY Files"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "PLY Files");
        }

        StartCoroutine(Status());
    }

    //void Post_method() => StartCoroutine(PostData(objects, saving_path));

    /// <summary>
    /// Post data to ShapE server
    /// </summary>
    /// <param name="objects"> string of objects </param>
    /// <param name="is_override"> allow to override exisiting file </param>
    /// <param name="num_gen"> number of same object generate </param>
    /// <returns></returns>

    public IEnumerator PostData(string objects, bool is_override = false, bool is_new = false, int num_gen = 1)
    {

        if (!shapeStatus)
        {
            yield break;
        }
        UIManager.Instance.ProgressStatus("3D Model");
        UIManager.Instance.SetStatus("Generating for 3D Model...", false);

        Debug.Log("Generating Objects from ShapE");
        Debug.Log(objects);
        string url = "http://127.0.0.1:7777/shap_e/user_input";
        //string url = "https://my-app-shape.nw.r.appspot.com/shap_e/user_input"; //public api
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] sending_text = System.Text.Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new sendRequest
                {
                    Objects = objects,
                    Saving_path = saving_path,
                    Is_override = is_override,
                    Is_new = is_new,
                    Num_gen = num_gen
                }));

            request.uploadHandler = new UploadHandlerRaw(sending_text);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest();

            int count = objects.Split(',').Length;
            if (count == 0) { count = 1; }  // if no object, count as 1
            slider.maxValue = count;
            slider.value = 0;
            StartCoroutine(Progress(count));

            while (!request.isDone)
            {
                yield return new WaitForSeconds(3);
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(request.downloadHandler.text);
            }
            else
            {
                Debug.Log(request.error);
                UIManager.Instance.SetStatus("Shap-e request error: " + request.error, true);
            }
            yield return null;
        }
    }


    private IEnumerator Progress(int count)
    {
        loadingBar.SetActive(true);
        while (true) {
            using (UnityWebRequest progressRequest = UnityWebRequest.Get("http://127.0.0.1:7777/shap_e/progress"))
            {
                progressRequest.disposeUploadHandlerOnDispose = true;
                progressRequest.disposeDownloadHandlerOnDispose = true;
                progressRequest.disposeCertificateHandlerOnDispose = true;
                progressRequest.SetRequestHeader("Content-Type", "application/json");
                progressRequest.downloadHandler = new DownloadHandlerBuffer();
                progressRequest.SendWebRequest();
                while (!progressRequest.isDone)
                {
                    yield return new WaitForSecondsRealtime(1f);
                    if (progressRequest.isNetworkError || progressRequest.isHttpError)
                    {
                        Debug.Log(progressRequest.error);
                        yield return null;
                        break;
                    }
                }

                string response = System.Text.Encoding.UTF8.GetString(progressRequest.downloadHandler.data);
                getResponse test = JsonConvert.DeserializeObject<getResponse>(response);
                var result = int.Parse(test.Data);
                //var progress = Mathf.Clamp01(result);
                slider.gameObject.GetComponent<ProgressBar>().IncrementProgress(result);
                //Debug.Log(test.Data);
                //Debug.Log(count);
                if (result == count)
                {
                    loadingBar.SetActive(false);
                    UIManager.Instance.StopProgressStatus();
                    pg.objectreqCompleted = true;
                    Debug.Log("SHAPE REQUEST IS DONE");
                    UIManager.Instance.SetStatus("Model Generation Done!");
                    yield return new WaitForSeconds(1f);
                    StopAllCoroutines();
                }
                yield return new WaitForSeconds(5f);
            }
        }
}

    private IEnumerator Status()
    {
        using (UnityWebRequest status = UnityWebRequest.Get("http://127.0.0.1:7777/shap_e/status"))
        {
            status.disposeUploadHandlerOnDispose = true;
            status.disposeDownloadHandlerOnDispose = true;
            status.disposeCertificateHandlerOnDispose = true;
            status.SetRequestHeader("Content-Type", "application/json");
            status.downloadHandler = new DownloadHandlerBuffer();
            status.SendWebRequest();
            var attempts = 0;
            while (!status.isDone && attempts <= 3)
            {
                attempts++;
                yield return new WaitForSeconds(1);
            }

            if (status.result == UnityWebRequest.Result.Success)
            {
                shapeStatus = true;
                UIManager.Instance.ShapeStatus(true);
                yield break;
            }
            UIManager.Instance.ShapeStatus(false);
        }
    }

}