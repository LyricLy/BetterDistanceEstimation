using Brimstone.BallDistanceJobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct LyricLyFirstGroundHitDistancesJob : IJobParallelFor
{
    public NativeArray<float> normalizedInitialSpeeds;
    public NativeHashMap<int2, TerrainManager.JobsTerrain> spatiallyHashedTerrains;
    public NativeHashMap<int, int> globalTerrainLayerIndicesPerLevelTerrainLayer;
    public NativeArray<float> allTerrainLayerWeights;
    public NativeArray<float> allTerrainHeights;
    public NativeList<BoundsManager.SecondaryOutOfBoundsHazardInstance> secondaryOutOfBoundsHazards;
    public NativeList<BoundsManager.LevelHazardInstance> levelHazards;
    public NativeArray<PlayerGolfer.SwingDistanceEstimation> estimatedDistances;
    public float mainOutOfBoundsHazardHeight;
    public OutOfBoundsHazard mainOutOfBoundsHazardType;
    public float2 terrainSize;
    public float2 initialWorldPosition2d;
    public float yawRad;
    public float fullInitialSpeed;
    public float baseInitialSpeed;
    public float verticalGravity;
    public float pitchRad;
    public float airDragCoefficient;
    public float deltaTime;
    public float3 flagPosition;

    public LyricLyFirstGroundHitDistancesJob(CalculateFirstGroundHitDistancesJob origJob, float3 flagPosition)
    {
        this.normalizedInitialSpeeds = origJob.normalizedInitialSpeeds;
        this.spatiallyHashedTerrains = origJob.spatiallyHashedTerrains;
        this.globalTerrainLayerIndicesPerLevelTerrainLayer = origJob.globalTerrainLayerIndicesPerLevelTerrainLayer;
        this.allTerrainLayerWeights = origJob.allTerrainLayerWeights;
        this.allTerrainHeights = origJob.allTerrainHeights;
        this.secondaryOutOfBoundsHazards = origJob.secondaryOutOfBoundsHazards;
        this.levelHazards = origJob.levelHazards;
        this.estimatedDistances = origJob.estimatedDistances;
        this.mainOutOfBoundsHazardHeight = origJob.mainOutOfBoundsHazardHeight;
        this.terrainSize = origJob.terrainSize;
        this.initialWorldPosition2d = origJob.initialWorldPosition2d;
        this.yawRad = origJob.yawRad;
        this.fullInitialSpeed = origJob.fullInitialSpeed;
        this.baseInitialSpeed = origJob.baseInitialSpeed;
        this.verticalGravity = origJob.verticalGravity;
        this.pitchRad = origJob.pitchRad;
        this.airDragCoefficient = origJob.airDragCoefficient;
        this.deltaTime = origJob.deltaTime;
        this.flagPosition = flagPosition;
    }

    public void Execute(int initialSpeedIndex)
    {
        math.sincos(this.yawRad, out float angle1, out float angle2);
        var angle = new float2(angle1, angle2);

        float speed = this.baseInitialSpeed + this.normalizedInitialSpeeds[initialSpeedIndex] * (this.fullInitialSpeed - this.baseInitialSpeed);
        math.sincos(this.pitchRad, out float v1, out float v2);
        var v = new float2(v2, v1) * speed;

        var us = this;
        var gravity = new float2(0f, this.verticalGravity);
        var lastPos = Vector2.zero;
        var simPos = float2.zero;

        float2 midpoint = this.flagPosition.xz / 2;
        var perp = new float2(-this.flagPosition.z, this.flagPosition.x);
        float denominator = perp.y * angle.x - perp.x * angle.y;
        float ledgeX = (perp.y * midpoint.x - perp.x * midpoint.y) / denominator;
        float Floor() => ledgeX > 0 && simPos.x > ledgeX ? us.flagPosition.y : 0;

        while (simPos.y >= Floor())
        {
            v += gravity * us.deltaTime;
            v *= math.max(0f, 1f - us.airDragCoefficient * math.lengthsq(v) * us.deltaTime);
            lastPos = simPos;
            simPos += v * us.deltaTime;
        }

        float finalX = lastPos.x <= ledgeX && simPos.x > ledgeX ? ledgeX : math.lerp(lastPos.x, simPos.x, BMath.InverseLerp(lastPos.y, simPos.y, Floor()));
        float2 endPos = this.initialWorldPosition2d + finalX * angle;

        int2 spatialHash = TerrainManager.GetSpatialHash(endPos, this.terrainSize);
        var terrainLayer = (TerrainLayer)(-1);
        var levelHazard = (LevelHazardType)(-1);
        var outOfBoundsHazard = (OutOfBoundsHazard)(-1);

        if (this.spatiallyHashedTerrains.TryGetValue(spatialHash, out var jobsTerrain))
        {
            float heightAt = jobsTerrain.GetHeightAt(endPos, this.allTerrainHeights);
            var point = new float3(endPos.x, heightAt, endPos.y);
            if (BoundsJobHelper.IsInOrOverLevelHazard(
                point,
                this.levelHazards,
                out bool isOverHazard, out _, out LevelHazardType hazardType, out _
            ) && !isOverHazard)
            {
                levelHazard = hazardType;
            }
            else
            {
                BoundsJobHelper.IsInOutOfBoundsHazard(
                    point,
                    this.secondaryOutOfBoundsHazards,
                    this.mainOutOfBoundsHazardHeight,
                    this.mainOutOfBoundsHazardType,
                    out _, out var hazardType2, out _
                );
                if (hazardType2 >= OutOfBoundsHazard.Water)
                {
                    outOfBoundsHazard = hazardType2;
                }
                else
                {
                    int dominantLayerIndexAt = jobsTerrain.GetDominantLayerIndexAt(endPos, this.allTerrainLayerWeights);
                    if (this.globalTerrainLayerIndicesPerLevelTerrainLayer.TryGetValue(dominantLayerIndexAt, out var tl))
                    {
                        terrainLayer = (TerrainLayer)tl;
                    }
                }
            }
        }

        this.estimatedDistances[initialSpeedIndex] = new PlayerGolfer.SwingDistanceEstimation(finalX, terrainLayer, levelHazard, outOfBoundsHazard);
    }
}
