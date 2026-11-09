using System;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using LTLM.UI;
using UnityEngine;

/// <summary>
/// Feature types that can be checked for conditional GameObject activation
/// </summary>
public enum LicenseFeatureType
{
    /// <summary>License has a time-based expiration (not perpetual)</summary>
    HasExpiration,

    /// <summary>License is perpetual (no expiration)</summary>
    IsPerpetual,

    /// <summary>Token consumption is enabled</summary>
    TokensEnabled,

    /// <summary>Concurrent seat management is enabled</summary>
    SeatsEnabled,

    /// <summary>License is a subscription (has billing date)</summary>
    IsSubscription,

    /// <summary>License is a one-time purchase (not subscription)</summary>
    IsOneTimePurchase,

    /// <summary>Offline mode is allowed</summary>
    OfflineEnabled,

    /// <summary>Check if specific capability is enabled</summary>
    HasCapability,

    /// <summary>License is currently active (includes grace / offline grace)</summary>
    IsActive,

    /// <summary>License is in trial period</summary>
    IsTrial,

    /// <summary>User can release their own devices</summary>
    CanReleaseDevices,

    /// <summary>User can release other seats</summary>
    CanReleaseSeats,

    /// <summary>License is expired but inside the server grace period</summary>
    GracePeriod,

    /// <summary>The SDK is running from cache because the server is unreachable</summary>
    IsOfflineGrace,

    /// <summary>The policy sells top-ups and the license allows buying them</summary>
    CanBuyTopUps,

    /// <summary>There are token consumptions waiting to sync</summary>
    HasPendingConsumptions
}

/// <summary>
/// Conditionally activates/deactivates a GameObject based on license features.
/// Refreshed at most once per frame after any license change.
/// </summary>
public class BindActiveToLicenseFeature : LTLMUIBinding
{
    [Header("Feature Check")] [Tooltip("Which feature to check")]
    public LicenseFeatureType feature;

    [Tooltip("Invert the check (activate when feature is NOT present)")]
    public bool invertCheck = false;

    [Header("Capability Check")] [Tooltip("Name of capability to check (only used when feature is HasCapability)")]
    public string capabilityName;

    [Header("Behavior")]
    [Tooltip("If true, toggles the children of this GameObject. If false, toggles the target object.")]
    public bool activateSelf = true;

    [Tooltip("Alternative GameObject to activate/deactivate (if activateSelf is false)")]
    public GameObject targetObject;

    private bool? _lastState;

    /// <summary>Updates the activation state based on current license.</summary>
    public void UpdateActivation() => Refresh();

    public override void Refresh()
    {
        bool conditionMet = CheckFeature();
        if (invertCheck) conditionMet = !conditionMet;

        if (_lastState == conditionMet) return; // no SetActive churn when nothing changed
        _lastState = conditionMet;

        if (activateSelf)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(conditionMet);
        }
        else if (targetObject != null)
        {
            targetObject.SetActive(conditionMet);
        }
    }

    private bool CheckFeature()
    {
        var manager = LTLMManager.Instance;
        if (manager == null) return false;

        var license = manager.ActiveLicense;
        if (license == null) return false;

        try
        {
            switch (feature)
            {
                case LicenseFeatureType.GracePeriod:
                    return manager.GetLicenseStatus() == LicenseStatus.GracePeriod;

                case LicenseFeatureType.IsOfflineGrace:
                    return manager.IsInOfflineGrace;

                case LicenseFeatureType.HasExpiration:
                    return !license.IsPerpetual;

                case LicenseFeatureType.IsPerpetual:
                    return license.IsPerpetual;

                case LicenseFeatureType.TokensEnabled:
                    return license.tokensEnabled ?? false;

                case LicenseFeatureType.SeatsEnabled:
                    return license.seatsEnabled ?? false;

                case LicenseFeatureType.IsSubscription:
                    return IsSubscription(license);

                case LicenseFeatureType.IsOneTimePurchase:
                    return !IsSubscription(license);

                case LicenseFeatureType.OfflineEnabled:
                    return license.offlineEnabled ?? false;

                case LicenseFeatureType.HasCapability:
                    return !string.IsNullOrEmpty(capabilityName) && manager.IsEntitled(capabilityName);

                case LicenseFeatureType.IsActive:
                    return manager.IsAuthenticated;

                case LicenseFeatureType.IsTrial:
                    return string.Equals(license.policy?.type, "trial", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(license.subscriptionStatus, "trialing", StringComparison.OrdinalIgnoreCase);

                case LicenseFeatureType.CanReleaseDevices:
                    return license.config?.customerActions?.allowHardwareRelease ?? false;

                case LicenseFeatureType.CanReleaseSeats:
                    return license.config?.customerActions?.allowRemoteSeatRelease ?? false;

                case LicenseFeatureType.CanBuyTopUps:
                    return (license.config?.customerActions?.canBuyTopUps ?? false) &&
                           (license.policy?.ResolvedTopUpOptions.Count ?? 0) > 0;

                case LicenseFeatureType.HasPendingConsumptions:
                    return manager.PendingConsumptionCount > 0;

                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BindActiveToLicenseFeature] Error checking feature {feature}: {e.Message}");
            return false;
        }
    }

    private static bool IsSubscription(LicenseData license)
    {
        string policyType = license.policy?.type?.ToLowerInvariant();
        return policyType == "subscription" || policyType == "trial";
    }
}
