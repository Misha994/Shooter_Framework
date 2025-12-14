using UnityEngine;

[CreateAssetMenu(menuName = "Mech/Leg Profile")]
public class MechLegProfile : ScriptableObject
{
	public float stepLength = 2f;
	public float stepHeight = 1f;
	public float stepSpeed = 4f;

	public float minBodySpeedForStep = 0.1f;
	public float minStepInterval = 0.15f;

	// Наскільки стопа може “відстати” від ідеальної позиції,
	// перш ніж нога вирішить зробити крок
	public float maxFootDistance = 2.5f;
}
