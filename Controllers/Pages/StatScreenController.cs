using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatScreenController : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var bindActive in GetComponentsInChildren<BindActiveToLicenseFeature>(true))
        {
            bindActive.gameObject.SetActive(true);
        }
    }
}