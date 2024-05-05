using UnityEngine;
using TMPro;
using UnityEngine.XR.OpenXR.Input;

// reference: Code Monkey: https://www.youtube.com/watch?v=YUIohCXt_pc&t=0s
public class Tooltip : MonoBehaviour
{
    public static Tooltip Instance { get; private set; }

    [SerializeField] private RectTransform canvasRectTransform;

    private RectTransform backgroundRectTransform;
    private TextMeshProUGUI textMeshPro;
    private RectTransform rectTransform;

    private void Awake()
    {
        Instance = this;

        backgroundRectTransform = transform.Find("Tooltip Image").GetComponent<RectTransform>();
        textMeshPro = transform.Find("Tooltip Text").GetComponent<TextMeshProUGUI>();
        rectTransform = transform.GetComponent<RectTransform>();

        HideTooltip();
    }

    private void SetText(string tooltipText)
    {
        textMeshPro.SetText(tooltipText);
        textMeshPro.ForceMeshUpdate();

        Vector2 textSize = textMeshPro.GetRenderedValues(false);
        Vector2 paddingSize = new Vector2(8, 8);

        backgroundRectTransform.sizeDelta = textSize + paddingSize;
    }

    public void ShowTooltip(string getTooltipTextFunc, Vector3 pos)
    {
        //this.getTooltipTextFunc = getTooltipTextFunc;
        gameObject.SetActive(true);
        SetText(getTooltipTextFunc);

        Vector3 anchoredPosition = pos + new Vector3(80, 10, -20f);

        Debug.Log("posX "+pos.x + "backgroundRectTransform " + backgroundRectTransform.rect.width);

        if (pos.x + backgroundRectTransform.rect.width > 920)
        {
            Debug.Log("Tooltip left screen on right side");
            // Tooltip left screen on right side
            anchoredPosition.x -= backgroundRectTransform.rect.width;
        }
        if (pos.y + backgroundRectTransform.rect.height > 1820)
        {
            Debug.Log("Tooltip left screen on top side");
            // Tooltip left screen on top side
            anchoredPosition.y -= backgroundRectTransform.rect.height;
        }

        //rectTransform.anchoredPosition = anchoredPosition;
        transform.localPosition = anchoredPosition;
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }

}
