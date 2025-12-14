using UnityEngine;
using System.Collections;

public class AutoFireController : IWeaponFireController
{
    private readonly IWeapon weapon;
    private readonly MonoBehaviour runner;
    private Coroutine routine;

    public AutoFireController(IWeapon weapon, MonoBehaviour runner)
    {
        this.weapon = weapon;
        this.runner = runner;

        if (this.weapon == null)
            Debug.LogError("[AutoFireController] weapon is null");
        if (this.runner == null)
            Debug.LogError("[AutoFireController] runner MonoBehaviour is null");
    }

    public AutoFireController(IWeapon weapon) : this(weapon, weapon as MonoBehaviour) { }

    public void OnFirePressed()
    {
        if (runner == null || weapon == null) return;
        if (routine != null) return;
        routine = runner.StartCoroutine(FireLoop());
    }

    public void OnFireReleased()
    {
        if (runner == null) return;
        if (routine == null) return;
        runner.StopCoroutine(routine);
        routine = null;
    }

    public void Update() { }

    // ⬇⬇⬇ Додаємо перезарядку: зупиняємо цикл і просимо зброю перезарядитись
    public void OnReloadPressed()
    {
        if (runner != null && routine != null)
        {
            runner.StopCoroutine(routine);
            routine = null;
        }

        if (weapon != null)
            weapon.Reload();
    }

    private IEnumerator FireLoop()
    {
        while (true)
        {
            if (weapon != null && weapon.CanFire)
                weapon.Fire();

            float interval = Mathf.Max(0.01f, weapon != null ? weapon.FireRate : 0.2f);
            yield return new WaitForSeconds(interval);
        }
    }
}
