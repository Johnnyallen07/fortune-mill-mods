using Mono.Cecil;
using Mono.Cecil.Cil;

const int CurrencyGainMultiplier = 5;
const int UpgradeCostDivisor = 10;
const double BonusMultiplier = 5.0;

var verifyOnly = args.Length > 0 && args[0] == "--verify-only";
var targetArgs = verifyOnly ? args.Skip(1).ToArray() : args;

if (targetArgs.Length == 0)
{
    Console.Error.WriteLine("Usage: FortuneMillPatcher [--verify-only] <FortuneMill.dll> [more dlls...]");
    return 2;
}

var exitCode = 0;
foreach (var path in targetArgs)
{
    try
    {
        if (verifyOnly)
        {
            VerifyAssembly(Path.GetFullPath(path));
        }
        else
        {
            PatchAssembly(Path.GetFullPath(path));
        }
    }
    catch (Exception ex)
    {
        exitCode = 1;
        Console.Error.WriteLine($"{path}: {ex.Message}");
    }
}

    return exitCode;

static void VerifyAssembly(string assemblyPath)
{
    if (!File.Exists(assemblyPath))
    {
        throw new FileNotFoundException("FortuneMill.dll was not found", assemblyPath);
    }

    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);

    using var assembly = AssemblyDefinition.ReadAssembly(
        assemblyPath,
        new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadWrite = false,
            InMemory = true,
        });

    var module = assembly.MainModule;
    var helperType = RequiredType(module, "JohnnyPowerPatchRuntime");
    var playerDataManager = RequiredType(module, "PlayerDataManager");
    var upgradeContainer = RequiredType(module, "UpgradeContainer");
    var attributeModifier = RequiredType(module, "AttributeModifier");

    RequireIntConstant(RequiredMethod(helperType, "ScalePositiveBigInteger", "System.Numerics.BigInteger"), CurrencyGainMultiplier);
    RequireIntConstant(RequiredMethod(helperType, "ScalePositiveInt64", "System.Int64"), CurrencyGainMultiplier);
    RequireIntConstant(RequiredMethod(helperType, "ScaleUpgradeCost", "System.Numerics.BigInteger"), UpgradeCostDivisor);
    RequireDoubleConstant(RequiredMethod(helperType, "ScalePositiveDouble", "System.Double"), BonusMultiplier);

    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddCurrency", "System.Int32", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddSecretCurrency", "System.Int32", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AdjustPachiBallCount", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddTokens", "System.Int64"), "ScalePositiveInt64");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddFuel", "System.Int64", "System.Int64"), "ScalePositiveInt64");
    RequireReturnPatch(RequiredMethod(upgradeContainer, "GetCost", "System.Int64"), "ScaleUpgradeCost");
    RequireReturnPatch(RequiredMethod(attributeModifier, "ComputeVal", "System.Int64"), "ScalePositiveDouble");

    Console.WriteLine($"{assemblyPath}: verified direct patch hooks currency={CurrencyGainMultiplier}x bonus={BonusMultiplier:g}x upgrade-cost=/{UpgradeCostDivisor}");
}

static void PatchAssembly(string assemblyPath)
{
    if (!File.Exists(assemblyPath))
    {
        throw new FileNotFoundException("FortuneMill.dll was not found", assemblyPath);
    }

    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);

    using var assembly = AssemblyDefinition.ReadAssembly(
        assemblyPath,
        new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadWrite = false,
            InMemory = true,
        });

    var module = assembly.MainModule;
    var playerDataManager = RequiredType(module, "PlayerDataManager");
    var upgradeContainer = RequiredType(module, "UpgradeContainer");
    var attributeModifier = RequiredType(module, "AttributeModifier");

    var addCurrency = RequiredMethod(playerDataManager, "AddCurrency", "System.Int32", "System.Numerics.BigInteger");
    var addSecretCurrency = RequiredMethod(playerDataManager, "AddSecretCurrency", "System.Int32", "System.Numerics.BigInteger");
    var adjustPachiBallCount = RequiredMethod(playerDataManager, "AdjustPachiBallCount", "System.Numerics.BigInteger");
    var addTokens = RequiredMethod(playerDataManager, "AddTokens", "System.Int64");
    var addFuel = RequiredMethod(playerDataManager, "AddFuel", "System.Int64", "System.Int64");
    var getCost = RequiredMethod(upgradeContainer, "GetCost", "System.Int64");
    var computeVal = RequiredMethod(attributeModifier, "ComputeVal", "System.Int64");

    var helperSetup = EnsureHelperRuntime(module, addCurrency.Parameters[1].ParameterType);
    var helperMethods = helperSetup.Methods;
    var patches = 0;

    patches += PatchArgument(addCurrency, 1, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(addSecretCurrency, 1, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(adjustPachiBallCount, 0, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(addTokens, 0, helperMethods.ScalePositiveInt64);
    patches += PatchArgument(addFuel, 0, helperMethods.ScalePositiveInt64);
    patches += PatchReturns(getCost, helperMethods.ScaleUpgradeCost);
    patches += PatchReturns(computeVal, helperMethods.ScalePositiveDouble);

    if (patches == 0 && helperSetup.Updates == 0)
    {
        Console.WriteLine($"{assemblyPath}: already patched");
        return;
    }

    var backupPath = assemblyPath + ".bak-johnny-powermod";
    if (!File.Exists(backupPath))
    {
        File.Copy(assemblyPath, backupPath);
    }

    var tempPath = assemblyPath + ".johnny-powermod.tmp";
    try
    {
        assembly.Write(tempPath);
        File.Copy(tempPath, assemblyPath, overwrite: true);
    }
    finally
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }

    Console.WriteLine($"{assemblyPath}: patched {patches} hook(s), updated {helperSetup.Updates} helper(s)");
    Console.WriteLine($"{assemblyPath}: backup {backupPath}");
}

static TypeDefinition RequiredType(ModuleDefinition module, string name)
{
    return module.Types.FirstOrDefault(t => t.Name == name)
        ?? throw new InvalidOperationException($"Required type not found: {name}");
}

static MethodDefinition RequiredMethod(TypeDefinition type, string name, params string[] parameterFullNames)
{
    return type.Methods.FirstOrDefault(m =>
            m.Name == name
            && m.Parameters.Count == parameterFullNames.Length
            && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameterFullNames))
        ?? throw new InvalidOperationException($"Required method not found: {type.Name}.{name}({string.Join(", ", parameterFullNames)})");
}

static HelperSetup EnsureHelperRuntime(ModuleDefinition module, TypeReference bigIntegerType)
{
    var updates = 0;
    var helperType = module.Types.FirstOrDefault(t => t.Name == "JohnnyPowerPatchRuntime");
    if (helperType is null)
    {
        helperType = new TypeDefinition(
            string.Empty,
            "JohnnyPowerPatchRuntime",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            module.TypeSystem.Object);
        module.Types.Add(helperType);
    }

    var scalePositiveBigInteger = EnsureMethod(
        helperType,
        "ScalePositiveBigInteger",
        bigIntegerType,
        new[] { bigIntegerType },
        method => HasIntConstant(method, CurrencyGainMultiplier),
        il => EmitScalePositiveBigInteger(il, module, bigIntegerType, CurrencyGainMultiplier),
        ref updates);

    var scaleUpgradeCost = EnsureMethod(
        helperType,
        "ScaleUpgradeCost",
        bigIntegerType,
        new[] { bigIntegerType },
        method => HasIntConstant(method, UpgradeCostDivisor),
        il => EmitScalePositiveBigInteger(il, module, bigIntegerType, UpgradeCostDivisor, divide: true),
        ref updates);

    var scalePositiveInt64 = EnsureMethod(
        helperType,
        "ScalePositiveInt64",
        module.TypeSystem.Int64,
        new[] { module.TypeSystem.Int64 },
        method => HasIntConstant(method, CurrencyGainMultiplier),
        EmitScalePositiveInt64,
        ref updates);

    var scalePositiveDouble = EnsureMethod(
        helperType,
        "ScalePositiveDouble",
        module.TypeSystem.Double,
        new[] { module.TypeSystem.Double },
        method => HasDoubleConstant(method, BonusMultiplier),
        EmitScalePositiveDouble,
        ref updates);

    return new HelperSetup(
        new HelperMethods(scalePositiveBigInteger, scalePositiveInt64, scaleUpgradeCost, scalePositiveDouble),
        updates);
}

static MethodDefinition EnsureMethod(
    TypeDefinition type,
    string name,
    TypeReference returnType,
    IReadOnlyList<TypeReference> parameterTypes,
    Func<MethodDefinition, bool> hasExpectedBody,
    Action<ILProcessor> emitBody,
    ref int updates)
{
    var existing = type.Methods.FirstOrDefault(m => m.Name == name);
    if (existing is not null)
    {
        if (!hasExpectedBody(existing))
        {
            RewriteMethodBody(existing, emitBody);
            updates++;
        }

        return existing;
    }

    var method = new MethodDefinition(
        name,
        MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
        returnType);

    for (var i = 0; i < parameterTypes.Count; i++)
    {
        method.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, parameterTypes[i]));
    }

    method.Body.InitLocals = false;
    method.Body.MaxStackSize = 3;
    type.Methods.Add(method);
    emitBody(method.Body.GetILProcessor());
    updates++;
    return method;
}

static void RewriteMethodBody(MethodDefinition method, Action<ILProcessor> emitBody)
{
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();
    method.Body.Instructions.Clear();
    method.Body.InitLocals = false;
    method.Body.MaxStackSize = 3;
    emitBody(method.Body.GetILProcessor());
}

static void EmitScalePositiveBigInteger(
    ILProcessor il,
    ModuleDefinition module,
    TypeReference bigIntegerType,
    int multiplier,
    bool divide = false)
{
    var scale = Instruction.Create(OpCodes.Ldarg_0);
    var getSign = new MethodReference("get_Sign", module.TypeSystem.Int32, bigIntegerType) { HasThis = true };
    var implicitInt32 = new MethodReference("op_Implicit", bigIntegerType, bigIntegerType)
    {
        HasThis = false,
        Parameters = { new ParameterDefinition(module.TypeSystem.Int32) },
    };
    var operation = new MethodReference(divide ? "op_Division" : "op_Multiply", bigIntegerType, bigIntegerType)
    {
        HasThis = false,
        Parameters =
        {
            new ParameterDefinition(bigIntegerType),
            new ParameterDefinition(bigIntegerType),
        },
    };

    il.Emit(OpCodes.Ldarga_S, il.Body.Method.Parameters[0]);
    il.Emit(OpCodes.Call, getSign);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Bgt_S, scale);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(scale);
    il.Emit(OpCodes.Ldc_I4, multiplier);
    il.Emit(OpCodes.Call, implicitInt32);
    il.Emit(OpCodes.Call, operation);
    il.Emit(OpCodes.Ret);
}

static void EmitScalePositiveInt64(ILProcessor il)
{
    var scale = Instruction.Create(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Conv_I8);
    il.Emit(OpCodes.Bgt_S, scale);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(scale);
    il.Emit(OpCodes.Ldc_I4, CurrencyGainMultiplier);
    il.Emit(OpCodes.Conv_I8);
    il.Emit(OpCodes.Mul);
    il.Emit(OpCodes.Ret);
}

static void EmitScalePositiveDouble(ILProcessor il)
{
    var scale = Instruction.Create(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 0.0);
    il.Emit(OpCodes.Bgt_S, scale);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(scale);
    il.Emit(OpCodes.Ldc_R8, BonusMultiplier);
    il.Emit(OpCodes.Mul);
    il.Emit(OpCodes.Ret);
}

static int PatchArgument(MethodDefinition method, int argumentIndex, MethodReference helper)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var il = method.Body.GetILProcessor();
    var first = method.Body.Instructions[0];
    var parameter = method.Parameters[argumentIndex];

    il.InsertBefore(first, Instruction.Create(OpCodes.Ldarg_S, parameter));
    il.InsertBefore(first, Instruction.Create(OpCodes.Call, helper));
    il.InsertBefore(first, Instruction.Create(OpCodes.Starg_S, parameter));
    return 1;
}

static int PatchReturns(MethodDefinition method, MethodReference helper)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var patched = 0;
    var il = method.Body.GetILProcessor();
    foreach (var ret in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
    {
        il.InsertBefore(ret, Instruction.Create(OpCodes.Call, helper));
        patched++;
    }

    return patched;
}

static bool CallsHelper(MethodDefinition method, string helperName)
{
    return method.Body.Instructions.Any(i =>
        i.Operand is MethodReference called
        && called.Name == helperName
        && called.DeclaringType.Name == "JohnnyPowerPatchRuntime");
}

static bool HasIntConstant(MethodDefinition method, int value)
{
    return method.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_I4 && i.Operand is int actual && actual == value)
        || method.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_I4_5 && value == 5)
        || method.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_I4_S && i.Operand is sbyte actual && actual == value);
}

static bool HasDoubleConstant(MethodDefinition method, double value)
{
    return method.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldc_R8 && i.Operand is double actual && Math.Abs(actual - value) < 0.000001);
}

static void RequireIntConstant(MethodDefinition method, int value)
{
    if (!HasIntConstant(method, value))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} does not contain expected integer constant {value}");
    }
}

static void RequireDoubleConstant(MethodDefinition method, double value)
{
    if (!HasDoubleConstant(method, value))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} does not contain expected double constant {value}");
    }
}

static void RequireArgumentPatch(MethodDefinition method, string helperName)
{
    if (!CallsHelper(method, helperName))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing {helperName}");
    }
}

static void RequireReturnPatch(MethodDefinition method, string helperName)
{
    var retCount = method.Body.Instructions.Count(i => i.OpCode == OpCodes.Ret);
    var helperCount = method.Body.Instructions.Count(i =>
        i.Operand is MethodReference called
        && called.Name == helperName
        && called.DeclaringType.Name == "JohnnyPowerPatchRuntime");

    if (helperCount < retCount)
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} has {helperCount}/{retCount} {helperName} return patches");
    }
}

readonly record struct HelperMethods(
    MethodReference ScalePositiveBigInteger,
    MethodReference ScalePositiveInt64,
    MethodReference ScaleUpgradeCost,
    MethodReference ScalePositiveDouble);

readonly record struct HelperSetup(HelperMethods Methods, int Updates);
