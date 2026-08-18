/*
作者：Ting
创建时间：2025.10.07
描述：范例，用于解析数据。
*/
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

//业务侧的代码：
public class AndroidParseData : MonoBehaviour
{
    [DllImport("youdoo_plugin")]
    protected static extern void ClearFrameInfoStorage(); //清除数据。不会把存储的对象释放掉

    [DllImport("youdoo_plugin")]
    protected static extern void InitFrameInfoStorage(int maxSize); //每一次调用都会清理且删除一下的存储，重新建立新的存储。

    // 导入Native插件函数
    [DllImport("youdoo_plugin")]
    protected static extern long GetLatestFrameInfoPtr(); //获取最新FrameInfo的指针 

    //获取 哪一帧的数据。 
    [DllImport("youdoo_plugin")]
    protected static extern long GetFrameInfoPtrByIndex(int index);  //通过索引获取FrameInfo指针

    /// <summary>
    /// 判断第几帧数，在XX 区域内的动作。 每一次调用都会 覆盖 outputStates 的值是  PersonState::STATE_UNKNOWN;
    /// </summary>
    /// <param name="frameIndex">第几帧</param>
    /// <param name="personId">人物的ID</param>
    /// <param name="outputStates">获得到当前这个人的状态，是一个数组</param>
    /// <param name="outputStatesLength">获得到当前这个人的状态，是一个数组，需要知道这个数组的长度</param>
    /// <returns></returns>
    [DllImport("youdoo_plugin")]
    protected static extern bool CheckPersonAction(int frameIndex, int personId,
        int[] outputStates, int outputStatesLength);

    /// <summary>
    /// 在单个区域内查找置信度最高的人
    /// </summary>
    [DllImport("youdoo_plugin")]
    protected static extern int CheckPersonScoreInArea(
        int frame_index,
        float area_left, float area_top, float area_right, float area_bottom,
        int[] exclude_person_ids, int exclude_count
    );

    /// <summary>
    /// 在多个区域内分别查找置信度最高的人
    /// </summary>
    [DllImport("youdoo_plugin")]
    protected static extern bool CheckPersonScoreInAreas(
        int frame_index,
        float[] area_rects_array, int area_count,
        int[] exclude_person_ids, int exclude_count,
        int[] output_person_ids
    );
    /// <summary>
    /// 通过这一帧获取到检测对象的数据。
    /// </summary>
    /// <param name="frameInfoPtr">传入的是这一帧的地址</param> 
    /// <param name="detectType">检测类型</param>
    /// <param name="frameTimeMs">输出这一帧的ID,当前帧的时间戳，单位毫秒</param>
    /// <param name="imageWidth">这是一幅多大的图片</param>
    /// <param name="imageHeight">这是一幅多大的图片</param>
    /// <param name="aHardwareBufferPtr">这一幅图片的 物理地址 </param>
    /// <param name="objectCount">当前这幅图片里面有多少个检测对象</param>
    /// SDK 4.0 及之后的版本，增加了 aHardwareBufferPtr 的参数已经作废，返回的值已经没有意义。可以参考 Vulkan OpenglES的方式显示相机的图片。
    /// 都已经封装完善，建议打包的时候只选择 Vulkan。具体的实现可以参考：\Assets\Scripts\AndroidServerInfo\CameraView
    [DllImport("youdoo_plugin")]
    protected static extern void GetFrameInfoBasicByType(long frameInfoPtr, int detectType,
        out long frameTimeMs, out int imageWidth, out int imageHeight, out long aHardwareBufferPtr, out int objectCount);

    /// <summary>
    /// 获取到这一帧的指定类型的所有对象的ID。
    /// </summary>
    /// <param name="frameInfoPtr">传入的是这一帧的地址</param>
    /// <param name="detectType">检测类型</param>
    /// <param name="outputArray">输出所有对象的ID到一个数组里面</param>
    /// <param name="outputArrayLength">outputArray的长度</param>
    /// <returns>返回实际有多少个对象</returns>
    [DllImport("youdoo_plugin")]
    protected static extern int GetPersonIdsByType(long frameInfoPtr, int detectType, int[] outputArray, int outputArrayLength);

    /// <summary>
    /// 获取特定检测对象的DetectBox信息
    /// </summary>
    /// <param name="frameInfoPtr"> 哪一帧 </param>
    /// <param name="detectType">检测类型</param>
    /// <param name="objectId">对象的id</param>
    /// <param name="left">这个对象的 RectF 的left的参数</param>
    /// <param name="top">这个对象的 RectF 的top的参数</param>
    /// <param name="right">这个对象的 RectF 的right的参数</param>
    /// <param name="bottom">这个对象的 RectF 的buttom的参数</param>
    /// <param name="score">这个对象的 置信度</param>
    /// <param name="keypointCount">有多少个关键点数据</param>
    /// <returns></returns>
    [DllImport("youdoo_plugin")]
    protected static extern bool GetDetectBoxByType(long frameInfoPtr, int detectType, int objectId,
        out float left, out float top, out float right, out float bottom,
        out float score, out int keypointCount);

    /// <summary>
    /// 2026.01.26 SDK 4.0 增加。与GetDetectBoxByType 函数相比，增加type的输出;
    /// </summary>
    /// <param name="frameInfoPtr">哪一帧</param>
    /// <param name="detectType">检测类型</param>
    /// <param name="objectId">对象的id</param>
    /// <param name="left">这个对象的 RectF 的left的参数</param>
    /// <param name="top">这个对象的 RectF 的top的参数</param>
    /// <param name="right">这个对象的 RectF 的right的参数</param>
    /// <param name="bottom">这个对象的 RectF 的buttom的参数</param>
    /// <param name="score"这个对象的 置信度></param>
    /// <param name="keypointCount">有多少个关键点数据</param>
    /// <param name="type">检测框识别类型，
    /// 如果是pose模型表示静态姿势编号待定。0是未知，，1举左手，2举右手、3举双手、4双手交在胸前，8双手叉腰，16蹲，type是个int32位，每一bit代表一个姿势，支持同时识别多个姿势  获取到type时与目标姿势进行&运算 结果不为0表示在做这个动作
    /// “举双手”对应掩码 3（即 1|2），它要求同时举起左手和右手。此时需要精确判断两个位是否都为 1：如果 (state & 3) == 3，说明玩家同时举起了双手。如果只检查 (state & 3) != 0，则当仅举左手或仅举右手时也会成立，这不符合“双手”的定义。
    /// 如果是hand模型表示静态手势编号，-1表示未识别，0表示握拳，5表示五指张开，
    /// 如果是face待定
    /// <returns></returns>
    [DllImport("youdoo_plugin")]
    protected static extern bool GetDetectBoxInfoByType(long frameInfoPtr, int detectType, int objectId,
        out float left, out float top, out float right, out float bottom,
        out float score, out int keypointCount, out int type);


    /// <summary>
    /// 获取这个检测对象的关键点
    /// </summary>
    /// <param name="frameInfoPtr">帧指针</param>
    /// <param name="detectType">检测类型</param>
    /// <param name="objectId">对象ID</param>
    /// <param name="keypointIndex">关键点索引</param>
    /// <param name="x">X坐标</param>
    /// <param name="y">Y坐标</param>
    /// <param name="z">Z坐标</param>
    /// <param name="score">置信度</param>
    /// <returns></returns>
    [DllImport("youdoo_plugin")]
    protected static extern bool GetKeyPointByType(long frameInfoPtr, int detectType, int objectId, int keypointIndex,
        out float x, out float y, out float z, out float score);

    /// <summary>
    /// 在检查人物准备的时候，调用这个方法， 
    /// 在指定的帧中
    ///遍历所有检测到的人物
    ///对每个人物检查是否满足目标状态（如举手、抬脚等）
    ///检查每个人物分别位于哪个预定义的区域内
    ///为每个区域返回满足条件且面积最大的人物ID
    /// </summary>
    /// <param name="frame_index">第X帧</param>
    /// <param name="rectFArray">区域的参数。是  RectF 的 数组， </param>
    /// <param name="rectFArray_count">  rectFArray 有多少个区域 </param>
    /// <param name="target_state">需要识别的动作 对应的是 PersonState值 </param>
    /// <param name="exclude_person_ids">不想之别这些人</param>
    /// <param name="exclude_count">不想识别这些人的数量</param>
    /// <param name="output_person_ids">把选中的人放到这个数组里面。</param>
    /// <returns></returns>
    [DllImport("youdoo_plugin")]
    protected static extern bool CheckPersonReadyAllRegions(
            int frame_index,
            float[] rectFArray,
            int rectFArray_count,
            int target_state,
            int[] exclude_person_ids,
            int exclude_count,
            int[] output_person_ids
        );

    public bool isCheckPersonReadyIng = true; //表示的是当前是游戏的 准备阶段。 
    public int[] personPlayerIds; //参与游戏的玩家的ID,保存的是确定需要玩的玩家。
    public int[] personPlayerLockIng; //锁定区域中的人，只有当锁定的人举手时才确认为玩家。
    public float[,] personPlayerReadyRectf;//人物准备动作的判定区域。

    /// <summary>
    /// 大部分游戏，
    /// 每一帧都会去获取当前的用户的骨骼的信息
    /// 然后进行加工。
    /// 请根据实际的情况去使用。
    /// </summary>
    public virtual void UpdateGetPersonData()
    {
        // #if !UNITY_EDITOR
        // 获取最新帧信息的指针
        long frameInfoPtr = GetLatestFrameInfoPtr();
        if (frameInfoPtr == 0)
        {
            Debug.Log("数据：165 没有数据");
            return;
        }


        GetFrameInfoBasicByType(frameInfoPtr, (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
  out long frameTimeMs, out int width, out int height, out long aHardwareBufferPtr, out int personCount);
        int[] currentFramePersonIds = new int[personCount];


        int perSonNumber = GetPersonIdsByType(frameInfoPtr, (int)YouDooSDKConstants.DetectType.DETECT_TYPE_PERSON,
            currentFramePersonIds, personCount);


        TraversePersion(frameInfoPtr, currentFramePersonIds, perSonNumber);
        CheckPersonReadyIng(0, currentFramePersonIds);
        GameLogic(frameInfoPtr, currentFramePersonIds, perSonNumber, aHardwareBufferPtr);
        // #endif
    }

    /// <summary>
    /// SDK 4.0 开始  aHardwareBufferPtr 已经没有意义。
    /// </summary>
    /// <param name="frameInfoPtr"></param>
    /// <param name="currentFramePersonIds"></param>
    /// <param name="perSonNumber"></param>
    /// <param name="aHardwareBufferPtr"></param>
    protected virtual void GameLogic(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber, long aHardwareBufferPtr)
    {

    }

    /// <summary>
    /// 遍历 这一帧的人物的数据
    /// 请根据实际的需求覆写这个方法。
    /// </summary>
    protected virtual void TraversePersion(long frameInfoPtr, int[] currentFramePersonIds, int perSonNumber)
    {

    }

    /// <summary>
    /// 游戏开始前 ，先锁定区域中的人，只有当锁定的人举手时才确认为玩家
    /// </summary>
    /// <param name="frameIndex">通常用的都是第0帧的数据。</param>
    /// <param name="currentFramePersonIds">游戏当前拥有的ID</param>
    protected virtual void CheckPersonReadyIng(int frameIndex, int[] currentFramePersonIds)
    {
        // 清理可能丢失的人
        CleanupMissingPlayersWhenReaday(currentFramePersonIds);

        // 第一步：锁定区域中的人（如果还没有锁定的话）
        LockPlayersInRegions(frameIndex, currentFramePersonIds);

        // 第二步：检查锁定的人是否举手，如果举手就确认为玩家
        CheckLockedPlayersReady(frameIndex);

        // 第三步：检查是否所有玩家都已确认
        CheckAllPlayersConfirmed();
    }


    /// <summary>
    /// 锁定各个区域中的人 2025.10.09 。 
    /// </summary>
    protected virtual void LockPlayersInRegions(int frameIndex, int[] currentFramePersonIds)
    {
        int regionCount = personPlayerReadyRectf.Length / 4;

        // 收集所有需要排除的玩家ID
        HashSet<int> excludedIds = new HashSet<int>();

        // 添加已确认的玩家（从personPlayerIds中获取）
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (personPlayerIds[i] != YouDooSDKConstants.PersonIdNull)
            {
                excludedIds.Add(personPlayerIds[i]);
            }
        }

        // 每一帧都得重置Locking的人。因为这个人在变成player之前会走来走去。
        for (int i = 0; i < personPlayerLockIng.Length; i++)
        {
            personPlayerLockIng[i] = YouDooSDKConstants.PersonIdNull;
        }

        int[] excludeArray = excludedIds.ToArray();

        for (int i = 0; i < regionCount; i++)
        {
            // 如果这个区域还没有锁定玩家，且还没有确认玩家
            if (personPlayerLockIng[i] == YouDooSDKConstants.PersonIdNull &&
                personPlayerIds[i] == YouDooSDKConstants.PersonIdNull)
            {
                // 获取这个区域中置信度最高的人
                float left = personPlayerReadyRectf[i, 0];
                float top = personPlayerReadyRectf[i, 1];
                float right = personPlayerReadyRectf[i, 2];
                float bottom = personPlayerReadyRectf[i, 3];

                int foundPersonId = CheckPersonScoreInArea(
                    frameIndex,
                    left, top, right, bottom,
                    excludeArray, excludeArray.Length
                );

                if (foundPersonId != YouDooSDKConstants.PersonIdNull)
                {
                    personPlayerLockIng[i] = foundPersonId;
                    // 立即添加到排除列表，避免后续区域重复锁定同一玩家
                    excludedIds.Add(foundPersonId);
                    excludeArray = excludedIds.ToArray();
                }
            }
        }
    }
    /// <summary>
    /// 检查锁定的玩家是否举手
    /// </summary>
    protected virtual void CheckLockedPlayersReady(int frameIndex)
    {
        
    }

    /// <summary>
    /// 检查是否所有玩家都已确认
    /// </summary>
    protected virtual void CheckAllPlayersConfirmed()
    {
        int readyCount = 0;
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            if (personPlayerIds[i] != YouDooSDKConstants.PersonIdNull)
            {
                readyCount++;
            }
        }
        if (readyCount >= personPlayerIds.Length)
        {
            isCheckPersonReadyIng = false;
            // 游戏开始后可以清理锁定数组，因为不再需要
            for (int i = 0; i < personPlayerLockIng.Length; i++)
            {
                personPlayerLockIng[i] = YouDooSDKConstants.PersonIdNull;
            }
        }
    }

  

    /// <summary>
    /// 我想知道这个人在第X帧的动作。
    /// </summary>
    public virtual void CheckSinglePersonAction(int frameIndex, int personId)
    {
        int[] states = new int[(int)YouDooSDKConstants.PersonState.STATE_COUNT];
        bool success = CheckPersonAction(frameIndex, personId, states, states.Length);
        if (success)
        {
            foreach (int value in states)
            {
                YouDooSDKConstants.PersonState state = (YouDooSDKConstants.PersonState)value;
                switch (state)
                {
                    case YouDooSDKConstants.PersonState.STATE_LEFT_HAND_UP:
                    case YouDooSDKConstants.PersonState.STATE_RIGHT_HAND_UP:
                    case YouDooSDKConstants.PersonState.STATE_LEFT_FOOT_UP:
                    case YouDooSDKConstants.PersonState.STATE_RIGHT_FOOT_UP:
                    case YouDooSDKConstants.PersonState.STATE_BOTH_HANDS_UP:
                    case YouDooSDKConstants.PersonState.STATE_HANDS_CROSSED_OVERHEAD:
                    case YouDooSDKConstants.PersonState.STATE_CLAPPING_HANDS:
                        Debug.Log($"人物识别到：{personId} = 状态: {state}");
                        break;
                    case YouDooSDKConstants.PersonState.STATE_ERROR:
                        Debug.Log($"人物识别到：{personId} = 状态: {state} 错误");
                        break;
                    case YouDooSDKConstants.PersonState.STATE_UNKNOWN:
                        return;
                    default:
                        Debug.Log($"人物识别到：{personId} = 状态: {state}");
                        break;
                }
            }
        }
    }
    /// <summary>
    /// 检查和清理丢失的玩家
    /// </summary>
    /// <param name="currentFramePersonIds"></param>
    /// <returns>true 玩家有缺失 </returns>
    protected virtual bool CheckAndCleanupMissingPlayers(int[] currentFramePersonIds)
    {
        bool hasCleanup = false;
        var currentPersonSet = new HashSet<int>();

        foreach (int id in currentFramePersonIds)
        {
            if (id != YouDooSDKConstants.PersonIdNull)
                currentPersonSet.Add(id);
        }
        // 清理已确认玩家
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            int playerId = personPlayerIds[i];
            if (playerId == YouDooSDKConstants.PersonIdNull)
            {
                hasCleanup = true;
            }
            else if (playerId != YouDooSDKConstants.PersonIdNull && !currentPersonSet.Contains(playerId))
            {
                personPlayerIds[i] = YouDooSDKConstants.PersonIdNull;
                hasCleanup = true;
            }
        }
        return hasCleanup;
    }

    /// <summary>
    /// 检查有哪些人离场了。
    /// </summary>
    protected virtual bool CleanupMissingPlayersWhenReaday(int[] currentFramePersonIds)
    {
        bool hasCleanup = false;
        var currentPersonSet = new HashSet<int>();

        foreach (int id in currentFramePersonIds)
        {
            if (id != YouDooSDKConstants.PersonIdNull)
                currentPersonSet.Add(id);
        }

        // 清理已确认玩家
        for (int i = 0; i < personPlayerIds.Length; i++)
        {
            int playerId = personPlayerIds[i];
            if (playerId != YouDooSDKConstants.PersonIdNull && !currentPersonSet.Contains(playerId))
            {
                personPlayerIds[i] = YouDooSDKConstants.PersonIdNull;
                hasCleanup = true;
            }
        }


        // 同时清理锁定玩家
        for (int i = 0; i < personPlayerLockIng.Length; i++)
        {
            int lockedId = personPlayerLockIng[i];
            if (lockedId != YouDooSDKConstants.PersonIdNull && !currentPersonSet.Contains(lockedId))
            {
                personPlayerLockIng[i] = YouDooSDKConstants.PersonIdNull;
                hasCleanup = true;
            }
        }
        return hasCleanup;
    }

}