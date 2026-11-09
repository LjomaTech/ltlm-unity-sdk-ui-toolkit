using System.Collections.Generic;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using UnityEngine;

namespace LTLM.UI
{
    /// <summary>
    /// Base class for UI components that mirror license state.
    /// Instead of every component subscribing to every SDK event and re-formatting on each one,
    /// components register here and are refreshed at most once per frame after any license change.
    /// </summary>
    public abstract class LTLMUIBinding : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            LTLMUIRefresher.Register(this);
        }

        protected virtual void OnDisable()
        {
            LTLMUIRefresher.Unregister(this);
        }

        /// <summary>Re-read license data and update the view. Called at most once per frame.</summary>
        public abstract void Refresh();
    }

    /// <summary>
    /// Single subscriber to LTLMManager events that coalesces refreshes for all registered bindings.
    /// </summary>
    public static class LTLMUIRefresher
    {
        private static readonly HashSet<LTLMUIBinding> _bindings = new HashSet<LTLMUIBinding>();
        private static readonly List<LTLMUIBinding> _scratch = new List<LTLMUIBinding>();
        private static bool _subscribed;
        private static bool _dirty;
        private static RefreshPump _pump;

        public static void Register(LTLMUIBinding binding)
        {
            if (binding == null) return;
            _bindings.Add(binding);
            EnsureSubscribed();
            // A newly enabled binding must show current data right away.
            binding.Refresh();
        }

        public static void Unregister(LTLMUIBinding binding)
        {
            if (binding == null) return;
            _bindings.Remove(binding);
            if (_bindings.Count == 0) TearDown();
        }

        /// <summary>Marks every binding dirty; they refresh in the next LateUpdate.</summary>
        public static void MarkDirty()
        {
            _dirty = true;
            if (_pump == null)
            {
                var go = new GameObject("LTLM_UI_RefreshPump") { hideFlags = HideFlags.HideAndDontSave };
                Object.DontDestroyOnLoad(go);
                _pump = go.AddComponent<RefreshPump>();
            }
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;
            LTLMManager.OnLicenseUpdated += OnLicenseUpdated;
            LTLMManager.OnLicenseStatusChanged += OnStatusChanged;
            LTLMManager.OnValidationCompleted += OnValidationCompleted;
            LTLMManager.OnTokensConsumed += OnLicenseUpdated;
            LTLMManager.OnSeatStatusChanged += OnSeatChanged;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TearDown();
            _bindings.Clear();
            _scratch.Clear();
            _dirty = false;
        }

        private static void TearDown()
        {
            if (_subscribed)
            {
                LTLMManager.OnLicenseUpdated -= OnLicenseUpdated;
                LTLMManager.OnLicenseStatusChanged -= OnStatusChanged;
                LTLMManager.OnValidationCompleted -= OnValidationCompleted;
                LTLMManager.OnTokensConsumed -= OnLicenseUpdated;
                LTLMManager.OnSeatStatusChanged -= OnSeatChanged;
                _subscribed = false;
            }

            _dirty = false;
            if (_pump == null) return;
            if (Application.isPlaying) Object.Destroy(_pump.gameObject);
            else Object.DestroyImmediate(_pump.gameObject);
            _pump = null;
        }

        private static void OnLicenseUpdated(LicenseData _) => MarkDirty();
        private static void OnStatusChanged(LicenseStatus _) => MarkDirty();
        private static void OnValidationCompleted(bool _, LicenseStatus __) => MarkDirty();
        private static void OnSeatChanged(string _, int __, int ___) => MarkDirty();

        internal static void Flush()
        {
            if (!_dirty) return;
            _dirty = false;

            _scratch.Clear();
            _scratch.AddRange(_bindings);
            for (int i = 0; i < _scratch.Count; i++)
            {
                var b = _scratch[i];
                if (b != null && b.isActiveAndEnabled) b.Refresh();
            }
        }

        private class RefreshPump : MonoBehaviour
        {
            private void LateUpdate() => Flush();
        }
    }
}
