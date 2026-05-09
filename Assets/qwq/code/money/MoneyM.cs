using TMPro;
using UnityEngine;

public class MoneyM : MonoBehaviour
{
    [SerializeField] TMP_Text fertilizer_text;


    public void UiUpdate(int fertilizer, int diamond)
    {
        fertilizer_text.text = "肥料" + fertilizer;
    }

}
