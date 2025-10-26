using System;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
// ReSharper disable CheckNamespace
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable InconsistentNaming
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable RemoveRedundantBraces

// Thanks, Digi, for providing me with this class.
// A minimum of alteration on my part to match my specific needs in this utility.

namespace Digi.BuildInfo.Features.LiveData;



public class TempBlockSpawn
{
    public const string TempGridDisplayName = "BuildInfo_TemporaryGrid";
    readonly Vector3D _spawnPos;

    readonly MyCubeBlockDefinition BlockDef;

    readonly TaskCompletionSource<IMySlimBlock> _tcs = new();

    TempBlockSpawn(MyCubeBlockDefinition def, Vector3D spawnPos)
    {
        BlockDef = def;
        _spawnPos = spawnPos;
    }

    public static async Task<IMySlimBlock> SpawnAsync(MyCubeBlockDefinition def, Vector3D spawnPos = default)
    {
        var instance = new TempBlockSpawn(def, spawnPos);
        return await instance.MakeItSo();
    }

    async Task<IMySlimBlock> MakeItSo()
    {
        Console.WriteLine(@$"Spawning {BlockDef.Id.ToString()}");

        MyObjectBuilder_CubeBlock blockOB = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(BlockDef.Id);
        blockOB.EntityId = 0;
        blockOB.Min = Vector3I.Zero;

        MyObjectBuilder_CubeGrid gridOB = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
        gridOB.EntityId = 0;
        gridOB.DisplayName = TempGridDisplayName;
        gridOB.CreatePhysics = false;
        gridOB.GridSizeEnum = BlockDef.CubeSize;
        gridOB.PositionAndOrientation = new MyPositionAndOrientation(_spawnPos, Vector3.Forward, Vector3.Up);
        gridOB.PersistentFlags = MyPersistentEntityFlags2.InScene;
        gridOB.IsStatic = true;
        gridOB.Editable = false;
        gridOB.DestructibleBlocks = false;
        gridOB.IsRespawnGrid = false;
        gridOB.CubeBlocks.Add(blockOB);

        MyCubeGrid grid = (MyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOB, true, SpawnCompleted);
        grid.IsPreview = true;
        grid.Save = false;
        return await _tcs.Task;
    }

    void SpawnCompleted(IMyEntity ent)
    {
        // has to be here if wanna do this, but not really important anyway
        // if done before it fully initializes, it can crash for certain blocks, like Holo LCD
        //ent.Render.Visible = false;

        IMyCubeGrid grid = ent as IMyCubeGrid;

        try
        {
            IMySlimBlock block = grid?.GetCubeBlock(Vector3I.Zero);
            if (block == null)
            {
                MyLog.Default.Error($"Can't get block from spawned entity for block: {BlockDef.Id.ToString()}; grid={grid?.EntityId.ToString() ?? "(NULL)"};");
                return;
            }
            _tcs.SetResult(block);
        }
        catch (Exception e)
        {
            MyLog.Default.Error(e.ToString());
            _tcs.SetException(e);
        }
    }
}