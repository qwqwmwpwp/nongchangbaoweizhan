using TMPro;
using UnityEngine;

public class MoneyM : MonoBehaviour
{
    [SerializeField] TMP_Text fertilizer_text;
    [SerializeField] TMP_Text diamond_text;

    public void UiUpdate(int fertilizer, int diamond)
    {
        fertilizer_text.text = "∑ ¡œ£ª" + fertilizer;
        diamond_text.text = "◊Í Ø£ª" + diamond;

    }

}
