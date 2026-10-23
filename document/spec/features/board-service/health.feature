@TB-001
Feature: Health của board-service
  Là vận hành
  Tôi muốn board-service có /health/live và /health/ready
  Để orchestrator kiểm tra service

  @S04 @FR-002
  Scenario: Live trả 200 khi service đã khởi động
    Given board-service đã khởi động
    When tôi gọi "GET /health/live"
    Then mã trạng thái là 200

  @S05 @FR-002
  Scenario: Ready trả 200 khi service đã sẵn sàng
    Given board-service đã khởi động
    When tôi gọi "GET /health/ready"
    Then mã trạng thái là 200
