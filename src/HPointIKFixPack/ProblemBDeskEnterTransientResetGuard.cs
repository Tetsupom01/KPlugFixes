// Problem B RC0: isolate the TEMPORARY MotionIK.Reset invoked by OriIkTarget.Restore.
// The normal CheckBeforePoseChange/Restore/ChangeAnimator/Calc pathways all remain intact.
// CLR2 and C# 3 compatible; no direct kPlug or Assembly-CSharp compile-time dependency.
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ProblemBDeskEnterTransientResetGuard
{
    [BepInPlugin("openai.koikatsu.problemb.deskentertransientresetguard", "Problem B Desk Enter Transient Reset Guard RC0", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const BindingFlags BF = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static Type CoreType;
        private static string LogPath;
        private static Harmony Patches;
        private static int Skipped = 0;
        private static int RelevantPassed = 0;
        private static int Errors = 0;
        private static bool Ready = false;

        private void Awake()
        {
            try
            {
                LogPath = Path.Combine(Paths.PluginPath, "KPlugFixes\\ProblemBDeskEnterTransientResetGuard.log");
                File.WriteAllText(LogPath, "", new UTF8Encoding(true));
                Write("START RC0 v0.1.0. One scoped change: skip temporary constructor Reset only; rest of game methods untouched.");
                Type motion = FindType("Assembly-CSharp", "MotionIK");
                if (System.Object.ReferenceEquals(motion, null)) { Write("INIT_STOP MotionIK type missing; no patch"); return; }
                MethodInfo reset = motion.GetMethod("Reset", BF, null, Type.EmptyTypes, null);
                if (System.Object.ReferenceEquals(reset, null) || !System.Object.ReferenceEquals(reset.ReturnType, typeof(void)))
                { Write("INIT_STOP MotionIK.Reset signature mismatch; no patch"); return; }
                MethodInfo prefix = typeof(Plugin).GetMethod("ResetPrefix", BF);
                if (System.Object.ReferenceEquals(prefix, null)) { Write("INIT_STOP Prefix missing; no patch"); return; }
                Patches = new Harmony("openai.koikatsu.problemb.deskentertransientresetguard");
                Patches.Patch(reset, new HarmonyMethod(prefix), null, null, null, null);
                Write("HOOK_OK MotionIK.Reset() " + reset);
                Ready = true;
                Write("READY RC0 v0.1.0 ResetHook=True FailOpen=True; wait for DESK_ENTER_SCOPED_SKIP count=2 on one Enter; original FlowDiag remains independent.");
                Logger.LogInfo("[ProblemBDeskEnterTransientResetGuard] READY RC0 v0.1.0 ResetHook=True dedicated_log=" + LogPath);
            }
            catch (Exception e)
            {
                Errors++;
                Write("INIT_ERROR " + e);
                Logger.LogError("[ProblemBDeskEnterTransientResetGuard] INIT_ERROR " + e);
            }
        }

        private static Type FindType(string assemblyName, string fullName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (a.GetName().Name == assemblyName) return a.GetType(fullName, false); }
                catch { }
            }
            return null;
        }

        private static object Member(object owner, string name)
        {
            if (System.Object.ReferenceEquals(owner, null)) return null;
            Type t = owner as Type;
            bool isStatic = !System.Object.ReferenceEquals(t, null);
            if (!isStatic) t = owner.GetType();
            try
            {
                FieldInfo f = t.GetField(name, BF);
                if (!System.Object.ReferenceEquals(f, null)) return f.GetValue(isStatic ? null : owner);
                PropertyInfo p = t.GetProperty(name, BF);
                if (!System.Object.ReferenceEquals(p, null) && p.GetIndexParameters().Length == 0) return p.GetValue(isStatic ? null : owner, null);
            }
            catch { }
            return null;
        }
        private static object First(object owner, params string[] names)
        {
            foreach (string name in names)
            {
                object v = Member(owner, name);
                if (!System.Object.ReferenceEquals(v, null)) return v;
            }
            return null;
        }
        private static string Value(object x) { return System.Object.ReferenceEquals(x, null) ? "null" : x.ToString(); }

        // This Prefix fails OPEN (run the entire original Reset) on every uncertainty.
        private static bool ResetPrefix(object __instance)
        {
            if (!Ready) return true;
            try
            {
                // No allocation-heavy stack inspection if not in exact observed desk-caress context.
                if (System.Object.ReferenceEquals(CoreType, null)) CoreType = FindType("kPlug", "kPlug.Core");
                if (System.Object.ReferenceEquals(CoreType, null)) return true;
                object scene = Member(CoreType, "hProc");
                if (System.Object.ReferenceEquals(scene, null)) return true;
                object flag = First(scene, "flags", "flagsH", "hFlag", "flag");
                object info = First(flag, "nowAnimationInfo", "selectAnimationListInfo");
                if (Value(Member(info, "nameAnimation")) != "机バック愛撫" || Value(First(flag, "mode", "nowMode")) != "aibu") return true;
                string stack = new StackTrace(1, false).ToString();
                if (stack.IndexOf("OriIkTarget.Restore", StringComparison.Ordinal) < 0 ||
                    stack.IndexOf("CheckBeforePoseChange", StringComparison.Ordinal) < 0 ||
                    stack.IndexOf("IE_ToPistonChain", StringComparison.Ordinal) < 0 ||
                    stack.IndexOf("MotionIK..ctor", StringComparison.Ordinal) < 0)
                    return true;

                object liveIK = Member(__instance, "ik");
                object enabledValue = Member(liveIK, "enabled");
                if (System.Object.ReferenceEquals(liveIK, null) || !(enabledValue is bool) || !(bool)enabledValue)
                { RelevantPassed++; Write("DESK_ENTER_PASS frame=" + Time.frameCount + " reason=missing-or-disabled-shared-IK passes=" + RelevantPassed); return true; }
                IList current = Member(scene, "lstMotionIK") as IList;
                if (System.Object.ReferenceEquals(current, null))
                { RelevantPassed++; Write("DESK_ENTER_PASS frame=" + Time.frameCount + " reason=scene-MotionIK-list-missing passes=" + RelevantPassed); return true; }
                int matched = -1;
                for (int i = 0; i < current.Count; i++)
                {
                    if (System.Object.ReferenceEquals(liveIK, Member(current[i], "ik"))) { matched = i; break; }
                }
                if (matched < 0)
                { RelevantPassed++; Write("DESK_ENTER_PASS frame=" + Time.frameCount + " reason=not-shared-with-scene passes=" + RelevantPassed); return true; }

                // Only the temporary wrapper constructed by OriIkTarget.Restore is spared Reset.
                // Original Restore still sets saved targets, weights and destroys OriIkTarget;
                // later HSceneProc.ChangeAnimator performs its original Reset/LoadData/Calc.
                Skipped++;
                Write("DESK_ENTER_SCOPED_SKIP frame=" + Time.frameCount + " actorIndex=" + matched + " count=" + Skipped + " sharedIK.enabled=True; temp-constructor Reset skipped");
                return false;
            }
            catch (Exception e)
            {
                Errors++;
                Write("GUARD_FAIL_OPEN frame=" + Time.frameCount + " errors=" + Errors + " " + e);
                return true;
            }
        }
        private static void Write(string s)
        {
            string message = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + s + Environment.NewLine;
            try { if (!String.IsNullOrEmpty(LogPath)) File.AppendAllText(LogPath, message, new UTF8Encoding(true)); }
            catch { }
        }
        private void OnDestroy() { Write("STOP skips=" + Skipped + " relevantPasses=" + RelevantPassed + " errors=" + Errors); }
    }
}
