using System.Collections.Generic;
using LTLM.SDK.Unity;
using UnityEngine.Events;

namespace LTLM.UI
{
    public class LoginForm : FormBinding
    {
        public UnityEvent<string> EmailCallbackUIHandling;

        public override void FormRequest(Dictionary<string, string> values)
        {
            var manager = LTLMManager.Instance;
            if (manager == null)
            {
                SendCallback(false, Error("email", "LTLM SDK is not initialised."));
                return;
            }

            string email = Get(values, "email");
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            {
                SendCallback(false, Error("email", "Please enter a valid email address."));
                return;
            }

            manager.RequestOTP(email, () =>
            {
                SendCallback(true, null);
                EmailCallbackUIHandling?.Invoke(email);
            }, error =>
            {
                SendCallback(false, Error("email", error));
            });
        }
    }
}
