using System;
using System.Collections;
using System.Linq;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
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
        ControlPanel,
        SeatManagement,
    }

    /// <summary>
    /// Drives the drop-in LTLM UI: maps SDK events to pages and error screens.
    /// </summary>
    public class LTLMUIManager : MonoBehaviour
    {
        #region values

        /// <summary>Singleton instance of the LTLMUIManager.</summary>
        public static LTLMUIManager Instance { get; private set; }

        private bool _isForcingSignout;
        private LTLMUIPages? _currentPage;
        private bool _uiHidden;

        #endregion

        #region settings

        [Tooltip("One entry per LTLMUIPages value, in enum order.")]
        public GameObject[] Pages;
        public GameObject[] Commons;
        public UITextInjector WelcomeInjector;
        public UITextInjector ErrorInjector;

        [Tooltip("0 = Reclaim seat, 1 = New license, 2 = Buy license, 3 = Buy top-up")]
        public Button[] ErrorsButtons;

        [Header("Project Settings")] public bool ShouldShowStartPage = false;
        public bool startWithCustomerLogin = true;

        [Tooltip("URL to redirect to after buy complete, use your site. It receives session information so you can verify the purchase.")]
        public string BuyCompleteRedirectURL = "";

        [Tooltip("Seconds to wait before quitting after a terminated/tampered notice.")]
        public float quitDelaySeconds = 3f;

        [Header("Enabled Features")] public FeatureBinding features =
            FeatureBinding.Login |
            FeatureBinding.Airgapped |
            FeatureBinding.Shop |
            FeatureBinding.ControlSettings;

        #endregion

        #region UnityLifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetActivePage(LTLMUIPages.Loading);

            // If the SDK already finished (e.g. autoValidateOnStart ran before this UI was enabled), catch up.
            var manager = LTLMManager.Instance;
            if (manager != null && !manager.IsValidating && manager.GetLicenseStatus() != LicenseStatus.Unauthenticated)
                OnValidationCompleted(manager.IsAuthenticated, manager.GetLicenseStatus());
        }

        private void OnEnable()
        {
            LTLMManager.OnValidationStarted += OnValidationStarted;
            LTLMManager.OnValidationCompleted += OnValidationCompleted;
            LTLMManager.OnLicenseStatusChanged += OnLicenseStatusChanged;
            LTLMManager.OnSeatStatusChanged += OnSeatStatusChanged;
            LTLMManager.OnKicked += OnKicked;
        }

        private void OnDisable()
        {
            LTLMManager.OnValidationStarted -= OnValidationStarted;
            LTLMManager.OnValidationCompleted -= OnValidationCompleted;
            LTLMManager.OnLicenseStatusChanged -= OnLicenseStatusChanged;
            LTLMManager.OnSeatStatusChanged -= OnSeatStatusChanged;
            LTLMManager.OnKicked -= OnKicked;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Public LTLMManager Interface

        public void ReclaimSeat()
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                ShowSdkUnavailable();
                return;
            }
            SetActivePage(LTLMUIPages.Loading);
            manager.ReactivateSeat(null, error =>
            {
                ShowError("Could not reclaim the seat: " + error, reclaim: true, newLicense: true, buyLicense: IsShop, buyTopups: HasSeatTopups && IsShop);
            });
        }

        /// <summary>Releases this device's seat (the license stays on the device).</summary>
        public void DeactivateSeat()
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                ShowSdkUnavailable();
                return;
            }
            _isForcingSignout = true;
            SetActivePage(LTLMUIPages.Loading);
            manager.DeactivateSeat(
                () => { _isForcingSignout = false; ShowEntryPage(); },
                error => { _isForcingSignout = false; ShowError(error, reclaim: true, newLicense: false, buyLicense: false, buyTopups: false); });
        }

        /// <summary>Signs out completely: releases the seat and wipes the local license.</summary>
        public void RemoveLicenseData()
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                ShowSdkUnavailable();
                return;
            }
            _isForcingSignout = true;
            SetActivePage(LTLMUIPages.Loading);
            manager.SignOut(() =>
            {
                _isForcingSignout = false;
                ShowEntryPage();
            });
        }

        public void OpenActivateLicenseForm() => SetActivePage(LTLMUIPages.ActivateLicense);
        public void OpenLoginPage() => SetActivePage(LTLMUIPages.Login);
        public void OpenControlPanel() => SetActivePage(LTLMUIPages.ControlPanel);
        public void OpenSeatManagement() => SetActivePage(LTLMUIPages.SeatManagement);

        /// <summary>Opens the checkout page for a new license of the given policy in the system browser.</summary>
        public void BuyPolicy(string policyId, string email)
        {
            LTLMManager.Instance?.CreateCheckoutSession(policyId, email, BuyCompleteRedirectURL,
                Application.OpenURL,
                error => ShowError("Could not open checkout: " + error, false, true, false, false));
        }

        /// <summary>Opens the checkout page for the given top-up pack in the system browser.</summary>
        public void BuyTopUp(string packId)
        {
            LTLMManager.Instance?.CreateTopUpSession(packId, BuyCompleteRedirectURL,
                Application.OpenURL,
                error => ShowError("Could not open checkout: " + error, false, false, false, false));
        }

        #endregion

        #region Callbacks

        private bool IsShop => features.HasFlag(FeatureBinding.Shop);

        private bool HasSeatTopups =>
            LTLMManager.Instance?.ActiveLicense?.policy?.ResolvedTopUpOptions.Any(x => x.seats != 0) ?? false;

        private bool HasTimeTopups =>
            LTLMManager.Instance?.ActiveLicense?.policy?.ResolvedTopUpOptions.Any(x => x.days != 0) ?? false;

        private void OnKicked(KickedNotice notice)
        {
            string by = notice?.kickedByNickname ?? "another device";
            ShowError($"Your seat was taken by {by}.", reclaim: true, newLicense: true, buyLicense: IsShop, buyTopups: HasSeatTopups && IsShop);
        }

        private void OnSeatStatusChanged(string status, int currentSeats, int maxSeats)
        {
            switch (status)
            {
                case "RELEASED":
                    if (_isForcingSignout) return;
                    ShowError("You have been released from this seat.", reclaim: true, newLicense: true, buyLicense: IsShop, buyTopups: HasSeatTopups && IsShop);
                    break;

                case "KICKED":
                    // OnKicked carries the details; only handle the case without a notice.
                    if (LTLMManager.Instance?.ActiveLicense?.kickedNotice == null)
                        ShowError("You have been kicked from this seat.", reclaim: true, newLicense: true, buyLicense: IsShop, buyTopups: HasSeatTopups && IsShop);
                    break;

                case "NO_SEAT":
                    ShowError($"All {maxSeats} seats are in use.", reclaim: true, newLicense: true, buyLicense: IsShop, buyTopups: HasSeatTopups && IsShop);
                    break;
            }
        }

        private void OnLicenseStatusChanged(LicenseStatus status)
        {
            switch (status)
            {
                case LicenseStatus.Tampered:
                    ShowError("License data was tampered with. Reset your device and try again.", false, true, false, false);
                    ForceSignOutAndQuit();
                    break;

                case LicenseStatus.Expired:
                    ShowError("Your license has expired. Renew it or buy a time top-up.", false, true, IsShop, HasTimeTopups && IsShop);
                    break;

                case LicenseStatus.GracePeriod:
                    // Still usable: keep the app visible, surface the warning through BindActiveToLicenseFeature(GracePeriod).
                    if (_uiHidden) return;
                    HideUI();
                    break;

                case LicenseStatus.OfflineGrace:
                    if (_uiHidden) return;
                    HideUI();
                    break;

                case LicenseStatus.Terminated:
                    ShowError("This license has been terminated. The application will close.", false, false, false, false);
                    ForceSignOutAndQuit();
                    break;

                case LicenseStatus.Revoked:
                    ShowError("This license was revoked by the administrator.", false, true, IsShop, false);
                    ForceSignOut();
                    break;

                case LicenseStatus.Suspended:
                    ShowError("This license has been suspended. Contact support.", false, true, false, false);
                    ForceSignOut();
                    break;

                case LicenseStatus.ConnectionRequired:
                    ShowError("An internet connection is required to continue.", true, false, false, false);
                    break;

                case LicenseStatus.Unauthenticated:
                    if (!_isForcingSignout && !(LTLMManager.Instance?.IsValidating ?? false)) ShowEntryPage();
                    break;
            }
        }

        private void OnValidationStarted()
        {
            SetActivePage(LTLMUIPages.Loading);
        }

        private void OnValidationCompleted(bool success, LicenseStatus status)
        {
            if (success)
            {
                switch (status)
                {
                    case LicenseStatus.Active:
                    case LicenseStatus.GracePeriod:
                    case LicenseStatus.OfflineGrace:
                        if (ShouldShowStartPage)
                        {
                            var license = LTLMManager.Instance?.ActiveLicense;
                            string name = null;
                            if (license?.customer?.metadata != null && license.customer.metadata.TryGetValue("name", out var n))
                                name = n?.ToString();
                            if (string.IsNullOrEmpty(name)) name = license?.customer?.email ?? "there";
                            WelcomeInjector?.WriteText(name);
                            SetActivePage(LTLMUIPages.Start);
                        }
                        else
                        {
                            HideUI();
                        }
                        break;

                    case LicenseStatus.ValidNoSeat:
                        ShowError("All license seats are currently in use.", true, true, IsShop, HasSeatTopups && IsShop);
                        break;

                    case LicenseStatus.Kicked:
                        ShowError("Your seat is no longer available on this device.", true, true, IsShop, HasSeatTopups && IsShop);
                        break;

                    default:
                        ShowError("The license could not be used (" + status + ").", false, true, IsShop, false);
                        break;
                }
                return;
            }

            if (status == LicenseStatus.Kicked || status == LicenseStatus.Tampered || status == LicenseStatus.ConnectionRequired)
                return; // handled by status/seat callbacks

            ShowEntryPage();
        }

        #endregion

        #region Helpers

        private void ForceSignOut()
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                _isForcingSignout = false;
                return;
            }
            _isForcingSignout = true;
            manager.SignOut(() => _isForcingSignout = false);
        }

        private void ForceSignOutAndQuit()
        {
            ForceSignOut();
            StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSecondsRealtime(quitDelaySeconds);
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        /// <summary>Shows the login page or the activation form depending on the configuration.</summary>
        public void ShowEntryPage()
        {
            if (startWithCustomerLogin && features.HasFlag(FeatureBinding.Login))
                SetActivePage(LTLMUIPages.Login);
            else
                SetActivePage(LTLMUIPages.ActivateLicense);
        }

        private void ShowError(string message, bool reclaim, bool newLicense, bool buyLicense, bool buyTopups)
        {
            ErrorInjector?.WriteText(message);
            EnableErrorButtons(reclaim, newLicense, buyLicense, buyTopups);
            SetActivePage(LTLMUIPages.Error);
        }

        private void ShowSdkUnavailable()
        {
            ShowError("The LTLM SDK is not available. Add an LTLM Manager to the startup scene.", false, false, false, false);
        }

        public void SetActivePage(LTLMUIPages page)
        {
            _uiHidden = false;
            if (_currentPage == page) return;
            _currentPage = page;

            if (Commons != null)
                for (int i = 0; i < Commons.Length; i++)
                    if (Commons[i] != null && !Commons[i].activeSelf) Commons[i].SetActive(true);

            if (Pages == null) return;
            int index = (int)page;
            if (index >= Pages.Length || Pages[index] == null)
            {
                Debug.LogError($"[LTLM UI] No page assigned for {page}. Assign it on the LTLMUIManager.");
            }

            for (int i = 0; i < Pages.Length; i++)
            {
                if (Pages[i] == null) continue;
                bool active = i == index;
                if (Pages[i].activeSelf != active) Pages[i].SetActive(active);
            }
        }

        private void EnableErrorButtons(bool reclaim, bool newLicense, bool buyNewLicense, bool buyTopups)
        {
            if (ErrorsButtons == null) return;
            SetButton(0, reclaim);
            SetButton(1, newLicense);
            SetButton(2, buyNewLicense);
            SetButton(3, buyTopups);
        }

        private void SetButton(int index, bool active)
        {
            if (index >= ErrorsButtons.Length || ErrorsButtons[index] == null) return;
            var go = ErrorsButtons[index].gameObject;
            if (go.activeSelf != active) go.SetActive(active);
        }

        public void HideUI()
        {
            // First line of defence: the UI can only be dismissed while the license grants access.
            if (LTLMManager.Instance == null || !LTLMManager.Instance.IsAuthenticated) return;

            _uiHidden = true;
            _currentPage = null;

            if (Pages != null)
                foreach (var p in Pages)
                    if (p != null && p.activeSelf) p.SetActive(false);

            if (Commons != null)
                foreach (var c in Commons)
                    if (c != null && c.activeSelf) c.SetActive(false);
        }

        #endregion
    }
}
