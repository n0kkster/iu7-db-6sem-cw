namespace Analyzer.Domain.Exceptions;

public class ComponentException(string messsage) : Exception(messsage)
{ }

public class InvalidComponentNameException(string message) : ComponentException(message)
{ }