using UnityEngine;
using System.Collections;
using NativeRTL;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Text DebugText;
    public InputField InputField;

    public void SetInputFieldText(string text)
    {
        InputField.text = text;
    }

    public void SetInputFieldRTLText(string text)
    {
        if (InputField is InputFieldRTLAdapter)
            ((InputFieldRTLAdapter)InputField).text = text;
    }

    public void PrintText()
    {
        DebugText.text = InputField.text;
    }
}
