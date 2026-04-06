namespace SV22T1020492.Models.Sales
{
    /// <summary>
    /// Thông tin của một đơn hàng khi xem chi tiết (DTO)
    /// </summary>
    public class OrderViewInfo : Order
    {
        /// <summary>
        /// Tên nhân viên phụ trách đơn hàng
        /// </summary>
        public string EmployeeName { get; set; } = "";

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string CustomerName { get; set; } = "";
        /// <summary>
        /// Tên giao dịch của khách hàng
        /// </summary>
        public string CustomerContactName { get; set; } = "";
        /// <summary>
        /// Email của khách hàng
        /// </summary>
        public string CustomerEmail { get; set; } = "";
        /// <summary>
        /// Điện thoại khách hàng
        /// </summary>
        public string CustomerPhone { get; set; } = "";
        /// <summary>
        /// Địa chỉ của khách hàng
        /// </summary>
        public string CustomerAddress { get; set; } = "";

        /// <summary>
        /// Tên người giao hàng
        /// </summary>
        public string ShipperName { get; set; } = "";
        /// <summary>
        /// Điện thoại người giao hàng
        /// </summary>
        public string ShipperPhone { get; set; } = "";
        /// <summary>
        /// Tổng trị giá của đơn hàng (Sửa tên này để khớp với SQL hoặc ngược lại)
        /// </summary>
        public decimal SumOfPrice { get; set; } = 0;

        public decimal TotalPrice => SumOfPrice;


        // Bạn có thể giữ TotalValue nếu View đang gọi nó, 
        // nhưng tốt nhất là dùng 1 tên thống nhất.
        public decimal TotalValue => SumOfPrice;

        /// <summary>
        /// Mô tả trạng thái đơn hàng (ví dụ: Đơn hàng mới, Đang giao hàng...)
        /// </summary>
        public string StatusDescription { get; set; } = "";

        /// <summary>
        /// Danh sách các mặt hàng trong đơn hàng (Dùng lớp ViewInfo để có ProductName, Unit)
        /// </summary>
        public List<OrderDetailViewInfo> Details { get; set; } = new List<OrderDetailViewInfo>();
    }
}
