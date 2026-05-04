using System;
using System.Collections.Generic;
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

// Thanks, Digi, for providing the original temp-block-spawn pattern.
// Adapted to spawn many blocks on a single shared grid.

namespace Digi.BuildInfo.Features.LiveData
{
    public class TempBlockSpawn
    {
        public const string TempGridDisplayName = "BuildInfo_TemporaryGrid";

        public static void Spawn(
            IReadOnlyList<(MyCubeBlockDefinition Definition, Vector3I Position)> blocks,
            MyCubeSize gridSize,
            Vector3D spawnPos,
            Action<IMyCubeGrid> callback)
        {
            new TempBlockSpawn(blocks, gridSize, spawnPos, callback);
        }

        readonly Action<IMyCubeGrid> Callback;

        TempBlockSpawn(
            IReadOnlyList<(MyCubeBlockDefinition Definition, Vector3I Position)> blocks,
            MyCubeSize gridSize,
            Vector3D spawnPos,
            Action<IMyCubeGrid> callback)
        {
            Callback = callback;

            MyObjectBuilder_CubeGrid gridOB = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
            gridOB.EntityId = 0;
            gridOB.DisplayName = TempGridDisplayName;
            gridOB.CreatePhysics = false;
            gridOB.GridSizeEnum = gridSize;
            gridOB.PositionAndOrientation = new MyPositionAndOrientation(spawnPos, Vector3.Forward, Vector3.Up);
            gridOB.PersistentFlags = MyPersistentEntityFlags2.InScene;
            gridOB.IsStatic = true;
            gridOB.Editable = false;
            gridOB.DestructibleBlocks = false;
            gridOB.IsRespawnGrid = false;

            foreach (var (def, pos) in blocks)
            {
                MyObjectBuilder_CubeBlock blockOB = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(def.Id);
                blockOB.EntityId = 0;
                blockOB.Min = pos;
                gridOB.CubeBlocks.Add(blockOB);
            }

            MyCubeGrid grid = (MyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOB, true, SpawnCompleted);

            grid.IsPreview = true;
            grid.Save = false;
        }

        void SpawnCompleted(IMyEntity ent)
        {
            IMyCubeGrid grid = ent as IMyCubeGrid;

            try
            {
                if (grid == null)
                {
                    MyLog.Default.Error("TempBlockSpawn: spawned entity was not an IMyCubeGrid");
                    return;
                }

                Callback?.Invoke(grid);
            }
            catch (Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
        }
    }
}
