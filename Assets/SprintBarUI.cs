using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SprintBarUI : MonoBehaviour
{
    public RectTransform fillBar;

    public void UpdateSprintBar(float percent)
    {
        if (fillBar == null) return;

        percent = Mathf.Clamp01(percent);
        fillBar.localScale = new Vector3(percent, 1f, 1f);
    }
}
