﻿// A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2018 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using SyncroSim.Core;
using SyncroSim.Common;
using SyncroSim.StochasticTime;
using System.Collections.Generic;

namespace SyncroSim.STSim
{
    public partial class STSimTransformer
    {
        /// <summary>Get the Cell Neighbor at the compass direction North relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the North compass direction</returns>
        private Cell GetCellNorth(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, -1, 0);
        }

        /// <summary>Get the Cell Neighbor at the compass direction North East relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the North East compass direction</returns>
        private Cell GetCellNortheast(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, -1, 1);
        }

        /// <summary>Get the Cell Neighbor at the compass direction East relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the East compass direction</returns>
        private Cell GetCellEast(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, 0, 1);
        }

        /// <summary>Get the Cell Neighbor at the compass direction South East relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the South East compass direction</returns>
        private Cell GetCellSoutheast(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, 1, 1);
        }

        /// <summary>Get the Cell Neighbor at the compass direction South relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the South compass direction</returns>
        private Cell GetCellSouth(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, 1, 0);
        }

        /// <summary>Get the Cell Neighbor at the compass direction South West relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the South West compass direction</returns>
        private Cell GetCellSouthwest(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, 1, -1);
        }

        /// <summary>Get the Cell Neighbor at the compass direction West relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the West compass direction</returns>
        private Cell GetCellWest(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, 0, -1);
        }

        /// <summary>Get the Cell Neighbor at the compass direction North West relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the North West compass direction</returns>
        private Cell GetCellNorthwest(Cell initiationCell)
        {
            return GetCellByOffset(initiationCell.CellId, -1, -1);
        }

        /// <summary>
        /// Gets a cell for the specified initiation cell Id and row and column offsets
        /// </summary>
        /// <param name="initiationCellId"></param>
        /// <param name="rowOffset"></param>
        /// <param name="columnOffset"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private Cell GetCellByOffset(int initiationCellId, int rowOffset, int columnOffset)
        {
            int id = this.m_InputRasters.GetCellIdByOffset(initiationCellId, rowOffset, columnOffset);

            if (id == -1)
            {
                return null;
            }
            else
            {
                if (this.Cells.Contains(id))
                {
                    return this.Cells[id];
                }
                else
                {
                    return null;
                }
            }
        }

        private List<Cell> GetNeighboringCells(Cell c)
        {
            List<Cell> neighbors = new List<Cell>();

            Action<Cell> addNeighbor = (Cell c1) =>
            {
                if (c1 != null)
                {
                    neighbors.Add(c1);
                }
            };

            addNeighbor(this.GetCellNorth(c));
            addNeighbor(this.GetCellNortheast(c));
            addNeighbor(this.GetCellEast(c));
            addNeighbor(this.GetCellSoutheast(c));
            addNeighbor(this.GetCellSouth(c));
            addNeighbor(this.GetCellSouthwest(c));
            addNeighbor(this.GetCellWest(c));
            addNeighbor(this.GetCellNorthwest(c));

            return neighbors;
        }

        /// <summary>Get the Cell Neighbor at the direction and distance relative to the specified Cell</summary>
        /// <param name="initiationCell">The cell we're looking for the neighbor of</param>
        /// <returns>The neighboring Cell at the specified direction and distance </returns>
        private Cell GetCellByDistanceAndDirection(Cell initiationCell, int directionDegrees, double distanceM)
        {
            int id = this.m_InputRasters.GetCellIdByDistanceAndDirection(initiationCell.CellId, directionDegrees, distanceM);

            if (id == -1)
            {
                return null;
            }
            else
            {
                if (this.Cells.Contains(id))
                {
                    return this.Cells[id];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the distance between two neighboring cells
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private double GetNeighborCellDistance(CardinalDirection direction)
        {
            double dist = this.m_InputRasters.GetCellSizeMeters();

            if (direction == CardinalDirection.NE || 
                direction == CardinalDirection.SE || 
                direction == CardinalDirection.SW || 
                direction == CardinalDirection.NW)
            {
                dist = this.m_InputRasters.GetCellSizeDiagonalMeters();
            }

            return dist;
        }

        /// <summary>
        /// Gets the slope for the specified cells
        /// </summary>
        /// <param name="sourceCell"></param>
        /// <param name="destinationCell"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private double GetSlope(Cell sourceCell, Cell destinationCell, double distance)
        {
            double rise = this.GetCellElevation(destinationCell) - this.GetCellElevation(sourceCell);
            double radians = Math.Atan(rise / distance);
            double degrees = radians * (180 / Math.PI);

            return degrees;
        }

        /// <summary>
        /// Gets the elevation for the specified cell
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private double GetCellElevation(Cell cell)
        {
            if (this.m_InputRasters.DemCells == null || (this.m_InputRasters.DemCells.Count() == 0))
            {
                return 1.0;
            }
            else
            {
                return this.m_InputRasters.DemCells[cell.CellId];
            }
        }

        /// <summary>
        /// Gets the average attribute value for the specified cell's neighborhood and attribute type
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="transitionGroupId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private double? GetNeighborhoodAttributeValue(Cell cell, int transitionGroupId)
        {
            if (this.m_TransitionAdjacencyStateAttributeValueMap.ContainsKey(transitionGroupId))
            {
                double[] attrVals = this.m_TransitionAdjacencyStateAttributeValueMap[transitionGroupId];

                if (attrVals[cell.CellId] == StochasticTimeRaster.DefaultNoDataValue)
                {
                    return null;
                }
                else
                {
                    return attrVals[cell.CellId];
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the transitioned pixels array for the specified timestep
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="transitionTypeId"></param>
        /// <param name="transitionedPixels"></param>
        /// <remarks></remarks>
        private void UpdateTransitionedPixels(Cell cell, int transitionTypeId, int[] transitionedPixels)
        {
            Debug.Assert(this.IsSpatial);

            if (!(this.m_CreateRasterTransitionOutput || this.m_CreateRasterAATPOutput))
            {
                return;
            }

            //Dereference to find TT "ID". If blank, dont bother to record transition.
            int? TransTypeMapId = this.m_TransitionTypes[transitionTypeId].MapId;

            if (TransTypeMapId.HasValue)
            {
                transitionedPixels[cell.CellId] = TransTypeMapId.Value;
            }
        }

        /// <summary>
        /// Creates a dictionary of transitioned pixel arrays, with Transition Group Id as the dictionary key
        /// </summary>
        /// <returns>Dictionary(Of Integer, Integer())</returns>
        /// <remarks></remarks>
        private Dictionary<int, int[]> CreateTransitionGroupTransitionedPixels()
        {
            Debug.Assert(this.IsSpatial);

            Dictionary<int, int[]> dictTransitionPixels = new Dictionary<int, int[]>();

            // Loop thru transition groups. 
            foreach (TransitionGroup tg in this.m_TransitionGroups)
            {
                //Make sure Primary
                if (tg.PrimaryTransitionTypes.Count == 0)
                {
                    continue;
                }

                // Create a transitionPixel array object. If no Transition Output actually configured, economize on memory by not
                // dimensioning the array

                int[] transitionPixel = null;

                if (this.m_CreateRasterTransitionOutput || this.m_CreateRasterAATPOutput)
                {
                    transitionPixel = new int[this.m_InputRasters.NumberCells];
                    // initialize to DEFAULT_NO_DATA_VLAUE
                    for (var i = 0; i < this.m_InputRasters.NumberCells; i++)
                    {
                        transitionPixel[i] = StochasticTimeRaster.DefaultNoDataValue;
                    }
                }

                dictTransitionPixels.Add(tg.TransitionGroupId, transitionPixel);
            }

            return dictTransitionPixels;
        }

        /// <summary>
        /// Creates a dictionary of transition attribute value arrays
        /// </summary>
        /// <param name="timestep"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private Dictionary<int, double[]> CreateRasterTransitionAttributeArrays(int timestep)
        {
            Debug.Assert(this.IsSpatial);

            Dictionary<int, double[]> dict = new Dictionary<int, double[]>();

            if (this.IsRasterTransitionAttributeTimestep(timestep))
            {
                foreach (int id in this.m_TransitionAttributeTypeIds.Keys)
                {
                    Debug.Assert(this.m_TransitionAttributeTypes.Contains(id));
                    double[] arr = new double[this.m_InputRasters.NumberCells];

                    //Initialize array to ApexRaster.DEFAULT_NO_DATA_VALUE

                    for (int i = 0; i < this.m_InputRasters.NumberCells; i++)
                    {
                        arr[i] = StochasticTimeRaster.DefaultNoDataValue;
                    }

                    dict.Add(id, arr);
                }
            }

            return dict;
        }

        /// <summary>
        /// Applies probabilistic transitions in raster mode
        /// </summary>
        /// <param name="iteration"></param>
        /// <param name="timestep"></param>
        /// <remarks></remarks>
        private void ApplyProbabilisticTransitionsRaster(
            int iteration, 
            int timestep, 
            Dictionary<int, double[]> rasterTransitionAttrValues, 
            Dictionary<int, int[]> dictTransitionedPixels)
        {
            Debug.Assert(this.IsSpatial);

            foreach (Stratum Stratum in this.m_Strata)
            {
                this.ShuffleStratumCells(Stratum);
            }

            Dictionary<int, TransitionGroup> RemainingTransitionGroups = new Dictionary<int, TransitionGroup>();

            foreach (TransitionGroup tg in this.m_ShufflableTransitionGroups)
            {
                RemainingTransitionGroups.Add(tg.TransitionGroupId, tg);
            }

            foreach (TransitionGroup TransitionGroup in this.m_ShufflableTransitionGroups)
            {
                if (TransitionGroup.PrimaryTransitionTypes.Count == 0)
                {
                    continue;
                }

                MultiLevelKeyMap1<Dictionary<int, TransitionAttributeTarget>> tatMap = new MultiLevelKeyMap1<Dictionary<int, TransitionAttributeTarget>>();

                this.ResetTransitionTargetMultipliers(iteration, timestep, TransitionGroup);
                this.ResetTranstionAttributeTargetMultipliers(iteration, timestep, RemainingTransitionGroups, tatMap, TransitionGroup);

                RemainingTransitionGroups.Remove(TransitionGroup.TransitionGroupId);

                //If the transition group has no size distribution or transition patches then call the non-spatial algorithm for this group.

                if ((!TransitionGroup.HasSizeDistribution) && (TransitionGroup.PatchPrioritization == null))
                {
                    foreach (Cell simulationCell in this.m_Cells)
                    {
                        ApplyProbabilisticTransitionsByCell(
                            simulationCell, iteration, timestep, TransitionGroup, 
                            dictTransitionedPixels[TransitionGroup.TransitionGroupId], 
                            rasterTransitionAttrValues);
                    }
                }
                else
                {
                    Dictionary<int, Cell> TransitionedCells = new Dictionary<int, Cell>();

                    foreach (Stratum Stratum in this.m_Strata)
                    {
                        double ExpectedArea = 0.0;
                        double MaxCellProbability = 0.0;

                        this.FillTransitionPatches(TransitionedCells, Stratum, TransitionGroup, iteration, timestep);

                        Dictionary<int, Cell> InitiationCells = this.CreateInitiationCellCollection(
                            TransitionedCells, Stratum.StratumId, TransitionGroup.TransitionGroupId, iteration, timestep, 
                            ref ExpectedArea, ref MaxCellProbability);

                        if (ExpectedArea > 0.0 && MaxCellProbability > 0.0)
                        {
                            bool GroupHasTarget = TransitionGroupHasTarget(TransitionGroup.TransitionGroupId, Stratum.StratumId, iteration, timestep);
                            bool MaximizeFidelityToTotalArea = this.MaximizeFidelityToTotalArea(TransitionGroup.TransitionGroupId, Stratum.StratumId, iteration, timestep);
                            double rand = this.m_RandomGenerator.GetNextDouble();

                            while ((MathUtils.CompareDoublesGT(ExpectedArea / this.m_AmountPerCell, rand, 0.000001)) && (InitiationCells.Count > 0))
                            {
                                List<TransitionEvent> TransitionEventList = this.CreateTransitionEventList(Stratum.StratumId, TransitionGroup.TransitionGroupId, iteration, timestep, ExpectedArea);

                                this.GenerateTransitionEvents(
                                    TransitionEventList, TransitionedCells, InitiationCells, Stratum.StratumId, TransitionGroup.TransitionGroupId, 
                                    iteration, timestep, MaxCellProbability, dictTransitionedPixels[TransitionGroup.TransitionGroupId], 
                                    ref ExpectedArea, rasterTransitionAttrValues);

                                if (!GroupHasTarget)
                                {
                                    if (!MaximizeFidelityToTotalArea)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    Dictionary<int, TransitionAttributeTarget> d = tatMap.GetItem(Stratum.StratumId);

                                    if (d != null)
                                    {
                                        bool TargetsMet = true;

                                        foreach (TransitionAttributeTarget tat in d.Values)
                                        {
                                            if (!tat.IsDisabled && tat.TargetRemaining > 0.0)
                                            {
                                                TargetsMet = false;
                                                break;
                                            }

                                            if (TargetsMet)
                                            {
                                                goto ExitWhile;
                                            }
                                        }
                                    }
                                }
                            }

                            ExitWhile: ;
                        }

                        this.ClearTransitionPatches(TransitionGroup);
                    }
                }
            }
        }

        private List<TransitionEvent> CreateTransitionEventList(int stratumId, int transitionGroupId, int iteration, int timestep, double expectedArea)
        {
            Debug.Assert(this.IsSpatial);
            Debug.Assert(expectedArea > 0.0);

            double AccumulatedArea = 0.0;
            List<TransitionEvent> TransitionEventList = new List<TransitionEvent>();

            while (MathUtils.CompareDoublesGT(expectedArea, AccumulatedArea, 0.000001))
            {
                double diff = expectedArea - AccumulatedArea;

                if (this.m_AmountPerCell > diff)
                {
                    double rand = this.m_RandomGenerator.GetNextDouble();
                    double prob = diff / this.m_AmountPerCell;

                    if (rand > prob)
                    {
                        break;
                    }
                }

                double MinimumSize = this.m_AmountPerCell;
                double MaximumSize = (expectedArea - AccumulatedArea);
                double TargetSize = this.m_AmountPerCell;
                double AreaDifference = (expectedArea - AccumulatedArea);

                this.GetTargetSizeClass(stratumId, transitionGroupId, iteration, timestep, AreaDifference, ref MinimumSize, ref MaximumSize, ref TargetSize);

                TransitionEventList.Add(new TransitionEvent(TargetSize));

                AccumulatedArea = AccumulatedArea + TargetSize;

                Debug.Assert(MinimumSize >= 0.0);
                Debug.Assert(MaximumSize >= 0.0);
                Debug.Assert(TargetSize >= 0.0);
                Debug.Assert(MinimumSize <= MaximumSize);
                Debug.Assert(TargetSize >= MinimumSize && TargetSize <= MaximumSize);
                Debug.Assert(TransitionEventList.Count < 100000);
            }

            this.SortTransitionEventList(stratumId, transitionGroupId, iteration, timestep, TransitionEventList);

            return TransitionEventList;
        }

        private void GetTargetSizeClass(int stratumId, int transitionGroupId, int iteration, int timestep, double areaDifference, ref double minimumSizeOut, ref double maximumSizeOut, ref double targetSizeOut)
        {
            Debug.Assert(this.IsSpatial);

            double CumulativeProportion = 0.0;
            double Rand1 = this.m_RandomGenerator.GetNextDouble();

            List<TransitionSizeDistribution> tsdlist = this.m_TransitionSizeDistributionMap.GetSizeDistributions(transitionGroupId, stratumId, iteration, timestep);

            if (tsdlist == null)
            {
                minimumSizeOut = this.m_AmountPerCell;
                maximumSizeOut = this.m_AmountPerCell;
                targetSizeOut = this.m_AmountPerCell;

                return;
            }

            foreach (TransitionSizeDistribution tsd in tsdlist)
            {
                CumulativeProportion += tsd.Proportion;

                if (CumulativeProportion >= Rand1)
                {
                    minimumSizeOut = tsd.MinimumSize;
                    maximumSizeOut = tsd.MaximumSize;

                    break;
                }
            }

            Debug.Assert(minimumSizeOut <= maximumSizeOut);

            if (maximumSizeOut > areaDifference)
            {
                maximumSizeOut = areaDifference;
                minimumSizeOut = areaDifference;
            }

            double Rand2 = this.m_RandomGenerator.GetNextDouble();
            double Rand3 = (maximumSizeOut - minimumSizeOut) * Rand2;

            targetSizeOut = Rand3 + minimumSizeOut;
        }

        /// <summary>
        /// Determines whether to maximize fidelity to total area
        /// </summary>
        /// <param name="transitionGroupId"></param>
        /// <param name="stratumId"></param>
        /// <param name="iteration"></param>
        /// <param name="timestep"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool MaximizeFidelityToTotalArea(int transitionGroupId, int stratumId, int iteration, int timestep)
        {
            TransitionSizePrioritization tsp = this.m_TransitionSizePrioritizationMap.GetSizePrioritization(transitionGroupId, stratumId, iteration, timestep);

            if (tsp == null)
            {
                return false;
            }
            else
            {
                return tsp.MaximizeFidelityToTotalArea;
            }
        }

        /// <summary>
        /// Determines whether there are transition targets or transition attribute targets associated with this 
        /// transition group, stratum, iteration and timestep
        /// </summary>
        /// <param name="transitionGroupId"></param>
        /// <param name="stratumId"></param>
        /// <param name="iteration"></param>
        /// <param name="timestep"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool TransitionGroupHasTarget(int transitionGroupId, int stratumId, int iteration, int timestep)
        {
            List<int?> SecondaryStratumIds = new List<int?>();
            List<int?> TertiaryStratumIds = new List<int?>();

            int? ssnull = null;
            int? tsnull = null;

            SecondaryStratumIds.Add(ssnull);
            TertiaryStratumIds.Add(tsnull);

            foreach (Stratum s in this.m_SecondaryStrata)
            {
                SecondaryStratumIds.Add(s.StratumId);
            }

            foreach (Stratum s in this.m_TertiaryStrata)
            {
                TertiaryStratumIds.Add(s.StratumId);
            }

            foreach (int? ss in SecondaryStratumIds)
            {
                foreach (int? ts in TertiaryStratumIds)
                {
                    TransitionTarget tt = this.m_TransitionTargetMap.GetTransitionTarget(transitionGroupId, stratumId, ss, ts, iteration, timestep);

                    if (tt != null)
                    {
                        return true;
                    }
                }
            }

            if (this.m_TransitionAttributeValueMap.TypeGroupMap.ContainsKey(transitionGroupId))
            {
                Dictionary<int, bool> d = this.m_TransitionAttributeValueMap.TypeGroupMap[transitionGroupId];

                foreach (TransitionAttributeType ta in this.m_TransitionAttributeTypes)
                {
                    if (d.ContainsKey(ta.TransitionAttributeId))
                    {
                        foreach (int? ss in SecondaryStratumIds)
                        {
                            foreach (int? ts in TertiaryStratumIds)
                            {
                                TransitionAttributeTarget tat = this.m_TransitionAttributeTargetMap.GetAttributeTarget(
                                    ta.TransitionAttributeId, stratumId, ss, ts, iteration, timestep);

                                if (tat != null)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void SortTransitionEventList(int stratumId, int transitionGroupId, int iteration, int timestep, List<TransitionEvent> transitionEventList)
        {
            TransitionSizePrioritization tsp = this.m_TransitionSizePrioritizationMap.GetSizePrioritization(
                transitionGroupId, stratumId, iteration, timestep);

            if ((tsp == null) || (tsp.SizePrioritization == SizePrioritization.None))
            {
                ShuffleUtilities.ShuffleList(transitionEventList, this.m_RandomGenerator.Random);
            }
            else
            {
                if (tsp.SizePrioritization == SizePrioritization.Smallest)
                {
                    transitionEventList.Sort((TransitionEvent e1, TransitionEvent e2) =>
                    {
                        return e1.TargetAmount.CompareTo(e2.TargetAmount);
                    });
                }
                else
                {
                    transitionEventList.Sort((TransitionEvent e1, TransitionEvent e2) =>
                    {
                        return (-(e1.TargetAmount.CompareTo(e2.TargetAmount)));
                    });
                }
            }
        }

        private Dictionary<int, Cell> CreateInitiationCellCollection(
            Dictionary<int, Cell> transitionedCells, int stratumId, int transitionGroupId, int iteration, int timestep, 
            ref double expectedAreaOut, ref double maxCellProbabilityOut)
        {
            Debug.Assert(this.IsSpatial);

            double ExpectedArea = 0.0;
            double MaxCellProbability = 0.0;
            Stratum Stratum = this.m_Strata[stratumId];
            Dictionary<int, Cell> InitiationCells = new Dictionary<int, Cell>();

            foreach (Cell SimulationCell in Stratum.Cells.Values)
            {
                Debug.Assert(SimulationCell.StratumId != 0);
                Debug.Assert(SimulationCell.StateClassId != 0);

                if (!transitionedCells.ContainsKey(SimulationCell.CellId))
                {
                    double CellProbability = this.SpatialCalculateCellProbabilityNonTruncated(SimulationCell, transitionGroupId, iteration, timestep);

                    ExpectedArea += (CellProbability * this.m_AmountPerCell);

                    //Include Initiation Multiplier in the calculation of cell probability once expected area has been calculated

                    CellProbability *= this.GetTransitionSpatialInitiationMultiplier(SimulationCell.CellId, transitionGroupId, iteration, timestep);

                    if (CellProbability > MaxCellProbability)
                    {
                        MaxCellProbability = CellProbability;

                        if (MaxCellProbability > 1.0)
                        {
                            MaxCellProbability = 1.0;
                        }
                    }

                    if (CellProbability > 0.0)
                    {
                        InitiationCells.Add(SimulationCell.CellId, SimulationCell);
                    }
                }
            }

            expectedAreaOut = ExpectedArea;
            maxCellProbabilityOut = MaxCellProbability;

            return InitiationCells;
        }

        private Cell SelectInitiationCell(
            Dictionary<int, Cell> initiationCells, int transitionGroupId, 
            int iteration, int timestep, double maxCellProbability)
        {
            Debug.Assert(this.IsSpatial);

            Cell SimulationCell = null;
            double CellProbability = 0.0;
            double Rand1 = this.m_RandomGenerator.GetNextDouble();
            int NumCellsChecked = 0;
            bool KeepLooping = true;

            do
            {
                NumCellsChecked += 1;
                int Rand2 = this.m_RandomGenerator.GetNextInteger(0, (initiationCells.Count - 1));
                SimulationCell = initiationCells.Values.ElementAt(Rand2);

                CellProbability = this.SpatialCalculateCellProbability(SimulationCell, transitionGroupId, iteration, timestep);
                CellProbability *= this.GetTransitionSpatialInitiationMultiplier(SimulationCell.CellId, transitionGroupId, iteration, timestep);
                CellProbability = CellProbability / maxCellProbability;

                //Increase probability of selection as the number of cells checked increases

                if (CellProbability < (NumCellsChecked / (double)initiationCells.Count))
                {
                    CellProbability = NumCellsChecked / (double)initiationCells.Count;
                }

                Rand1 = this.m_RandomGenerator.GetNextDouble();

                if (!MathUtils.CompareDoublesGT(Rand1, CellProbability, 0.000001))
                {
                    KeepLooping = false;
                }

                if (CellProbability == 0.0)
                {
                    KeepLooping = true;
                }

                if (initiationCells.Count == 0)
                {
                    KeepLooping = false;
                }
            } while (KeepLooping);

            initiationCells.Remove(SimulationCell.CellId);

            return SimulationCell;
        }

        private void GenerateTransitionEvents(List<TransitionEvent> transitionEventList, Dictionary<int, Cell> transitionedCells, Dictionary<int, Cell> initiationCells, int stratumId, int transitionGroupId, int iteration, int timestep, double maxCellProbability, int[] transitionedPixels, ref double expectedArea, Dictionary<int, double[]> rasterTransitionAttrValues)
        {
#if DEBUG

            Debug.Assert(this.IsSpatial);
            Debug.Assert(maxCellProbability > 0.0);

            foreach (Cell c in initiationCells.Values)
            {
                Debug.Assert(c.StratumId == stratumId);
            }

#endif

            TransitionGroup TransitionGroup = this.m_TransitionGroups[transitionGroupId];

            while ((transitionEventList.Count > 0) && (initiationCells.Count > 0) && (expectedArea > 0))
            {
                Cell InitiationCell = null;

                if (TransitionGroup.PatchPrioritization != null)
                {
                    InitiationCell = this.SelectPatchInitiationCell(TransitionGroup);

                    if (InitiationCell == null)
                    {
                        Debug.Assert(TransitionGroup.PatchPrioritization.TransitionPatches.Count == 0);
                        initiationCells.Clear(); //No Patches left. Clear Initiation Cells.

                        break;
                    }
                }
                else
                {
                    InitiationCell = this.SelectInitiationCell(initiationCells, transitionGroupId, iteration, timestep, maxCellProbability);
                }

                if (InitiationCell != null)
                {
                    double CellProbability = this.SpatialCalculateCellProbability(InitiationCell, transitionGroupId, iteration, timestep);

                    if (CellProbability > 0.0)
                    {
                        TransitionEvent TransitionEvent = transitionEventList[0];
                        TransitionSizePrioritization tsp = this.m_TransitionSizePrioritizationMap.GetSizePrioritization(
                            transitionGroupId, stratumId, iteration, timestep);

                        this.GrowTransitionEvent(
                            transitionEventList, TransitionEvent, transitionedCells, initiationCells, InitiationCell, transitionGroupId, 
                            iteration, timestep, transitionedPixels, ref expectedArea, rasterTransitionAttrValues, tsp);
                    }
                }
            }
        }

        private void GrowTransitionEvent(
            List<TransitionEvent> transitionEventList, TransitionEvent transitionEvent, Dictionary<int, Cell> transitionedCells, 
            Dictionary<int, Cell> initiationCells, Cell initiationCell, int transitionGroupId, int iteration, int timestep, 
            int[] transitionedPixels, ref double expectedArea, Dictionary<int, double[]> rasterTransitionAttrValues, TransitionSizePrioritization tsp)
        {
            Debug.Assert(this.IsSpatial);

            double TotalEventAmount = 0.0;
            GrowEventRecordCollection EventCandidates = new GrowEventRecordCollection(this.m_RandomGenerator);
            Dictionary<int, Cell> SeenBefore = new Dictionary<int, Cell>();

            EventCandidates.AddRecord(new GrowEventRecord(initiationCell, 0.0, 1.0));
            SeenBefore.Add(initiationCell.CellId, initiationCell);

            Dictionary<int, Transition> transitionDictionary = new Dictionary<int, Transition>();

            while ((EventCandidates.Count > 0) && (TotalEventAmount <= expectedArea))
            {
                Transition Transition = null;
                GrowEventRecord CurrentRecord = EventCandidates.RemoveRecord();
                List<Cell> neighbors = this.GetNeighboringCells(CurrentRecord.Cell);

                TransitionPathwayAutoCorrelation AutoCorrelation = this.m_TransitionPathwayAutoCorrelationMap.GetCorrelation(transitionGroupId, CurrentRecord.Cell.StratumId, CurrentRecord.Cell.SecondaryStratumId, CurrentRecord.Cell.TertiaryStratumId, iteration, timestep);

                if (AutoCorrelation != null)
                {
                    if (AutoCorrelation.SpreadTo == AutoCorrelationSpread.ToSamePrimaryStratum && 
                        CurrentRecord.Cell.StratumId != initiationCell.StratumId)
                    {
                        continue;
                    }
                    else if (AutoCorrelation.SpreadTo == AutoCorrelationSpread.ToSameSecondaryStratum && 
                        CurrentRecord.Cell.SecondaryStratumId != initiationCell.SecondaryStratumId)
                    {
                        continue;
                    }
                    else if (AutoCorrelation.SpreadTo == AutoCorrelationSpread.ToSameTertiaryStratum && 
                        CurrentRecord.Cell.TertiaryStratumId != initiationCell.TertiaryStratumId)
                    {
                        continue;
                    }

                    foreach (Cell c in neighbors)
                    {
                        if (transitionDictionary.ContainsKey(c.CellId))
                        {
                            Transition neighborTransition = transitionDictionary[c.CellId];
                            if (CurrentRecord.Cell.Transitions.Contains(neighborTransition))
                            {
                                Transition = neighborTransition;
                                break;
                            }
                        }
                    }
                }

                if (Transition == null)
                {
                    if (AutoCorrelation != null)
                    {
                        if ((AutoCorrelation.SpreadTo == AutoCorrelationSpread.ToSamePathway) && (transitionDictionary.Count > 0))
                        {
                            continue;
                        }
                    }

                    Transition = this.SelectTransitionPathway(CurrentRecord.Cell, transitionGroupId, iteration, timestep);
                }
                else
                {
                    if (AutoCorrelation == null || (!AutoCorrelation.AutoCorrelation))
                    {
                        Transition = this.SelectTransitionPathway(CurrentRecord.Cell, transitionGroupId, iteration, timestep);
                    }
                }

                if (Transition == null)
                {
                    continue;
                }

                if (this.IsTransitionAttributeTargetExceded(CurrentRecord.Cell, Transition, iteration, timestep))
                {
                    initiationCells.Remove(CurrentRecord.Cell.CellId);
                    continue;
                }

                this.OnSummaryTransitionOutput(CurrentRecord.Cell, Transition, iteration, timestep);
                this.OnSummaryTransitionByStateClassOutput(CurrentRecord.Cell, Transition, iteration, timestep);

                this.ChangeCellForProbabilisticTransition(CurrentRecord.Cell, Transition, iteration, timestep, rasterTransitionAttrValues);

                if (!transitionDictionary.ContainsKey(CurrentRecord.Cell.CellId))
                {
                    transitionDictionary.Add(CurrentRecord.Cell.CellId, Transition);
                }

                this.FillProbabilisticTransitionsForCell(CurrentRecord.Cell, iteration, timestep);

                this.UpdateCellPatchMembership(transitionGroupId, CurrentRecord.Cell);
                this.UpdateTransitionedPixels(CurrentRecord.Cell, Transition.TransitionTypeId, transitionedPixels);

                Debug.Assert(!transitionedCells.ContainsKey(CurrentRecord.Cell.CellId));

                transitionedCells.Add(CurrentRecord.Cell.CellId, CurrentRecord.Cell);
                initiationCells.Remove(CurrentRecord.Cell.CellId);

                TotalEventAmount += this.m_AmountPerCell;

                if ((TotalEventAmount >= (transitionEvent.TargetAmount - (0.5 * this.m_AmountPerCell))) || (TotalEventAmount >= expectedArea))
                {
                    break;
                }

                double tempVar = CurrentRecord.TravelTime;

                this.AddGrowEventRecords(
                    EventCandidates, transitionedCells, SeenBefore, CurrentRecord.Cell, 
                    transitionGroupId, iteration, timestep, ref tempVar);
            }

            expectedArea -= TotalEventAmount;

            if (expectedArea < 0.0)
            {
                expectedArea = 0.0;
            }

            bool MaximizeFidelityToDistribution = true;

            if (tsp != null)
            {
                MaximizeFidelityToDistribution = tsp.MaximizeFidelityToDistribution;
            }

            if ((!MaximizeFidelityToDistribution) || (TotalEventAmount >= transitionEvent.TargetAmount))
            {
                transitionEventList.Remove(transitionEvent);
            }
            else
            {
                RemoveNearestSizedEvent(transitionEventList, TotalEventAmount);
            }
        }

        private void AddGrowEventRecords(GrowEventRecordCollection eventCandidates, Dictionary<int, Cell> transitionedCells, Dictionary<int, Cell> seenBefore, Cell initiationCell, int transitionGroupId, int iteration, int timestep, ref double travelTime)
        {
            Debug.Assert(this.IsSpatial);

            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellNorth(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.N);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellEast(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.E);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellSouth(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.S);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellWest(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.W);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellNortheast(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.NE);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellSoutheast(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.SE);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellSouthwest(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.SW);
            this.AddGrowEventRecord(eventCandidates, transitionedCells, seenBefore, initiationCell, this.GetCellNorthwest(initiationCell), transitionGroupId, iteration, timestep, travelTime, CardinalDirection.NW);
        }

        private void AddGrowEventRecord(GrowEventRecordCollection eventCandidates, Dictionary<int, Cell> transitionedCells, Dictionary<int, Cell> seenBefore, Cell initiationCell, Cell simulationCell, int transitionGroupId, int iteration, int timestep, double travelTime, CardinalDirection direction)
        {
            Debug.Assert(this.IsSpatial);

            if (simulationCell != null)
            {
                if ((!transitionedCells.ContainsKey(simulationCell.CellId)) & (!seenBefore.ContainsKey(simulationCell.CellId)))
                {
                    TransitionGroup tg = this.m_TransitionGroups[transitionGroupId];

                    if (tg.PatchPrioritization != null)
                    {
                        if (tg.PatchPrioritization.PatchPrioritizationType == PatchPrioritizationType.LargestEdgesOnly || tg.PatchPrioritization.PatchPrioritizationType == PatchPrioritizationType.SmallestEdgesOnly)
                        {
                            if (tg.PatchPrioritization.TransitionPatches.Count() == 0)
                            {
                                return;
                            }

                            TransitionPatch patch = tg.PatchPrioritization.TransitionPatches.First();

                            if (tg.PatchPrioritization.PatchPrioritizationType == PatchPrioritizationType.LargestEdgesOnly)
                            {
                                patch = tg.PatchPrioritization.TransitionPatches.Last();
                            }

                            if (!patch.EdgeCells.ContainsKey(simulationCell.CellId))
                            {
                                return;
                            }
                        }
                    }

                    double Probability = this.SpatialCalculateCellProbability(simulationCell, transitionGroupId, iteration, timestep);

                    if (Probability > 0.0)
                    {
                        double dist = this.GetNeighborCellDistance(direction);
                        double slope = GetSlope(initiationCell, simulationCell, dist);

                        double dirmult = this.m_TransitionDirectionMultiplierMap.GetDirectionMultiplier(
                            transitionGroupId, simulationCell.StratumId, simulationCell.SecondaryStratumId, simulationCell.TertiaryStratumId, 
                            direction, iteration, timestep);

                        double slopemult = this.m_TransitionSlopeMultiplierMap.GetSlopeMultiplier(
                            transitionGroupId, simulationCell.StratumId, simulationCell.SecondaryStratumId, simulationCell.TertiaryStratumId, 
                            iteration, timestep, slope);

                        double rate = slopemult * dirmult;

                        Debug.Assert(rate >= 0.0);

                        if (rate > 0.0)
                        {
                            double tt = travelTime + (dist / rate);
                            //DevToDo - Change variable name li to something more understandable.
                            double li = Probability / tt;

                            GrowEventRecord Record = new GrowEventRecord(simulationCell, tt, li);

                            eventCandidates.AddRecord(Record);
                            seenBefore.Add(simulationCell.CellId, simulationCell);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes the event that is nearest in size to the specified total event amount
        /// </summary>
        /// <param name="transitionEvents"></param>
        /// <param name="totalEventAmount"></param>
        /// <remarks>
        /// This function expects the transition events to be sorted in descending order by target amount.
        /// </remarks>
        private static void RemoveNearestSizedEvent(List<TransitionEvent> transitionEvents, double totalEventAmount)
        {
            if (transitionEvents.Count > 0)
            {
                TransitionEvent RemoveEvent = null;
                double CurrentDifference = double.MaxValue;

                foreach (TransitionEvent TransitionEvent in transitionEvents)
                {
                    double ThisDifference = Math.Abs(totalEventAmount - TransitionEvent.TargetAmount);

                    if (ThisDifference <= CurrentDifference)
                    {
                        RemoveEvent = TransitionEvent;
                        CurrentDifference = ThisDifference;
                    }
                    else
                    {
                        break;
                    }
                }

                Debug.Assert(RemoveEvent != null);
                transitionEvents.Remove(RemoveEvent);
            }
        }

        /// <summary>
        /// Create Spatial Initial Condition files and appropriate config based on Non-spatial Initial Condition configuration
        /// </summary>
        /// <remarks></remarks>
        private void CreateSpatialICFromNonSpatialIC()
        {
            DataRow drta = this.ResultScenario.GetDataSheet(Strings.DATASHEET_NSIC_NAME).GetDataRow();
            var CalcNumCellsFromDist = DataTableUtilities.GetDataBool(drta, Strings.DATASHEET_NSIC_CALC_FROM_DIST_COLUMN_NAME);

            if (CalcNumCellsFromDist)
            {
                this.CreateRastersFromNonRasterICCalcFromDist();
            }
            else
            {
                this.CreateRastersFromNonRasterICNoCalcFromDist();
            }
        }

        /// <summary>
        /// Create Spatial Initial Condition files and appropriate config based on Non-spatial
        /// Initial Condition configuration. Calculate Cell Area based on Distributtion
        /// </summary>
        /// <remarks></remarks>
        private void CreateRastersFromNonRasterICCalcFromDist()
        {
            // Fetch the number of cells from the NS IC setting
            DataRow drrc = this.ResultScenario.GetDataSheet(Strings.DATASHEET_NSIC_NAME).GetDataRow();
            int numCells = Convert.ToInt32(drrc[Strings.DATASHEET_NSIC_NUM_CELLS_COLUMN_NAME], CultureInfo.InvariantCulture);
            double ttlArea = Convert.ToDouble(drrc[Strings.DATASHEET_NSIC_TOTAL_AMOUNT_COLUMN_NAME], CultureInfo.InvariantCulture);

            CreateICSpatialProperties(numCells, ttlArea);

            // Get a list of the Iterations that are defined in the  InitialConditionsDistribution
            var lstIterations = this.m_InitialConditionsDistributions.GetSortedIterationList();

            CellCollection cells = new CellCollection();

            for (int CellId = 0; CellId < numCells; CellId++)
            {
                cells.Add(new Cell(CellId));
            }

            foreach (var iteration in lstIterations)
            {
                int cellIndex = 0;

                InitialConditionsDistributionCollection icds = this.m_InitialConditionsDistributions.GetForIteration(iteration);
                double sumOfRelativeAmountForIteration = CalcSumOfRelativeAmount(iteration);

                foreach (InitialConditionsDistribution icd in icds)
                {
                    // DEVNOTE:To support multiple iterations, use relativeAmount / sum For Iteration as scale of total number of cells. Number of cells determined by 1st iteration specified. 
                    // Otherwise, there's too much likelyhood that Number of cells will vary per iteration, which we cant/wont support.

                    int NumCellsICD = Convert.ToInt32(Math.Round(icd.RelativeAmount / sumOfRelativeAmountForIteration * numCells));
                    for (int i = 0; i < NumCellsICD; i++)
                    {
                        Cell c = cells[cellIndex];

                        int sisagemin = Math.Min(icd.AgeMin, icd.AgeMax);
                        int sisagemax = Math.Max(icd.AgeMin, icd.AgeMax);

                        int Iter = this.MinimumIteration;

                        if (iteration.HasValue)
                        {
                            Iter = iteration.Value;
                        }

                        this.InitializeCellAge(c, icd.StratumId, icd.StateClassId, sisagemin, sisagemax, Iter, this.m_TimestepZero);

                        c.StratumId = icd.StratumId;
                        c.StateClassId = icd.StateClassId;
                        c.SecondaryStratumId = icd.SecondaryStratumId;
                        c.TertiaryStratumId = icd.TertiaryStratumId;

                        cellIndex += 1;
                    }
                }

                // Randomize the cell distriubtion so we dont get blocks of same  ICD pixels.
                List<Cell> lst = new List<Cell>();
                foreach (Cell c in cells)
                {
                    lst.Add(c);
                }

                ShuffleUtilities.ShuffleList(lst, this.m_RandomGenerator.Random);
                SaveCellsToUndefinedICRasters(lst, iteration);
            }
        }

        /// <summary>
        /// Create Spatial Initial Condition files and appropriate config based on Non-spatial 
        /// Initial Condition configuration. Use entered Cell area (don't Calculate Cell Area based on Distributtion)
        /// </summary>
        /// <remarks></remarks>
        private void CreateRastersFromNonRasterICNoCalcFromDist()
        {
            // Fetch the number of cells from the NS IC setting
            DataRow drrc = this.ResultScenario.GetDataSheet(Strings.DATASHEET_NSIC_NAME).GetDataRow();
            int numCells = Convert.ToInt32(drrc[Strings.DATASHEET_NSIC_NUM_CELLS_COLUMN_NAME], CultureInfo.InvariantCulture);
            double ttlArea = Convert.ToDouble(drrc[Strings.DATASHEET_NSIC_TOTAL_AMOUNT_COLUMN_NAME], CultureInfo.InvariantCulture);

            CreateICSpatialProperties(numCells, ttlArea);

            // Get a list of the Iterations that are defined in the  InitialConditionsDistribution
            var lstIterations = this.m_InitialConditionsDistributions.GetSortedIterationList();

            foreach (var iteration in lstIterations)
            {
                var sumOfRelativeAmount = CalcSumOfRelativeAmount(iteration);

                InitialConditionsDistributionCollection icds = this.m_InitialConditionsDistributions.GetForIteration(iteration);

                CellCollection cells = new CellCollection();

                for (int CellId = 0; CellId < numCells; CellId++)
                {
                    cells.Add(new Cell(CellId));
                }

                foreach (Cell c in cells)
                {
                    double Rand = this.m_RandomGenerator.GetNextDouble();
                    double CumulativeProportion = 0.0;

                    foreach (InitialConditionsDistribution icd in icds)
                    {
                        CumulativeProportion += (icd.RelativeAmount / sumOfRelativeAmount);

                        if (Rand < CumulativeProportion)
                        {
                            int sisagemin = Math.Min(icd.AgeMin, icd.AgeMax);
                            int sisagemax = Math.Max(icd.AgeMin, icd.AgeMax);

                            int Iter = this.MinimumIteration;

                            if (iteration.HasValue)
                            {
                                Iter = iteration.Value;
                            }

                            this.InitializeCellAge(c, icd.StratumId, icd.StateClassId, sisagemin, sisagemax, Iter, this.m_TimestepZero);

                            c.StratumId = icd.StratumId;
                            c.StateClassId = icd.StateClassId;
                            c.SecondaryStratumId = icd.SecondaryStratumId;
                            c.TertiaryStratumId = icd.TertiaryStratumId;

                            break;
                        }
                    }
                }

                List<Cell> lst = new List<Cell>();
                foreach (Cell c in cells)
                {
                    lst.Add(c);
                }

                SaveCellsToUndefinedICRasters(lst, iteration);
            }
        }

        /// <summary>
        /// Create Spatial Initial Condition files and appropriate config based on a combination of Spatial 
        /// and Non-spatial Initial Condition configuration. 
        /// </summary>
        /// <remarks></remarks>
        private void CreateSpatialICFromCombinedIC()
        {
            DataSheet dsIC = this.ResultScenario.GetDataSheet(Strings.DATASHEET_SPIC_NAME);

            // Get a list of the Iterations that are defined in the InitialConditionsSpatials
            var lstIterations = this.m_InitialConditionsSpatials.GetSortedIterationList();
            bool StateClassDefined = false;
            bool SecondaryStratumDefined = false;
            bool TertiaryStratumDefined = false;
            bool AgeDefined = false;
            string sMsg = null;
            bool ICFilesCreated = false;

            foreach (var iteration in lstIterations)
            {
                InitialConditionsSpatial ics = this.m_InitialConditionsSpatialMap.GetICS(iteration);
                int[] primary_stratum_cells = null;
                string ssName = ics.SecondaryStratumFileName;
                string tsName = ics.TertiaryStratumFileName;
                string scName = ics.StateClassFileName;
                string ageName = ics.AgeFileName;
                int[] stateclass_cells = new int[1];
                int[] age_cells = new int[1];
                int[] secondary_stratum_cells = new int[1];
                int[] tertiary_stratum_cells = new int[1];
                DataSheet dsRemap = null;

                if (ics.PrimaryStratumFileName.Length == 0)
                {
                    throw new ArgumentException(MessageStrings.ERROR_SPATIAL_PRIMARY_STRATUM_FILE_NOT_DEFINED);
                }

                StateClassDefined = (!string.IsNullOrEmpty(scName));
                SecondaryStratumDefined = (!string.IsNullOrEmpty(ssName));
                TertiaryStratumDefined = (!string.IsNullOrEmpty(tsName));
                AgeDefined = (!string.IsNullOrEmpty(ageName));

                if (SecondaryStratumDefined && SecondaryStratumDefined && TertiaryStratumDefined && AgeDefined)
                {
                    // If all the Spatial files are already defined, then we've got nothing to do for this iteration
                    continue;
                }

                // So we've got a PS file defined, so lets load it up
                // Load the Primary Stratum Raster
                string rasterFileName = ics.PrimaryStratumFileName;
                string fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                StochasticTimeRaster raster = new StochasticTimeRaster();

                RasterFiles.LoadRasterFile(fullFileName, raster, RasterDataType.DTInteger);

                // Now lets remap the ID's in the raster to the Stratum PK values
                dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STRATA_NAME);
                primary_stratum_cells = RasterCells.RemapRasterCells(raster.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);

                // Load the State Class Raster, if defined
                if (StateClassDefined)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, ics.StateClassFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, raster, RasterDataType.DTInteger);

                    // Now lets remap the ID's in the raster to the State Class PK values
                    dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STATECLASS_NAME);
                    stateclass_cells = RasterCells.RemapRasterCells(raster.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);

                    if (stateclass_cells.Count() != primary_stratum_cells.Count())
                    {
                        throw new DataException(string.Format(CultureInfo.InvariantCulture, 
                            MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, fullFileName, "Different Cell Count"));
                    }
                }

                // Load the Age Raster, if defined
                if (AgeDefined)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, ics.AgeFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, raster, RasterDataType.DTInteger);

                    age_cells = raster.IntCells;

                    if (age_cells.Count() != primary_stratum_cells.Count())
                    {
                        throw new DataException(string.Format(CultureInfo.InvariantCulture, 
                            MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, fullFileName, "Different Cell Count"));
                    }
                }

                // Load the Secondary Stratum Raster, if defined
                if (SecondaryStratumDefined)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, ics.SecondaryStratumFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, raster, RasterDataType.DTInteger);

                    // Now lets remap the ID's in the raster to the Secondary Stratum PK values
                    dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_SECONDARY_STRATA_NAME);
                    secondary_stratum_cells = RasterCells.RemapRasterCells(raster.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);

                    if (secondary_stratum_cells.Count() != primary_stratum_cells.Count())
                    {
                        throw new DataException(string.Format(CultureInfo.InvariantCulture, 
                            MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, fullFileName, "Different Cell Count"));
                    }
                }

                // Load the Tertiary Stratum Raster, if defined
                if (TertiaryStratumDefined)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, ics.TertiaryStratumFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, raster, RasterDataType.DTInteger);

                    // Now lets remap the ID's in the raster to the Tertiary Stratum PK values
                    dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_TERTIARY_STRATA_NAME);
                    tertiary_stratum_cells = RasterCells.RemapRasterCells(raster.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);

                    if (tertiary_stratum_cells.Count() != primary_stratum_cells.Count())
                    {
                        throw new DataException(string.Format(CultureInfo.InvariantCulture, 
                            MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, fullFileName, "Different Cell Count"));
                    }
                }

                // Initalize a Cells collection
                CellCollection cells = new CellCollection();
                for (int CellId = 0; CellId < primary_stratum_cells.Count(); CellId++)
                {
                    Cell c = new Cell(CellId);
                    c.StratumId = primary_stratum_cells[CellId];

                    if (StateClassDefined)
                    {
                        c.StateClassId = stateclass_cells[CellId];
                    }
                    else
                    {
                        c.StateClassId = StochasticTimeRaster.DefaultNoDataValue;
                    }

                    if (AgeDefined)
                    {
                        c.Age = age_cells[CellId];
                    }
                    else
                    {
                        c.Age = StochasticTimeRaster.DefaultNoDataValue;
                    }

                    if (SecondaryStratumDefined)
                    {
                        c.SecondaryStratumId = secondary_stratum_cells[CellId];
                    }
                    else
                    {
                        c.SecondaryStratumId = StochasticTimeRaster.DefaultNoDataValue;
                    }

                    if (TertiaryStratumDefined)
                    {
                        c.TertiaryStratumId = tertiary_stratum_cells[CellId];
                    }
                    else
                    {
                        c.TertiaryStratumId = StochasticTimeRaster.DefaultNoDataValue;
                    }

                    cells.Add(c);
                }

                InitialConditionsDistributionCollection icds = this.m_InitialConditionsDistributionMap.GetICDs(iteration);

                if (icds == null)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, 
                        MessageStrings.STATUS_SPATIAL_RUN_USING_COMBINED_IC_MISSING_ICD, iteration.GetValueOrDefault());

                    this.RecordStatus(StatusType.Warning, sMsg);
                }
                else
                {
                    foreach (Cell c in cells)
                    {
                        if (c.StratumId != 0)
                        {
                            // Now lets filter the ICDs by Primary Stratum, and optionally Age, StateClass, and Secondary Stratum 
                            InitialConditionsDistributionCollection filteredICDs = icds.GetFiltered(c);

                            var sumOfRelativeAmount = filteredICDs.CalcSumOfRelativeAmount();

                            double Rand = this.m_RandomGenerator.GetNextDouble();
                            double CumulativeProportion = 0.0;

                            foreach (InitialConditionsDistribution icd in filteredICDs)
                            {
                                CumulativeProportion += (icd.RelativeAmount / sumOfRelativeAmount);

                                if (Rand < CumulativeProportion)
                                {
                                    if (!AgeDefined)
                                    {
                                        int sisagemin = Math.Min(icd.AgeMin, icd.AgeMax);
                                        int sisagemax = Math.Max(icd.AgeMin, icd.AgeMax);

                                        int Iter = this.MinimumIteration;

                                        if (iteration.HasValue)
                                        {
                                            Iter = iteration.Value;
                                        }

                                        this.InitializeCellAge(
                                            c, icd.StratumId, icd.StateClassId, 
                                            sisagemin, sisagemax, 
                                            Iter, this.m_TimestepZero);
                                    }

                                    c.StratumId = icd.StratumId;
                                    c.StateClassId = icd.StateClassId;
                                    c.SecondaryStratumId = icd.SecondaryStratumId;
                                    c.TertiaryStratumId = icd.TertiaryStratumId;

                                    break;
                                }
                            }
                        }
                    }
                }

                List<Cell> lst = new List<Cell>();
                foreach (Cell c in cells)
                {
                    lst.Add(c);
                }

                if (SaveCellsToUndefinedICRasters(lst, ics.Iteration))
                {
                    ICFilesCreated = true;
                }
            }

            if (ICFilesCreated)
            {
                this.RecordStatus(StatusType.Information, MessageStrings.STATUS_SPATIAL_RUN_USING_COMBINED_IC);
            }
        }

        /// <summary>
        /// Create a Initial Condition Spatial Properties record for the current Results Scenario, based on values dervied from Non-Spatial Initial condition settings
        /// </summary>
        /// <param name="numberOfCells">The number of cells </param>
        /// <param name="ttlArea">The total area</param>
        /// <remarks></remarks>
        private void CreateICSpatialProperties(int numberOfCells, double ttlArea)
        {
            // We want a square raster thats just big enough to accomodate the number of cells specified by user
            int numRasterCells = Convert.ToInt32(System.Math.Pow(Math.Ceiling(Math.Sqrt(numberOfCells)), 2));

            DataSheet dsSpicProp = this.ResultScenario.GetDataSheet(Strings.DATASHEET_SPPIC_NAME);
            DataRow drSpIcProp = dsSpicProp.GetDataRow();

            if (drSpIcProp == null)
            {
                drSpIcProp = dsSpicProp.GetData().NewRow();
                dsSpicProp.GetData().Rows.Add(drSpIcProp);
            }
            else
            {
                Debug.Assert(false, "We should not be here if there's already a IC Spatial Properties record defined");
            }

            // We need convert from Terminalogy Units to M2 for Raster.
            double cellSizeTermUnits = ttlArea / numberOfCells;

            string amountlabel = null;
            TerminologyUnit units = 0;

            TerminologyUtilities.GetAmountLabelTerminology(
                this.Project.GetDataSheet(Strings.DATASHEET_TERMINOLOGY_NAME), ref amountlabel, ref units);

            string cellSizeUnits = RasterCellSizeUnit.Meter.ToString();
            double convFactor = InitialConditionsSpatialDataSheet.CalcCellArea(1.0, cellSizeUnits, units);
            double cellArea = cellSizeTermUnits / convFactor;

            drSpIcProp[Strings.DATASHEET_SPPIC_NUM_ROWS_COLUMN_NAME] = Convert.ToInt32(Math.Sqrt(numRasterCells), CultureInfo.InvariantCulture);
            drSpIcProp[Strings.DATASHEET_SPPIC_NUM_COLUMNS_COLUMN_NAME] = Convert.ToInt32(Math.Sqrt(numRasterCells), CultureInfo.InvariantCulture);
            drSpIcProp[Strings.DATASHEET_SPPIC_NUM_CELLS_COLUMN_NAME] = numberOfCells;
            drSpIcProp[Strings.DATASHEET_SPPIC_CELL_AREA_COLUMN_NAME] = cellSizeTermUnits;
            drSpIcProp[Strings.DATASHEET_SPPIC_CELL_SIZE_COLUMN_NAME] = Convert.ToDecimal(Math.Sqrt(cellArea), CultureInfo.InvariantCulture);

            // Arbitrary values
            drSpIcProp[Strings.DATASHEET_SPPIC_CELL_SIZE_UNITS_COLUMN_NAME] = cellSizeUnits;
            drSpIcProp[Strings.DATASHEET_SPPIC_YLLCORNER_COLUMN_NAME] = 0;
            drSpIcProp[Strings.DATASHEET_SPPIC_XLLCORNER_COLUMN_NAME] = 0;

            // DEVNOTE: Set Projection  - Corresponds to NAD83 / UTM zone 12N EPSG:26912. Totally arbitrary, but need something to support units of Meters.
            drSpIcProp[Strings.DATASHEET_SPPIC_SRS_COLUMN_NAME] = "+proj=utm +zone=10 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs";
        }

        /// <summary>
        /// Save the values found in the List of Cells to Initial Conditions Spatial Input Raster files, using default naming templates. Note that the file 
        /// will only be saved in currently unspecified in the Initial Conditions Spatial datasheet. Also, update the appropriate
        /// file names in the  Initial Conditions Spatial datasheet.  
        /// </summary>
        /// <param name="cells">A List of Cell objects</param>
        /// <param name="iteration">The iteration that we are creating the raster file(s) for.</param>
        /// <returns>True is a raster file was saved</returns>
        /// <remarks>Raster files will only be created if not already defined in the Initial Conditions Spatial datasheet.</remarks>
        private bool SaveCellsToUndefinedICRasters(List<Cell> cells, int? iteration)
        {
            bool rasterSaved = false;

            int iterVal = 0;
            if (iteration == null)
            {
                iterVal = 0;
            }
            else
            {
                iterVal = iteration.Value;
            }

            // OK, we've got an Initialized cells collection. So now lets create the Initial Condition rasters as required.
            int numValidCells = cells.Count;
            StochasticTimeRaster rst = new StochasticTimeRaster();


            // Get the IC Spatial properties
            DataSheet dsSpIcProp = this.ResultScenario.GetDataSheet(Strings.DATASHEET_SPPIC_NAME);
            DataRow drProp = dsSpIcProp.GetDataRow();

            rst.ProjectionString = drProp[Strings.DATASHEET_SPPIC_SRS_COLUMN_NAME].ToString();
            rst.NumberCols = Convert.ToInt32(drProp[Strings.DATASHEET_SPPIC_NUM_COLUMNS_COLUMN_NAME], CultureInfo.InvariantCulture);
            rst.NumberRows = Convert.ToInt32(drProp[Strings.DATASHEET_SPPIC_NUM_ROWS_COLUMN_NAME], CultureInfo.InvariantCulture);
            rst.CellSize = Convert.ToDecimal(drProp[Strings.DATASHEET_SPPIC_CELL_SIZE_COLUMN_NAME], CultureInfo.InvariantCulture);
            rst.CellSizeUnits = drProp[Strings.DATASHEET_SPPIC_CELL_SIZE_UNITS_COLUMN_NAME].ToString();
            rst.XllCorner = Convert.ToDecimal(drProp[Strings.DATASHEET_SPPIC_XLLCORNER_COLUMN_NAME], CultureInfo.InvariantCulture);
            rst.YllCorner = Convert.ToDecimal(drProp[Strings.DATASHEET_SPPIC_YLLCORNER_COLUMN_NAME], CultureInfo.InvariantCulture);

            // We also need to get the datarow for this InitialConditionSpatial
            string filter = null;
            DataSheet dsSpatialIC = this.ResultScenario.GetDataSheet(Strings.DATASHEET_SPIC_NAME);
            DataRow drICS = null;

            if ((iteration == null))
            {
                filter = string.Format(CultureInfo.InvariantCulture, "iteration is null");
            }
            else
            {
                filter = string.Format(CultureInfo.InvariantCulture, "iteration={0}", iteration.Value);
            }

            DataRow[] drICSpatials = dsSpatialIC.GetData().Select(filter);

            if (drICSpatials.Count() == 0)
            {
                drICS = dsSpatialIC.GetData().NewRow();
                if (iteration.HasValue)
                {
                    drICS[Strings.DATASHEET_ITERATION_COLUMN_NAME] = iteration;
                }
                dsSpatialIC.GetData().Rows.Add(drICS);
            }
            else
            {
                drICS = drICSpatials[0];
            }

            DataSheet dsRemap = null;
            string filename = null;

            // Create Primary Stratum file,  if not already defined
            if (string.IsNullOrEmpty(drICS[Strings.DATASHEET_SPIC_STRATUM_FILE_COLUMN_NAME].ToString()))
            {
                rst.InitIntCells();
                for (var i = 0; i < numValidCells; i++)
                {
                    rst.IntCells[i] = cells[i].StratumId;
                }

                // We need to remap the Primary Stratum PK to the Raster values ( PK - > ID)
                dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STRATA_NAME);
                rst.IntCells = RasterCells.RemapRasterCells(rst.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME, false, StochasticTimeRaster.DefaultNoDataValue);

                filename = SpatialUtilities.SavePrimaryStratumInputRaster(rst, this.ResultScenario, iterVal, 0);
                File.SetAttributes(filename, FileAttributes.Normal);
                drICS[Strings.DATASHEET_SPIC_STRATUM_FILE_COLUMN_NAME] = Path.GetFileName(filename);
                dsSpatialIC.AddExternalInputFile(filename);
                rasterSaved = true;
            }


            // Create State Class IC raster, if not already defined
            if (string.IsNullOrEmpty(drICS[Strings.DATASHEET_SPIC_STATE_CLASS_FILE_COLUMN_NAME].ToString()))
            {
                rst.InitIntCells();
                for (var i = 0; i < numValidCells; i++)
                {
                    rst.IntCells[i] = cells[i].StateClassId;
                }

                // We need to remap the State Class PK to the Raster values ( PK - > ID)
                dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STATECLASS_NAME);
                rst.IntCells = RasterCells.RemapRasterCells(rst.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME, false, StochasticTimeRaster.DefaultNoDataValue);

                filename = SpatialUtilities.SaveStateClassInputRaster(rst, this.ResultScenario, iterVal, 0);
                File.SetAttributes(filename, FileAttributes.Normal);
                drICS[Strings.DATASHEET_SPIC_STATE_CLASS_FILE_COLUMN_NAME] = Path.GetFileName(filename);
                dsSpatialIC.AddExternalInputFile(filename);
                rasterSaved = true;
            }

            // Create Secondary Stratum IC raster , if appropriate and/or not already defined
            if (string.IsNullOrEmpty(drICS[Strings.DATASHEET_SPIC_SECONDARY_STRATUM_FILE_COLUMN_NAME].ToString()))
            {
                rst.InitIntCells();
                for (var i = 0; i < numValidCells; i++)
                {
                    if (cells[i].SecondaryStratumId.HasValue)
                    {
                        rst.IntCells[i] = cells[i].SecondaryStratumId.Value;
                    }
                }

                // Test the 2nd stratum has values worth exporting
                if (rst.IntCells.Distinct().Count() > 1 || rst.IntCells[0] != StochasticTimeRaster.DefaultNoDataValue)
                {
                    // We need to remap the Stratum PK to the Raster values ( PK - > ID)
                    dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_SECONDARY_STRATA_NAME);
                    rst.IntCells = RasterCells.RemapRasterCells(rst.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME, false, StochasticTimeRaster.DefaultNoDataValue);

                    filename = SpatialUtilities.SaveSecondaryStratumInputRaster(rst, this.ResultScenario, iterVal, 0);
                    File.SetAttributes(filename, FileAttributes.Normal);
                    drICS[Strings.DATASHEET_SPIC_SECONDARY_STRATUM_FILE_COLUMN_NAME] = Path.GetFileName(filename);
                    dsSpatialIC.AddExternalInputFile(filename);
                    rasterSaved = true;
                }
            }

            // Create Age IC raster , if not already defined
            if (string.IsNullOrEmpty(drICS[Strings.DATASHEET_SPIC_AGE_FILE_COLUMN_NAME].ToString()))
            {
                //Create Age IC Raster
                rst.InitIntCells();
                for (var i = 0; i < numValidCells; i++)
                {
                    rst.IntCells[i] = cells[i].Age;
                }

                filename = SpatialUtilities.SaveAgeInputRaster(rst, this.ResultScenario, iterVal, 0);
                File.SetAttributes(filename, FileAttributes.Normal);
                drICS[Strings.DATASHEET_SPIC_AGE_FILE_COLUMN_NAME] = Path.GetFileName(filename);
                dsSpatialIC.AddExternalInputFile(filename);
                rasterSaved = true;
            }

            return rasterSaved;
        }

        /// <summary>
        /// Calculates the cell probability
        /// </summary>
        /// <param name="simulationCell"></param>
        /// <param name="transitionGroupId"></param>
        /// <param name="iteration"></param>
        /// <param name="timestep"></param>
        /// <returns>If the probability excedes 1.0 then it returns 1.0</returns>
        /// <remarks></remarks>
        private double SpatialCalculateCellProbability(Cell simulationCell, int transitionGroupId, int iteration, int timestep)
        {
            Debug.Assert(this.IsSpatial);
            double CellProbability = this.SpatialCalculateCellProbabilityNonTruncated(simulationCell, transitionGroupId, iteration, timestep);

            if (CellProbability > 1.0)
            {
                CellProbability = 1.0;
            }

            return CellProbability;
        }

        /// <summary>
        /// Calculates the cell probability
        /// </summary>
        /// <param name="simulationCell"></param>
        /// <param name="transitionGroupId"></param>
        /// <param name="iteration"></param>
        /// <param name="timestep"></param>
        /// <returns></returns>
        /// <remarks>
        /// If the probability excedes 1 it will not be adjusted in any way.
        /// </remarks>
        private double SpatialCalculateCellProbabilityNonTruncated(Cell simulationCell, int transitionGroupId, int iteration, int timestep)
        {
            Debug.Assert(this.IsSpatial);
            double CellProbability = 0.0;
            TransitionGroup TransitionGroup = this.m_TransitionGroups[transitionGroupId];

            foreach (Transition tr in simulationCell.Transitions)
            {
                if (TransitionGroup.PrimaryTransitionTypes.Contains(tr.TransitionTypeId))
                {
                    double multiplier = GetTransitionMultiplier(tr.TransitionTypeId, iteration, timestep, simulationCell);
                    multiplier *= this.GetExternalTransitionMultipliers(tr.TransitionTypeId, iteration, timestep, simulationCell);

                    TransitionTarget target = this.m_TransitionTargetMap.GetTransitionTarget(
                        TransitionGroup.TransitionGroupId, simulationCell.StratumId, simulationCell.SecondaryStratumId,
                        simulationCell.TertiaryStratumId, iteration, timestep);

                    bool TargetPrioritizationMultiplierApplied = false;

                    if (target != null && !target.IsDisabled)
                    {
                        if (target.Prioritizations != null)
                        {
                            TransitionTargetPrioritization pri = target.GetPrioritization(
                                simulationCell.StratumId, simulationCell.SecondaryStratumId,
                                simulationCell.TertiaryStratumId, simulationCell.StateClassId);

                            if (pri != null)
                            {
                                if (pri.ProbabilityOverride.HasValue)
                                {
                                    Debug.Assert(pri.ProbabilityOverride.Value == 1.0 || pri.ProbabilityOverride.Value == 0.0);

                                    if (pri.ProbabilityOverride.Value == 1.0)
                                    {
                                        return 1.0;
                                    }
                                    else if (pri.ProbabilityOverride.Value == 0.0)
                                    {
                                        return 0.0;
                                    }
                                }
                            }
                            else
                            {
                                multiplier *= pri.ProbabilityMultiplier;
                                TargetPrioritizationMultiplierApplied = true;
                            }
                        }
                    }

                    if (!TargetPrioritizationMultiplierApplied)
                    {
                        multiplier *= this.GetTransitionTargetMultiplier(
                            TransitionGroup.TransitionGroupId, simulationCell.StratumId, simulationCell.SecondaryStratumId,
                            simulationCell.TertiaryStratumId, iteration, timestep);
                    }
                   
                    if (this.IsSpatial)
                    {
                        multiplier *= this.GetTransitionSpatialMultiplier(simulationCell.CellId, tr.TransitionTypeId, iteration, timestep);

                        TransitionType tt = this.m_TransitionTypes[tr.TransitionTypeId];

                        foreach (TransitionGroup tg in tt.TransitionGroups)
                        {
                            multiplier *= this.GetTransitionAdjacencyMultiplier(tg.TransitionGroupId, iteration, timestep, simulationCell);
                            multiplier *= this.GetExternalSpatialMultipliers(simulationCell, iteration, timestep, tg.TransitionGroupId);
                        }
                    }

                    if (this.m_TransitionAttributeTargets.Count > 0)
                    {
                        TransitionType tt = this.TransitionTypes[tr.TransitionTypeId];
                        multiplier = this.ModifyMultiplierForTransitionAttributeTarget(multiplier, tt, simulationCell, iteration, timestep);
                    }

                    CellProbability += tr.Probability * tr.Proportion * multiplier;
                }
            }

            return CellProbability;
        }

        /// <summary>
        /// Initializes all simulations cells in Raster mode
        /// </summary>
        /// <remarks></remarks>
        private void InitializeCellsRaster(int iteration)
        {
            Debug.Assert(this.IsSpatial);
            Debug.Assert(this.m_Cells.Count > 0);

            //Loop thru cells and set stratum(s),state class, and age.
            //Note that some cells in the raster don't have a valid state class or stratum.
            //We need to ignore these cells in this routine.

            for (int i = 0; i < this.m_InputRasters.NumberCells; i++)
            {
                // Skip a cell that wasnt initially created because of StateClass or Stratum = 0
                if (!this.m_Cells.Contains(i))
                {
                    continue;
                }

                Cell c = this.m_Cells[i];

                c.StateClassId = this.m_InputRasters.SClassCells[i];
                c.StratumId = this.m_InputRasters.StratumCells[i];

                Debug.Assert(!(c.StateClassId == 0 || c.StratumId == 0), "The Cell object should never have been created with StateClass or Stratum = 0");

                if (this.m_InputRasters.SecondaryStratumCells != null)
                {
                    c.SecondaryStratumId = this.m_InputRasters.SecondaryStratumCells[i];
                }

                if (this.m_InputRasters.TertiaryStratumCells != null)
                {
                    c.TertiaryStratumId = this.m_InputRasters.TertiaryStratumCells[i];
                }

                if (this.m_InputRasters.AgeCells == null)
                {
                    this.InitializeCellAge(c, c.StratumId, c.StateClassId, 0, int.MaxValue, iteration, this.m_TimestepZero);
                }
                else
                {
                    c.Age = this.m_InputRasters.AgeCells[i];
                    int ndv = this.m_InputRasters.NoDataValueAsInteger;

                    if (c.Age == ndv && ndv != 0)
                    {
                        this.InitializeCellAge(c, c.StratumId, c.StateClassId, 0, int.MaxValue, iteration, this.m_TimestepZero);
                    }
                    else
                    {
                        DeterministicTransition dt = this.GetDeterministicTransition(c, iteration, this.m_TimestepZero);

                        if (dt != null)
                        {
                            if (c.Age < dt.AgeMinimum || c.Age > dt.AgeMaximum)
                            {
                                c.Age = this.m_RandomGenerator.GetNextInteger(dt.AgeMinimum, dt.AgeMaximum);
                            }
                        }
                    }
                }

                this.InitializeCellTstValues(c, iteration);

#if DEBUG
                this.VALIDATE_INITIALIZED_CELL(c, iteration, this.m_TimestepZero);
#endif

                this.m_Strata[c.StratumId].Cells.Add(c.CellId, c);
                this.m_ProportionAccumulatorMap.AddOrIncrement(c.StratumId, c.SecondaryStratumId, c.TertiaryStratumId);

                this.OnSummaryStateClassOutput(c, iteration, this.m_TimestepZero);
                this.OnSummaryStateAttributeOutput(c, iteration, this.m_TimestepZero);

				if (CellInitialized != null)
                    CellInitialized(this, new CellEventArgs(c, iteration, this.m_TimestepZero));
            }

			if (CellsInitialized != null)
                CellsInitialized(this, new CellEventArgs(null, iteration, this.m_TimestepZero));
        }

        /// <summary>
        /// Fills the raster data if this is a raster model run
        /// </summary>
        /// <remarks></remarks>
        private void InitializeRasterData(int iteration)
        {
            Debug.Assert(this.IsSpatial);

            StochasticTimeRaster rastSclass = new StochasticTimeRaster();
            StochasticTimeRaster rastPrimaryStratum = new StochasticTimeRaster();
            StochasticTimeRaster rastSecondaryStratum = new StochasticTimeRaster();
            StochasticTimeRaster rastTertiaryStratum = new StochasticTimeRaster();
            StochasticTimeRaster rastAge = new StochasticTimeRaster();
            StochasticTimeRaster rastDem = new StochasticTimeRaster();
            InputRasters inpRasts = this.m_InputRasters;
            string sMsg = null;

            // Now import the rasters, if they are configured in the RasterInitialCondition 
            DataSheet dsIC = this.ResultScenario.GetDataSheet(Strings.DATASHEET_SPIC_NAME);

            InitialConditionsSpatial ics = this.m_InitialConditionsSpatialMap.GetICS(iteration);
            if (ics == null)
            {
                throw new ArgumentException(MessageStrings.ERROR_NO_APPLICABLE_INITIAL_CONDITIONS_SPATIAL_RECORDS);
            }

            // Load the State Class Raster
            string rasterFileName = null;
            string fullFileName = null;

            rasterFileName = ics.StateClassFileName;

            if (rasterFileName.Length > 0)
            {
                if (rasterFileName != inpRasts.StateClassName)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, rastSclass, RasterDataType.DTInteger);

                    inpRasts.StateClassName = rasterFileName;
                    // Now lets remap the ID's in the raster to the SClass PK values
                    DataSheet dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STATECLASS_NAME);
                    inpRasts.SClassCells = RasterCells.RemapRasterCells(rastSclass.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);
                }
            }
            else
            {
                // IC State Class file must be defined
                throw new ArgumentException(MessageStrings.ERROR_SPATIAL_FILE_NOT_DEFINED);
            }

            // Load the Primary Stratum Raster
            rasterFileName = ics.PrimaryStratumFileName;

            if (rasterFileName.Length > 0)
            {
                if (rasterFileName != inpRasts.PrimaryStratumName)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, rastPrimaryStratum, RasterDataType.DTInteger);

                    // Only set the metadata the 1st time thru
                    if (string.IsNullOrEmpty(inpRasts.PrimaryStratumName))
                    {
                        inpRasts.SetMetadata(rastPrimaryStratum);
                    }

                    inpRasts.PrimaryStratumName = rasterFileName;

                    // Now lets remap the ID's in the raster to the Stratum PK values
                    DataSheet dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_STRATA_NAME);
                    inpRasts.StratumCells = RasterCells.RemapRasterCells(rastPrimaryStratum.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);

                    // See if the Primary Stratum has a Projection associated with it
                    if (rastPrimaryStratum.ProjectionString == "")
                    {
                        sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISSING_PROJECTION_WARNING, fullFileName);
                        this.RecordStatus(StatusType.Information, sMsg);
                    }
                }
            }
            else
            {
                // IC Stratum file must be defined
                throw new ArgumentException(MessageStrings.ERROR_SPATIAL_FILE_NOT_DEFINED);
            }

            // Load the Secondary Stratum Raster
            rasterFileName = ics.SecondaryStratumFileName;

            if (rasterFileName.Length > 0)
            {
                if (rasterFileName != inpRasts.SecondaryStratumName)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, rastSecondaryStratum, RasterDataType.DTInteger);
                    inpRasts.SecondaryStratumName = rasterFileName;
                    // Now lets remap the ID's in the raster to the Secondary Stratum PK values
                    DataSheet dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_SECONDARY_STRATA_NAME);
                    inpRasts.SecondaryStratumCells = RasterCells.RemapRasterCells(rastSecondaryStratum.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);
                }
            }
            else
            {
                // IC Secondary Stratum file does not have to be defined
                inpRasts.SecondaryStratumName = "";
            }

            // Load the Tertiary Stratum Raster
            rasterFileName = ics.TertiaryStratumFileName;

            if (rasterFileName.Length > 0)
            {
                if (rasterFileName != inpRasts.TertiaryStratumName)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, rastTertiaryStratum, RasterDataType.DTInteger);
                    inpRasts.TertiaryStratumName = rasterFileName;
                    // Now lets remap the ID's in the raster to the Tertiary Stratum PK values
                    DataSheet dsRemap = this.Project.GetDataSheet(Strings.DATASHEET_TERTIARY_STRATA_NAME);
                    inpRasts.TertiaryStratumCells = RasterCells.RemapRasterCells(rastTertiaryStratum.IntCells, dsRemap, Strings.DATASHEET_MAPID_COLUMN_NAME);
                }
            }
            else
            {
                // IC Tertiary Stratum file does not have to be defined
                inpRasts.TertiaryStratumName = "";
            }

            // Load the Age Raster
            rasterFileName = ics.AgeFileName;

            if (rasterFileName.Length > 0)
            {
                if (rasterFileName != inpRasts.AgeName)
                {
                    fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                    RasterFiles.LoadRasterFile(fullFileName, rastAge, RasterDataType.DTInteger);
                    inpRasts.AgeName = rasterFileName;
                    inpRasts.AgeCells = rastAge.IntCells;
                }
            }
            else
            {
                inpRasts.AgeName = "";
            }

            // Load the Digital Elevation Model (DEM) Raster
            dsIC = this.ResultScenario.GetDataSheet(Strings.DATASHEET_DIGITAL_ELEVATION_MODEL_NAME);
            DataRow drRIS = dsIC.GetDataRow();

            if (drRIS != null)
            {
                rasterFileName = drRIS[Strings.DATASHEET_DIGITAL_ELEVATION_MODEL_FILE_NAME_COLUMN_NAME].ToString();

                if (rasterFileName.Length > 0)
                {
                    if (rasterFileName != inpRasts.DemName)
                    {
                        fullFileName = RasterFiles.GetInputFileName(dsIC, rasterFileName, false);
                        RasterFiles.LoadRasterFile(fullFileName, rastDem, RasterDataType.DTDouble);

                        inpRasts.DemName = rasterFileName;
                        inpRasts.DemCells = rastDem.DblCells;
                    }
                }
                else
                {
                    inpRasts.DemName = "";
                }
            }

            // Compare the rasters to make sure meta data matches. Note that we might not have loaded a raster 
            // because one of the same name already loaded for a previous iteration.

            CompareMetadataResult cmpResult = 0;
            string cmpMsg = "";

            // Primary Stratum
            if (rastPrimaryStratum.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastPrimaryStratum, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.PrimaryStratumName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.PrimaryStratumName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }

            // SClass is mandatory
            if (rastSclass.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastSclass, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.StateClassName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.StateClassName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }

            // Age
            if (rastAge.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastAge, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.AgeName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.AgeName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }

            //Secondary Stratum
            if (rastSecondaryStratum.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastSecondaryStratum, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.SecondaryStratumName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.SecondaryStratumName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }

            //Tertiary Stratum
            if (rastTertiaryStratum.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastTertiaryStratum, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.TertiaryStratumName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.TertiaryStratumName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }

            //DEM 
            if (rastDem.NumberCells > 0)
            {
                cmpResult = inpRasts.CompareMetadata(rastDem, ref cmpMsg);
                if (cmpResult == CompareMetadataResult.ImportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.ERROR_SPATIAL_FILE_MISMATCHED_METADATA, inpRasts.DemName, cmpMsg);
                    throw new STSimException(sMsg);
                }
                else if (cmpResult == CompareMetadataResult.UnimportantDifferences)
                {
                    sMsg = string.Format(CultureInfo.InvariantCulture, MessageStrings.STATUS_SPATIAL_FILE_MISMATCHED_METADATA_INFO, inpRasts.DemName, cmpMsg);
                    this.RecordStatus(StatusType.Information, sMsg);
                }
            }
        }

        /// <summary>
        /// Initializes the Annual Average Transition Probability Maps
        /// </summary>
        /// <remarks></remarks>
        private void InitializeAnnualAvgTransitionProbMaps()
        {
            Debug.Assert(this.IsSpatial);
            Debug.Assert(this.MinimumTimestep > 0);

            if (!this.m_CreateRasterAATPOutput)
            {
                return;
            }

            // Loop thru transition groups. 
            foreach (TransitionGroup tg in this.m_TransitionGroups)
            {
                //Make sure Primary
                if (tg.PrimaryTransitionTypes.Count == 0)
                {
                    continue;
                }

                Dictionary<int, double[]> dicTgAATP = new Dictionary<int, double[]>();

                // Loop thru timesteps
                for (var timestep = this.MinimumTimestep; timestep <= this.MaximumTimestep; timestep++)
                {
                    // Create a dictionary for this transtion group
                    // Create a aatp array object on Maximum Timestep and intervals of user spec'd freq.

                    if ((timestep == this.MaximumTimestep) || ((timestep - this.TimestepZero) % this.m_RasterAATPTimesteps) == 0)
                    {
                        double[] aatp = null;
                        aatp = new double[this.m_InputRasters.NumberCells];

                        // Initialize cells values
                        for (var i = 0; i < this.m_InputRasters.NumberCells; i++)
                        {
                            if (!this.Cells.Contains(i))
                            {
                                aatp[i] = StochasticTimeRaster.DefaultNoDataValue;
                            }
                            else
                            {
                                aatp[i] = 0;
                            }
                        }

                        dicTgAATP.Add(timestep, aatp);
                    }
                }

                this.m_AnnualAvgTransitionProbMap.Add(tg.TransitionGroupId, dicTgAATP);
            }
        }
    }
}