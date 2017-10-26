﻿'*********************************************************************************************
' ST-Sim: A SyncroSim Module for the ST-Sim State-and-Transition Model.
'
' Copyright © 2007-2017 Apex Resource Management Solution Ltd. (ApexRMS). All rights reserved.
'
'*********************************************************************************************

Imports SyncroSim.Core
Imports SyncroSim.Common

Friend Class TstRandomizeMap
    Inherits STSimMapBase

    Private m_map As New MultiLevelKeyMap5(Of SortedKeyMap1(Of TstRandomize))

    Public Sub New(ByVal scenario As Scenario)
        MyBase.New(scenario)
    End Sub

    Public Function GetTstRandomize(
        ByVal transitionGroupId As Nullable(Of Integer),
        ByVal stratumId As Nullable(Of Integer),
        ByVal secondaryStratumId As Nullable(Of Integer),
        ByVal tertiaryStratumId As Nullable(Of Integer),
        ByVal stateClassId As Nullable(Of Integer),
        ByVal iteration As Nullable(Of Integer)) As TstRandomize

        If (Not Me.HasItems) Then
            Return Nothing
        End If

        Dim m As SortedKeyMap1(Of TstRandomize) =
            Me.m_map.GetItem(transitionGroupId, stratumId, secondaryStratumId, tertiaryStratumId, stateClassId)

        If (m Is Nothing) Then
            Return Nothing
        End If

        Return m.GetItem(iteration)

    End Function

    Public Sub AddTstRandomize(
        ByVal transitionGroupId As Nullable(Of Integer),
        ByVal stratumId As Nullable(Of Integer),
        ByVal secondaryStratumId As Nullable(Of Integer),
        ByVal tertiaryStratumId As Nullable(Of Integer),
        ByVal stateClassId As Nullable(Of Integer),
        ByVal iteration As Nullable(Of Integer),
        ByVal tstRandomize As TstRandomize)

        Dim m As SortedKeyMap1(Of TstRandomize) =
            Me.m_map.GetItemExact(transitionGroupId, stratumId, secondaryStratumId, tertiaryStratumId, stateClassId)

        If (m Is Nothing) Then

            m = New SortedKeyMap1(Of TstRandomize)(SearchMode.ExactPrev)
            Me.m_map.AddItem(transitionGroupId, stratumId, secondaryStratumId, tertiaryStratumId, stateClassId, m)

        End If

        Dim v As TstRandomize = m.GetItemExact(iteration)

        If (v IsNot Nothing) Then

            Dim template As String =
                "A duplicate Time-Since-Transition Randomize value was detected: More information:" & vbCrLf &
                "Transition Group={0}, {1}={2}, {3}={4}, {5}={6}, State Class={7}, Iteration={8}."

            ExceptionUtils.ThrowArgumentException(
                template,
                Me.GetTransitionGroupName(transitionGroupId),
                Me.PrimaryStratumLabel,
                Me.GetStratumName(stratumId),
                Me.SecondaryStratumLabel,
                Me.GetSecondaryStratumName(secondaryStratumId),
                Me.TertiaryStratumLabel,
                Me.GetTertiaryStratumName(tertiaryStratumId),
                Me.GetStateClassName(stateClassId),
                STSimMapBase.FormatValue(iteration))

        End If

        m.AddItem(iteration, tstRandomize)
        Me.SetHasItems()

    End Sub

End Class


