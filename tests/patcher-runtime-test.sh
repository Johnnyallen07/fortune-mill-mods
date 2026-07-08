#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$(cd -- "${SCRIPT_DIR}/.." && pwd)"
GAME_ROOT="$(cd -- "${MOD_DIR}/.." && pwd)"
SOURCE_DIR="${GAME_ROOT}/Fortune Mill.app/Contents/Resources/data_FortuneMill_macos_arm64"
SOURCE_DLL="${SOURCE_DIR}/FortuneMill.dll"
BACKUP_DLL="${SOURCE_DLL}.bak-johnny-powermod"
TMP_DIR="$(mktemp -d)"

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

if [[ -f "$BACKUP_DLL" ]]; then
  cp "$BACKUP_DLL" "${TMP_DIR}/FortuneMill.dll"
else
  cp "$SOURCE_DLL" "${TMP_DIR}/FortuneMill.dll"
fi

dotnet run --project "${MOD_DIR}/tools/FortuneMillPatcher/FortuneMillPatcher.csproj" -- "${TMP_DIR}/FortuneMill.dll" >/tmp/fortune-mill-patcher-runtime-patch.log

cat >"${TMP_DIR}/RuntimeCheck.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
EOF

cat >"${TMP_DIR}/Program.cs" <<'EOF'
using System.Reflection;
using System.Runtime.Loader;

var assemblyPath = args[0];
var dependencyDir = args[1];

AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
{
    var localCandidate = Path.Combine(Path.GetDirectoryName(assemblyPath)!, assemblyName.Name + ".dll");
    if (File.Exists(localCandidate))
    {
        return context.LoadFromAssemblyPath(localCandidate);
    }

    var gameCandidate = Path.Combine(dependencyDir, assemblyName.Name + ".dll");
    return File.Exists(gameCandidate) ? context.LoadFromAssemblyPath(gameCandidate) : null;
};

var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
var helper = assembly.GetType("JohnnyPowerPatchRuntime", throwOnError: true)!;
var scalePositiveDouble = helper.GetMethod("ScalePositiveDouble", BindingFlags.Public | BindingFlags.Static)!;
var clampTrialMultiplier = helper.GetMethod("ClampTrialMultiplier", BindingFlags.Public | BindingFlags.Static)!;
var clampDartBoardAttribute = helper.GetMethod("ClampDartBoardAttribute", BindingFlags.Public | BindingFlags.Static)!;
var isZenithApplying = helper.GetField("IsZenithApplying", BindingFlags.NonPublic | BindingFlags.Static)!;

var general = (double)scalePositiveDouble.Invoke(null, new object[] { 2.0 })!;
if (general != 2.0)
{
    throw new InvalidOperationException($"expected general bonus unchanged at 2, got {general}");
}

isZenithApplying.SetValue(null, true);
var zenith = (double)scalePositiveDouble.Invoke(null, new object[] { 2.0 })!;
if (zenith != 20.0)
{
    throw new InvalidOperationException($"expected zenith bonus 20, got {zenith}");
}

var trial = (double)clampTrialMultiplier.Invoke(null, new object[] { 16_834_001_183_299_768.0 })!;
if (trial != 10_000.0)
{
    throw new InvalidOperationException($"expected trial multiplier cap 10000, got {trial}");
}

var bullseyeSize = (double)clampDartBoardAttribute.Invoke(null, new object[] { 24.0, 6 })!;
if (bullseyeSize != 1.0)
{
    throw new InvalidOperationException($"expected bullseye size cap 1, got {bullseyeSize}");
}

var bullseyeCount = (double)clampDartBoardAttribute.Invoke(null, new object[] { 418.0, 7 })!;
if (bullseyeCount != 20.0)
{
    throw new InvalidOperationException($"expected bullseye count cap 20, got {bullseyeCount}");
}

var otherAttribute = (double)clampDartBoardAttribute.Invoke(null, new object[] { 418.0, 8 })!;
if (otherAttribute != 418.0)
{
    throw new InvalidOperationException($"expected unrelated attribute unchanged, got {otherAttribute}");
}

Console.WriteLine("PASS runtime helper ScalePositiveDouble executes");
EOF

dotnet run --project "${TMP_DIR}/RuntimeCheck.csproj" -- "${TMP_DIR}/FortuneMill.dll" "$SOURCE_DIR"
