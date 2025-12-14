// Assets/_Project/Code/Runtime/AI/AIInputService.cs
using Fogoyote.BFLike.AI;
using Fogoyote.BFLike.AI.Navigation;
using UnityEngine;

[DisallowMultipleComponent]
public class AIInputService : MonoBehaviour, IInputService
{
    [SerializeField] private AITacticalPlanner planner;
    [SerializeField] private NavToIntentAdapter nav;

    private Vector2 _move01;
    private bool _aim, _shootHeld, _jump, _sprint;
    private int _weaponSlotRequest = -1;

    private bool _shootHeldPrev, _shootPressedThisFrame, _shootReleasedThisFrame;

    private bool _reloadPressedThisFrame;
    private bool _wantReloadPrev;

    [SerializeField] private bool isLocal = false;
    [Header("Fallback")]
    [SerializeField] private float fallbackAggroRadius = 15f;

    private IHumanoidWeaponInventory _inv;

    // NEW: таймери для коротких реакцій
    private float _aimUntil;
    private float _shootUntil;

    private void Awake() { _inv = GetComponentInParent<IHumanoidWeaponInventory>(); }

    // NEW: публічні гачки
    public void SetAim(bool on, float autoReleaseAfterSeconds = 0f)
    {
        _aim = on;
        _aimUntil = on && autoReleaseAfterSeconds > 0f
            ? Time.time + autoReleaseAfterSeconds : 0f;
    }

    public void BurstShoot(float duration)
    {
        _shootHeld = true;
        _shootUntil = Time.time + Mathf.Max(0.05f, duration);
    }

    void Update()
    {
        _shootPressedThisFrame = _shootReleasedThisFrame = _reloadPressedThisFrame = false;

        // авто-відпуск гачків за таймерами
        if (_aimUntil > 0f && Time.time >= _aimUntil) { _aim = false; _aimUntil = 0f; }
        if (_shootUntil > 0f && Time.time >= _shootUntil) { _shootHeld = false; _shootUntil = 0f; }

        AIIntent intent = default;
        bool hadIntent = planner != null && planner.TryMakeIntent(out intent);

        if (hadIntent)
        {
            Vector3 dir = (nav != null) ? nav.DesiredMoveWorld : intent.MoveDir;

            Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = transform.right; right.y = 0; right.Normalize();
            _move01 = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd));

            bool atCover = (nav != null) && intent.WantsCover && nav.ReachedDestination;

            // не перезаписуємо короткий aim/burst, якщо вони активні
            if (_aimUntil <= 0f) _aim = atCover || intent.Aim;
            if (_shootUntil <= 0f) _shootHeld = atCover ? true : intent.Fire;

            _sprint = !atCover && intent.Sprint;

            if (intent.Target)
            {
                var to = intent.Target.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
            }
        }
        else
        {
            _move01 = Vector2.zero;
            _sprint = false;

            var player = FindClosestPlayer(fallbackAggroRadius);
            if (player)
            {
                var to = player.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);

                if (_aimUntil <= 0f) _aim = true;
                if (_shootUntil <= 0f) _shootHeld = true;
            }
            else
            {
                if (_aimUntil <= 0f) _aim = false;
                if (_shootUntil <= 0f) _shootHeld = false;
            }
        }

        // ====== АВТО-РЕЛОАД ДЛЯ NPC ======
        bool wantReload = false;
        var cur = _inv != null ? _inv.GetCurrentWeapon() : null;
        if (cur != null && cur.Ammo != null)
        {
            if (cur.Ammo.CurrentAmmo <= 0)
                wantReload = true;
        }

        if (wantReload && !_wantReloadPrev)
        {
            _reloadPressedThisFrame = true;
            _shootHeld = false; // кадр відпускаємо тригер, щоб не заважати стейт-машині
        }
        _wantReloadPrev = wantReload;

        // ====== edge-події натиск/відпуск тригера ======
        if (_shootHeld != _shootHeldPrev)
        {
            if (_shootHeld) _shootPressedThisFrame = true;
            else _shootReleasedThisFrame = true;
        }
        _shootHeldPrev = _shootHeld;
    }

    Transform FindClosestPlayer(float radius)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int mask = (playerLayer >= 0) ? (1 << playerLayer) : Physics.AllLayers;

        var hits = Physics.OverlapSphere(transform.position, radius, mask, QueryTriggerInteraction.Ignore);
        float best = float.PositiveInfinity;
        Transform bestT = null;
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            float d = (t.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; bestT = t; }
        }
        return bestT;
    }

    // ===== IInputService (без змін інтерфейсу) =====
    public Vector2 GetMoveAxis() => _move01;
    public Vector2 GetLookDelta() => Vector2.zero;

    public bool IsShootPressed() => _shootPressedThisFrame;
    public bool IsShootReleased() => _shootReleasedThisFrame;
    public bool IsShootHeld() => _shootHeld;

    public bool IsReloadPressed() => _reloadPressedThisFrame;
    public bool IsAimHeld() => _aim;
    public bool IsJumpPressed() => false;
    public bool IsSprintHeld() => _sprint;

    public bool IsWeaponSwitchRequested(out int index) { index = _weaponSlotRequest; return _weaponSlotRequest >= 0; }
    public bool IsFireModeSwitchPressed() => false;
    public bool IsGrenadeThrowPressed() => false;
    public bool IsGrenadeReleased() => false;
    public bool IsGrenadeSwitchPressed() => false;
    public bool IsUsePressed() => false;
    public bool IsExitPressed() => false;

    public bool IsLocal => isLocal;
}
