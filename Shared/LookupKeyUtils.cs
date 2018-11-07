﻿// A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2018 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.

namespace SyncroSim.STSim
{
    internal static class LookupKeyUtils
    {
        public static int GetOutputCollectionKey(int? stratumId)
        {
            if (stratumId.HasValue)
            {
                return stratumId.Value;
            }
            else
            {
                return Constants.OUTPUT_COLLECTION_WILDCARD_KEY;
            }
        }
    }
}