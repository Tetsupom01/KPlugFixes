using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace HPointIKStaleExtraIKGuard
{
    [BepInPlugin("koikatsu.hpoint.ik.staleextraikguard.exp", "HPoint IK Desk Scoped ExtraIK Guard EXPERIMENT", "0.2.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        const string ExpectedKPlug = "34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855";
        const string ExpectedAssembly = "0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5";
        const string HarmonyId = "koikatsu.hpoint.ik.staleextraikguard.exp";
        const string DeskName = "机バック愛撫";
        const string DeskFile = "kha_f_05";
        static Plugin instance;
        static Harmony harmony;
        static FieldInfo coreFlag, coreCtrl, coreCfg, freeH, cachedAnim, cachedState, extras, dark, useIk;
        static FieldInfo stateName, animInfo, female, path, file, targetName, targetMode, targetId;
        static PropertyInfo mode;
        static MethodInfo contains;
        static string logPath;
        static readonly object gate = new object();
        static bool ready;
        static int animatorCount, deskCount, checkCount, suppressed, passed, errors;
        // The scope is owned by the synchronous ChangeAnimator call, not by a global one-shot.
        public sealed class Scope
        {
            internal int id, frame, checkId;
            internal object target;
            internal bool inPostSetPlay, inCheck, used;
            internal object checkManager;
            internal string sourceAnim, oldState, newState;
        }
        [ThreadStatic] static Scope active;

        void Start()
        {
            instance = this;
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
                Type hf=NeedType(game,"HFlag");
                Type proc=NeedType(game,"HSceneProc");
                Type target=NeedType(game,"HSceneProc+AnimationListInfo");
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
                MethodInfo change=proc.GetMethod("ChangeAnimator",i,null,new Type[]{target,typeof(bool)},null);
                MethodInfo post=core.GetMethod("HActionBase_PostSetPlay",s,null,new Type[]{typeof(string),typeof(bool)},null);
                MethodInfo check=mgr.GetMethod("CheckOnStateChange",i,null,Type.EmptyTypes,null);
                MethodInfo set=mgr.GetMethod("SetIkTargets",i,null,new Type[]{typeof(bool)},null);
                if (change==null || post==null || check==null || set==null || change.ReturnType!=typeof(void) ||
                    post.ReturnType!=typeof(void) || check.ReturnType!=typeof(void) || set.ReturnType!=typeof(void))
                    throw new MissingMemberException("Exact ChangeAnimator / HActionBase_PostSetPlay / CheckOnStateChange / SetIkTargets signatures");
                if (Harmony.GetPatchInfo(check)!=null || Harmony.GetPatchInfo(set)!=null)
                    throw new InvalidOperationException("Another Harmony patch on CheckOnStateChange or SetIkTargets; refuse overlap");
                logPath=Path.Combine(dir,"HPointIKStaleExtraIKGuard.log");
                Log("=== SESSION_START "+DateTime.Now.ToString("o")+" DESK_SCOPE EXPERIMENT v0.2.0 ===");
                harmony=new Harmony(HarmonyId);
                Type self=typeof(Plugin);
                // All scope hooks are observational; only SetPrefix can skip one original method body.
                harmony.Patch(change,new HarmonyMethod(self.GetMethod("AnimatorEnter",s)),new HarmonyMethod(self.GetMethod("AnimatorExit",s)),null,null,null);
                harmony.Patch(post,new HarmonyMethod(self.GetMethod("PostEnter",s)),new HarmonyMethod(self.GetMethod("PostExit",s)),null,null,null);
                harmony.Patch(check,new HarmonyMethod(self.GetMethod("CheckEnter",s)),new HarmonyMethod(self.GetMethod("CheckExit",s)),null,null,null);
                harmony.Patch(set,new HarmonyMethod(self.GetMethod("SetPrefix",s)),null,null,null,null);
                ready=true;
                Logger.LogInfo("[HPointStaleIK] READY v0.2.0 EXP scoped to ChangeAnimator(Desk Back Caress) > Core.HActionBase_PostSetPlay > CheckOnStateChange > SetIkTargets(false); fail-open");
                Log("READY v0.2.0 destination=desk; exactly one SetIkTargets(false) per eligible ChangeAnimator invocation; NO global one-shot");
            }
            catch(Exception e)
            {
                ready=false;
                if(harmony!=null) try{harmony.UnpatchSelf();}catch(Exception un){Logger.LogError("[HPointStaleIK] UNPATCH_FAIL "+un);}
                Logger.LogError("[HPointStaleIK] PATCH_NOT_APPLIED "+e);
                Log("PATCH_NOT_APPLIED "+e);
            }
        }
        static Assembly FindAssembly(string n){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())if(String.Equals(a.GetName().Name,n,StringComparison.OrdinalIgnoreCase))return a;return null;}
        static Type NeedType(Assembly a,string n){Type t=a.GetType(n,false);if(t==null)throw new TypeLoadException(n);return t;}
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
            try{string line=DateTime.Now.ToString("HH:mm:ss.fff")+" "+m;lock(gate){if(logPath!=null)File.AppendAllText(logPath,line+Environment.NewLine);}}
            catch(Exception){} // Diagnostics never throw into the game.
        }
        // Harmony __state restores any enclosing synchronous ChangeAnimator scope.
        public static void AnimatorEnter(object __0, out Scope __state)
        {
            __state=active;
            active=null;
            if(!ready)return;
            int id=++animatorCount;
            try
            {
                if(__0==null)return;
                string n=S(targetName.GetValue(__0)), m=S(targetMode.GetValue(__0)), f=FileOfInfo(__0);
                if(n!=DeskName || m!="aibu" || f!=DeskFile)return;
                Scope scope=new Scope();scope.id=id;scope.frame=Time.frameCount;scope.target=__0;
                active=scope;++deskCount;
                Log("DESK_ANIM_ENTER id="+id+" frame="+scope.frame+" dest.name="+n+" dest.mode="+m+" dest.id="+S(targetId.GetValue(__0))+" dest.file="+f);
            }
            catch(Exception e){active=null;++errors;Log("GUARD_READ_FAIL_ANIM (FAIL_OPEN) "+e.GetType().Name+" "+e.Message);}
        }
        public static void AnimatorExit(Scope __state)
        {
            Scope x=active;
            if(x!=null)Log("DESK_ANIM_EXIT id="+x.id+" frame="+Time.frameCount+" used="+x.used);
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
                bool candidate=(Time.frameCount==x.frame && m=="aibu" && previous!=null &&
                    S(targetMode.GetValue(previous))=="sonyu" && !System.Object.ReferenceEquals(previous,x.target) &&
                    source==cache && cache!=DeskFile && oldState!="Idle" && newState=="Idle" &&
                    isFree && isExtra && !isDark && ik);
                Log("CHECK_WINDOW desk="+x.id+" check="+id+" frame="+Time.frameCount+" candidate="+candidate+
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
            if(x.inCheck)Log("CHECK_EXIT desk="+x.id+" check="+x.checkId+" used="+x.used);
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
                bool stillValid=(S(mode.GetValue(flag,null))=="aibu" && NowFile(flag)==x.sourceAnim &&
                    S(cachedAnim.GetValue(__instance))==x.sourceAnim && S(cachedState.GetValue(__instance))=="Idle" &&
                    S(stateName.GetValue(flag))=="Idle" && (bool)freeH.GetValue(null) &&
                    (bool)useIk.GetValue(cfg) && !(bool)dark.GetValue(ctl) &&
                    !System.Object.ReferenceEquals(animInfo.GetValue(flag),x.target) &&
                    S(targetMode.GetValue(animInfo.GetValue(flag)))=="sonyu");
                if(!stillValid){++passed;Log("PASS_CONDITION_CHANGED desk="+x.id+" check="+x.checkId);return true;}
                x.used=true;++suppressed;
                string msg="SUPPRESS_DESK_EXTRA desk="+x.id+" check="+x.checkId+" frame="+Time.frameCount+
                    " source="+x.sourceAnim+" oldState="+x.oldState+" newState="+x.newState+" saveOld=false count="+suppressed;
                Log(msg);
                if(instance!=null)instance.Logger.LogInfo("[HPointStaleIK] "+msg);
                return false; // One original SetIkTargets(false) body only; do not skip Check/AdaptToState/ChangeAnimator.
            }
            catch(Exception e){++errors;++passed;Log("GUARD_READ_FAIL_SET (FAIL_OPEN) "+e.GetType().Name+" "+e.Message);return true;}
        }
        void OnDestroy()
        {
            ready=false;
            if(harmony!=null)try{harmony.UnpatchSelf();}catch(Exception){}
            Log("END animatorCalls="+animatorCount+" deskCalls="+deskCount+" checks="+checkCount+
                " suppressed="+suppressed+" passed="+passed+" errors="+errors);
        }
    }
}
