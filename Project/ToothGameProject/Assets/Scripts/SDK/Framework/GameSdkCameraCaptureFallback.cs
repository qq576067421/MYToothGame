using System.Text;
using UnityEngine;
using YouDooSDK.Native;
using YouDooResolution = YouDooSDK.Native.Resolution;

// [新SDK整合] 采集档位候选表与降档工具。
//
// 默认不参与运行期选档：GameSdkPerfOptions.EnableRuntimeCaptureStageSwitch 为 false，
// 档位由 GameSdkModelSelectionBootstrap 在相机第一次启动前定下来。
//
// 放弃运行期切档的原因（2026-11-13 真机实测）：
// 原生采集输出确实会跟着 resolutions 变（SdtGameCamera 按新尺寸出帧），
// 但 Unity 侧 RenderTexture 不会重建——SDK 的 GetTextureWidthVulkan() 只在
// CameraTextureImage 桥初始化时读一次。于是一边是 640x480 的采集输出，
// 另一边是 4048x3040 的纹理（日志里 requested=640x480/prepare 与 frame=4048x3040 并存），
// 画面被写在纹理的错位区域，四个座位框各裁 1/4 后就是纯黑。
//
// 保留本文件是为了：开关重新打开时能直接用上已调好的候选表与黑名单机制。
//
// 使用约束（仅在开关打开时生效）：
// 1. 切档必须重建原生相机流，只能在相机已停止时调用；Stop/Start 由调用方负责，这里只下发配置。
// 2. 只用 catalog 里真实存在、且与设备默认档同宽高比的档位。比例一旦不同，
//    native 输出的画面就会被拉伸/压扁（1280x720 那次就是），所以比例不同宁可放弃切档。
// 3. 不写死具体宽高：每个阶段只给一个“长边上限”，在同宽高比的候选里挑不超上限的最大一档。
//    写死 960x540 会在部分机型上直接落空（实测有设备只有 4:3 档），
//    而按默认档面积比例算又会失衡（默认档 4048x3040 时 30% 仍有 1440x1080）。
// 4. 任何一步失败都只打日志并保持原样，绝不抛异常影响战斗流程。
internal static class GameSdkCameraCaptureFallback
{
    private const string LogTag = "[新SDK整合] capture ";

    // 阶段标识：准备界面与战斗内各算一个阶段。null 表示设备默认档。
    internal const string StagePrepare = "prepare";
    internal const string StageBattle = "battle";

    // 当前实际下发的采集档位；0 表示设备默认档。
    internal static int CurrentWidth { get; private set; }
    internal static int CurrentHeight { get; private set; }

    // 当前所处的降载阶段；null / 空表示设备默认档。
    internal static string CurrentStage { get; private set; }

    // 已确认在本机跑不出画面的档位（无帧 / 整幅纯黑 / 输出比例对不上）。
    // key 统一用 "宽x高"，与宽高对象不绑定，避免依赖 catalog 里的实例。
    //
    // 存盘原因：catalog 里声明支持、原生侧实际起不来的档是设备固件层面的问题，
    // 不会因为重启 App 就好。不存盘的话每次冷启动都要在同一个坏档上白停一次相机，
    // 而在默认档 4048x3040 上停流正是 Vulkan DEVICE_LOST 的高发时刻。
    // 带版本前缀：换了包（version + buildGUID 变化）就重新观察，不把旧结论永久锁死。
    private const string KnownBadPrefsKey = "GameSdk.CameraCapture.KnownBad";
    private const string KnownBadPrefsVersionKey = "GameSdk.CameraCapture.KnownBadBuild";

    private static readonly System.Collections.Generic.HashSet<string> s_KnownBadResolutions =
        new System.Collections.Generic.HashSet<string>();

    private static bool s_KnownBadLoaded;

    internal static bool IsKnownBad(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        EnsureKnownBadLoaded();
        return s_KnownBadResolutions.Contains(width + "x" + height);
    }

    internal static void MarkKnownBad(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        EnsureKnownBadLoaded();
        string key = width + "x" + height;
        if (s_KnownBadResolutions.Add(key))
        {
            Debug.LogWarning(LogTag + "resolution marked unavailable: " + key);
            PersistKnownBad();
        }
    }

    private static void EnsureKnownBadLoaded()
    {
        if (s_KnownBadLoaded)
        {
            return;
        }

        s_KnownBadLoaded = true;
        try
        {
            if (PlayerPrefs.GetString(KnownBadPrefsVersionKey, string.Empty) != CurrentBuildTag())
            {
                // 换包了：上一版记下的坏档可能已经被修好，清空重新观察。
                if (PlayerPrefs.HasKey(KnownBadPrefsKey))
                {
                    PlayerPrefs.DeleteKey(KnownBadPrefsKey);
                    PlayerPrefs.Save();
                }

                return;
            }

            string csv = PlayerPrefs.GetString(KnownBadPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(csv))
            {
                return;
            }

            string[] keys = csv.Split(',');
            for (int i = 0; i < keys.Length; i++)
            {
                string entry = keys[i].Trim();
                if (entry.Length > 0)
                {
                    s_KnownBadResolutions.Add(entry);
                }
            }

            if (s_KnownBadResolutions.Count > 0)
            {
                Debug.Log(LogTag + "known-bad restored count=" + s_KnownBadResolutions.Count);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(LogTag + "known-bad load failed: " + exception.Message);
        }
    }

    private static void PersistKnownBad()
    {
        try
        {
            var builder = new StringBuilder(48);
            foreach (string entry in s_KnownBadResolutions)
            {
                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(entry);
            }

            PlayerPrefs.SetString(KnownBadPrefsKey, builder.ToString());
            PlayerPrefs.SetString(KnownBadPrefsVersionKey, CurrentBuildTag());
            PlayerPrefs.Save();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(LogTag + "known-bad save failed: " + exception.Message);
        }
    }

    // 版本 + buildGUID：同一个包内保留结论，换包重新观察。
    private static string CurrentBuildTag()
    {
        return Application.version + "|" + Application.buildGUID;
    }

    internal static bool IsDeviceDefault => string.IsNullOrEmpty(CurrentStage);

    internal static bool IsStage(string stage) => CurrentStage == stage;

    // 当前阶段与目标不同时才需要重建相机流。
    internal static bool NeedsSwitch(string stage) => CurrentStage != stage;

    internal static string CurrentResolutionText =>
        CurrentWidth > 0 && CurrentHeight > 0
            ? CurrentWidth + "x" + CurrentHeight + "/" + (CurrentStage ?? "default")
            : "device-default";

    // 按阶段的长边上限选一档并下发。返回 true 表示 native 接受了新配置。
    //
    // 不接收具体宽高：不同设备的 catalog 差异很大（默认档可能是 4048x3040 的 4:3，
    // 也可能没有 960x540），写死宽高会在部分机型上直接落空，
    // 而按默认档的面积比例算又会失衡（默认档越大，降完仍然很大）。
    // 所以只用长边封顶，在同宽高比的候选里挑“长边不超上限”的最大一档。
    internal static bool TryApplyStage(string stage, string reason)
    {
        return TryApplyStage(stage, reason, 0);
    }

    // attempt：该阶段第几个候选档（0 = 首选，1 = 次选，……）。
    //
    // catalog 里声明支持、原生侧却起不来的档确实存在（实测 960x720 就是：
    // SetCurUseMode 返回成功，但之后整幅纯黑、拿不到任何新帧）。
    // 调用方的做法是拿 MarkKnownBad 把刚被证伪的档剔除后重新调一次，
    // 候选表会重算首选，落到“离它最近、且仍满足降载目标”的一档，
    // 而不是一步跳回设备默认档——默认档可能是 4048x3040，战斗里会直接把帧率打回去。
    internal static bool TryApplyStage(string stage, string reason, int attempt)
    {
        if (string.IsNullOrEmpty(stage))
        {
            return false;
        }

        if (!TryLoadCatalog(reason, out AndroidServerInfo host, out GameServiceConfig catalog,
                out ModelConfig[] models))
        {
            return false;
        }

        CameraConfig camera = PickCamera(catalog.camList, out YouDooResolution deviceDefault);
        if (camera?.resolutions == null || camera.resolutions.Length == 0)
        {
            Debug.LogWarning(LogTag + "apply skipped reason=" + reason +
                             ": device camera has no resolutions.");
            return false;
        }

        int maxLongEdge = GetStageLongEdge(stage);
        bool preferSmallest = GetStagePreferSmallest(stage);

        System.Collections.Generic.List<YouDooResolution> candidates = BuildStageCandidates(
            camera.resolutions, deviceDefault, maxLongEdge, GetStageMinArea(stage), preferSmallest);
        if (candidates == null || attempt < 0 || attempt >= candidates.Count)
        {
            Debug.LogWarning(LogTag + "apply skipped reason=" + reason + " stage=" + stage +
                             ": no usable lower resolution sharing the device default aspect " +
                             (deviceDefault != null
                                 ? deviceDefault.width + "x" + deviceDefault.height
                                 : "unknown") +
                             ". attempt=" + attempt +
                             " candidates=" + (candidates?.Count ?? 0) +
                             " known-bad=" + s_KnownBadResolutions.Count +
                             " available=[" + DescribeResolutions(camera) + "]");
            return false;
        }

        YouDooResolution match = candidates[attempt];
        if (CurrentWidth == match.width && CurrentHeight == match.height)
        {
            // 挑中的档位与当前一致（准备与战斗落到同一档），只更新阶段标记，不重复下发配置。
            CurrentStage = stage;
            return true;
        }

        bool applied = Apply(host, models, camera, match, reason, stage);
        if (applied)
        {
            CurrentWidth = match.width;
            CurrentHeight = match.height;
            CurrentStage = stage;
            // 把设备默认档与完整候选项一次打全：真机上“降得比预期多”或“被退到邻档”时，
            // 这条日志能直接说明是 catalog 只有这几档 / 哪个档被列了黑名单，不需要再靠猜。
            Debug.Log(LogTag + "catalog reason=" + reason + " stage=" + stage +
                      " device-default=" + (deviceDefault != null
                          ? deviceDefault.width + "x" + deviceDefault.height
                          : "unknown") +
                      " max-long-edge=" + maxLongEdge +
                      " pick=" + (preferSmallest ? "smallest" : "max-under-long-edge") +
                      " attempt=" + attempt + "/" + candidates.Count +
                      " applied=" + match.width + "x" + match.height +
                      " candidates=[" + DescribeCandidates(candidates) + "]" +
                      " available=[" + DescribeResolutions(camera) + "]");
        }

        return applied;
    }

    // 只计算目标档位，不下发、不改状态：调用方用它预先判断“这次切阶段是否真的会换分辨率”。
    // 返回 false 表示 catalog 里挑不到该阶段的可用档（例如本来就只有一档，或全被判了黑名单）。
    internal static bool ResolveStageResolution(string stage, out int width, out int height)
    {
        return ResolveStageResolution(stage, 0, out width, out height);
    }

    internal static bool ResolveStageResolution(string stage, int attempt, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrEmpty(stage) || attempt < 0)
        {
            return false;
        }

        if (!TryLoadCatalog(stage, out AndroidServerInfo _, out GameServiceConfig catalog, out ModelConfig[] _))
        {
            return false;
        }

        CameraConfig camera = PickCamera(catalog.camList, out YouDooResolution deviceDefault);
        if (camera?.resolutions == null || camera.resolutions.Length == 0)
        {
            return false;
        }

        System.Collections.Generic.List<YouDooResolution> candidates = BuildStageCandidates(
            camera.resolutions, deviceDefault, GetStageLongEdge(stage), GetStageMinArea(stage),
            GetStagePreferSmallest(stage));
        if (candidates == null || attempt >= candidates.Count)
        {
            return false;
        }

        width = candidates[attempt].width;
        height = candidates[attempt].height;
        return true;
    }

    // 只更新阶段标记：目标阶段的档位与当前档位一致时用，不重建相机流。
    internal static void AdoptStage(string stage)
    {
        if (!string.IsNullOrEmpty(stage))
        {
            CurrentStage = stage;
        }
    }

    // 恢复设备默认档位（catalog 里面积最大的档），比例异常与黑屏兑底都用它。
    internal static bool TryRestoreDeviceDefault(string reason)
    {
        if (!TryLoadCatalog(reason, out AndroidServerInfo host, out GameServiceConfig catalog,
                out ModelConfig[] models))
        {
            return false;
        }

        CameraConfig camera = PickCamera(catalog.camList, out YouDooResolution best);
        if (camera == null || best == null)
        {
            Debug.LogWarning(LogTag + "restore skipped reason=" + reason +
                             ": device catalog exposes no usable resolution.");
            return false;
        }

        bool applied = Apply(host, models, camera, best, reason, "restore-default");
        if (applied)
        {
            // 恢复默认之后就不再算降载状态。
            CurrentWidth = 0;
            CurrentHeight = 0;
            CurrentStage = null;
        }

        return applied;
    }

    // 准备界面与战斗内允许的最小采集面积不同：准备界面要看清真人，不能降得太狠。
    private static long GetStageMinArea(string stage) =>
        stage == StagePrepare
            ? (long)GameSdkPerfOptions.MinPrepareCaptureWidth * GameSdkPerfOptions.MinPrepareCaptureHeight
            : (long)GameSdkPerfOptions.MinBattleCaptureWidth * GameSdkPerfOptions.MinBattleCaptureHeight;

    // 各阶段的采集长边上限。
    private static int GetStageLongEdge(string stage) =>
        stage == StagePrepare
            ? GameSdkPerfOptions.PrepareCaptureLongEdge
            : GameSdkPerfOptions.BattleCaptureLongEdge;

    // 是否直接取同比例可行档里面积最小的一档（忽略长边上限）。
    private static bool GetStagePreferSmallest(string stage) =>
        stage != StagePrepare && GameSdkPerfOptions.BattleUseSmallestResolution;

    // 按“离首选档最近”排好的同比例候选表，0 号就是首选。
    //
    // 只在“与设备默认档同宽高比”的档位里取，保证换档不会改变画面比例（比例不同人物会被压扁）；
    // 候选需低于默认档且不低于该阶段的面积下限；
    // 已在本机被证伪的档（s_KnownBadResolutions）直接剔除，不再重复黑屏。
    //
    // 退让顺序：先试比首选大的近邻（更保守，也更容易在真机上起得来），再试比它小的。
    // 这样首选档在原生侧起不来时，落到的是“面积最接近且更大”的一档，而不是一步跌到最小档。
    private static System.Collections.Generic.List<YouDooResolution> BuildStageCandidates(
        YouDooResolution[] resolutions,
        YouDooResolution deviceDefault,
        int maxLongEdge,
        long minArea,
        bool preferSmallest)
    {
        if (deviceDefault == null || deviceDefault.width <= 0 || deviceDefault.height <= 0)
        {
            return null;
        }

        long defaultArea = (long)deviceDefault.width * deviceDefault.height;
        var pool = new System.Collections.Generic.List<YouDooResolution>(resolutions.Length);

        for (int i = 0; i < resolutions.Length; i++)
        {
            YouDooResolution candidate = resolutions[i];
            if (candidate == null || candidate.width <= 0 || candidate.height <= 0)
            {
                continue;
            }

            if (!SameAspect(deviceDefault, candidate))
            {
                continue;
            }

            long area = (long)candidate.width * candidate.height;
            // 低于下限会明显影响骨架识别；不小于默认档则等于没降，两者都跳过。
            if (area < minArea || area >= defaultArea)
            {
                continue;
            }

            if (IsKnownBad(candidate.width, candidate.height))
            {
                continue;
            }

            pool.Add(candidate);
        }

        if (pool.Count == 0)
        {
            return null;
        }

        YouDooResolution preferred = PickPreferred(pool, maxLongEdge, preferSmallest);
        if (preferred == null)
        {
            return null;
        }

        pool.Remove(preferred);
        pool.Sort((left, right) => CompareByDistance(left, right, preferred));

        var ordered = new System.Collections.Generic.List<YouDooResolution>(pool.Count + 1) { preferred };
        ordered.AddRange(pool);
        return ordered;
    }

    // 首选档：preferSmallest 时取面积最小的一档；
    // 否则取“长边不超上限”里面积最大的一档，都超上限则退到面积最小的一档（它仍小于默认档）。
    private static YouDooResolution PickPreferred(
        System.Collections.Generic.List<YouDooResolution> pool,
        int maxLongEdge,
        bool preferSmallest)
    {
        YouDooResolution best = null;
        long bestArea = 0;
        YouDooResolution smallest = null;
        long smallestArea = 0;

        for (int i = 0; i < pool.Count; i++)
        {
            YouDooResolution candidate = pool[i];
            long area = (long)candidate.width * candidate.height;
            int longEdge = Mathf.Max(candidate.width, candidate.height);
            if (!preferSmallest && longEdge <= maxLongEdge && area > bestArea)
            {
                bestArea = area;
                best = candidate;
            }

            if (smallest == null || area < smallestArea)
            {
                smallestArea = area;
                smallest = candidate;
            }
        }

        return preferSmallest ? smallest : best ?? smallest;
    }

    // 离首选档越近越靠前；距离相同时面积大的优先——更大的档更容易在真机上起得来。
    private static int CompareByDistance(
        YouDooResolution left,
        YouDooResolution right,
        YouDooResolution preferred)
    {
        long preferredArea = (long)preferred.width * preferred.height;
        long leftArea = (long)left.width * left.height;
        long rightArea = (long)right.width * right.height;
        long leftDelta = System.Math.Abs(leftArea - preferredArea);
        long rightDelta = System.Math.Abs(rightArea - preferredArea);
        if (leftDelta != rightDelta)
        {
            return leftDelta.CompareTo(rightDelta);
        }

        return rightArea.CompareTo(leftArea);
    }

    private static bool TryLoadCatalog(
        string reason,
        out AndroidServerInfo host,
        out GameServiceConfig catalog,
        out ModelConfig[] models)
    {
        host = null;
        catalog = null;
        models = null;

        if (Application.platform != RuntimePlatform.Android)
        {
            return false;
        }

        if (!AndroidServerInfo.IsInitialized)
        {
            Debug.LogWarning(LogTag + "skipped reason=" + reason +
                             ": AndroidServerInfo is not initialized.");
            return false;
        }

        try
        {
            host = AndroidServerInfo.Instance;
            if (host == null)
            {
                return false;
            }

            string catalogJson = host.GetGameServiceConfigAll();
            if (string.IsNullOrEmpty(catalogJson))
            {
                Debug.LogWarning(LogTag + "skipped reason=" + reason + ": device catalog is empty.");
                return false;
            }

            catalog = JsonUtility.FromJson<GameServiceConfig>(catalogJson);
            if (catalog?.modelList == null || catalog.modelList.Length == 0 ||
                catalog.camList == null || catalog.camList.Length == 0)
            {
                Debug.LogWarning(LogTag + "skipped reason=" + reason +
                                 ": device catalog has no model or camera.");
                return false;
            }

            ModelConfig pose = PickPoseModel(catalog.modelList);
            ModelConfig hand = PickHandModel(catalog.modelList);
            if (pose == null && hand == null)
            {
                Debug.LogWarning(LogTag + "skipped reason=" + reason +
                                 ": no usable pose/hand model in device catalog.");
                return false;
            }

            models = pose != null && hand != null
                ? new[] { pose, hand }
                : new[] { pose ?? hand };
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(LogTag + "skipped reason=" + reason + ": " + exception.Message);
            return false;
        }
    }

    private static bool Apply(
        AndroidServerInfo host,
        ModelConfig[] models,
        CameraConfig camera,
        YouDooResolution resolution,
        string reason,
        string stage)
    {
        try
        {
            var config = new GameServiceConfig
            {
                disableFiltering = false,
                modelList = models,
                camList = new[]
                {
                    new CameraConfig
                    {
                        cameraId = camera.cameraId,
                        lensFacing = camera.lensFacing,
                        resolutions = new[] { resolution },
                    },
                },
            };

            bool applied = host.SetCurUseMode(JsonUtility.ToJson(config, false));
            Debug.Log(LogTag + stage + " reason=" + reason +
                      " applied=" + (applied ? "1" : "0") +
                      " models=" + JoinModelNames(models) +
                      " cam=" + camera.cameraId +
                      " res=" + resolution.width + "x" + resolution.height);
            return applied;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(LogTag + stage + " failed reason=" + reason + ": " + exception.Message);
            return false;
        }
    }

    // 宽高比是否一致（容许 2% 偏差）。拿不到任一侧时返回 false，宁可不动。
    private static bool SameAspect(YouDooResolution left, YouDooResolution right)
    {
        if (left == null || right == null || left.width <= 0 || left.height <= 0 ||
            right.width <= 0 || right.height <= 0)
        {
            return false;
        }

        double leftAspect = (double)left.width / left.height;
        double rightAspect = (double)right.width / right.height;
        return System.Math.Abs(leftAspect - rightAspect) <= rightAspect * 0.02;
    }

    private static string DescribeCandidates(System.Collections.Generic.List<YouDooResolution> candidates)
    {
        var builder = new StringBuilder(96);
        for (int i = 0; i < candidates.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(candidates[i].width).Append('x').Append(candidates[i].height);
        }

        return builder.ToString();
    }

    private static string DescribeResolutions(CameraConfig camera)
    {
        var builder = new StringBuilder(96);
        for (int i = 0; i < camera.resolutions.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(camera.resolutions[i]?.width).Append('x').Append(camera.resolutions[i]?.height);
        }

        return builder.ToString();
    }

    // 与 SDK 默认选择保持一致：adla + 18 关键点姿态模型优先。
    private static ModelConfig PickPoseModel(ModelConfig[] models)
    {
        ModelConfig best = null;
        for (int i = 0; i < models.Length; i++)
        {
            ModelConfig model = models[i];
            if (model == null || model.type != "pose")
            {
                continue;
            }

            if (model.format == "adla" && model.kptNum == 18)
            {
                return model;
            }

            if (best == null)
            {
                best = model;
            }
        }

        return best;
    }

    private static ModelConfig PickHandModel(ModelConfig[] models)
    {
        ModelConfig best = null;
        for (int i = 0; i < models.Length; i++)
        {
            ModelConfig model = models[i];
            if (model == null || model.type != "hand")
            {
                continue;
            }

            if (model.format == "adla")
            {
                return model;
            }

            if (best == null)
            {
                best = model;
            }
        }

        return best;
    }

    // 与 SDK 默认选择保持一致：第一个有分辨率的相机 + 面积最大的档位。
    // 必须固定用同一个相机，避免切到别的 camera（例如后置）导致画面全黑。
    private static CameraConfig PickCamera(CameraConfig[] cameras, out YouDooResolution resolution)
    {
        resolution = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            CameraConfig camera = cameras[i];
            if (camera?.resolutions == null || camera.resolutions.Length == 0)
            {
                continue;
            }

            YouDooResolution best = camera.resolutions[0];
            for (int j = 1; j < camera.resolutions.Length; j++)
            {
                YouDooResolution candidate = camera.resolutions[j];
                if (candidate != null && candidate.width * (long)candidate.height >
                    best.width * (long)best.height)
                {
                    best = candidate;
                }
            }

            resolution = best;
            return camera;
        }

        return null;
    }

    private static string JoinModelNames(ModelConfig[] models)
    {
        var builder = new StringBuilder(48);
        for (int i = 0; i < models.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(models[i].name);
        }

        return builder.ToString();
    }
}
