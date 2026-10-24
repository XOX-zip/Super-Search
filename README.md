# SS — Super Search

一款优先搜索 / 侦查工具。抓取 Bing 搜索结果，本地打分排序后，可选用 AI 精筛，最终输出高相关度结果。

---

## 产品介绍

SS（Super Search）是一款桌面搜索增强工具，核心流程：

```
用户输入关键词
    │
    ▼
Bing 网页抓取（解析搜索结果页）
    │
    ▼
本地评分排序（词频、位置、标题权重等）
    │
    ▼
可选 AI 精筛（调用兼容 OpenAI 的接口二次过滤）
    │
    ▼
结果展示（标题 / 链接 / 摘要 / 评分）
```

### 特性

- **本地优先**：不依赖任何服务端，Bing 抓取 + 本地评分即可独立工作
- **AI 增强**：可接入任意兼容 OpenAI 协议的大模型接口，对候选结果做语义精筛
- **降级机制**：AI 接口不可用时自动回退到纯本地模式，不影响使用
- **筛选控制**：支持结果数限制、站点限定（site:）、AI 开关、详细模式
- **深色界面**：WPF 自绘深色主题，自绘标题栏、结果卡片、详情面板

---

## 启动方式

### 方式一：直接运行（已编译）

```
bin\Release\net11.0-windows\SS(Super Search).exe
```

### 方式二：从源码构建

环境要求：.NET 11 SDK（Windows）

```powershell
# 在项目根目录执行
dotnet build -c Release

# 运行
dotnet run -c Release
```

### 首次启动

首次运行会弹出设置窗口，配置 AI 接口（可选）：

| 字段 | 说明 |
|---|---|
| AI 接口地址 | 兼容 OpenAI 协议的 API 端点，如 `https://api.example.com/v1` |
| API Key | 接口密钥 |
| 模型名称 | 如 `gpt-4o-mini`、`agnes-3.0-flash` 等 |
| 默认结果数 | 每次搜索返回的结果条数（默认 10） |

不配置 AI 也可使用（纯本地模式），点击"跳过"即可。

---

## 依赖

### 运行时依赖

| 依赖 | 版本 | 说明 |
|---|---|---|
| .NET Runtime | 11.0 (windows) | WPF 桌面应用运行时 |

### 开发依赖

| 依赖 | 版本 | 说明 |
|---|---|---|
| .NET SDK | 11.0 | 构建与编译 |

### 内部使用的 .NET 类库（无外部 NuGet 包）

| 命名空间 | 用途 |
|---|---|
| `System.Net.Http` | HTTP 请求（Bing 抓取、AI 接口调用） |
| `System.Text.Json` | JSON 序列化 / 反序列化（配置文件、AI 响应） |
| `System.Text.RegularExpressions` | HTML 解析、结果提取 |
| `System.Threading` | 异步搜索、取消令牌 |
| `System.Windows` | WPF UI 框架 |

---

## 使用说明

### 主界面

- **搜索框**：输入关键词，回车或点击搜索按钮
- **结果数**：限制返回结果数量
- **站点限定**：仅搜索指定站点（等效于 `site:example.com`）
- **AI 开关**：是否启用 AI 精筛
- **详细模式**：显示评分等额外信息

### 操作快捷键

| 快捷键 | 功能 |
|---|---|
| `Enter` | 执行搜索 |
| `Esc` | 取消当前搜索 |
| `Ctrl + F` | 聚焦搜索框 |
| 双击结果 | 在浏览器中打开链接 |

### 结果卡片

- 显示标题、域名、摘要、本地评分
- 右侧详情面板展示选中项的完整信息
- 支持"打开链接"与"复制链接"

---

## 配置文件

配置存储位置（按顺序查找）：

1. `%APPDATA%\SS\config.json`
2. `%LOCALAPPDATA%\SS\config.json`
3. `<程序目录>\SS\config.json`
4. `%TEMP%\SS\config.json`

配置示例：

```json
{
  "Configured": true,
  "AiEndpoint": "https://api.example.com/v1",
  "AiKey": "sk-xxxxxxxxxxxxxxxx",
  "AiModel": "gpt-4o-mini",
  "DefaultLimit": 10
}
```

---

## 项目结构

```
SS(Super Search)/
├── App.xaml / App.xaml.cs        # 应用入口与启动流程
├── Theme.xaml                     # 深色主题资源（色板、样式）
├── SS(Super Search).csproj        # 项目文件
├── src/
│   ├── MainWindow.xaml(.cs)       # 主窗口
│   ├── SettingsWindow.xaml(.cs)   # 设置窗口（AI 配置）
│   ├── SearchService.cs           # 搜索编排（抓取→评分→AI→输出）
│   ├── BingSearch.cs              # Bing 抓取
│   ├── Ranker.cs                  # 本地评分
│   ├── AiClient.cs                # AI 接口客户端
│   ├── Config.cs                  # 配置加载 / 保存
│   ├── SearchOptions.cs           # 搜索参数
│   ├── SearchResult.cs            # 结果数据结构
│   └── ResultRow.cs               # 结果列表项 ViewModel
└── LICENSE                        # CC BY-NC-SA 4.0 协议
```

---

## 协议

本作品采用 [知识共享 署名-非商业性使用-相同方式共享 4.0 国际许可协议](https://creativecommons.org/licenses/by-nc-sa/4.0/) 进行许可。

**简要说明：**

- ✅ **署名（BY）**：必须标明原作者，保留版权声明
- ✅ **非商业（NC）**：禁止用于商业盈利目的
- ✅ **相同方式共享（SA）**：衍生作品必须以相同协议发布
- ✅ 允许修改、二创、分发

完整协议文本见 [LICENSE](./LICENSE) 文件。

---

## 免责声明

本软件按"原样"提供，不附带任何明示或暗示的担保。使用本软件产生的一切后果由使用者自行承担。
