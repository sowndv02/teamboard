@TB-001
Feature: Telemetry OpenTelemetry của ServiceDefaults
  Là lập trình viên
  Tôi muốn trace, metric, log xuất bằng OpenTelemetry
  Để debug nhiều service mà không lộ dữ liệu nhạy cảm

  @S08 @FR-003
  Scenario: Request tạo span có tên service board-service
    Given một host dùng AddServiceDefaults với tên service "board-service" đã khởi động
    When tôi gọi "GET /health/live"
    Then có một span của request với service.name "board-service"

  @S09 @FR-003
  Scenario: Thiếu endpoint OTLP thì service vẫn khởi động
    Given biến môi trường endpoint OTLP không được đặt
    And một host dùng AddServiceDefaults với tên service "board-service" đã khởi động
    When tôi gọi "GET /health/live"
    Then mã trạng thái là 200

  @S10 @NFR-004
  Scenario: Telemetry không chứa Authorization hay body
    Given một host dùng AddServiceDefaults với tên service "board-service" đã khởi động
    When tôi gửi "POST /echo" với header Authorization "Bearer token-bi-mat-123" và body "noi-dung-bi-mat-456"
    Then không span hay bản ghi log nào chứa "token-bi-mat-123"
    And không span hay bản ghi log nào chứa "noi-dung-bi-mat-456"
