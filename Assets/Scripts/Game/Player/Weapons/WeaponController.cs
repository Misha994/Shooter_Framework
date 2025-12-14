using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
	// Ін’ єкції тільки з локального GameObjectContext (префаб/інстанс)
	[Inject(Optional = true, Source = InjectSources.Local)] private IInputService _input;
	[Inject(Optional = true, Source = InjectSources.Local)] private IHumanoidWeaponInventory _inventory;
	[Inject(Optional = true, Source = InjectSources.Local)] private IWeaponInventoryEvents _inventoryEvents;

	[SerializeField] private bool _autoFireForAI = true;

	private IWeaponFireController _fireController;
	private WeaponBase _weapon;     // поточна зброя (для підписки на події)
	private bool _wasHeld;

	// Зовнішнє блокування (наприклад, коли у транспорті)
	[SerializeField] private bool _externallySuppressed = false;

	/// <summary>Заборонити/дозволити будь-яку стрільбу (рубає інпут зверху).</summary>
	public void SetExternalSuppression(bool on)
	{
		if (_externallySuppressed == on) return;
		_externallySuppressed = on;

		if (on)
		{
			_fireController?.OnFireReleased();
			_wasHeld = false;
		}
	}

	private void Awake()
	{
		// Перевага — локальним компонентам на самому об'єкті
		var localInput = GetComponentInParent<IInputService>();
		if (localInput != null) _input = localInput;

		var localInv = GetComponentInParent<IHumanoidWeaponInventory>();
		if (localInv != null) _inventory = localInv;

		if (_inventoryEvents == null)
			_inventoryEvents = _inventory as IWeaponInventoryEvents;
	}

	private void OnEnable()
	{
		if (_inventoryEvents != null)
			_inventoryEvents.WeaponChanged += OnWeaponChanged;

		RefreshCurrentWeapon();

		// Автофайр — тільки для не-локального (AI)
		if (_input != null)
			_autoFireForAI = !_input.IsLocal;
	}

	private void OnDisable()
	{
		if (_inventoryEvents != null)
			_inventoryEvents.WeaponChanged -= OnWeaponChanged;

		if (_weapon != null)
			_weapon.FireModeChanged -= OnWeaponFireModeChanged;
	}

	private void OnDestroy()
	{
		if (_weapon != null)
			_weapon.FireModeChanged -= OnWeaponFireModeChanged;
	}

	private void OnWeaponChanged(WeaponBase prev, WeaponBase next)
	{
		if (prev != null)
			prev.FireModeChanged -= OnWeaponFireModeChanged;

		_weapon = next;

		if (_weapon != null)
			_weapon.FireModeChanged += OnWeaponFireModeChanged;

		RecreateFireController();
	}

	/// <summary>Публічний виклик, якщо інвентар оновився без події.</summary>
	public void RefreshCurrentWeapon()
	{
		var w = _inventory != null ? _inventory.Current : null;

		if (_weapon != null)
			_weapon.FireModeChanged -= OnWeaponFireModeChanged;

		_weapon = w;

		if (_weapon != null)
			_weapon.FireModeChanged += OnWeaponFireModeChanged;

		RecreateFireController();
	}

	private void OnWeaponFireModeChanged(FireModeDescriptor _)
	{
		RecreateFireController();
	}

	/// <summary>Перебудувати контролер вогню з урахуванням поточного режиму зброї.</summary>
	private void RecreateFireController()
	{
		_fireController = null;
		_wasHeld = false;

		if (_weapon == null) return;

		// 1) спроба знайти компонент-контролер на самій зброї
		_fireController = _weapon.GetComponentInChildren<IWeaponFireController>(true);

		// 2) фолбек: створити контролер з фабрики за режимом зброї
		if (_fireController == null)
			_fireController = WeaponFireModeFactory.Create(_weapon.FireMode, _weapon);

		if (_fireController == null)
			Debug.LogWarning($"[{name}] No IWeaponFireController for '{_weapon.name}'. Shooting disabled.");
	}

	private void Update()
	{
		if (_input == null) return;

		// === ГАРЯЧІ КЛАВІШІ ПЕРЕМИКАННЯ ЗБРОЇ 1/2/3 ===
		// PlayerInputReader піднімає флаг через TryGetWeaponSwitch(out index)
		if (_inventory != null && _input.IsWeaponSwitchRequested(out int slotIndex))
		{
			if (_inventory.Count == 0)
			{
				Debug.LogWarning("[WeaponController] Switch requested, but inventory is empty.");
			}
			else if (slotIndex < 0 || slotIndex >= _inventory.Count)
			{
				Debug.LogWarning($"[WeaponController] Switch requested to slot {slotIndex}, " +
								 $"but inventory has {_inventory.Count} item(s).");
			}
			else
			{
				_inventory.SwitchWeapon(slotIndex);
				// Після свічу контролер і так перебудується в OnWeaponChanged
				// але на випадок, якщо немає подій — підстрахуємось:
				RecreateFireController();
			}
		}

		// Якщо нема контролера — подальша логіка не потрібна
		if (_fireController == null)
			return;

		// Перемикання режиму вогню
		if (_weapon != null && _input.IsFireModeSwitchPressed())
		{
			_weapon.TrySwitchFireMode(); // викличе подію та перебудує контролер через OnWeaponFireModeChanged
		}

		// Глобальне блокування (наприклад, поки персонаж у транспорті)
		if (_externallySuppressed)
		{
			_fireController.Update();
			return;
		}

		// Тригер стрільби
		bool heldNow;
		if (_input.IsLocal)
			heldNow = _input.IsShootHeld();
		else
			heldNow = _autoFireForAI ? _input.IsShootHeld() : _input.IsShootHeld();

		bool pressedNow = heldNow && !_wasHeld;
		bool releasedNow = !heldNow && _wasHeld;

		if (pressedNow) _fireController.OnFirePressed();
		if (releasedNow) _fireController.OnFireReleased();
		_wasHeld = heldNow;

		// Перезарядка
		if (_input.IsReloadPressed())
			_fireController.OnReloadPressed();

		_fireController.Update();
	}
}
