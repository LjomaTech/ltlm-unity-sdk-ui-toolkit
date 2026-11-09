using TMPro;
using UnityEngine;

public class UITextInjector : MonoBehaviour
{
    [Header("Use $value to inject the value you send into the text")]
    public string TextValueRef;
    private TMP_Text _target;

    private void Awake()
    {
        _target = GetComponent<TMP_Text>();
    }

    public void WriteText(string value)
    {
        if (_target == null) _target = GetComponent<TMP_Text>();
        if (_target == null) return;
        _target.text = (TextValueRef ?? string.Empty).Replace("$value", value ?? string.Empty);
    }
}
