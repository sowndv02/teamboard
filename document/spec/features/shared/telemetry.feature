@TB-001 @shared
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
    When tôi gửi một yêu cầu ghi dữ liệu với header Authorization "Bearer token-bi-mat-123", cookie "phien=cookie-bi-mat-789", query "ma=query-bi-mat-321" và body "noi-dung-bi-mat-456"
    Then telemetry đã ghi nhận ít nhất một span
    And không span hay bản ghi log nào chứa "token-bi-mat-123"
    And không span hay bản ghi log nào chứa "cookie-bi-mat-789"
    And không span hay bản ghi log nào chứa "query-bi-mat-321"
    And không span hay bản ghi log nào chứa "noi-dung-bi-mat-456"

  @S12 @FR-003
  Scenario: Request tạo metric của máy chủ web
    Given một host dùng AddServiceDefaults với tên service "board-service" đã khởi động
    When tôi gọi "GET /health/live"
    Then telemetry đã ghi nhận ít nhất một metric

  @S13 @FR-003
  Scenario: Request tạo bản ghi log
    Given một host dùng AddServiceDefaults với tên service "board-service" đã khởi động
    When tôi gọi "GET /health/live"
    Then telemetry đã ghi nhận ít nhất một bản ghi log

  @S16 @FR-003
  Scenario: Endpoint OTLP không tới được thì service vẫn chạy
    Given một host dùng AddServiceDefaults với tên service "board-service" và endpoint OTLP không có dịch vụ lắng nghe đã khởi động
    When tôi gọi "GET /health/live"
    Then mã trạng thái là 200
