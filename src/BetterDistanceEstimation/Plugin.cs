using BepInEx;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using System;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Unity.Jobs;
using Brimstone.BallDistanceJobs;

namespace BetterDistanceEstimation;

[MonoDetourTargets(typeof(PlayerGolfer))]
public class PlayerGolferHooks
{
    [MonoDetourHookInitialize]
    static void Init()
    {
        Md.PlayerGolfer._OnBUpdate_g__UpdateHoleDistanceEstimationForOwnBall_153_4.ILHook(ILHook_CallOurJob);
    }

    static void ILHook_CallOurJob(ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        var start = w.MatchRelaxed(
            x => x.MatchLdcI4(301) && w.SetCurrentTo(x),
            x => x.MatchLdcI4(1),
            x => x.MatchLdloca(9),
            x => x.MatchInitobj(out _),
            x => x.MatchLdloc(9),
            x => x.MatchCall(((Func<CalculateFirstGroundHitDistancesJob, int, int, JobHandle, JobHandle>) IJobParallelForExtensions.Schedule).Method)
        ).ThrowIfFailure().Current;

        var end = w.MatchRelaxed(
            x => x.MatchLdcI4(301),
            x => x.MatchLdcI4(1),
            x => x.MatchLdloca(9),
            x => x.MatchInitobj(out _),
            x => x.MatchLdloc(9),
            x => x.MatchCall(((Func<CalculateFirstGroundHitDistancesJob, int, int, JobHandle, JobHandle>) IJobParallelForExtensions.Schedule).Method) && w.SetCurrentTo(x)
        ).ThrowIfFailure().Current;

        w.RemoveRangeAndShiftLabels(start, end);
        w.InsertBeforeCurrent(
            w.Create(OpCodes.Ldarg, 0),
            w.CreateDelegateCall((CalculateFirstGroundHitDistancesJob origJob, PlayerGolfer that) =>
            {
                var hole = GolfHoleManager.MainHole.transform.position - that.NetworkownBall.transform.position;
                return (new LyricLyFirstGroundHitDistancesJob(origJob, hole)).Schedule(301, 1, default(JobHandle));
            })
        );
    }
}

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        MonoDetourManager.InvokeHookInitializers(Assembly.GetExecutingAssembly());
    }
}
