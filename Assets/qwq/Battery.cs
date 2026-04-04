using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battery : MonoBehaviour, IWeapon
{
    public List<IDamageable> enemys = new();
    public float t_max = 1;
    float t;
    public GameObject bullet;
    private void Update()
    {
        if (t > t_max)
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
    public void Fire(IDamageable target)
    {
        GameObject newBullet = Instantiate(bullet, transform.position, transform.localRotation);
        newBullet!.GetComponent<IWeapon>().Fire(target);

    }
}
