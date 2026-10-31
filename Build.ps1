param([switch]$Test)
$ErrorActionPreference = 'Stop'
$sampleRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
$frameworkReferences = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.5'
if (-not (Test-Path -LiteralPath $compiler) -or -not (Test-Path -LiteralPath $frameworkReferences)) {
    throw 'Windows .NET Framework C# 컴파일러와 .NET Framework 4.5 Targeting Pack이 필요합니다.'
}
$compileDirectory = Join-Path ([IO.Path]::GetTempPath()) ('PortfolioSamplesBuild-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $compileDirectory | Out-Null
$coreSources = @(Get-ChildItem -LiteralPath (Join-Path $sampleRoot 'Source/Core') -Recurse -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$demoSources = @(Get-ChildItem -LiteralPath (Join-Path $sampleRoot 'Source/Demo') -Filter '*.cs' | Select-Object -ExpandProperty FullName)
$referenceNames = @('mscorlib.dll','System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Xml.dll')
$referenceArguments = @($referenceNames | ForEach-Object { '/reference:' + (Join-Path $frameworkReferences $_) })
$common = @('/nologo','/noconfig','/nostdlib+','/warn:4','/warnaserror+','/optimize+','/codepage:65001') + $referenceArguments
$coreAssembly = Join-Path $compileDirectory 'PortfolioSamples.Core.dll'
& $compiler @common '/target:library' ('/out:' + $coreAssembly) @coreSources
if ($LASTEXITCODE -ne 0) { throw '핵심 라이브러리 빌드 실패' }
$demoExecutable = Join-Path $compileDirectory 'PortfolioSamples.Demo.exe'
& $compiler @common '/target:winexe' ('/out:' + $demoExecutable) ('/reference:' + $coreAssembly) ('/reference:' + (Join-Path $frameworkReferences 'System.Windows.Forms.dll')) ('/reference:' + (Join-Path $frameworkReferences 'System.Drawing.dll')) @demoSources
if ($LASTEXITCODE -ne 0) { throw 'WinForms 샘플 빌드 실패' }
Copy-Item -LiteralPath $demoExecutable -Destination (Join-Path $sampleRoot 'PortfolioSamples.Demo.exe') -Force
Copy-Item -LiteralPath $coreAssembly -Destination (Join-Path $sampleRoot 'PortfolioSamples.Core.dll') -Force
Write-Output '빌드 성공: .NET Framework 4.5 참조 어셈블리 사용, 경고 없음'
if ($Test) {
    $testExecutable = Join-Path $compileDirectory 'PortfolioSamples.Tests.exe'
    & $compiler @common '/target:exe' ('/out:' + $testExecutable) ('/reference:' + $coreAssembly) (Join-Path $sampleRoot 'Source/Tests/Program.cs')
    if ($LASTEXITCODE -ne 0) { throw '테스트 빌드 실패' }
    & $testExecutable
    if ($LASTEXITCODE -ne 0) { throw '샘플 테스트 실패' }
}
# 빌드 중간 파일은 저장소 밖의 고유 작업 폴더에서 생성합니다. 기존 파일·설정을 자동 삭제하지 않습니다.
