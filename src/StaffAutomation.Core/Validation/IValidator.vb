Namespace Validation
    ''' <summary>
    ''' Generic Validator interface contract adhering to the Public API Stability Policy.
    ''' </summary>
    ''' <typeparam name="T">Type of object to validate</typeparam>
    Public Interface IValidator(Of T)
        Function Validate(instance As T) As ValidationResult
    End Interface
End Namespace
