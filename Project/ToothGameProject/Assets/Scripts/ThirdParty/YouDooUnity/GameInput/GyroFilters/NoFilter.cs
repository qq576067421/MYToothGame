using UnityEngine;

public class NoFilter : IGyroFilter
{
    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyro, Vector3 acce, Vector3 magnetLocal, float deltaTime)
    {
        // 陀螺仪角加速度积分
        float deltaAngle = gyro.magnitude * deltaTime;
        Vector3 deltaAxis = gyro.normalized;
        Quaternion gyroRotationDelta = Quaternion.AngleAxis(deltaAngle, deltaAxis);
        rotation *= gyroRotationDelta;
    }

    public void Reset()
    {
    }
}