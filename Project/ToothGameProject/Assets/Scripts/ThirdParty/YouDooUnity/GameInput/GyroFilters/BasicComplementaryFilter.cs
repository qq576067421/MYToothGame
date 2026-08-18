using UnityEngine;

public class BasicComplementaryFilter : IGyroFilter
{
    float Alpha;
    public float AlphaThreshold = 0.0f;
    public float MinimumAlpha = 0.90f;
    public float MaximumAlpha = 0.98f;
    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyro, Vector3 acce, Vector3 magnetLocal, float deltaTime)
    {
        // 根据角速度判断陀螺仪的运动情况
        Alpha = Mathf.Clamp(gyro.magnitude / AlphaThreshold, MinimumAlpha, MaximumAlpha);
        // 陀螺仪角加速度积分
        Quaternion gyroRotationDelta = Quaternion.Euler(gyro * deltaTime);

        // 使用加速度确定陀螺仪朝向
        // 陀螺仪加速度, 陀螺仪静止时为重力方向
        Quaternion acceRotation = Quaternion.FromToRotation(acce.normalized, Vector3.down);

        Quaternion gyroRotationDeltaLerped = Quaternion.Lerp(Quaternion.identity, gyroRotationDelta, Alpha);
        rotation = Quaternion.Lerp(acceRotation, rotation, Alpha);
        rotation *= gyroRotationDeltaLerped;
    }

    public void Reset()
    {
    }
}