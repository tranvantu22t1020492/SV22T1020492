using Dapper;
using SV22T1020492.DataLayers.Interfaces;
using SV22T1020492.Models.Common;
using SV22T1020492.Models.Sales;

namespace SV22T1020492.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến đơn hàng trên SQL Server,
    /// bao gồm cả chi tiết đơn hàng (OrderDetails)
    /// </summary>
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL</param>
        public OrderRepository(string connectionString) : base(connectionString)
        {
        }

        // ===================== ORDER =====================

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// theo tên khách hàng, trạng thái và khoảng ngày lập đơn
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang đơn hàng</param>
        /// <returns>Kết quả phân trang danh sách thông tin đơn hàng dùng để hiển thị</returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = GetConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@searchValue", $"%{input.SearchValue}%");
            // Sử dụng kiểu int nullable (int?) để Dapper tự hiểu
            parameters.Add("@status", (int)input.Status == 0 ? (int?)null : (int)input.Status);
            parameters.Add("@dateFrom", input.DateFrom);
            parameters.Add("@dateTo", input.DateTo);
            parameters.Add("@pageSize", input.PageSize);
            parameters.Add("@offset", input.Offset);

            var sql = @"SELECT COUNT(*)
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                        WHERE  (c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo));

                        SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(c.CustomerName, N'') AS CustomerName,
                               ISNULL(c.Phone,        N'') AS CustomerPhone,
                               ISNULL(e.FullName,     N'') AS EmployeeName,
                               ISNULL((SELECT SUM(d.Quantity * d.SalePrice)
                                       FROM   OrderDetails d
                                       WHERE  d.OrderID = o.OrderID), 0) AS SumOfPrice
                        FROM   Orders o
                               LEFT JOIN Customers c  ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e  ON o.EmployeeID = e.EmployeeID
                        WHERE  (c.CustomerName LIKE @searchValue OR c.Phone LIKE @searchValue)
                          AND  (@status   IS NULL OR o.Status     = @status)
                          AND  (@dateFrom IS NULL OR o.OrderTime >= @dateFrom)
                          AND  (@dateTo   IS NULL OR o.OrderTime <= DATEADD(day, 1, @dateTo))
                        ORDER  BY o.OrderTime DESC
                        OFFSET @offset ROWS
                        FETCH  NEXT @pageSize ROWS ONLY;";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);
            int rowCount = await multi.ReadFirstAsync<int>();
            var data = (await multi.ReadAsync<OrderViewInfo>()).ToList();

            return new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = data
            };
        }

        /// <summary>
        /// Lấy thông tin chi tiết của 1 đơn hàng để hiển thị
        /// (bao gồm thông tin khách hàng, nhân viên, người giao hàng)
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin đơn hàng dùng để hiển thị hoặc null nếu không tồn tại</returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT o.OrderID,
                               o.CustomerID,
                               o.OrderTime,
                               o.DeliveryProvince,
                               o.DeliveryAddress,
                               o.EmployeeID,
                               o.AcceptTime,
                               o.ShipperID,
                               o.ShippedTime,
                               o.FinishedTime,
                               o.Status,
                               ISNULL(e.FullName,          N'') AS EmployeeName,
                               ISNULL(c.CustomerName,      N'') AS CustomerName,
                               ISNULL(c.ContactName,       N'') AS CustomerContactName,
                               ISNULL(c.Email,             N'') AS CustomerEmail,
                               ISNULL(c.Phone,             N'') AS CustomerPhone,
                               ISNULL(c.Address,           N'') AS CustomerAddress,
                               ISNULL(s.ShipperName,       N'') AS ShipperName,
                               ISNULL(s.Phone,             N'') AS ShipperPhone
                        FROM   Orders o
                               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                               LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                               LEFT JOIN Shippers  s ON o.ShipperID  = s.ShipperID
                        WHERE  o.OrderID = @orderID";
            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        /// <summary>
        /// Bổ sung một đơn hàng mới vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng cần bổ sung</param>
        /// <returns>Mã đơn hàng vừa được bổ sung</returns>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = GetConnection();
            var sql = @"INSERT INTO Orders (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress,
                                            EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                        VALUES (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress,
                                @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                        SELECT SCOPE_IDENTITY();";
            var result = await connection.ExecuteScalarAsync<decimal>(sql, data);
            return (int)result;
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng trong CSDL
        /// (thường dùng để cập nhật trạng thái, thông tin giao hàng, nhân viên xử lý, người giao hàng...)
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE Orders
                        SET CustomerID       = @CustomerID,
                            OrderTime        = @OrderTime,
                            DeliveryProvince = @DeliveryProvince,
                            DeliveryAddress  = @DeliveryAddress,
                            EmployeeID       = @EmployeeID,
                            AcceptTime       = @AcceptTime,
                            ShipperID        = @ShipperID,
                            ShippedTime      = @ShippedTime,
                            FinishedTime     = @FinishedTime,
                            Status           = @Status
                        WHERE OrderID = @OrderID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa đơn hàng có mã là orderID (bao gồm cả chi tiết đơn hàng liên quan)
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = GetConnection();
            // OrderDetails sẽ bị xóa tự động nhờ ON DELETE CASCADE trên FK_OrderDetails_Orders
            var sql = "DELETE FROM Orders WHERE OrderID = @orderID";
            int rows = await connection.ExecuteAsync(sql, new { orderID });
            return rows > 0;
        }

        // ===================== ORDER DETAILS =====================

        /// <summary>
        /// Lấy danh sách mặt hàng trong một đơn hàng (kèm theo tên hàng, đơn vị tính, ảnh)
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách chi tiết đơn hàng dùng để hiển thị</returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT d.OrderID,
                               d.ProductID,
                               d.Quantity,
                               d.SalePrice,
                               ISNULL(p.ProductName, N'') AS ProductName,
                               ISNULL(p.Unit,        N'') AS Unit,
                               ISNULL(p.Photo,       N'') AS Photo
                        FROM   OrderDetails d
                               LEFT JOIN Products p ON d.ProductID = p.ProductID
                        WHERE  d.OrderID = @orderID
                        ORDER  BY p.ProductName";
            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng cụ thể trong một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin chi tiết đơn hàng dùng để hiển thị hoặc null nếu không tồn tại</returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();
            var sql = @"SELECT d.OrderID,
                               d.ProductID,
                               d.Quantity,
                               d.SalePrice,
                               ISNULL(p.ProductName, N'') AS ProductName,
                               ISNULL(p.Unit,        N'') AS Unit,
                               ISNULL(p.Photo,       N'') AS Photo
                        FROM   OrderDetails d
                               LEFT JOIN Products p ON d.ProductID = p.ProductID
                        WHERE  d.OrderID   = @orderID
                          AND  d.ProductID = @productID";
            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { orderID, productID });
        }

        /// <summary>
        /// Bổ sung một mặt hàng vào đơn hàng (thêm vào giỏ hàng).
        /// Nếu mặt hàng đã tồn tại trong đơn hàng thì cập nhật số lượng và giá bán
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết đơn hàng cần bổ sung</param>
        /// <returns>true nếu bổ sung / cập nhật thành công</returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();
            // Dùng MERGE để xử lý cả trường hợp đã tồn tại và chưa tồn tại
            var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID)
                            UPDATE OrderDetails
                            SET    Quantity  = @Quantity,
                                   SalePrice = @SalePrice
                            WHERE  OrderID   = @OrderID
                              AND  ProductID = @ProductID
                        ELSE
                            INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                            VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết đơn hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công, false nếu không có bản ghi nào được cập nhật</returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();
            var sql = @"UPDATE OrderDetails
                        SET Quantity  = @Quantity,
                            SalePrice = @SalePrice
                        WHERE OrderID   = @OrderID
                          AND ProductID = @ProductID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng cần xóa khỏi đơn hàng</param>
        /// <returns>true nếu xóa thành công, false nếu không tìm thấy bản ghi</returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM OrderDetails WHERE OrderID = @orderID AND ProductID = @productID";
            int rows = await connection.ExecuteAsync(sql, new { orderID, productID });
            return rows > 0;
        }
    }
}