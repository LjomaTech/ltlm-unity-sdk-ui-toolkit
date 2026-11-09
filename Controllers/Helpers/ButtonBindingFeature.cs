using System;
using LTLM.UI;
using UnityEngine;

[Flags]
public enum FeatureBinding
{
    None = 0,
    Login = 1 << 1,
    ControlSettings = 1 << 2,
    Shop = 1 << 3,
    Airgapped = 1 << 4,
}

/// <summary>
/// Shows this GameObject only when the given UI feature is enabled on the <see cref="LTLMUIManager"/>.
/// </summary>
public class ButtonBindingFeature : MonoBehaviour
{
    public FeatureBinding feature;
    private FeatureBinding _previous;

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        var ui = LTLMUIManager.Instance;
        if (ui == null) return; // keep the designer's default state until the UI manager exists
        gameObject.SetActive((ui.features & feature) != 0);
    }

    private void OnValidate()
    {
        if (feature == _previous) return;

        // Keep only the newly selected flag (single-select behaviour in the inspector)
        var changed = feature ^ _previous;
        if (changed != 0) feature = changed;
        _previous = feature;
    }
}
