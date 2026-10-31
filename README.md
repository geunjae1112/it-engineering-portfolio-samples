# 김근재 · IT 운영과 시스템 개발 공개 코드 샘플

제조업 IT 프로그램에서 직접 구현한 **실패 분류·상태 추적·재시도·입력 검증**을 읽고 실행해 볼 수 있도록 정리했습니다.

이 저장소는 **실제 프로젝트의 핵심 코드를 발췌·일반화한 공개 샘플**입니다. 전체 운영 프로그램이나 실제 MES·DB 접속 클라이언트가 아닙니다. 기존 비공개 저장소와 이력은 가져오지 않았습니다. 모의 실행 UI·설정 저장·샘플 테스트는 공개 검증을 위해 별도로 구성했습니다.

## 먼저 볼 코드

- [MES 상태·복구 흐름](Source/Core/Mes/UploadStateMachine.cs): 성공·확정 실패·응답 불명확·결과 재확인 대기를 구분합니다.
- [통신 장애와 업무 거절 분리](Source/Core/Mes/CircuitBreaker.cs): 통신 장애 누적 시 호출을 차단하고, 복구 확인은 동시에 한 건만 허용합니다. 시험용 시계를 주입해 대기 없이 경계 조건을 검증합니다.
- [변환 오류의 재시도 조건](Source/Core/Conversion/RetryPolicy.cs): 시간·입력·출력·설정 변경을 구분하고, 사용자 취소는 재시도로 삼키지 않습니다.
- [설정 기반 통화코드 검증](Source/Core/Currency/CurrencyCodeMapper.cs): 정규화·미등록 코드·정규화 이후 중복을 확인합니다. 사내 DB의 고정 순번은 제거하고 공개용 설정을 주입합니다.
- [설정·출력 경로 검증](Source/Core/Configuration/SampleSettings.cs): 절대 경로·상위 폴더 이동을 거부하고 샘플 작업 폴더 내부에서만 결과를 생성합니다.
- [테스트](Source/Tests/Program.cs): 정상·실패·권한·잠금·재시도·동시 복구 시험·잘못된 설정을 실제로 검증합니다.

## 실행

Windows / Visual Studio 2015 / .NET Framework 4.5 기준입니다. 외부 NuGet 패키지·DB·계정이 필요하지 않습니다.

```powershell
./Build.ps1 -Test
./PortfolioSamples.Demo.exe
```

또는 `Source/PortfolioSamples.sln`을 열어 Demo 프로젝트를 시작합니다. `MainForm.cs`에 로직, `MainForm.Designer.cs`에 화면·이벤트 연결을 두었습니다. C# 소스는 UTF-8 BOM으로 저장했습니다.

기본 빌드 스크립트는 .NET Framework 4.5 참조 어셈블리를 사용하며 경고를 오류로 처리합니다. 빌드 실행 파일은 최상위에 생성되고 Git에는 포함되지 않습니다. 중간 파일은 저장소 밖의 고유 빌드 폴더에 생성합니다.

## 공개용으로 바꾼 점

- 개인 PC·서버의 고정 경로를 제거했습니다. 실행 위치·로컬 사용자 데이터 폴더에서 경로를 구성합니다.
- 출력 폴더 이름·장애 임계치·재시험 대기·로그 상한·통화 매핑을 [공개 설정 예제](Settings/settings.example.json)로 분리했습니다.
- 실제 MES DLL·DB 드라이버와 접속 설정, 자격정보는 제외했습니다. 비밀번호나 API 키를 예제 설정에 채울 필요가 없습니다.
- 회사·고객·장비 식별자, 현장 샘플 데이터·로그·빌드 결과, 과거 Git 이력은 포함하지 않습니다.
- 로그는 원문 메시지·예외·경로를 저장하지 않고 정해진 상태 코드만 KST 날짜별로 기록합니다. 크기 상한에 도달하면 추가 기록을 멈추며 자동 삭제하지 않습니다.

실행 설정은 `%LOCALAPPDATA%/PortfolioSamples/Settings/settings.local.json`에 저장됩니다. 파일이 없거나 형식·값이 잘못되면 기본값으로 실행하고 원본 설정을 임의로 덮어쓰지 않습니다. 출력은 같은 샘플 작업 폴더 아래에서만 생성됩니다.

## 확인 범위

2026.10.31 기준 .NET Framework 4.5 참조 빌드, 공개 샘플 테스트 **35개 통과**. 이 수치는 공개 샘플의 새 시험 결과이며, 포트폴리오에 기재한 원래 프로그램의 333건·114개 시험과 합산하지 않습니다.

실제 장비 통신, MES 통합, 운영 DB, 운영 데이터 변환과 현장 성능은 이 저장소의 검증 범위에 포함되지 않습니다. [구조와 설명](Docs/architecture.md), [공개 범위 점검](Docs/public-review.md)을 함께 확인해 주세요.

[포트폴리오](https://kim-geunjae-it-portfolio.geunjae1112.chatgpt.site/)
