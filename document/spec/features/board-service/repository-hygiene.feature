@TB-001 @board-service
Feature: Vệ sinh phụ thuộc và cấu hình của repo
  Là lập trình viên
  Tôi muốn phiên bản gói cố định và không có bí mật viết cứng
  Để build lặp lại được và không lộ bí mật

  @S14 @NFR-003
  Scenario: Mọi gói NuGet của repo có phiên bản cố định
    When tôi đọc khai báo gói của các dự án .NET
    Then mọi gói có phiên bản chính xác, không dùng dải hay dấu sao

  @S15 @NFR-001
  Scenario: Tệp compose không chứa bí mật viết cứng
    When tôi đọc các tệp compose của repo
    Then mọi giá trị mật khẩu, khoá hay token chỉ tham chiếu biến môi trường
