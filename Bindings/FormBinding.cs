using System.Collections.Generic;
using LjomaAssets.FormManagement;
using UnityEngine;
using UnityEngine.Events;

namespace LTLM.UI
{
    /// <summary>
    /// Bridges a LjomaTech <see cref="FormManager"/> submit to an LTLM SDK call.
    /// </summary>
    public abstract class FormBinding : MonoBehaviour
    {
        public FormManager FormManager;
        public UnityEvent OnSuccess;
        public UnityEvent<string> OnFailure;

        public abstract void FormRequest(Dictionary<string, string> values);

        public virtual void SendCallback(bool success, Dictionary<string, string> errors)
        {
            if (FormManager != null) FormManager.OnLoadingFinished(success, errors);

            if (success)
            {
                OnSuccess?.Invoke();
                return;
            }

            string first = null;
            if (errors != null)
                foreach (var kv in errors) { first = kv.Value; break; }
            OnFailure?.Invoke(first ?? "Request failed");
        }

        protected static Dictionary<string, string> Error(string field, string message) =>
            new Dictionary<string, string> { { field, message } };

        protected static string Get(Dictionary<string, string> values, string key) =>
            values != null && values.TryGetValue(key, out var v) ? v?.Trim() : null;
    }
}
