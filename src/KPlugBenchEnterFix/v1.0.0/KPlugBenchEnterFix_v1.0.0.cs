using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using HarmonyLib;
using UnityEngine;

// Official v1.0.0 candidate. Runtime logic retained from game-tested v0.7.5.
// Historical refinement from v0.7.1: kPlug native Chain can call ShowKokan BEFORE
// ToPistonChain, so suppress that single clothing side effect in bench Idle Enter.
// Preserve native Action, Chain sound, W/S/O cum gate and oral-choice lifecycle.
// Narrow one-shot deferred native Idle-start request: a first Enter request can be
// consumed while the native voice is still playing. Queue that same request and
// re-issue only HFlag.click=speedup at a safe native HHoushi.Proc entry when the
// native female voice state has changed to breath. The ORIGINAL Proc then owns
// WLoop transition, UI, voice, IK and gauge handling; no animation is forced.
// Keep the proven v0.7.2 input and clothes gates, all other operations untouched.
[BepInPlugin("tetsupom.kplug.benchenterfix", "KPlug Bench Enter Fix", "1.0.0")]
public sealed class KPlugBenchEnterFix : BaseUnityPlugin
{
    const string Owner = "tetsupom.kplug.benchenterfix";
    const string KplugSha = "34C13976108DB0A18A7AD6B7CFDA5517B4A9A83D85C9A909820144264C743855";
    const string GameSha = "0038281CAF8DF48A7903C55DC389642EEEB3F2A9114BD9D68AC11C8AC0396BC5";
    const string CrossFaderSha = "2392CCAFE5663EFC4FD80F8F72E9741796DE1DDD85D92F9A0E2C0F6C3E45ACFC";
    const string BenchFixSha = "170757DA0A4880CF2F8CF23C7E206C70BC6F9487F0051AAB470353D57A3408BD";
    const BindingFlags St = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags It = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static Harmony harmony;
    static StreamWriter writer;
    static bool ready, stopped;
    static int nextInit = 0, intercepts = 0, lastInterceptFrame = -10000, observedUntil = -1;
    static string lastObservedState = "";
    static bool sawWLoop = false, sawInsertion = false;
    static FieldInfo coreFlag, coreCtrl, coreCombo, coreHAnim;
    static FieldInfo flagState, flagMode, flagInfo, flagClick, flagGauge, infoName;
    static FieldInfo ctrlPosMod, ctrlFreeze, comboInPos, comboLaunch;
    static FieldInfo cfGateActive;
    static MethodInfo chain, piston, action, showKokan, nativeHoushiProc, getAnimatorState;
    static FieldInfo flagVoiceWait, houshiFemale, houshiVoice, houshiAutoStart, voiceNowVoices;
    static int startProbeFrame = -1;
    static string lastStartProbe = "";
    static int startProbeRecords = 0;
    // One user key-down -> at most one deferred native click, no extra native Chain sound.
    static bool pendingIdleStart, idleStartDispatched;
    static object pendingIdleStartFlag;
    static int pendingIdleStartFrame=-1;
    static object nativeSpeedupClick, nativeNoneClick;
    static int deferredIdleStartCount, cancelledIdleStartCount, droppedRepeatStartCount;
    [ThreadStatic] static bool insideChain;
    [ThreadStatic] static int chainScopeFrame;
    static int chainKeySeen, blockedActionAfterCum, blockedChainAfterCum, skippedIdleClothes;
    static bool benchWasActive;
    static string lastBenchSnapshotState="";
    // A transition from another motion may leave insertion coroutine flags set
    // even after the native game has reached bench Idle. Track this specific
    // entry, do not mutate or stop either coroutine.
    static bool benchEnteredWithCarriedPistonFlags;
    static object benchEntryFlag, benchEntryCombo;
    static int benchEntryFrame=-1;
    // Decision is cached for one Unity frame: Action and Chain see the SAME verdict.
    enum EnterVerdict { Pass, Block, AcceptStart, AcceptCum, AcceptOral }
    static int verdictFrame = -1;
    static EnterVerdict verdict = EnterVerdict.Pass;
    static bool cumAccepted, oralAccepted, cumStateObserved;
    static string lastGuardState = "";
    static int acceptedCumCount, acceptedOralCount, blockedCount;
    static object lastGuardFlag;

    void Awake()
    {
        try {
            string p=Path.Combine(Paths.BepInExRootPath,"KPlugBenchEnterFix.log");
            writer=new StreamWriter(p,false,new UTF8Encoding(false));writer.AutoFlush=true;
            Log("SESSION_START version=1.0.0 base=v0.7.5 identityOnly=True noFlagReset=True noCoroutineStop=True");
        } catch(Exception e) { stopped=true; Logger.LogError("KPlugBenchEnterFix logger: "+e.Message); }
    }
    void Start(){TryInit();}
    void Update()
    {
        if(stopped)return;
        if(!ready && Time.frameCount>=nextInit){nextInit=Time.frameCount+30;TryInit();}
        if(!ready)return;
        try { RefreshCycleState(); TraceBenchTransitions(); }
        catch(Exception e){Log("CYCLE_MONITOR_ERROR "+e.GetType().Name+" "+e.Message);}
        if(Input.GetKeyDown(KeyCode.F7)) Log("VISUAL_F7 "+Snapshot());
        if(Input.GetKeyDown(KeyCode.F8)) Log("VISUAL_F8 "+Snapshot());
        if(observedUntil>=0 && Time.frameCount<=observedUntil)
        {
            string current=Snapshot();
            if(!String.Equals(current,lastObservedState,StringComparison.Ordinal))
            {
                Log("POST_BLOCK_STATE deltaFrame="+(Time.frameCount-lastInterceptFrame)+" "+current);
                lastObservedState=current;
            }
            string state=GetString(flagState,GetObj(coreFlag,null));
            if(String.Equals(state,"WLoop",StringComparison.Ordinal))sawWLoop=true;
            string mode=GetString(flagMode,GetObj(coreFlag,null));
            if(String.Equals(mode,"sonyu",StringComparison.Ordinal))sawInsertion=true;
            if(Time.frameCount==observedUntil){
                Log("POST_BLOCK_RESULT wloopSeen="+sawWLoop+" insertionModeSeen="+sawInsertion+" NOT_VISUAL_PROOF=True");
                observedUntil=-1;
            }
        }
    }
    void OnDestroy()
    {
        Log("END deferredIdleStart="+deferredIdleStartCount+" cancelledIdleStart="+cancelledIdleStartCount+
            " blockedRepeatStart="+droppedRepeatStartCount+" skippedIdleClothes="+skippedIdleClothes+" acceptedCum="+acceptedCumCount+" acceptedOral="+acceptedOralCount+" blockedEnter="+blockedCount+" blockedActionAfterCum="+blockedActionAfterCum+" blockedChainAfterCum="+blockedChainAfterCum+" ready="+ready+" blocks="+intercepts+" chainKeySeen="+chainKeySeen+" lastFrame="+lastInterceptFrame);
        if(harmony!=null)try{harmony.UnpatchSelf();}catch(Exception e){Log("UNPATCH_ERROR "+e.GetType().Name);}
        if(writer!=null)try{writer.Dispose();}catch{}
    }
    static void Log(string s){if(writer!=null)try{writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")+" frame="+Time.frameCount+" "+s);}catch{}}
    static string Hash(string p){using(FileStream f=File.Open(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)).Replace("-","");}
    static void RequireHash(string p,string sha){if(!File.Exists(p)||!String.Equals(Hash(p),sha,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("BASELINE_SHA_MISMATCH "+p);}
    static Assembly Find(string n){foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies())if(String.Equals(a.GetName().Name,n,StringComparison.OrdinalIgnoreCase))return a;return null;}
    static Type T(Assembly a,string n){Type t=a.GetType(n,false);if(object.ReferenceEquals(t,null))throw new MissingMemberException(n);return t;}
    static FieldInfo F(Type t,string n,BindingFlags b){FieldInfo f=t.GetField(n,b);if(f==null)throw new MissingFieldException(t.FullName,n);return f;}
    static object GetObj(FieldInfo f,object o){return f.GetValue(o);}
    static string GetString(FieldInfo f,object o){object x=GetObj(f,o);return x==null?"null":x.ToString();}
    static string Snapshot()
    {
        object f=GetObj(coreFlag,null), c=GetObj(coreCtrl,null), x=GetObj(coreCombo,null);
        if(f==null||c==null||x==null)return "NO_SCENE";
        object info=GetObj(flagInfo,f);
        return "anim="+(info==null?"null":GetString(infoName,info))+" hAnim="+GetString(coreHAnim,null)+
             " mode="+GetString(flagMode,f)+" posMod="+GetString(ctrlPosMod,c)+
             " state="+GetString(flagState,f)+" gaugeMale="+GetString(flagGauge,f)+" click="+GetString(flagClick,f)+" voiceWait="+GetString(flagVoiceWait,f)+
             " freeze="+GetString(ctrlFreeze,c)+" inPosChain="+GetString(comboInPos,x)+
             " inLaunchMov="+GetString(comboLaunch,x)+" CFgate="+GetString(cfGateActive,null);
    }
    static bool Eligible()
    {
        if(!Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))return false;
        // Combined mouse/control actions must keep the original kPlug route.
        if(Input.GetMouseButton(0)||Input.GetMouseButton(1)||Input.GetMouseButton(2))return false;
        if(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||
           Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt)||
           Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))return false;
        object f=GetObj(coreFlag,null),c=GetObj(coreCtrl,null),combo=GetObj(coreCombo,null);
        if(f==null||c==null||combo==null)return false;
        object info=GetObj(flagInfo,f);
        if(info==null)return false;
        string state = GetString(flagState,f);
        bool beforeEjaculation = String.Equals(state,"Idle",StringComparison.Ordinal) ||
                                 String.Equals(state,"WLoop",StringComparison.Ordinal) ||
                                 String.Equals(state,"SLoop",StringComparison.Ordinal) ||
                                 String.Equals(state,"OLoop",StringComparison.Ordinal);
        return String.Equals(GetString(infoName,info),"ベンチフェラ",StringComparison.Ordinal) &&
               String.Equals(GetString(coreHAnim,null),"S_Sit_SideBlowJob",StringComparison.Ordinal) &&
               String.Equals(GetString(flagMode,f),"houshi",StringComparison.Ordinal) &&
               String.Equals(GetString(ctrlPosMod,c),"Service",StringComparison.Ordinal) &&
               beforeEjaculation &&
               Convert.ToSingle(GetObj(ctrlFreeze,c))==0f &&
               !Convert.ToBoolean(GetObj(comboInPos,combo)) &&
               !Convert.ToBoolean(GetObj(comboLaunch,combo)) &&
               !Convert.ToBoolean(GetObj(cfGateActive,null));
    }
    static bool PureKeypadEnter()
    {
        return Input.GetKeyDown(KeyCode.KeypadEnter) && !Input.GetKeyDown(KeyCode.Return) &&
               !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2) &&
               !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl) &&
               !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt) &&
               !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift);
    }
    static bool NativeCumLoop(string state)
    {
        return String.Equals(state,"WLoop",StringComparison.Ordinal) ||
               String.Equals(state,"SLoop",StringComparison.Ordinal) ||
               String.Equals(state,"OLoop",StringComparison.Ordinal);
    }
    static bool DuringCumOrTransition(string state)
    {
        return String.Equals(state,"IN_Start",StringComparison.Ordinal) ||
               String.Equals(state,"IN_Loop",StringComparison.Ordinal) ||
               String.Equals(state,"M_OUT_Start",StringComparison.Ordinal) ||
               String.Equals(state,"M_OUT_Loop",StringComparison.Ordinal) ||
               String.Equals(state,"Oral_Idle_IN",StringComparison.Ordinal) ||
               String.Equals(state,"OUT_A",StringComparison.Ordinal) ||
               String.Equals(state,"OUT_Start",StringComparison.Ordinal) ||
               String.Equals(state,"OUT_Loop",StringComparison.Ordinal);
    }
    static bool IsBenchContext(object f, object c)
    {
        if(object.ReferenceEquals(f,null)||object.ReferenceEquals(c,null))return false;
        object info=GetObj(flagInfo,f);
        if(object.ReferenceEquals(info,null))return false;
        return String.Equals(GetString(infoName,info),"ベンチフェラ",StringComparison.Ordinal) &&
               String.Equals(GetString(coreHAnim,null),"S_Sit_SideBlowJob",StringComparison.Ordinal) &&
               String.Equals(GetString(flagMode,f),"houshi",StringComparison.Ordinal) &&
               String.Equals(GetString(ctrlPosMod,c),"Service",StringComparison.Ordinal);
    }
    static void CancelPendingIdleStart(string reason)
    {
        if(pendingIdleStart) {
            cancelledIdleStartCount++;
            Log("IDLE_START_PENDING_CANCEL #"+cancelledIdleStartCount+" reason="+reason+
                " dispatched="+idleStartDispatched);
        }
        pendingIdleStart=false;
        idleStartDispatched=false;
        pendingIdleStartFlag=null;
        pendingIdleStartFrame=-1;
    }
    static void RefreshCycleState()
    {
        object f=GetObj(coreFlag,null),c=GetObj(coreCtrl,null);
        if(!IsBenchContext(f,c))
        {
            CancelPendingIdleStart("bench_context_exit");
            if(cumAccepted||oralAccepted)Log("CYCLE_RESET scene_exit state="+lastGuardState);
            cumAccepted=false;oralAccepted=false;cumStateObserved=false;lastGuardState="";lastGuardFlag=null;
            return;
        }
        if(!object.ReferenceEquals(lastGuardFlag,f))
        {
            CancelPendingIdleStart("HFlag_object_changed");
            cumAccepted=false;oralAccepted=false;cumStateObserved=false;lastGuardFlag=f;
        }
        string state=GetString(flagState,f);
        if(pendingIdleStart && !String.Equals(state,"Idle",StringComparison.Ordinal))
            CancelPendingIdleStart("left_Idle_to_"+state);
        if(pendingIdleStart && Time.frameCount-pendingIdleStartFrame>480)
            CancelPendingIdleStart("timeout_480_frames");
        if(DuringCumOrTransition(state))cumStateObserved=true;
        // Re-arm when a completed cycle returns to the normal pre-cum loop with depleted gauge.
        if(cumStateObserved && NativeCumLoop(state) &&
           Convert.ToSingle(GetObj(flagGauge,f)) <= 70f)
        {
            if(cumAccepted||oralAccepted)Log("CYCLE_RESET next_native_cycle state="+state);
            cumAccepted=false;oralAccepted=false;cumStateObserved=false;
        }
        // Keep oralAccepted through OUT_A / transition until scene resets.
        lastGuardState=state;
    }
    // Safely scoped exception: the game is already back in bench Idle,
    // but insertion-chain flags were still set when this bench context began.
    // This is ONLY an input admission rule. The flags are never rewritten.
    static bool BenchIdleCarriedFlags(object f, object c, object combo)
    {
        if(!benchWasActive || !benchEnteredWithCarriedPistonFlags ||
           Time.frameCount<=benchEntryFrame ||
           !object.ReferenceEquals(f,benchEntryFlag) ||
           !object.ReferenceEquals(combo,benchEntryCombo) ||
           !IsBenchContext(f,c) ||
           !String.Equals(GetString(flagState,f),"Idle",StringComparison.Ordinal))return false;
        return Convert.ToBoolean(GetObj(comboInPos,combo)) &&
               Convert.ToBoolean(GetObj(comboLaunch,combo));
    }
    // Observe bench entries/animation-state changes without writing any game state.
    // This helps distinguish an HPoint route/state carryover from a subsequent Enter decision.
    static void TraceBenchTransitions()
    {
        object f=GetObj(coreFlag,null), c=GetObj(coreCtrl,null);
        if(!IsBenchContext(f,c)) {
            if(benchWasActive)Log("BENCH_CONTEXT_EXIT previousState="+lastBenchSnapshotState);
            benchWasActive=false;lastBenchSnapshotState="";
            benchEnteredWithCarriedPistonFlags=false;
            benchEntryFlag=null;benchEntryCombo=null;benchEntryFrame=-1;
            return;
        }
        string current=GetString(flagState,f);
        if(!benchWasActive){
            object combo=GetObj(coreCombo,null);
            benchEntryFlag=f;benchEntryCombo=combo;benchEntryFrame=Time.frameCount;
            benchEnteredWithCarriedPistonFlags=combo!=null &&
                String.Equals(current,"Idle",StringComparison.Ordinal) &&
                Convert.ToBoolean(GetObj(comboInPos,combo)) &&
                Convert.ToBoolean(GetObj(comboLaunch,combo));
            if(benchEnteredWithCarriedPistonFlags)
                Log("BENCH_IDLE_CARRIED_PISTON_FLAGS_AT_ENTRY "+Snapshot());
        }
        if(!benchWasActive || !String.Equals(current,lastBenchSnapshotState,StringComparison.Ordinal)) {
            Log((benchWasActive?"BENCH_STATE_CHANGE":"BENCH_CONTEXT_ENTER")+" "+Snapshot());
            lastBenchSnapshotState=current;
            benchWasActive=true;
            if(startProbeFrame>=0 && String.Equals(current,"WLoop",StringComparison.Ordinal)) {
                Log("IDLE_START_PROBE_WLOOP deltaFrame="+(Time.frameCount-startProbeFrame)+" "+Snapshot());
                startProbeFrame=-1;lastStartProbe="";
            }
        }
    }
    // Snapshot + gated one-shot native click before ORIGINAL HHoushi.Proc().
    // Void prefix NEVER suppresses the original Proc, animations, or coroutines.
    private static void NativeIdleStartProbePrefix(object __instance)
    {
        if(!ready||stopped||startProbeFrame<0)return;
        int frame=Time.frameCount;
        if(frame>startProbeFrame+480){
            Log("IDLE_START_PROBE_END reason=timeout deltaFrame="+(frame-startProbeFrame));
            startProbeFrame=-1;lastStartProbe="";return;
        }
        try {
            object f=GetObj(coreFlag,null), c=GetObj(coreCtrl,null);
            if(!IsBenchContext(f,c))return;
            object female=GetObj(houshiFemale,__instance);
            object voice=GetObj(houshiVoice,__instance);
            object entries=voice==null?null:GetObj(voiceNowVoices,voice);
            Array voices=entries as Array;
            string voiceState="missing";
            if(voices!=null && voices.Length>0 && voices.GetValue(0)!=null){
                object first=voices.GetValue(0);
                FieldInfo field=first.GetType().GetField("state",It);
                if(field!=null)voiceState=Convert.ToString(field.GetValue(first));
            }
            string animatorIdle="missing";
            if(female!=null){
                object anim=getAnimatorState.Invoke(female,new object[]{0});
                if(anim is AnimatorStateInfo)
                    animatorIdle=((AnimatorStateInfo)anim).IsName("Idle").ToString();
            }
            // Only the first requested Idle start is held. The native HHoushi.Proc
            // must still make the transition. Never inject during a voice, other
            // UI action, movement, or any other motion/scene.
            if(pendingIdleStart && !idleStartDispatched &&
               object.ReferenceEquals(pendingIdleStartFlag,f) &&
               String.Equals(GetString(flagState,f),"Idle",StringComparison.Ordinal) &&
               String.Equals(voiceState,"breath",StringComparison.Ordinal) &&
               String.Equals(animatorIdle,"True",StringComparison.Ordinal) &&
               !Convert.ToBoolean(GetObj(flagVoiceWait,f)) &&
               !Convert.ToBoolean(GetObj(houshiAutoStart,__instance)) &&
               object.Equals(GetObj(flagClick,f),nativeNoneClick))
            {
                object combo=GetObj(coreCombo,null);
                bool carried=combo!=null && BenchIdleCarriedFlags(f,c,combo);
                bool free=combo!=null && Convert.ToSingle(GetObj(ctrlFreeze,c))==0f &&
                    ((!Convert.ToBoolean(GetObj(comboInPos,combo)) &&
                      !Convert.ToBoolean(GetObj(comboLaunch,combo))) || carried) &&
                    !Convert.ToBoolean(GetObj(cfGateActive,null));
                if(free) {
                    // One narrowly scoped write. No Action()/Chain() replay, so
                    // no extra keyboard sound or ShowKokan()/ToPistonChain() call.
                    flagClick.SetValue(f,nativeSpeedupClick);
                    idleStartDispatched=true;
                    deferredIdleStartCount++;
                    Log("IDLE_START_DEFERRED_NATIVE_CLICK #"+deferredIdleStartCount+
                        " originalFrame="+pendingIdleStartFrame+
                        " delayFrames="+(frame-pendingIdleStartFrame)+
                        " voice0="+voiceState+" animatorIdle="+animatorIdle+
                        " click=speedup nativeProcNext=True");
                }
            }
            string sample="state="+GetString(flagState,f)+" click="+GetString(flagClick,f)+
                " voiceWait="+GetString(flagVoiceWait,f)+" voice0="+voiceState+
                " femaleAnimatorIdle="+animatorIdle+
                " autoStart="+GetString(houshiAutoStart,__instance);
            if(sample!=lastStartProbe || (frame-startProbeFrame)%30==0 || frame==startProbeFrame){
                Log("IDLE_START_PROBE_PROC deltaFrame="+(frame-startProbeFrame)+" "+sample);
                lastStartProbe=sample;startProbeRecords++;
            }
        } catch(Exception e){
            Log("IDLE_START_PROBE_READ_ERROR "+e.GetType().Name+" "+e.Message);
            startProbeFrame=-1;
        }
    }
    // Only guard the original clothing call that precedes ToPistonChain in native Chain().
    // This does not stop the native input Action or the native Chain sound/start logic.
    private static bool IdleClothesPrefix()
    {
        if(!ready||stopped||!insideChain||chainScopeFrame!=Time.frameCount ||
           verdictFrame!=Time.frameCount||verdict!=EnterVerdict.AcceptStart)return true;
        try {
            object f=GetObj(coreFlag,null),c=GetObj(coreCtrl,null);
            if(!PureKeypadEnter() || !IsBenchContext(f,c) ||
               !String.Equals(GetString(flagState,f),"Idle",StringComparison.Ordinal))return true;
            skippedIdleClothes++;
            Log("SKIP_IDLE_SHOW_KOKAN #"+skippedIdleClothes+" "+Snapshot());
            return false;
        } catch(Exception e) {
            Log("CLOTHES_GATE_ERROR_FAIL_OPEN "+e.GetType().Name+" "+e.Message);
            return true;
        }
    }
    // Evaluate exactly once per frame, regardless of Action/Chain hook invocation order.
    static EnterVerdict KeypadEnterVerdict()
    {
        if(!ready || stopped || !PureKeypadEnter())return EnterVerdict.Pass;
        int frame=Time.frameCount;
        if(verdictFrame==frame)return verdict;
        verdictFrame=frame;verdict=EnterVerdict.Pass;
        object f=GetObj(coreFlag,null),c=GetObj(coreCtrl,null),combo=GetObj(coreCombo,null);
        if(!IsBenchContext(f,c) || object.ReferenceEquals(combo,null))return verdict;
        string state=GetString(flagState,f);
        float gauge=Convert.ToSingle(GetObj(flagGauge,f));
        bool carriedIdle=BenchIdleCarriedFlags(f,c,combo);
        bool free=Convert.ToSingle(GetObj(ctrlFreeze,c))==0f &&
             ((!Convert.ToBoolean(GetObj(comboInPos,combo)) &&
               !Convert.ToBoolean(GetObj(comboLaunch,combo))) || carriedIdle) &&
             !Convert.ToBoolean(GetObj(cfGateActive,null));
        if(String.Equals(state,"Oral_Idle",StringComparison.Ordinal))
        {
            if(!oralAccepted && free) {oralAccepted=true;acceptedOralCount++;verdict=EnterVerdict.AcceptOral;}
            else verdict=EnterVerdict.Block;
        }
        else if(DuringCumOrTransition(state))verdict=EnterVerdict.Block;
        else if(String.Equals(state,"Idle",StringComparison.Ordinal))
        {
            // Preserve native Action+Chain (including one sound) for the first
            // key-down. If native Proc is voice-busy it consumes click=speedup;
            // our HHoushi.Proc prefix reissues that click ONCE when voice0=breath.
            if(pendingIdleStart) {
                verdict=EnterVerdict.Block;
                droppedRepeatStartCount++;
                Log("IDLE_START_REPEAT_BLOCKED #"+droppedRepeatStartCount+
                    " queuedFrame="+pendingIdleStartFrame);
            }
            else if(!cumAccepted && !oralAccepted && free){
                verdict=EnterVerdict.AcceptStart;
                pendingIdleStart=true;idleStartDispatched=false;
                pendingIdleStartFlag=f;pendingIdleStartFrame=frame;
                startProbeFrame=frame;lastStartProbe="";
                Log("IDLE_START_PENDING_QUEUED frame="+frame+" carriedIdle="+carriedIdle+" "+Snapshot());
            }
            else verdict=EnterVerdict.Block;
        }
        else if(NativeCumLoop(state))
        {
            // Emission requests remain strictly over 70; all lower-gauge loop presses are silent.
            if(gauge > 70f && !cumAccepted && free)
            {cumAccepted=true;acceptedCumCount++;verdict=EnterVerdict.AcceptCum;}
            else verdict=EnterVerdict.Block;
        }
        if(verdict==EnterVerdict.Block)blockedCount++;
        Log("KEYPAD_ENTER_DECISION verdict="+verdict+" state="+state+
            " gauge="+gauge+" cumLatched="+cumAccepted+" oralLatched="+oralAccepted+
            " free="+free+" carriedIdle="+carriedIdle+" "+Snapshot());
        return verdict;
    }
    // Same decision gates both native input methods; rejected input cannot play Chain sound.
    private static bool ActionReentryPrefix()
    {
        try {
            EnterVerdict d=KeypadEnterVerdict();
            if(d!=EnterVerdict.Block)return true;
            blockedActionAfterCum++;
            Log("BLOCK_KEYPAD_ENTER_ACTION #"+blockedActionAfterCum+" "+Snapshot());
            return false;
        } catch(Exception e) {Log("ACTION_GATE_ERROR_FAIL_OPEN "+e.GetType().Name+" "+e.Message);return true;}
    }
    // Shield native progression from redundant KeypadEnter, without changing HHoushi state.
    private static bool ChainBegin()
    {
        try {
            if(KeypadEnterVerdict()==EnterVerdict.Block){
                blockedChainAfterCum++;
                insideChain=false;chainScopeFrame=-1;
                Log("BLOCK_KEYPAD_ENTER_CHAIN #"+blockedChainAfterCum+" "+Snapshot());
                return false;
            }
        } catch(Exception e) {Log("CHAIN_GATE_ERROR_FAIL_OPEN "+e.GetType().Name+" "+e.Message);}
        insideChain=true;
        chainScopeFrame=Time.frameCount;
        if (ready && Input.GetKeyDown(KeyCode.KeypadEnter)) {
            chainKeySeen++;
            Log("CHAIN_KEYPAD_ENTER #"+chainKeySeen+" "+Snapshot());
        }
        return true;
    }
    private static void ChainEnd()
    {
        if(ready && Input.GetKeyDown(KeyCode.KeypadEnter))Log("CHAIN_COMPLETE "+Snapshot());
        insideChain=false;chainScopeFrame=-1;
    }
    // Original v0.3.0 pre-cum ToPistonChain hook, unchanged. Called from Chain().
    private static bool PistonGatePrefix()
    {
        if(!ready || stopped || !insideChain || chainScopeFrame!=Time.frameCount)return true;
        try {
            if(!Eligible())return true;
            int frame=Time.frameCount;
            if(lastInterceptFrame!=frame){
                intercepts++;
                lastInterceptFrame=frame;
                observedUntil=frame+360;
                lastObservedState="";sawWLoop=false;sawInsertion=false;
                Log("SKIP_PRE_CUM_TO_PISTON #"+intercepts+" "+Snapshot());
            }
            return false;
        } catch(Exception e){Log("GATE_ERROR_FAIL_OPEN "+e.GetType().Name+" "+e.Message);return true;}
    }
    static void RequireOwner(MethodBase m)
    {
        object patchInfo=Harmony.GetPatchInfo(m);
        if(patchInfo==null)throw new InvalidOperationException("Harmony info missing: "+m.Name);
        PropertyInfo p=patchInfo.GetType().GetProperty("Owners",It);
        if(p==null)throw new MissingMemberException("PatchInfo Owners: "+m.Name);
        IEnumerable owners=p.GetValue(patchInfo,null) as IEnumerable;
        if(owners==null)throw new InvalidOperationException("Owners unavailable: "+m.Name);
        foreach(object o in owners)if(String.Equals(o as string,Owner,StringComparison.Ordinal))return;
        throw new InvalidOperationException("KPlugBenchEnterFix owner not installed: "+m.Name);
    }
    void TryInit()
    {
        if(ready||stopped)return;
        try {
            Assembly k=Find("kPlug"),game=Find("Assembly-CSharp");
            if(k==null||game==null)return;
            string root=Path.GetFullPath(Path.Combine(Paths.BepInExRootPath,".."));
            RequireHash(Path.Combine(root,@"BepInEx\plugins\kPlug\kPlug.dll"),KplugSha);
            RequireHash(Path.Combine(root,@"Koikatu_Data\Managed\Assembly-CSharp.dll"),GameSha);
            RequireHash(Path.Combine(root,@"BepInEx\plugins\KPlugFixes\KPlugCrossFaderCompatFix.dll"),CrossFaderSha);
            RequireHash(Path.Combine(root,@"BepInEx\plugins\HPointBenchRouteFix.dll"),BenchFixSha);
            Type core=T(k,"kPlug.Core"),ctrl=T(k,"kPlug.CmpH.HCtrl"),combo=T(k,"kPlug.CmpH.HComboCtrl");
            Type parser=T(k,"kPlug.Inputs.HinputParser"),flag=T(game,"HFlag"),info=T(game,"HSceneProc+AnimationListInfo");
            coreFlag=F(core,"hFlag",St);coreCtrl=F(core,"hCtrl",St);coreCombo=F(core,"combo",St);coreHAnim=F(core,"hAnim",St);
            flagState=F(flag,"nowAnimStateName",It);flagMode=F(flag,"<mode>k__BackingField",It);
            flagInfo=F(flag,"nowAnimationInfo",It);flagClick=F(flag,"click",It);flagGauge=F(flag,"gaugeMale",It);infoName=F(info,"nameAnimation",It);
            flagVoiceWait=F(flag,"voiceWait",It);
            if(!flagClick.FieldType.IsEnum)throw new InvalidOperationException("HFlag.click is not an enum");
            nativeNoneClick=Enum.Parse(flagClick.FieldType,"none",false);
            nativeSpeedupClick=Enum.Parse(flagClick.FieldType,"speedup",false);
            ctrlPosMod=F(ctrl,"posMod",It);ctrlFreeze=F(ctrl,"inputsFreezeTime",It);
            comboInPos=F(combo,"<inPosChain>k__BackingField",It);comboLaunch=F(combo,"inLaunchMov",It);
            Type cf=null;
            foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()){
                Type tmp=a.GetType("CompatGate",false);if(!object.ReferenceEquals(tmp,null)){cf=tmp;break;}
            }
            if(object.ReferenceEquals(cf,null))return;
            cfGateActive=F(cf,"_gateActive",St);
            // Do not repeat old CumRouteDiag error: Action requires five parameters.
            action=parser.GetMethod("Action",St,null,new Type[]{typeof(bool),typeof(bool),typeof(float),typeof(bool),typeof(bool)},null);
            if(action==null || !object.ReferenceEquals(action.ReturnType,typeof(void)))throw new MissingMethodException(parser.FullName,"Action(bool,bool,float,bool,bool)");
            chain=parser.GetMethod("Chain",St,null,Type.EmptyTypes,null);
            if(chain==null)throw new MissingMethodException(parser.FullName,"Chain");
            Type pose=T(k,"kPlug.DefAndParse.Def+PoseMod");
            piston=combo.GetMethod("ToPistonChain",It,null,new Type[]{pose,typeof(bool)},null);
            if(piston==null)throw new MissingMethodException(combo.FullName,"ToPistonChain(PoseMod,bool)");
            Type clothes=T(k,"kPlug.Tools.Clothes"),chaControl=T(game,"ChaControl");
            showKokan=clothes.GetMethod("ShowKokan",St,null,new Type[]{chaControl},null);
            if(showKokan==null || !showKokan.IsStatic ||
               !object.ReferenceEquals(showKokan.ReturnType,typeof(void)))
                 throw new MissingMethodException(clothes.FullName,"static void ShowKokan(ChaControl)");
            // Fifth hook replays a pending native click only after checked readiness.
            Type houshi=T(game,"HHoushi"),hbase=T(game,"HActionBase"),hvoice=T(game,"HVoiceCtrl");
            nativeHoushiProc=houshi.GetMethod("Proc",It,null,Type.EmptyTypes,null);
            if(nativeHoushiProc==null || nativeHoushiProc.IsStatic ||
               !object.ReferenceEquals(nativeHoushiProc.ReturnType,typeof(bool)))
                throw new MissingMethodException("HHoushi.Proc() instance bool");
            houshiFemale=F(hbase,"female",It);
            houshiVoice=F(hbase,"voice",It);
            houshiAutoStart=F(houshi,"autoStart",It);
            voiceNowVoices=F(hvoice,"nowVoices",It);
            getAnimatorState=chaControl.GetMethod("getAnimatorStateInfo",It,null,new Type[]{typeof(int)},null);
            if(getAnimatorState==null || !object.ReferenceEquals(getAnimatorState.ReturnType,typeof(AnimatorStateInfo)))
                throw new MissingMethodException("ChaControl.getAnimatorStateInfo(int)");
            harmony=new Harmony(Owner);
            Type self=typeof(KPlugBenchEnterFix);
            MethodInfo before=self.GetMethod("ChainBegin",St);
            MethodInfo after=self.GetMethod("ChainEnd",St);
            MethodInfo gate=self.GetMethod("PistonGatePrefix",St);
            MethodInfo actionGate=self.GetMethod("ActionReentryPrefix",St);
            MethodInfo clothesGate=self.GetMethod("IdleClothesPrefix",St);
            MethodInfo diag=self.GetMethod("NativeIdleStartProbePrefix",St);
            if(before==null || after==null || gate==null || actionGate==null || clothesGate==null || diag==null)
                 throw new MissingMethodException("KPlugBenchEnterFix patch methods");
            // Five targeted methods: four unchanged v0.7.2 hooks + one scoped
            // Proc-prefix one-shot click replay. No transpiler/base DLL edits.
            harmony.Patch(chain,new HarmonyMethod(before),new HarmonyMethod(after),null,null,null);
            harmony.Patch(piston,new HarmonyMethod(gate),null,null,null,null);
            harmony.Patch(action,new HarmonyMethod(actionGate),null,null,null,null);
            harmony.Patch(showKokan,new HarmonyMethod(clothesGate),null,null,null,null);
            harmony.Patch(nativeHoushiProc,new HarmonyMethod(diag),null,null,null,null);
            RequireOwner(nativeHoushiProc);
            RequireOwner(chain);
            RequireOwner(piston);
            RequireOwner(action);
            RequireOwner(showKokan);
            ready=true;
            Log("READY hooks=ChainScope+ToPistonChain+ActionLifecycleGate+IdleClothesGate+IdleStartProbe preCumGate=True owner="+Owner+" nativeWrite=DeferredClickOnly baselineSHA=OK");
        } catch(Exception e) {
            stopped=true;ready=false;Log("PATCH_NOT_APPLIED "+e.GetType().Name+" "+e.Message);
            if(harmony!=null)try{harmony.UnpatchSelf();}catch{}
            Logger.LogWarning("KPlugBenchEnterFix not applied: "+e.Message);
        }
    }
}
