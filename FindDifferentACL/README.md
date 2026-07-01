# FindDifferentACL

一个 Windows 平台命令行工具，用于**递归查找子文件夹中所有与父目录文件权限（ACL）不一致的目录**。

## 使用场景

当目录结构中的某些子目录被手动修改了权限（ACL），权限与父目录不一致时，该工具可以帮助快速定位这些"异常"目录。

## 功能

- 递归遍历指定根目录下的所有子目录
- 获取每个子目录的 ACL，与**其直接父目录**的 ACL 进行比较
- 对完全继承父目录权限（无显式权限规则）的子目录自动跳过，避免误报
- 支持以 SDDL 字符串形式输出详细差异
- 通过退出码区分扫描结果：
  - `0`：所有子目录权限与父目录一致
  - `1`：存在权限不一致的目录
  - `2`：参数错误或无法访问根目录

## 使用方式

```bash
# 扫描当前目录
FindDifferentACL .

# 扫描指定目录
FindDifferentACL C:\SomeFolder

# 带详细 SDDL 差异输出
FindDifferentACL C:\SomeFolder --details
```

或通过 dotnet run 运行：

```bash
dotnet run -- .
dotnet run -- C:\SomeFolder
dotnet run -- C:\SomeFolder --details
```

## 输出示例

**无差异：**

```
正在扫描: E:\SomeScripts\FindDifferentACL

扫描完成: 共扫描 16 个子目录，0 个权限不一致，8 个跳过（完全继承父目录权限）。
```

**发现差异：**

```
正在扫描: C:\TestFolder

扫描完成: 共扫描 5 个子目录，2 个权限不一致，3 个跳过（完全继承父目录权限）。

以下目录权限与其父目录不一致:
  SubDir2  (父目录: .)
  SubDir2\Nested  (父目录: SubDir2)

提示: 使用 --details 或 -d 参数可查看详细的 SDDL 差异。
```

**详细模式：**

```
正在扫描: C:\TestFolder

不一致: SubDir2
  子目录  SDDL: D:PAI(D;;WD;;;WD)(A;;FA;;;SY)(A;;FA;;;BA)(A;;0x1200a9;;;BU)(A;;0x1200a9;;;AU)
  父目录  SDDL: D:PAI(A;;FA;;;SY)(A;;FA;;;BA)(A;;0x1200a9;;;BU)(A;;0x1200a9;;;AU)

扫描完成: 共扫描 5 个子目录，1 个权限不一致，3 个跳过（完全继承父目录权限）。
```

## 原理

1. 使用 `DirectorySecurity.GetAccessRules()` 获取子目录的显式（非继承）权限规则。若没有显式规则，说明完全继承父目录，直接跳过。
2. 对有显式规则的子目录，使用 `GetSecurityDescriptorSddlForm()` 获取 SDDL 字符串，与父目录的 SDDL 进行字符串比较。
3. 若 SDDL 不同，则判定为权限不一致。

## 构建

```bash
dotnet build
dotnet publish -c Release
```
