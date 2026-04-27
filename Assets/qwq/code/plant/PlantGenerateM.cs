using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlantGenerateM : MonoBehaviour
{
    [SerializeField] Button plant1;
    [SerializeField] Button plant2;
    [SerializeField] GameObject PlantGenerateUI;
    private void Awake()
    {
        plant1.onClick.AddListener(ButtonUI);
        plant2.onClick.AddListener(ButtonUI);
        PlantGenerateUI.SetActive(false);
    }
    private void Start()
    {
        
    }
    public void enterUI(Vector2 position)
    {
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
