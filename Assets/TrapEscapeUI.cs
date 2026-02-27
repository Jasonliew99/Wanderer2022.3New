using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrapEscapeUI : MonoBehaviour
{
    public static TrapEscapeUI Instance;

    [Header("UI Root")]
    public GameObject root;   // child object

    [Header("Progress")]
    public Image fillBar;

    void Awake()
    {
        Instance = this;

        // Hide visuals safely
        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
            SetProgress(0f);
        }
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void SetProgress(float value)
    {
        if (fillBar != null)
            fillBar.fillAmount = Mathf.Clamp01(value);
    }
}
