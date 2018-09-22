﻿// ST-Sim: A SyncroSim Module for the ST-Sim State-and-Transition Model.
// Copyright © 2007-2018 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.

using System.Diagnostics;
using System.ComponentModel;
using SyncroSim.StochasticTime;

namespace SyncroSim.STSim
{
    public abstract class STSimDistributionBase
    {
        private bool InstanceFieldsInitialized = false;

        private void InitializeInstanceFields()
        {
            m_DistributionFrequency = DistributionFrequency.Timestep;
        }

        private int? m_Iteration;
        private int? m_Timestep;
        private int? m_StratumId;
        private int? m_SecondaryStratumId;
        private int? m_TertiaryStratumId;
        private double? m_DistributionValue;
        private int? m_DistributionTypeId;
        private DistributionFrequency m_DistributionFrequency;
        private double? m_DistributionSD;
        private double? m_DistributionMin;
        private double? m_DistributionMax;
        private double? m_CurrentValue;

        protected STSimDistributionBase
            (int? iteration, int? timestep, int? stratumId, int? secondaryStratumId, int? tertiaryStratumId, 
            double? distributionValue, int? distributionTypeId, DistributionFrequency? distributionFrequency, 
            double? distributionSD, double? distributionMin, double? distributionMax)
        {
            if (!InstanceFieldsInitialized)
            {
                InitializeInstanceFields();
                InstanceFieldsInitialized = true;
            }
            this.m_Iteration = iteration;
            this.m_Timestep = timestep;
            this.m_StratumId = stratumId;
            this.m_SecondaryStratumId = secondaryStratumId;
            this.m_TertiaryStratumId = tertiaryStratumId;
            this.m_DistributionValue = distributionValue;
            this.m_DistributionTypeId = distributionTypeId;
            this.m_DistributionSD = distributionSD;
            this.m_DistributionMin = distributionMin;
            this.m_DistributionMax = distributionMax;

            if (distributionFrequency.HasValue)
            {
                this.m_DistributionFrequency = distributionFrequency.Value;
            }
        }

        public int? Iteration
        {
            get
            {
                return this.m_Iteration;
            }
        }

        public int? Timestep
        {
            get
            {
                return this.m_Timestep;
            }
        }

        public int? StratumId
        {
            get
            {
                return this.m_StratumId;
            }
            set
            {
                this.m_StratumId = value;
            }
        }

        public int? SecondaryStratumId
        {
            get
            {
                return this.m_SecondaryStratumId;
            }
            set
            {
                this.m_SecondaryStratumId = value;
            }
        }

        public int? TertiaryStratumId
        {
            get
            {
                return this.m_TertiaryStratumId;
            }
            set
            {
                this.m_TertiaryStratumId = value;
            }
        }

        public double? DistributionValue
        {
            get
            {
                return this.m_DistributionValue;
            }
        }

        public int? DistributionTypeId
        {
            get
            {
                return this.m_DistributionTypeId;
            }
        }

        public DistributionFrequency DistributionFrequency
        {
            get
            {
                return this.m_DistributionFrequency;
            }
        }

        public double? DistributionSD
        {
            get
            {
                return this.m_DistributionSD;
            }
        }

        public double? DistributionMin
        {
            get
            {
                return this.m_DistributionMin;
            }
        }

        public double? DistributionMax
        {
            get
            {
                return this.m_DistributionMax;
            }
        }

        [Browsable(false)]
        public double? CurrentValue
        {
            get
            {
                Debug.Assert(this.m_CurrentValue.HasValue);
                return this.m_CurrentValue;
            }
        }

        public void Initialize(int iteration, int timestep, STSimDistributionProvider provider)
        {
            this.InternalInitialize(iteration, timestep, provider);
        }

        public double Sample(int iteration, int timestep, STSimDistributionProvider provider, DistributionFrequency frequency)
        {
            return this.InternalSample(iteration, timestep, provider, frequency);
        }

        public abstract STSimDistributionBase Clone();

        private void InternalInitialize(int iteration, int timestep, STSimDistributionProvider provider)
        {
            if (this.m_DistributionTypeId.HasValue)
            {
                int IterationToUse = iteration;
                int TimestepToUse = timestep;

                if (this.m_Iteration.HasValue)
                {
                    IterationToUse = this.m_Iteration.Value;
                }

                if (this.m_Timestep.HasValue)
                {
                    TimestepToUse = this.m_Timestep.Value;
                }

                this.m_CurrentValue = provider.STSimSample(
                    this.m_DistributionTypeId.Value, this.m_DistributionValue, this.m_DistributionSD, this.m_DistributionMin,
                    this.m_DistributionMax, IterationToUse, TimestepToUse, this.m_StratumId, this.m_SecondaryStratumId);
            }
            else
            {
                Debug.Assert(this.m_DistributionValue.HasValue);
                this.m_CurrentValue = this.m_DistributionValue.Value;
            }

            Debug.Assert(this.m_CurrentValue.HasValue);
        }

        private double InternalSample(int iteration, int timestep, STSimDistributionProvider provider, DistributionFrequency frequency)
        {
            if (this.m_DistributionTypeId.HasValue)
            {
                if (this.m_DistributionFrequency == frequency || this.m_DistributionFrequency == StochasticTime.DistributionFrequency.Always)
                {
                    this.m_CurrentValue = provider.STSimSample(
                        this.m_DistributionTypeId.Value, this.m_DistributionValue, this.m_DistributionSD, this.m_DistributionMin, 
                        this.m_DistributionMax, iteration, timestep, this.m_StratumId, this.m_SecondaryStratumId);
                }
            }

            Debug.Assert(this.m_CurrentValue.HasValue);
            return this.m_CurrentValue.Value;
        }
    }
}
