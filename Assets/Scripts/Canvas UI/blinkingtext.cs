using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class blinkingtext : MonoBehaviour
{
    public TextMeshProUGUI textMesh;

    [Header("Blink Settings")]
    public float fadeSpeed = 1.5f;
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;

    private float alphaValue = 1f;
    private bool fadingOut = true;

    void Update()
    {
        if (fadingOut)
        {
            alphaValue -= Time.deltaTime * fadeSpeed;

            if (alphaValue <= minAlpha)
            {
                alphaValue = minAlpha;
                fadingOut = false;
            }
        }
        else
        {
            alphaValue += Time.deltaTime * fadeSpeed;

            if (alphaValue >= maxAlpha)
            {
                alphaValue = maxAlpha;
                fadingOut = true;
            }
        }

        Color textColor = textMesh.color;
        textColor.a = alphaValue;
        textMesh.color = textColor;
    }
}
