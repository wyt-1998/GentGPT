using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowTooltip : MonoBehaviour
{
    private bool isMouseClick = false;
    [TextArea(10, 25)][SerializeField] private string text;

    private void OnMouseEnter()
    {
        Tooltip.Instance.ShowTooltip(text, transform.localPosition);
    }

    private void OnMouseExit()
    {
        if (!isMouseClick) Tooltip.Instance.HideTooltip();
    }

    private void OnMouseDown()
    {
        if (!isMouseClick)
        {
            isMouseClick = true;
            Tooltip.Instance.ShowTooltip(text, transform.localPosition);
        }
        else
        {
            isMouseClick = false;
            //Tooltip.Instance.HideTooltip();
        }
    }
}

