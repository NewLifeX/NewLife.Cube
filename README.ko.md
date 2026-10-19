# NewLife.Cube - 웹 고속 개발 및 관리자 프레임워크

![GitHub License](https://img.shields.io/github/license/NewLifeX/NewLife.Cube?logo=github)
![NuGet Downloads](https://img.shields.io/nuget/dt/NewLife.Cube?logo=nuget)
![NuGet](https://img.shields.io/nuget/v/NewLife.Cube?logo=nuget)
![NuGet prerelease](https://img.shields.io/nuget/vpre/NewLife.Cube?label=dev%20nuget&logo=nuget)
[![test](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml/badge.svg?branch=master)](https://github.com/NewLifeX/NewLife.Cube/actions/workflows/test.yml)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20arm64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/NewLife.Cube?logo=github)](https://github.com/NewLifeX/NewLife.Cube/graphs/contributors)

<!-- lang:start -->
🌐 [中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [العربية](README.ar.md)
<!-- lang:end -->

> 이 번역은 중국어 버전보다 늦을 수 있습니다.

NewLife.Cube는 권한, SSO, OAuth, 엔터티 CRUD를 통합한 웹 고속 개발 프레임워크입니다. XCode와 함께라면 관리자 백엔드를 거의 코드 없이 만들 수 있습니다.

## 설치

```bash
dotnet add package NewLife.Cube
```

## 빠른 시작

```csharp
services.AddCube();
app.UseCube();

public class UserController : EntityController<User> { }
```

## 주요 기능

- 코드 생성과 강력한 형식 API로 반복 코드 감소
- 인증, 권한 부여, 암호화 및 보안 보호
- NewLife 생태계(XCode/Cube/Remoting)와 원활한 통합

## 문서

> 📖 전체 문서(중국어): [저장소 문서](Doc)

## 라이선스

MIT License © 2002-2026 NewLife 팀

[GitHub](https://github.com/NewLifeX/NewLife.Cube) | [Website](https://newlifex.com)
