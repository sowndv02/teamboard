@TB-021 @identity-service @docker
Feature: Cơ sở dữ liệu của identity-service
  Là vận hành
  Tôi muốn identity-service chỉ báo sẵn sàng khi cơ sở dữ liệu dùng được và khôi phục được bằng migration
  Để không nhận đăng nhập khi không có chỗ lưu và dựng lại được môi trường dev từ đầu

  @S02 @NFR-001
  Scenario: Ready trả 200 khi cơ sở dữ liệu chạy
    Given một cơ sở dữ liệu PostgreSQL trống đang chạy
    And identity-service được cấu hình trỏ tới cơ sở dữ liệu đó và tự áp migration khi khởi động
    When tôi gửi "GET" tới "/health/ready"
    Then mã trạng thái là 200

  @S03 @NFR-001 @NFR-003
  Scenario: Ready trả 503 khi cơ sở dữ liệu đã dừng
    Given một cơ sở dữ liệu PostgreSQL trống đang chạy
    And identity-service được cấu hình trỏ tới cơ sở dữ liệu đó và tự áp migration khi khởi động
    And cơ sở dữ liệu đã dừng
    When tôi gửi "GET" tới "/health/ready"
    Then mã trạng thái là 503
    And nội dung phản hồi không chứa mật khẩu hay chuỗi kết nối

  @S04 @NFR-009
  Scenario: Migration nền áp lên cơ sở dữ liệu trống và áp lần hai không đổi gì
    Given một cơ sở dữ liệu PostgreSQL trống đang chạy
    When identity-service khởi động hai lần liên tiếp với cơ sở dữ liệu đó và tự áp migration
    Then lịch sử migration của cơ sở dữ liệu có đúng một bản ghi nền

  @S07 @NFR-009
  Scenario: Không tự áp migration khi cờ tự áp tắt
    Given một cơ sở dữ liệu PostgreSQL trống đang chạy
    When identity-service khởi động với cơ sở dữ liệu đó và không bật tự áp migration
    Then identity-service vẫn trả mã trạng thái 200 cho kiểm tra còn sống
    And cơ sở dữ liệu chưa có lịch sử migration
