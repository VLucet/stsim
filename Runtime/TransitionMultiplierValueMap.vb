﻿'*********************************************************************************************
' ST-Sim: A SyncroSim Module for the ST-Sim State-and-Transition Model.
'
' Copyright © 2007-2017 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.
'
'*********************************************************************************************

Imports SyncroSim.Core
Imports SyncroSim.StochasticTime

Class TransitionMultiplierValueMap
    Inherits STSimMapBase5(Of TransitionMultiplierValue)

    Private m_DistributionProvider As STSimDistributionProvider

    Public Sub New(
        ByVal scenario As Scenario,
        ByVal multipliers As TransitionMultiplierValueCollection,
        ByVal distributionProvider As STSimDistributionProvider)

        MyBase.New(scenario)

        Me.m_DistributionProvider = distributionProvider

        For Each item As TransitionMultiplierValue In multipliers
            Me.TryAddMultiplier(item)
        Next

    End Sub

    Public Function GetTransitionMultiplier(
        ByVal transitionGroupId As Integer,
        ByVal stratumId As Integer,
        ByVal secondaryStratumId As Nullable(Of Integer),
        ByVal tertiaryStratumId As Nullable(Of Integer),
        ByVal stateClassId As Integer,
        ByVal iteration As Integer,
        ByVal timestep As Integer) As TransitionMultiplierValue

        Dim v As TransitionMultiplierValue = Me.GetItem(
            transitionGroupId,
            stratumId,
            secondaryStratumId,
            tertiaryStratumId,
            stateClassId,
            iteration,
            timestep)

        If (v IsNot Nothing) Then

            v.Sample(
                iteration,
                timestep,
                Me.m_DistributionProvider,
                DistributionFrequency.Always)

        End If

        Return v

    End Function

    Private Sub TryAddMultiplier(ByVal item As TransitionMultiplierValue)

        Try

            MyBase.AddItem(
                item.TransitionGroupId,
                item.StratumId,
                item.SecondaryStratumId,
                item.TertiaryStratumId,
                item.StateClassId,
                item.Iteration,
                item.Timestep,
                item)

        Catch ex As STSimMapDuplicateItemException

            Dim template As String =
                "A duplicate transition multiplier value was detected: More information:" & vbCrLf &
                "Transition Group={0}, {1}={2}, {3}={4}, {5}={6}, State Class={7}, Iteration={8}, Timestep={9}." & vbCrLf &
                "NOTE: A user defined distribution can result in additional transition multiplier values when the model is run."

            ExceptionUtils.ThrowArgumentException(
                template,
                Me.GetTransitionGroupName(item.TransitionGroupId),
                Me.PrimaryStratumLabel,
                Me.GetStratumName(item.StratumId),
                Me.SecondaryStratumLabel,
                Me.GetSecondaryStratumName(item.SecondaryStratumId),
                Me.TertiaryStratumLabel,
                Me.GetTertiaryStratumName(item.TertiaryStratumId),
                Me.GetStateClassName(item.StateClassId),
                STSimMapBase.FormatValue(item.Iteration),
                STSimMapBase.FormatValue(item.Timestep))

        End Try

    End Sub

End Class
