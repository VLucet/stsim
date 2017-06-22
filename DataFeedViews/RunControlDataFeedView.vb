﻿'*********************************************************************************************
' ST-Sim: A SyncroSim Module for the ST-Sim State-and-Transition Model.
'
' Copyright © 2007-2017 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.
'
'*********************************************************************************************

Imports SyncroSim.Core
Imports System.Reflection
Imports System.Globalization

<ObfuscationAttribute(Exclude:=True, ApplyToMembers:=False)>
Class RunControlDataFeedView

    Public Overrides Sub LoadDataFeed(dataFeed As DataFeed)

        MyBase.LoadDataFeed(dataFeed)

        Me.SetTextBoxBinding(Me.TextBoxStartTimestep, "MinimumTimestep")
        Me.SetTextBoxBinding(Me.TextBoxEndTimestep, "MaximumTimestep")
        Me.SetTextBoxBinding(Me.TextBoxTotalIterations, "MaximumIteration")
        Me.SetCheckBoxBinding(Me.CheckBoxIsSpatial, "IsSpatial")

        Me.MonitorDataSheet(DATASHEET_TERMINOLOGY_NAME, AddressOf Me.OnTerminologyChanged, True)
        Me.AddStandardCommands()

    End Sub

    Private Sub OnTerminologyChanged(ByVal e As DataSheetMonitorEventArgs)

        Dim t As String = CStr(e.GetValue("TimestepUnits", "Timestep")).ToLower(CultureInfo.InvariantCulture)

        Me.LabelStartTimestep.Text = String.Format(CultureInfo.InvariantCulture, "Start {0}:", t)
        Me.LabelEndTimestep.Text = String.Format(CultureInfo.InvariantCulture, "End {0}:", t)

    End Sub

End Class
