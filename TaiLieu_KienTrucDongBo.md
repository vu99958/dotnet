# 📑 BÁO CÁO SẾP: TÀI LIỆU KIẾN TRÚC & LUỒNG ĐỒNG BỘ DỮ LIỆU

Dạ thưa sếp, em vừa hoàn thiện xong bản thiết kế luồng chạy cho hệ thống đồng bộ Chấm công và Sinh trắc học (vân tay, khuôn mặt). Em làm tài liệu này để sếp nắm được anh em mình đang xây dựng hệ thống chạy ngầm bên dưới ra sao, cấu trúc code thế nào để đảm bảo tính ổn định và dễ bảo trì về lâu dài nhé.

---

## 1. 🏗️ Tổng quan hệ thống mình có những gì?

Để hệ thống chạy mượt mà và không bị phụ thuộc, em chia nó ra làm 4 phần rõ rệt (mô hình Client-Server):

1. **Máy chấm công vật lý (Ronald Jack/ZKTeco):** Em coi đây như cục lưu trữ offline. Nhân viên cứ tới quẹt vân tay, máy tự nhớ.
2. **Desktop App (C# WinForms):** Em cài cái này ở các chi nhánh. Nó đóng vai trò làm "trạm trung chuyển" (Middleware). Nó sẽ dùng bộ thư viện `zkemkeeper.dll` của hãng để "nói chuyện" trực tiếp với máy chấm công.
3. **Backend Server (ABP Framework):** Đây là não bộ chính. Em xây dựng các API trên này theo chuẩn kiến trúc DDD để xử lý logic, tránh việc Desktop App gọi thẳng vào database cho an toàn sếp ạ.
4. **Database (SQL Server):** Chỗ lưu trữ tập trung cuối cùng, chứa toàn bộ log chấm công và các mẫu vân tay/khuôn mặt của cả công ty mình.

---

## 2. 🔄 Cơ chế đồng bộ Chấm công (Attendance Sync) hoạt động thế nào?

Để không bao giờ sót dữ liệu của nhân viên, em làm 2 cơ chế chạy song song luôn sếp ạ:

### A. Đồng bộ bằng tay (Kéo nguyên cục dữ liệu)
Cái này dùng khi phần mềm mới bật lên, hoặc sếp muốn chốt sổ cuối tháng. 
- App sẽ chui vào máy chấm công, hút toàn bộ log ra.
- Lọc xem cái nào là dữ liệu cũ (đã đồng bộ rồi) thì bỏ qua, chỉ lấy dữ liệu mới sinh ra.
- Xong xuôi em đóng gói lại rồi gọi API ném 1 cục lên Server lưu.

### B. Đồng bộ Real-time (Thời gian thực)
Đây là cái hay nhất để sếp xem Dashboard trực tiếp. 
- Em cài một cái "móc" (Event Hook) vào máy chấm công. 
- Cứ hễ có nhân viên nào vừa tít vân tay xong, máy nó sẽ báo ngay cho App Desktop, rồi App Desktop bắn thẳng lên Server trong chưa tới 1 giây.

### C. Tính năng "Bao lô" rớt mạng (Auto-Recovery)
Thực tế đi làm ở các chi nhánh, hay có vụ ai đó lỡ đá văng dây mạng hoặc mất điện. 
- Em cho App ngầm "Ping" máy chấm công 10 giây/lần.
- Nếu phát hiện tịt ngòi (rút cáp), App sẽ tự động ngắt kết nối an toàn để không bị treo phần mềm.
- Rồi nó cứ 5 giây lại gọi thử 1 lần. Chừng nào mạng có lại, nó tự động kết nối và gắn lại cái Hook Real-time ở trên. Sếp không cần phải bấm kết nối lại bằng tay.

---

## 3. 🧬 Giải bài toán Sinh trắc học (Vân tay + Khuôn mặt)

Bài toán sếp giao: "Làm sao để nhân sự đăng ký vân tay ở máy A, sang máy B vẫn chấm được mà không cần đăng ký lại?". Em xử lý theo luồng này:

### Chiều 1: Hút từ Máy A lên Server (Backup)
- Khi nhân viên đăng ký xong trên máy A, anh em admin mở phần mềm bấm nút "Tải lên".
- App của em sẽ vét sạch sành sanh: Tối đa 10 ngón tay của người đó (Index từ 0 đến 9) và 1 mẫu Khuôn mặt (Index 50 - theo chuẩn của Ronald Jack).
- Gom xong, em ném qua API để Server cất vào két sắt Database. Nếu vân tay đó có rồi thì em cho cập nhật đè lên bản mới nhất.

### Chiều 2: Bơm từ Server xuống Máy B (Deploy chéo)
- Sếp mua máy B về cắm mạng rẹt rẹt. Mở phần mềm lên, bấm "Đẩy xuống".
- Phần mềm gọi API kéo toàn bộ vân tay/khuôn mặt trên Database về, rồi dùng lệnh `SSR_SetUserTmpStr` bơm thẳng vào máy B.
- Xong xuôi em gọi lệnh `RefreshData()` để máy B "tỉnh ngủ" và nhận diện được nhân viên cũ ngay lập tức. Cực kỳ nhanh gọn!

---

## 4. 📐 Về mặt kỹ thuật, code có chuẩn DDD (Domain-Driven Design) không?

Sếp yên tâm, em code cực kỳ kỉ luật theo Onion Architecture của ABP Framework:

- **Tầng Giao tiếp máy (Infrastructure):** Nằm gọn trong file `RonaldJackService.cs`. Lớp này chuyên làm "thợ đụng" dọn rác do SDK máy chấm công nhả ra. Server không hề biết sự tồn tại của SDK này. Dữ liệu rác rến, lỗi kết nối... em chặn đứng ở đây hết rồi chuẩn hóa thành DTO sạch sẽ.
- **Tầng Domain (Trái tim của hệ thống):** Thể hiện qua cái `BiometricTemplate`. Em thiết lập nó là `AggregateRoot` độc lập luôn. Nhiệm vụ của nó là canh gác tính toàn vẹn dữ liệu (đúng mã NV, đúng vị trí ngón, vân vân). 
- **Tầng Application:** Cái này như ông Nhạc trưởng (`BiometricAppService.cs`). Chỉ có ông này mới được quyền nhận lệnh từ màn hình UI và ra lệnh cho Database lưu trữ.

**🔥 Chốt lại lợi ích cho sếp:**
Sau này công ty mình lớn mạnh, sếp đổi sang xài máy hãng khác (Hikvision, Hanwei...) thì em chỉ việc đập bỏ file `RonaldJackService.cs` rồi viết cái file mới cắm vào. Tuyệt đối không phải đụng tới 1 dòng code nào trên Backend hay Database. Code cực kỳ "Sạch - Dễ bảo trì - Dễ nâng cấp" luôn sếp nhé!
