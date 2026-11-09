using System.Collections.Generic;
using LTLM.SDK.Core.Models;
using UnityEngine;

namespace LTLM.UI
{
    /// <summary>
    /// Shows the customer's licenses (and buyable policies when the Shop feature is on) after OTP login.
    /// Card instances are pooled and reused between refreshes instead of destroyed/instantiated.
    /// </summary>
    public class SelectLicensePage : MonoBehaviour
    {
        public LoginPageController loginPageController;
        public GameObject licenseCardPrefab;
        public GameObject policyCardPrefab;
        public Transform licenseCardsParent;

        private string SavedEmail;
        private readonly List<LicenseCard> _licenseCards = new List<LicenseCard>();
        private readonly List<PolicyCard> _policyCards = new List<PolicyCard>();

        public void SetupEmail(string email)
        {
            SavedEmail = email;
        }

        public void SetupLicenseCardsPage(List<LicenseData> licenseDatas)
        {
            licenseDatas = licenseDatas ?? new List<LicenseData>();

            for (int i = 0; i < licenseDatas.Count; i++)
            {
                var card = GetOrCreate(_licenseCards, licenseCardPrefab, i);
                if (card == null) continue;
                card.gameObject.SetActive(true);
                card.transform.SetSiblingIndex(i);
                card.SetupLicense(licenseDatas[i]);
            }
            for (int i = licenseDatas.Count; i < _licenseCards.Count; i++)
                _licenseCards[i].gameObject.SetActive(false);

            bool shop = LTLMUIManager.Instance != null && LTLMUIManager.Instance.features.HasFlag(FeatureBinding.Shop);
            var policies = shop && loginPageController != null ? loginPageController.BuyablePolicies : null;
            int policyCount = policies?.Count ?? 0;

            for (int i = 0; i < policyCount; i++)
            {
                var card = GetOrCreate(_policyCards, policyCardPrefab, i);
                if (card == null) continue;
                card.gameObject.SetActive(true);
                card.transform.SetAsLastSibling();
                card.SetupPolicy(policies[i], SavedEmail);
            }
            for (int i = policyCount; i < _policyCards.Count; i++)
                _policyCards[i].gameObject.SetActive(false);
        }

        private T GetOrCreate<T>(List<T> pool, GameObject prefab, int index) where T : Component
        {
            if (index < pool.Count) return pool[index];
            if (prefab == null || licenseCardsParent == null) return null;
            var go = Instantiate(prefab, licenseCardsParent);
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogError($"[LTLM UI] Prefab {prefab.name} has no {typeof(T).Name} component.");
                Destroy(go);
                return null;
            }
            pool.Add(comp);
            return comp;
        }
    }
}
