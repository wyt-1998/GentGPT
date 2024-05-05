using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public Image statusImage;
    public TMPro.TextMeshProUGUI statusText;
    public TMPro.TextMeshProUGUI progressText;

    private Coroutine inProgress;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        SetStatus("Welcome to GenGPT, a experimental 3D environment generator powered by GPT4 and Shap-e!");
    }
    public void ShapeStatus(bool stat)
    {
        if (!stat)
        {
            statusImage.color = Color.red;
            SetStatus("Shap-e is not running! Please restart Shap-e and application!", true);
        }
        else { statusImage.color = Color.green; }
    }
    public void SetStatus(string text, bool isError = false)
    {
        string t = String.Empty;
        if (isError)
        {
            t = "[ERROR] ";
            statusText.color = Color.red;
        }
        else
        {
            t = "[INFO] ";
            statusText.color = Color.white;
        }
        statusText.text = t + text;
    }

    public void ProgressStatus(string text)
    {
        inProgress = StartCoroutine(StartProgressStatus(text));
    }

    private IEnumerator StartProgressStatus(string text)
    {
        while (true)
        {
            var dots = "";
            for (int i = 0; i < 3; i++)
            {
                dots += ".";
                progressText.SetText("In Progress: " + text + dots);
                yield return new WaitForSeconds(1f);
            }
            progressText.SetText("In Progress: " + text);
        }
    }

    public void StopProgressStatus()
    {
        progressText.SetText("");
        StopAllCoroutines();
        //StopCoroutine(inProgress);
    }
    public IEnumerator ButtonClick(Button btn, string text, string finishText, float seconds)
    {
        btn.interactable = false;
        btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = text;
        yield return new WaitForSeconds(seconds);
        btn.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = finishText;
    }
}