using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantGenerateM : MonoBehaviour
{
    [SerializeField] Button plant1;
    [SerializeField] Button plant2;

    public static PlantGenerateM instance;
    [SerializeField] GameObject PlantGenerateUI;
    private void Awake()
    {
        if (instance)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }
    private void Start()
    {
        plant1.onClick.AddListener(ButtonUI);
    }
    public void enterUI(Vector2 position)
    {
        Debug.Log(position);
        PlantGenerateUI.SetActive(true);
        RectTransform rectTransform = PlantGenerateUI.GetComponent<RectTransform>();
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(position);
      rectTransform.transform.position = screenPosition;
    }
    public void ButtonUI()
    {
        PlantGenerateC.instance.PlantGenerated();
        PlantGenerateUI.SetActive(false);
    }
}
