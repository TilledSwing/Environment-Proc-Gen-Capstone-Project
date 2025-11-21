using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderUpdateInput : MonoBehaviour
{
    public TMP_InputField input;
    public Slider slider;
    public bool isFloat;

    public void UpdateInput()
    {
        input.text = isFloat ? slider.value.ToString("F2") : slider.value.ToString();
    }
}
