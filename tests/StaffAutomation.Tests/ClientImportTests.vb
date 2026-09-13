Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Moq
Imports StaffAutomation.BLL.Services
Imports StaffAutomation.Core.DTOs
Imports StaffAutomation.Core.Entities
Imports StaffAutomation.Core.Enums
Imports StaffAutomation.Core.Logging
Imports StaffAutomation.DAL.Interfaces
Imports StaffAutomation.Core.Security
Imports StaffAutomation.Core.Interfaces

Namespace StaffAutomation.Tests
    <TestClass>
    Public Class ClientImportTests
        Private _mockRepo As Mock(Of IClientRepository)
        Private _mockAppLogger As Mock(Of IAppLogger)
        Private _service As ClientService

        <TestInitialize>
        Public Sub Setup()
            _mockRepo = New Mock(Of IClientRepository)()
            _mockAppLogger = New Mock(Of IAppLogger)()
            Dim mockSqlHelper = New Mock(Of ISqlHelper)()
            Dim mockAuditLogger = New BLL.Logging.AuditLogger(mockSqlHelper.Object)
            _service = New ClientService(_mockRepo.Object, _mockAppLogger.Object, mockAuditLogger)

            ' Mock authentication for tests
            CurrentUserContext.CurrentUser = New UserDto() With {.UserId = 99, .UserName = "TestUser", .Role = UserRole.Owner}
        End Sub

        <TestCleanup>
        Public Sub Cleanup()
            CurrentUserContext.ClearSession()
        End Sub

        <TestMethod>
        Public Async Function Test_NullableDepartment_CanBeCreated() As Task
            Dim createdClient As ClientEntity = Nothing
            _mockRepo.Setup(Function(r) r.AddAsync(It.IsAny(Of ClientEntity)())).
                Callback(Sub(c As ClientEntity) createdClient = c).
                ReturnsAsync(1)
            
            _mockRepo.Setup(Function(r) r.GetAllAsync(Nothing, False)).ReturnsAsync(New List(Of ClientEntity)())

            Dim dto As New ClientDto() With {
                .ClientCode = "NULL-TEST",
                .ClientName = "Null Dept Client",
                .Department = Nothing
            }

            Await _service.CreateClientAsync(dto)

            Assert.IsNotNull(createdClient)
            Assert.IsFalse(createdClient.Department.HasValue, "Department should be NULL")
        End Function

        <TestMethod>
        Public Async Function Test_NullableDepartment_CanBeUpdated() As Task
            Dim updatedClient As ClientEntity = Nothing
            _mockRepo.Setup(Function(r) r.UpdateAsync(It.IsAny(Of ClientEntity)())).
                Callback(Sub(c As ClientEntity) updatedClient = c).
                ReturnsAsync(True)
            _mockRepo.Setup(Function(r) r.GetByIdAsync(1)).ReturnsAsync(New ClientEntity() With {.ClientId = 1, .Department = DepartmentType.IncomeTax})

            Dim dto As New ClientDto() With {
                .ClientId = 1,
                .ClientCode = "NULL-UPDATE",
                .ClientName = "Update Null Client",
                .Department = Nothing
            }

            Await _service.UpdateClientAsync(dto)

            Assert.IsNotNull(updatedClient)
            Assert.IsFalse(updatedClient.Department.HasValue, "Department should have been set to NULL upon update")
        End Function

        <TestMethod>
        Public Async Function Test_ExcelImport_CreatesClientsWithNullDepartmentAndCorrectCreatedBy() As Task
            Dim bulkClients As IEnumerable(Of ClientEntity) = Nothing
            _mockRepo.Setup(Function(r) r.AddBulkAsync(It.IsAny(Of IEnumerable(Of ClientEntity))())).
                Callback(Sub(c As IEnumerable(Of ClientEntity)) bulkClients = c).
                ReturnsAsync(2)

            Dim dtos As New List(Of ClientDto)() From {
                New ClientDto() With {.ClientCode = "IMP-1", .ClientName = "Imp 1", .Department = Nothing},
                New ClientDto() With {.ClientCode = "IMP-2", .ClientName = "Imp 2", .Department = Nothing}
            }

            Dim result = Await _service.CreateClientsBulkAsync(dtos)
            Assert.AreEqual(2, result)
            Assert.IsNotNull(bulkClients)

            For Each client In bulkClients
                Assert.IsFalse(client.Department.HasValue, "Bulk imported client should have NULL department")
                Assert.AreEqual(99, client.CreatedBy, "CreatedBy should equal the authenticated user ID (not hardcoded)")
            Next
        End Function

        <TestMethod>
        Public Async Function Test_ExistingDepartmentSpecific_ClientRecordsStillWork() As Task
            Dim fetchedClient = New ClientEntity() With {
                .ClientId = 5,
                .ClientCode = "DEP-TEST",
                .Department = DepartmentType.Accounting
            }
            _mockRepo.Setup(Function(r) r.GetByIdAsync(5)).ReturnsAsync(fetchedClient)

            Dim result = Await _service.GetClientByIdAsync(5)
            Assert.IsNotNull(result)
            Assert.IsTrue(result.Department.HasValue)
            Assert.AreEqual(DepartmentType.Accounting, result.Department.Value)
        End Function

        <TestMethod>
        Public Async Function Test_DepartmentFilter_WorksWhenExplicitlySupplied() As Task
            _mockRepo.Setup(Function(r) r.GetAllAsync(DepartmentType.IncomeTax, False)).
                ReturnsAsync(New List(Of ClientEntity)() From {New ClientEntity() With {.Department = DepartmentType.IncomeTax}})

            Dim results = Await _service.GetAllClientsAsync(DepartmentType.IncomeTax)
            
            Assert.IsNotNull(results)
            Assert.AreEqual(1, results.Count)
            Assert.AreEqual(DepartmentType.IncomeTax, results(0).Department)
            _mockRepo.Verify(Function(r) r.GetAllAsync(DepartmentType.IncomeTax, False), Times.Once())
        End Function
        
        <TestMethod>
        <ExpectedException(GetType(UnauthorizedAccessException))>
        Public Async Function Test_CreatedBy_NeverHardcoded() As Task
            _mockRepo.Setup(Function(r) r.GetAllAsync(Nothing, False)).ReturnsAsync(New List(Of ClientEntity)())
            CurrentUserContext.ClearSession() ' No user is authenticated
            Dim dto As New ClientDto() With {
                .ClientCode = "FAIL-TEST",
                .ClientName = "Fail Dept Client",
                .Department = Nothing
            }

            Await _service.CreateClientAsync(dto) ' Should throw
        End Function
    End Class
End Namespace
