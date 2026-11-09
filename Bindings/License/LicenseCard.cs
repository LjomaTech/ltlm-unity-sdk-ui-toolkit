using System.Globalization;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One license in the customer's portfolio (login flow). Re-usable: SetupLicense can be called repeatedly.
/// </summary>
public class LicenseCard : MonoBehaviour
{
    public TMP_Text Key;
    public TMP_Text PolicyName;
    public TMP_Text TokensUsage;
    public TMP_Text ActiveSeats;
    public TMP_Text Capabilites;
    public TMP_Text ExpireDate;
    public Button Activate;

    [Header("Formatting")]
    public string dateFormat = "dd-MM-yyyy";

    private LicenseData _license;

    private void Awake()
    {
        if (Activate != null) Activate.onClick.AddListener(OnActivateClicked);
    }

    private void OnDestroy()
    {
        if (Activate != null) Activate.onClick.RemoveListener(OnActivateClicked);
    }

    public void SetupLicense(LicenseData licenseData)
    {
        _license = licenseData;
        if (licenseData == null) return;

        if (Key != null) Key.text = BindToLicenseStat.MaskKey(licenseData.licenseKey);
        if (PolicyName != null) PolicyName.text = licenseData.policy?.name ?? "Unknown Policy";

        var config = licenseData.config;

        if (TokensUsage != null)
        {
            bool tokens = config?.limits?.tokens?.enabled == true || licenseData.tokensEnabled == true;
            TokensUsage.text = tokens
                ? $"Tokens: {licenseData.tokensRemaining?.ToString() ?? "∞"}/{licenseData.tokensLimit?.ToString() ?? "∞"}"
                : string.Empty;
            TokensUsage.gameObject.SetActive(tokens);
        }

        if (ActiveSeats != null)
        {
            bool seats = config?.limits?.seats?.enabled == true || licenseData.seatsEnabled == true;
            int max = licenseData.maxConcurrentSeats ?? config?.limits?.seats?.maxSeats ?? 1;
            ActiveSeats.text = seats ? $"Active Seats: {licenseData.activeSeats ?? 0}/{(max > 0 ? max : 1)}" : string.Empty;
            ActiveSeats.gameObject.SetActive(seats);
        }

        if (Capabilites != null)
        {
            var caps = LTLMManager.Instance != null
                ? LTLMManager.Instance.GetEntitledCapabilities(licenseData)
                : new System.Collections.Generic.List<string>();
            Capabilites.text = caps.Count > 0 ? "Capabilities: " + string.Join(", ", caps) : "Capabilities: -";
        }

        if (ExpireDate != null)
        {
            var expiry = licenseData.ValidUntilUtc;
            ExpireDate.text = licenseData.IsPerpetual || !expiry.HasValue
                ? "Perpetual"
                : "Expires: " + expiry.Value.ToLocalTime().ToString(dateFormat, CultureInfo.InvariantCulture);
        }

        if (Activate != null)
        {
            bool activatable = string.Equals(licenseData.status, "active", System.StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(licenseData.status, "grace_period", System.StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(licenseData.status, "VALID_NO_SEAT", System.StringComparison.OrdinalIgnoreCase);
            Activate.interactable = activatable;
        }
    }

    private void OnActivateClicked()
    {
        if (_license == null || LTLMManager.Instance == null) return;
        if (Activate != null) Activate.interactable = false;
        LTLMManager.Instance.ActivateLicense(_license.licenseKey,
            (license, status) => { if (Activate != null) Activate.interactable = true; },
            error => { if (Activate != null) Activate.interactable = true; });
    }
}
