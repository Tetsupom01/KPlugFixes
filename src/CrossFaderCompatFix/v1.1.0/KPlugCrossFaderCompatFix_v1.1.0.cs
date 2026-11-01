using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("tetsupom.kplug.crossfader.compatfix", "kPlug CrossFader Compat Fix", "1.1.0")]
public sealed class KPlugCrossFaderCompatFix : BaseUnityPlugin
{
    private Harmony _harmony;

    private void Awake()
    {
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
            CompatGate.Log("v1.1.0 MoveBoundary active.");
        }
        catch (Exception ex)
        {
            CompatGate.Log("[FATAL] " + ex);
            throw;
        }
    }
}

internal static class CompatGate
{
    private const string Version = "1.1.0";

    private static string _logPath;
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

        try
        {
            File.WriteAllText(_logPath, "");
        }
        catch
        {
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
                File.AppendAllText(_logPath, line + Environment.NewLine);
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

// CrossFaderCompatFix v1.0.0 source append-only HPointMove boundary.
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
        CompatGate.Log("[MOVE_BOUNDARY READY] version=1.1.0; original first-wins and stable wait unchanged");
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
    internal static void CategoryBegin() { if (ready && callbackDepth > 0) categoryDepth++; }
    internal static void CategoryEnd(System.Exception e)
    {
        if (categoryDepth > 0) categoryDepth--;
        if (!object.ReferenceEquals(e, null)) CompatGate.Log("[MOVE_BOUNDARY CATEGORY_EXCEPTION] " + e.GetType().FullName);
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
