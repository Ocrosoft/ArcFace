namespace ServerCore.Models
{
    public class ApiResult
    {
        public ApiResult()
        {
            code = 0;
            message = "";
            data = null;
        }

        public int code { get; set; }
        public string message { get; set; }
        public object data { get; set; }
    }
}