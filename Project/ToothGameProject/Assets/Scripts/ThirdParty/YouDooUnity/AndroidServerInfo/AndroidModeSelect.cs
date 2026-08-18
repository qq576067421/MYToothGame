using System;
using UnityEngine;

[System.Serializable]
public class GameServiceConfig
{
    public CameraConfig[] camList;
    public ModelConfig[] modelList;

    //SDK4.1 新增字段，是否禁用过滤。默认值是false，也就是不禁用过滤。开发者可以根据自己的需求来设置这个值。 
    public bool disableFiltering;

}

[System.Serializable]
public class CameraConfig
{
    public string cameraId;
    public int lensFacing; // 0=前置, 1=后置
    public Resolution[] resolutions;
}

[System.Serializable]
public class ModelConfig
{
    public string format;
    public string id;
    public int inputHeight;
    public int inputWidth;
    public int kptNum;
    public string model;
    public string name;
    public string optimizedFor;
    public string quantization;
    public string type;
}

[System.Serializable]
public class Resolution
{
    public int height;
    public int width;
}


public class AndroidModeSelect
{
    private GameServiceConfig _allConfig;

    public GameServiceConfig AllConfig { get => _allConfig; set => _allConfig = value; }

    public void SetAllModelConfig(string configList)
    {
        int maxLength = 1000; // Unity Console每段大约限制
        for (int i = 0; i < configList.Length; i += maxLength)
        {
            int length = Math.Min(maxLength, configList.Length - i);
            Debug.Log(configList.Substring(i, length));
        }

        _allConfig = new GameServiceConfig();
        _allConfig = JsonUtility.FromJson<GameServiceConfig>(configList);
        Debug.Log($"解析配置 JSON 字符串 完成: {_allConfig.modelList.Length} 个模型配置，{_allConfig.camList.Length} 个摄像头配置");
        foreach (var model in _allConfig.modelList)
        {
            Debug.Log($"格式format:{model.format},模型ID: {model.id},输入尺寸inputWidth X inputHeight: {model.inputWidth}x{model.inputHeight}");
            Debug.Log($"关键点数kptNum: {model.kptNum},模型model: {model.model}, 名字name: {model.name}");
            Debug.Log($"优化optimizedFor: {model.optimizedFor},量化quantization: {model.quantization}, 类型type: {model.type}");
        }
        // 获取摄像头支持的分辨率
        // foreach (var camera in _allConfig.camList)
        // {
        //     Debug.Log($"摄像头: {camera.cameraId}, 朝向: {camera.lensFacing}");
        //     foreach (var res in camera.resolutions)
        //     {
        //         Debug.Log($"支持分辨率: {res.width}x{res.height}");
        //     }
        // }
    }

    /// <summary>
    /// </summary>
    /// <param name="_modeChangeIndex"></param>
    /// <param name="_cameraChangeIndex"></param>
    /// <param name="_resolutionChangeIndex"></param>
    /// <param name="disableFiltering">SDK 4.1 开始增加怎个参数。默认是就是false </param>
    /// <returns></returns>
    public GameServiceConfig SetNeedModelConfig(int _modeChangeIndex, int _cameraChangeIndex, int _resolutionChangeIndex, bool disableFiltering = false)
    {
        if (_allConfig == null)
        {
            Debug.LogError("模型配置未初始化，请先调用 SetAllModelConfig 方法。");
            return null;
        }

        if (_modeChangeIndex >= _allConfig.modelList.Length)
        {
            _modeChangeIndex = 0;
        }

        if (_cameraChangeIndex >= _allConfig.camList.Length)
        {
            _cameraChangeIndex = 0;
        }

        if (_resolutionChangeIndex >= _allConfig.camList[_cameraChangeIndex].resolutions.Length)
        {
            _resolutionChangeIndex = 0;
        }
        var selectModel = new GameServiceConfig
        {
            disableFiltering = disableFiltering,
            modelList = new[] { _allConfig.modelList[1], _allConfig.modelList[2] },
            camList = new[]
            {
                new CameraConfig
                {
                cameraId = _allConfig.camList[_cameraChangeIndex].cameraId,
                lensFacing =_allConfig.camList[_cameraChangeIndex].lensFacing,
                resolutions = new[] { _allConfig.camList[_cameraChangeIndex].resolutions[_resolutionChangeIndex] }
                }
            }
        };

        string targetStr = JsonUtility.ToJson(selectModel, false);
#if !UNITY_EDITOR
        AndroidServerInfo.Instance.SetCurUseMode(targetStr);
#endif
        return selectModel;
    }

    public GameServiceConfig SetNeedModelConfig(string[] modeNames, string cameraId, int resolutionH, int resolutionW, bool disableFiltering = false)
    {
        if (_allConfig == null)
        {
            Debug.LogError("模型配置未初始化，请先调用 SetAllModelConfig 方法。");
            return null;
        }

        System.Collections.Generic.List<ModelConfig> selectedModels = new System.Collections.Generic.List<ModelConfig>();
        int _cameraChangeIndex = 0;
        int _resolutionChangeIndex = 0;

        if (modeNames != null && modeNames.Length > 0)
        {
            foreach (var requestedModeName in modeNames)
            {
                for (int index = 0; index < _allConfig.modelList.Length; index++)
                {
                    var element = _allConfig.modelList[index];
                    if (element.name == requestedModeName)
                    {
                        selectedModels.Add(element);
                        break; // 找到当前请求的模型即可
                    }
                }
            }
        }

        // 如果都没找到，给个默认的第一项防止报错
        if (selectedModels.Count == 0 && _allConfig.modelList.Length > 0)
        {
            Debug.LogWarning("未找到匹配的模型名，默认使用配置中第一个模型。");
            selectedModels.Add(_allConfig.modelList[0]);
        }

        for (int index = 0; index < _allConfig.camList.Length; index++)
        {
            var element = _allConfig.camList[index];
            if (element.cameraId == cameraId)
            {
                _cameraChangeIndex = index;
                break;
            }
        }

        for (int index = 0; index < _allConfig.camList[_cameraChangeIndex].resolutions.Length; index++)
        {
            var element = _allConfig.camList[_cameraChangeIndex].resolutions[index];
            if (element.height == resolutionH && element.width == resolutionW)
            {
                _resolutionChangeIndex = index;
                break;
            }
        }

        var selectModel = new GameServiceConfig
        {
            disableFiltering = disableFiltering,
            modelList = selectedModels.ToArray(),
            camList = new[]
            {
                new CameraConfig
                {
                cameraId = _allConfig.camList[_cameraChangeIndex].cameraId,
                lensFacing =_allConfig.camList[_cameraChangeIndex].lensFacing,
                resolutions = new[] { _allConfig.camList[_cameraChangeIndex].resolutions[_resolutionChangeIndex] }
                }
            }
        };

        string targetStr = JsonUtility.ToJson(selectModel, false);
#if !UNITY_EDITOR
        AndroidServerInfo.Instance.SetCurUseMode(targetStr);
#endif
        return selectModel;
    }
}
