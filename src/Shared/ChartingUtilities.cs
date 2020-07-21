﻿// stsim: A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

using System;
using System.Linq;
using System.Data;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using SyncroSim.Core;
using SyncroSim.StochasticTime.Forms;

namespace SyncroSim.STSim
{
    internal static class ChartingUtilities
    {
        /// <summary>
        /// Determines if the specified descriptor has an age reference
        /// </summary>
        /// <param name="descriptor"></param>
        /// <returns></returns>
        public static bool HasAgeReference(ChartDescriptor descriptor)
        {
            if (descriptor.IncludeDataFilter != null && descriptor.IncludeDataFilter.Contains("AgeClass"))
            {
                return true;
            }

            if (descriptor.DisaggregateFilter != null && descriptor.DisaggregateFilter.Contains("AgeClass"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determinies if a descriptor in the specified collection has an age reference
        /// </summary>
        /// <param name="descriptors"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static bool HasAgeReference(ChartDescriptorCollection descriptors)
        {
            foreach (ChartDescriptor d in descriptors)
            {
                if (HasAgeReference(d))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates the age class column for the specified table
        /// </summary>
        /// <param name="store"></param>
        /// <param name="dataSheet"></param>
        /// <remarks></remarks>
        public static void UpdateAgeClassColumn(DataStore store, DataSheet dataSheet)
        {
            IEnumerable<AgeDescriptor> e = AgeUtilities.GetAgeDescriptors(dataSheet.Project);

            if (e == null)
            {
                return;
            }

            if (!AnyDataExists(store, dataSheet))
            {
                return;
            }

            if (!AgeDataExists(store, dataSheet))
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            Debug.Assert(!(e.ElementAtOrDefault(e.Count() - 1).MaximumAge.HasValue));

            if (e.Count() > 1)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, 
                    "UPDATE [{0}] SET AgeClass = CASE", 
                    dataSheet.Name);

                for (int i = 0; i < e.Count(); i++)
                {
                    AgeDescriptor d = e.ElementAtOrDefault(i);

                    if (d.MaximumAge.HasValue)
                    {
                        Debug.Assert(i < e.Count() - 1);

                        sb.AppendFormat(CultureInfo.InvariantCulture, 
                            " WHEN AgeMin >= {0} And AgeMax <= {1} THEN {2}", 
                            d.MinimumAge, d.MaximumAge.Value, d.MinimumAge);
                    }
                    else
                    {
                        Debug.Assert(i == e.Count() - 1);

                        sb.AppendFormat(CultureInfo.InvariantCulture, 
                            " WHEN AgeMin >= {0} THEN {1}", 
                            d.MinimumAge, d.MinimumAge);
                    }
                }

                sb.Append(" END");
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture,
                    "UPDATE [{0}] SET AgeClass = {1}", 
                    dataSheet.Name, e.ElementAtOrDefault(0).MinimumAge);
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, " WHERE ScenarioID = {0}", dataSheet.Scenario.Id);
            store.ExecuteNonQuery(sb.ToString());
        }

        public static DataTable CreateProportionChartData(Scenario scenario, ChartDescriptor descriptor, string tableName, DataStore store)
        {
            Dictionary<string, double> dict = CreateAmountDictionary(scenario, descriptor, store);

            if (dict.Count == 0)
            {
                return null;
            }

            string tag = GetChartCacheTag(descriptor);
            string query = CreateRawDataQuery(scenario, descriptor, tableName);
            DataTable dt = StochasticTime.ChartCache.GetCachedData(scenario, query, tag);

            if (dt == null)
            {
                dt = store.CreateDataTableFromQuery(query, "RawData");
                StochasticTime.ChartCache.SetCachedData(scenario, query, dt, tag);
            }

            foreach (DataRow dr in dt.Rows)
            {
                if (dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME] != DBNull.Value)
                {
                    int it = Convert.ToInt32(dr[Strings.DATASHEET_ITERATION_COLUMN_NAME], CultureInfo.InvariantCulture);
                    int ts = Convert.ToInt32(dr[Strings.DATASHEET_TIMESTEP_COLUMN_NAME], CultureInfo.InvariantCulture);

                    string k = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", it, ts);

                    dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME] = 
                        Convert.ToDouble(dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME], CultureInfo.InvariantCulture) / dict[k];
                }
            }

            return dt;
        }

        public static DataTable CreateRawAttributeChartData(
            Scenario scenario, 
            ChartDescriptor descriptor, 
            string tableName, 
            string attributeTypeColumnName, 
            int attributeTypeId, 
            bool isDensity, 
            DataStore store)
        {
            string tag = GetChartCacheTag(descriptor);
            string query = CreateRawAttributeDataQuery(scenario, descriptor, tableName, attributeTypeColumnName, attributeTypeId);
            DataTable dt = StochasticTime.ChartCache.GetCachedData(scenario, query, tag);

            if (dt == null)
            {
                dt = store.CreateDataTableFromQuery(query, "RawData");
                StochasticTime.ChartCache.SetCachedData(scenario, query, dt, tag);
            }
                          
            if (isDensity)
            {
                Dictionary<string, double> dict = CreateAmountDictionary(scenario, descriptor, store);

                if (dict.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        if (dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME] != DBNull.Value)
                        {
                            int it = Convert.ToInt32(dr[Strings.DATASHEET_ITERATION_COLUMN_NAME], CultureInfo.InvariantCulture);
                            int ts = Convert.ToInt32(dr[Strings.DATASHEET_TIMESTEP_COLUMN_NAME], CultureInfo.InvariantCulture);

                            string k = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", it, ts);

                            dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME] = 
                                Convert.ToDouble(dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME], CultureInfo.InvariantCulture) / dict[k];
                        }
                    }
                }
            }

            return dt;
        }

        public static DataTable CreateRawExternalVariableData(
            Scenario scenario,
            ChartDescriptor descriptor,
            DataStore store)
        {
            string tag = GetChartCacheTag(descriptor);
            string query = CreateRawExternalVariableDataQuery(scenario, descriptor);
            DataTable dt = StochasticTime.ChartCache.GetCachedData(scenario, query, tag);

            if (dt == null)
            {
                dt = store.CreateDataTableFromQuery(query, "RawData");
                StochasticTime.ChartCache.SetCachedData(scenario, query, dt, tag);
            }

            return dt;
        }

        public static Dictionary<string, double> CreateAmountDictionary(Scenario scenario, ChartDescriptor descriptor, DataStore store)
        {
            string tag = GetChartCacheTag(descriptor);
            Dictionary<string, double> dict = new Dictionary<string, double>();
            string query = CreateAmountQuery(scenario, descriptor);
            DataTable dt = StochasticTime.ChartCache.GetCachedData(scenario, query, tag);

            if (dt == null)
            {
                dt = store.CreateDataTableFromQuery(query, "AmountData");
                StochasticTime.ChartCache.SetCachedData(scenario, query, dt, tag);
            }

            foreach (DataRow dr in dt.Rows)
            {
                if (dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME] != DBNull.Value)
                {
                    int it = Convert.ToInt32(dr[Strings.DATASHEET_ITERATION_COLUMN_NAME], CultureInfo.InvariantCulture);
                    int ts = Convert.ToInt32(dr[Strings.DATASHEET_TIMESTEP_COLUMN_NAME], CultureInfo.InvariantCulture);

                    string k = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", it, ts);
                    dict.Add(k, Convert.ToDouble(dr[Strings.DATASHEET_SUMOFAMOUNT_COLUMN_NAME], CultureInfo.InvariantCulture));
                }
            }

            return dict;
        }

        private static string CreateAmountQuery(Scenario scenario, ChartDescriptor descriptor)
        {
            string ScenarioClause = string.Format(CultureInfo.InvariantCulture, 
                "([{0}]={1})", 
                Strings.DATASHEET_SCENARIOID_COLUMN_NAME, scenario.Id);

            string WhereClause = ScenarioClause;
            string Disagg = RemoveUnwantedColumnReferences(descriptor.DisaggregateFilter);
            string IncData = RemoveUnwantedColumnReferences(descriptor.IncludeDataFilter);

            if (!string.IsNullOrEmpty(Disagg))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} And ({1})", 
                    WhereClause, Disagg);
            }

            if (!string.IsNullOrEmpty(IncData))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} And ({1})", 
                    WhereClause, IncData);
            }

            string query = string.Format(CultureInfo.InvariantCulture,
                "SELECT Iteration, Timestep, SUM(Amount) AS SumOfAmount FROM stsim_OutputStratum WHERE ({0}) GROUP BY Iteration, Timestep", 
                WhereClause);

            return query;
        }

        private static string CreateRawDataQuery(Scenario scenario, ChartDescriptor descriptor, string tableName)
        {
            string ScenarioClause = string.Format(CultureInfo.InvariantCulture, 
                "([{0}]={1})",
                Strings.DATASHEET_SCENARIOID_COLUMN_NAME, scenario.Id);

            string WhereClause = ScenarioClause;

            if (!string.IsNullOrEmpty(descriptor.DisaggregateFilter))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} AND ({1})", 
                    WhereClause, descriptor.DisaggregateFilter);
            }

            if (!string.IsNullOrEmpty(descriptor.IncludeDataFilter))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} AND ({1})", 
                    WhereClause, descriptor.IncludeDataFilter);
            }

            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT Iteration, Timestep, SUM(Amount) AS SumOfAmount FROM {0} WHERE ({1}) GROUP BY Iteration, Timestep", 
                tableName, WhereClause);

            return query;
        }

        private static string CreateRawAttributeDataQuery(Core.Scenario scenario, ChartDescriptor descriptor, string tableName, string attributeTypeColumnName, int attributeTypeId)
        {
            string ScenarioClause = string.Format(CultureInfo.InvariantCulture,
                "([{0}]={1})", 
                Strings.DATASHEET_SCENARIOID_COLUMN_NAME, scenario.Id);

            string WhereClause = string.Format(CultureInfo.InvariantCulture, 
                "{0} AND ([{1}]={2})", 
                ScenarioClause, attributeTypeColumnName, attributeTypeId);

            if (!string.IsNullOrEmpty(descriptor.DisaggregateFilter))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} AND ({1})", 
                    WhereClause, descriptor.DisaggregateFilter);
            }

            if (!string.IsNullOrEmpty(descriptor.IncludeDataFilter))
            {
                WhereClause = string.Format(CultureInfo.InvariantCulture, 
                    "{0} AND ({1})",
                    WhereClause, descriptor.IncludeDataFilter);
            }

            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT Iteration, Timestep, SUM(Amount) AS SumOfAmount FROM {0} WHERE ({1}) GROUP BY Iteration, Timestep", 
                tableName, WhereClause);

            return query;
        }

        private static string CreateRawExternalVariableDataQuery(Core.Scenario scenario, ChartDescriptor descriptor)
        {
            string[] s = descriptor.VariableName.Split('-');
            Debug.Assert(s.Count() == 2 && s[0] == "stsim_ExternalVariable");
            int ExtVarTypeId = int.Parse(s[1], CultureInfo.InvariantCulture);

            string ScenarioClause = string.Format(CultureInfo.InvariantCulture,
                "([{0}]={1})",
                Strings.DATASHEET_SCENARIOID_COLUMN_NAME, scenario.Id);

            string WhereClause = string.Format(CultureInfo.InvariantCulture,
                "{0} AND ([{1}]={2})",
                ScenarioClause, Strings.OUTPUT_EXTERNAL_VARIABLE_VALUE_TYPE_ID_COLUMN_NAME, ExtVarTypeId);

            string query = string.Format(CultureInfo.InvariantCulture,
                "SELECT Iteration, Timestep, Value AS SumOfAmount FROM {0} WHERE ({1}) ORDER BY Iteration, Timestep",
                Strings.OUTPUT_EXTERNAL_VARIABLE_VALUE_DATASHEET_NAME, WhereClause);

            return query;
        }

        private static string RemoveUnwantedColumnReferences(string filter)
        {
            if (filter == null)
            {
                return null;
            }

            string[] AndSplit = filter.Split(new[] {" AND "}, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();

            foreach (string s in AndSplit)
            {
                if (s.Contains("StratumID") || s.Contains("SecondaryStratumID") | s.Contains("TertiaryStratumID"))
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0} AND ", s);
                }
            }

            string final = sb.ToString();

            if (final.Count() > 0)
            {
                Debug.Assert(final.Count() >= 5);
                return final.Substring(0, final.Length - 5);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if any data exists for the specified data sheet
        /// </summary>
        /// <param name="store"></param>
        /// <param name="dataSheet"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool AnyDataExists(DataStore store, DataSheet dataSheet)
        {
            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT COUNT(ScenarioID) FROM {0} WHERE ScenarioID = {1}", 
                dataSheet.Name, dataSheet.Scenario.Id);

            if (Convert.ToInt32(store.ExecuteScalar(query), CultureInfo.InvariantCulture) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Determines if any age data exists for the specified scenario
        /// </summary>
        /// <param name="store"></param>
        /// <param name="dataSheet"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool AgeDataExists(DataStore store, DataSheet dataSheet)
        {
            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT COUNT(AgeMin) FROM {0} WHERE ScenarioID = {1}", 
                dataSheet.Name, dataSheet.Scenario.Id);

            if (Convert.ToInt32(store.ExecuteScalar(query), CultureInfo.InvariantCulture) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Determines if the current age classes match the data
        /// </summary>
        /// <param name="store"></param>
        /// <param name="dataSheet"></param>
        /// <returns></returns>
        /// <remarks>
        /// Mismatched age bins which can occur due to a difference in the current age configuration
        /// and the age data that was generated when the scenario was run.
        /// </remarks>
        private static bool AgeClassesMatchData(DataStore store, DataSheet dataSheet)
        {
            //If any AgeClass values are NULL then the AgeMin and AgeMax did not fall into an AgeClass bin.
            //This could happen if the data contains values such as 10 - 19, but the frequency is 13.  In this
            //case the bins would be 0-12, 13-25, etc., and 10-19 does not go into any of these bins.

            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT COUNT(AgeMin) FROM {0} WHERE (AgeClass IS NULL) AND ScenarioID = {1}", 
                dataSheet.Name, dataSheet.Scenario.Id);

            if (Convert.ToInt32(store.ExecuteScalar(query), CultureInfo.InvariantCulture) != 0)
            {
                return false;
            }

            return true;
        }

        private static string GetChartCacheTag(ChartDescriptor descriptor)
        {
            if (HasAgeReference(descriptor))
            {
                return Constants.AGE_QUERY_CACHE_TAG;
            }
            else
            {
                return null;
            }
        }
    }
}
