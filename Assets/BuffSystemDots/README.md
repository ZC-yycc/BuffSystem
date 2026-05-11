# BuffSystemDots — 基于 Unity DOTS (ECS) 的 Buff 系统

## 概述

BuffSystemDots 是一个基于 Unity DOTS (Data-Oriented Technology Stack) 架构的高性能 Buff 系统，利用 Entities、ISystem、BlobAsset、Burst Compiler 等 ECS 核心特性，实现 Buff 的添加、持续、叠加和移除等功能。

## 依赖

- Unity 6+ (6000.0+)
- `com.unity.entities` (1.0+)
- `com.unity.entities.graphics` (1.0+)
- `com.unity.burst` (1.8+)
- `com.unity.collections` (2.0+)
- `com.unity.mathematics` (1.3+)

## 目录结构

```
Assets/BuffSystemDots/
├── BuffDotsBootstrap.cs        # 启动引导组件（挂载到 GameObject）
├── Components/
│   ├── BuffEnums.cs            # 枚举定义（BuffEffectType, BuffTagType）
│   └── BuffComponents.cs       # ECS 组件定义
├── Config/
│   ├── BuffConfigBlob.cs      # BlobAsset 配置（运行时共享）
│   └── BuffConfigData.cs      # ScriptableObject 配置资产
├── Manager/
│   └── BuffDotsManager.cs     # 面向非 ECS 代码的 API 封装
├── Systems/
│   ├── BuffApplySystem.cs     # Buff 施加（处理 BuffAddRequest）
│   ├── BuffDurationSystem.cs  # 持续时间更新（非堆叠类型）
│   ├── BuffStackSystem.cs     # 层管理（Stackable 类型）
│   ├── BuffEffectSystem.cs    # 特效管理
│   ├── BuffRemoveSystem.cs    # Buff 移除（处理 BuffRemoveRequest）
│   └── BuffCleanupSystem.cs   # 过期实体清理
└── README.md
```

## 架构设计

### 数据流

```
BuffDotsManager.AddBuff()
    ↓ 创建 BuffAddRequest 实体
BuffApplySystem
    ↓ 检查配置 → 创建 Buff 实例实体 / 添加层实体
BuffDurationSystem / BuffStackSystem
    ↓ 每帧更新持续时间
BuffCleanupSystem
    ↓ 销毁过期实体
```

### 系统执行顺序

```
BuffApplySystem
    ↓
BuffDurationSystem → BuffStackSystem
    ↓
BuffEffectSystem
    ↓
BuffRemoveSystem
    ↓
BuffCleanupSystem
```

### ECS 实体结构

#### Buff 实例实体
- `BuffInstanceTag` — 标记标签
- `BuffRuntimeData` — 运行时动态数据
- `BuffConfigRef` — 配置引用（指向 BlobAsset）
- `BuffEffectReference` — 特效引用（可选）
- `BuffExpiredTag` — 过期标记（动态添加）

#### Stackable 层实体
- `BuffLayerTag` — 层标记
- `BuffLayerData` — 层数据（父 Buff、层索引、时间等）

#### 请求实体（临时，由系统消费后销毁）
- `BuffAddRequest` — 添加 Buff 请求
- `BuffRemoveRequest` — 移除 Buff 请求

### Buff 类型对比

| 特性       | Refreshable | Independent | Stackable   |
|-----------|-------------|-------------|-------------|
| 叠加       | 刷新时间     | 独立并存     | 多层层叠     |
| 层管理     | 无层         | 无层         | 有子层实体   |
| 效果计算   | 单次         | 各自独立     | 按层累加     |
| 特效       | 共用         | 各自独立     | 共用（父级） |

## 使用方法

### 1. 创建配置资产

右键 `Create → BuffSystemDots → Buff Config` 创建 BuffConfigData：

- **ConfigId**: 唯一标识
- **EffectType**: Refreshable / Independent / Stackable
- **Duration**: 持续时间（秒）
- **MaxCount**: 最大层数限制
- **TriggerInterval**: 触发间隔

### 2. 挂载 Bootstrap

在场景中的 GameObject 上挂载 `BuffDotsBootstrap` 组件，并将 BuffConfigData 资产拖入 `buffConfigs` 数组。

### 3. 代码调用

```csharp
using BuffSystemDots;
using Unity.Entities;

// 初始化（由 Bootstrap 自动完成）
// 添加 Buff
Entity target = /* 目标实体 */;
Entity request = BuffDotsManager.Instance.AddBuff(target, configId: 1001, effectValue: 10f);

// 移除 Buff
BuffDotsManager.Instance.RemoveBuff(buffId: 5);

// 检查 Buff 是否存在
bool hasBuff = BuffDotsManager.Instance.HasBuff(target, 1001);
```

## 扩展指南

### 添加自定义效果组件

继承该架构，在 BuffRuntimeData 上扩展自定义字段，或新增 IComponentData 组件到 Buff 实例实体：

```csharp
// 自定义血量加成组件
public struct BuffHealthBonus : IComponentData
{
    public float BonusValue;
}
```

然后在 BuffApplySystem 或新系统中处理效果。

### 自定义特效

在 BuffEffectSystem 中注册特效预制体，或通过 `BuffEffectReference` 的 `EffectPrefabHash` 字段关联。

## 设计原则

1. **纯 ECS**: 使用 ISystem + BurstCompile 最大化性能
2. **BlobAsset**: 配置数据使用 BlobAsset 共享引用，零拷贝
3. **ECB**: 所有结构性变更使用 EntityCommandBuffer 集中回放
4. **无托管数据**: 组件全部使用 unmanaged 类型确保 Burst 兼容性
5. **分层架构**: Components → System → Manager 三层架构