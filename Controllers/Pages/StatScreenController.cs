using LTLM.UI;
using UnityEngine;

/// <summary>
/// Makes sure every feature-bound element under the stats screen evaluates its condition when the screen opens.
/// </summary>
public class StatScreenController : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var bindActive in GetComponentsInChildren<BindActiveToLicenseFeature>(true))
        {
            if (!bindActive.gameObject.activeSelf) bindActive.gameObject.SetActive(true);
        }
        LTLMUIRefresher.MarkDirty();
    }
}
