using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingTextAnimation : MonoBehaviour
{
    public TMP_Text LoadingText;
    public string baseText = "Loading";
    public float stepSeconds = 0.5f;

    private string[] _frames;
    private Coroutine _routine;

    private void Awake()
    {
        if (LoadingText == null) LoadingText = GetComponent<TMP_Text>();
        BuildFrames();
    }

    private void OnValidate() => BuildFrames();

    private void BuildFrames()
    {
        _frames = new[] { baseText, baseText + ".", baseText + "..", baseText + "..." };
    }

    private void OnEnable()
    {
        if (LoadingText == null) return;
        _routine = StartCoroutine(LoadingAnimation());
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
    }

    public IEnumerator LoadingAnimation()
    {
        int i = 0;
        var wait = new WaitForSecondsRealtime(stepSeconds);
        while (true)
        {
            LoadingText.text = _frames[i % _frames.Length]; // no per-frame string allocation
            i++;
            yield return wait;
        }
    }
}
