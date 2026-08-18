using System;

namespace CompanyInternalTools.BoneParserLib
{
    public struct BoneVector2
    {
        public float m_X;
        public float m_Y;

        public static readonly BoneVector2 m_Zero = new BoneVector2(0f, 0f);

        public BoneVector2(float x, float y)
        {
            m_X = x;
            m_Y = y;
        }

        public float ReadLengthSquared()
        {
            return m_X * m_X + m_Y * m_Y;
        }

        public static BoneVector2 operator +(BoneVector2 left, BoneVector2 right)
        {
            return new BoneVector2(left.m_X + right.m_X, left.m_Y + right.m_Y);
        }

        public static BoneVector2 operator -(BoneVector2 left, BoneVector2 right)
        {
            return new BoneVector2(left.m_X - right.m_X, left.m_Y - right.m_Y);
        }

        public static BoneVector2 operator *(BoneVector2 value, float scale)
        {
            return new BoneVector2(value.m_X * scale, value.m_Y * scale);
        }
    }

    public struct BoneVector3
    {
        public float m_X;
        public float m_Y;
        public float m_Z;

        public static readonly BoneVector3 m_Zero = new BoneVector3(0f, 0f, 0f);
        public static readonly BoneVector3 m_Forward = new BoneVector3(0f, 0f, 1f);

        public BoneVector3(float x, float y, float z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        public float ReadLengthSquared()
        {
            return m_X * m_X + m_Y * m_Y + m_Z * m_Z;
        }
    }

    internal static class BoneMath
    {
        public static float Abs(float value)
        {
            return Math.Abs(value);
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        public static float Cos(float radians)
        {
            return (float)Math.Cos(radians);
        }

        public static float Distance(BoneVector2 left, BoneVector2 right)
        {
            float deltaX = left.m_X - right.m_X;
            float deltaY = left.m_Y - right.m_Y;
            return Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public static float Lerp(float start, float end, float t)
        {
            return start + (end - start) * Clamp01(t);
        }

        public static float Max(float left, float right)
        {
            return Math.Max(left, right);
        }

        public static float Min(float left, float right)
        {
            return Math.Min(left, right);
        }

        public static BoneVector3 NormalizeOrDefault(BoneVector3 value, BoneVector3 fallback)
        {
            float lengthSquared = value.ReadLengthSquared();
            if (lengthSquared <= 0.0001f)
            {
                return fallback;
            }

            float invLength = 1f / Sqrt(lengthSquared);
            return new BoneVector3(value.m_X * invLength, value.m_Y * invLength, value.m_Z * invLength);
        }

        public static float Sign(float value)
        {
            if (value > 0f)
            {
                return 1f;
            }

            if (value < 0f)
            {
                return -1f;
            }

            return 0f;
        }

        public static float Sin(float radians)
        {
            return (float)Math.Sin(radians);
        }

        public static float Sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }
    }
}
