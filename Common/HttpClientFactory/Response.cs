using System.Net;

namespace Common.HttpClientFactory;

public class Response<T>
{
    public bool IsSuccess { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public Exception Exception { get; set; }

    public Response(bool isSuccess, HttpStatusCode statusCode, T data, string message = null, Exception exception = null)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Data = data;
        Message = message;
        Exception = exception;
    }

    public Response()
    {
    }
}