using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Adaptive9AxisFilter : IGyroFilter
{
    // 自适应互补滤波参数
    public float AcceDeltaMin = 0.04f;                  // 加速度模偏移最小值, 未达到时加速度模信任系数为1
    public float AcceDeltaMax = 0.07f;                  // 加速度模偏移最大值, 超出时加速度模信任系数为0
    public float GyroTrustMin = 20.0f;                  // 角速度模最小值, 未达到时角速度模信任系数为1
    public float GyroTrustMax = 200.0f;                  // 角速度模最大值, 超出时角速度模信任系数为0
    public float GravityFixAlphaMin = 0.00f;            // 修正重力时的插值最小值
    public float GravityFixAlphaMax = 0.30f;            // 修正重力时的插值最大值

    // 加速度计修正力度参数
    public float AcceLerp = 0.65f;
    public float AcceLerpVibrating = 0.05f;
    // 磁力计修正力度参数
    public float MagnetYawGain = 0.05f;
    public float MinVaildMangetMagnitude = 100.0f;
    public float MaxVaildMangetMagnitude = 1500.0f;
    // 零点位置磁力计读数(投影到世界水平面)
    public Vector3 YawZeroMagWorldHor = Vector3.zero;

    public const int ACCE_BUFFER_SIZE = 10;
    public Queue<Vector3> AcceBuffer = new Queue<Vector3>(ACCE_BUFFER_SIZE);
    public Vector3 AcceBufferSum = Vector3.zero;
    public float VarianceScale = 0.2f;
    float variance;
    float alpha_variance = 0.0f;
    public float MinAlpha_variance = 0.5f;
    public float MaxAlpha_variance = 1.0f;
    float alpha;

    public Quaternion Q0 = Quaternion.identity;
    Vector3 lastMagnet = Vector3.zero;
    public Quaternion LastDeviceRotation = Quaternion.identity;
    public Vector3 YawPlaneNormal = Vector3.up;
    public Vector3 PitchPlaneNormal = Vector3.right;
    float vibrateLeftTime = 0;
    float vibrateRecoverTime = 0.1f;

    public void QuaternionIntegrate(ref Quaternion rotation, Vector3 gyroLocal, Vector3 acceLocal, Vector3 magnetLocal, float deltaTime)
    {
        // 更新震动计时
        vibrateLeftTime = Mathf.Max(vibrateLeftTime - deltaTime, 0.0f);

        // --- 陀螺仪辅助的重力估计（互补滤波） ---
        // 1. 用陀螺仪预测重力方向的变化
        Vector3 gyroAngle = gyroLocal * deltaTime; // 本帧角位移（度）
        float angleMag = gyroAngle.magnitude;
        Quaternion gyroQ = (angleMag > 0.001f)
            ? Quaternion.AngleAxis(angleMag, gyroAngle / angleMag)
            : Quaternion.identity;
        rotation *= gyroQ;
        // Vector3 gravityPredicted = (Quaternion.Inverse(gyroQ) * FixedGravity).normalized;
        Vector3 gravityPredicted = Quaternion.Inverse(rotation) * Vector3.down;

        // 2. 用加速度计缓慢校正漂移
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
        // 手柄有震动时选用小插值来减少震动带来的影响
        var finalAcceLerp = vibrateLeftTime > 0 ? AcceLerpVibrating : AcceLerp;
        var lerpGravityFixQ = Quaternion.Slerp(Quaternion.identity, fullGravityFixQ, finalAcceLerp);
        rotation = lerpGravityFixQ * rotation;

        // 使用磁力计补正yaw角度
        if (magnetLocal.magnitude > MinVaildMangetMagnitude && magnetLocal.magnitude < MaxVaildMangetMagnitude)
        {
            var magnetWorldHor = rotation * magnetLocal;
            magnetWorldHor.y = 0;
            magnetWorldHor.Normalize();
            // 计算两个方向的角度差（绕 Y 轴）
            float yawError = Vector3.SignedAngle(magnetWorldHor, YawZeroMagWorldHor, Vector3.up);

            // 积分yaw角度补正
            rotation = Quaternion.AngleAxis(yawError * MagnetYawGain, Vector3.up) * rotation;
        }
        // 磁力计不存在或读数异常, 退化到方差调整Yaw方法
        else
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

        LastDeviceRotation = rotation;
        lastMagnet = magnetLocal;
    }

    public void Reset()
    {
        AcceBuffer.Clear();
        AcceBufferSum = Vector3.zero;
        variance = 0.0f;

        var originYaw = LastDeviceRotation.eulerAngles.y;
        var yawClearQ = Quaternion.Euler(0, -originYaw, 0);
        LastDeviceRotation = yawClearQ * LastDeviceRotation;
        Q0 = LastDeviceRotation;
        SetQ0Clamp(Quaternion.identity, Quaternion.identity);

        if (lastMagnet.magnitude >= 0.01f)
        {
            YawZeroMagWorldHor = Q0 * lastMagnet;
            YawZeroMagWorldHor.y = 0;
            YawZeroMagWorldHor.Normalize();
        }
    }

    /// <summary>
    /// 通过旋转Q0来实现钳制效果
    /// </summary>
    /// <param name="yawClampQ"></param>
    /// <param name="pitchClampQ"></param>
    public void SetQ0Clamp(Quaternion yawClampQ, Quaternion pitchClampQ)
    {
        Q0 = yawClampQ * (pitchClampQ * Q0);
        var q0ForwardWorld = Q0 * Vector3.forward;
        // 确认yaw平面
        YawPlaneNormal = Vector3.Cross(q0ForwardWorld, Vector3.right);
        if (YawPlaneNormal.magnitude <= 0.01f)
        {
            // q0forward与world.right同线时, 选用world.forward做叉乘
            YawPlaneNormal = Vector3.Cross(Vector3.forward, q0ForwardWorld);
        }
        // 确保yaw平面y分量 > 0
        if (YawPlaneNormal.y < 0)
        {
            YawPlaneNormal *= -1;
        }

        // 确认pitch平面
        PitchPlaneNormal = Vector3.Cross(Vector3.up, q0ForwardWorld);
        if (PitchPlaneNormal.magnitude <= 0.01f)
        {
            // q0forward与world.up同线时, 选用world.做叉乘
            PitchPlaneNormal = Vector3.Cross(q0ForwardWorld, Vector3.forward);
        }
        // 确保pitch平面x分量 > 0
        if (PitchPlaneNormal.x < 0)
        {
            PitchPlaneNormal *= -1;
        }

        if (lastMagnet.magnitude >= 0.01f)
        {
            YawZeroMagWorldHor = yawClampQ * (pitchClampQ * YawZeroMagWorldHor);
            YawZeroMagWorldHor.y = 0;
            YawZeroMagWorldHor.Normalize();
        }
    }

    public void SetLeftVibrateTime(float time)
    {
        vibrateLeftTime = time + vibrateRecoverTime;
    }
}