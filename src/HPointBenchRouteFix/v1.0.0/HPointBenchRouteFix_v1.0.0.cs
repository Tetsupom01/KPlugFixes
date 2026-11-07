// HPoint Bench Route Fix v1.0.0 (candidate runtime; compiled DLL requires verification)
// One local IL branch adjustment: for same-category HPoint movement from insertion mode
// to explicitly chosen bench-fella category (12), use the game's existing reload route.
// Does not change CrossFader, Reset, IK, game mode, Animator, or category state directly.
// Independent BepInEx + Harmony patch for Koikatu 5.1's vanilla HSceneProc.
// Neither kPlug nor CrossFader is needed for activation; game Assembly-CSharp SHA is guarded.
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace HPointBenchRouteFixTrial
{
    [BepInPlugin("tetsupom.hpoint.benchroute.fix", "HPoint Bench Route Fix", "1.0.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string Owner = "tetsupom.hpoint.benchroute.fix";
        private const string AHash = "0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5";
        private static readonly BindingFlags Ins = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly BindingFlags Sta = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static Harmony HarmonyInstance;
        private static StreamWriter Writer;
        private static string LogFile;
        private static Type ProcType, PointType, FlagType, InfoType;
        private static FieldInfo ProcFlags, ProcCategories, CurrentInfo, InfoName, InfoMode;
        private static PropertyInfo CurrentMode;
        private static MethodInfo PointCategories, CategoryMethod;
        private static bool Ready, Fatal;
        private static int LastAttempt=-10000, Forced, Errors, CallCount;
        private static int InitFrame;
        private static string LastError;

        private void Awake()
        {
            InitFrame = Time.frameCount;
            LogFile = Path.Combine(Paths.BepInExRootPath, "HPointBenchRouteFix.log");
            try
            {
                Writer = new StreamWriter(new FileStream(LogFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                Write("SESSION_START version=1.0.0 independentCandidate=True target=Koikatu5.1");
            }
            catch (Exception e)
            {
                Fatal = true;
                Logger.LogError("[BenchRouteFix] LOG_INIT_FAILED " + e);
            }
        }
        private void Start() { TryInitialize(); }
        private void Update()
        {
            if (Fatal) return;
            if (!Ready && Time.frameCount - LastAttempt >= 20)
            {
                TryInitialize();
                if (Time.frameCount - InitFrame > 600 && !Ready)
                {
                    Write("PATCH_NOT_APPLIED timeout locating vanilla HSceneProc types");
                    Flush(); Fatal=true;
                }
            }
        }
        private void OnDestroy()
        {
            Write("END ready=" + Ready + " forced=" + Forced + " checks="+CallCount + " errors="+Errors);
            Flush();
            if (HarmonyInstance != null) { try { HarmonyInstance.UnpatchSelf(); } catch { } }
            if (Writer != null) { try { Writer.Dispose(); } catch { } }
        }
        private static void Write(string s)
        {
            if (Writer == null) return;
            try { Writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " frame=" + Time.frameCount + " " + s); }
            catch { Fatal=true; }
        }
        private static void Flush() { if (Writer != null) try { Writer.Flush(); } catch { Fatal=true; } }
        private static Type NeedType(string n)
        { Type t=AccessTools.TypeByName(n); if(t==null) throw new TypeLoadException(n); return t; }
        private static FieldInfo NeedField(Type t,string n)
        { FieldInfo f=t.GetField(n,Ins|Sta); if(f==null)throw new MissingFieldException(t.FullName,n); return f; }
        private static MethodInfo NeedMethod(Type t,string n,Type[] args)
        { MethodInfo m=t.GetMethod(n,Ins|Sta,null,args,null);if(m==null)throw new MissingMethodException(t.FullName,n);return m; }
        private static string Hash(string p)
        {
            if(!File.Exists(p)) throw new FileNotFoundException("Baseline missing",p);
            using(FileStream f=File.Open(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            using(SHA256 h=SHA256.Create()) {return BitConverter.ToString(h.ComputeHash(f)).Replace("-", "");}
        }
        private static void RequireHash(string p,string expected)
        {if(!String.Equals(Hash(p),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("BASELINE_SHA_MISMATCH path="+p);}
        private static bool OwnerPresent(MethodBase method,string owner)
        {
            object info=Harmony.GetPatchInfo(method);
            if(info==null)return false;
            PropertyInfo prop=info.GetType().GetProperty("Owners",Ins);
            if(prop==null)throw new MissingMemberException("Harmony Owners");
            IEnumerable vals=prop.GetValue(info,null) as IEnumerable;
            if(vals==null)throw new InvalidOperationException("Harmony Owners unreadable");
            foreach(object v in vals)if(String.Equals(v as string,owner,StringComparison.Ordinal))return true;
            return false;
        }
        private static void TryInitialize()
        {
            LastAttempt=Time.frameCount;
            if(Ready||Fatal)return;
            try
            {
                // Only vanilla game types are needed. The game assembly may initialize
                // after BepInEx Awake, so defer until the types are available.
                Type flagType=AccessTools.TypeByName("HFlag");
                Type infoType=AccessTools.TypeByName("HSceneProc+AnimationListInfo");
                Type procType=AccessTools.TypeByName("HSceneProc");
                Type pointType=AccessTools.TypeByName("H.HPointData");
                if(flagType==null||infoType==null||procType==null||pointType==null)return;
                string root=Paths.BepInExRootPath;
                RequireHash(Path.Combine(root,"../Koikatu_Data/Managed/Assembly-CSharp.dll"),AHash);
                InfoType=infoType;ProcType=procType;PointType=pointType;FlagType=flagType;
                ProcFlags=NeedField(ProcType,"flags");ProcCategories=NeedField(ProcType,"categorys");
                CurrentInfo=NeedField(FlagType,"nowAnimationInfo");
                InfoName=NeedField(InfoType,"nameAnimation");InfoMode=NeedField(InfoType,"mode");
                CurrentMode=FlagType.GetProperty("mode",Ins);
                if(CurrentMode==null)throw new MissingMemberException("HFlag.mode");
                PointCategories=NeedMethod(PointType,"get_category",Type.EmptyTypes);
                CategoryMethod=NeedMethod(ProcType,"ChangeCategory",new Type[]{PointType,typeof(int)});
                if(CategoryMethod.ReturnType!=typeof(void))throw new InvalidOperationException("ChangeCategory signature mismatch");
                HarmonyInstance=new Harmony(Owner);
                MethodInfo patch=typeof(Plugin).GetMethod("Transpiler",Sta);
                if(patch==null)throw new MissingMethodException("Transpiler");
                HarmonyInstance.Patch(CategoryMethod,null,null,new HarmonyMethod(patch),null,null);
                if(!OwnerPresent(CategoryMethod,Owner))throw new InvalidOperationException("Transpiler patch ownership not confirmed");
                Ready=true;
                Write("READY version=1.0.0 method=HSceneProc.ChangeCategory matched=1 patchOwner="+Owner);
                Flush();
            }
            catch(Exception ex)
            {
                ++Errors;LastError=ex.GetType().Name+": "+ex.Message;
                Write("PATCH_NOT_APPLIED "+LastError);Flush();
                if(HarmonyInstance!=null)try{HarmonyInstance.UnpatchSelf();}catch{}
                Fatal=true;
            }
        }
        private static bool IsCall(CodeInstruction code,string declaringType,string method)
        {
            if(code.opcode!=OpCodes.Call&&code.opcode!=OpCodes.Callvirt)return false;
            MethodBase m=code.operand as MethodBase;
            return m!=null && m.DeclaringType!=null && m.DeclaringType.FullName==declaringType && m.Name==method;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> list=new List<CodeInstruction>(instructions);
            int at=-1,count=0;
            for(int i=1;i+1<list.Count;i++)
            {
                if(IsCall(list[i-1],"Illusion.Extensions.IEnumerableExtensions","SymmetricExcept") &&
                   IsCall(list[i],"System.Linq.Enumerable","Any") &&
                   (list[i+1].opcode==OpCodes.Brfalse || list[i+1].opcode==OpCodes.Brfalse_S))
                { at=i;count++; }
            }
            if(count!=1)throw new InvalidOperationException("ChangeCategory IL pattern mismatch; matched="+count);
            MethodInfo fix=typeof(Plugin).GetMethod("ShouldReload",Sta);
            if(fix==null)throw new MissingMethodException("ShouldReload");
            // The original Any<int>() result remains on the evaluation stack.
            // Add (scene, point, menu category) and replace the branch test result only.
            list.InsertRange(at+1,new CodeInstruction[]{
               new CodeInstruction(OpCodes.Ldarg_0),
               new CodeInstruction(OpCodes.Ldarg_1),
               new CodeInstruction(OpCodes.Ldarg_2),
               new CodeInstruction(OpCodes.Call,fix)});
            return list;
        }
        private static bool Contains(IList arr,int value)
        {
            if(arr==null)return false;
            for(int i=0;i<arr.Count;i++)if(Convert.ToInt32(arr[i])==value)return true;
            return false;
        }
        public static bool ShouldReload(bool vanillaDifferent, object scene, object point,int selected)
        {
            if(vanillaDifferent)return true; // Original game choice unchanged.
            ++CallCount;
            try
            {
                if(selected!=12||scene==null||point==null)return false;
                object flag=ProcFlags.GetValue(scene);
                if(flag==null)return false;
                int mode=Convert.ToInt32(CurrentMode.GetValue(flag,null));
                if(mode!=2)return false; // Only actual insertion source state, not aibu/houshi.
                object current=CurrentInfo.GetValue(flag);
                if(current==null||!String.Equals(Convert.ToString(InfoMode.GetValue(current)),"sonyu",StringComparison.Ordinal))return false;
                if(String.Equals(Convert.ToString(InfoName.GetValue(current)),"ベンチフェラ",StringComparison.Ordinal))return false;
                IList oldCats=ProcCategories.GetValue(scene) as IList;
                IList targetCats=PointCategories.Invoke(point,null) as IList;
                if(oldCats==null||targetCats==null||oldCats.Count!=2||targetCats.Count!=2)return false;
                if(!Contains(oldCats,2)||!Contains(oldCats,12)||!Contains(targetCats,2)||!Contains(targetCats,12))return false;
                UnityEngine.Object unityPoint=point as UnityEngine.Object;
                string pointName=unityPoint==null ? "null" : unityPoint.name;
                if(!pointName.StartsWith("isu_",StringComparison.Ordinal))return false;
                // This single guarded override takes the ORIGINAL ChangeCategory reload route.
                ++Forced;
                Write("FORCE_RELOAD target=bench-fella selected=12 point="+pointName+
                      " current="+Convert.ToString(InfoName.GetValue(current))+
                      " mode=sonyu cats=[2,12] count="+Forced);
                Flush();
                return true;
            }
            catch(Exception e)
            {
                ++Errors;
                Write("FAIL_OPEN guard="+e.GetType().FullName+" "+e.Message);Flush();
                return vanillaDifferent;
            }
        }
    }
}
