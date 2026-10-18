# NewLife.Cube - إطار تطوير ويب وإدارة سريع

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md)
<!-- lang:end -->

> قد تتأخر هذه الترجمة عن النسخة الصينية.

NewLife.Cube إطار تطوير ويب سريع يجمع الصلاحيات وSSO وOAuth وCRUD للكيانات؛ ومع XCode يمكن بناء لوحة إدارة شبه دون كود.

## التثبيت

```bash
dotnet add package NewLife.Cube
```

## البدء السريع

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## الميزات الرئيسية

- توليد الكود وواجهات قوية الأنواع مع تقليل الكود المتكرر
- المصادقة والتفويض والتشفير والحماية الأمنية
- تكامل سلس مع منظومة NewLife (XCode/Cube/Remoting)

## التوثيق

> 📖 التوثيق الكامل (بالصينية): [مستندات المستودع](Doc)

## الترخيص

MIT License © 2002-2026 فريق NewLife

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
