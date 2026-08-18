# 声音资源目录规范

`sound` 的一级目录只表示音频导入策略，实际业务用途继续放在下级目录。新增声音时应先按时长和使用频率选择一级目录，再配置对应的混音器输出分组。

## streaming

- 用于持续时间较长的背景音乐。
- 推荐 `Load Type` 设置为 `Streaming`。
- 当前业务目录：`music`。
- 安卓平台需要同时检查平台覆盖设置，不能只修改默认平台。

## compressed

- 用于中长语音，以及不适合完整解压到内存的声音。
- 推荐 `Load Type` 设置为 `Compressed In Memory`。
- 当前业务目录：`voice`。
- 语音预制件输出到 `VoiceNormal`。

## short_effect

- 用于高频、短小的界面和战斗音效。
- 推荐 `Load Type` 设置为 `Decompress On Load`。
- `ui` 中的预制件输出到 `UINormal`。
- `world` 中的预制件输出到 `WorldNormal`，其中 `effect` 存放战斗效果声音，`player` 存放角色攻击声音。

目录规范只提供推荐设置，不增加强制导入器或构建拦截。声音配置表必须填写预制件对应的新 AssetBundle 路径，不允许业务代码直接传声音资源路径播放。
