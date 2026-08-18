# RichTextEx - UGUI 图文混排组件

## 概述

`RichTextEx` 是一个基于 Unity UGUI 的图文混排组件，**不依赖 TextMeshPro**。它支持在文本中内嵌图片、富文本样式、超链接、下划线、删除线，并遵循中文排版禁则和英文单词不断行规则。

---

## 快速上手

### 1. 在 Inspector 中添加

1. 在场景中创建一个 GameObject
2. 添加组件：`UI > RichTextEx`（或搜索 `RichTextEx`）
3. 在 Inspector 面板中设置字体、字号、颜色等属性
4. 在 **文本内容 (Text)** 字段中输入带标签的文字

### 2. 通过代码设置

```csharp
using UnityUI;

// 获取组件
RichTextEx richText = GetComponent<RichTextEx>();

// 设置文本
richText.SetText("获得了<img=texture_set/item:gold_icon,24,24>x100金币");
```

---

## 支持的标签

### 图片标签
```
<img=abName:assetName,width,height>
```

| 参数 | 说明 | 示例 |
|------|------|------|
| `abName` | AB包路径 | `texture_set/item` 或 `ui/hero_icon` |
| `assetName` | 资源名（图集时用冒号分隔） | `gold_icon` |
| `width` | 图片宽度（像素，可选） | `24` |
| `height` | 图片高度（像素，可选） | `24` |

**图集Sprite**（以 `texture_set/` 开头）：
```
<img=texture_set/item:gold_icon,24,24>
```

**普通Texture**：
```
<img=ui/hero_icon,64,64>
```

### 颜色
```
<color=#FF0000>红色文本</color>
<color=#00FF00AA>半透明绿色</color>
```

### 字号
```
<size=30>大号字体</size>
```

### 粗体 / 斜体
```
<b>粗体文本</b>
<i>斜体文本</i>
```

### 下划线
```
<u>带下划线的文本</u>
```

### 删除线
```
<s>被删除的文本</s>
```

### 超链接
```
<a=https://example.com>点击这里</a>
<a=item:12345>查看装备</a>
```

---

## API 参考

### RichTextEx 组件

#### 属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `Font` | `Font` | 使用的字体（默认加载 `Assets/art/out/font/notosanshans-thin.ttf`） |
| `FontSize` | `int` | 默认字号，默认值 24 |
| `LineSpacing` | `float` | 行间距倍数，默认值 1.2 |
| `Alignment` | `TextAnchor` | 文本对齐方式（左/中/右 × 上/中/下） |
| `Text` | `string` | 文本内容（支持标签） |
| `PreferredHeight` | `float` | 只读，排版后的内容高度（可用于自适应） |

#### 方法

| 方法 | 说明 |
|------|------|
| `SetText(string text)` | 设置文本内容（主入口） |

#### 事件

| 事件 | 参数 | 说明 |
|------|------|------|
| `OnHyperlinkClick` | `string url` | 超链接被点击时触发 |
| `OnImageClick` | `ImageElementInfo info` | 内联图片被点击时触发 |

---

## 使用示例

### 聊天系统
```csharp
RichTextEx chatRichText = GetComponent<RichTextEx>();

string chatText = "玩家：<color=#FF0000>[世界]</color> 获得了" +
    "<img=texture_set/item:gold_icon,24,24>x100";
chatRichText.SetText(chatText);
```

### 技能描述
```csharp
string skillDesc = "<size=28><color=#FFFF00>火球术</color></size>\n" +
    "向目标发射一枚火球，造成200点火焰伤害，" +
    "并有<color=#FF0000>50%</color>几率造成<img=texture_set/effect:burn,16,16>灼烧效果";
skillRichText.SetText(skillDesc);
```

### 任务说明
```csharp
string questDesc = "任务目标：\n" +
    "1. 击败<img=texture_set/monster:goblin,20,20>哥布林 x10\n" +
    "2. 收集<img=texture_set/item:herb,20,20>草药 x5";
questRichText.SetText(questDesc);
```

### 超链接回调
```csharp
richText.OnHyperlinkClick += (url) =>
{
    Debug.Log("点击了超链接: " + url);
    // 自定义处理，如打开网页、跳转界面等
};
```

### 图片点击回调
```csharp
richText.OnImageClick += (imageInfo) =>
{
    Debug.Log("点击了图片: " + imageInfo.abName + ":" + imageInfo.assetName);
    // 自定义处理，如弹出详情面板
};
```

---

## 运行 Demo

1. 在场景中创建一个空 GameObject
2. 添加 `RichTextExDemo` 脚本
3. 运行场景即可看到聊天、技能描述、任务说明、样式展示等示例

---

## 排版规则

### 中文标点禁则
- 以下标点符号**不会出现在行首**：`，`, `,`, `。`, `.`, `！`, `!`, `？`, `?`, `：`, `:`, `；`, `;`, `）`, `)`, `】`, `]`, `」`, `』`, `》`, `〉`, `、`, `"`, `'`, `…`, `—`, `～`, `·`
- 以下标点符号**不会出现在行尾**：`（`, `(`, `【`, `[`, `「`, `『`, `《`, `〈`, `"`

### 英文单词不断行
- 连续的拉丁字母、数字、连字符、撇号组成的单词不会在中间断行
- 如果单个单词超过容器宽度，则允许溢出（避免无限循环）

---

## 文件结构

```
Assets/Scripts/ThirdParty/UnityUI/Component/RichTextEx/
├── RichTextEx.cs          # 主组件（继承 MaskableGraphic）
├── RichTextParser.cs      # 富文本解析器
├── InlineImageManager.cs  # 内联图片管理（对象池 + 异步加载）
├── RichTextExDemo.cs      # 演示脚本
├── Editor/
│   └── RichTextExEditor.cs # 自定义 Inspector
└── RichTextEx_README.md   # 本文档
```

---

## 资源加载

图片加载使用项目已有的 `LCL.TextureManager.SetImageSprite` 方法：
- `texture_set/` 开头的路径会作为图集Sprite加载
- 其他路径作为普通Texture加载
- 支持异步加载，不阻塞主线程
- 内置缓存机制，避免重复加载

---

## 性能特性

- **对象池**：内联图片使用对象池管理，`RecycleAll()` 回收，避免频繁 `Instantiate/Destroy`
- **资源缓存**：已加载的 Texture/Sprite 会被缓存复用
- **按需排版**：仅在内容或尺寸变化时重新排版，使用脏标记机制
- **VertexHelper 直接生成**：文字顶点直接通过 `OnPopulateMesh` 生成，无额外中间对象
