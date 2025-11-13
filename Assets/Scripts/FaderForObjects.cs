using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaderForObjects : MonoBehaviour
{
    public float fadeSpeed = 1f, fadeAmount = 0.5f;
    private float originalOpacity;
    private Material Mat;
    public bool DoFade = false;

    void Start()
    {
        Mat = GetComponent<Renderer>().material;
        originalOpacity = Mat.color.a;
    }

    void Update()
    {
        if (DoFade)
            FadeNow();
        else
            ResetFade();
    }

    void FadeNow()
    {
        Color currentColor = Mat.color;
        Color smoothColor = new Color(currentColor.r, currentColor.g, currentColor.b,
            Mathf.Lerp(currentColor.a, fadeAmount, fadeSpeed * Time.deltaTime));

        Mat.color = smoothColor;
    }

    void ResetFade()
    {
        Color currentColor = Mat.color;
        Color smoothColor = new Color(currentColor.r, currentColor.g, currentColor.b,
            Mathf.Lerp(currentColor.a, originalOpacity, fadeSpeed * Time.deltaTime));

        Mat.color = smoothColor;
    }
}
