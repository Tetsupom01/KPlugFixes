using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace KPlugPoseBridge
{
    [BepInPlugin("openai.koikatsu.kplugposebridge", "KPlug Pose Bridge", "0.2.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private static ManualLogSource Log;
        private static bool ExtraAnimationsReady = false;
        private static bool LoggedEarlyCreateList = false;

        private void Awake()
        {
            Log = Logger;

            try
            {
                harmony = new Harmony("openai.koikatsu.kplugposebridge");

                Type extraType = FindLoadedType("kPlug.CmpH.ExtraAnimManager");
                if (extraType == null)
                {
                    Log.LogError("[PoseBridge] ExtraAnimManager type not found.");
                    return;
                }

                MethodInfo extraStart = extraType.GetMethod(
                    "Start",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (extraStart == null)
                {
                    Log.LogError("[PoseBridge] ExtraAnimManager.Start not found.");
                    return;
                }

                MethodInfo afterExtraStart = typeof(Plugin).GetMethod(
                    "AfterExtraAnimStart",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (!PatchByReflection(harmony, extraStart, new HarmonyMethod(afterExtraStart)))
                {
                    Log.LogError("[PoseBridge] Could not patch ExtraAnimManager.Start.");
                    return;
                }

                Type hSceneType = FindLoadedType("HSceneProc");
                if (hSceneType == null)
                {
                    Log.LogError("[PoseBridge] HSceneProc type not found.");
                    return;
                }

                MethodInfo createList = hSceneType.GetMethod(
                    "CreateListAnimationFileName",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new Type[] { typeof(bool), typeof(int) },
                    null);

                if (createList == null)
                {
                    Log.LogError("[PoseBridge] HSceneProc.CreateListAnimationFileName(bool,int) not found.");
                    return;
                }

                MethodInfo afterCreateList = typeof(Plugin).GetMethod(
                    "AfterCreateListAnimationFileName",
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (!PatchByReflection(harmony, createList, new HarmonyMethod(afterCreateList)))
                {
                    Log.LogError("[PoseBridge] Could not patch HSceneProc.CreateListAnimationFileName.");
                    return;
                }

                Log.LogInfo(
                    "[PoseBridge] v0.2.0.1 active. mode=2 main UI will follow " +
                    "ToolAnimSelector.GetAvailablePiston() and ParserAnim.GetHAnim().");
            }
            catch (Exception ex)
            {
                Log.LogError("[PoseBridge] Awake failed: " + ex);
            }
        }

        private static void AfterExtraAnimStart()
        {
            ExtraAnimationsReady = true;
            SyncMainPistonList("ExtraAnimManager.Start");
        }

        private static void AfterCreateListAnimationFileName()
        {
            if (!ExtraAnimationsReady)
            {
                if (!LoggedEarlyCreateList)
                {
                    LoggedEarlyCreateList = true;
                    Log.LogInfo(
                        "[PoseBridge] CreateListAnimationFileName occurred before ExtraAnimManager.Start; " +
                        "leaving vanilla list untouched until extras are ready.");
                }
                return;
            }

            SyncMainPistonList("CreateListAnimationFileName");
        }

        private static void SyncMainPistonList(string reason)
        {
            try
            {
                Type coreType = FindLoadedType("kPlug.Core");
                if (coreType == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: kPlug.Core not found.");
                    return;
                }

                object hProc = GetStaticFieldValue(coreType, "hProc");
                if (hProc == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: hProc unavailable.");
                    return;
                }

                FieldInfo fLists = FindField(hProc.GetType(), "lstUseAnimInfo");
                Array arr = fLists != null ? fLists.GetValue(hProc) as Array : null;
                if (arr == null || arr.Length <= 2)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: lstUseAnimInfo unavailable.");
                    return;
                }

                IList dst = arr.GetValue(2) as IList;
                if (dst == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: lstUseAnimInfo[2] unavailable.");
                    return;
                }

                Type toolSelector = FindLoadedType("kPlug.Tools.ToolAnimSelector");
                if (toolSelector == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: ToolAnimSelector type not found.");
                    return;
                }

                MethodInfo getAvailablePiston = toolSelector.GetMethod(
                    "GetAvailablePiston",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, Type.EmptyTypes, null);

                if (getAvailablePiston == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: GetAvailablePiston not found.");
                    return;
                }

                IList canonicalList = getAvailablePiston.Invoke(null, null) as IList;
                if (canonicalList == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: GetAvailablePiston returned null/non-list.");
                    return;
                }

                Type parserAnim = FindLoadedType("kPlug.DefAndParse.ParserAnim");
                if (parserAnim == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: ParserAnim type not found.");
                    return;
                }

                MethodInfo getHAnim = parserAnim.GetMethod(
                    "GetHAnim",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string) }, null);

                if (getHAnim == null)
                {
                    Log.LogWarning("[PoseBridge] Sync skipped: ParserAnim.GetHAnim(string) not found.");
                    return;
                }

                IDictionary buttonNames = GetSelectorButtonNames();
                ArrayList resolvedCopies = new ArrayList();
                ArrayList canonicalNames = new ArrayList();
                ArrayList unresolved = new ArrayList();

                for (int i = 0; i < canonicalList.Count; i++)
                {
                    string canonical = canonicalList[i] == null
                        ? ""
                        : Convert.ToString(canonicalList[i]);

                    if (canonical.Length == 0)
                    {
                        unresolved.Add("<empty>");
                        continue;
                    }

                    object info;
                    try
                    {
                        info = getHAnim.Invoke(null, new object[] { canonical });
                    }
                    catch (TargetInvocationException tie)
                    {
                        Exception inner = tie.InnerException != null ? tie.InnerException : tie;
                        Log.LogWarning(
                            "[PoseBridge] SYNC_DEFER reason=" + reason +
                            " canonical=" + canonical +
                            " cause=" + inner.GetType().Name +
                            (string.IsNullOrEmpty(inner.Message) ? "" : ": " + inner.Message) +
                            ". Existing main list kept unchanged.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning(
                            "[PoseBridge] SYNC_DEFER reason=" + reason +
                            " canonical=" + canonical +
                            " cause=" + ex.GetType().Name +
                            (string.IsNullOrEmpty(ex.Message) ? "" : ": " + ex.Message) +
                            ". Existing main list kept unchanged.");
                        return;
                    }

                    if (info == null)
                    {
                        unresolved.Add(canonical);
                        continue;
                    }

                    object copy = ShallowClone(info);
                    if (copy == null)
                    {
                        unresolved.Add(canonical + "(clone)");
                        continue;
                    }

                    string display = GetButtonDisplayName(buttonNames, canonical);
                    if (display.Length > 0)
                    {
                        FieldInfo fName = FindField(copy.GetType(), "nameAnimation");
                        if (fName != null)
                            fName.SetValue(copy, display);
                    }

                    resolvedCopies.Add(copy);
                    canonicalNames.Add(canonical);
                }

                if (unresolved.Count > 0)
                {
                    Log.LogWarning(
                        "[PoseBridge] SYNC_ABORT reason=" + reason +
                        " requested=" + canonicalList.Count +
                        " resolved=" + resolvedCopies.Count +
                        " unresolved=" + unresolved.Count +
                        " names=[" + JoinStrings(unresolved, " | ") + "]");
                    return;
                }

                int oldCount = dst.Count;
                dst.Clear();

                for (int i = 0; i < resolvedCopies.Count; i++)
                    dst.Add(resolvedCopies[i]);

                string cats = GetCurrentCategories(hProc);

                Log.LogInfo(
                    "[PoseBridge] SYNC_OK reason=" + reason +
                    " currentCats=[" + cats + "]" +
                    " oldCount=" + oldCount +
                    " requested=" + canonicalList.Count +
                    " finalCount=" + dst.Count +
                    " canonicalUnique=" + CountUnique(canonicalNames));
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException != null ? tie.InnerException : tie;
                Log.LogError("[PoseBridge] Sync invocation failed: " + inner);
            }
            catch (Exception ex)
            {
                Log.LogError("[PoseBridge] Sync failed: " + ex);
            }
        }

        private static int CountUnique(IList values)
        {
            Hashtable seen = new Hashtable();
            for (int i = 0; i < values.Count; i++)
            {
                string s = values[i] == null ? "" : Convert.ToString(values[i]);
                if (!seen.ContainsKey(s))
                    seen.Add(s, true);
            }
            return seen.Count;
        }

        private static string JoinStrings(IList values, string sep)
        {
            string s = "";
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) s += sep;
                s += values[i] == null ? "" : Convert.ToString(values[i]);
            }
            return s;
        }

        private static string GetCurrentCategories(object hProc)
        {
            try
            {
                FieldInfo f = FindField(hProc.GetType(), "categorys");
                IList cats = f != null ? f.GetValue(hProc) as IList : null;
                if (cats == null) return "";

                string s = "";
                for (int i = 0; i < cats.Count; i++)
                {
                    if (i > 0) s += ",";
                    s += Convert.ToString(cats[i]);
                }
                return s;
            }
            catch
            {
                return "";
            }
        }

        private static string GetButtonDisplayName(IDictionary buttonNames, string canonical)
        {
            if (buttonNames != null && buttonNames.Contains(canonical))
            {
                object v = buttonNames[canonical];
                if (v != null)
                {
                    string s = Convert.ToString(v);
                    if (s.Length > 0) return s;
                }
            }

            return canonical;
        }

        private static IDictionary GetSelectorButtonNames()
        {
            try
            {
                Type selectorType = FindLoadedType("kPlug.CmpH.AnimSelector");
                if (selectorType != null)
                {
                    UnityEngine.Object selector = UnityEngine.Object.FindObjectOfType(selectorType);
                    if (selector != null)
                    {
                        FieldInfo f = FindField(selectorType, "buttonNames");
                        if (f != null)
                        {
                            IDictionary live = f.GetValue(selector) as IDictionary;
                            if (live != null && live.Count > 0)
                                return live;
                        }
                    }
                }

                Type toolType = FindLoadedType("kPlug.Tools.ToolAnimSelector");
                if (toolType != null)
                {
                    MethodInfo m = toolType.GetMethod(
                        "ButtonNames",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);

                    if (m != null)
                    {
                        IDictionary fallback = m.Invoke(null, null) as IDictionary;
                        if (fallback != null && fallback.Count > 0)
                            return fallback;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning("[PoseBridge] ButtonNames lookup failed: " + ex.Message);
            }

            return null;
        }

        private static object ShallowClone(object obj)
        {
            if (obj == null) return null;

            MethodInfo m = typeof(object).GetMethod(
                "MemberwiseClone",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return m != null ? m.Invoke(obj, null) : null;
        }

        private static object GetStaticFieldValue(Type t, string name)
        {
            FieldInfo f = FindField(t, name);
            return f != null ? f.GetValue(null) : null;
        }

        private static FieldInfo FindField(Type t, string name)
        {
            while (t != null)
            {
                FieldInfo f = t.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic);

                if (f != null) return f;
                t = t.BaseType;
            }

            return null;
        }

        private static Type FindLoadedType(string fullName)
        {
            Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                Type t = asms[i].GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static bool PatchByReflection(Harmony h, MethodBase original, HarmonyMethod postfix)
        {
            MethodInfo[] methods = typeof(Harmony).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m.Name != "Patch") continue;

                ParameterInfo[] p = m.GetParameters();
                if (p.Length < 2) continue;
                if (!typeof(MethodBase).IsAssignableFrom(p[0].ParameterType)) continue;

                object[] args = new object[p.Length];
                args[0] = original;

                bool postfixPlaced = false;
                bool usable = true;

                for (int j = 1; j < p.Length; j++)
                {
                    if (p[j].ParameterType != typeof(HarmonyMethod))
                    {
                        usable = false;
                        break;
                    }

                    string n = p[j].Name == null ? "" : p[j].Name.ToLowerInvariant();
                    if (!postfixPlaced && n.IndexOf("post") >= 0)
                    {
                        args[j] = postfix;
                        postfixPlaced = true;
                    }
                    else
                    {
                        args[j] = null;
                    }
                }

                if (!usable || !postfixPlaced) continue;

                try
                {
                    m.Invoke(h, args);
                    Log.LogInfo("[PoseBridge] Harmony.Patch overload selected: " + m);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.LogWarning(
                        "[PoseBridge] Patch overload failed: " +
                        m + " / " + ex.Message);
                }
            }

            return false;
        }
    }
}
