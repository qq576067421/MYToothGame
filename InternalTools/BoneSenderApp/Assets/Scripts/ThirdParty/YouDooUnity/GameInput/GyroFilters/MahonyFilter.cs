using UnityEngine;

public class MahonyFilter : IGyroFilter
{
    public float Kp = 500;
    public float Ki = 0;
    Vector3 gyroBias;
    Vector3 correctGyro;
    public Vector3 CorrectGyro => correctGyro;
    public Vector3 GyroBias => gyroBias;
    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyro, Vector3 acce, Vector3 magnetLocal, float deltaTime)
    {
        // 测量的加速度方向
        Vector3 gMeasured = acce.normalized;
        // 预测当前重力方向
        Vector3 gEstimate = (Quaternion.Inverse(rotation) * Vector3.down).normalized;

        // 计算重力偏差
        Vector3 vError = Vector3.Cross(gMeasured, gEstimate);
        gyroBias += vError * deltaTime;
        correctGyro = gyro + Kp * vError + Ki * gyroBias;

        // 积分修正后的角速度        
        float deltaAngle = correctGyro.magnitude * deltaTime;
        Vector3 deltaAxis = correctGyro.normalized;
        Quaternion gyroCorrectionRotationDelta = Quaternion.AngleAxis(deltaAngle, deltaAxis);
        rotation *= gyroCorrectionRotationDelta;
    }

    public void Reset()
    {
        gyroBias = Vector3.zero;
        correctGyro = Vector3.zero;
    }
}