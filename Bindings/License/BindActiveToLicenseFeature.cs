using System;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
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
    
    /// <summary>License is currently active</summary>
    IsActive,
    
    /// <summary>License is in trial period</summary>
    IsTrial,
    
    /// <summary>User can release their own devices</summary>
    CanReleaseDevices,
    
    /// <summary>User can release other seats</summary>
    CanReleaseSeats
}

/// <summary>
/// Conditionally activates/deactivates a GameObject based on license features.
/// Useful for showing/hiding UI elements based on license type or capabilities.
/// </summary>
public class BindActiveToLicenseFeature : MonoBehaviour
{
    [Header("Feature Check")]
    [Tooltip("Which feature to check")]
    public LicenseFeatureType feature;
    
    [Tooltip("Invert the check (activate when feature is NOT present)")]
    public bool invertCheck = false;

    [Header("Capability Check")]
    [Tooltip("Name of capability to check (only used when feature is HasCapability)")]
    public string capabilityName;

    [Header("Behavior")]
    [Tooltip("If true, activates this GameObject when condition is met. If false, activates target object.")]
    public bool activateSelf = true;
    
    [Tooltip("Alternative GameObject to activate/deactivate (if activateSelf is false)")]
    public GameObject targetObject;

    private void OnEnable()
    {
        // Subscribe to license events
        LTLMManager.OnValidationCompleted += OnValidationCompleted;
        LTLMManager.OnLicenseStatusChanged += OnLicenseStatusChanged;
        
        // Update immediately
        UpdateActivation();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        LTLMManager.OnValidationCompleted -= OnValidationCompleted;
        LTLMManager.OnLicenseStatusChanged -= OnLicenseStatusChanged;
    }

    private void OnValidationCompleted(bool success, LicenseStatus status)
    {
        UpdateActivation();
    }

    private void OnLicenseStatusChanged(LicenseStatus status)
    {
        UpdateActivation();
    }

    /// <summary>
    /// Updates the activation state based on current license
    /// </summary>
    public void UpdateActivation()
    {
        bool conditionMet = CheckFeature();
        
        if (invertCheck)
        {
            conditionMet = !conditionMet;
        }

        if (activateSelf)
        {
            // We can't deactivate ourselves if we're the target, so we use a different approach
            // Note: This component stays enabled, but we can control children or visibility
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(conditionMet);
            }
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
                case LicenseFeatureType.HasExpiration:
                    return HasExpiration(license);

                case LicenseFeatureType.IsPerpetual:
                    return !HasExpiration(license);

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
                    if (string.IsNullOrEmpty(capabilityName)) return false;
                    return manager.IsEntitled(capabilityName);

                case LicenseFeatureType.IsActive:
                    return manager.IsAuthenticated;

                case LicenseFeatureType.IsTrial:
                    return license.policy?.type?.ToLower() == "trial" ||
                           license.subscriptionStatus?.ToLower() == "trialing";

                case LicenseFeatureType.CanReleaseDevices:
                    return license.config?.customerActions?.allowHardwareRelease ?? false;

                case LicenseFeatureType.CanReleaseSeats:
                    return license.config?.customerActions?.allowRemoteSeatRelease ?? false;

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

    private bool HasExpiration(LicenseData license)
    {
        // No expiration date = perpetual
        if (license.validUntil == null || license.validUntil == DateTime.MinValue)
        {
            return false;
        }
        
        // Very far future = perpetual
        if (license.validUntil > DateTime.Now.AddYears(50))
        {
            return false;
        }
        
        return true;
    }

    private bool IsSubscription(LicenseData license)
    {
        // Check for subscription indicators
        if (!string.IsNullOrEmpty(license.subscriptionStatus))
        {
            return true;
        }
        
        // Has billing date = subscription
        if (license.nextBillingDate != null && license.nextBillingDate != DateTime.MinValue)
        {
            return true;
        }
        
        // Check policy type
        string policyType = license.policy?.type?.ToLower();
        return policyType == "subscription" || policyType == "recurring";
    }
}
