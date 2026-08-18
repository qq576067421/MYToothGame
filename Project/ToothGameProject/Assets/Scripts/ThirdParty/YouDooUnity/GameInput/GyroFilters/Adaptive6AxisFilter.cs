using System.Collections.Generic;
using UnityEngine;

public class Adaptive6AxisFilter : IGyroFilter
{
    // 自适应互补滤波参数
    public float AcceDeltaMin = 0.03f;                  // 加速度模偏移最小值, 未达到时加速度模信任系数为1
    public float AcceDeltaMax = 0.035f;                  // 加速度模偏移最大值, 超出时加速度模信任系数为0
    public float GyroTrustMin = 20.0f;                  // 角速度模最小值, 未达到时角速度模信任系数为1
    public float GyroTrustMax = 30.0f;                  // 角速度模最大值, 超出时角速度模信任系数为0
    public float GravityFixAlphaMin = 0.00f;            // 修正重力时的插值最小值
    public float GravityFixAlphaMax = 0.30f;            // 修正重力时的插值最大值
    public float AcceLerp = 0.65f;
    // 是否使用方差调整yaw
    public bool IsUseAcceVarianceFixYaw;
    public const int ACCE_BUFFER_SIZE = 10;
    public Queue<Vector3> AcceBuffer = new Queue<Vector3>(ACCE_BUFFER_SIZE);
    public Vector3 AcceBufferSum = Vector3.zero;
    public float VarianceScale = 0.2f;
    float variance;
    float alpha_variance = 0.0f;
    public float MinAlpha_variance = 0.5f;
    public float MaxAlpha_variance = 1.0f;
    float alpha;

    Quaternion deviceRotation;
    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyroLocal, Vector3 acceLocal, Vector3 magnetLocal, float deltaTime)
    {
        // --- 陀螺仪辅助的重力估计（互补滤波） ---
        // 1. 用陀螺仪预测重力方向的变化（快速响应，无延迟）
        //    注意：不能用 Quaternion.Euler，因为 Euler 是按 X→Y→Z 顺序分别旋转，
        //    在角速度较大时会引入交叉耦合误差（左右摇时污染 pitch 分量）。
        //    正确做法：用角轴表示（axis-angle），将角速度视为单一旋转轴。
        Vector3 gyroAngle = gyroLocal * deltaTime; // 本帧角位移（度）
        float angleMag = gyroAngle.magnitude;
        Quaternion gyroQ = (angleMag > 0.001f)
            ? Quaternion.AngleAxis(angleMag, gyroAngle / angleMag)
            : Quaternion.identity;
        rotation *= gyroQ;
        // Vector3 gravityPredicted = (Quaternion.Inverse(gyroQ) * FixedGravity).normalized;
        Vector3 gravityPredicted = Quaternion.Inverse(rotation) * Vector3.down;

        // 2. 用加速度计缓慢校正漂移
        //    关键：运动时必须大幅降低加速度计权重，否则运动加速度会污染重力估计
        float accelMag = acceLocal.magnitude;
        float gyroMag = gyroLocal.magnitude;

        // 加速度计信任度：多重条件判断
        float acceTrust = 1f;
        // 条件1：加速度偏离1g时降低信任（运动产生额外加速度）
        acceTrust *= 1f - Mathf.Clamp01((Mathf.Abs(accelMag - 1.0f) - AcceDeltaMin) / (AcceDeltaMax - AcceDeltaMin));
        // 条件2：角速度大时降低信任（正在运动中）
        acceTrust *= 1f - Mathf.Clamp01((gyroMag - GyroTrustMin) / (GyroTrustMax - GyroTrustMin));

        // alpha: 静止时加大加速度计权重快速回正，运动时降低避免漂移
        alpha = Mathf.Clamp01(Mathf.Lerp(GravityFixAlphaMin, GravityFixAlphaMax, acceTrust)); // 动态校正系数

        var fixedGravity = Vector3.Slerp(
            gravityPredicted,
            acceLocal.normalized,
            alpha
        ).normalized;

        // 修正重力方向, 使用插值减少抖动
        var fullGravityFixQ = Quaternion.FromToRotation(rotation * fixedGravity, Vector3.down);
        var lerpGravityFixQ = Quaternion.Slerp(Quaternion.identity, fullGravityFixQ, AcceLerp);
        rotation = lerpGravityFixQ * rotation;


        if (IsUseAcceVarianceFixYaw)
        {
            // 加速度方差回正
            // 缓存加速度数据
            if (AcceBuffer.Count >= ACCE_BUFFER_SIZE)
            {
                var dq = AcceBuffer.Dequeue();
                AcceBufferSum -= dq;
            }
            AcceBuffer.Enqueue(acceLocal.normalized);
            AcceBufferSum += acceLocal.normalized;
            if (AcceBuffer.Count <= 1) return;

            // 计算加速度方差
            var mean = AcceBufferSum / AcceBuffer.Count;
            float sqrSum = 0.0f;
            foreach (var a in AcceBuffer)
            {
                sqrSum += Vector3.SqrMagnitude(a - mean);
            }
            variance = sqrSum / AcceBuffer.Count;

            // 使用加速度的方差做陀螺仪的移动状态判断
            // 方差增大时，陀螺仪积分权重减小，加速度计增大
            alpha_variance = MaxAlpha_variance - (MaxAlpha_variance - MinAlpha_variance) * Mathf.Min(variance * VarianceScale, 1.0f);

            Quaternion acceRotation = Quaternion.FromToRotation(acceLocal.normalized, Vector3.down);
            rotation = Quaternion.Lerp(acceRotation, rotation, alpha_variance);
        }

        deviceRotation = rotation;
    }

    public void Reset()
    {
        AcceBuffer.Clear();
        AcceBufferSum = Vector3.zero;
        variance = 0.0f;

        var originYaw = deviceRotation.eulerAngles.y;
        var yawClearQ = Quaternion.Euler(0, -originYaw, 0);
        deviceRotation = yawClearQ * deviceRotation;
    }
}