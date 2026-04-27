using qwq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlantGenerateM : MonoBehaviour
{
    [Header("布局设置")]
    [SerializeField] private float radius = 200f;     // 圆的半径


    [SerializeField] List<PantGenerateButton> plantButton;
    [SerializeField] GameObject PlantGenerateUI;
    [SerializeField] GameObject buttonObj;
    private void Awake()
    {
        PlantGenerateUI.SetActive(false);
    }
    private void Start()
    {

    }

    public void enterUI(Vector2 position, List<GameObject> plants)
    {
        if (plantButton.Count < plants.Count)
        {
            for (int i = plantButton.Count; i < plants.Count; i++)
            {
                GameObject newObj = Instantiate(buttonObj,PlantGenerateUI.transform);
                PantGenerateButton newButton = newObj.GetComponent<PantGenerateButton>();
                plantButton.Add(newButton);
            }

            List<(Sprite sprite, int monmy)> values = new();
            foreach (GameObject obj in plants)
            {
                PlantsCtx ctx = obj.GetComponent<Plants>().plantsCtx;
                values.Add((ctx.UI, ctx.fertilizer));
            }

            InitializeUI(values);
        }

        PlantGenerateUI.SetActive(true);
        RectTransform rectTransform = PlantGenerateUI.GetComponent<RectTransform>();
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(position);
        rectTransform.transform.position = screenPosition;
    }

    public void InitializeUI(List<(Sprite sprite, int monmy)> values)
    {
        if (plantButton == null || plantButton.Count == 0) return;

        for (int i = 0; i < plantButton.Count; i++)
        {
            int index = i;
            plantButton[i].button.onClick.AddListener(()=>ButtonUI(index));
            plantButton[i].text.text = "肥料" + values[i].monmy;
            plantButton[i].image.sprite = values[i].sprite;
        }
        Vector2 center = Vector2.zero;// 中心点

        int count = plantButton.Count;
        float angleStep = 360f / count;  // 每个元素的角度间隔

        for (int i = 0; i < count; i++)
        {
            RectTransform element = plantButton[i].GetComponent<RectTransform>();
            if (element == null) continue;

            // 计算当前元素的角度（弧度）
            float angle = i * angleStep * Mathf.Deg2Rad;

            // 计算圆形坐标
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;

            // 设置UI位置
            element.anchoredPosition = new Vector2(x, y);
        }
    }

    private void ButtonUI(int type)
    {
        PlantGenerateC.instance.PlantGenerated(type);
        PlantGenerateUI.SetActive(false);
    }
}
