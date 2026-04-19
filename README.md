# ScreenWordOverlay

<div align="center">

**屏幕逐词翻译覆盖工具** - 框选即译，无干扰阅读

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://github.com/zhuansi0307/ScreenWordOverlay)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

</div>

---

## 📖 项目介绍

### 背景

在阅读英文文档、浏览外文网页时，经常遇到不认识的单词。传统的做法是：
- 复制 → 打开翻译软件 → 粘贴 → 查词 → 关闭
- 操作繁琐，打断阅读思路

**ScreenWordOverlay** 解决这个痛点：**右键框选 → 直接显示翻译**，无需切换窗口，保持阅读流畅性。

### 适用场景

| 场景 | 说明 |
|------|------|
| 阅读英文 PDF/电子书 | 鼠标拖选，翻译即时显示 |
| 浏览外文网页 | 不打开翻译插件，保持原页面布局 |
| 学习编程文档 | 技术术语精准翻译 |
| 游戏/软件界面 | 快速了解未知界面元素 |

---

## ✨ 功能特性

### 核心功能

| 功能 | 描述 |
|------|------|
| 🎯 **区域截取** | 右键拖拽框选屏幕任意矩形区域 |
| 📝 **OCR 识别** | 基于 Tesseract 5.x 的本地文字识别 |
| 🌐 **逐词翻译** | 内置英汉词典 + 有道词典在线补全 |
| 🎨 **原位覆盖** | 翻译结果直接覆盖在原文字位置 |
| 🔄 **滚轮跟随** | 选中区域滚动鼠标时，翻译层随内容同步滚动 |

### 技术亮点

- **本地优先**：内置词典覆盖常用词汇，离线可用
- **智能补全**：本地未命中的单词自动调用在线 API
- **异步设计**：在线翻译完全异步执行，不阻塞 UI
- **全局钩子**：可在任意应用程序中使用
- **托盘运行**：最小化后后台运行，不占用任务栏

---

## 🖥️ 界面预览

### 主窗口
简洁的设置面板，可控制翻译功能开关

```
┌─────────────────────────────────────┐
│  📖 ScreenWordOverlay               │
├─────────────────────────────────────┤
│                                     │
│  ☐ 启用在线翻译                      │
│  ☐ 在线翻译补全                      │
│                                     │
│  📊 内置词典: 12,000+ 词条           │
│  📊 术语表: 自定义映射               │
│                                     │
│  ─────────────────────────────────  │
│  右键拖拽框选区域开始翻译              │
│  按 ESC 关闭翻译覆盖层               │
└─────────────────────────────────────┘
```

### 翻译效果示意

原文区域：
```
The quick brown fox jumps over the lazy dog.
```

框选后显示：
```
The 快速地
quick 迅速的
brown 棕色的
fox 狐狸
jumps 跳跃
over 越过
the 定冠词
lazy 懒惰的
dog 狗
```

---

## 🛠️ 技术栈

| 层级 | 技术选型 |
|------|----------|
| **框架** | .NET 8 + WPF |
| **OCR 引擎** | Tesseract 5.x |
| **在线翻译** | 有道词典建议 API |
| **系统钩子** | Windows API (user32.dll) |
| **词典存储** | JSON 格式，可编辑扩展 |

---

## 📁 项目结构

```
ScreenWordOverlay/
├── Models/
│   ├── OcrWord.cs           # OCR 识别结果模型
│   └── AppSettings.cs       # 应用设置模型
├── Services/
│   ├── OcrService.cs        # OCR 识别服务
│   ├── TranslationService.cs # 翻译服务（含本地+在线）
│   └── MouseHookService.cs  # 全局鼠标钩子
├── Windows/
│   └── OverlayWindow.xaml   # 翻译覆盖窗口
├── Data/
│   ├── dictionary.json      # 内置英汉词典
│   └── glossary.json        # 自定义术语表
├── Resources/
│   └── tessdata/            # Tesseract 语言包
├── MainWindow.xaml          # 主窗口
└── App.xaml                 # 应用程序入口
```

---

## 🚀 快速开始

### 环境要求

- Windows 10/11
- .NET 8 Runtime

### 运行程序

```bash
# 克隆项目
git clone https://github.com/zhuansi0307/ScreenWordOverlay.git

# 进入目录
cd ScreenWordOverlay

# 构建运行
dotnet build -c Release
dotnet run
```

### 使用方法

1. **启动程序** → 主窗口出现后自动最小化至系统托盘
2. **框选区域** → 在任意界面按住右键拖动选择翻译区域
3. **查看翻译** → 选中文本自动识别并显示逐词翻译
4. **跟随滚动** → 在选中区域内滚动鼠标，翻译层同步跟随
5. **关闭翻译** → 按 ESC 键或点击空白区域

---

## ⚙️ 配置说明

| 设置项 | 默认值 | 说明 |
|--------|--------|------|
| 启用在线翻译 | ✅ 开启 | 关闭后仅使用本地词典 |
| 在线翻译补全 | ✅ 开启 | 本地未命中时调用有道 API |

### 自定义词典

编辑 `Data/glossary.json` 添加自定义术语：

```json
{
  "API": "应用程序接口",
  "Debug": "调试",
  "Repository": "仓库"
}
```

---

## ⚠️ 注意事项

1. **OCR 语言包**：首次运行需下载 English 语言包（约 20MB）
2. **在线翻译限制**：有道 API 有频率限制，大量使用建议关闭在线翻译
3. **管理员权限**：全局鼠标钩子可能触发安全提示，请允许
4. **多显示器**：当前版本主要支持主显示器

---

## 📝 更新日志

### v1.0.0 (2024-04-19)
- ✅ 基础 OCR + 翻译功能
- ✅ 内置英汉词典
- ✅ 有道词典在线补全
- ✅ 全局鼠标钩子
- ✅ 滚轮跟随翻译
- ✅ 系统托盘运行

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

---

## 📄 License

MIT License - 详见 [LICENSE](LICENSE) 文件
