﻿// stsim: A SyncroSim Package for developing state-and-transition simulation models using ST-Sim.
// Copyright © 2007-2019 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.

using System;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using SyncroSim.Core;

namespace SyncroSim.STSim
{
    internal class TransitionGroupDataSheet : DataSheet
    {
        public override void Validate(object proposedValue, string columnName)
        {
            base.Validate(proposedValue, columnName);

            if (columnName == Strings.DATASHEET_NAME_COLUMN_NAME)
            {
                ValidateName(Convert.ToString(proposedValue, CultureInfo.InvariantCulture));
            }
        }

        public override void Validate(DataRow proposedRow, DataTransferMethod transferMethod)
        {
            base.Validate(proposedRow, transferMethod);
            ValidateName(Convert.ToString(proposedRow[Strings.DATASHEET_NAME_COLUMN_NAME], CultureInfo.InvariantCulture));
        }

        public override void Validate(DataTable proposedData, DataTransferMethod transferMethod)
        {
            base.Validate(proposedData, transferMethod);

            foreach (DataRow dr in proposedData.Rows)
            {
                if (!DataTableUtilities.GetDataBool(dr, Strings.IS_AUTO_COLUMN_NAME))
                {
                    ValidateName(Convert.ToString(dr[Strings.DATASHEET_NAME_COLUMN_NAME], CultureInfo.InvariantCulture));
                }
            }
        }

        public override void DeleteRows(IEnumerable<DataRow> rows)
        {
            List<DataRow> l = new List<DataRow>();

            foreach (DataRow dr in rows)
            {
                if (!DataTableUtilities.GetDataBool(dr, Strings.IS_AUTO_COLUMN_NAME))
                {
                    l.Add(dr);
                }
            }

            if (l.Count > 0)
            {
                base.DeleteRows(l);
            }
        }

        internal void DeleteAutoGeneratedRows(IEnumerable<DataRow> rows)
        {
            this.BeginDeleteRows();

            DataTable dt = this.GetData();

            foreach (DataRow dr in rows)
            {
                DataTableUtilities.DeleteTableRow(dt, dr);
            }

            this.EndDeleteRows();
        }

        private static void ValidateName(string name)
        {
            if (name.EndsWith(Strings.AUTO_COLUMN_SUFFIX, StringComparison.Ordinal))
            {
                string msg = string.Format(CultureInfo.InvariantCulture, 
                    "The transition group name cannot have the suffix: '{0}'.", 
                    Strings.AUTO_COLUMN_SUFFIX);

                throw new DataException(msg);
            }
        }
    }
}