using TMPro;
using UnityEngine;

public class InputTextManager : MonoBehaviour
{
    public TextMeshProUGUI text;
    public TMP_InputField input;
    public void SetInputText()
    {
        input.text = text.text;
    }
}
