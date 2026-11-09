using System.Collections.Generic;
using LTLM.SDK.Core.Models;
using LTLM.SDK.Unity;
using UnityEngine.Events;

namespace LTLM.UI
{
    public class OTPForm : FormBinding
    {
        public UnityEvent<List<LicenseData>> OnSuccessListCallback;
        public UnityEvent<string> OnSuccessEmailCallback;

        public override void FormRequest(Dictionary<string, string> values)
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                SendCallback(false, Error("otp", "LTLM SDK is not initialised."));
                return;
            }

            string email = Get(values, "email");
            string otp = Get(values, "otp");
            if (string.IsNullOrEmpty(otp))
            {
                SendCallback(false, Error("otp", "Please enter the verification code."));
                return;
            }

            manager.VerifyOTP(email, otp, list =>
            {
                OnSuccessEmailCallback?.Invoke(email);
                SendCallback(true, null);
                OnSuccessListCallback?.Invoke(list ?? new List<LicenseData>());
            }, error =>
            {
                SendCallback(false, Error("otp", error));
            });
        }
    }
}
