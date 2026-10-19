# NewLife.Cube - Web-Framework für schnelle Entwicklung und Verwaltung

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> Diese Übersetzung kann hinter der chinesischen Version zurückliegen.

NewLife.Cube ist ein Web-Framework für schnelle Entwicklung mit Berechtigungen, SSO, OAuth und Entity-CRUD; mit XCode entsteht ein Admin-Backend nahezu ohne Code.

## Installation

```bash
dotnet add package NewLife.Cube
```

## Schnellstart

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## Funktionen

- Codegenerierung und stark typisierte APIs mit weniger Boilerplate
- Authentifizierung, Autorisierung, Verschlüsselung und Sicherheitsschutz
- Nahtlose Integration in das NewLife-Ökosystem (XCode/Cube/Remoting)

## Dokumentation

> 📖 Vollständige Dokumentation (Chinesisch): [Repository-Dokumente](Doc)

## Lizenz

MIT License © 2002-2026 NewLife-Team

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
