using TMPro;
using UnityEngine;

public class RPGResponseBubble : MonoBehaviour
{
    public TextMeshProUGUI responseText;

    public void Show(string message)
    {
        responseText.text = message;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
