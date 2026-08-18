using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityUI;

namespace LCL
{
    public class UIFps : MonoBehaviour
    {
        private const float m_DefaultUpdateInterval = 0.25f;
        private const long m_BytesPerMb = 1024L * 1024L;

        public LUIText m_Info;
        public bool m_bShowFps = true;
        public bool m_bShowMemory = true;
        public bool m_bShowRenderInfo = true;

        [SerializeField]
        private float m_UpdateInterval = m_DefaultUpdateInterval;

        private readonly StringBuilder m_StringBuilder = new StringBuilder(64);
        private float m_ElapsedTime;
        private float m_AccumulatedDeltaTime;
        private int m_AccumulatedFrameCount;
        private int m_LastShownFps = int.MinValue;
        private long m_LastShownTotalMemoryMb = long.MinValue;
        private long m_LastShownMonoUsedMemoryMb = long.MinValue;
        private bool m_LastShownBloomEnabled;
        private AntialiasingMode m_LastShownAntiAliasingMode = (AntialiasingMode)(-1);
        private int m_LastShownMsaaSampleCount = int.MinValue;
        private int m_LastShownRenderScaleValue = int.MinValue;
        private int m_LastShownQualityLevel = int.MinValue;
        private string m_LastShownQualityName = string.Empty;
        private int m_LastShownDeviceRamMb = int.MinValue;
        private bool m_LastShowFpsState;
        private bool m_LastShowMemoryState;
        private bool m_LastShowRenderInfoState;
        private bool m_HasDisplayText;
        private int m_DeviceRamMb;

        private void Awake()
        {
            if (m_Info == null)
            {
                m_Info = GetComponent<LUIText>();
            }
            m_UpdateInterval = Mathf.Max(0.1f, m_UpdateInterval);
            m_DeviceRamMb = SystemInfo.systemMemorySize;
            ResetSample();
        }

        private void OnEnable()
        {
            ResetSample();
            RefreshDisplay(true);
        }

        private void OnDisable()
        {
            ClearDisplay();
        }

        private void Update()
        {
            m_AccumulatedFrameCount++;
            m_AccumulatedDeltaTime += Time.unscaledDeltaTime;
            m_ElapsedTime += Time.unscaledDeltaTime;

            if (m_ElapsedTime < m_UpdateInterval)
            {
                if (m_LastShowFpsState != m_bShowFps ||
                    m_LastShowMemoryState != m_bShowMemory ||
                    m_LastShowRenderInfoState != m_bShowRenderInfo)
                {
                    RefreshDisplay(true);
                }
                return;
            }

            RefreshDisplay(false);
            ResetSample();
        }

        private void RefreshDisplay(bool forceRefresh)
        {
            if (m_Info == null)
            {
                return;
            }

            if (!m_bShowFps && !m_bShowMemory && !m_bShowRenderInfo)
            {
                ClearDisplay();
                m_LastShowFpsState = m_bShowFps;
                m_LastShowMemoryState = m_bShowMemory;
                m_LastShowRenderInfoState = m_bShowRenderInfo;
                return;
            }

            int fps = m_LastShownFps;
            if (m_bShowFps)
            {
                fps = ResolveFps();
            }

            long totalMemoryMb = m_LastShownTotalMemoryMb;
            long monoUsedMemoryMb = m_LastShownMonoUsedMemoryMb;
            if (m_bShowMemory)
            {
                totalMemoryMb = BytesToMb(Profiler.GetTotalAllocatedMemoryLong());
                monoUsedMemoryMb = BytesToMb(Profiler.GetMonoUsedSizeLong());
            }

            bool bloomEnabled = m_LastShownBloomEnabled;
            AntialiasingMode antiAliasingMode = m_LastShownAntiAliasingMode;
            int msaaSampleCount = m_LastShownMsaaSampleCount;
            int renderScaleValue = m_LastShownRenderScaleValue;
            int qualityLevel = m_LastShownQualityLevel;
            string qualityName = m_LastShownQualityName;
            int deviceRamMb = m_LastShownDeviceRamMb;
            if (m_bShowRenderInfo)
            {
                ResolveRenderInfo(out bloomEnabled, out antiAliasingMode, out msaaSampleCount,
                    out renderScaleValue, out qualityLevel, out qualityName, out deviceRamMb);
            }

            if (!forceRefresh &&
                m_HasDisplayText &&
                m_LastShowFpsState == m_bShowFps &&
                m_LastShowMemoryState == m_bShowMemory &&
                m_LastShowRenderInfoState == m_bShowRenderInfo &&
                m_LastShownFps == fps &&
                m_LastShownTotalMemoryMb == totalMemoryMb &&
                m_LastShownMonoUsedMemoryMb == monoUsedMemoryMb &&
                m_LastShownBloomEnabled == bloomEnabled &&
                m_LastShownAntiAliasingMode == antiAliasingMode &&
                m_LastShownMsaaSampleCount == msaaSampleCount &&
                m_LastShownRenderScaleValue == renderScaleValue &&
                m_LastShownQualityLevel == qualityLevel &&
                m_LastShownQualityName == qualityName &&
                m_LastShownDeviceRamMb == deviceRamMb)
            {
                return;
            }

            m_StringBuilder.Length = 0;
            if (m_bShowFps)
            {
                m_StringBuilder.Append("FPS:");
                m_StringBuilder.Append(fps);
            }

            if (m_bShowMemory)
            {
                if (m_StringBuilder.Length > 0)
                {
                    m_StringBuilder.Append('\n');
                }
                m_StringBuilder.Append("MEM:");
                m_StringBuilder.Append(totalMemoryMb);
                m_StringBuilder.Append("MB");
                m_StringBuilder.Append("  MONO:");
                m_StringBuilder.Append(monoUsedMemoryMb);
                m_StringBuilder.Append("MB");
            }

            if (m_bShowRenderInfo)
            {
                if (m_StringBuilder.Length > 0)
                {
                    m_StringBuilder.Append('\n');
                }
                m_StringBuilder.Append("Q:");
                m_StringBuilder.Append(qualityName);
                m_StringBuilder.Append('(');
                m_StringBuilder.Append(qualityLevel);
                m_StringBuilder.Append(")  Bloom:");
                AppendSwitchText(m_StringBuilder, bloomEnabled);
                m_StringBuilder.Append("  AA:");
                AppendAntiAliasingText(m_StringBuilder, antiAliasingMode, msaaSampleCount);
                m_StringBuilder.Append("  RS:");
                AppendRenderScaleText(m_StringBuilder, renderScaleValue);
                m_StringBuilder.Append("  RAM:");
                m_StringBuilder.Append(deviceRamMb);
                m_StringBuilder.Append("MB");
            }

            m_Info.SetTextByString(m_StringBuilder.ToString());
            m_HasDisplayText = m_StringBuilder.Length > 0;
            m_LastShownFps = fps;
            m_LastShownTotalMemoryMb = totalMemoryMb;
            m_LastShownMonoUsedMemoryMb = monoUsedMemoryMb;
            m_LastShownBloomEnabled = bloomEnabled;
            m_LastShownAntiAliasingMode = antiAliasingMode;
            m_LastShownMsaaSampleCount = msaaSampleCount;
            m_LastShownRenderScaleValue = renderScaleValue;
            m_LastShownQualityLevel = qualityLevel;
            m_LastShownQualityName = qualityName;
            m_LastShownDeviceRamMb = deviceRamMb;
            m_LastShowFpsState = m_bShowFps;
            m_LastShowMemoryState = m_bShowMemory;
            m_LastShowRenderInfoState = m_bShowRenderInfo;
        }

        private int ResolveFps()
        {
            if (m_AccumulatedFrameCount <= 0 || m_AccumulatedDeltaTime <= 0.0001f)
            {
                return 0;
            }

            return Mathf.RoundToInt(m_AccumulatedFrameCount / m_AccumulatedDeltaTime);
        }

        private void ResetSample()
        {
            m_ElapsedTime = 0f;
            m_AccumulatedDeltaTime = 0f;
            m_AccumulatedFrameCount = 0;
        }

        private void ClearDisplay()
        {
            if (m_Info == null || !m_HasDisplayText)
            {
                return;
            }

            m_Info.SetTextByString(string.Empty);
            m_HasDisplayText = false;
        }

        private static long BytesToMb(long bytes)
        {
            if (bytes <= 0)
            {
                return 0;
            }

            return (bytes + m_BytesPerMb - 1) / m_BytesPerMb;
        }

        private void ResolveRenderInfo(out bool bloomEnabled, out AntialiasingMode antiAliasingMode,
            out int msaaSampleCount, out int renderScaleValue, out int qualityLevel,
            out string qualityName, out int deviceRamMb)
        {
            UniversalAdditionalCameraData cameraData = ResolveWorldCameraData();
            UniversalRenderPipelineAsset urpAsset = ResolveUrpAsset();

            bool renderPostProcessing = cameraData == null || cameraData.renderPostProcessing;
            Bloom bloom = VolumeManager.instance.stack == null ? null : VolumeManager.instance.stack.GetComponent<Bloom>();
            bloomEnabled = renderPostProcessing && bloom != null && bloom.active && bloom.IsActive();

            antiAliasingMode = AntialiasingMode.None;
            if (cameraData != null && renderPostProcessing)
            {
                antiAliasingMode = cameraData.antialiasing;
            }

            msaaSampleCount = urpAsset == null ? QualitySettings.antiAliasing : urpAsset.msaaSampleCount;
            renderScaleValue = urpAsset == null ? -1 : Mathf.RoundToInt(urpAsset.renderScale * 100f);
            qualityLevel = QualitySettings.GetQualityLevel();
            qualityName = ResolveQualityName(qualityLevel);
            deviceRamMb = m_DeviceRamMb;
        }

        private static UniversalAdditionalCameraData ResolveWorldCameraData()
        {
            Camera camera = RenderAPI.GetWorldCamera();
            if (camera == null || camera.Equals(null))
            {
                camera = Camera.main;
            }

            if (camera == null || camera.Equals(null))
            {
                return null;
            }

            return camera.GetComponent<UniversalAdditionalCameraData>();
        }

        private static UniversalRenderPipelineAsset ResolveUrpAsset()
        {
            UniversalRenderPipelineAsset urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            }
            if (urpAsset == null)
            {
                urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            }

            return urpAsset;
        }

        private static string ResolveQualityName(int qualityLevel)
        {
            string[] qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityLevel < 0 || qualityLevel >= qualityNames.Length)
            {
                return "Unknown";
            }

            return qualityNames[qualityLevel];
        }

        private static void AppendSwitchText(StringBuilder builder, bool enabled)
        {
            builder.Append(enabled ? "On" : "Off");
        }

        private static void AppendAntiAliasingText(StringBuilder builder, AntialiasingMode antiAliasingMode, int msaaSampleCount)
        {
            bool postAntiAliasingEnabled = antiAliasingMode != AntialiasingMode.None;
            bool msaaEnabled = msaaSampleCount > 1;
            if (!postAntiAliasingEnabled && !msaaEnabled)
            {
                builder.Append("Off");
                return;
            }

            builder.Append("On(");
            bool hasPrevious = false;
            if (postAntiAliasingEnabled)
            {
                AppendAntiAliasingModeName(builder, antiAliasingMode);
                hasPrevious = true;
            }

            if (msaaEnabled)
            {
                if (hasPrevious)
                {
                    builder.Append('+');
                }
                builder.Append("MSAAx");
                builder.Append(msaaSampleCount);
            }
            builder.Append(')');
        }

        private static void AppendAntiAliasingModeName(StringBuilder builder, AntialiasingMode antiAliasingMode)
        {
            if (antiAliasingMode == AntialiasingMode.FastApproximateAntialiasing)
            {
                builder.Append("FXAA");
            }
            else if (antiAliasingMode == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
            {
                builder.Append("SMAA");
            }
            else if (antiAliasingMode == AntialiasingMode.TemporalAntiAliasing)
            {
                builder.Append("TAA");
            }
            else
            {
                builder.Append("Unknown");
            }
        }

        private static void AppendRenderScaleText(StringBuilder builder, int renderScaleValue)
        {
            if (renderScaleValue < 0)
            {
                builder.Append("N/A");
                return;
            }

            builder.Append(renderScaleValue / 100);
            builder.Append('.');
            int fraction = renderScaleValue % 100;
            if (fraction < 10)
            {
                builder.Append('0');
            }
            builder.Append(fraction);
        }
    }
}
