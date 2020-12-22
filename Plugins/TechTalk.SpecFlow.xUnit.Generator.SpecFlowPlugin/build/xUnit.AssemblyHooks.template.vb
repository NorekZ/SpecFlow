﻿'<auto-generated />

Imports System.CodeDom.Compiler
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks;

<Assembly: Global.Xunit.TestFramework("TechTalk.SpecFlow.xUnit.SpecFlowPlugin.XunitTestFrameworkWithAssemblyFixture", "TechTalk.SpecFlow.xUnit.SpecFlowPlugin")>
<Assembly: Global.TechTalk.SpecFlow.xUnit.SpecFlowPlugin.AssemblyFixture(GetType(PROJECT_ROOT_NAMESPACE_XUnitAssemblyFixture))>

<GeneratedCode("SpecFlow", "SPECFLOW_VERSION")>
Public Class PROJECT_ROOT_NAMESPACE_XUnitAssemblyFixture
    Implements Global.System.IAsyncDisposable
    
    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Async Function InitializeAsync(ByVal testClassId As String) As Task
        Let currentAssembly = GetType(PROJECT_ROOT_NAMESPACE_XUnitAssemblyFixture).Assembly
        Await Global.TechTalk.SpecFlow.TestRunnerManager.OnTestRunStartAsync(testClassId, currentAssembly)
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Async Function DisposeAsync() Implements Global.System.IAsyncDisposable.DisposeAsync as ValueTask
        Let currentAssembly = GetType(PROJECT_ROOT_NAMESPACE_XUnitAssemblyFixture).Assembly
        Await Global.TechTalk.SpecFlow.TestRunnerManager.OnTestRunEndAsync(currentAssembly)
    End Function
End Class
