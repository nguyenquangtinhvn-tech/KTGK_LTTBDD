# TOEIC Reading Admin

Frontend quản trị độc lập, được dựng theo thiết kế Google Stitch của project
`TOEIC Reading - Admin Dashboard` (`4138776416071832597`). Ứng dụng dùng
TypeScript và Vite; luồng nhập đề Word kết nối ASP.NET Core để phân tích DOCX,
xem trước và lưu đề ở trạng thái bản nháp.

## Yêu cầu

- Windows 10/11
- Node.js 20.19+ hoặc 22.12+ và npm

## Cài đặt và chạy

Mở PowerShell tại `D:\KIEMTRAGIUAKY\Admin`, sau đó chạy:

```powershell
npm install
Copy-Item .env.example .env
npm run dev
```

Mở địa chỉ được Vite hiển thị, mặc định là `http://localhost:5173`.
Hãy dùng hostname `localhost` (không đổi sang `127.0.0.1`) nếu chưa restart
backend sau khi thêm origin CORS cho địa chỉ loopback.

## Các trang

- Tổng quan: chỉ số mẫu, biểu đồ kết quả và hoạt động gần đây.
- Quản lý đề thi: tìm kiếm, lọc, phân trang và xem chi tiết; danh sách API Admin
bao gồm đề nháp.
- Tải đề từ Word: chọn/kéo thả DOCX, phân tích trên backend, rà soát lỗi, xác
  nhận đáp án và nhập đề thật.
- Quản lý câu hỏi: tìm kiếm, lọc và biểu mẫu sửa bài đọc/câu hỏi/lựa chọn/đáp án/
  gợi ý/giải thích.
- Chi tiết đề thi: thông tin và cấu trúc đề, trạng thái xuất bản và công tắc
  kích hoạt ca thi.

Giao diện co giãn cho desktop, tablet và điện thoại; dữ liệu ví dụ đều có nhãn
“DỮ LIỆU MẪU”.

## Dữ liệu và API

Mặc định `VITE_USE_MOCK_DATA=true`. Các dữ liệu demo chỉ tồn tại trong bộ nhớ
trình duyệt; thao tác demo nêu rõ rằng chúng **không được lưu vào SQL Server**.
Trong chế độ này, luồng nhập DOCX yêu cầu chuyển sang API thật thay vì giả lập
phân tích hoặc lưu.

Lớp gọi API nằm tại `src/services/apiClient.ts` và
`src/services/adminApi.ts`. Để chuyển sang API thật:

1. Tạo `Admin\.env.local` (hoặc cập nhật `.env`) với:

   ```dotenv
   VITE_USE_MOCK_DATA=false
   VITE_API_BASE_URL=https://localhost:44311
   ```

2. Đảm bảo backend đang chạy ở môi trường `Development` và cho phép CORS từ
   `http://localhost:5173`. Launch profile mặc định đặt môi trường này.
3. Tất cả API quản trị, bao gồm hai API Word, yêu cầu JWT và quyền
   `Pages.Exams.Import` trong cả Development lẫn Production. API đăng nhập là
   `POST /api/TokenAuth/Authenticate`; frontend gửi token dưới dạng
   `Authorization: Bearer <token>`. Khi phiên hết hạn, hãy đăng nhập lại.

## Đăng nhập và tài khoản Development

Frontend dùng cơ chế JWT/ASP.NET Boilerplate hiện có, không tạo token hoặc
endpoint đăng nhập riêng. Tài khoản Admin cần được gán role quản trị có quyền
`Pages.Exams.Import`; tài khoản Student chỉ được gán role `Student` và không có
quyền này.

Seeder tài khoản mẫu chỉ chạy ở môi trường `Development` khi bật tường minh
bằng `SampleAccounts__SeedOnStartup=true`. Mật khẩu phải được truyền qua biến
môi trường cục bộ, không đặt trong mã nguồn, `.env` hoặc README. Đặt thêm
`SampleAccounts__ResetExistingPasswords=true` chỉ trong lần khởi tạo cần reset
mật khẩu tài khoản đã tồn tại; nếu không bật, seeder giữ nguyên mật khẩu hiện
có. Tài khoản sẽ được tạo/cập nhật trong database đã cấu hình, không chạy
migration.

Ví dụ PowerShell (thay các giá trị placeholder cục bộ, không commit chúng):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:SampleAccounts__SeedOnStartup = 'true'
$env:SampleAccounts__ResetExistingPasswords = 'true'
$env:SampleAccounts__AdminPassword = '<local-admin-password>'
$env:SampleAccounts__StudentPassword = '<local-student-password>'
dotnet run --project .\src\ToeicReading.Web.Host
```

Sau dòng log xác nhận tài khoản đã sẵn sàng, dừng Host bằng `Ctrl+C`, xóa
biến seed khỏi PowerShell rồi khởi động Host bình thường. Không bật cờ seed
hoặc reset thường trực trong môi trường Production.

```powershell
Remove-Item Env:SampleAccounts__SeedOnStartup -ErrorAction SilentlyContinue
Remove-Item Env:SampleAccounts__ResetExistingPasswords -ErrorAction SilentlyContinue
Remove-Item Env:SampleAccounts__AdminPassword -ErrorAction SilentlyContinue
Remove-Item Env:SampleAccounts__StudentPassword -ErrorAction SilentlyContinue
dotnet run --project .\src\ToeicReading.Web.Host
```

Khi `VITE_USE_MOCK_DATA=true`, luồng DOCX không giả lập phân tích hay lưu thành
công mà báo rõ cần bật API thật. Cấu hình `.env.local` ở trên đặt ứng dụng vào
chế độ API thật.

## Nhập đề từ DOCX

1. Chạy ASP.NET Core Host và frontend bằng các lệnh ở trên.
2. Chọn hoặc kéo thả file `.docx` (tối đa 10 MB) tại trang **Tải đề từ Word**.
3. Bấm **Đọc file Word**. Backend kiểm tra cấu trúc và trả bản xem trước; lỗi
   parser hoặc lỗi xác thực được hiển thị trong trang.
4. Mở **Xem trước nội dung**. Có thể điền lựa chọn còn thiếu và xác nhận đáp án
   A/B/C/D. Không thể nhập khi còn lỗi hoặc đáp án chưa xác định.
5. Bấm **Nhập đề** để gửi dữ liệu đã xác nhận. Backend tạo đề chưa công bố,
   bài đọc, câu hỏi và bốn phương án trong một Unit of Work. Khi thành công,
   giao diện hiển thị mã đề và tải lại danh sách đề.

Các endpoint:

- `GET /api/services/app/Exam/GetPublishedExams`: API danh sách đề công khai,
  chỉ trả các đề đã công bố. Trang Admin dùng endpoint có bảo vệ bên dưới.
- `GET /api/admin/exams`: danh sách quản trị gồm cả đề nháp, nếu Host đã được
  build/restart với endpoint Admin mới.
- `POST /api/services/app/Exam/SetPublicationStatus`: JSON
  `{ "id": 1, "isPublished": true }`; yêu cầu JWT và quyền
  `Pages.Exams.Import`. Backend chỉ xuất bản khi đề có câu hỏi hợp lệ. Part 5
  và Part 7 phải có `QuestionText`; Part 6 có thể lưu chỗ trống của câu hỏi
  dưới dạng marker `[số câu]` trong nội dung bài đọc (`ReadingPassage.Content`).
  Mọi câu vẫn phải có đủ bốn phương án có nội dung và đúng một đáp án đúng.
  Trạng thái `IsPublished` hiện cũng là trạng thái hiển thị đề trong danh sách
  Student; dự án chưa có trạng thái kích hoạt riêng.
- `POST /api/admin/exams/parse-word`: `multipart/form-data`, trường `file`;
  trả `result` gồm tiêu đề, thời lượng, bài đọc, câu hỏi, lựa chọn, lỗi và cảnh
  báo. Endpoint chỉ phân tích, không ghi database. Luôn yêu cầu JWT và quyền
  Admin `Pages.Exams.Import`, kể cả khi gọi từ localhost Development.
- `POST /api/admin/exams/import-word`: JSON `WordExamPreviewDto` đã được kiểm
  tra/xác nhận; trả `result` gồm `examId`, `passageCount`, `questionCount` và
  thông báo. Từ chối tên đề trùng, payload không hợp lệ và không công bố đề.
  Xác thực giống API parse.

Để chạy production cục bộ, đặt `ASPNETCORE_ENVIRONMENT=Production` và khởi động
Host không dùng launch profile (profile dự án đặt môi trường `Development`):

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
dotnet run --no-launch-profile --configuration Release --project .\src\ToeicReading.Web.Host
```

Khi chạy production, các endpoint Word yêu cầu đăng nhập và quyền Admin như
trước; không bật chế độ miễn đăng nhập bằng biến frontend.

Backend hiện chưa có API tạo đề thủ công hoặc cập nhật câu hỏi. Admin hiển thị
lỗi rõ ràng cho các thao tác đó thay vì gọi các URL chưa được triển khai; tạo
đề hiện có thể thực hiện bằng luồng nhập DOCX.

Hiện backend chưa có API hoặc bảng lưu lượt thi. Student chưa thể bắt đầu bài
thi qua các API của dự án, nên không tồn tại lượt thi đang diễn ra để cản thao
tác tắt xuất bản. Khi bổ sung vòng đời lượt thi, cần thêm kiểm tra lượt thi
đang diễn ra trước khi cho phép tắt trạng thái này.

File Word dùng các tag `[TITLE]`, `[DESCRIPTION]`, `[DURATION]`,
`[PASSAGE PART=6 TITLE="..."]`, `[END PASSAGE]`,
`[QUESTION PART=5 NUMBER=101]`, các dòng lựa chọn `A.` đến `D.`,
`[ANSWER]`, tùy chọn `[EXPLANATION]`/`[HINT]`, `[END QUESTION]` và kết thúc
bằng `[END EXAM]`. Không cần quy định số dòng trống cố định.

## Chạy backend và kiểm tra dữ liệu

Từ PowerShell:

```powershell
Set-Location D:\KIEMTRAGIUAKY\backend\10.2.0\aspnet-core
dotnet run --project .\src\ToeicReading.Web.Host
```

Build backend và chạy riêng parser tests:

```powershell
# Dừng Host đang chạy trước khi build để tránh khóa DLL đầu ra.
dotnet build .\ToeicReading.sln
dotnet test .\test\ToeicReading.Tests\ToeicReading.Tests.csproj --filter "FullyQualifiedName~WordExamImportParserTests"
```

Trong SQL Server Management Studio, chọn database `ToeicReadingDb`, sau đó
kiểm tra bảng đề thi và quan hệ:

```sql
SELECT Id, Title, DurationMinutes, IsPublished, CreationTime
FROM Exams
ORDER BY Id DESC;

SELECT Id, ExamId, PartNumber, Title, OrderNumber
FROM ReadingPassages
ORDER BY ExamId DESC, OrderNumber;

SELECT q.Id, q.ExamId, q.ReadingPassageId, q.PartNumber,
       q.QuestionNumber, o.Label, o.Content, o.IsCorrect
FROM Questions AS q
JOIN QuestionOptions AS o ON o.QuestionId = q.Id
ORDER BY q.ExamId DESC, q.QuestionNumber, o.OrderNumber;
```

Nếu dự án cấu hình tiền tố bảng khác, xem tên bảng thực tế trong Object
Explorer trước khi chạy truy vấn.

Không đặt API key, mật khẩu hoặc thông tin xác thực trong mã nguồn hay biến
`VITE_*`; các biến Vite được đóng gói công khai về phía trình duyệt.

## Build kiểm tra

```powershell
npm run build
```

File đầu ra được tạo trong `dist/`.
