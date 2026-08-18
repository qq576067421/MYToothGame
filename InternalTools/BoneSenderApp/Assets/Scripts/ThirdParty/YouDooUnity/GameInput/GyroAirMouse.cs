
using UnityEngine;

public class GyroAirMouse
{
    enum ClampMode
    {
        NoClamp,            // 无限制
        Clamp1,             // 只限制最终结果, 不限制鼠标位置
        Clamp2,             // 限制鼠标位置
    }
    public float HSensitivity = 30.0f;       // 水平灵敏度
    public float VSensitivity = 30.0f;       // 垂直灵敏度
    float senScalar = 1.0f;
    const int NormalUnit = 1080;
    const float halfHeight = 0.5f;
    const float halfWidth = 8.0f / 9.0f;
    const float MIN_SEN = 0.5f;
    const float MAX_SEN = 3.0f;

    Vector2 position = Vector2.zero;
    public Vector2 ARScreenNPosition => position;
    ClampMode clampMode = ClampMode.Clamp1;

    /// <summary>
    /// Get Aspect Ration Screen Normalized Positio
    /// 返回屏幕归一化坐标, 考虑高宽比
    /// </summary>
    /// <param name="rotation"></param>
    /// <param name="gyro"></param>
    /// <returns></returns>
    public Vector2 GetScreenNPositionAR(Quaternion rotation, Vector3 gyro)
    {
        Vector3 dir = rotation * Vector3.forward;
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg * HSensitivity;
        float pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg * VSensitivity;
        position = new Vector2(yaw / NormalUnit, pitch / NormalUnit);
        Vector2 finalResult = position;
        if (clampMode == ClampMode.Clamp1)
        {
            // 只限制最终结果, 不限制鼠标位置
            finalResult.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            finalResult.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);
        }
        else if (clampMode == ClampMode.Clamp2)
        {
            // 限制鼠标位置
            position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            position.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);
            finalResult = position;
        }
        return finalResult;
    }

    public Vector2 GetScreenNPositionAR(ref Quaternion rotation, Adaptive9AxisFilter ada9)
    {
        // 计算forwardQ0和forwardQ1投影在YawPlane/PitchPlane的夹角作为空鼠的x/y
        Vector3 forwardQ0 = ada9.Q0 * Vector3.forward;
        Vector3 forwardQ1 = ada9.LastDeviceRotation * Vector3.forward;
        Vector3 forwardQ0YawProj = Vector3.ProjectOnPlane(forwardQ0, ada9.YawPlaneNormal);
        Vector3 forwardQ1YawProj = Vector3.ProjectOnPlane(forwardQ1, ada9.YawPlaneNormal);
        Vector3 forwardQ0PitchProj = Vector3.ProjectOnPlane(forwardQ0, ada9.PitchPlaneNormal);
        Vector3 forwardQ1PitchProj = Vector3.ProjectOnPlane(forwardQ1, ada9.PitchPlaneNormal);
        float yaw = Vector3.SignedAngle(forwardQ0YawProj, forwardQ1YawProj, ada9.YawPlaneNormal);
        float pitch = Vector3.SignedAngle(forwardQ0PitchProj, forwardQ1PitchProj, ada9.PitchPlaneNormal);

        position = new Vector2(yaw * HSensitivity / NormalUnit, -pitch * VSensitivity / NormalUnit);
        Vector2 finalResult = position;
        if (clampMode == ClampMode.Clamp1)
        {
            // 只限制鼠标最终结果, 不限制设备旋转位置
            finalResult.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            finalResult.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);
        }
        else if (clampMode == ClampMode.Clamp2)
        {
            // 钳制设备旋转, 超出后需要回转
            var yawClampDelta = ClampDelta(position.x, -halfWidth, halfWidth) * NormalUnit / HSensitivity;
            var pitchClampDelta = -ClampDelta(position.y, -halfHeight, halfHeight) * NormalUnit / VSensitivity;
            var yawClampQ = Quaternion.AngleAxis(yawClampDelta, -ada9.YawPlaneNormal);
            var pitchClampQ = Quaternion.AngleAxis(pitchClampDelta, -ada9.PitchPlaneNormal);
            ada9.SetQ0Clamp(yawClampQ, pitchClampQ);

            position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
            position.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);
            finalResult = position;
        }
        return finalResult;
    }


    public void Reset()
    {
        position = Vector2.zero;
    }

    public void SetClampMode(int value)
    {
        clampMode = (ClampMode)value;
    }

    public void SenPlus(int value)
    {
        senScalar = Mathf.Clamp(senScalar + value, MIN_SEN, MAX_SEN);
    }

    float ClampDelta(float value, float min, float max)
    {
        if (value < min) return min - value;
        if (value > max) return max - value;
        return 0f;
    }
}