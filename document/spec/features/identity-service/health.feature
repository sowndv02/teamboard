@TB-021 @identity-service
Feature: Health của identity-service
  Là vận hành
  Tôi muốn identity-service có /health/live và /health/ready
  Để orchestrator biết service còn sống và khi nào nó sẵn sàng nhận request

  @S01 @NFR-001
  Scenario: Live trả 200 khi cơ sở dữ liệu không chạy
    Given identity-service được cấu hình trỏ tới một cơ sở dữ liệu không chạy
    When tôi gửi "GET" tới "/health/live"
    Then mã trạng thái là 200

  @S08 @NFR-001
  Scenario: Telemetry của identity-service mang đúng tên service
    Given identity-service được cấu hình trỏ tới một cơ sở dữ liệu không chạy
    When tôi gửi "GET" tới "/health/live"
    Then telemetry của service mang tên "identity-service"
