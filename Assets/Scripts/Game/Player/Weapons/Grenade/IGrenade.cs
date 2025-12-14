using UnityEngine;

public interface IGrenade
{
    void Throw(Vector3 position, Vector3 direction, float force);
}
