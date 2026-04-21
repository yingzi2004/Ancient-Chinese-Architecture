# 玉佩拾取游戏配置指南

## 脚本概述

本游戏包含4个主要脚本：

1. **JadePendant.cs** - 玉佩物品脚本
2. **PlayerPickup.cs** - 玩家拾取系统
3. **GrandpaNPC.cs** - 老爷爷NPC脚本
4. **JadePendantQuestManager.cs** - 任务管理器

---

## Unity编辑器配置步骤

### 第一步：设置玩家对象

1. 在场景中找到你的玩家对象（VR Camera或Player）
2. 添加 **Tag**: `Player` (如果还没有)
3. 添加 `PlayerPickup.cs` 脚本
4. 配置参数：
   - **Pickup Range**: 3（拾取范围）
   - **Pickup Key**: E（拾取键）
   - **Hand Transform**: 手持位置（可选）

---

### 第二步：创建玉佩对象

1. 创建或找到玉佩3D模型
2. 添加 `JadePendant.cs` 脚本
3. 添加 `Collider` (Box Collider 或 Sphere Collider)
4. 确保 `Collider` 的 `Is Trigger` 根据需要设置
5. 配置参数：
   - **Pendant ID**: 唯一标识符
   - **Can Be Picked Up**: ✓ 勾选
   - **Pickup Prompt**: "按 E 键拾取玉佩"
   - **Use Glow Effect**: ✓ 勾选（自动添加发光效果）

---

### 第三步：设置老爷爷NPC

1. 找到老爷爷角色模型
2. 添加 `GrandpaNPC.cs` 脚本
3. 添加 `Collider` (Sphere Collider)，设置为 Trigger
4. 配置参数：
   - **Interaction Range**: 3（交互范围）
   - **Interaction Key**: E
   - **Dialogue No Pendant**: 设置玩家没有玉佩时的对话
   - **Dialogue Has Pendant**: 设置归还玉佩时的对话
   - **Dialogue Quest Complete**: 设置任务完成后的对话
   - **Complete Effect Prefab**: 可选的完成特效

---

### 第四步：创建任务管理器

1. 在场景中创建一个空GameObject，命名为 `JadePendantQuestManager`
2. 添加 `JadePendantQuestManager.cs` 脚本
3. 配置参数：
   - **Quest Name**: "寻找玉佩"
   - **Quest Description**: "老爷爷的玉佩丢了，帮他找回来吧！"
   - **Total Pendants In Scene**: 场景中玉佩的总数
   - **Show Quest UI**: ✓ 勾选（显示任务UI）
   - **Reward Score**: 100（奖励分数）
   - **Load Next Scene On Complete**: 如果需要自动切换场景，勾选此项

---

## 游戏流程

```
开始任务
    ↓
玩家寻找玉佩
    ↓
靠近玉佩 → 按E拾取 → 玉佩进入背包
    ↓
找到老爷爷NPC
    ↓
靠近老爷爷 → 按E交互 → 归还玉佩
    ↓
任务完成 → 获得奖励
```

---

## 可选扩展功能

### 1. 添加音效

在 `JadePendant.cs` 中的 `Pickup()` 方法添加：
```csharp
AudioSource.PlayClipAtPoint(pickupSound, transform.position);
```

### 2. 添加对话系统UI

在 `GrandpaNPC.cs` 中的 `ShowDialogue()` 方法集成现有对话系统：
```csharp
DialogueManager.Instance.ShowDialogue(dialogue);
```

### 3. 多个玉佩

在场景中放置多个玉佩，每个添加 `JadePendant.cs` 脚本
任务管理器会自动统计所有玉佩

### 4. VR交互

如果使用VR设备，可以修改为VR手柄交互：
- 将 `Input.GetKeyDown(KeyCode.E)` 替换为VR控制器输入
- 添加手柄振动反馈

---

## 检查清单

- [ ] 玩家对象有 `Player` Tag
- [ ] 玩家对象添加了 `PlayerPickup.cs`
- [ ] 玉佩添加了 `JadePendant.cs` 和 `Collider`
- [ ] 老爷爷添加了 `GrandpaNPC.cs` 和 `Collider`
- [ ] 场景中有 `JadePendantQuestManager`
- [ ] 所有Tag和Layer设置正确
- [ ] 玉佩和老爷爷的距离设置合理

---

## 脚本文件位置

所有脚本位于：
```
Assets/Scripts/
├── JadePendant.cs
├── PlayerPickup.cs
├── GrandpaNPC.cs
└── JadePendantQuestManager.cs
```

---

## 常见问题

**Q: 玩家无法拾取玉佩？**
A: 检查：
- 玩家对象是否有 `Player` Tag
- 玉佩是否有 `Collider`
- 玩家是否在拾取范围内（3米）

**Q: 老爷爷无法交互？**
A: 检查：
- 老爷爷是否有 `Collider` 且设置为 Trigger
- 玩家是否在交互范围内
- `GrandpaNPC.cs` 是否正确添加

**Q: 任务无法完成？**
A: 检查：
- 场景中是否有 `JadePendantQuestManager`
- 玉佩是否成功拾取（检查Console日志）

---

## 调试信息

所有脚本都包含 `Debug.Log()` 输出，在Unity Console中可以查看：
- 拾取状态
- 交互状态
- 任务进度
- 完成状态
