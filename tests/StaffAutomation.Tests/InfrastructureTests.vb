Imports System
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports StaffAutomation.Core.Common
Imports StaffAutomation.Core.Constants
Imports StaffAutomation.Core.Exceptions
Imports StaffAutomation.Core.Validation

Namespace StaffAutomation.Tests
    ''' <summary>
    ''' Unit tests for Phase 3 Application Infrastructure and Security Foundation.
    ''' </summary>
    <TestClass>
    Public Class InfrastructureTests
        <TestMethod>
        Public Sub TestExceptionHierarchyInheritance()
            Dim ex As New BusinessException("Test Rule Violation", "BR-01")
            If Not (TypeOf ex Is StaffAutomationException) Then
                Throw New InvalidOperationException("BusinessException must inherit from StaffAutomationException.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestCommonValidators()
            Dim res1 = CommonValidators.ValidateRequired("", "TestField")
            If res1.IsValid Then
                Throw New InvalidOperationException("ValidateRequired failed to catch empty string.")
            End If

            Dim res2 = CommonValidators.ValidateLength("SampleText", 2, 20, "TestField")
            If Not res2.IsValid Then
                Throw New InvalidOperationException("ValidateLength failed on valid length string.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestNoMagicValuesConstants()
            If String.IsNullOrEmpty(AppConstants.AppName) OrElse String.IsNullOrEmpty(AppConstants.RoleNameOwner) Then
                Throw New InvalidOperationException("AppConstants must define required central values.")
            End If
        End Sub

        <TestMethod>
        Public Sub TestSystemDateTimeProvider()
            Dim clock As IDateTimeProvider = New SystemDateTimeProvider()
            If clock.UtcNow.Year < 2026 Then
                Throw New InvalidOperationException("SystemDateTimeProvider UtcNow returned an invalid past date.")
            End If
        End Sub
    End Class
End Namespace
