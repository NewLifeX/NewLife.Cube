# NewLife.Cube - Framework web de développement rapide et d'administration

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

> Cette traduction peut être en retard sur la version chinoise.

NewLife.Cube est un framework web de développement rapide intégrant permissions, SSO, OAuth et CRUD d'entités ; avec XCode, le back-office se crée presque sans code.

## Installation

```bash
dotnet add package NewLife.Cube
```

## Démarrage rapide

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## Fonctionnalités

- Génération de code et API fortement typées, moins de code répétitif
- Authentification, autorisation, chiffrement et protections de sécurité
- Intégration transparente à l'écosystème NewLife (XCode/Cube/Remoting)

## Documentation

> 📖 Documentation complète (chinois) : [documents du dépôt](Doc)

## Licence

MIT License © 2002-2026 Équipe NewLife

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
