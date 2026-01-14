using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace LTLM.UI
{
    public enum LTLMUIPages
    {
        Loading,
        Login,
        Start,
        Error,
        ActivateLicense,
        Shop,
        SeatManagement,
    }

    public class LTLMUIManager : MonoBehaviour
    {
        #region values

        /// <summary>
        /// Singleton instance of the LTLMUIManager.
        /// Access via <c>LTLMUIManager.Instance</c> after initialization.
        /// </summary>
        public static LTLMUIManager Instance { get; private set; }
        private bool isForcingSignout = false;

        #endregion

        #region settings

        public GameObject[] Pages;
        public GameObject[] Commons;
        public UITextInjector ErrorInjector;
        public Button[] ErrorsButtons;

        [Header("Project Settings")] public bool ShouldShowStartPage = false;
        public bool startWithCustomerLogin = true;
        
        [Tooltip("URL to redirect to after buy complete, use your site. it sends some information also if want to use API to verify it using your own interface!")]
        public string BuyCompleteRedirectURL = "";
        
        [Header("Enabled Features")] public FeatureBinding features =
        FeatureBinding.Login |
        FeatureBinding.Airgapped |
        FeatureBinding.Shop |
        FeatureBinding.Logout;
        
        #endregion


        #region UnityLifecycle

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // should be in app start.
            SetActivePage(LTLMUIPages.Loading);
        }
        
        private void OnEnable()
        {
            LTLMManager.OnValidationStarted += LTLMManagerOnOnValidationStarted;
            LTLMManager.OnValidationCompleted += LTLMManagerOnOnValidationCompleted;
            LTLMManager.OnLicenseStatusChanged += LTLMManagerOnOnLicenseStatusChanged;
            LTLMManager.OnTokensConsumed += LTLMManagerOnOnTokensConsumed;
            LTLMManager.OnSeatStatusChanged += LTLMManagerOnOnSeatStatusChanged;
        }

        private void OnDisable()
        {
            LTLMManager.OnValidationStarted -= LTLMManagerOnOnValidationStarted;
            LTLMManager.OnValidationCompleted -= LTLMManagerOnOnValidationCompleted;
            LTLMManager.OnLicenseStatusChanged -= LTLMManagerOnOnLicenseStatusChanged;
            LTLMManager.OnTokensConsumed -= LTLMManagerOnOnTokensConsumed;
            LTLMManager.OnSeatStatusChanged -= LTLMManagerOnOnSeatStatusChanged;
        }

        #endregion

        #region Public LTLMManager Interface

        public void ReclaimSeat()
        {
            LTLMManager.Instance.ReactivateSeat();
        }

        public void DeactivateSeat()
        {
            LTLMManager.Instance.DeactivateSeat();
        }

        public void RemoveLicenseData()
        {
            LTLMManager.Instance.DeactivateSeat();
            LTLMManager.Instance.ClearLicenseCache();
            LTLMManager.Instance.ValidateLicense(null, (data, status) => { }, (error) => { });
        }

        public void OpenActivateLicenseForm()
        {
            SetActivePage(LTLMUIPages.ActivateLicense);
        }

        public void OpenLoginPage()
        {
            SetActivePage(LTLMUIPages.Login);
        }

        #endregion

        #region Callbacks

        private void LTLMManagerOnOnSeatStatusChanged(string status, int currentSeats, int maxSeats)
        {
            var license = LTLMManager.Instance.ActiveLicense;
            var topups = license?.policy?.config?.customerActions?.topUpOptions;
            var haveTopupForSeats = topups?.Any(x => x.seats != 0) ?? false;
            var isSellable = license?.policy?.config?.visibility?.storefront ?? false;
            
            if (status == "RELEASED" && isForcingSignout == false)
            {
                ErrorInjector.WriteText("You have been released from this seat.");
                SetActivePage(LTLMUIPages.Error);
                EnableErrorButtons(true, true, isSellable, haveTopupForSeats && isSellable);
            }
            else if (status == "KICKED")
            {
                ErrorInjector.WriteText("You have been kicked from this seat from portal.");
                SetActivePage(LTLMUIPages.Error);
                EnableErrorButtons(true, true, isSellable, haveTopupForSeats && isSellable);
            }
            else if (status == "NO_SEAT")
            {
                ErrorInjector.WriteText("You have exceeded the maximum number of seats.");
                EnableErrorButtons(true, true, isSellable, haveTopupForSeats && isSellable);
                SetActivePage(LTLMUIPages.Error);
            }
        }

        private void LTLMManagerOnOnTokensConsumed(LicenseData obj)
        {
        }

        private void LTLMManagerOnOnLicenseStatusChanged(LicenseStatus status)
        {
            switch (status)
            {
                case LicenseStatus.Tampered:
                    ErrorInjector.WriteText("License was tampered. Reset your device and try again.");
                    SetActivePage(LTLMUIPages.Error);
                    isForcingSignout = true;
                    LTLMManager.Instance.DeactivateSeat();
                    LTLMManager.Instance.ClearLicenseCache();
                    StartCoroutine(CloseDestory());
                    EnableErrorButtons(false, true, false, false);
                    break;
                case LicenseStatus.Expired:
                    ErrorInjector.WriteText("License is expired. Buy a new license or a time Topup.");
                    SetActivePage(LTLMUIPages.Error);
                    var expLicense = LTLMManager.Instance.ActiveLicense;
                    var expTopups = expLicense?.policy?.config?.customerActions?.topUpOptions;
                    var haveTopupForDays = expTopups?.Any(x => x.days != 0) ?? false;
                    var expIsSellable = expLicense?.policy?.config?.visibility?.storefront ?? false;
                    EnableErrorButtons(false, true, expIsSellable, haveTopupForDays && expIsSellable);
                    break;
                    
                case LicenseStatus.GracePeriod:
                    // User is in offline grace period - show notification but allow usage
                    Debug.Log("[LTLM UI] Running in offline grace period.");
                    // Don't interrupt the user, but they should know they're offline
                    // Optionally show a non-blocking toast/banner (depends on UI implementation)
                    break;
                case LicenseStatus.Terminated:
                    ErrorInjector.WriteText("Termination Notice has been sent. Application will close now");
                    SetActivePage(LTLMUIPages.Error);
                    isForcingSignout = true;
                    LTLMManager.Instance.DeactivateSeat();
                    LTLMManager.Instance.ClearLicenseCache();
                    StartCoroutine(CloseDestory());
                    EnableErrorButtons(false, true, false, false);

                    break;

                case LicenseStatus.Revoked:
                    ErrorInjector.WriteText("License was revoked from administrator.");
                    SetActivePage(LTLMUIPages.Error);
                    isForcingSignout = true;
                    LTLMManager.Instance.DeactivateSeat();
                    LTLMManager.Instance.ClearLicenseCache();
                    EnableErrorButtons(false, true, false, false);
                    break;

                case LicenseStatus.Suspended:
                    ErrorInjector.WriteText("License was suspended from organization management.");
                    SetActivePage(LTLMUIPages.Error);
                    isForcingSignout = true;
                    LTLMManager.Instance.DeactivateSeat();
                    LTLMManager.Instance.ClearLicenseCache();
                    EnableErrorButtons(false, true, false, false);
                    break;

                case LicenseStatus.ConnectionRequired:
                    // Smart way to tell him he got his offline grace, and needs connection to keep working.
                    ErrorInjector.WriteText("License requires internet connection.");
                    SetActivePage(LTLMUIPages.Error);
                    EnableErrorButtons(false, false, false, false);
                    break;
            }
        }

        private void LTLMManagerOnOnValidationStarted()
        {
            SetActivePage(LTLMUIPages.Loading);
        }

        private void LTLMManagerOnOnValidationCompleted(bool success, LicenseStatus status)
        {
            if (success)
            {
                switch (status)
                {
                    case LicenseStatus.Active:
                    case LicenseStatus.GracePeriod: // Also valid, just in grace mode
                        if (ShouldShowStartPage)
                            SetActivePage(LTLMUIPages.Start);
                        else
                        {
                            HideUI();
                        }
                        break;
                        
                    case LicenseStatus.ValidNoSeat:
                        // License is valid but no seat available - show seat management
                        ErrorInjector.WriteText("All seats are in use. Release a seat or wait for one to become available.");
                        SetActivePage(LTLMUIPages.Error);
                        var vnsLicense = LTLMManager.Instance.ActiveLicense;
                        var vnsTopups = vnsLicense?.policy?.config?.customerActions?.topUpOptions;
                        var haveTopupForSeats = vnsTopups?.Any(x => x.seats != 0) ?? false;
                        var vnsSellable = vnsLicense?.policy?.config?.visibility?.storefront ?? false;
                        EnableErrorButtons(true, false, vnsSellable, haveTopupForSeats && vnsSellable);
                        break;
                }
            }
            else
            {
                if(startWithCustomerLogin && (features & FeatureBinding.Login) != 0)
                    SetActivePage(LTLMUIPages.Login);
                else
                {
                    SetActivePage(LTLMUIPages.ActivateLicense);
                }
            }
        }

        #endregion

        #region Helpers

        IEnumerator CloseDestory()
        {
            yield return new WaitForSeconds(3);
            Application.Quit();
        }


        public void SetActivePage(LTLMUIPages page)
        {
            for (int i = 0; i < Commons.Length; i++)
            {
                Commons[i].SetActive(true);
            }

            for (int i = 0; i < Pages.Length; i++)
            {
                Pages[i].SetActive(false);
            }

            Pages[(int)page].SetActive(true);
        }

        private void EnableErrorButtons(bool Reclaim, bool NewLicense, bool BuyNewLicense, bool BuyTopups)
        {
            ErrorsButtons[0].gameObject.SetActive(Reclaim);
            ErrorsButtons[1].gameObject.SetActive(NewLicense);
            ErrorsButtons[2].gameObject.SetActive(BuyNewLicense);
            ErrorsButtons[3].gameObject.SetActive(BuyTopups);
        }

        public void HideUI()
        {
            // DO NOT ALLOW HIDING WHILE GAME IS NOT AUTHENTICATED, FIRST LINE OF DEFENSE IS NOT ALLOWING IT.
            if (!LTLMManager.Instance.IsAuthenticated)
            {
                return;
            }

            for (int i = 0; i < Pages.Length; i++)
            {
                Pages[i].SetActive(false);
            }

            for (int i = 0; i < Commons.Length; i++)
            {
                Commons[i].SetActive(false);
            }
        }

        #endregion
    }
}