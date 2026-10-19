# NewLife.Cube - Web 高速開発・管理画面フレームワーク

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> この翻訳は中国語版より遅れている場合があります。

NewLife.Cube は権限、SSO、OAuth、エンティティ CRUD を統合した Web 高速開発フレームワークです。XCode と組み合わせれば管理画面をほぼコードなしで構築できます。

## インストール

```bash
dotnet add package NewLife.Cube
```

## クイックスタート

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## 主な機能

- コード生成と強く型付けされた API で定型的なコードを削減
- 認証・認可・暗号化・セキュリティ対策を完備
- NewLife エコシステム（XCode/Cube/Remoting）とシームレスに連携

## ドキュメント

> 📖 詳細ドキュメント（中国語）： [リポジトリ内ドキュメント](Doc)

## ライセンス

MIT License © 2002-2026 NewLife チーム

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
