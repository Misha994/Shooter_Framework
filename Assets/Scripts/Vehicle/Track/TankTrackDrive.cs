// SPDX-License-Identifier: MIT
using UnityEngine;

namespace TracksWC
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody))]
	public class TankTrackDrive : MonoBehaviour, ISeatInputConsumer, IDriveController
	{
		// === Орієнтація "де перед" ===
		public enum Axis { X, Y, Z, NegX, NegY, NegZ }

		[Header("Drive Frame (напрям 'вперед' для W)")]
		[Tooltip("Порожній трансформ (наприклад, 'DriveFrame'), куди дивиться 'перед'. Поверни його як треба.")]
		public Transform driveFrame;
		[Tooltip("Яку ЛОКАЛЬНУ вісь driveFrame вважати 'forward'.")]
		public Axis forwardAxis = Axis.Z;
		[Tooltip("Яку ЛОКАЛЬНУ вісь driveFrame вважати 'up' (вісь yaw).")]
		public Axis upAxis = Axis.Y;

		[Tooltip("Насильно інвертувати газ (зазвичай не потрібно).")]
		public bool invertThrottle = false;

		[Header("WheelColliders (assign in order from front to back)")]
		public WheelCollider[] leftWheels;
		public WheelCollider[] rightWheels;

		[Header("Drive")]
		[Tooltip("Max forward/backward torque per driven wheel (Nm).")]
		public float forwardTorque = 8000f;
		[Tooltip("Additional torque multiplier for spot-turn and assisted turning.")]
		public float turnTorqueMul = 1.6f;
		[Tooltip("Max vehicle speed (m/s). Torque fades to 0 near this value.")]
		public float maxSpeed = 18f;

		[Header("Brakes")]
		public float brakeOnIdle = 5000f;
		public float brakeOnReverseDir = 20000f;
		public float brakeAssistTurning = 3000f;

		[Header("Friction (sideways stiffness blending)")]
		[Range(0.0f, 1.0f)] public float sideStiffStraight = 1.0f;
		[Range(0.0f, 1.0f)] public float sideStiffOnSpot = 0.08f;
		[Range(0.0f, 1.0f)] public float sideStiffTurning = 0.18f;

		[Header("Input")]
		[Tooltip("Використовувати старі осі лише як ДЕБАГ-резерв, коли немає пасажира.")]
		public bool allowLegacyAxes = false;
		public string verticalAxis = "Vertical";
		public string horizontalAxis = "Horizontal";

		[Header("Stability / Heavy feel")]
		[Tooltip("Гасіння курсової швидкості (рад/с) моментом навколо upAxis. Чим більше — тим менше “довгого” ковзання при повороті.")]
		public float yawDamping = 6000f;
		[Tooltip("Зменшення чутливості керма зі швидкістю: 0 = вимкнено, 1 = половина керма на V=maxSpeed.")]
		[Range(0, 1)] public float speedSteerAtten = 0.5f;
		[Tooltip("Сабстепи WC для стабільності (низькі швидкості/високі).")]
		public int substepsLowSpeed = 12;
		public int substepsHighSpeed = 15;
		public float substepsThresholdSpeed = 5f;

		// ---- runtime
		Rigidbody _rb;
		IInputService _input;
		bool _disabled;

		float _throttleCmd;
		float _steerCmd;
		bool _haveExternalCmd;

		public bool IsActive => enabled && !_disabled;

		// ===== Seat wiring =====
		public void SetSeatInput(IInputService input)
		{
			_input = input;
			if (_input == null) KillDrive();
		}
		public void SetInput(IInputService input) => SetSeatInput(input);

		public void SetDisabled(bool disabled)
		{
			_disabled = disabled;
			if (disabled) KillDrive();
		}

		public void KillDrive()
		{
			ApplyToAllWheels(0f, brakeOnIdle);
		}

		// ===== External command (AI / net) =====
		public void SetCommand(float throttle, float steer)
		{
			_throttleCmd = Mathf.Clamp(throttle, -1f, 1f);
			_steerCmd = Mathf.Clamp(steer, -1f, 1f);
			_haveExternalCmd = true;
		}
		public void GetLastCommands(out float throttle, out float steer)
		{
			throttle = _throttleCmd; steer = _steerCmd;
		}

		void Reset()
		{
			if (!driveFrame) driveFrame = transform;
		}

		void Awake()
		{
			_rb = GetComponent<Rigidbody>();
			if (!driveFrame) driveFrame = transform;
			if (leftWheels == null) leftWheels = new WheelCollider[0];
			if (rightWheels == null) rightWheels = new WheelCollider[0];

			// WC сабстепи
			ConfigureSubsteps();
		}

		void OnValidate()
		{
			substepsLowSpeed = Mathf.Max(1, substepsLowSpeed);
			substepsHighSpeed = Mathf.Max(substepsLowSpeed, substepsHighSpeed);
			substepsThresholdSpeed = Mathf.Max(0.1f, substepsThresholdSpeed);
		}

		void FixedUpdate()
		{
			if (_disabled || _rb == null) return;

			// 1) команда
			if (_haveExternalCmd)
			{
				_haveExternalCmd = false;
			}
			else if (_input != null)
			{
				var move = _input.GetMoveAxis(); // X=steer, Y=throttle
				_throttleCmd = Mathf.Clamp(move.y, -1f, 1f);
				_steerCmd = Mathf.Clamp(move.x, -1f, 1f);
			}
			else if (allowLegacyAxes)
			{
				_throttleCmd = Mathf.Clamp(Input.GetAxisRaw(verticalAxis), -1f, 1f);
				_steerCmd = Mathf.Clamp(Input.GetAxisRaw(horizontalAxis), -1f, 1f);
			}
			else
			{
				_throttleCmd = 0f;
				_steerCmd = 0f;
			}

			// 2) напрямки
			Vector3 fwd = AxisToDir(driveFrame, forwardAxis);
			Vector3 up = AxisToDir(driveFrame, upAxis);

			// знак газу (щоб їхав у бік driveFrame.forward)
			float dot = Vector3.Dot(transform.forward, fwd);
			float throttleSign = (dot >= 0f ? 1f : -1f) * (invertThrottle ? -1f : 1f);

			// 3) fade мотора біля maxSpeed
			var v = _rb.GetPointVelocity(_rb.worldCenterOfMass);
			var fwdSpeed = Vector3.Dot(v, fwd);
			var speed01 = Mathf.Clamp01(Mathf.Abs(fwdSpeed) / Mathf.Max(0.01f, maxSpeed));
			var speedK = 1f - speed01; // 1 → 0

			// 4) атенюація керма зі швидкістю
			float steerAtten = Mathf.Lerp(1f, 1f - speedSteerAtten, speed01);

			// 5) змішування гусениць
			float leftCmd = Mathf.Clamp(throttleSign * _throttleCmd - _steerCmd * steerAtten, -1f, 1f);
			float rightCmd = Mathf.Clamp(throttleSign * _throttleCmd + _steerCmd * steerAtten, -1f, 1f);

			bool onSpot = Mathf.Abs(_throttleCmd) < 0.10f && Mathf.Abs(_steerCmd) >= 0.25f;
			bool turning = Mathf.Abs(_throttleCmd) >= 0.10f && Mathf.Abs(_steerCmd) >= 0.20f;
			bool cruising = Mathf.Abs(_throttleCmd) >= 0.10f && Mathf.Abs(_steerCmd) < 0.20f;

			float stiff =
				onSpot ? sideStiffOnSpot :
				turning ? sideStiffTurning :
				cruising ? sideStiffTurning : sideStiffStraight;

			DriveSide(leftWheels, leftCmd, speedK, onSpot, isLeft: true, frictionStiff: stiff);
			DriveSide(rightWheels, rightCmd, speedK, onSpot, isLeft: false, frictionStiff: stiff);

			// 6) yaw-демпфер — гасимо курсову швидкість (рад/с)
			float yawRate = Vector3.Dot(_rb.angularVelocity, up);
			_rb.AddTorque(-up * yawRate * yawDamping, ForceMode.Force);
		}

		void DriveSide(WheelCollider[] side, float cmd, float speedK, bool onSpot, bool isLeft, float frictionStiff)
		{
			if (side == null) return;

			for (int i = 0; i < side.Length; i++)
			{
				var wc = side[i]; if (!wc) continue;

				// blend sideways friction
				var sf = wc.sidewaysFriction;
				sf.stiffness = Mathf.Clamp01(frictionStiff);
				wc.sidewaysFriction = sf;

				// target torque
				float torque = forwardTorque * cmd * speedK;

				// boost torque when we are turning (spot or mixed)
				if (onSpot || Mathf.Abs(_steerCmd) > 0.2f)
					torque *= turnTorqueMul;

				// braking logic
				bool idle = Mathf.Approximately(_throttleCmd, 0f) && Mathf.Approximately(_steerCmd, 0f);
				float brake = 0f;
				if (idle) brake = brakeOnIdle;

				// assist turning: gently brake inner side when steering on move
				bool steerLeft = _steerCmd < -0.001f;
				bool steerRight = _steerCmd > +0.001f;
				if (!onSpot && Mathf.Abs(_throttleCmd) > 0.1f && Mathf.Abs(_steerCmd) > 0.05f)
				{
					bool thisIsInner = (steerLeft && isLeft) || (steerRight && !isLeft);
					if (thisIsInner) brake = Mathf.Max(brake, brakeAssistTurning);
				}

				// if torque opposes current wheel rpm direction -> apply hard brake
				if ((wc.rpm > 1f && torque < -1f) || (wc.rpm < -1f && torque > 1f))
					brake = Mathf.Max(brake, brakeOnReverseDir);

				wc.brakeTorque = brake;
				wc.motorTorque = brake > 0.5f ? 0f : torque;
			}
		}

		void ApplyToAllWheels(float torque, float brake)
		{
			if (leftWheels != null)
				foreach (var wc in leftWheels) { if (!wc) continue; wc.motorTorque = torque; wc.brakeTorque = brake; }
			if (rightWheels != null)
				foreach (var wc in rightWheels) { if (!wc) continue; wc.motorTorque = torque; wc.brakeTorque = brake; }
		}

		void ConfigureSubsteps()
		{
			if (leftWheels != null)
				foreach (var wc in leftWheels) wc?.ConfigureVehicleSubsteps(substepsThresholdSpeed, substepsLowSpeed, substepsHighSpeed);
			if (rightWheels != null)
				foreach (var wc in rightWheels) wc?.ConfigureVehicleSubsteps(substepsThresholdSpeed, substepsLowSpeed, substepsHighSpeed);
		}

		static Vector3 AxisToDir(Transform t, Axis a)
		{
			if (!t) return Vector3.forward;
			return a switch
			{
				Axis.X => t.right,
				Axis.Y => t.up,
				Axis.Z => t.forward,
				Axis.NegX => -t.right,
				Axis.NegY => -t.up,
				Axis.NegZ => -t.forward,
				_ => t.forward
			};
		}

		// ---- helpers for other scripts ---- 
		public static float AverageRPM(WheelCollider[] wheels)
		{
			if (wheels == null || wheels.Length == 0) return 0f;
			float rpm = 0f; int n = 0;
			for (int i = 0; i < wheels.Length; i++) { if (!wheels[i]) continue; n++; rpm += wheels[i].rpm; }
			return n > 0 ? rpm / n : 0f;
		}

		public static float AverageGroundedRPM(WheelCollider[] wheels)
		{
			if (wheels == null || wheels.Length == 0) return 0f;
			float rpm = 0f; int n = 0; WheelHit hit;
			for (int i = 0; i < wheels.Length; i++)
			{
				var w = wheels[i]; if (!w) continue;
				if (w.GetGroundHit(out hit)) { rpm += w.rpm; n++; }
			}
			if (n == 0) return AverageRPM(wheels);
			return rpm / n;
		}
	}
}
