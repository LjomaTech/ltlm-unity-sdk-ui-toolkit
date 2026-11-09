using System.Globalization;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using LTLM.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One purchasable policy in the in-app store. Re-usable: SetupPolicy can be called repeatedly.
/// </summary>
public class PolicyCard : MonoBehaviour
{
    public TMP_Text PolicyName;
    public TMP_Text PolicyDescription;
    public TMP_Text Tokens;
    public TMP_Text Seats;
    public TMP_Text Type;
    public TMP_Text Time;
    public TMP_Text Capabilites;
    public TMP_Text Price;
    public Button Buy;

    private PolicyData _policy;
    private string _email;

    private void Awake()
    {
        if (Buy != null) Buy.onClick.AddListener(OnBuyClicked);
    }

    private void OnDestroy()
    {
        if (Buy != null) Buy.onClick.RemoveListener(OnBuyClicked);
    }

    public void SetupPolicy(PolicyData policyData, string email)
    {
        _policy = policyData;
        _email = email;
        if (policyData == null) return;

        var config = policyData.config;
        var limits = config?.limits;

        Set(PolicyName, policyData.name);
        Set(PolicyDescription, policyData.shortDescription ?? policyData.description);

        bool tokensEnabled = limits?.tokens?.enabled == true;
        Set(Tokens, tokensEnabled ? $"Tokens: {limits.tokens.maxTokens}" : string.Empty);

        bool seatsEnabled = limits?.seats?.enabled == true;
        Set(Seats, seatsEnabled ? $"Seats: {limits.seats.maxSeats}" : string.Empty);

        var caps = config?.EnabledCapabilities;
        Set(Capabilites, caps != null && caps.Count > 0 ? "Capabilities: " + string.Join(", ", caps) : string.Empty);

        Set(Type, policyData.type ?? string.Empty);

        string currency = string.IsNullOrEmpty(policyData.currency) ? "$" : policyData.currency + " ";
        string type = policyData.type ?? string.Empty;
        string duration = limits?.time?.mode == "duration" && limits.time.durationDays > 0
            ? $"{limits.time.durationDays} Days"
            : string.Empty;

        switch (type)
        {
            case "subscription":
                Set(Time, string.Empty);
                Set(Price, $"{FormatMoney(policyData.recurringPrice ?? policyData.price, currency)} / {policyData.recurringIntervalCount ?? 1} {policyData.recurringInterval}");
                break;
            case "trial":
                Set(Time, duration);
                Set(Price, string.IsNullOrEmpty(duration)
                    ? "Free trial"
                    : $"Free for {duration}");
                break;
            case "perpetual":
                Set(Time, "Lifetime");
                Set(Price, FormatMoney(policyData.price, currency));
                break;
            default:
                Set(Time, duration);
                Set(Price, FormatMoney(policyData.price, currency));
                break;
        }

        if (Buy != null) Buy.interactable = true;
    }

    private static string FormatMoney(float? amount, string currency)
    {
        if (!amount.HasValue || amount.Value <= 0f) return "Free";
        return currency + amount.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static void Set(TMP_Text target, string value)
    {
        if (target == null) return;
        target.text = value ?? string.Empty;
        target.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void OnBuyClicked()
    {
        if (_policy == null || LTLMManager.Instance == null) return;
        if (Buy != null) Buy.interactable = false;

        string redirect = LTLMUIManager.Instance != null ? LTLMUIManager.Instance.BuyCompleteRedirectURL : string.Empty;
        LTLMManager.Instance.CreateCheckoutSession(_policy.PolicyID.ToString(), _email, redirect,
            url =>
            {
                if (Buy != null) Buy.interactable = true;
                if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
            },
            error =>
            {
                if (Buy != null) Buy.interactable = true;
                Debug.LogWarning("[LTLM UI] Checkout link failed: " + error);
            });
    }
}
