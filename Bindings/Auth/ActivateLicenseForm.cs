using System.Collections.Generic;
using LTLM.SDK.Unity;
using UnityEngine;

namespace LTLM.UI
{
    public class ActivateLicenseForm : FormBinding
    {
        public override void FormRequest(Dictionary<string, string> values)
        {
            if (LTLM.SDK.Unity.LTLMManager.Instance.IsAuthenticated)
            {
                // Already authenticated - this shouldn't happen normally
                SendCallback(true, null);
                return;
            }
            
            LTLMManager.Instance.ActivateLicense(values["licenseKey"], (license, status) =>
            {
                if (status != LicenseStatus.Active)
                {
                    SendCallback(false, new Dictionary<string, string>(new[] { new KeyValuePair<string, string>("licenseKey", "Activation Failed, License is currently " +  status.ToString()) }));
                    return; // BUG FIX: Was missing return, causing SendCallback(true) to be called after error
                }
                SendCallback(true, null);
            }, (error) =>
            {
                SendCallback(false, new Dictionary<string, string>(new[] { new KeyValuePair<string, string>("licenseKey", error) }));
            });
        }
    }
}