namespace CMSECommerce.Areas.Admin.Services
{
    public class ServiceResponse
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
        public int StatusCode { get; set; }

        public static ServiceResponse Success(string message) =>
            new() { Succeeded = true, Message = message, StatusCode = 200 };

        public static ServiceResponse Failure(string message, int code = 400) =>
            new() { Succeeded = false, Message = message, StatusCode = code };
    }
}
