using System;
using System.Globalization;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using LTLM.UI;
using TMPro;
using UnityEngine;

/// <summary>
/// License stat types that can be bound to UI elements
/// </summary>
public enum LicenseStat
{
    // Time-based
    ExpiresAt,
    NextBillingDate,
    DaysRemaining,

    // Tokens
    TokensConsumed,
    TokensLimit,
    TokensRemaining,

    // Seats
    ActiveSeats,
    MaxSeats,
    SeatsRemaining,

    // Devices/HWID
    ActiveDevices,
    MaxDevices,
    DevicesRemaining,

    // License Info
    LicenseType,
    LicenseStatus,
    LicenseKey,
    PolicyName,

    // Subscription
    SubscriptionStatus,

    // Capability (requires CapabilityName to be set)
    CapabilityEnabled,

    // Customer
    CustomerEmail,

    // Offline grace remaining (hours) or "-" when online
    OfflineGraceRemaining
}

/// <summary>
/// Binds a TMP_Text component to a license statistic value.
/// Refreshed at most once per frame after any license change (see <see cref="LTLMUIRefresher"/>).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class BindToLicenseStat : LTLMUIBinding
{
    [Header("Stat Configuration")] [Tooltip("Which license stat to display")]
    public LicenseStat stat;

    [Header("Capability Check")] [Tooltip("Name of capability to check (only used when stat is CapabilityEnabled)")]
    public string capabilityName;

    [Header("Formatting")] [Tooltip("Format string for dates (e.g., 'MMM dd, yyyy')")]
    public string dateFormat = "dd-MM-yyyy";

    [Tooltip("Text to show for perpetual licenses")]
    public string perpetualText = "Perpetual";

    [Tooltip("Text to show when value is not available")]
    public string notAvailableText = "-";

    [Tooltip("Prefix to add before the value")]
    public string prefix = "";

    [Tooltip("Suffix to add after the value")]
    public string suffix = "";

    [Tooltip("Mask the middle of the license key (LIC-0BEC…F012F)")]
    public bool maskLicenseKey = true;

    private TMP_Text _text;
    private string _lastValue;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    /// <summary>Updates the text value based on current license data.</summary>
    public void UpdateValue() => Refresh();

    public override void Refresh()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();
        if (_text == null) return;

        string value = prefix + GetStatValue(LTLMManager.Instance?.ActiveLicense) + suffix;
        if (value == _lastValue) return; // avoid TMP re-layout when nothing changed
        _lastValue = value;
        _text.text = value;
    }

    private string GetStatValue(LicenseData license)
    {
        if (license == null) return notAvailableText;

        try
        {
            switch (stat)
            {
                // ===== Time-based =====
                case LicenseStat.ExpiresAt:
                {
                    if (license.IsPerpetual) return perpetualText;
                    var d = license.ValidUntilUtc;
                    return d.HasValue ? d.Value.ToLocalTime().ToString(dateFormat, CultureInfo.InvariantCulture) : notAvailableText;
                }

                case LicenseStat.NextBillingDate:
                {
                    var d = license.NextBillingDateUtc;
                    return d.HasValue ? d.Value.ToLocalTime().ToString(dateFormat, CultureInfo.InvariantCulture) : notAvailableText;
                }

                case LicenseStat.DaysRemaining:
                {
                    if (license.IsPerpetual) return perpetualText;
                    int days = LTLMManager.Instance != null ? LTLMManager.Instance.GetDaysRemaining() : -1;
                    return days >= 0 ? days.ToString() : notAvailableText;
                }

                // ===== Tokens =====
                case LicenseStat.TokensConsumed:
                    if (!(license.tokensEnabled ?? false)) return notAvailableText;
                    return (license.tokensConsumed ?? 0).ToString();

                case LicenseStat.TokensLimit:
                    if (!(license.tokensEnabled ?? false)) return notAvailableText;
                    return license.tokensLimit?.ToString() ?? "∞";

                case LicenseStat.TokensRemaining:
                    if (!(license.tokensEnabled ?? false)) return notAvailableText;
                    return license.tokensRemaining?.ToString() ?? "∞";

                // ===== Seats =====
                case LicenseStat.ActiveSeats:
                    if (!(license.seatsEnabled ?? false)) return notAvailableText;
                    return (license.activeSeats ?? 0).ToString();

                case LicenseStat.MaxSeats:
                    if (!(license.seatsEnabled ?? false)) return notAvailableText;
                    return (license.maxConcurrentSeats ?? 1).ToString();

                case LicenseStat.SeatsRemaining:
                {
                    if (!(license.seatsEnabled ?? false)) return notAvailableText;
                    int remaining = (license.maxConcurrentSeats ?? 1) - (license.activeSeats ?? 0);
                    return Math.Max(0, remaining).ToString();
                }

                // ===== Devices =====
                case LicenseStat.ActiveDevices:
                    return (license.machines?.Count ?? 0).ToString();

                case LicenseStat.MaxDevices:
                    return (license.config?.limits?.devices?.maxDevices ?? license.maxActivations ?? 1).ToString();

                case LicenseStat.DevicesRemaining:
                {
                    int max = license.config?.limits?.devices?.maxDevices ?? license.maxActivations ?? 1;
                    return Math.Max(0, max - (license.machines?.Count ?? 0)).ToString();
                }

                // ===== License Info =====
                case LicenseStat.LicenseType:
                    return license.policy?.type ?? notAvailableText;

                case LicenseStat.LicenseStatus:
                    return FormatStatus(LTLMManager.Instance != null ? LTLMManager.Instance.GetLicenseStatus() : LicenseStatus.Unauthenticated, license.status);

                case LicenseStat.LicenseKey:
                    return string.IsNullOrEmpty(license.licenseKey)
                        ? notAvailableText
                        : (maskLicenseKey ? MaskKey(license.licenseKey) : license.licenseKey);

                case LicenseStat.PolicyName:
                    return license.policy?.name ?? notAvailableText;

                // ===== Subscription =====
                case LicenseStat.SubscriptionStatus:
                    return FormatSubscriptionStatus(license.subscriptionStatus);

                // ===== Capability =====
                case LicenseStat.CapabilityEnabled:
                    if (string.IsNullOrEmpty(capabilityName)) return notAvailableText;
                    return license.HasCapability(capabilityName) ? "Enabled" : "Disabled";

                case LicenseStat.CustomerEmail:
                    return license.customer?.email ?? notAvailableText;

                case LicenseStat.OfflineGraceRemaining:
                {
                    var remaining = LTLMManager.Instance?.OfflineGraceRemaining;
                    return remaining.HasValue ? $"{remaining.Value.TotalHours:F1}h" : notAvailableText;
                }

                default:
                    return notAvailableText;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BindToLicenseStat] Error getting stat {stat}: {e.Message}");
            return notAvailableText;
        }
    }

    public static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length <= 8) return key;
        int keep = Math.Min(4, key.Length / 4);
        return key.Substring(0, keep) + new string('•', Math.Max(4, key.Length - keep * 2)) + key.Substring(key.Length - keep);
    }

    private string FormatStatus(LicenseStatus resolved, string raw)
    {
        switch (resolved)
        {
            case LicenseStatus.Active: return "Active";
            case LicenseStatus.OfflineGrace: return "Active (offline)";
            case LicenseStatus.GracePeriod: return "Grace Period";
            case LicenseStatus.ValidNoSeat: return "No Seat Available";
            case LicenseStatus.Kicked: return "Session Kicked";
            case LicenseStatus.Expired: return "Expired";
            case LicenseStatus.Suspended: return "Suspended";
            case LicenseStatus.Revoked: return "Revoked";
            case LicenseStatus.Terminated: return "Terminated";
            case LicenseStatus.ConnectionRequired: return "Connection Required";
            case LicenseStatus.Tampered: return "Tampered";
            case LicenseStatus.Unauthenticated: return string.IsNullOrEmpty(raw) ? notAvailableText : raw;
            default: return raw ?? notAvailableText;
        }
    }

    private string FormatSubscriptionStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return notAvailableText;

        switch (status.ToLowerInvariant())
        {
            case "active": return "Active";
            case "trialing": return "Trial";
            case "past_due": return "Past Due";
            case "canceled": return "Canceled";
            case "unpaid": return "Unpaid";
            case "incomplete": return "Incomplete";
            default: return status;
        }
    }
}
