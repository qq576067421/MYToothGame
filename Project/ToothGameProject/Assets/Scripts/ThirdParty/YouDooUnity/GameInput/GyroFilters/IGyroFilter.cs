using UnityEngine;

public interface IGyroFilter
{
    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyro, Vector3 acce, Vector3 magnet, float deltaTime);
    public void Reset();
}