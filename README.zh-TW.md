# NewLife.Cube - Web 快速開發框架與後台管理平台

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> 本翻譯可能落後，以簡體中文版為準。

魔方（NewLife.Cube）是集使用者權限、SSO、OAuth 與實體 CRUD 於一體的 Web 快速開發框架，基於 XCode 一行程式碼即可產生管理後台。

## 安裝

```bash
dotnet add package NewLife.Cube
```

## 快速開始

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## 核心特性

- 程式碼產生與強型別 API，減少樣板程式碼
- 認證、授權、加密與安全防護齊備
- 與 NewLife 生態（XCode/Cube/Remoting）無縫整合

## 文件

> 📖 完整文件（中文）見 [儲存庫文件](Doc)

## 授權

MIT License © 2002-2026 新生命開發團隊

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
