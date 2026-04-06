namespace SV22T1020492.Admin
{
    /// <summary>
    /// Trả kết quả về cho lời gọi API
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiResult(int code, string message)
        {
            Code = code;
            Message = message;
        }


        /// <summary>
        /// Mã kết quả (qui ước 1: thành công, 0: không thành công)
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Thông báo lỗi (nếu có)
        /// </summary>
        public string Message { get; set; } = "";

    }
}
