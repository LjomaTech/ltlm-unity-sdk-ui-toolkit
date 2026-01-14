using System;
using System.Collections;
using System.Collections.Generic;
using LTLM.SDK.Unity;
using UnityEngine;
using UnityEngine.Events;

namespace LTLM.UI
{
    public class LoginForm : FormBinding
    {

        public UnityEvent<string> EmailCallbackUIHandling;
        public override void FormRequest(Dictionary<string, string> values)
        {
            if (LTLM.SDK.Unity.LTLMManager.Instance.IsAuthenticated)
            {
                // Already authenticated - proceed directly
                SendCallback(true, null);
                EmailCallbackUIHandling?.Invoke(values["email"]);
                return;
            }
            
            LTLMManager.Instance.RequestOTP(values["email"], () =>
            {
                SendCallback(true, null);
                EmailCallbackUIHandling?.Invoke(values["email"]);
            }, (error) =>
            {
                SendCallback(false, new Dictionary<string, string>(new[] { new KeyValuePair<string, string>("email", error) }));
            });
        }
    }
}