using Mono.Cecil;
using Mono.Cecil.Cil;

const int CurrencyGainMultiplier = 5;
const double ZenithBonusMultiplier = 10.0;
const double SecretShopBonusMultiplier = 5.0;
const double UpgradeCostGrowthBase = 1.25;
const double MaxTrialMultiplier = 10_000.0;
const double MaxDartBullseyeSize = 165.39594025728948;
const double MaxDartBullseyeCount = 20.0;

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

    var isZenithApplying = RequiredField(helperType, "IsZenithApplying");
    var isSecretShopApplying = RequiredField(helperType, "IsSecretShopApplying");
    RequireIntConstant(RequiredMethod(helperType, "ScalePositiveBigInteger", "System.Numerics.BigInteger"), CurrencyGainMultiplier);
    RequireIntConstant(RequiredMethod(helperType, "ScalePositiveInt64", "System.Int64"), CurrencyGainMultiplier);
    RequireIdentityMethod(RequiredMethod(helperType, "ScaleUpgradeCost", "System.Numerics.BigInteger"));
    RequireScalePositiveDoubleHelper(RequiredMethod(helperType, "ScalePositiveDouble", "System.Double"), isZenithApplying, isSecretShopApplying);
    RequireDoubleConstant(RequiredMethod(helperType, "ScaleUpgradeCostGrowth", "System.Double"), UpgradeCostGrowthBase);
    RequireDoubleConstant(RequiredMethod(helperType, "ClampTrialMultiplier", "System.Double"), MaxTrialMultiplier);
    RequireDoubleConstant(RequiredMethod(helperType, "ClampDartBoardAttribute", "System.Double", "System.Int32"), MaxDartBullseyeSize);
    _ = RequiredMethod(helperType, "ApplyZenithAttributes", "System.Int64", "AttributeModifier[]", "System.Double[]", "System.Double[]");
    _ = RequiredMethod(helperType, "ApplySecretShopAttributes", "UpgradeContainer", "System.Int64", "System.Double[]", "System.Double[]");

    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddCurrency", "System.Int32", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddSecretCurrency", "System.Int32", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AdjustPachiBallCount", "System.Numerics.BigInteger"), "ScalePositiveBigInteger");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddTokens", "System.Int64"), "ScalePositiveInt64");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "AddFuel", "System.Int64", "System.Int64"), "ScalePositiveInt64");
    RequireArgumentPatch(RequiredMethod(upgradeContainer, "GetCost", "System.Int64"), "ScaleUpgradeCostGrowth");
    RequireReturnPatch(RequiredMethod(attributeModifier, "ComputeVal", "System.Int64"), "ScalePositiveDouble");
    RequireCallPatch(RequiredMethod(playerDataManager, "RecalculateAttributes"), "ApplyZenithAttributes");
    RequireCallPatch(RequiredMethod(playerDataManager, "RecalculateAttributes"), "ApplySecretShopAttributes");
    RequireReturnPatch(RequiredMethod(playerDataManager, "GetTrialMulti"), "ClampTrialMultiplier");
    RequireArgumentPatch(RequiredMethod(playerDataManager, "MaybeUpdateTrialMulti", "System.Double"), "ClampTrialMultiplier");
    RequireReturnPatch(RequiredMethod(playerDataManager, "GetAttribute", "AttributeIndex"), "ClampDartBoardAttribute");

    Console.WriteLine($"{assemblyPath}: verified direct patch hooks currency={CurrencyGainMultiplier}x general-bonus=1x zenith-bonus={ZenithBonusMultiplier:g}x secret-shop-bonus={SecretShopBonusMultiplier:g}x upgrade-growth={UpgradeCostGrowthBase:g}x trial-multi<={MaxTrialMultiplier:g}x dart-bullseye-size<={MaxDartBullseyeSize:g}x dart-bullseye-count<={MaxDartBullseyeCount:g}");
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
    var recalculateAttributes = RequiredMethod(playerDataManager, "RecalculateAttributes");
    var getTrialMulti = RequiredMethod(playerDataManager, "GetTrialMulti");
    var maybeUpdateTrialMulti = RequiredMethod(playerDataManager, "MaybeUpdateTrialMulti", "System.Double");
    var getAttribute = RequiredMethod(playerDataManager, "GetAttribute", "AttributeIndex");
    var applyContainerAttributes = RequiredMethod(upgradeContainer, "applyAttributes", "System.Int64", "System.Double[]", "System.Double[]");
    var applyAllAttributes = RequiredMethod(attributeModifier, "ApplyAllAttributes", "System.Int64", "AttributeModifier[]", "System.Double[]", "System.Double[]");

    var helperSetup = EnsureHelperRuntime(module, addCurrency.Parameters[1].ParameterType, applyAllAttributes, applyContainerAttributes);
    var helperMethods = helperSetup.Methods;
    var patches = 0;

    patches += PatchArgument(addCurrency, 1, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(addSecretCurrency, 1, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(adjustPachiBallCount, 0, helperMethods.ScalePositiveBigInteger);
    patches += PatchArgument(addTokens, 0, helperMethods.ScalePositiveInt64);
    patches += PatchArgument(addFuel, 0, helperMethods.ScalePositiveInt64);
    patches += PatchUpgradeGrowth(getCost, helperMethods.ScaleUpgradeCostGrowth);
    patches += PatchReturns(computeVal, helperMethods.ScalePositiveDouble);
    patches += PatchSecretShopAttributeApplication(recalculateAttributes, helperMethods.ApplySecretShopAttributes);
    patches += PatchZenithAttributeApplication(recalculateAttributes, helperMethods.ApplyZenithAttributes);
    patches += PatchReturns(getTrialMulti, helperMethods.ClampTrialMultiplier);
    patches += PatchArgument(maybeUpdateTrialMulti, 0, helperMethods.ClampTrialMultiplier);
    patches += PatchReturnsWithArgument(getAttribute, helperMethods.ClampDartBoardAttribute, 0);

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

static FieldDefinition RequiredField(TypeDefinition type, string name)
{
    return type.Fields.FirstOrDefault(f => f.Name == name)
        ?? throw new InvalidOperationException($"Required field not found: {type.Name}.{name}");
}

static HelperSetup EnsureHelperRuntime(ModuleDefinition module, TypeReference bigIntegerType, MethodReference applyAllAttributes, MethodReference applyContainerAttributes)
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

    var isZenithApplying = EnsureField(helperType, "IsZenithApplying", module.TypeSystem.Boolean);
    var isSecretShopApplying = EnsureField(helperType, "IsSecretShopApplying", module.TypeSystem.Boolean);

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
        IsIdentityMethod,
        EmitIdentity,
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
        method => IsScalePositiveDoubleHelperValid(method, isZenithApplying, isSecretShopApplying),
        il => EmitScalePositiveDouble(il, isZenithApplying, isSecretShopApplying),
        ref updates);

    var scaleUpgradeCostGrowth = EnsureMethod(
        helperType,
        "ScaleUpgradeCostGrowth",
        module.TypeSystem.Double,
        new[] { module.TypeSystem.Double },
        method => HasDoubleConstant(method, UpgradeCostGrowthBase),
        EmitScaleUpgradeCostGrowth,
        ref updates);

    var clampTrialMultiplier = EnsureMethod(
        helperType,
        "ClampTrialMultiplier",
        module.TypeSystem.Double,
        new[] { module.TypeSystem.Double },
        method => HasDoubleConstant(method, MaxTrialMultiplier),
        EmitClampTrialMultiplier,
        ref updates);

    var clampDartBoardAttribute = EnsureMethod(
        helperType,
        "ClampDartBoardAttribute",
        module.TypeSystem.Double,
        new[] { module.TypeSystem.Double, module.TypeSystem.Int32 },
        method => HasDoubleConstant(method, MaxDartBullseyeCount),
        EmitClampDartBoardAttribute,
        ref updates);

    var applyZenithAttributes = EnsureMethod(
        helperType,
        "ApplyZenithAttributes",
        module.TypeSystem.Void,
        new TypeReference[] { module.TypeSystem.Int64, applyAllAttributes.Parameters[1].ParameterType, new ArrayType(module.TypeSystem.Double), new ArrayType(module.TypeSystem.Double) },
        method => CallsField(method, isZenithApplying) && CallsMethod(method, applyAllAttributes.Name, applyAllAttributes.DeclaringType.Name),
        il => EmitApplyZenithAttributes(il, isZenithApplying, applyAllAttributes),
        ref updates);

    var applySecretShopAttributes = EnsureMethod(
        helperType,
        "ApplySecretShopAttributes",
        module.TypeSystem.Void,
        new TypeReference[] { applyContainerAttributes.DeclaringType, module.TypeSystem.Int64, new ArrayType(module.TypeSystem.Double), new ArrayType(module.TypeSystem.Double) },
        method => CallsField(method, isSecretShopApplying) && CallsMethod(method, applyContainerAttributes.Name, applyContainerAttributes.DeclaringType.Name),
        il => EmitApplySecretShopAttributes(il, isSecretShopApplying, applyContainerAttributes),
        ref updates);

    return new HelperSetup(
        new HelperMethods(scalePositiveBigInteger, scalePositiveInt64, scaleUpgradeCost, scalePositiveDouble, scaleUpgradeCostGrowth, clampTrialMultiplier, clampDartBoardAttribute, applyZenithAttributes, applySecretShopAttributes),
        updates);
}

static FieldDefinition EnsureField(TypeDefinition type, string name, TypeReference fieldType)
{
    var existing = type.Fields.FirstOrDefault(f => f.Name == name);
    if (existing is not null)
    {
        return existing;
    }

    var field = new FieldDefinition(name, FieldAttributes.Private | FieldAttributes.Static, fieldType);
    type.Fields.Add(field);
    return field;
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

static void EmitIdentity(ILProcessor il)
{
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
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

static void EmitScalePositiveDouble(ILProcessor il, FieldReference isZenithApplying, FieldReference isSecretShopApplying)
{
    var positive = Instruction.Create(OpCodes.Ldsfld, isSecretShopApplying);
    var checkZenith = Instruction.Create(OpCodes.Ldsfld, isZenithApplying);
    var secretScale = Instruction.Create(OpCodes.Ldarg_0);
    var zenithScale = Instruction.Create(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 0.0);
    il.Emit(OpCodes.Bgt_S, positive);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(positive);
    il.Emit(OpCodes.Brtrue_S, secretScale);
    il.Append(checkZenith);
    il.Emit(OpCodes.Brtrue_S, zenithScale);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(secretScale);
    il.Emit(OpCodes.Ldc_R8, SecretShopBonusMultiplier);
    il.Emit(OpCodes.Mul);
    il.Emit(OpCodes.Ret);
    il.Append(zenithScale);
    il.Emit(OpCodes.Ldc_R8, ZenithBonusMultiplier);
    il.Emit(OpCodes.Mul);
    il.Emit(OpCodes.Ret);
}

static void EmitScaleUpgradeCostGrowth(ILProcessor il)
{
    var maybeCap = Instruction.Create(OpCodes.Ldarg_0);
    var cap = Instruction.Create(OpCodes.Ldc_R8, UpgradeCostGrowthBase);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 1.0);
    il.Emit(OpCodes.Bgt_S, maybeCap);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(maybeCap);
    il.Emit(OpCodes.Ldc_R8, UpgradeCostGrowthBase);
    il.Emit(OpCodes.Bgt_S, cap);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(cap);
    il.Emit(OpCodes.Ret);
}

static void EmitClampTrialMultiplier(ILProcessor il)
{
    var maybeCap = Instruction.Create(OpCodes.Ldarg_0);
    var cap = Instruction.Create(OpCodes.Ldc_R8, MaxTrialMultiplier);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 1.0);
    il.Emit(OpCodes.Bge_S, maybeCap);
    il.Emit(OpCodes.Ldc_R8, 1.0);
    il.Emit(OpCodes.Ret);
    il.Append(maybeCap);
    il.Emit(OpCodes.Ldc_R8, MaxTrialMultiplier);
    il.Emit(OpCodes.Bgt_S, cap);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(cap);
    il.Emit(OpCodes.Ret);
}

static void EmitClampDartBoardAttribute(ILProcessor il)
{
    var checkCount = Instruction.Create(OpCodes.Ldarg_1);
    var sizeMaybeMax = Instruction.Create(OpCodes.Ldarg_0);
    var sizeCap = Instruction.Create(OpCodes.Ldc_R8, MaxDartBullseyeSize);
    var unchanged = Instruction.Create(OpCodes.Ldarg_0);
    var countMaybeMax = Instruction.Create(OpCodes.Ldarg_0);
    var countCap = Instruction.Create(OpCodes.Ldc_R8, MaxDartBullseyeCount);

    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Ldc_I4_6);
    il.Emit(OpCodes.Bne_Un_S, checkCount);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 0.0);
    il.Emit(OpCodes.Bge_S, sizeMaybeMax);
    il.Emit(OpCodes.Ldc_R8, 0.0);
    il.Emit(OpCodes.Ret);
    il.Append(sizeMaybeMax);
    il.Emit(OpCodes.Ldc_R8, MaxDartBullseyeSize);
    il.Emit(OpCodes.Bgt_S, sizeCap);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(sizeCap);
    il.Emit(OpCodes.Ret);

    il.Append(checkCount);
    il.Emit(OpCodes.Ldc_I4_7);
    il.Emit(OpCodes.Bne_Un_S, unchanged);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldc_R8, 1.0);
    il.Emit(OpCodes.Bge_S, countMaybeMax);
    il.Emit(OpCodes.Ldc_R8, 1.0);
    il.Emit(OpCodes.Ret);
    il.Append(countMaybeMax);
    il.Emit(OpCodes.Ldc_R8, MaxDartBullseyeCount);
    il.Emit(OpCodes.Bgt_S, countCap);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ret);
    il.Append(countCap);
    il.Emit(OpCodes.Ret);

    il.Append(unchanged);
    il.Emit(OpCodes.Ret);
}

static void EmitApplyZenithAttributes(ILProcessor il, FieldReference isZenithApplying, MethodReference applyAllAttributes)
{
    il.Emit(OpCodes.Ldc_I4_1);
    il.Emit(OpCodes.Stsfld, isZenithApplying);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Ldarg_2);
    il.Emit(OpCodes.Ldarg_3);
    il.Emit(OpCodes.Call, applyAllAttributes);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Stsfld, isZenithApplying);
    il.Emit(OpCodes.Ret);
}

static void EmitApplySecretShopAttributes(ILProcessor il, FieldReference isSecretShopApplying, MethodReference applyContainerAttributes)
{
    il.Emit(OpCodes.Ldc_I4_1);
    il.Emit(OpCodes.Stsfld, isSecretShopApplying);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Ldarg_2);
    il.Emit(OpCodes.Ldarg_3);
    il.Emit(OpCodes.Callvirt, applyContainerAttributes);
    il.Emit(OpCodes.Ldc_I4_0);
    il.Emit(OpCodes.Stsfld, isSecretShopApplying);
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

static int PatchReturnsWithArgument(MethodDefinition method, MethodReference helper, int argumentIndex)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var patched = 0;
    var il = method.Body.GetILProcessor();
    var parameter = method.Parameters[argumentIndex];
    foreach (var ret in method.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToArray())
    {
        il.InsertBefore(ret, Instruction.Create(OpCodes.Ldarg_S, parameter));
        il.InsertBefore(ret, Instruction.Create(OpCodes.Call, helper));
        patched++;
    }

    return patched;
}

static int PatchUpgradeGrowth(MethodDefinition method, MethodReference helper)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var instructions = method.Body.Instructions;
    var powCall = instructions.FirstOrDefault(i =>
        i.OpCode == OpCodes.Call
        && i.Operand is MethodReference called
        && called.DeclaringType.FullName == "System.Math"
        && called.Name == "Pow");

    if (powCall is null)
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing System.Math.Pow");
    }

    var powIndex = instructions.IndexOf(powCall);
    var growthLoad = instructions.Take(powIndex).LastOrDefault(IsLoadLocal3)
        ?? throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing cost growth load before System.Math.Pow");

    method.Body.GetILProcessor().InsertAfter(growthLoad, Instruction.Create(OpCodes.Call, helper));
    return 1;
}

static int PatchZenithAttributeApplication(MethodDefinition method, MethodReference helper)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var patched = 0;
    foreach (var instruction in method.Body.Instructions)
    {
        if (instruction.Operand is MethodReference called
            && called.DeclaringType.Name == "AttributeModifier"
            && called.Name == "ApplyAllAttributes")
        {
            instruction.Operand = helper;
            patched++;
        }
    }

    if (patched == 0)
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing Zenith ApplyAllAttributes call");
    }

    return patched;
}

static int PatchSecretShopAttributeApplication(MethodDefinition method, MethodReference helper)
{
    if (CallsHelper(method, helper.Name))
    {
        return 0;
    }

    var sawSecretDatabase = false;
    foreach (var instruction in method.Body.Instructions)
    {
        if (instruction.Operand is FieldReference field
            && field.DeclaringType.Name == "SecretDatabase"
            && field.Name == "upgradeDatabase")
        {
            sawSecretDatabase = true;
            continue;
        }

        if (sawSecretDatabase
            && instruction.Operand is MethodReference called
            && called.DeclaringType.Name == "UpgradeContainer"
            && called.Name == "applyAttributes")
        {
            instruction.OpCode = OpCodes.Call;
            instruction.Operand = helper;
            return 1;
        }
    }

    throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing SecretDatabase applyAttributes call");
}

static bool CallsHelper(MethodDefinition method, string helperName)
{
    return method.Body.Instructions.Any(i =>
        i.Operand is MethodReference called
        && called.Name == helperName
        && called.DeclaringType.Name == "JohnnyPowerPatchRuntime");
}

static bool CallsMethod(MethodDefinition method, string methodName, string declaringTypeName)
{
    return method.Body.Instructions.Any(i =>
        i.Operand is MethodReference called
        && called.Name == methodName
        && called.DeclaringType.Name == declaringTypeName);
}

static bool CallsField(MethodDefinition method, FieldReference field)
{
    return method.Body.Instructions.Any(i =>
        i.Operand is FieldReference called
        && called.Name == field.Name
        && called.DeclaringType.Name == field.DeclaringType.Name);
}

static bool IsLoadLocal3(Instruction instruction)
{
    return instruction.OpCode == OpCodes.Ldloc_3
        || (instruction.OpCode == OpCodes.Ldloc_S && instruction.Operand is VariableDefinition variable && variable.Index == 3)
        || (instruction.OpCode == OpCodes.Ldloc && instruction.Operand is VariableDefinition variableLong && variableLong.Index == 3);
}

static bool IsIdentityMethod(MethodDefinition method)
{
    var meaningful = method.Body.Instructions.Where(i => i.OpCode != OpCodes.Nop).ToArray();
    return meaningful.Length == 2
        && meaningful[0].OpCode == OpCodes.Ldarg_0
        && meaningful[1].OpCode == OpCodes.Ret;
}

static bool IsScalePositiveDoubleHelperValid(MethodDefinition method, FieldReference isZenithApplying, FieldReference isSecretShopApplying)
{
    if (!HasDoubleConstant(method, ZenithBonusMultiplier)
        || !HasDoubleConstant(method, SecretShopBonusMultiplier)
        || !CallsField(method, isZenithApplying)
        || !CallsField(method, isSecretShopApplying))
    {
        return false;
    }

    return true;
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

static void RequireIdentityMethod(MethodDefinition method)
{
    if (!IsIdentityMethod(method))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is not an identity helper");
    }
}

static void RequireScalePositiveDoubleHelper(MethodDefinition method, FieldReference isZenithApplying, FieldReference isSecretShopApplying)
{
    if (!IsScalePositiveDoubleHelperValid(method, isZenithApplying, isSecretShopApplying))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} has invalid positive-double helper IL");
    }
}

static void RequireArgumentPatch(MethodDefinition method, string helperName)
{
    if (!CallsHelper(method, helperName))
    {
        throw new InvalidOperationException($"{method.DeclaringType.Name}.{method.Name} is missing {helperName}");
    }
}

static void RequireCallPatch(MethodDefinition method, string helperName)
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
    MethodReference ScalePositiveDouble,
    MethodReference ScaleUpgradeCostGrowth,
    MethodReference ClampTrialMultiplier,
    MethodReference ClampDartBoardAttribute,
    MethodReference ApplyZenithAttributes,
    MethodReference ApplySecretShopAttributes);

readonly record struct HelperSetup(HelperMethods Methods, int Updates);
