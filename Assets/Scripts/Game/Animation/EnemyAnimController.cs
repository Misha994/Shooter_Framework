// Assets/Scripts/Game/Animation/EnemyAnimController.cs
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimController : CharacterAnimController
{
    private NavMeshAgent _agent;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        float speed = (_agent && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;
        SetLocomotion(speed, 0, speed, true);

        // Aim/IsAiming можеш керувати через IInputService (AIInputService) або через сигнал/параметр аніматора окремо.
    }

    public override void CompleteReload()
    {
        var w = GetComponentInChildren<WeaponBase>();
        if (w?.Ammo != null) w.Ammo.TryReload();
    }
}
