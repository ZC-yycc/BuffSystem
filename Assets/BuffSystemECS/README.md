# Buff System ECS

## 概述

基于 **ECS（Entity-Component-System）** 架构的模块化 Buff 系统。将 Buff 的行为拆分为纯数据（Component）和纯逻辑（System），遵循 ECS 的数据与行为分离原则。

与原 `BuffSystem`（GameObject + MonoBehaviour 继承）的区别：

| 特点 | 原 BuffSystem | BuffSystemECS |
|------|:---:|:---:|
| 架构 | GameObject + 继承 | ECS（数据/行为分离） |
| 数据存储 | `BuffData` class + 虚方法 | `BuffRuntimeData` struct（纯数据） |
| 生命周期逻辑 | 虚方法（Init/Update/Release） | System.Update 统一处理 |
| 实体标识 | GameObject Component 引用 | `BuffEntity` struct 值类型 |
| 扩展方式 | 继承基类 | 注册新 System / 切换枚举类型 |

## 目录结构

```
Assets/BuffSystemECS/
├── Core/
│   ├── BuffConfigProvider.cs     # Buff 配置加载器（从 Resources 读取 ScriptableObject）
│   ├── BuffEntity.cs             # Entity + EntityManager（数据存储）
│   └── BuffRuntimeData.cs        # 纯数据组件 + BuffEffectType 枚举
├── Systems/
│   ├── IBuffSystem.cs            # ISystem 接口 + BuffSystemBase 抽象基类
│   ├── BuffWorld.cs              # 世界容器（管理 Entity/System 生命周期）
│   ├── BuffDurationSystem.cs     # 时长管理（Refreshable / Independent 计时）
│   ├── BuffStackSystem.cs        # 层管理（Stackable 每层独立计时）
│   └── BuffEffectSystem.cs       # 特效管理（创建/更新位置/销毁特效）
├── Editor/
│   └── BuffHandleECSGenerater.cs # 代码生成器（菜单：Tools/BuffSystemECS/生成BuffHandleECS脚本）
├── Tool/（自动生成）
│   └── BuffHandleECS.cs          # 便捷静态类（由生成器自动创建）
└── BuffSystemECSManager.cs       # MonoBehaviour 桥接层（对外 API）
```

## 三种 Buff 类型

| 类型 | 枚举值 | 行为 |
|------|--------|------|
| **不可叠加** | `Refreshable` | 相同 ID 只有一个实例，`Count = 1`。重复施加仅刷新持续时间。 |
| **数值叠加 + 时间刷新** | `Independent` | 相同 ID 只有一个实例。重复施加层数叠加（`Count++`），持续时间刷新。层数受 `max_count` 上限限制。 |
| **每层独立计时** | `Stackable` | 重复施加时每层独立存在，各有独立持续时间。各层过期后逐层移除。**整个 Buff 共用一个特效实例**。 |

## 核心架构

```
┌─────────────────────────────────────────────────┐
│                   BuffWorld                     │
│  ┌──────────────┐  ┌─────────────────────────┐  │
│  │ EntityManager│  │       Systems[]          │  │
│  │  - 实体增删   │  │  BuffDurationSystem      │  │
│  │  - Buff 增删  │  │  BuffStackSystem         │  │
│  │  - 特效引用   │  │  BuffEffectSystem        │  │
│  └──────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────┘
         ▲                        ▲
         │                        │
┌────────┴────────┐    ┌─────────┴──────────┐
│  BuffEntity     │    │  IBuffSystem       │
│  (struct, 值)   │    │  .Initialize()     │
│  .id            │    │  .Update(dt)       │
│  .is_active     │    └────────────────────┘
└─────────────────┘
         │
         ▼
┌─────────────────┐
│ BuffRuntimeData │
│ (struct, 纯数据) │
│ .buff_id        │
│ .duration       │
│ .count          │
│ .max_count      │
│ .effect_type    │
└─────────────────┘
```

### 数据流

```
BuffSystemECSManager.AddBuff()
  │
  ├─ 实体不存在 → 创建 BuffEntity
  ├─ 检查已存在同 ID Buff → 迭代处理
  ├─ 创建新 Buff → BuffConfigProvider → BuffRuntimeData.FromConfig()
  │
  └─ World.Update() 每帧驱动
       ├─ DurationSystem.Update()   → 减少 duration, 处理过期
       ├─ StackSystem.Update()      → 每层独立计时, 处理层过期
       └─ EffectSystem.Update()     → 更新特效位置
```

## 快速入门

### 1. 创建 Buff 配置

在 Project 窗口中右键 → `Create → BuffData → CreateBuffData`，填写 BuffConfig 参数。

配置文件放在 `Assets/Resources/BuffData/` 目录下。

### 2. 使用 BuffSystemECSManager

在任何 MonoBehaviour 中通过单例访问：

```csharp
using BuffSystemECS;

// 添加 Buff（使用配置默认类型和时长）
BuffSystemECSManager.Instance.AddBuff(target, buffId, BuffEffectType.Refreshable);

// 添加 Buff（指定时长）
BuffSystemECSManager.Instance.AddBuff(target, buffId, BuffEffectType.Stackable, duration: 5f);

// 添加 Buff（完整参数）
BuffSystemECSManager.Instance.AddBuff(target, buffId, BuffEffectType.Independent, 
    duration: 10f, imposer: attacker, effectTarget: headTransform);

// 移除 Buff
BuffSystemECSManager.Instance.RemoveBuff(entity, buffId);

// 移除所有 Buff
BuffSystemECSManager.Instance.RemoveAllBuffs(entity);
```

### 3. 生成 BuffHandleECS

点击菜单 `Tools → BuffSystemECS → 生成BuffHandleECS脚本`，自动生成便捷调用类：

```csharp
// 使用便捷静态类
BuffHandleECS.AddBuff(target, buffId);
BuffHandleECS.AddBuff(target, buffId, duration: 5f);
BuffHandleECS.RemoveBuff(target, buffId);
BuffHandleECS.RemoveAllBuffs(target);
BuffHandleECS.AddBlockBuff(target, buffId);
int count = BuffHandleECS.GetActiveBuffCount(target);
```

### 4. 注册事件回调

```csharp
var manager = BuffSystemECSManager.Instance;

// 层数增加
manager.OnAddBuffLayer += (buff) => Debug.Log($"Buff 层数增加: {buff.buff_name}");

// Buff 移除
manager.OnBuffRemoved += (buff) => Debug.Log($"Buff 移除: {buff.buff_name}");

// 活跃 Buff 数量变化
manager.OnActiveBuffCountChange += (entity) => {
    int count = manager.GetEntityBuffCount(entity);
    Debug.Log($"活跃 Buff 数量: {count}");
};

// 层数减少（仅 Stackable）
manager.OnBuffLayerRemoved += (buff) => Debug.Log($"层数减少: {buff.buff_name}");

// 每层效果触发（仅 Stackable）
manager.OnLayerEffectTrigger += (buff, layerIndex) => {
    // 例如：对目标施加每层的 DoT 伤害
};
```

## 事件系统

| 事件 | 类型 | 触发时机 |
|------|------|---------|
| `OnAddBuffLayer` | `Action<BuffRuntimeData>` | 层数增加时 |
| `OnBuffRemoved` | `Action<BuffRuntimeData>` | Buff 被完全移除时 |
| `OnActiveBuffCountChange` | `Action<BuffEntity>` | 活跃 Buff 数量变化时 |
| `OnBuffLayerRemoved` | `Action<BuffRuntimeData>` | Stackable 单层过期移除时 |
| `OnLayerEffectTrigger` | `Action<BuffRuntimeData, int>` | Stackable 每层间隔触发时 |

## BuffConfig 配置字段

| 字段 | 说明 |
|------|------|
| `buff_id_` | 唯一标识符 |
| `name_` | Buff 名称 |
| `icon_` | Buff 图标 |
| `duration_` | 默认持续时间（秒） |
| `value_` | 效果数值 |
| `stack_count_` | 最大叠加层数 |
| `effect_prefab_` | 特效预制体 |

## 生命周期

```
AddBuff()
  ├── 首次添加（Buff 不存在）
  │     ├── BuffRuntimeData.FromConfig() 创建数据
  │     ├── EntityManager.AddBuffToEntity()
  │     └── EffectSystem.CreateEffect()  创建特效
  └── 重复添加（同 ID Buff 已存在）
        └── IterateBuff()
              ├── Refreshable：刷新 duration
              ├── Independent：count++, 刷新 duration
              └── Stackable：添加新层 (AddLayer)

World.Update() 每帧
  ├── DurationSystem.Update()
  │     └── duration -= deltaTime → 过期则移除 Buff + 释放特效
  ├── StackSystem.Update()
  │     └── 每层 layer_duration -= deltaTime → 单层过期 → 所有层过期则移除 Buff + 释放共用特效
  └── EffectSystem.Update()
        └── 更新所有特效位置跟随挂载点
```

## 扩展指南

### 添加新的 Buff 类型

1. 在 `BuffRuntimeData.cs` 的 `BuffEffectType` 枚举中添加新值
2. 创建新的 System 类继承 `BuffSystemBase`
3. 在 `BuffWorld` 或 `BuffSystemECSManager.InitializeSystems()` 中注册新 System

### 自定义特效行为

继承或修改 `BuffEffectSystem` 的 `CreateEffect` / `ReleaseEffect` 方法，例如接入对象池：

```csharp
public class PooledBuffEffectSystem : BuffEffectSystem
{
    public override void CreateEffect(BuffEntity entity, BuffRuntimeData buff)
    {
        // 从对象池获取特效
        GameObject effect = EffectPool.Instance.Get(buff.effect_prefab);
        // ...
    }
    
    public override void ReleaseEffect(BuffEntity entity, BuffRuntimeData buff)
    {
        // 回收到对象池
        EffectPool.Instance.Release(effect);
    }
}
```

## 需求对应关系

| # | 需求 | BuffEffectType | 处理 System |
|---|------|:---:|------|
| 1 | 数值叠加然后时间刷新 | `Independent` | `BuffDurationSystem` |
| 2 | 每层独立计时，共用一个特效 | `Stackable` | `BuffStackSystem` + `BuffEffectSystem` |
| 3 | 不可叠加层数，仅计时刷新 | `Refreshable` | `BuffDurationSystem` |