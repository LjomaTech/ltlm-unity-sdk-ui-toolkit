using System.Collections.Generic;
using LTLM.SDK.Unity;

namespace LTLM.UI
{
    public class ActivateLicenseForm : FormBinding
    {
        public override void FormRequest(Dictionary<string, string> values)
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                SendCallback(false, Error("licenseKey", "LTLM SDK is not initialised."));
                return;
            }

            string key = Get(values, "licenseKey");
            if (string.IsNullOrEmpty(key))
            {
                SendCallback(false, Error("licenseKey", "Please enter a license key."));
                return;
            }

            if (manager.IsAuthenticated && manager.ActiveLicense?.licenseKey == key)
            {
                SendCallback(true, null);
                return;
            }

            manager.ActivateLicense(key, (license, status) =>
            {
                if (status != LicenseStatus.Active && status != LicenseStatus.GracePeriod && status != LicenseStatus.OfflineGrace)
                {
                    SendCallback(false, Error("licenseKey", "Activation failed. License is currently " + status));
                    return;
                }
                SendCallback(true, null);
            }, error =>
            {
                SendCallback(false, Error("licenseKey", error));
            });
        }
    }
}
