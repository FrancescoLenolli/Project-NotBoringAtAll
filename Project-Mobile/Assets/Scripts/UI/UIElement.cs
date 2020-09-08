﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIElement : MonoBehaviour
{
    [HideInInspector] public CanvasGroup canvasGroup;
    [HideInInspector] public RectTransform rectTransform;

    public void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

}