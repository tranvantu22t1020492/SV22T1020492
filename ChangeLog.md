
29-01-2026
## kHởi tạo Solution

- Tạo Solution có tên SV22T1020492
- Bổ sung cho Solution cá Project sau:
	- SV22T1020492.Admin: Project dạng ASP.NET core MVC
	- SV22T1020492.Shop: Project dạng ASP.NET core MVC
	- SV22T1020492.Models: Project dạng Class Library
	- SV22T1020492.DataLayers: Project dạng Class Library
	- SV22T1020492.BusinessLayers: Project dạng Class Library


## CHức năng cho SV22T1020492.Admin
- Trang chủ: Home/Index
- Account:
	- Account/Login 
	- Account/Logout  
	- Account/ChangePassword  
- Supplier
	- Supplier/Index    
	- Supplier/Create
	- Supplier/Edit/{id}
	- Supplier/Delete/{id}
- Customer:
	- Customer/Index
	- Customer/Create
	- Customer/Edit/{id}
	- Customer/Delete/{id}
- Shipper:
	- Shipper/Index
	- Shipper/Create
	- Shipper/Edit/{id}
	- Shipper/Delete/{id} 
- Employee
	- Employee/Index
	- Employee/Create
	- Employee/Edit/{id}
	- Employee/Delete/{id}
	- Employee/ChangePassword/{id}
	- Employee/ChangeRole/{id}
- Category
	- Category/Index
	- Category/Create
	- Category/Edit/{id}
	- Category/Delete/{id}
- Product
	- Product/Index
		- Tìm kiếm, lọc mặt hàng theo nhà cung cấp, phân loại, khoảng giá tên
		- Hiển thị dưới dạng phân trang
	- Product/Create
	- Product/Edit/{id}
	- Product/Delete/{id}
	- Product/Detail/{id}
	- Product/ListAttributes/{id}
	- Product/CreateAttribute/{id}
	- Product/EditAttribute/{id}?attributeId={attributeId}
	- Product/DeleteAttribute/{id}?attributeId={attributeId}
	- Product/CreatePhoto/{id}s 
	- Product/ListPhotos/{id}
	- Product/EditPhoto/{id}?photoId={photoId}
	- Product/DeletePhoto/{id}?photoId={photoId}
- Order
	- Order/Index
	- Order/Search
	- Order/Create
	- Order/Detail/{id}
	- Order/EditCartItem/{id}?productId={productId}
	- Order/DeleteCartItem/{id}?productId={productId}
	- Order/ClearCart
	- Order/Accept/{id}
	- Order/Shipping/{id}
	- Order/Finish/{id}
	- Order/Reject/{id}
	- Order/Cancel/{id}
	- Order/Delete/{id}

Trong file layout:
- @RenderBody(): Đặt tại vị trí mà nội dung các trong web sẽ được "ghi" vào đó
- @{
	await Html.RenderPartialAsyc("ParitalView");
}
hoặc
await Html.PartialAsyc("ParitalView");
Dùng để lấy nội dung của một PartialView (Phần code HTML được tách ra ở 1 file view) và "ghi/chèn" vào một vị trí nào đó.)
- @await RenderSectionAsync("SectionName", required" false")


Domain:
	- Data Distrinary  + Province
	- Partner + Supplier
			  + Customer
			  + Shipper
	- HR	+ Employee
	- Catalog	+ Product
				+ Category
				+ ProductAtribute
				+ ProductPhoto
	- Sales		+ OrderStatus
				+ Order
				+ OrderDetail
	- Security	+ WrerAccount
	- Common

Interfaces: Dùng để định nghĩa các "giao diện" xử lý dữ liệu

Viết lớp SupplierRepository cài dặt cho interface trên:
- Sử dụng Dapper
- CSDL SQLServer
- Lớp có constructor nhận đầu vào là connectionString
- Lớp nằm trong namespce: LiteCommerce.DataLayers.SQLServer

Tìm kiếm, phân trang: Đầu vào tìm kiếm, phân trang: Page, PageSize, SearchValue(nhà cc, khách hàng, shipper, category, employee)
Lấy thông tin cảu 1 đối tượng dựa vào id
Bổ sung 1 đối tượng vào CSDL (gửi vào server phải có thuộc tính name)
Cập nhật 1 đối tượng trong CSDL
Xóa 1 đối tượng ra khỏi CSDL dựa vào id
Kiểm tra xem 1 đối tượng có dữ liệu liên quan hay không?


- Khi Action có trả dữ liệu về cho View thì phải biết kiểu dữ liệu là gì
- Trong View (trên cùng), Phải có chỉ thị khai báo kiểu dữ liệu mà Action trả về
	@model Kiểu_dữ_liệu
- Trong View, dữ liệu mà Action trả về lưu trong thuộc tính có tên là Model(trong View thông qua thuộc tính này để lấy dữ liệu)

SQL Server Profiler dùng để tối ưu dòng code sql trong DataLayers

Quy trình bảo mật Security
	- Người dùng cung cấp thông tin để kiểm tra xem có được phép vào hệ thống hay không?
	- Hệ thống kiểm tra, nếu hợp lệ thì cấp cho một Cookie ( giấy chứng nhận) (Authentication)
	- Phía client xuất trình Cookie mỗi khi thực hiện các Request (kèm cookie trong header của lời gọi)
	- Phía server dựa vào cookie để kiểm tra (Authorization)
2 thuật ngữ:
	- Authentication: Kiểm tra xem đăng nhập có hợp lệ hay không
	- Authorization: Kiểm tra cookie xem có chức năng gì để sử dụng (phải được phân quyền để sử dụng chức năng)

User
