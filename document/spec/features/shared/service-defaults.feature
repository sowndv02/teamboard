@TB-001 @shared
Feature: ServiceDefaults dùng chung cho mọi service
  Là lập trình viên
  Tôi muốn một thư viện ServiceDefaults dùng lại được
  Để mọi service có cùng health và telemetry mà không copy code

  @S06 @FR-002
  Scenario: Check sẵn sàng báo không khoẻ trước khi ứng dụng khởi động xong
    Given một host dùng AddServiceDefaults chưa khởi động
    When tôi chạy các health check có tag "ready"
    Then kết quả là Unhealthy

  @S07 @FR-004
  Scenario: Một service khác dùng ServiceDefaults nhận cùng health
    Given một host tối thiểu gọi AddServiceDefaults và MapDefaultEndpoints đã khởi động
    When tôi gọi cả "GET /health/live" và "GET /health/ready"
    Then cả hai phản hồi có mã 200
