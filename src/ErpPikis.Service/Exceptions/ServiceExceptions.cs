namespace ErpPikis.Service.Exceptions;

public class NoEncontradoException : Exception
{
    public NoEncontradoException(string message) : base(message)
    {
    }
}

public class ImpresoraException : Exception
{
    public ImpresoraException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("Usuario o contraseña incorrectos.")
    {
    }
}
