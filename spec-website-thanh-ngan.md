# SPEC DỰ ÁN: WEBSITE BÁN HÀNG & QUẢN LÝ BÁN HÀNG — THƯƠNG HIỆU THANH NGÂN

## 1. TỔNG QUAN DỰ ÁN

**Tên thương hiệu:** Thanh Ngân
**Tên miền/Website:** thanhnganseafood.com.vn
**Lĩnh vực:** Kinh doanh hải sản
**Ngành hàng:**
1. Hải sản đông lạnh (tôm, mực, cá, cua, ghẹ, nghêu, sò... đông lạnh)
2. Hải sản tươi sống (tôm, cá, mực, cua, ghẹ... tươi, ướp lạnh giao trong ngày)
3. Mắm cá cơm (sản phẩm chế biến/lên men đặc sản)

**Mục tiêu hệ thống:**
- Xây dựng website bán hàng trực tuyến (frontend cho khách hàng), cho phép xem sản phẩm mà không cần đăng nhập
- Xây dựng hệ thống quản lý bán hàng (backoffice/admin) cho chủ shop vận hành: quản lý sản phẩm, đơn hàng, kho, khách hàng, khuyến mãi, báo cáo

**Công nghệ đề xuất:**
- Backend: ASP.NET Core MVC (.NET 10)
- Database: SQL Server
- ORM: Entity Framework Core
- Frontend: Razor View + Bootstrap/Tailwind CSS + jQuery/AJAX cho phần tương tác (giỏ hàng, lọc sản phẩm)
- Authentication: ASP.NET Core Identity (phân quyền: Khách hàng, Nhân viên kho, Nhân viên bán hàng, Quản lý, Admin)

---

## 2. NHÓM CHỨC NĂNG DÀNH CHO KHÁCH HÀNG (WEBSITE BÁN HÀNG)

### 2.1. Trang chủ
- Banner giới thiệu thương hiệu Thanh Ngân
- Danh mục sản phẩm nổi bật theo 3 nhóm: Hải sản đông lạnh / Hải sản tươi sống / Mắm cá cơm
- Sản phẩm bán chạy, sản phẩm khuyến mãi
- Tin tức / cẩm nang chế biến hải sản (blog)
- Thông tin liên hệ, khu vực giao hàng

### 2.2. Danh mục & tìm kiếm sản phẩm
- Danh mục sản phẩm theo nhóm (đông lạnh / tươi sống / mắm cá cơm) và danh mục con (tôm, mực, cá, cua ghẹ...)
- Bộ lọc: theo giá, loại hải sản, trọng lượng/quy cách đóng gói, khuyến mãi
- Tìm kiếm theo tên sản phẩm
- Sắp xếp: giá tăng/giảm, bán chạy, mới nhất

### 2.3. Trang chi tiết sản phẩm
- Hình ảnh sản phẩm (nhiều ảnh/gallery)
- Tên, mô tả chi tiết, nguồn gốc xuất xứ
- Giá bán theo quy cách (ví dụ: 500g, 1kg, khay, hũ...)
- Tình trạng: còn hàng/hết hàng
- Hướng dẫn bảo quản (đông lạnh) / hạn sử dụng (mắm cá cơm)
- Đánh giá & bình luận của khách hàng
- Sản phẩm liên quan / gợi ý mua kèm
- Nút "Thêm vào giỏ hàng" / "Mua ngay"

### 2.4. Giỏ hàng & Thanh toán
- Xem sản phẩm, thêm vào giỏ hàng **không cần đăng nhập** (giỏ hàng tạm lưu theo session/cookie)
- Hỗ trợ **đặt hàng không cần tài khoản (Guest Checkout)** — chỉ cần nhập tên, số điện thoại, địa chỉ giao hàng — để tăng tỷ lệ chốt đơn
- Chỉ yêu cầu đăng nhập khi: xem lịch sử đơn hàng, viết đánh giá, lưu sản phẩm yêu thích
- Xem/sửa/xóa sản phẩm trong giỏ, cập nhật số lượng
- Áp dụng mã giảm giá/voucher
- Chọn phương thức giao hàng (giao tận nơi nội thành, giao tỉnh qua vận chuyển lạnh, nhận tại cửa hàng)
- Chọn phương thức thanh toán: COD, chuyển khoản ngân hàng, ví điện tử (Momo, VNPay, ZaloPay)
- Xác nhận đơn hàng, gửi email/SMS xác nhận

### 2.5. Tài khoản khách hàng
- Đăng ký/đăng nhập (email, số điện thoại, hoặc Google/Facebook login)
- Quản lý thông tin cá nhân, sổ địa chỉ giao hàng
- Lịch sử đơn hàng, theo dõi trạng thái đơn hàng
- Danh sách sản phẩm yêu thích (wishlist)
- Đánh giá sản phẩm đã mua

### 2.6. Chương trình khuyến mãi
- Hiển thị mã giảm giá, chương trình combo (VD: combo hải sản tươi + mắm cá cơm)
- Điểm tích lũy/thành viên thân thiết (tùy chọn giai đoạn sau)

### 2.7. Trang nội dung khác
- Giới thiệu thương hiệu Thanh Ngân (câu chuyện, quy trình sản xuất/đánh bắt, cam kết chất lượng)
- Chính sách giao hàng, đổi trả, bảo quản
- Liên hệ (form liên hệ, bản đồ, hotline, Zalo)
- Câu hỏi thường gặp (FAQ)

---

## 3. NHÓM CHỨC NĂNG QUẢN LÝ BÁN HÀNG (ADMIN/BACKOFFICE)

### 3.1. Quản lý sản phẩm
- Thêm/sửa/xóa sản phẩm, danh mục, thương hiệu
- Quản lý biến thể sản phẩm theo quy cách đóng gói (500g/1kg/khay)
- Quản lý hình ảnh sản phẩm
- Quản lý giá bán, giá khuyến mãi theo thời gian
- Đánh dấu sản phẩm nổi bật, sản phẩm mới, hết hàng

### 3.2. Quản lý kho (đặc thù ngành hải sản)
- **Phiếu nhập kho**: chọn nhà cung cấp, ngày nhập, danh sách sản phẩm + số lượng + **giá nhập** theo từng lô (giá hải sản biến động theo ngày/mùa vụ), hạn sử dụng — tự động cộng vào tồn kho
- **Phiếu xuất kho**: xuất bán (tự động sinh khi đơn hàng được xác nhận), xuất hủy (hàng hỏng/hết hạn — ghi nhận riêng là **hao hụt**), xuất chuyển kho (nếu có nhiều chi nhánh)
- **Kiểm kê kho**: đối chiếu tồn kho thực tế và hệ thống, điều chỉnh chênh lệch (cần Quản lý duyệt)
- Tính **giá vốn** theo phương pháp **FIFO** (nhập trước xuất trước — phù hợp hải sản vì hàng cũ phải bán trước)
- Tồn kho theo từng sản phẩm/quy cách, theo từng lô
- Cảnh báo sắp hết hàng, cảnh báo cận hạn sử dụng (đặc biệt với hàng tươi sống)
- Quản lý nhà cung cấp (ngư dân, vựa hải sản, cơ sở làm mắm...)
- **Lịch sử nhập/xuất** theo từng sản phẩm (audit trail: ai tạo, ai sửa, thời điểm, nội dung thay đổi)

**Quy tắc kiểm soát nhập/xuất (phân quyền):**
- Nhân viên kho: được **tạo** phiếu nhập/xuất, **không được tự sửa/xóa** sau khi đã lưu
- Muốn sửa/hủy phiếu đã lưu: phải được **Quản lý duyệt lại**
- Phiếu nhập/xuất giá trị lớn hoặc phiếu kiểm kê điều chỉnh tồn kho: bắt buộc **Quản lý duyệt** trước khi có hiệu lực
- Mọi thao tác đều ghi log (không cho sửa/xóa âm thầm)

### 3.3. Quản lý đơn hàng
- Danh sách đơn hàng, lọc theo trạng thái (chờ xác nhận, đang xử lý, đang giao, hoàn thành, hủy, hoàn trả)
- Xử lý đơn hàng: xác nhận, phân công giao hàng, cập nhật trạng thái
- In hóa đơn/phiếu giao hàng
- Xử lý đổi trả/hoàn tiền

### 3.4. Quản lý khách hàng
- Danh sách khách hàng, lịch sử mua hàng
- Phân nhóm khách hàng (khách lẻ, khách sỉ, khách VIP)
- Chăm sóc khách hàng: ghi chú, lịch sử liên hệ

### 3.5. Quản lý khuyến mãi
- Tạo mã giảm giá (theo %, theo số tiền, theo sản phẩm)
- Tạo chương trình combo/flash sale
- Thiết lập thời gian áp dụng, giới hạn số lượng

### 3.6. Quản lý nhân viên & phân quyền
- Tài khoản nhân viên (bán hàng, kho, giao hàng, quản lý)
- Phân quyền theo vai trò (Role-based access)

**Bảng phân quyền đề xuất:**

| Vai trò | Xem tồn kho | Tạo phiếu nhập | Tạo phiếu xuất | Sửa/xóa phiếu đã lưu | Duyệt phiếu | Quản lý sản phẩm/giá | Xem báo cáo lời/lỗ |
|---|---|---|---|---|---|---|---|
| Nhân viên kho | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Nhân viên bán hàng | ✅ (chỉ xem) | ❌ | ✅ (tự động khi bán) | ❌ | ❌ | ❌ | ❌ |
| Quản lý/Trưởng cửa hàng | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (giới hạn) |
| Admin (chủ shop) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (toàn bộ) |

### 3.7. Báo cáo & thống kê
- Doanh thu theo ngày/tháng/quý/năm
- Doanh thu theo nhóm sản phẩm (đông lạnh/tươi sống/mắm cá cơm)
- Sản phẩm bán chạy/tồn kho lâu
- **Báo cáo lời/lỗ**: Lợi nhuận = Giá bán − Giá vốn (giá vốn lấy theo lô xuất FIFO), xem theo từng đơn hàng, từng sản phẩm, từng lô nhập, hoặc theo ngày/tháng/quý
- **Báo cáo hao hụt**: giá trị hàng hủy/hỏng/hết hạn — trừ vào lợi nhuận thực tế để không bị ảo số lời
- Xuất báo cáo Excel/PDF

### 3.8. Cấu hình hệ thống
- Cấu hình phương thức thanh toán, đơn vị vận chuyển
- Cấu hình khu vực giao hàng, phí ship
- Quản lý nội dung trang tĩnh (giới thiệu, chính sách...)

---

## 4. MÔ HÌNH DỮ LIỆU CHÍNH (GỢI Ý BẢNG DATABASE)

- **Category** (Danh mục: Đông lạnh / Tươi sống / Mắm cá cơm + danh mục con)
- **Product** (Sản phẩm)
- **ProductVariant** (Biến thể: quy cách đóng gói, giá, tồn kho)
- **ProductImage**
- **Supplier** (Nhà cung cấp)
- **InventoryBatch** (Lô hàng nhập kho — có ngày nhập, giá nhập, hạn sử dụng)
- **StockInReceipt** / **StockInReceiptDetail** (Phiếu nhập kho)
- **StockOutReceipt** / **StockOutReceiptDetail** (Phiếu xuất kho: xuất bán / xuất hủy-hao hụt / chuyển kho)
- **StockAdjustment** (Phiếu kiểm kê/điều chỉnh tồn kho)
- **Customer**
- **Address** (Sổ địa chỉ)
- **Order**
- **OrderDetail**
- **Cart** / **CartItem**
- **Promotion** / **Coupon**
- **Review** (Đánh giá sản phẩm)
- **Employee** / **Role** / **Permission**
- **Payment**
- **ShippingMethod**

---

## 5. YÊU CẦU PHI CHỨC NĂNG

- Giao diện responsive (dùng tốt trên điện thoại — vì khách mua hải sản online phần lớn qua mobile)
- Tốc độ tải trang nhanh, tối ưu SEO cho các trang sản phẩm/danh mục
- Bảo mật: mã hóa mật khẩu, chống CSRF/XSS, HTTPS
- Có khả năng mở rộng: thêm ngành hàng mới trong tương lai
- Hỗ trợ đa kênh liên hệ: hotline, Zalo Chat, Facebook Messenger (nhúng widget)

---

## 6. GỢI Ý THỨ TỰ TRIỂN KHAI (ROADMAP)

1. **Giai đoạn 1 — MVP:** Quản lý sản phẩm/danh mục, hiển thị sản phẩm, giỏ hàng, đặt hàng (COD), quản lý đơn hàng cơ bản
2. **Giai đoạn 2:** Tài khoản khách hàng, thanh toán online, quản lý kho theo lô/hạn sử dụng, khuyến mãi
3. **Giai đoạn 3:** Báo cáo thống kê nâng cao, phân quyền nhân viên, chương trình khách hàng thân thiết, tối ưu SEO/marketing

---

*Ghi chú: File này dùng làm spec đầu vào để AI agent (hoặc lập trình viên) triển khai code. Có thể điều chỉnh phạm vi chức năng theo từng giai đoạn phát triển thực tế.*
