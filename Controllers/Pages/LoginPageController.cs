using System.Collections.Generic;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using LTLM.UI;
using UnityEngine;

public class LoginPageController : MonoBehaviour
{
    public GameObject[] LoginFlowPages;
    public List<PolicyData> BuyablePolicies = new List<PolicyData>();

    private bool _policiesLoaded;

    private void OnEnable()
    {
        SetActiveStep(0);
        LoadBuyablePolicies();
    }

    /// <summary>Loads the store catalogue once, only when the Shop feature is enabled.</summary>
    public void LoadBuyablePolicies(bool force = false)
    {
        if (_policiesLoaded && !force) return;
        if (LTLMUIManager.Instance != null && !LTLMUIManager.Instance.features.HasFlag(FeatureBinding.Shop)) return;
        if (LTLMManager.Instance == null) return;

        LTLMManager.Instance.GetBuyablePolicies(policies =>
        {
            BuyablePolicies = policies ?? new List<PolicyData>();
            _policiesLoaded = true;
        }, error =>
        {
            Debug.LogWarning($"[LTLM UI] Failed to load buyable policies: {error}");
            BuyablePolicies = new List<PolicyData>();
        });
    }

    public void SetActiveStep(int step)
    {
        if (LoginFlowPages == null) return;
        for (int i = 0; i < LoginFlowPages.Length; i++)
        {
            if (LoginFlowPages[i] != null) LoginFlowPages[i].SetActive(i == step);
        }
    }
}
