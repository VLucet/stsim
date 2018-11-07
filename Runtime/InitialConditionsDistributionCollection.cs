﻿// A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2018 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.

using System;
using SyncroSim.StochasticTime;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SyncroSim.STSim
{
    /// <summary>
    /// Initial Conditions Distirbution collection
    /// </summary>
    internal class InitialConditionsDistributionCollection : Collection<InitialConditionsDistribution>
    {
        /// <summary>
        /// Get a collection of InitialConditionDistribution objects for the specified Iteration
        /// </summary>
        /// <param name="iteration"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public InitialConditionsDistributionCollection GetForIteration(int? iteration)
        {
            InitialConditionsDistributionCollection icds = new InitialConditionsDistributionCollection();
            foreach (SyncroSim.STSim.InitialConditionsDistribution icd in this)
            {
                if (Nullable.Equals(icd.Iteration, iteration))
                {
                    icds.Add(icd);
                }
            }

            return icds;
        }

        /// <summary>
        /// Get a Sorted List of Iterations contained in this collection
        /// </summary>
        /// <returns>A list of iterations</returns>
        /// <remarks></remarks>
        public List<int?> GetSortedIterationList()
        {
            List<int?> lstIterations = new List<int?>();
            foreach (InitialConditionsDistribution icd in this)
            {
                var iteration = icd.Iteration;
                if (!lstIterations.Contains(iteration))
                {
                    lstIterations.Add(iteration);
                }
            }

            //Sort Ascending with Null at start
            lstIterations.Sort();

            return lstIterations;
        }

        /// <summary>
        /// Get a Filtered Collection of InitialConditionsDistribution for specified parameter
        /// </summary>
        /// <returns>A Collection of InitialConditionsDistribution</returns>
        /// <remarks></remarks>
        public InitialConditionsDistributionCollection GetFiltered(Cell cell)
        {
            InitialConditionsDistributionCollection retVal = new InitialConditionsDistributionCollection();

            foreach (InitialConditionsDistribution icd in this)
            {
                if (cell.StratumId != icd.StratumId)
                {
                    continue;
                }

                if (cell.StateClassId != StochasticTimeRaster.DefaultNoDataValue)
                {
                    if (cell.StateClassId != icd.StateClassId)
                    {
                        continue;
                    }
                }

                if (cell.SecondaryStratumId != StochasticTimeRaster.DefaultNoDataValue)
                {
                    if (cell.SecondaryStratumId != icd.SecondaryStratumId)
                    {
                        continue;
                    }
                }

                if (cell.TertiaryStratumId != StochasticTimeRaster.DefaultNoDataValue)
                {
                    if (cell.TertiaryStratumId != icd.TertiaryStratumId)
                    {
                        continue;
                    }
                }

                if (cell.Age != StochasticTimeRaster.DefaultNoDataValue)
                {
                    if (cell.Age < icd.AgeMin || cell.Age > icd.AgeMax)
                    {
                        continue;
                    }
                }

                // Passed all the tests, so we'll take this one
                retVal.Add(icd);
            }

            return retVal;
        }

        public double CalcSumOfRelativeAmount()
        {
            double sumOfRelativeAmount = 0.0;

            foreach (InitialConditionsDistribution sis in this)
            {
                sumOfRelativeAmount += sis.RelativeAmount;
            }

            return sumOfRelativeAmount;
        }
    }
}