# Buff System

## 概述

一个基于 Unity 的模块化 Buff 系统，支持三种 Buff 叠加模式，通过配置驱动、代码生成来实现快速扩展。

## 目录结构

```
Assets/BuffSystem/
├── AutomationScripts/    # 自动生成的 Buff 逻辑脚本（继承对应基类）
│   ├── Buff10001.cs      # 示例：数值叠加 + 时间刷新
│   ├── Buff10002.cs      # 示例：每层独立计时
│   └── Buff10003.cs      # 示例：不可叠加，仅刷新时间
├── Editor/               # 编辑器工具
│   ├── BuffHandleGenerater.cs  # 代码生成器（菜单：Tools/生成BuffHandle脚本）
│   └── GeneraterHelper.cs      # 生成辅助类
├── Resources/BuffData/   # Buff 配置文件（ScriptableObject）
├── Scripts/              # 核心脚本
│   ├── BuffComponent.cs          # Buff 管理器（挂载到目标对象上）
│   ├── BuffConfig.cs             # Buff 配置 ScriptableObject
│   ├── BuffData.cs               # Buff 运行时数据
│   ├── BuffEffectBase.cs         # Buff 效果基类（生命周期定义）
│   ├── IndependentEffectBase.cs  # 基类：数值叠加 + 时间刷新
│   ├── StackableEffectBase.cs    # 基类：每层独立计时
│   └── RefreshableEffectBase.cs  # 基类：不可叠加，仅刷新时间
└── Tool/                 # 自动生成的工具脚本
    └── BuffHandle.cs
```

## 三种 Buff 类型

| 类型 | 基类 | 行为描述 |
|------|------|---------|
| **数值叠加 + 时间刷新** | `IndependentEffectBase` | 相同 ID 的 Buff 只存在一个实例。重复施加时层数叠加（`Count++`），持续时间刷新为完整时长。层数受 `stack_count_` 上限限制。 |
| **每层独立计时** | `StackableEffectBase` | 相同 ID 的 Buff 每层独立存在。每层有自己的持续时间和触发间隔，过期后单层移除。适用于可叠层的 DoT / HoT。 |
| **不可叠加** | `RefreshableEffectBase` | 相同 ID 的 Buff 只存在一层，固定 `Count = 1`。重复施加仅刷新持续时间。适用于单体 Buff（护盾、加速等）。 |

## 核心架构

```
IBuffEffect（接口）
  └── BuffEffectBase（抽象基类）       ← 特效管理、生命周期回调
        ├── IndependentEffectBase       ← 叠加层数 + 刷新时间
        ├── StackableEffectBase         ← 独立层计时
        └── RefreshableEffectBase       ← 不可叠加
```

### BuffComponent（Buff 管理组件）

挂载到需要接收/施加 Buff 的 GameObject 上。核心职责：

- `AddBuff(buff_id, duration, imposer, count)` — 添加 Buff
- `RemoveBuff(buff_data)` — 移除 Buff
- `RemoveBuffByID(buff_id)` — 按 ID 移除
- `RemoveAllBuffs()` — 移除所有 Buff

事件回调：

| 事件 | 说明 |
|------|------|
| `OnAddBuffLayer` | 层数增加时触发 |
| `OnRemoveBuffLayer` | 层数减少时触发（仅 StackableEffectBase） |
| `OnBuffDataRemoved` | Buff 被完全移除时触发 |
| `OnActiveBuffCountChange` | 活跃 Buff 数量变化时触发 |

### BuffConfig（配置资产）

通过 `CreateAssetMenu` 创建 Buff 配置 ScriptableObject：

| 字段 | 说明 |
|------|------|
| `buff_id_` | 唯一标识符 |
| `name_` | Buff 名称 |
| `icon_` | Buff 图标 |
| `duration_` | 持续时间（秒） |
| `value_` | 效果值（治疗量/攻击力增幅等） |
| `stack_count_` | 最大叠加层数（**重要：IndependentEffectBase 和 StackableEffectBase 受此限制**） |
| `effect_prefab_` | 特效预制体 |

### BuffData（运行时数据）

`BuffData` 从 `BuffConfig` 读取配置并存储运行时状态。`Count` 属性的 setter 被 `Mathf.Clamp` 钳制到 `[0, max_count_]` 范围内。

> ⚠ **注意**：若 `StackableEffectBase` 的 `stack_count_` 设为 1，每次添加新层会先踢掉旧层，导致永远只有 1 层。请确保 `stack_count_` 大于等于预期的最大叠加层数。

## 快速入门

### 1. 创建 Buff 配置

在 Project 窗口中右键 → `Create → BuffData → CreateBuffData`，填写配置参数。

### 2. 创建 Buff 逻辑脚本

在 `Assets/BuffSystem/AutomationScripts/` 下创建 C# 脚本，继承对应基类：

```csharp
// 示例：数值叠加类型的 Buff
using UnityEngine;
public class Buff10001 : IndependentEffectBase
{
    public override void Init()
    {
        base.Init();
        // 初始化逻辑
    }
    public override void Iteration()
    {
        base.Iteration();
        // 每次叠加时执行
    }
    public override void Update()
    {
        base.Update();
        // 每帧更新
    }
    public override void Release()
    {
        base.Release();
        // Buff 移除时清理
    }
}
```

### 3. 生成 BuffHandle 代码

点击菜单 `Tools → 生成BuffHandle脚本`，系统会自动扫描 `AutomationScripts/` 下的所有 Buff 脚本并生成 `BuffHandle.cs`。

### 4. 使用

```csharp
// 获取对象上的 BuffComponent
BuffComponent comp = gameObject.GetComponent<BuffComponent>();

// 添加 Buff
comp.AddBuff(10001, duration: -1, imposer: attackerComp, count: 1);
// duration 为 -1 时使用配置中的默认时长
```

## 生命周期

```
AddBuff()
  ├── 首次添加（Buff 不存在）
  │     ├── BuffDataFactory.CreateEffect()  创建 BuffData
  │     ├── effect_.Init()                  初始化
  │     └── effect_.Iteration()             首次迭代
  └── 重复添加（Buff 已存在）
        └── BuffIteration()
              ├── data_.duration_ = duration    刷新时间
              └── effect_.Iteration()           迭代（叠加层数）

Update()
  └── effect_.Update()                      每帧更新有效期

过期 → effect_.Release() → target_buff_map_.Remove()
```

## 需求对应关系

| # | 需求 | 基类 |
|---|------|------|
| 1 | 数值叠加然后时间刷新 | `IndependentEffectBase` |
| 2 | 每层独立计时，数值也独立，相当于多个相同 ID 的 Buff | `StackableEffectBase` |
| 3 | 不可叠加层数的 Buff，只有计时刷新 | `RefreshableEffectBase` |