using System;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
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
    CapabilityEnabled
}

/// <summary>
/// Sub-type for stats that have Current/Max/Remaining variants
/// </summary>
public enum StatType
{
    Current,
    Max,
    Remaining
}

/// <summary>
/// Binds a TMP_Text component to a license statistic value.
/// Automatically updates when license data changes.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class BindToLicenseStat : MonoBehaviour
{
    [Header("Stat Configuration")]
    [Tooltip("Which license stat to display")]
    public LicenseStat stat;
    
    [Tooltip("For stats with variants (tokens, seats, devices)")]
    public StatType statType = StatType.Current;

    [Header("Capability Check")]
    [Tooltip("Name of capability to check (only used when stat is CapabilityEnabled)")]
    public string capabilityName;

    [Header("Formatting")]
    [Tooltip("Format string for dates (e.g., 'MMM dd, yyyy')")]
    public string dateFormat = "MMM dd, yyyy";
    
    [Tooltip("Text to show for perpetual licenses")]
    public string perpetualText = "Perpetual";
    
    [Tooltip("Text to show when value is not available")]
    public string notAvailableText = "-";
    
    [Tooltip("Prefix to add before the value")]
    public string prefix = "";
    
    [Tooltip("Suffix to add after the value")]
    public string suffix = "";

    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // Subscribe to relevant events
        LTLMManager.OnValidationCompleted += OnValidationCompleted;
        LTLMManager.OnLicenseStatusChanged += OnLicenseStatusChanged;
        LTLMManager.OnTokensConsumed += OnTokensConsumed;
        LTLMManager.OnSeatStatusChanged += OnSeatStatusChanged;
        
        // Update immediately with current data
        UpdateValue();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        LTLMManager.OnValidationCompleted -= OnValidationCompleted;
        LTLMManager.OnLicenseStatusChanged -= OnLicenseStatusChanged;
        LTLMManager.OnTokensConsumed -= OnTokensConsumed;
        LTLMManager.OnSeatStatusChanged -= OnSeatStatusChanged;
    }

    private void OnValidationCompleted(bool success, LicenseStatus status)
    {
        UpdateValue();
    }

    private void OnLicenseStatusChanged(LicenseStatus status)
    {
        UpdateValue();
    }

    private void OnTokensConsumed(LicenseData license)
    {
        if (stat == LicenseStat.TokensConsumed || 
            stat == LicenseStat.TokensLimit || 
            stat == LicenseStat.TokensRemaining)
        {
            UpdateValue();
        }
    }

    private void OnSeatStatusChanged(string seatStatus, int activeSeats, int maxSeats)
    {
        if (stat == LicenseStat.ActiveSeats || 
            stat == LicenseStat.MaxSeats || 
            stat == LicenseStat.SeatsRemaining)
        {
            UpdateValue();
        }
    }

    /// <summary>
    /// Updates the text value based on current license data
    /// </summary>
    public void UpdateValue()
    {
        if (_text == null) return;

        var license = LTLMManager.Instance?.ActiveLicense;
        string value = GetStatValue(license);
        _text.text = prefix + value + suffix;
    }

    private string GetStatValue(LicenseData license)
    {
        if (license == null)
        {
            return notAvailableText;
        }

        try
        {
            switch (stat)
            {
                // ===== Time-based =====
                case LicenseStat.ExpiresAt:
                    if (license.validUntil == null || license.validUntil == DateTime.MinValue)
                    {
                        return perpetualText;
                    }
                    // Check if it's a very far future date (perpetual)
                    if (license.validUntil > DateTime.Now.AddYears(50))
                    {
                        return perpetualText;
                    }
                    return license.validUntil?.ToString(dateFormat) ?? notAvailableText;

                case LicenseStat.NextBillingDate:
                    if (license.nextBillingDate == null || license.nextBillingDate == DateTime.MinValue)
                    {
                        return notAvailableText;
                    }
                    return license.nextBillingDate?.ToString(dateFormat) ?? notAvailableText;

                case LicenseStat.DaysRemaining:
                    if (license.validUntil == null || license.validUntil == DateTime.MinValue)
                    {
                        return perpetualText;
                    }
                    if (license.validUntil > DateTime.Now.AddYears(50))
                    {
                        return perpetualText;
                    }
                    var days = (license.validUntil.Value - DateTime.Now).Days;
                    return days >= 0 ? days.ToString() : "0";

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
                    if (!(license.seatsEnabled ?? false)) return notAvailableText;
                    int remaining = (license.maxConcurrentSeats ?? 1) - (license.activeSeats ?? 0);
                    return Math.Max(0, remaining).ToString();

                // ===== Devices =====
                case LicenseStat.ActiveDevices:
                    return (license.machines?.Length ?? 0).ToString();

                case LicenseStat.MaxDevices:
                    return (license.maxActivations ?? 1).ToString();

                case LicenseStat.DevicesRemaining:
                    int devicesRemaining = (license.maxActivations ?? 1) - (license.machines?.Length ?? 0);
                    return Math.Max(0, devicesRemaining).ToString();

                // ===== License Info =====
                case LicenseStat.LicenseType:
                    return license.policy?.type ?? notAvailableText;

                case LicenseStat.LicenseStatus:
                    return FormatStatus(license.status);

                case LicenseStat.LicenseKey:
                    return license.licenseKey ?? notAvailableText;

                case LicenseStat.PolicyName:
                    return license.policy?.name ?? notAvailableText;

                // ===== Subscription =====
                case LicenseStat.SubscriptionStatus:
                    return FormatSubscriptionStatus(license.subscriptionStatus);

                // ===== Capability =====
                case LicenseStat.CapabilityEnabled:
                    if (string.IsNullOrEmpty(capabilityName)) return notAvailableText;
                    bool hasCapability = LTLMManager.Instance?.IsEntitled(capabilityName) ?? false;
                    return hasCapability ? "Enabled" : "Disabled";

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

    private string FormatStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return notAvailableText;
        
        return status.ToLower() switch
        {
            "active" => "Active",
            "expired" => "Expired",
            "suspended" => "Suspended",
            "revoked" => "Revoked",
            "grace_period" => "Grace Period",
            "valid_no_seat" => "No Seat Available",
            "kicked" => "Session Kicked",
            _ => status
        };
    }

    private string FormatSubscriptionStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return notAvailableText;
        
        return status.ToLower() switch
        {
            "active" => "Active",
            "trialing" => "Trial",
            "past_due" => "Past Due",
            "canceled" => "Canceled",
            "unpaid" => "Unpaid",
            "incomplete" => "Incomplete",
            _ => status
        };
    }
}
