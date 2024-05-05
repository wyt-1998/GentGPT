using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    private Slider slider;

    private float fillSpeed = 0.9f;

    private float target = 0;

    void Awake()
    {
        slider = gameObject.GetComponent<Slider>();
        gameObject.transform.parent.gameObject.SetActive(false);
    }

    void Start()
    {
        //IncrementProgress(0.75f);
    }

    // Update is called once per frame
    void Update()
    {
        if (slider.value < target)
        {
            slider.value += fillSpeed * Time.deltaTime;
        }
    }

    public void IncrementProgress(float newProgress)
    {
        target = newProgress;
    }

}
