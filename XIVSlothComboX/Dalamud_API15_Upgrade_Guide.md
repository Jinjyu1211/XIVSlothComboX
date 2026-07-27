# Dalamud API 14 → API 15 升级指南

## 概述

本文档详细记录了 Dalamud 从 API 14 升级到 API 15 的主要变化，帮助插件开发者了解需要修改的内容。

**配套游戏版本**: FF14 Patch 7.5
**.NET 版本**: .NET 10.0
**Windows 最低版本**: Windows 10

---

## 1. 项目配置变化

### 1.1 .csproj 文件更新

#### 新增 Sdk 风格

```xml
<!-- 旧版 -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="DalamudPackager" Version="14.0.0" />
  </ItemGroup>
</Project>

<!-- 新版 -->
<Project Sdk="Dalamud.NET.Sdk/14.0.2">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
</Project>
```

#### 关键变化

| 配置项 | API 14 | API 15 |
|--------|--------|--------|
| TargetFramework | net8.0-windows | net10.0-windows |
| ImplicitUsings | disable | enable |
| Nullable | disable | enable |
| LangVersion | 默认 | latest |

### 1.2 依赖版本

```xml
<!-- API 15 必需的依赖版本 -->
<PackageReference Include="DalamudPackager" Version="14.0.2" />
```

---

## 2. 框架和事件签名变化

### 2.1 IClientState 事件

#### TerritoryChanged 事件

```csharp
// API 14
private void OnTerritoryChanged(ushort territoryId)
{
    // ...
}
Service.ClientState.TerritoryChanged += OnTerritoryChanged;

// API 15
private void OnTerritoryChanged(uint territoryId)
{
    // ...
}
Service.ClientState.TerritoryChanged += OnTerritoryChanged;
```

**变化**: 参数类型从 `ushort` 改为 `uint`

### 2.2 IDutyState 事件

#### DutyRecommenced 事件 - 已移除

```csharp
// API 14 - 此事件已不存在
Service.DutyState.DutyRecommenced += OnDutyRecommenced;

// API 15 - 事件已移除，需要使用其他方式检测
// 可以通过 TerritoryChanged 或其他事件组合实现类似功能
```

#### 事件参数传递方式

| 旧版 | 新版 |
|------|------|
| 直接参数传递 | 通过 `IDutyStateEventArgs` 接口 |
| `ushort` 参数类型 | `RowRef<T>` 泛型引用 |

```csharp
// API 15 新接口
public interface IDutyStateEventArgs
{
    RowRef<DutyInfo> DutyInfo { get; }
    RowRef<ContentFinderConfig> ContentFinderConfig { get; }
}
```

### 2.3 登录/登出事件

```csharp
// API 14
Service.ClientState.Login += OnLogin;

// API 15 - 参数现在使用 RowRef 包装
Service.ClientState.Login += OnLogin;

private void OnLogin(ClientState.LoginEventArgs args)
{
    // args.Character 现在是 RowRef<Character> 类型
    var character = args.Character;
}
```

---

## 3. ImRaii 系统重构

这是 API 15 中**破坏性最大**的变化。

### 3.1 核心架构变化

| 旧版 | 新版 |
|------|------|
| `IEndObject` 接口 | 改为 `ref struct`，消除装箱开销 |
| `IEndObjects` 集合 | **已移除** |
| 布尔失败控制参数 | **已移除** |
| 非确定性资源管理 | 确定性析构（基于 `ref struct`） |

### 3.2 新增类型

```csharp
// API 15 新增的 ref struct 类型
ImRaii.Color(...)      // 颜色管理
ImRaii.Style(...)     // 样式管理
ImRaii.Disabled(...)  // 禁用状态管理
ImRaii.Group(...)      // 组管理（不再有 bool 转换）
ImRaii.Tooltip(...)    // 工具提示（不再有 bool 转换）
```

### 3.3 重要变化

#### Group 和 Tooltip 不再有 bool 转换

```csharp
// API 14
using (var group = ImRaii.Group())
{
    // ...
}

// API 15 - Group 不再有 bool 转换，不会失败
using (ImRaii.Group())
{
    // ...
}
```

#### Color 和 Style 不再需要布尔参数

```csharp
// API 14
using (var color = ImRaii.Color(color, true))  // 第二个参数控制是否失败
using (var style = ImRaii.Style(StyleVar.Alpha, alpha, true))

// API 15 - 布尔参数已移除
using (ImRaii.Color(color))
using (ImRaii.Style(StyleVar.Alpha, alpha))
```

### 3.4 实际升级示例

#### 示例：修复 ImRaii.Disabled

```csharp
// API 14
bool isDisabled = DisabledJobsPVE.Any(x => x == jobId);
using (var disabled = ImRaii.Disabled(isDisabled, isDisabled))
{
    ImGui.Text($"{header} {(disabled ? "(因更新而禁用)" : "")}");
}

// API 15
bool isDisabled = DisabledJobsPVE.Any(x => x == jobId);
using (var disabled = ImRaii.Disabled(isDisabled))
{
    // 注意：disabled 不再是 bool，需要用单独的变量
    ImGui.Text($"{header} {(isDisabled ? "(因更新而禁用)" : "")}");
}
```

#### 示例：条件设置样式

```csharp
// API 14
using (var style = ImRaii.Style(StyleVar.Alpha, ShouldDisable ? 0.5f : 1.0f, ShouldDisable))

// API 15 - 需要提前计算值
float alpha = ShouldDisable ? 0.5f : 1.0f;
using (ImRaii.Style(StyleVar.Alpha, alpha))
{
    // ...
}
```

---

## 4. IChatGui 消息传递重构

### 4.1 参数传递方式变化

| 旧版 | 新版 |
|------|------|
| 直接传递 `XivChatType` | 通过 `IChatMessage` 接口 |
| 单独的参数 | 打包为对象属性 |

### 4.2 新增接口

```csharp
public interface IChatMessage
{
    XivChatType Type { get; }
    uint SenderId { get; }
    string SenderName { get; }
    SeString Message { get; }

    // API 15 新增属性
    ChatSourceKind SourceKind { get; }
    ChatTargetKind TargetKind { get; }
}
```

### 4.3 升级示例

```csharp
// API 14
Service.ChatGui.Print(message);

// API 15 - Print 方法签名未变，但内部使用 IChatMessage
Service.ChatGui.Print(message);

// 新增的发送消息方式
Service.ChatGui.Print(new ChatMessage
{
    Type = XivChatType.Urgent,
    Message = message,
    SourceKind = ChatSourceKind.LinkShell1
});
```

---

## 5. FFXIVClientStructs 类型变化

### 5.1 ValueType 枚举重命名

| 旧版 | 新版 |
|------|------|
| `FFXIVClientStructs.FFXIV.Component.GUI.ValueType` | `AtkValueType` |

```csharp
// API 14
values[0] = new()
{
    Type = ValueType.Int,
    Int = 2
};

// API 15
values[0] = new()
{
    Type = AtkValueType.AtkValueType_Int,
    Int = 2
};
// 或使用数值
values[0] = new()
{
    Type = (AtkValueType)1,
    Int = 2
};
```

### 5.2 常见 AtkValueType 数值映射

| 枚举成员 | 数值 | 说明 |
|---------|------|------|
| `AtkValueType_None` | 0 | 无 |
| `AtkValueType_Int` | 1 | 整数 |
| `AtkValueType_Ptr` | 2 | 指针 |
| `AtkValueType_Float` | 3 | 浮点数 |
| `AtkValueType_String` | 4 | 字符串 |
| `AtkValueType_Type` | 5 | 类型 |

---

## 6. Festival 系统重构

### 6.1 合并的变化

| API 14 | API 15 |
|--------|--------|
| `ActiveFestivals[]` | 合并为单个 `IReadOnlyList<FestivalEntry>` |
| `ActiveFestivalPhases[]` | 合并到 `FestivalEntry` 结构 |

### 6.2 新数据结构

```csharp
// API 15 新结构
public readonly struct FestivalEntry
{
    public uint FestivalId { get; }
    public uint Phase { get; }
    public DateTimeOffset StartTime { get; }
    public DateTimeOffset EndTime { get; }
    public TimeSpan RemainingTime { get; }
}

// API 15 新属性
public IReadOnlyList<FestivalEntry> ActiveFestivals { get; }
```

### 6.3 升级示例

```csharp
// API 14
var festivals = Service.ClientState.ActiveFestivals;
var phases = Service.ClientState.ActiveFestivalPhases;

// API 15
var festivals = Service.ClientState.ActiveFestivals;
foreach (var festival in festivals)
{
    Console.WriteLine($"Festival: {festival.FestivalId}, Phase: {festival.Phase}");
}
```

---

## 7. 新增异步插件支持

### 7.1 IAsyncDalamudPlugin 接口

```csharp
public interface IAsyncDalamudPlugin : IDalamudPlugin
{
    // 异步加载，可在线程池执行
    Task LoadAsync();

    // 异步卸载
    ValueTask UnloadAsync();
}
```

### 7.2 使用示例

```csharp
public class MyPlugin : IAsyncDalamudPlugin
{
    public async Task LoadAsync()
    {
        // 异步初始化，可以执行耗时操作而不阻塞 UI
        await Task.Run(() => InitializeServices());
        await LoadResourcesAsync();
    }

    public async ValueTask UnloadAsync()
    {
        // 异步清理资源
        await SaveStateAsync();
        CleanupResources();
    }
}
```

### 7.3 向后兼容性

- 实现 `IAsyncDalamudPlugin` 的插件仍然可以使用旧版 `IDalamudPlugin` 接口
- Dalamud 会自动处理接口兼容性问题

---

## 8. 性能改进

### 8.1 装箱消除

| 旧版 | 新版 |
|------|------|
| 接口装箱（`IEndObject`） | `ref struct` 栈分配 |
| GC 压力高 | GC 压力显著降低 |

### 8.2 确定性资源管理

```csharp
// 旧版 - 非确定性，等待 GC
using (var color = ImRaii.Color(oldColor))
{
    // 可能在不确定的时间释放
}

// 新版 - 确定性，基于 ref struct 的 Dispose
using (ImRaii.Color(newColor))
{
    // 离开 using 块立即释放
}
```

### 8.3 类型安全增强

- `readonly struct` 替代可变结构体
- 增强的空引用检查
- 更好的泛型约束

---

## 9. 升级检查清单

### 必需修改

- [ ] 更新 `.csproj` 中的 `TargetFramework` 为 `net10.0-windows`
- [ ] 更新 `ImplicitUsings` 为 `enable`
- [ ] 更新 `Nullable` 为 `enable`
- [ ] 更新 `DalamudPackager` 版本为 `14.0.2`
- [ ] 修改所有 `TerritoryChanged` 事件参数从 `ushort` 改为 `uint`
- [ ] **移除** `DutyRecommenced` 事件订阅
- [ ] **重写**所有 `ImRaii` 的 `using` 语句
- [ ] 检查并更新所有 `ValueType` 引用为 `AtkValueType`

### 可能需要修改

- [ ] 更新 `IChatGui` 消息处理逻辑（如有使用）
- [ ] 更新 Festival 相关代码（如有使用）
- [ ] 检查枚举成员名称是否变化（见下节）

### 枚举成员名称变化

以下枚举成员在升级过程中发现名称可能有变化，建议使用数值比较：

| 枚举类型 | 成员 | 建议处理 |
|---------|------|---------|
| `Song` (BRD) | `Wanderer`, `Mage`, `Army`, `None` | 使用 `(int)gauge.Song == 1` 等 |
| `Nadi` (MNK) | `None`, `Solar`, `Lunar` | 使用 `(int)gauge.Nadi == 0` 等 |
| `ObjectKind` | `Player` | 使用 `(int)objectKind == 1` |
| `BattleNpcSubKind` | `Enemy` | 使用 `(int)npcKind == 2` |

---

## 10. 常见问题

### Q1: 为什么 ImRaii 变化这么大？

A: 为了解决长期以来的性能问题。`IEndObject` 接口需要装箱操作，会产生 GC 压力。改为 `ref struct` 后，资源管理更高效，且完全确定性。

### Q2: DutyRecommenced 事件被移除后如何检测副本重置？

A: 可以通过组合使用以下事件：
- `TerritoryChanged` - 区域变化时检测
- `ClientState.Login` - 重新登录时检测
- 定时检查当前副本状态

### Q3: 为什么枚举成员名称找不到？

A: FFXIVClientStructs 在游戏更新时可能会重命名枚举成员。建议使用数值比较代替名称比较，以确保兼容性。

### Q4: API 15 是否向后兼容？

A: 大部分变化是向后兼容的，但 `ImRaii` 的变化是破坏性改变，需要修改代码。如果插件不涉及这些 API，基本可以无缝升级。

---

## 参考资源

- [Dalamud 官方文档](https://goatcorp.github.io/)
- [FFXIVClientStructs GitHub](https://github.com/goatcorp/FFXIVClientStructs)
- [Dalamud Discord](https://discord.gg/goatcorp)

---

**最后更新**: 2026-04-30
**适用版本**: Dalamud API 15 / FF14 7.5
