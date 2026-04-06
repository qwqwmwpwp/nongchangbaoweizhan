using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battery : MonoBehaviour, IWeapon
{
    public List<IDamageable> enemys = new();

    [SerializeField] private BatteryDataSO batteryData;
    private float fireInterval = 1f;
    float t;
    public GameObject bullet;

    private void Awake()
    {
        if (batteryData != null)
            fireInterval = batteryData.FireInterval;
    }

    private void Update()
    {
        if (t > fireInterval)
        {
            if (enemys.Count < 1) return;
            foreach (var enemy in enemys)
                if (enemy == null) enemys.RemoveAt(0);
            t = 0;
            Fire(enemys[0]);
        }
        else
        {
            t += Time.deltaTime;
        }
    }

    private void OnValidate()
    {
        if (batteryData != null)
            fireInterval = batteryData.FireInterval;
    }
    public void Fire(IDamageable target)
    {
        GameObject newBullet = Instantiate(bullet, transform.position, transform.localRotation);
        newBullet!.GetComponent<IWeapon>().Fire(target);

    }
}
