# SetIPAddress

命令行小工具：更改指定网卡的 IPv4 配置（静态地址 / 网关 / 掩码 / DNS / 切回 DHCP），重点面向 **Windows Server 2008 R2** 等老系统。

- 单文件、自包含发布（约 8.6 MB），目标机无需安装 .NET 运行时
- 目标框架 `net6.0-windows`，`win-x64`
- 已嵌入 `requireAdministrator` 清单，双击/启动即请求提权
- 不依赖 WMI、PowerShell 模块，全部通过 `netsh interface ipv4` + `IP Helper` 数据回读完成

## 兼容性

| 项            | 要求                                                                                                       |
| ------------- | ---------------------------------------------------------------------------------------------------------- |
| 系统          | Windows Server 2008 R2 **SP1**（x64）；Vista 及以上同代码路径                                              |
| Universal CRT | KB2999226 / KB3118401，或安装 VC++ 2015-2022 x64 运行库；缺失时报 `api-ms-win-crt-runtime-l1-1-0.dll` 缺失 |
| KB2533623     | .NET Core 3.0+ 在 Win7 内核上的加载前提，通常已随累积更新存在                                              |
| 权限          | 管理员（清单已声明，UAC 关闭时需用管理员账户运行）                                                         |
| 临时目录      | 单文件首次启动会解压到 `%TEMP%`，需可写；受限环境可用 `DOTNET_BUNDLE_EXTRACT_BASE_DIR` 指定目录            |

发布产物 PE 头 `Subsystem 6.0`，导入仅 `KERNEL32 / ADVAPI32 / OLE32 / OLEAUT32 / USER32 / SHELL32 + api-ms-win-crt-*`。

## 构建

```powershell
dotnet publish -c Release -v q
```

产物：`bin\Release\net6.0-windows\win-x64\publish\SetIPAddress.exe`

`RuntimeIdentifier`、`SelfContained`、`PublishSingleFile`、压缩与清单均已写入 `SetIPAddress.csproj`。

体积优化主要靠 `PublishTrimmed=true` + `TrimMode=link`：本程序不用反射、不用动态代码、无序列化，全量裁剪安全（构建无 IL2xxx 裁剪告警）。实测体积：

| 配置                       | 体积       |
| -------------------------- | ---------- |
| 仅自包含单文件 + 压缩      | 31.4 MB    |
| + `TrimMode=link`          | **8.6 MB** |
| + 关闭单文件压缩（对比用） | 9.95 MB    |

进一步压缩的空间：`UseSystemResourceKeys` 实测无收益（8.65 MB 持平）；减少 BCL 面（把 `NetworkInterface` 换成自写 `GetAdaptersAddresses` P/Invoke）最多再省 1 MB 左右，不值得。若目标机已安装 .NET 6 运行时，可改框架依赖发布，产物约几百 KB，但那就不再是"免安装"。

> 说明：**net6 无法做 NativeAOT**——NuGet 上 `Microsoft.DotNet.ILCompiler` 最早版本为 `7.0.0-preview.2`，`Microsoft.NETCore.App.Runtime.NativeAOT.*` 从 `9.0.0-preview.1` 才有，SDK 也以 `NETSDK1207` 拒绝 net6 AOT。因此本项目采用自包含单文件，并移除了原本仅为 AOT 服务的 YY-Thunks 依赖。

## 用法

```
SetIPAddress list [--all]
SetIPAddress show  <选择器>
SetIPAddress set   <选择器> --ip <IPv4[/前缀]> [--mask <掩码|前缀>] [--gw <IPv4|none>]
                            [--dns <IPv4[,IPv4...]|none>] [--gw-metric <0-9999>] [--force]
SetIPAddress dhcp  <选择器> [--force]
```

### 选择器（可组合，多条件为“与”）

| 参数                | 说明                                          |
| ------------------- | --------------------------------------------- |
| `--name <连接名>`   | 例如 `"本地连接"`，需与 `list` 中“连接名”一致 |
| `--desc <关键字>`   | 网卡描述的子串，忽略大小写                    |
| `--mac <MAC>`       | `AA-BB-CC-DD-EE-FF` 或 `AA:BB:CC:DD:EE:FF`    |
| `--index <ifIndex>` | IPv4 接口索引（`list` 的“索引”列）            |

不指定选择器时，仅当**恰好有一个**“已连接”网卡才自动选中，否则要求显式指定并列出候选。

### set 参数

| 参数          | 说明                                                                             |
| ------------- | -------------------------------------------------------------------------------- |
| `--ip`        | 必填。`192.168.1.10` 或 `192.168.1.10/24`                                        |
| `--mask`      | 可选。点分掩码或前缀长度；优先级：`--ip` 前缀 > `--mask` > 网卡当前前缀 > `/24`  |
| `--gw`        | 省略 = **保持原网关不变**；填 IPv4 = 改为该网关；`none` = 删除网关               |
| `--dns`       | 省略 = 不修改；逗号分隔最多 4 个 = 覆盖设置（`register=primary`）；`none` = 清空 |
| `--gw-metric` | 网关跃点数 0-9999，仅与网关一起下发                                              |
| `--force`     | 目标网卡当前持有网关时必需，避免误断远程连接                                     |

### 示例

```powershell
SetIPAddress.exe list
SetIPAddress.exe show --name "本地连接"

# 改地址 + 网关 + DNS（网卡原持有网关，需 --force）
SetIPAddress.exe set --name "本地连接" --ip 192.168.1.10/24 --gw 192.168.1.1 --dns 114.114.114.114,8.8.8.8 --force

# 只改地址，网关、DNS 均保持原样
SetIPAddress.exe set --name "本地连接" --ip 192.168.1.20 --mask 255.255.255.0

# 改地址并删除网关
SetIPAddress.exe set --name "本地连接" --ip 192.168.1.20/24 --gw none

# 切回 DHCP
SetIPAddress.exe dhcp --name "本地连接"
```

## 执行流程

```mermaid
flowchart LR
  A["解析参数"] --> B["按选择器定位网卡"]
  B --> C{"已是目标配置?"}
  C -- "是" --> Z["退出 0"]
  C -- "否" --> D{"持有网关且无 --force?"}
  D -- "是" --> R["拒绝执行, 退出 6"]
  D -- "否" --> E["netsh set address"]
  E --> F["删除残留旧地址"]
  F --> G["DNS 下发"]
  G --> H["回读校验"]
  H -- "一致" --> Z
  H -- "不一致" --> I["按快照回滚, 退出 5"]
```

## 退出码

| 码  | 含义                             |
| --- | -------------------------------- |
| 0   | 成功（或已是目标配置）           |
| 1   | 参数错误 / 未知命令              |
| 2   | 网卡定位失败（未命中或多命中）   |
| 3   | 权限不足（非管理员）             |
| 4   | netsh 执行失败（已尝试回滚）     |
| 5   | 回读校验失败（已回滚）           |
| 6   | 风险闸口拒绝执行（需 `--force`） |
| 9   | 未预期错误（打印完整异常链）     |

## 行为与限制

- **成功判定只认回读**：netsh 退出码 0 不作为成功依据；重新枚举网卡比对地址/前缀/网关/DNS（DNS 按集合比对）。
- **旧地址会被清理**：`netsh set address` 只替换主地址、不删除附加地址，工具会显式 `delete address`，保证网卡最终只有目标地址。
- **回滚**：校验不匹配时按改动前快照恢复（含多地址、网关、DNS 或 DHCP）；若校验过程本身抛异常，则保留已提交配置不盲目回滚，提示人工核对。
- **网关保持**：`netsh set address` 省略 `gateway=` 会清空网关（实测），故“保持”是取当前网关回填；多网关只保留第一个，原跃点数不保留（可显式 `--gw-metric`）。
- **DHCP 标志可能滞后**：媒体断开的网卡会把静态地址存为“备用配置”，此时 `DHCP` 列仍显示“是”，地址本身已生效；工具会打印提示。
- **输出编码**：stdout 是控制台时走 `WriteConsoleW`（UTF-16），中英混排在 936 代码页下不乱码；重定向到文件时为 UTF-8，而 netsh 自身输出仍为系统 OEM 代码页。
- 仅管理 IPv4 单地址、主网关与 DNS；不做路由表、IPv6、网卡启用/禁用操作。

## 源码结构

| 文件           | 职责                                                                |
| -------------- | ------------------------------------------------------------------- |
| `Program.cs`   | CLI 解析、命令分派、回读校验、回滚、表格输出                        |
| `Adapters.cs`  | 网卡枚举/快照/选择器匹配（`System.Net.NetworkInformation`）         |
| `Netsh.cs`     | `netsh interface ipv4` 调用封装（不重定向子进程输出，避免解码问题） |
| `ConsoleIo.cs` | 控制台写出（`WriteConsoleW`）、管理员检测（`TokenElevation`）       |
| `app.manifest` | `requireAdministrator` + 支持的 OS 列表                             |
