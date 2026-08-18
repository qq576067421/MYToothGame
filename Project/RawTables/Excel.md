# ExcelCli Usage

适用范围：

- 本文档只描述给 AI 使用的 Excel 修改工具。
- 默认工作目录是 `D:\lichunlin\MYToothGame\Project\RawTables`
- 可执行文件路径是 `Tools\ExcelCli.exe`

## 重要约束

- 禁止使用 `csv -> 覆盖回写 excel` 的方式修改源表。
- AI 修改配置表时，必须直接操作 Excel 源表中的 `sheet / row / cell`。
- 原因是项目里的 Excel 可能包含额外说明列、注释列、人工维护列，这些列不一定参与 CSV 导出。
- 导出 CSV 时会根据表头规则判断是否导出某列，因此不能假设 Excel 只由 CSV 可逆还原。
- `ExcelCli` 的默认使用方式是直接修改 workbook 并保存，不是整 sheet 用 CSV 重建。
- 修改前优先先 `dump-sheet` 看清前 4 行和实际列布局，避免误伤人工维护列。

## 目录约定

推荐 AI 先切到 `RawTables` 再调用：

```bat
cd /d D:\lichunlin\MYToothGame\Project\RawTables
```

这样可以直接使用：

- `j*.xlsm`
- `b*.xlsx`
- `jn*.xlsx`

这类通配符路径，避免中文和空格文件名带来的参数问题。

## 已支持命令

### 1. 列出工作表

```bat
Tools\ExcelCli.exe list-sheets j*.xlsm
```

作用：

- 输出 workbook 中的所有 sheet 名称

### 2. 打印 sheet 前几行

```bat
Tools\ExcelCli.exe dump-sheet j*.xlsm t_heroBean 8
```

参数：

- `workbook`: excel 文件路径或通配符
- `sheet`: sheet 名
- `rowCount`: 打印的行数，可省略，默认 8

### 3. 读取单元格

```bat
Tools\ExcelCli.exe get-cell j*.xlsm t_heroBean A5
```

参数：

- `cellRef`: Excel 坐标，如 `A5`、`R1`

### 4. 修改单元格

```bat
Tools\ExcelCli.exe set-cell j*.xlsm t_heroBean A5 1000_ai
```

作用：

- 直接把指定 cell 写成目标值
- 写完会自动保存 workbook

### 5. 按主键修改字段

```bat
Tools\ExcelCli.exe set-field-by-key j*.xlsm t_heroBean t_id 1001 t_desc hero_tooth_2_desc_ai
```

参数：

- `sheet`
- `keyColumn`
- `keyValue`
- `targetColumn`
- `value`

默认规则：

- 表头在第 1 行（索引 0）
- 数据从第 5 行开始（索引 4）

也就是默认适配当前 RawTables 配置表格式：

1. 字段名
2. 占位说明
3. 类型
4. 描述
5. 数据起始

### 6. 新增字段

```bat
Tools\ExcelCli.exe add-field j*.xlsm t_heroBean t_ai_test ~s string ai_test_desc
```

作用：

- 在表尾新增一列字段
- 同时写入前四行：
  - 字段名
  - 占位值
  - 类型
  - 描述

可选参数：

```bat
Tools\ExcelCli.exe add-field j*.xlsm t_heroBean t_ai_test ~s string ai_test_desc 5
```

这里最后的 `5` 表示把字段插入到指定列索引。

### 7. 追加一行数据

```bat
Tools\ExcelCli.exe append-row j*.xlsm t_heroBean Tools\EXCEL\tmp\append-row.json
```

其中 `append-row.json` 的格式是一个简单对象：

```json
{
  "t_id": "9000",
  "t_name": "hero_append",
  "t_desc": "hero_append_desc",
  "t_job": "2"
}
```

规则：

- key 必须是表头字段名
- 只会写你提供的字段
- 新行默认追加到数据区最后一条非空记录之后

### 8. 按主键删除一行

```bat
Tools\ExcelCli.exe delete-row-by-key j*.xlsm t_heroBean t_id 9000
```

作用：

- 在 `t_heroBean` 里找到 `t_id=9000` 的数据行
- 删除这一整行

### 9. 批量执行 JSON 操作

```bat
Tools\ExcelCli.exe apply-json Tools\EXCEL\tmp\apply-json.json
```

`apply-json.json` 格式：

```json
{
  "workbook": "D:\\lichunlin\\MYToothGame\\Project\\RawTables\\Tools\\EXCEL\\tmp\\j_role_test.xlsm",
  "operations": [
    {
      "op": "set-field-by-key",
      "sheet": "t_heroBean",
      "keyColumn": "t_id",
      "keyValue": "1002",
      "targetColumn": "t_desc",
      "value": "hero_tooth_3_desc_json"
    },
    {
      "op": "append-row",
      "sheet": "t_heroBean",
      "values": {
        "t_id": "9001",
        "t_name": "hero_json",
        "t_desc": "hero_json_desc"
      }
    }
  ]
}
```

当前 `apply-json` 已支持的 `op`：

- `set-cell`
- `set-field-by-key`
- `add-field`
- `append-row`
- `delete-row-by-key`

## 当前最推荐的调用方式

针对当前项目里的配置表，AI 优先使用下面这组命令：

```bat
Tools\ExcelCli.exe list-sheets j*.xlsm
Tools\ExcelCli.exe dump-sheet j*.xlsm t_heroBean 8
Tools\ExcelCli.exe set-cell j*.xlsm t_heroBean A5 1000_ai
Tools\ExcelCli.exe set-field-by-key j*.xlsm t_heroBean t_id 1001 t_desc hero_tooth_2_desc_ai
Tools\ExcelCli.exe add-field j*.xlsm t_heroBean t_ai_test ~s string ai_test_desc
Tools\ExcelCli.exe append-row j*.xlsm t_heroBean Tools\EXCEL\tmp\append-row.json
Tools\ExcelCli.exe delete-row-by-key j*.xlsm t_heroBean t_id 9000
Tools\ExcelCli.exe apply-json Tools\EXCEL\tmp\apply-json.json
```

## 建议

- 优先用通配符文件名，不直接传中文全路径
- 先 `dump-sheet` 看结构，再决定修改
- 修改配置表后，再执行现有导表流程：

```bat
导客户端表.bat
```

## 当前能力汇总

当前版本已支持：

- 读 sheet
- 读 cell
- 改 cell
- 按 key 改字段
- 加字段
- 追加数据行
- 按 key 删除数据行
- 用 JSON 批量执行操作
