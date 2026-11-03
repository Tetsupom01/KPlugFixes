using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("tetsupom.kplug.crossfader.compatfix", "kPlug CrossFader Compat Fix", "1.2.0")]
public sealed class KPlugCrossFaderCompatFix : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
        // Production logging is OFF by default. Reading this BepInEx setting does not
        // alter the CrossFader gate, MoveBoundary or ExtraIK suppression conditions.
        try
        {
            CompatGate.DiagnosticLoggingEnabled = Config.Bind<bool>(
                "Logging", "EnableDiagnosticLogs", false,
                "Write CrossFader and ExtraIK diagnostic logs (restart required). Default: false.").Value;
        }
        catch (Exception e)
        {
            CompatGate.DiagnosticLoggingEnabled = false;
            Logger.LogWarning("Logging configuration could not be read; diagnostic logging remains OFF: " + e.Message);
        }
        CompatGate.Initialize();

        try
        {
            _harmony = new Harmony("tetsupom.kplug.crossfader.compatfix");

            MethodInfo patchAllAssembly = typeof(Harmony).GetMethod(
                "PatchAll",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Assembly) },
                null);

            if (!object.ReferenceEquals(patchAllAssembly, null))
            {
                patchAllAssembly.Invoke(_harmony, new object[] { typeof(KPlugCrossFaderCompatFix).Assembly });
            }
            else
            {
                MethodInfo patchAllNoArgs = typeof(Harmony).GetMethod(
                    "PatchAll",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    Type.EmptyTypes,
                    null);

                if (object.ReferenceEquals(patchAllNoArgs, null))
                    throw new MissingMethodException("Harmony.PatchAll overload not found.");

                patchAllNoArgs.Invoke(_harmony, null);
            }

            if (!CompatGate.UpdateTranspilerPatched)
                throw new InvalidOperationException("HSceneProc.Update transpiler did not patch the expected block.");

            if (!CompatGate.SetterPatchReady)
                throw new InvalidOperationException("HFlag.selectAnimationListInfo setter patch was not resolved.");

            CFMoveBoundaryState.ReadyCheck();
            CompatGate.Log("v1.2.0-integrated-final MoveBoundary active (original 1.1.0 logic unchanged).");
        }
        catch (Exception ex)
        {
            CompatGate.Log("[FATAL] " + ex);
            throw;
        }
    }

    // Optional ExtraIK guard starts after the proven CrossFader Awake workflow.
    private void Start() { CFExtraIKGuard.TryInitialize(); }
    private void Update() { CFExtraIKGuard.Tick(); }
    private void OnDestroy() { CFExtraIKGuard.Cleanup(); }
}

internal static class CompatGate
{
    private const string Version = "1.2.0";

    internal static bool DiagnosticLoggingEnabled;
    private static string _logPath;
    private static readonly object _logSync = new object();
    private static bool _initialized;

    private static MethodInfo _cfIsEnabled;
    private static MethodInfo _cfInTransition;
    private static bool _cfResolvedLogged;
    private static bool _cfMissingLogged;

    private static bool _gateActive;
    private static bool _changeStarted;
    private static object _firstPending;
    private static int _gateSerial;
    private static int _gateStartFrame;
    private static int _waitStableFrames;
    private static int _busyStableFrames;
    private static int _blockedWrites;

    internal static bool UpdateTranspilerPatched;
    internal static bool SetterPatchReady;

    internal static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _logPath = Path.Combine(Paths.BepInExRootPath, "KPlugCrossFaderCompatFix.log");

        if (DiagnosticLoggingEnabled)
        {
            try { File.WriteAllText(_logPath, ""); }
            catch { }
        }

        Log("=== KPlugCrossFaderCompatFix v" + Version + " ===");
        Log("Strategy=first request wins at HFlag.selectAnimationListInfo setter.");
        Log("First non-null request is accepted and arms the gate.");
        Log("Later non-null setter writes are blocked while gate is active; null clear is always allowed.");
        Log("First request is deferred in HSceneProc.Update until CrossFader and all H Animators are stable for 2 consecutive frames.");
        Log("After ChangeAnimator executes, gate remains active until all H Animators are stable for 2 consecutive frames.");
        Log("Unchanged=voiceWait, click, ChangeAnimator body, runtimeAnimatorController setter, CrossFader body, kPlug input methods.");
    }

    internal static void ResolveSetterPatch()
    {
        Type hFlag = AccessTools.TypeByName("HFlag");
        if (object.ReferenceEquals(hFlag, null))
            throw new TypeLoadException("HFlag not found.");

        PropertyInfo p = hFlag.GetProperty(
            "selectAnimationListInfo",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (object.ReferenceEquals(p, null))
            throw new MissingMemberException("HFlag.selectAnimationListInfo property not found.");

        MethodInfo setter = p.GetSetMethod(true);
        if (object.ReferenceEquals(setter, null))
            throw new MissingMethodException("HFlag.set_selectAnimationListInfo not found.");

        SetterPatchReady = true;
    }

    internal static bool SetterPrefix(object value)
    {
        // Vanilla null clear must always pass.
        if (object.ReferenceEquals(value, null))
            return true;

        if (!CrossFaderIsEnabled())
            return true;

        if (!_gateActive)
        {
            _gateActive = true;
            _changeStarted = false;
            _firstPending = value;
            _gateSerial++;
            _gateStartFrame = Time.frameCount;
            _waitStableFrames = 0;
            _busyStableFrames = 0;
            _blockedWrites = 0;

            Log("[GATE START #" + _gateSerial + "] frame=" + Time.frameCount +
                " first={" + DescribePending(value) + "}");

            return true;
        }

        // Same object re-write is harmless; allow it.
        if (object.ReferenceEquals(value, _firstPending))
            return true;

        _blockedWrites++;

        Log("[BLOCK WRITE #" + _gateSerial + "." + _blockedWrites + "] frame=" +
            Time.frameCount +
            " first={" + DescribePending(_firstPending) + "}" +
            " blocked={" + DescribePending(value) + "}");

        return false;
    }

    internal static bool ShouldDeferUpdate(object hSceneProc)
    {
        if (!_gateActive || _changeStarted)
            return false;

        bool cfTransition;
        string animatorTransition;
        bool stable = IsStable(hSceneProc, out cfTransition, out animatorTransition);

        if (stable)
        {
            _waitStableFrames++;

            if (_waitStableFrames == 1)
            {
                Log("[WAIT STABLE 1 #" + _gateSerial + "] frame=" + Time.frameCount +
                    " first={" + DescribePending(_firstPending) + "}");
            }

            if (_waitStableFrames >= 2)
            {
                _changeStarted = true;
                _busyStableFrames = 0;

                Log("[ALLOW CHANGE #" + _gateSerial + "] frame=" + Time.frameCount +
                    " waitedFrames=" + (Time.frameCount - _gateStartFrame) +
                    " blockedWrites=" + _blockedWrites +
                    " first={" + DescribePending(_firstPending) + "}");

                return false;
            }
        }
        else
        {
            if (_waitStableFrames != 0)
                _waitStableFrames = 0;

            LogOccasionalWait(cfTransition, animatorTransition);
        }

        return true;
    }

    internal static void OnUpdatePostfix(object hSceneProc)
    {
        if (!_gateActive || !_changeStarted)
            return;

        try
        {
            bool cfTransition;
            string animatorTransition;
            bool stable = IsStable(hSceneProc, out cfTransition, out animatorTransition);

            if (stable)
            {
                _busyStableFrames++;

                if (_busyStableFrames == 1)
                {
                    Log("[BUSY STABLE 1 #" + _gateSerial + "] frame=" + Time.frameCount);
                }

                if (_busyStableFrames >= 2)
                {
                    Log("[GATE RELEASE #" + _gateSerial + "] frame=" + Time.frameCount +
                        " blockedWrites=" + _blockedWrites +
                        " first={" + DescribePending(_firstPending) + "}");

                    ResetGate();
                }
            }
            else
            {
                _busyStableFrames = 0;
            }
        }
        catch (Exception ex)
        {
            _busyStableFrames = 0;
            Log("[WARN] gate release observation failed; gate remains closed: " +
                ex.GetType().FullName + ": " + ex.Message);
        }
    }

    private static void LogOccasionalWait(bool cfTransition, string animatorTransition)
    {
        int elapsed = Time.frameCount - _gateStartFrame;

        if (elapsed == 0 || elapsed == 1 || (elapsed % 120) == 0)
        {
            Log("[WAIT #" + _gateSerial + "] frame=" + Time.frameCount +
                " CrossFader=" + cfTransition +
                " Animator=" + (animatorTransition == null ? "none" : animatorTransition) +
                " first={" + DescribePending(_firstPending) + "}");
        }
    }

    // HPointMove-only correction. Preserves the original first-wins and stability logic.
    // Called synchronously after vanilla destination ChangeAnimator completes.
    internal static bool TryExpirePendingAtMoveBoundary(object expectedFirst, object flag,
        System.Reflection.PropertyInfo selectedProperty, int moveStartFrame, int nowFrame)
    {
        if (!_gateActive || _changeStarted || object.ReferenceEquals(expectedFirst, null) ||
            !object.ReferenceEquals(_firstPending, expectedFirst)) return false;
        if (_gateStartFrame < moveStartFrame || _gateStartFrame > nowFrame) return false;
        if (object.ReferenceEquals(flag, null) || object.ReferenceEquals(selectedProperty, null)) return false;
        if (!object.ReferenceEquals(selectedProperty.GetValue(flag, null), expectedFirst)) return false;
        // The existing setter always accepts null; cancel only this matching, unexecuted request.
        selectedProperty.SetValue(flag, null, null);
        if (!object.ReferenceEquals(selectedProperty.GetValue(flag, null), null))
        {
            Log("[MOVE_BOUNDARY ERROR] selected null-clear not verified; original gate retained");
            return false;
        }
        Log("[MOVE_BOUNDARY CANCEL-FIRST] gate=" + _gateSerial + " frame=" + nowFrame +
            " firstFrame=" + _gateStartFrame + " previouslyBlocked=" + _blockedWrites);
        ResetGate(); // Original implementation, unchanged.
        return true;
    }

    private static void ResetGate()
    {
        _gateActive = false;
        _changeStarted = false;
        _firstPending = null;
        _gateStartFrame = 0;
        _waitStableFrames = 0;
        _busyStableFrames = 0;
        _blockedWrites = 0;
    }

    private static bool ResolveCrossFader()
    {
        if (!object.ReferenceEquals(_cfIsEnabled, null) &&
            !object.ReferenceEquals(_cfInTransition, null))
            return true;

        try
        {
            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < loaded.Length; i++)
            {
                Type hooks = null;

                try
                {
                    hooks = loaded[i].GetType("Bero.CrossFader.Hooks", false);
                }
                catch
                {
                    hooks = null;
                }

                if (object.ReferenceEquals(hooks, null))
                    continue;

                MethodInfo isEnabled = hooks.GetMethod(
                    "IsEnabled",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                MethodInfo inTransition = hooks.GetMethod(
                    "InTransition",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);

                if (object.ReferenceEquals(isEnabled, null) ||
                    object.ReferenceEquals(inTransition, null))
                    continue;

                _cfIsEnabled = isEnabled;
                _cfInTransition = inTransition;

                if (!_cfResolvedLogged)
                {
                    _cfResolvedLogged = true;
                    Log("[CrossFader] Hooks resolved from " + loaded[i].GetName().Name + ".");
                }

                return true;
            }

            if (!_cfMissingLogged)
            {
                _cfMissingLogged = true;
                Log("[CrossFader] Hooks not loaded yet; fail-open until available.");
            }
        }
        catch (Exception ex)
        {
            Log("[WARN] ResolveCrossFader failed: " + ex.GetType().FullName + ": " + ex.Message);
        }

        return false;
    }

    private static bool CrossFaderIsEnabled()
    {
        if (!ResolveCrossFader())
            return false;

        try
        {
            object r = _cfIsEnabled.Invoke(null, null);
            return r is bool && (bool)r;
        }
        catch
        {
            return false;
        }
    }

    private static bool CrossFaderInTransitionSafe()
    {
        if (!ResolveCrossFader())
            return true;

        try
        {
            object r = _cfInTransition.Invoke(null, null);
            return r is bool && (bool)r;
        }
        catch
        {
            return true;
        }
    }

    private static Animator GetAnimator(object cha)
    {
        if (object.ReferenceEquals(cha, null))
            return null;

        try
        {
            Type t = cha.GetType();

            PropertyInfo p = t.GetProperty(
                "animBody",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (!object.ReferenceEquals(p, null))
                return p.GetValue(cha, null) as Animator;

            FieldInfo f = t.GetField(
                "animBody",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (!object.ReferenceEquals(f, null))
                return f.GetValue(cha) as Animator;
        }
        catch
        {
        }

        return null;
    }

    private static string CheckAnimator(object cha, string label)
    {
        if (object.ReferenceEquals(cha, null))
            return null;

        Animator animator = GetAnimator(cha);
        if (animator == null)
            return null;

        try
        {
            int layers = animator.layerCount;

            for (int layer = 0; layer < layers; layer++)
            {
                if (animator.IsInTransition(layer))
                    return label + "[layer=" + layer + "]";
            }

            return null;
        }
        catch (Exception ex)
        {
            return label + "[read-error=" + ex.GetType().Name + "]";
        }
    }

    private static string FindAnyAnimatorTransition(object hSceneProc)
    {
        if (object.ReferenceEquals(hSceneProc, null))
            return "HSceneProc[null]";

        Type t = hSceneProc.GetType();

        try
        {
            FieldInfo femalesField = t.GetField(
                "lstFemale",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (!object.ReferenceEquals(femalesField, null))
            {
                IEnumerable females = femalesField.GetValue(hSceneProc) as IEnumerable;

                if (!object.ReferenceEquals(females, null))
                {
                    int index = 0;

                    foreach (object female in females)
                    {
                        string hit = CheckAnimator(female, "female" + index);

                        if (hit != null)
                            return hit;

                        index++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return "female-list[read-error=" + ex.GetType().Name + "]";
        }

        string[] maleFields = new string[] { "male", "male1" };

        for (int i = 0; i < maleFields.Length; i++)
        {
            try
            {
                FieldInfo f = t.GetField(
                    maleFields[i],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (object.ReferenceEquals(f, null))
                    continue;

                string hit = CheckAnimator(f.GetValue(hSceneProc), maleFields[i]);

                if (hit != null)
                    return hit;
            }
            catch (Exception ex)
            {
                return maleFields[i] + "[read-error=" + ex.GetType().Name + "]";
            }
        }

        return null;
    }

    private static bool IsStable(object hSceneProc, out bool cfTransition, out string animatorTransition)
    {
        cfTransition = CrossFaderInTransitionSafe();
        animatorTransition = FindAnyAnimatorTransition(hSceneProc);

        return !cfTransition && animatorTransition == null;
    }

    private static string DescribePending(object pending)
    {
        if (object.ReferenceEquals(pending, null))
            return "null";

        try
        {
            Type t = pending.GetType();

            FieldInfo fId = t.GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo fMode = t.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo fName = t.GetField("nameAnimation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            object id = object.ReferenceEquals(fId, null) ? null : fId.GetValue(pending);
            object mode = object.ReferenceEquals(fMode, null) ? null : fMode.GetValue(pending);
            object name = object.ReferenceEquals(fName, null) ? null : fName.GetValue(pending);

            return "id=" + SafeText(id) + " mode=" + SafeText(mode) + " name=" + SafeText(name);
        }
        catch
        {
            return pending.GetType().FullName;
        }
    }

    private static string SafeText(object value)
    {
        if (object.ReferenceEquals(value, null))
            return "<null>";

        try
        {
            return value.ToString();
        }
        catch
        {
            return "<ToString failed>";
        }
    }

    internal static void Log(string message)
    {
        if (!DiagnosticLoggingEnabled)
        {
            // Keep operational errors visible in BepInEx console even when
            // continuous diagnostic logging is disabled.
            if (message.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("FATAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("WARN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("EXCEPTION", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                try { Console.WriteLine("[KPlugCrossFaderCompatFix] " + message); } catch { }
            }
            return;
        }
        string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message;

        try
        {
            Console.WriteLine("[KPlugCrossFaderCompatFix] " + message);
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrEmpty(_logPath))
                lock (_logSync) { File.AppendAllText(_logPath, line + Environment.NewLine); }
        }
        catch
        {
        }
    }
}

[HarmonyPatch]
internal static class HFlagSelectAnimationSetterPatch
{
    private static MethodBase TargetMethod()
    {
        CompatGate.ResolveSetterPatch();

        Type hFlag = AccessTools.TypeByName("HFlag");
        PropertyInfo p = hFlag.GetProperty(
            "selectAnimationListInfo",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return p.GetSetMethod(true);
    }

    [HarmonyPrefix]
    private static bool Prefix(object value)
    {
        return CompatGate.SetterPrefix(value);
    }
}

[HarmonyPatch]
internal static class HSceneProcUpdateCompatPatch
{
    private static MethodBase TargetMethod()
    {
        Type t = AccessTools.TypeByName("HSceneProc");

        if (object.ReferenceEquals(t, null))
            throw new TypeLoadException("HSceneProc not found.");

        MethodInfo m = AccessTools.Method(t, "Update");

        if (object.ReferenceEquals(m, null))
            throw new MissingMethodException("HSceneProc.Update not found.");

        return m;
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        List<CodeInstruction> list = new List<CodeInstruction>(instructions);

        int changeAnimatorCall = -1;
        int setterCall = -1;

        for (int i = 0; i < list.Count; i++)
        {
            MethodBase mb = list[i].operand as MethodBase;

            if (object.ReferenceEquals(mb, null))
                continue;

            if (mb.Name == "ChangeAnimator" &&
                !object.ReferenceEquals(mb.DeclaringType, null) &&
                mb.DeclaringType.FullName == "HSceneProc")
            {
                changeAnimatorCall = i;
                break;
            }
        }

        if (changeAnimatorCall < 5)
            throw new InvalidOperationException("Expected HSceneProc.ChangeAnimator call was not found.");

        for (int i = changeAnimatorCall + 1; i < list.Count; i++)
        {
            MethodBase mb = list[i].operand as MethodBase;

            if (!object.ReferenceEquals(mb, null) &&
                mb.Name == "set_selectAnimationListInfo" &&
                !object.ReferenceEquals(mb.DeclaringType, null) &&
                mb.DeclaringType.FullName == "HFlag")
            {
                setterCall = i;
                break;
            }

            if (i > changeAnimatorCall + 12)
                break;
        }

        if (setterCall < 0 || setterCall + 1 >= list.Count)
            throw new InvalidOperationException("Expected vanilla selectAnimationListInfo clear was not found.");

        int start = changeAnimatorCall - 5;

        if (list[start].opcode != OpCodes.Ldarg_0 ||
            list[start + 1].opcode != OpCodes.Ldarg_0 ||
            list[start + 2].opcode != OpCodes.Ldfld ||
            !(list[start + 3].opcode == OpCodes.Call || list[start + 3].opcode == OpCodes.Callvirt) ||
            list[start + 4].opcode != OpCodes.Ldc_I4_0)
        {
            throw new InvalidOperationException("ChangeAnimator argument-build IL shape did not match expected HSceneProc.Update.");
        }

        FieldInfo flagsField = list[start + 2].operand as FieldInfo;

        if (object.ReferenceEquals(flagsField, null) ||
            object.ReferenceEquals(flagsField.DeclaringType, null) ||
            flagsField.DeclaringType.FullName != "HSceneProc" ||
            flagsField.Name != "flags")
        {
            throw new InvalidOperationException("Expected HSceneProc.flags field not found.");
        }

        Label skipChangeAndClear = generator.DefineLabel();
        list[setterCall + 1].labels.Add(skipChangeAndClear);

        MethodInfo gate = AccessTools.Method(typeof(CompatGate), "ShouldDeferUpdate");

        if (object.ReferenceEquals(gate, null))
            throw new MissingMethodException("CompatGate.ShouldDeferUpdate not found.");

        List<Label> originalLabels = new List<Label>(list[start].labels);
        list[start].labels.Clear();

        CodeInstruction first = new CodeInstruction(OpCodes.Ldarg_0);
        for (int i = 0; i < originalLabels.Count; i++)
            first.labels.Add(originalLabels[i]);

        List<CodeInstruction> inject = new List<CodeInstruction>();
        inject.Add(first);
        inject.Add(new CodeInstruction(OpCodes.Call, gate));
        inject.Add(new CodeInstruction(OpCodes.Brtrue, skipChangeAndClear));

        list.InsertRange(start, inject);

        CompatGate.UpdateTranspilerPatched = true;
        CompatGate.Log("[PATCH] HSceneProc.Update: first pending request is preserved until safe; deferred frames skip ChangeAnimator and vanilla null-clear.");

        return list;
    }

    [HarmonyPostfix]
    private static void Postfix(object __instance)
    {
        CompatGate.OnUpdatePostfix(__instance);
    }
}

// CrossFaderCompatFix v1.0.0 source append-only HPointMove boundary trial.
// Original source is SHA pinned. This file is appended after a single small CompatGate API injection.
// Intended for kPlug 3.6.0, Mono CLR2, Unity 5.6.2. Not verified on Windows yet.
[HarmonyPatch]
internal static class CFMoveStartPatch
{
    private static System.Reflection.MethodBase TargetMethod()
    {
        return CFMoveBoundaryState.RequireMethod("HSceneProc", "GotoPointMoveScene", System.Type.EmptyTypes);
    }
    private static void Postfix(object __instance) { CFMoveBoundaryState.MoveBegin(__instance); }
}

[HarmonyPatch]
internal static class CFMoveCallbackPatch
{
    private static System.Reflection.MethodBase TargetMethod()
    {
        return CFMoveBoundaryState.RequireMethod("HPointMove+<Update>c__AnonStorey2", "<>m__0", System.Type.EmptyTypes);
    }
    private static void Prefix() { CFMoveBoundaryState.CallbackBegin(); }
    private static System.Exception Finalizer(System.Exception __exception)
    {
        CFMoveBoundaryState.CallbackEnd(__exception);
        return __exception;
    }
}

[HarmonyPatch]
internal static class CFMoveCategoryPatch
{
    private static System.Reflection.MethodBase TargetMethod()
    {
        System.Type point = CFMoveBoundaryState.RequireType("H.HPointData");
        return CFMoveBoundaryState.RequireMethod("HSceneProc", "ChangeCategory", new System.Type[] { point, typeof(int) });
    }
    private static void Prefix() { CFMoveBoundaryState.CategoryBegin(); }
    private static System.Exception Finalizer(System.Exception __exception)
    {
        CFMoveBoundaryState.CategoryEnd(__exception);
        return __exception;
    }
}

[HarmonyPatch]
internal static class CFMoveAppliedPatch
{
    private static System.Reflection.MethodBase TargetMethod()
    {
        System.Type info = CFMoveBoundaryState.RequireType("HSceneProc+AnimationListInfo");
        return CFMoveBoundaryState.RequireMethod("HSceneProc", "ChangeAnimator", new System.Type[] { info, typeof(bool) });
    }
    private static void Postfix(object __instance, object __0, bool __1)
    {
        CFMoveBoundaryState.AfterMoveAnimator(__instance, __0, __1);
    }
}

internal static class CFMoveBoundaryState
{
    private const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
    private static bool ready;
    private static object moveProc;
    private static int moveStartFrame = -1, moveGeneration, invalidatedGeneration = -1;
    private static int candidates, invalidated, skipped, errors;
    private static int extraIkCategorySerial;
    [System.ThreadStatic] private static int callbackDepth;
    [System.ThreadStatic] private static int categoryDepth;
    private static System.Reflection.FieldInfo flagsField, nowInfoField, femaleField, femalePathField, pathFileField;
    private static System.Reflection.PropertyInfo selectedProperty;

    internal static System.Type RequireType(string fullName)
    {
        foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type t = a.GetType(fullName, false);
            if (!object.ReferenceEquals(t, null)) return t;
        }
        throw new System.TypeLoadException("MOVE_BOUNDARY_TYPE_MISSING: " + fullName);
    }
    private static System.Reflection.FieldInfo RequireField(System.Type t, string name)
    {
        System.Reflection.FieldInfo f = t.GetField(name, All);
        if (object.ReferenceEquals(f, null)) throw new System.MissingFieldException(t.FullName, name);
        return f;
    }
    internal static System.Reflection.MethodBase RequireMethod(string type, string name, System.Type[] args)
    {
        System.Type t = RequireType(type);
        System.Reflection.MethodInfo m = t.GetMethod(name, All, null, args, null);
        if (object.ReferenceEquals(m, null)) throw new System.MissingMethodException(t.FullName, name);
        return m;
    }
    internal static void ReadyCheck()
    {
        System.Type proc = RequireType("HSceneProc");
        System.Type flag = RequireType("HFlag");
        System.Type info = RequireType("HSceneProc+AnimationListInfo");
        flagsField = RequireField(proc, "flags");
        nowInfoField = RequireField(flag, "nowAnimationInfo");
        selectedProperty = flag.GetProperty("selectAnimationListInfo", All);
        if (object.ReferenceEquals(selectedProperty, null) || object.ReferenceEquals(selectedProperty.GetSetMethod(true), null))
            throw new System.MissingMemberException("MOVE_BOUNDARY_SELECTED_PROPERTY_MISSING");
        femaleField = RequireField(info, "paramFemale");
        femalePathField = RequireField(femaleField.FieldType, "path");
        pathFileField = RequireField(femalePathField.FieldType, "file");
        // Ensure Harmony targets are resolved before enabling the new behavior.
        RequireMethod("HSceneProc", "GotoPointMoveScene", System.Type.EmptyTypes);
        RequireMethod("HPointMove+<Update>c__AnonStorey2", "<>m__0", System.Type.EmptyTypes);
        RequireMethod("HSceneProc", "ChangeCategory", new System.Type[] { RequireType("H.HPointData"), typeof(int) });
        RequireMethod("HSceneProc", "ChangeAnimator", new System.Type[] { info, typeof(bool) });
        ready = true;
        CompatGate.Log("[MOVE_BOUNDARY READY] version=1.2.0-final; original first-wins and stable wait unchanged");
    }
    internal static void MoveBegin(object proc)
    {
        if (!ready || object.ReferenceEquals(proc, null)) return;
        moveProc = proc;
        moveStartFrame = UnityEngine.Time.frameCount;
        moveGeneration++;
        CompatGate.Log("[MOVE_BOUNDARY START] generation=" + moveGeneration + " frame=" + moveStartFrame);
    }
    internal static void CallbackBegin() { if (ready) callbackDepth++; }
    internal static void CallbackEnd(System.Exception e)
    {
        if (callbackDepth > 0) callbackDepth--;
        if (!object.ReferenceEquals(e, null)) CompatGate.Log("[MOVE_BOUNDARY CALLBACK_EXCEPTION] " + e.GetType().FullName);
    }
    internal static void CategoryBegin() { if (ready && callbackDepth > 0) { categoryDepth++; extraIkCategorySerial++; } }
    internal static void CategoryEnd(System.Exception e)
    {
        if (categoryDepth > 0) categoryDepth--;
        if (!object.ReferenceEquals(e, null)) CompatGate.Log("[MOVE_BOUNDARY CATEGORY_EXCEPTION] " + e.GetType().FullName);
    }
    // Shared read-only HPointMove boundary predicate for the independent ExtraIK guard.
    // Never changes CrossFader gate/pending request/Animator logic.
    internal static int CurrentExtraIkCategoryStamp(object proc)
    {
        int frame = UnityEngine.Time.frameCount;
        if (!ready || callbackDepth <= 0 || categoryDepth <= 0 ||
            object.ReferenceEquals(proc, null) || !object.ReferenceEquals(proc, moveProc) ||
            moveStartFrame < 0 || frame < moveStartFrame || frame - moveStartFrame > 7200)
            return -1;
        return extraIkCategorySerial;
    }
    private static string FemaleFile(object info)
    {
        object female = femaleField.GetValue(info);
        if (object.ReferenceEquals(female, null)) return null;
        object path = femalePathField.GetValue(female);
        if (object.ReferenceEquals(path, null)) return null;
        return pathFileField.GetValue(path) as string;
    }
    internal static void AfterMoveAnimator(object proc, object destination, bool boolArg)
    {
        if (!ready || boolArg || callbackDepth <= 0 || categoryDepth <= 0) return;
        candidates++;
        try
        {
            int frame = UnityEngine.Time.frameCount;
            if (object.ReferenceEquals(proc, null) || object.ReferenceEquals(destination, null) ||
                !object.ReferenceEquals(proc, moveProc) || moveStartFrame < 0 || frame < moveStartFrame ||
                frame - moveStartFrame > 7200 || invalidatedGeneration == moveGeneration) { Skip("context"); return; }
            object flag = flagsField.GetValue(proc);
            if (object.ReferenceEquals(flag, null)) { Skip("no_flags"); return; }
            object now = nowInfoField.GetValue(flag);
            object pending = selectedProperty.GetValue(flag, null);
            if (!object.ReferenceEquals(now, destination) || object.ReferenceEquals(pending, null) ||
                object.ReferenceEquals(now, pending)) { Skip("not_stale_candidate"); return; }
            string oldFile = FemaleFile(pending), newFile = FemaleFile(now);
            if (System.String.IsNullOrEmpty(oldFile) || System.String.IsNullOrEmpty(newFile) ||
                System.String.Equals(oldFile, newFile, System.StringComparison.Ordinal)) { Skip("same_or_unknown_file"); return; }
            // Gate state is checked and changed atomically by CompatGate on the Unity main thread.
            if (CompatGate.TryExpirePendingAtMoveBoundary(pending, flag, selectedProperty, moveStartFrame, frame))
            {
                invalidatedGeneration = moveGeneration;
                invalidated++;
                CompatGate.Log("[MOVE_BOUNDARY INVALIDATED] generation=" + moveGeneration + " frame=" + frame +
                    " oldFile=" + oldFile + " destinationFile=" + newFile + " count=" + invalidated);
            }
            else Skip("gate_not_pending_or_changed");
        }
        catch (System.Exception ex)
        {
            errors++;
            CompatGate.Log("[MOVE_BOUNDARY ERROR] count=" + errors + " exception=" + ex);
            // No fallback state mutation. Source gate retains its pre-existing policy.
        }
    }
    private static void Skip(string why)
    {
        skipped++;
        if (candidates <= 40) CompatGate.Log("[MOVE_BOUNDARY PASS] reason=" + why + " candidate=" + candidates);
    }
}

// BEGIN validated ExtraIK guard integration from historical v0.2.4
internal static class CFExtraIKGuard
{
        const string ExpectedKPlug = "34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855";
        const string ExpectedAssembly = "0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5";
        const string HarmonyId = "tetsupom.kplug.crossfader.compatfix.extraik.trial";
        const string BuildMarker = "CF_EXTRA_IK_INTEGRATED_V1_2_0";
        static Harmony harmony;
        static FieldInfo coreFlag, coreCtrl, coreCfg, freeH, cachedAnim, cachedState, extras, dark, useIk;
        static FieldInfo stateName, animInfo, female, path, file, targetName, targetMode, targetId;
        static PropertyInfo mode;
        static MethodInfo contains;
        static bool sceneProbed;
        static int goodMarks,badMarks;
        static bool ready;
        // Read-only hook inventory at target transition and F7/F8; no extra Harmony patches.
        static Type hookType;
        static FieldInfo hookAnim, hookState, hookEffector, hookTarget, hookDestroy;
        static int sampleFrame=-1, sampleStage, samples;
        static int animatorCount, destCount, checkCount, suppressed, passed, errors;
        static int lastCategoryStamp = -1;
        // The exact original chain continues even when one stale SetIkTargets(false) is skipped.
        public sealed class Scope
        {
            internal int id, frame, checkId;
            internal object target;
            internal bool inPostSetPlay, inCheck, used;
            internal object checkManager;
            internal string sourceAnim, oldState, newState, destMode, destFile;
            internal int categoryStamp;
            internal object proc;
        }
        [ThreadStatic] static Scope active;

        internal static void TryInitialize()
        {
            try
            {
                string root = Path.GetFullPath(Path.Combine(Paths.BepInExRootPath, ".."));
                AssertHash(Path.Combine(root, @"BepInEx\plugins\kPlug\kPlug.dll"), ExpectedKPlug);
                AssertHash(Path.Combine(root, @"Koikatu_Data\Managed\Assembly-CSharp.dll"), ExpectedAssembly);
                string dir = Path.Combine(root, @"BepInEx\plugins\KPlugFixes");
                foreach (string n in new string[] { "HPointIKTargetRestoreFix.dll", "HPointIKPreApplyRestore.dll", "HPointIKStateBranchDiag.dll", "HPointILRuntimeProbe.dll" })
                    if (File.Exists(Path.Combine(dir,n))) throw new InvalidOperationException("Conflicting old diagnostic/experiment present: " + n);
                Assembly kp = FindAssembly("kPlug"), game = FindAssembly("Assembly-CSharp");
                if (kp == null || game == null) throw new InvalidOperationException("kPlug or Assembly-CSharp not loaded");
                BindingFlags i=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
                BindingFlags s=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
                Type core=NeedType(kp,"kPlug.Core");
                Type mgr=NeedType(kp,"kPlug.CmpH.ExtraAnimManager");
                Type tool=NeedType(kp,"kPlug.Tools.ToolAnimation");
                Type cfg=NeedType(kp,"kPlug.CmpBase.Cfg");
                Type ctrl=NeedType(kp,"kPlug.CmpH.HCtrl");
                hookType=NeedType(kp,"kPlug.Tools.ToolExtrAnimIK+IkHook");
                hookAnim=NeedField(hookType,"anim",i);
                hookState=NeedField(hookType,"state",i);
                hookEffector=NeedField(hookType,"ikEffector",i);
                hookTarget=NeedField(hookType,"target",i);
                hookDestroy=NeedField(hookType,"inDestroyOrder",i);
                Type hf=NeedType(game,"HFlag");
                Type proc=NeedType(game,"HSceneProc");
                Type target=NeedType(game,"HSceneProc+AnimationListInfo");
                Type point=NeedRuntimeType("H.HPointData");
                Type callbackType=NeedRuntimeType("HPointMove+<Update>c__AnonStorey2");
                coreFlag=NeedField(core,"hFlag",s); coreCtrl=NeedField(core,"hCtrl",s);
                coreCfg=NeedField(core,"config",s); freeH=NeedField(core,"inFreeH",s);
                cachedAnim=NeedField(mgr,"currentAnim",i); cachedState=NeedField(mgr,"currentState",i);
                extras=NeedField(tool,"kExtraAssets",s); dark=NeedField(ctrl,"InCustomDarkness",i);
                useIk=NeedField(cfg,"animUseIks",i);
                stateName=NeedField(hf,"nowAnimStateName",i); animInfo=NeedField(hf,"nowAnimationInfo",i);
                female=NeedField(target,"paramFemale",i);
                path=NeedField(female.FieldType,"path",i); file=NeedField(path.FieldType,"file",i);
                targetName=NeedField(target,"nameAnimation",i);
                targetMode=NeedField(target,"mode",i); targetId=NeedField(target,"id",i);
                mode=hf.GetProperty("mode",i);
                if (mode==null || mode.GetGetMethod(true)==null)throw new MissingMemberException("HFlag.mode");
                contains=extras.FieldType.GetMethod("Contains",i,null,new Type[]{typeof(string)},null);
                if (contains==null || contains.ReturnType!=typeof(bool))throw new MissingMemberException("kExtraAssets.Contains(string)");
                MethodInfo moveBegin=proc.GetMethod("GotoPointMoveScene",i,null,Type.EmptyTypes,null);
                MethodInfo callback=callbackType.GetMethod("<>m__0",i,null,Type.EmptyTypes,null);
                MethodInfo category=proc.GetMethod("ChangeCategory",i,null,new Type[]{point,typeof(int)},null);
                MethodInfo change=proc.GetMethod("ChangeAnimator",i,null,new Type[]{target,typeof(bool)},null);
                MethodInfo post=core.GetMethod("HActionBase_PostSetPlay",s,null,new Type[]{typeof(string),typeof(bool)},null);
                MethodInfo check=mgr.GetMethod("CheckOnStateChange",i,null,Type.EmptyTypes,null);
                MethodInfo set=mgr.GetMethod("SetIkTargets",i,null,new Type[]{typeof(bool)},null);
                if (moveBegin==null || callback==null || category==null ||
                    change==null || post==null || check==null || set==null || change.ReturnType!=typeof(void) ||
                    post.ReturnType!=typeof(void) || check.ReturnType!=typeof(void) || set.ReturnType!=typeof(void))
                    throw new MissingMemberException("Exact HPointMove / ChangeCategory / ChangeAnimator / PostSetPlay / CheckOnStateChange / SetIkTargets signatures");
                if (Harmony.GetPatchInfo(check)!=null || Harmony.GetPatchInfo(set)!=null)
                    throw new InvalidOperationException("Another Harmony patch on CheckOnStateChange or SetIkTargets; refuse overlap");
                // Guard against changed test conditions; read-only ownership checks.
                MethodInfo reset=NeedType(game,"MotionIK").GetMethod("Reset",i,null,Type.EmptyTypes,null);
                if(reset==null || Harmony.GetPatchInfo(reset)!=null)
                    throw new InvalidOperationException("RC0 OFF not verified or Reset patched");
                MethodInfo selector=hf.GetMethod("set_selectAnimationListInfo",i);
                if(selector==null || !HasPatchOwner(selector,"tetsupom.kplug.crossfader.compatfix"))
                    throw new InvalidOperationException("CrossFader Harmony owner missing");
                Log("SESSION_START integrated v1.2.0 run="+BuildMarker+" target=HPointMove_first_ChangeAnimator_only");
                harmony=new Harmony(HarmonyId);
                Type self=typeof(CFExtraIKGuard);
                // Observe the same synchronous Move -> click callback -> Category boundary as CrossFader v1.1.0.
                // All boundary hooks are observational; only SetPrefix may skip an original method body.
                harmony.Patch(change,new HarmonyMethod(self.GetMethod("AnimatorEnter",s)),new HarmonyMethod(self.GetMethod("AnimatorExit",s)),null,null,null);
                harmony.Patch(post,new HarmonyMethod(self.GetMethod("PostEnter",s)),new HarmonyMethod(self.GetMethod("PostExit",s)),null,null,null);
                harmony.Patch(check,new HarmonyMethod(self.GetMethod("CheckEnter",s)),new HarmonyMethod(self.GetMethod("CheckExit",s)),null,null,null);
                harmony.Patch(set,new HarmonyMethod(self.GetMethod("SetPrefix",s)),null,null,null,null);
                if(!HasPatchOwner(change,HarmonyId)||
                   !HasPatchOwner(post,HarmonyId)||!HasPatchOwner(check,HarmonyId)||!HasPatchOwner(set,HarmonyId))
                    throw new InvalidOperationException("Required Harmony hook owner missing; fail closed");
                ready=true;
                CompatGate.Log("[HPointStaleIK] READY integrated v1.2.0 source=v0.2.4 predicate=CrossFaderMoveCategoryStamp; fail-open");
                Log("READY integrated v1.2.0 run="+BuildMarker+" RC1_OFF=True RC0_OFF=True CrossFader=shared target=HPointMove_first_ChangeAnimator");
            }
            catch(Exception e)
            {
                ready=false;
                if(harmony!=null) try{harmony.UnpatchSelf();}catch(Exception un){CompatGate.Log("[HPointStaleIK] UNPATCH_FAIL "+un);}
                CompatGate.Log("[HPointStaleIK] PATCH_NOT_APPLIED "+e);
                Log("PATCH_NOT_APPLIED "+e);
            }
        }
        static bool HasPatchOwner(MethodBase method,string owner)
        {
            object info=Harmony.GetPatchInfo(method);
            if(info==null)return false;
            PropertyInfo prop=info.GetType().GetProperty("Owners",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if(prop==null)throw new MissingMemberException("Harmony patch owners");
            IEnumerable values=prop.GetValue(info,null) as IEnumerable;
            if(values==null)throw new InvalidOperationException("Harmony owners unavailable");
            foreach(object x in values)if(String.Equals(x as string,owner,StringComparison.Ordinal))return true;
            return false;
        }
        static Assembly FindAssembly(string n){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())if(String.Equals(a.GetName().Name,n,StringComparison.OrdinalIgnoreCase))return a;return null;}
        static Type NeedType(Assembly a,string n){Type t=a.GetType(n,false);if(t==null)throw new TypeLoadException(n);return t;}
        static Type NeedRuntimeType(string n){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){Type t=a.GetType(n,false);if(t!=null)return t;}throw new TypeLoadException(n);}
        static FieldInfo NeedField(Type t,string n,BindingFlags f){FieldInfo x=t.GetField(n,f);if(x==null)throw new MissingFieldException(t.FullName,n);return x;}
        static void AssertHash(string p,string expected){if(!File.Exists(p))throw new FileNotFoundException("Required baseline missing",p);using(FileStream f=File.Open(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(SHA256 h=SHA256.Create()){string v=BitConverter.ToString(h.ComputeHash(f)).Replace("-","");if(!String.Equals(v,expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Baseline SHA mismatch: "+p);}}
        static string S(object x){return x==null?"<null>":Convert.ToString(x);}
        static string FileOfInfo(object info)
        {
            if(info==null)return "<null-info>";
            object f=female.GetValue(info);if(f==null)return "<null-female>";
            object p=path.GetValue(f);return p==null?"<null-path>":S(file.GetValue(p));
        }
        static string NowFile(object flag){return FileOfInfo(animInfo.GetValue(flag));}
        static void Log(string m)
        {
            // Shared diagnostic sink. OFF mode still reports errors to BepInEx.
            CompatGate.Log("[ExtraIK] " + m);
        }
        static string TransformInfo(object value)
        {
            Transform t=value as Transform;
            return t==null?"none":t.name+"#"+t.GetInstanceID()+" y="+t.position.y.ToString("F4");
        }
        static void HookSnapshot(string label)
        {
            if (!CompatGate.DiagnosticLoggingEnabled) return;
            try
            {
                UnityEngine.Object[] hooks=UnityEngine.Object.FindObjectsOfType(hookType);
                ++samples;
                Log("HOOK_SNAPSHOT label="+label+" frame="+Time.frameCount+" activeCount="+hooks.Length);
                for(int j=0;j<hooks.Length && j<24;j++)
                {
                    object h=hooks[j];
                    Log("HOOK_ITEM label="+label+" idx="+j+" uid="+((UnityEngine.Object)h).GetInstanceID()+
                        " savedAnim="+S(hookAnim.GetValue(h))+" savedState="+S(hookState.GetValue(h))+
                        " destroying="+S(hookDestroy.GetValue(h))+
                        " eff="+TransformInfo(hookEffector.GetValue(h))+" src="+TransformInfo(hookTarget.GetValue(h)));
                }
                if(hooks.Length>24)Log("HOOK_SNAPSHOT_TRUNCATED label="+label);
            }
            catch(Exception e){++errors;Log("HOOK_SNAPSHOT_ERROR label="+label+" "+e.GetType().Name+" "+e.Message);}
        }
        // Harmony __state restores any enclosing synchronous ChangeAnimator scope.
        public static void AnimatorEnter(object __instance, object __0, bool __1, out Scope __state)
        {
            __state=active;
            active=null;
            if(!ready)return;
            int id=++animatorCount;
            int stamp=CFMoveBoundaryState.CurrentExtraIkCategoryStamp(__instance);
            if(stamp<=0 || lastCategoryStamp==stamp)return;
            // First ChangeAnimator of this original HPointMove ChangeCategory only.
            lastCategoryStamp=stamp;
            if(__1)return; // Direct HPoint destination ChangeAnimator uses false; reject other invocation forms.
            try
            {
                if(__0==null)return;
                string n=S(targetName.GetValue(__0)), m=S(targetMode.GetValue(__0)), f=FileOfInfo(__0);
                if(String.IsNullOrEmpty(m)||String.IsNullOrEmpty(f)||f.StartsWith("<"))return;
                Scope scope=new Scope();scope.id=id;scope.frame=Time.frameCount;scope.target=__0;
                scope.destMode=m;scope.destFile=f;scope.categoryStamp=stamp;scope.proc=__instance;
                active=scope;++destCount;
                Log("MOVE_FIRST_ANIM_ENTER id="+id+" catStamp="+stamp+" frame="+scope.frame+
                    " dest.name="+n+" dest.mode="+m+" dest.id="+S(targetId.GetValue(__0))+" dest.file="+f);
            }
            catch(Exception e){active=null;++errors;Log("GUARD_READ_FAIL_ANIM (FAIL_OPEN) "+e.GetType().Name+" "+e.Message);}
        }
        public static void AnimatorExit(Scope __state)
        {
            Scope x=active;
            if(x!=null)
            {
                Log("DEST_ANIM_EXIT id="+x.id+" frame="+Time.frameCount+" used="+x.used);
                sampleFrame=Time.frameCount;sampleStage=0;
            }
            active=__state;
        }
        public static void PostEnter(out bool __state)
        {
            Scope x=active;
            __state=x!=null && x.inPostSetPlay;
            if(x!=null && ready)x.inPostSetPlay=true;
        }
        public static void PostExit(bool __state)
        {
            Scope x=active;if(x!=null)x.inPostSetPlay=__state;
        }
        public static void CheckEnter(object __instance,out bool __state)
        {
            Scope x=active;
            __state=x!=null && x.inCheck;
            if(!ready||x==null||!x.inPostSetPlay||x.used)return;
            int id=++checkCount;
            try
            {
                object flag=coreFlag.GetValue(null),ctl=coreCtrl.GetValue(null),cfg=coreCfg.GetValue(null);
                if(flag==null||ctl==null||cfg==null)return;
                object previous=animInfo.GetValue(flag);
                string m=S(mode.GetValue(flag,null)), source=NowFile(flag), cache=S(cachedAnim.GetValue(__instance));
                string oldState=S(cachedState.GetValue(__instance)), newState=S(stateName.GetValue(flag));
                object coll=extras.GetValue(null);
                bool isExtra=(coll!=null && (bool)contains.Invoke(coll,new object[]{cache}));
                bool isFree=(bool)freeH.GetValue(null), isDark=(bool)dark.GetValue(ctl), ik=(bool)useIk.GetValue(cfg);
                bool candidate=(Time.frameCount==x.frame &&
                    CFMoveBoundaryState.CurrentExtraIkCategoryStamp(x.proc)==x.categoryStamp &&
                    m==x.destMode && previous!=null && x.destFile!=cache &&
                    S(targetMode.GetValue(previous))=="sonyu" && !System.Object.ReferenceEquals(previous,x.target) &&
                    source==cache && (oldState=="InsertIdle" || oldState=="WLoop" || oldState=="OUT_A" || oldState=="Insert") && newState=="Idle" &&
                    isFree && isExtra && !isDark && ik);
                Log("CHECK_WINDOW dest="+x.id+" check="+id+" frame="+Time.frameCount+" candidate="+candidate+
                    " mode="+m+" old.info="+S(previous==null?null:targetName.GetValue(previous))+" old.info.mode="+S(previous==null?null:targetMode.GetValue(previous))+
                    " flag.file="+source+" cache.anim="+cache+" oldState="+oldState+" newState="+newState+
                    " inFreeH="+isFree+" extraContains="+isExtra+" customDark="+isDark+" animUseIks="+ik);
                if(!candidate)return;
                x.inCheck=true;x.checkId=id;x.checkManager=__instance;
                x.sourceAnim=cache;x.oldState=oldState;x.newState=newState;
            }
            catch(Exception e){x.inCheck=false;x.checkManager=null;++errors;Log("GUARD_READ_FAIL_CHECK (FAIL_OPEN) "+e.GetType().Name+" "+e.Message);}
        }
        public static void CheckExit(bool __state)
        {
            Scope x=active;if(x==null)return;
            if(x.inCheck)Log("CHECK_EXIT dest="+x.id+" check="+x.checkId+" used="+x.used);
            x.inCheck=__state;x.checkManager=null;
        }
        public static bool SetPrefix(object __instance,bool __0)
        {
            Scope x=active;
            if(!ready||x==null||__0||x.used||!x.inPostSetPlay||!x.inCheck||
               !System.Object.ReferenceEquals(x.checkManager,__instance)||Time.frameCount!=x.frame)return true;
            try
            {
                object flag=coreFlag.GetValue(null),cfg=coreCfg.GetValue(null),ctl=coreCtrl.GetValue(null);
                if(flag==null||cfg==null||ctl==null)return true;
                bool stillValid=(CFMoveBoundaryState.CurrentExtraIkCategoryStamp(x.proc)==x.categoryStamp &&
                    x.destFile!=x.sourceAnim &&
                    S(mode.GetValue(flag,null))==x.destMode && NowFile(flag)==x.sourceAnim &&
                    S(cachedAnim.GetValue(__instance))==x.sourceAnim && S(cachedState.GetValue(__instance))=="Idle" &&
                    S(stateName.GetValue(flag))=="Idle" && (bool)freeH.GetValue(null) &&
                    (bool)useIk.GetValue(cfg) && !(bool)dark.GetValue(ctl) &&
                    !System.Object.ReferenceEquals(animInfo.GetValue(flag),x.target) &&
                    S(targetMode.GetValue(animInfo.GetValue(flag)))=="sonyu" &&
                    (x.oldState=="InsertIdle" || x.oldState=="WLoop" || x.oldState=="OUT_A" || x.oldState=="Insert"));
                if(!stillValid){++passed;Log("PASS_CONDITION_CHANGED dest="+x.id+" check="+x.checkId);return true;}
                x.used=true;++suppressed;
                string msg="SUPPRESS_OLD_EXTRA_MOVE dest="+x.id+" check="+x.checkId+" frame="+Time.frameCount+
                    " source="+x.sourceAnim+" oldState="+x.oldState+" newState="+x.newState+" saveOld=false count="+suppressed;
                Log(msg);
                CompatGate.Log("[HPointStaleIK] "+msg);
                return false; // One original SetIkTargets(false) body only; do not skip Check/AdaptToState/ChangeAnimator.
            }
            catch(Exception e){++errors;++passed;Log("GUARD_READ_FAIL_SET (FAIL_OPEN) "+e.GetType().Name+" "+e.Message);return true;}
        }
        internal static void Tick()
        {
            if(!ready || !CompatGate.DiagnosticLoggingEnabled)return;
            try
            {
                object f=coreFlag.GetValue(null);
                if(f!=null && !sceneProbed){sceneProbed=true;Log("SCENE_PROBE_VALID frame="+Time.frameCount);}
                if(sampleFrame>=0 && sampleStage==0 && Time.frameCount>sampleFrame)
                {HookSnapshot("AFTER_CHANGE");sampleStage=1;}
                if(sampleFrame>=0 && sampleStage==1 && Time.frameCount>=sampleFrame+30)
                {HookSnapshot("AFTER_30_FRAMES");sampleStage=2;}
                if(Input.GetKeyDown(KeyCode.F7)){++goodMarks;Log("GOOD_F7 frame="+Time.frameCount+" flag.file="+(f==null?"none":NowFile(f))+" count="+goodMarks);HookSnapshot("GOOD_F7");}
                if(Input.GetKeyDown(KeyCode.F8)){++badMarks;Log("BAD_F8 frame="+Time.frameCount+" flag.file="+(f==null?"none":NowFile(f))+" count="+badMarks);HookSnapshot("BAD_F8");}
            }
            catch(Exception e){++errors;Log("OBSERVATION_ERROR "+e.GetType().Name+" "+e.Message);}
        }
        internal static void Cleanup()
        {
            ready=false;
            if(harmony!=null)try{harmony.UnpatchSelf();}catch(Exception){}
            Log("END animatorCalls="+animatorCount+" destCalls="+destCount+" checks="+checkCount+
                " suppressed="+suppressed+" passed="+passed+" snapshots="+samples+" errors="+errors);
        }
    }

// END validated ExtraIK guard integration
