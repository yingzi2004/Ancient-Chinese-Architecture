import os

files = {
    'Assets/Scripts/leicha.cs': [
        ('// λ', '// 控制擂茶交互流程，指定关键的物体位置'),
        ('//1)  Inspector  Slot1Slot2  SpawnPrefabSlot3', '// 1) 在 Inspector 中指定 Slot1、Slot2 和 SpawnPrefab'),
        ('//2) ?? Slot1????? Slot2 ?? SpawnPrefabе?', '// 2) 如果用户先点击 Slot1，然后在超时时间内点击 Slot2 时，会在相机前方生成 SpawnPrefab 或激活场景对象'),
        ('// ??е?? GameManager', '// 将此脚本挂载到场景中的一个空对象上（如 GameManager）'),
        ('[Header("λ Inspector ?壩")]', '[Header("指定对象与位置（在 Inspector 中设置对应对象）")]'),
        ('[Tooltip("?壨Slot1")]', '[Tooltip("先点击的物体（Slot1）")]'),
        ('[Tooltip("壨Slot2")]', '[Tooltip("后点击的物体（Slot2）")]'),
        ('[Tooltip(" Slot1->Slot2 ???е?????? SetActive(true)")]', '[Tooltip("按照 Slot1->Slot2 顺序点击后生成或激活的对象。场景内对象若隐藏则会被 SetActive(true)")]'),
        ('[Tooltip("?? spawnPrefab ???Чλ?")]', '[Tooltip("生成对象距离相机的距离（当 spawnPrefab 为预制件实例化时生效，单位为米）")]'),
        ('[Tooltip("?????Ч")]', '[Tooltip("生成对象相对于相机的局部偏移（当实例化时生效）")]'),
        ('[Tooltip(" Slot1 ?? Slot2 Ч?")]', '[Tooltip("点击 Slot1 后，在这个时间内点击 Slot2 才有效（秒）")]'),
        ('[Tooltip("???в")]', '[Tooltip("使用的射线检测层，默认为所有层")]'),
        ('// ? Slot1 ', '// 超时取消 Slot1 状态'),
        ('// ??? Slot1? slot1 ?', '// 如果当前没有选中 Slot1，检查是否点击了 slot1 或其子对象'),
        ('// 壬', '// 点击了其他物体，忽略'),
        ('// ? Slot1? Slot2 ?', '// 已选中 Slot1，检查是否点击了 Slot2 或其子对象'),
        ('//?', '//触发生成或激活'),
        ('//  Slot2??? Slot1 ', '// 点击了非 Slot2，取消选择（恢复为未选中 Slot1 状态）'),
        ('//  spawnPrefab е???λú???', '// 如果 spawnPrefab 指向场景中的对象（已放好位置和角度），则直接激活它'),
        ('// ?? -> ??', '// 否则视为预制件资源 -> 在相机前方实例化'),
        ('Debug.LogWarning("spawnPrefab δ??");', 'Debug.LogWarning("spawnPrefab 未设置，无法生成或激活");')
    ],
    'Assets/Scripts/leicha-dao.cs': [
        ('// ??????', '// 模拟倒茶时抬起并倾斜物体的动画。将此脚本挂载到需要倒茶的物体上。'),
        ('[Tooltip("??λ??")]', '[Tooltip("抬起的高度（局部坐标，单位为米）")]'),
        ('[Tooltip("???")]', '[Tooltip("抬起过程的时长（秒）")]'),
        ('[Tooltip("???? Vector3.right")]', '[Tooltip("局部空间内的倒茶旋转轴（默认为 Vector3.right）")]'),
        ('[Tooltip("?????")]', '[Tooltip("倾倒时的旋转角度（度）")]'),
        ('[Tooltip("????")]', '[Tooltip("倾倒旋转过程的时长（秒）")]'),
        ('[Tooltip("????")]', '[Tooltip("保持倾倒状态的时间（秒）")]'),
        ('[Tooltip("?????")]', '[Tooltip("返回初始状态的过程时长（秒）")]'),
        ('[Tooltip("??????")]', '[Tooltip("倒茶前想要朝向的目标（可为空）")]'),
        ('[Tooltip("??????0")]', '[Tooltip("转向朝向目标的对齐时长（秒）。为0时不再做朝向对齐的处理")]'),
        ('// ????OnMouseDown', '// 确保有碰撞体以接收点击事件（OnMouseDown）'),
        ('// ?С?????Χ', '// 注意：如果没有渲染器可能大小不合适，这里默认使用对象包围盒大小'),
        ('//  pourTarget???Χ local up', '// 如果设置了 pourTarget，转向目标（仅绕 local up 轴的水平朝向）'),
        ('// ??????', '// 投影到水平面上以计算偏角'),
        ('//?????? Y?', '//目标朝向（世界空间，计算目标朝向的 Y轴旋转转化为角度）'),
        ('//?? up', '//计算抬起目标（由于朝向了目标，沿起初的 local up 轴向上）'),
        ('// ', '// 抬起'),
        ('// б', '// 倾斜倒茶'),
        ('// ', '// 保持'),
        ('// ????', '// 取消倾斜旋转（先回到抬起且对齐的旋转偏角）'),
        ('// ???????', '// 如果之前做了朝向对齐，恢复到原始旋转以避免保留偏角'),
        ('// ?', '// 重置为原本状态以防累积误差')
    ],
    'Assets/Scripts/leicha-mo.cs': [
        ('// ?Χ????', '// 模拟一个围绕指定轴心研磨（打圈）的动画。可用来模拟擂茶研磨。'),
        ('// ????Pestle? Pivot?ɡ', '// 注意：将此脚本挂载到研磨棒（Pestle）上，并指定 Pivot（研磨钵中心）。'),
        ('[Tooltip("????????")]', '[Tooltip("研磨轴心所在的对象位置。如果为空，将使用对象的父级作为轴心。")]'),
        ('[Tooltip("?ε??2 3")]', '[Tooltip("每次点击研磨的圈数（通常2或3圈）")]'),
        ('[Tooltip("?????? = rotations * rotationDuration")]', '[Tooltip("每圈研磨的时间（秒），总时间 = rotations * rotationDuration")]'),
        ('[Tooltip("???? Y ????")]', '[Tooltip("绕哪个轴旋转（世界空间）。通常绕 Y 轴为水平面上打圈")]'),
        ('[Tooltip("Χ pivot ???0 ?? pivot ??")]', '[Tooltip("围绕 pivot 画圆的半径（若为0 则使用当前对象跟 pivot 的距离）")]'),
        ('[Tooltip("???0-1???С?0.5 ??")]', '[Tooltip("缩放半径的比例（0-1），常用于把半径缩小，默认0.5表示画一半大的圈")]'),
        ('[Tooltip("?0 ??")]', '[Tooltip("最大研磨半径（0 表示不限制约束长度）")]'),
        ('[Tooltip("????С??Ч")]', '[Tooltip("是否在研磨时同时使研磨棒微小倾斜（增加视觉逼真感）")]'),
        ('[Tooltip("???? tiltDuringGrinding ? true Ч")]', '[Tooltip("微倾斜的最大角度（度，仅当 tiltDuringGrinding 为 true 时有效）")]'),
        ('[Tooltip("????? leicha ?")]', '[Tooltip("达到指定的研磨点击次数后要显示的擂茶最终模型")]'),
        ('[Tooltip("?Ч??? leicha ?")]', '[Tooltip("达到多少次有效研磨点击后，显示 leicha 模型")]'),
        ('// ? ? ? OnMouseDown ', '// 确保盒体碰撞体以接收 OnMouseDown 事件'),
        ('//λ?', '// 记录原位置和旋转'),
        ('// ? pivot ? >  > ', '// 确定 pivot 使用优先级：字段 > 父级对象 > 自身原点'),
        ('//???', '//计算初始偏移量与半径'),
        ('//淶???', '//规范化偏移到指定半径与旋转方向'),
        ('//???λ', '//旋转轴（世界空间单位向量）'),
        ('// ???', '// 总角度（度）'),
        ('// ??С??', '// 在循环里旋转且同时伴随微小倾斜，以此增强研磨感'),
        ('//??λ? pivotPos? offset', '//计算当前位置：绕 pivotPos 旋转 offset'),
        ('//?Сsin ?', '//围绕某个旋转轴进行微小倾斜（利用 sin 曲线）'),
        ('//? worldAxis ?', '//倾斜旋转轴，使得倾斜总是朝向圆周的切线或法线。若无则取 transform.right'),
        ('//????', '//结束时恢复到初始状态（防浮点误差累积偏离）')
    ]
}

def rewrite_file(path, replacements):
    try:
        with open(path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        
        for old_str, new_str in replacements:
            content = content.replace(old_str, new_str)
            
        with open(path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Fixed {path}")
    except Exception as e:
        print(f"Error for {path}: {e}")

for f, reps in files.items():
    rewrite_file(f, reps)
