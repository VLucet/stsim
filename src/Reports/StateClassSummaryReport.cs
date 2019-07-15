﻿// stsim: A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2019 Apex Resource Management Solutions Ltd. (ApexRMS). All rights reserved.

using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.Common;
using System.Globalization;
using System.Collections.Generic;
using SyncroSim.Core;
using SyncroSim.Core.Forms;
using SyncroSim.Common;

namespace SyncroSim.STSim
{
    internal class StateClassSummaryReport : ExportTransformer
    {
        private bool m_SecondaryStrataExist;
        private Dictionary<int, ScenarioData> m_ScenarioData;
        private MultiLevelKeyMap4<StratumAmount> m_PrimaryStratumAmountMap;
        private MultiLevelKeyMap5<StratumAmount> m_SecondaryStratumAmountMap;
        private bool m_MultiplePrimaryStrataExist;

        private const string CSV_INTEGER_FORMAT = "F0";
        private const string CSV_DOUBLE_FORMAT = "F4";

        protected override void Export(string location, ExportType exportType)
        {
            this.InternalExport(location, exportType, true);
        }

        internal void InternalExport(string location, ExportType exportType, bool showMessage)
        {
            using (DataStore store = this.Library.CreateDataStore())
            {
                this.FillScenarioData(store);

                if (this.m_ScenarioData.Count == 0)
                {
                    FormsUtilities.ErrorMessageBox("There is no data for the specified scenarios.");
                    return;
                }

                this.FillPrimaryStratumAmountMap(store);
                this.FillSecondaryStratumAmountMap(store);

                this.m_MultiplePrimaryStrataExist = this.MultiplePrimaryStrataExist();
                this.m_SecondaryStrataExist = this.AnySeconaryStrataExist();
            }

            if (exportType == ExportType.ExcelFile)
            {
                this.CreateExcelReport(location);
            }
            else
            {
                this.CreateCSVReport(location);

                if (showMessage)
                {
                    FormsUtilities.InformationMessageBox("Data saved to '{0}'.", location);
                }
            }
        }

        private ExportColumnCollection CreateColumnCollection()
        {
            ExportColumnCollection c = new ExportColumnCollection();

            string AmountLabel = null;
            string UnitsLabel = null;
            TerminologyUnit TermUnit = 0;
            string PrimaryStratumLabel = null;
            string SecondaryStratumLabel = null;
            string TertiaryStratumLabel = null;
            DataSheet dsterm = this.Project.GetDataSheet(Strings.DATASHEET_TERMINOLOGY_NAME);
            string TimestepLabel = TerminologyUtilities.GetTimestepUnits(this.Project);

            TerminologyUtilities.GetAmountLabelTerminology(dsterm, ref AmountLabel, ref TermUnit);
            TerminologyUtilities.GetStratumLabelTerminology(dsterm, ref PrimaryStratumLabel, ref SecondaryStratumLabel, ref TertiaryStratumLabel);
            UnitsLabel = TerminologyUtilities.TerminologyUnitToString(TermUnit);

            string AmountTitle = string.Format(CultureInfo.InvariantCulture, "{0} ({1})", AmountLabel, UnitsLabel);
            string Propn2Title = string.Format(CultureInfo.InvariantCulture, "Proportion of {0}", PrimaryStratumLabel);
            string Propn3Title = string.Format(CultureInfo.InvariantCulture, "Proportion of {0}/{1}", PrimaryStratumLabel, SecondaryStratumLabel);

            c.Add(new ExportColumn("ScenarioID", "Scenario ID"));
            c.Add(new ExportColumn("ScenarioName", "Scenario"));
            c.Add(new ExportColumn("Iteration", "Iteration"));
            c.Add(new ExportColumn("Timestep", TimestepLabel));
            c.Add(new ExportColumn("Stratum", PrimaryStratumLabel));
            c.Add(new ExportColumn("SecondaryStratum", SecondaryStratumLabel));
            c.Add(new ExportColumn("TertiaryStratum", TertiaryStratumLabel));
            c.Add(new ExportColumn("StateClass", "State Class"));
            c.Add(new ExportColumn("AgeMin", "Age Min"));
            c.Add(new ExportColumn("AgeMax", "Age Max"));
            c.Add(new ExportColumn("Amount", AmountTitle));
            c.Add(new ExportColumn("Proportion1", "Proportion of Landscape"));

            c["Amount"].DecimalPlaces = 2;
            c["Amount"].Alignment = ColumnAlignment.Right;

            c["Proportion1"].Alignment = ColumnAlignment.Right;
            c["Proportion1"].DecimalPlaces = 4;

            if (this.m_MultiplePrimaryStrataExist)
            {
                c.Add(new ExportColumn("Proportion2", Propn2Title));
                c["Proportion2"].Alignment = ColumnAlignment.Right;
                c["Proportion2"].DecimalPlaces = 4;
            }

            if (this.m_SecondaryStrataExist)
            {
                c.Add(new ExportColumn("Proportion3", Propn3Title));
                c["Proportion3"].Alignment = ColumnAlignment.Right;
                c["Proportion3"].DecimalPlaces = 4;
            }

            return c;
        }

        private void CreateExcelReport(string fileName)
        {
            string AmountLabel = null;
            TerminologyUnit AmountLabelUnits = 0;
            string ReportQuery = CreateReportQuery(false);
            DataTable ReportData = this.GetDataTableForReport(ReportQuery);
            DataSheet dsterm = this.Project.GetDataSheet(Strings.DATASHEET_TERMINOLOGY_NAME);

            TerminologyUtilities.GetAmountLabelTerminology(dsterm, ref AmountLabel, ref AmountLabelUnits);
            string WorksheetName = string.Format(CultureInfo.InvariantCulture, "{0} by State Class", AmountLabel);

            ExportTransformer.ExcelExport(fileName, this.CreateColumnCollection(), ReportData, WorksheetName);
        }

        private void CreateCSVReport(string fileName)
        {
            using (DataStore store = this.Library.CreateDataStore())
            {
                this.CreateCSVReport(fileName, store);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private void CreateCSVReport(string fileName, DataStore store)
        {
            string Propn2Title = "ProportionOfStratumID";
            string Propn3Title = "ProportionOfStratumIDOverSecondaryStratumID";

            using (StreamWriter sw = new StreamWriter(fileName, false))
            {
                sw.Write("ScenarioID,");
                sw.Write("Iteration,");
                sw.Write("Timestep,");
                sw.Write("StratumID,");
                sw.Write("SecondaryStratumID,");
                sw.Write("TertiaryStratumID,");
                sw.Write("StateClassID,");
                sw.Write("AgeMin,");
                sw.Write("AgeMax,");
                sw.Write("Amount,");
                sw.Write("ProportionOfLandscape");

                if (this.m_MultiplePrimaryStrataExist)
                {
                    sw.Write(",");
                    sw.Write(CSVFormatString(Propn2Title));
                }

                if (this.m_SecondaryStrataExist)
                {
                    sw.Write(",");
                    sw.Write(CSVFormatString(Propn3Title));
                }

                using (DbCommand cmd = store.CreateCommand())
                {
                    cmd.CommandText = CreateReportQuery(true);
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = store.DatabaseConnection;

                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            sw.Write(Environment.NewLine);

                            int ScenarioId = reader.GetInt32(0);
                            int Iteration = reader.GetInt32(1);
                            int Timestep = reader.GetInt32(2);
                            int StratumId = reader.GetInt32(3);
                            int? SecondaryStratumId = null;
                            int? TertiaryStratumId = null;
                            string SecondaryStratumName = null;
                            string TertiaryStratumName = null;

                            if (!reader.IsDBNull(4))
                            {
                                SecondaryStratumId = reader.GetInt32(4);
                            }

                            if (!reader.IsDBNull(5))
                            {
                                TertiaryStratumId = reader.GetInt32(5);
                            }

                            string StratumName = reader.GetString(6);

                            if (!reader.IsDBNull(7))
                            {
                                SecondaryStratumName = reader.GetString(7);
                            }

                            if (!reader.IsDBNull(8))
                            {
                                TertiaryStratumName = reader.GetString(8);
                            }

                            string StateClass = reader.GetString(9);
                            int? AgeMin = null;

                            if (!reader.IsDBNull(10))
                            {
                                AgeMin = reader.GetInt32(10);
                            }

                            int? AgeMax = null;

                            if (!reader.IsDBNull(11))
                            {
                                AgeMax = reader.GetInt32(11);
                            }

                            double Amount = reader.GetDouble(12);

                            sw.Write(CSVFormatInteger(ScenarioId));
                            sw.Write(",");

                            sw.Write(CSVFormatInteger(Iteration));
                            sw.Write(",");

                            sw.Write(CSVFormatInteger(Timestep));
                            sw.Write(",");

                            sw.Write(CSVFormatString(StratumName));
                            sw.Write(",");

                            if (SecondaryStratumName != null)
                            {
                                sw.Write(CSVFormatString(SecondaryStratumName));
                            }

                            sw.Write(",");

                            if (TertiaryStratumName != null)
                            {
                                sw.Write(CSVFormatString(TertiaryStratumName));
                            }

                            sw.Write(",");

                            sw.Write(CSVFormatString(StateClass));
                            sw.Write(",");

                            if (AgeMin.HasValue)
                            {
                                sw.Write(CSVFormatInteger(AgeMin.Value));
                            }

                            sw.Write(",");

                            if (AgeMax.HasValue)
                            {
                                sw.Write(CSVFormatInteger(AgeMax.Value));
                            }

                            sw.Write(",");

                            sw.Write(CSVFormatDouble(Amount));
                            sw.Write(",");

                            //Proportion1
                            if (this.m_ScenarioData[ScenarioId].TotalAmount == 0.0)
                            {
                                sw.Write("");
                            }
                            else
                            {
                                sw.Write(CSVFormatDouble(Amount / this.m_ScenarioData[ScenarioId].TotalAmount));
                            }

                            //Proportion2
                            if (this.m_MultiplePrimaryStrataExist)
                            {
                                sw.Write(",");

                                StratumAmount sa = this.m_PrimaryStratumAmountMap.GetItemExact(ScenarioId, StratumId, Iteration, Timestep);

                                if (sa == null || sa.Amount == 0.0)
                                {
                                    sw.Write("");
                                }
                                else
                                {
                                    sw.Write(CSVFormatDouble(Amount / sa.Amount));
                                }
                            }

                            //Proportion 3
                            if (this.m_SecondaryStrataExist)
                            {
                                sw.Write(",");

                                StratumAmount sa = this.m_SecondaryStratumAmountMap.GetItemExact(ScenarioId, StratumId, SecondaryStratumId, Iteration, Timestep);

                                if (sa == null || sa.Amount == 0.0)
                                {
                                    sw.Write("");
                                }
                                else
                                {
                                    sw.Write(CSVFormatDouble(Amount / sa.Amount));
                                }
                            }
                        }
                    }
                }
            }
        }

        private string CreateReportQuery(bool isCSV)
        {
            string ScenFilter = CreateIntegerFilter(this.m_ScenarioData.Keys);

            if (isCSV)
            {
                return string.Format(CultureInfo.InvariantCulture, 
                    "SELECT " + "stsim__OutputStratumState.ScenarioID, " + "stsim__OutputStratumState.Iteration,  " 
                    + "stsim__OutputStratumState.Timestep,  " + "stsim__OutputStratumState.StratumID, " + "stsim__OutputStratumState.SecondaryStratumID, " +
                    "stsim__OutputStratumState.TertiaryStratumID, " + "stsim__Stratum.Name AS Stratum,  " + "stsim__SecondaryStratum.Name AS SecondaryStratum,  " + 
                    "stsim__TertiaryStratum.Name AS TertiaryStratum,  " + "stsim__StateClass.Name as StateClass, " + "stsim__OutputStratumState.AgeMin, " + 
                    "stsim__OutputStratumState.AgeMax, " + "stsim__OutputStratumState.Amount " + "FROM stsim__OutputStratumState " + 
                    "INNER JOIN stsim__Stratum ON stsim__Stratum.StratumID = stsim__OutputStratumState.StratumID " + 
                    "LEFT JOIN stsim__SecondaryStratum ON stsim__SecondaryStratum.SecondaryStratumID = stsim__OutputStratumState.SecondaryStratumID " +
                    "LEFT JOIN stsim__TertiaryStratum ON stsim__TertiaryStratum.TertiaryStratumID = stsim__OutputStratumState.TertiaryStratumID " +
                    "INNER JOIN stsim__StateClass ON stsim__StateClass.StateClassID = stsim__OutputStratumState.StateClassID " + 
                    "WHERE stsim__OutputStratumState.ScenarioID IN ({0})  " + "ORDER BY " + "stsim__OutputStratumState.ScenarioID, " + 
                    "stsim__OutputStratumState.Iteration, " + "stsim__OutputStratumState.Timestep, " + "stsim__OutputStratumState.StratumID, " + 
                    "stsim__OutputStratumState.SecondaryStratumID, " + "stsim__OutputStratumState.TertiaryStratumID, " + "stsim__Stratum.Name, " +
                    "stsim__SecondaryStratum.Name, " + "stsim__TertiaryStratum.Name, " + "stsim__StateClass.Name, " + "AgeMin, " + "AgeMax", ScenFilter);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, 
                    "SELECT " + "stsim__OutputStratumState.ScenarioID, " + "system__Scenario.Name AS ScenarioName,  " + 
                    "stsim__OutputStratumState.Iteration,  " + "stsim__OutputStratumState.Timestep,  " +
                    "stsim__OutputStratumState.StratumID, " + "stsim__OutputStratumState.SecondaryStratumID, " +
                    "stsim__OutputStratumState.TertiaryStratumID, " + "stsim__Stratum.Name AS Stratum,  " + 
                    "stsim__SecondaryStratum.Name AS SecondaryStratum,  " + "stsim__TertiaryStratum.Name AS TertiaryStratum,  " + 
                    "stsim__StateClass.Name as StateClass, " + "stsim__OutputStratumState.AgeMin, " + "stsim__OutputStratumState.AgeMax, " + 
                    "stsim__OutputStratumState.Amount " + "FROM stsim__OutputStratumState " + 
                    "INNER JOIN system__Scenario ON system__Scenario.ScenarioID = stsim__OutputStratumState.ScenarioID " + 
                    "INNER JOIN stsim__Stratum ON stsim__Stratum.StratumID = stsim__OutputStratumState.StratumID " + 
                    "LEFT JOIN stsim__SecondaryStratum ON stsim__SecondaryStratum.SecondaryStratumID = stsim__OutputStratumState.SecondaryStratumID " +
                    "LEFT JOIN stsim__TertiaryStratum ON stsim__TertiaryStratum.TertiaryStratumID = stsim__OutputStratumState.TertiaryStratumID " + 
                    "INNER JOIN stsim__StateClass ON stsim__StateClass.StateClassID = stsim__OutputStratumState.StateClassID " + 
                    "WHERE stsim__OutputStratumState.ScenarioID IN ({0})  " + "ORDER BY " + "stsim__OutputStratumState.ScenarioID, " +
                    "system__Scenario.Name, " + "stsim__OutputStratumState.Iteration, " + "stsim__OutputStratumState.Timestep, " + 
                    "stsim__OutputStratumState.StratumID, " + "stsim__OutputStratumState.SecondaryStratumID, " + 
                    "stsim__OutputStratumState.TertiaryStratumID, " + "stsim__Stratum.Name, " + "stsim__SecondaryStratum.Name, " + 
                    "stsim__TertiaryStratum.Name, " + "stsim__StateClass.Name, " + "AgeMin, " + "AgeMax", ScenFilter);
            }
        }

        private DataTable GetDataTableForReport(string reportQuery)
        {
            if (this.m_ScenarioData.Count == 0)
            {
                return null;
            }

            using (DataStore store = this.Library.CreateDataStore())
            {
                DataTable dt = store.CreateDataTableFromQuery(reportQuery, "State Class Summary");

                dt.Columns.Add(new DataColumn("Proportion1", typeof(double)));
                dt.Columns.Add(new DataColumn("Proportion2", typeof(double)));
                dt.Columns.Add(new DataColumn("Proportion3", typeof(double)));

                foreach (DataRow dr in dt.Rows)
                {
                    int sid = Convert.ToInt32(dr["ScenarioID"], CultureInfo.InvariantCulture);

                    dr["Proportion1"] = DBNull.Value;
                    dr["Proportion2"] = DBNull.Value;
                    dr["Proportion3"] = DBNull.Value;

                    if (this.m_ScenarioData[sid].TotalAmount == 0.0)
                    {
                        dr["Proportion1"] = DBNull.Value;
                    }
                    else
                    {
                        dr["Proportion1"] = Convert.ToDouble(
                            dr["Amount"], CultureInfo.InvariantCulture) / this.m_ScenarioData[sid].TotalAmount;
                    }

                    StratumAmount sa = this.m_PrimaryStratumAmountMap.GetItemExact(
                        Convert.ToInt32(dr["ScenarioID"], CultureInfo.InvariantCulture), 
                        Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture),
                        Convert.ToInt32(dr["Iteration"], CultureInfo.InvariantCulture), 
                        Convert.ToInt32(dr["Timestep"], CultureInfo.InvariantCulture));

                    if (sa != null)
                    {
                        if (sa.Amount == 0.0)
                        {
                            Debug.Assert(false);
                            dr["Proportion2"] = DBNull.Value;
                        }
                        else
                        {
                            dr["Proportion2"] = Convert.ToDouble(dr["Amount"], CultureInfo.InvariantCulture) / sa.Amount;
                        }
                    }

                    if (dr["SecondaryStratumID"] != DBNull.Value)
                    {
                        sa = this.m_SecondaryStratumAmountMap.GetItemExact(
                            Convert.ToInt32(dr["ScenarioID"], CultureInfo.InvariantCulture), 
                            Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture), 
                            Convert.ToInt32(dr["SecondaryStratumID"], CultureInfo.InvariantCulture), 
                            Convert.ToInt32(dr["Iteration"], CultureInfo.InvariantCulture), 
                            Convert.ToInt32(dr["Timestep"], CultureInfo.InvariantCulture));

                        if (sa != null)
                        {
                            if (sa.Amount == 0.0)
                            {
                                Debug.Assert(false);
                                dr["Proportion3"] = DBNull.Value;
                            }
                            else
                            {
                                dr["Proportion3"] = Convert.ToDouble(
                                    dr["Amount"], CultureInfo.InvariantCulture) / sa.Amount;
                            }
                        }
                    }
                }

                return dt;
            }
        }

        /// <summary>
        /// Determines if there is OutputStratumState data for the specified scenario Id
        /// </summary>
        /// <param name="store"></param>
        /// <param name="scenarioId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool ScenarioHasData(DataStore store, int scenarioId)
        {
            object o1 = store.ExecuteScalar(string.Format(CultureInfo.InvariantCulture, "SELECT MIN(Iteration) FROM stsim__OutputStratumState WHERE ScenarioID={0}", scenarioId));
            object o2 = store.ExecuteScalar(string.Format(CultureInfo.InvariantCulture, "SELECT MIN(Timestep) FROM stsim__OutputStratumState WHERE ScenarioID={0}", scenarioId));

            return (o1 != DBNull.Value && o2 != DBNull.Value);
        }

        /// <summary>
        /// Gets the total area for the specified scenario Id
        /// </summary>
        /// <param name="store"></param>
        /// <param name="scenarioId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static double GetTotalArea(DataStore store, int scenarioId)
        {
            string q1 = string.Format(CultureInfo.InvariantCulture, "SELECT MIN(Iteration) FROM stsim__OutputStratumState WHERE ScenarioID={0}", scenarioId);
            string q2 = string.Format(CultureInfo.InvariantCulture, "SELECT MIN(Timestep) FROM stsim__OutputStratumState WHERE ScenarioID={0}", scenarioId);
            int Iteration = Convert.ToInt32(store.ExecuteScalar(q1), CultureInfo.InvariantCulture);
            int Timestep = Convert.ToInt32(store.ExecuteScalar(q2), CultureInfo.InvariantCulture);
            string q3 = string.Format(CultureInfo.InvariantCulture, "SELECT SUM(Amount) FROM stsim__OutputStratumState WHERE ScenarioID={0} AND Iteration={1} AND Timestep={2}", scenarioId, Iteration, Timestep);
            object o = store.ExecuteScalar(q3);

            if (o == DBNull.Value)
            {
                Debug.Assert(false);
                return 0.0;
            }
            else
            {
                return Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Fills the primary strata for the specified scenario Id
        /// </summary>
        /// <param name="store"></param>
        /// <param name="scenarioId"></param>
        /// <param name="sd"></param>
        /// <remarks></remarks>
        private static void FillPrimaryStrata(DataStore store, int scenarioId, ScenarioData sd)
        {
            Debug.Assert(sd.PrimaryStrata.Count == 0);

            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT DISTINCT StratumID FROM stsim__OutputStratumState WHERE ScenarioID={0}", 
                scenarioId);

            DataTable dt = store.CreateDataTableFromQuery(query, "DistinctStrata");

            foreach (DataRow dr in dt.Rows)
            {
                Debug.Assert(!sd.PrimaryStrata.Contains(Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture)));
                sd.PrimaryStrata.Add(Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Determines if there are any secondary strata for the specified scenario Id
        /// </summary>
        /// <param name="store"></param>
        /// <param name="scenarioId"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static bool HasSecondaryStrata(DataStore store, int scenarioId)
        {
            string query = string.Format(CultureInfo.InvariantCulture, 
                "SELECT DISTINCT SecondaryStratumID FROM stsim__OutputStratumState WHERE ScenarioID={0} AND SecondaryStratumID IS NOT NULL", 
                scenarioId);

            DataTable dt = store.CreateDataTableFromQuery(query, "DistinctSecondaryStrata");
            return (dt.Rows.Count > 0);
        }

        /// <summary>
        /// Determines if multiple primary strata exist across all result scenarios
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool MultiplePrimaryStrataExist()
        {
            Dictionary<int, bool> dict = new Dictionary<int, bool>();

            foreach (ScenarioData sd in this.m_ScenarioData.Values)
            {
                if (sd.PrimaryStrata.Count > 1)
                {
                    return true;
                }

                foreach (int id in sd.PrimaryStrata)
                {
                    if (!dict.ContainsKey(id))
                    {
                        dict.Add(id, true);
                    }
                }
            }

            return (dict.Count > 1);
        }

        /// <summary>
        /// Determines if any secondary strata exist
        /// </summary>
        /// <returns></returns>
        /// <remarks></remarks>
        private bool AnySeconaryStrataExist()
        {
            foreach (ScenarioData sd in this.m_ScenarioData.Values)
            {
                if (sd.HasSecondaryStrata)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fills the scenario data
        /// </summary>
        /// <param name="store"></param>
        /// <remarks></remarks>
        private void FillScenarioData(DataStore store)
        {
            this.m_ScenarioData = new Dictionary<int, ScenarioData>();

            foreach (Scenario s in this.GetActiveResultScenarios())
            {
                if (ScenarioHasData(store, s.Id))
                {
                    ScenarioData sd = new ScenarioData(GetTotalArea(store, s.Id));

                    FillPrimaryStrata(store, s.Id, sd);
                    sd.HasSecondaryStrata = HasSecondaryStrata(store, s.Id);

                    this.m_ScenarioData.Add(s.Id, sd);
                }
            }
        }

        /// <summary>
        /// Fills the primary stratum amount map
        /// </summary>
        /// <param name="store"></param>
        /// <remarks></remarks>
        private void FillPrimaryStratumAmountMap(DataStore store)
        {
            this.m_PrimaryStratumAmountMap = new MultiLevelKeyMap4<StratumAmount>();
            string query = string.Format(CultureInfo.InvariantCulture, "SELECT ScenarioID, Iteration, Timestep, StratumID, SUM(Amount) as SumOfAmount " + "FROM stsim__OutputStratum " + "WHERE ScenarioID IN ({0}) " + "GROUP BY ScenarioID, Iteration, Timestep, StratumID", CreateIntegerFilter(this.m_ScenarioData.Keys));
            DataTable dt = store.CreateDataTableFromQuery(query, "PrimaryStratumAmounts");

            foreach (DataRow dr in dt.Rows)
            {
                StratumAmount sa = new StratumAmount(Convert.ToDouble(dr["SumOfAmount"], CultureInfo.InvariantCulture));

                this.m_PrimaryStratumAmountMap.AddItem(
                    Convert.ToInt32(dr["ScenarioID"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["Iteration"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["Timestep"], CultureInfo.InvariantCulture), sa);
            }
        }

        /// <summary>
        /// Fills the secondary stratum amount map
        /// </summary>
        /// <param name="store"></param>
        /// <remarks></remarks>
        private void FillSecondaryStratumAmountMap(DataStore store)
        {
            this.m_SecondaryStratumAmountMap = new MultiLevelKeyMap5<StratumAmount>();
            string query = string.Format(CultureInfo.InvariantCulture, "SELECT ScenarioID, Iteration, Timestep, StratumID, SecondaryStratumID, Amount " + "FROM stsim__OutputStratum " + "WHERE ScenarioID IN ({0}) AND SecondaryStratumID IS NOT NULL", CreateIntegerFilter(this.m_ScenarioData.Keys));
            DataTable dt = store.CreateDataTableFromQuery(query, "PrimaryStratumAmounts");

            foreach (DataRow dr in dt.Rows)
            {
                StratumAmount sa = new StratumAmount(Convert.ToDouble(dr["Amount"], CultureInfo.InvariantCulture));

                this.m_SecondaryStratumAmountMap.AddItem(
                    Convert.ToInt32(dr["ScenarioID"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["StratumID"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["SecondaryStratumID"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["Iteration"], CultureInfo.InvariantCulture), 
                    Convert.ToInt32(dr["Timestep"], CultureInfo.InvariantCulture), sa);
            }
        }

        /// <summary>
        /// Creates a SQL filter from the specified integers
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        private static string CreateIntegerFilter(IEnumerable<int> values)
        {
            StringBuilder sb = new StringBuilder();

            foreach (int id in values)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0},", id);
            }

            Debug.Assert(values.Count() > 0);
            return sb.ToString().TrimEnd(',');
        }

        private static string CSVFormatInteger(Int32 value)
        {
            return value.ToString(CSV_INTEGER_FORMAT, CultureInfo.InvariantCulture);
        }

        private static string CSVFormatInteger(Int64 value)
        {
            return value.ToString(CSV_INTEGER_FORMAT, CultureInfo.InvariantCulture);
        }

        private static string CSVFormatDouble(double value)
        {
            return value.ToString(CSV_DOUBLE_FORMAT, CultureInfo.InvariantCulture);
        }

        private static string CSVFormatString(string value)
        {
            return InternalFormatStringCSV(value);
        }

        private static string InternalFormatStringCSV(string value)
        {
            bool ContainsComma = value.Contains(','.ToString());
            bool ContainsQuote = value.Contains('\"'.ToString());

            if (!ContainsComma && !ContainsQuote)
            {
                return value;
            }

            if (ContainsQuote)
            {
                string s = value.Replace("\"", "\"\"");
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", s);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", value);
            }
        }
    }

    /// <summary>
    /// Stratum Amount class
    /// </summary>
    /// <remarks>So we can have NULL references in the multi-level maps</remarks>
    internal class StratumAmount
    {
        private double m_Amount;

        public StratumAmount(double amount)
        {
            this.m_Amount = amount;
        }

        public double Amount
        {
            get
            {
                return this.m_Amount;
            }
        }
    }

    /// <summary>
    /// Scenario Data class
    /// </summary>
    /// <remarks></remarks>
    internal class ScenarioData
    {
        public double m_TotalAmount;
        public List<int> m_PrimaryStrata = new List<int>();
        public bool m_HasSecondaryStrata;

        public ScenarioData(double totalAmount)
        {
            this.m_TotalAmount = totalAmount;
        }

        public double TotalAmount
        {
            get
            {
                return this.m_TotalAmount;
            }
        }

        public List<int> PrimaryStrata
        {
            get
            {
                return this.m_PrimaryStrata;
            }
        }

        public bool HasSecondaryStrata
        {
            get
            {
                return this.m_HasSecondaryStrata;
            }
            set
            {
                this.m_HasSecondaryStrata = value;
            }
        }
    }
}
