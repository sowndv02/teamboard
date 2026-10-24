@TB-010 @gateway
Feature: Health của gateway
  Là vận hành
  Tôi muốn gateway có /health/live và /health/ready độc lập với upstream
  Để orchestrator kiểm tra gateway mà không bị kéo theo lỗi của service phía sau

  @S03 @FR-002 @BR-003
  Scenario: Live trả 200
    Given một upstream giả đang chạy
    And gateway được cấu hình trỏ tới upstream giả
    When tôi gửi "GET" tới "/health/live"
    Then mã trạng thái là 200
    And upstream giả không nhận request nào

  @S04 @FR-002
  Scenario: Ready trả 200 khi upstream đã dừng
    Given một upstream giả đang chạy
    And gateway được cấu hình trỏ tới upstream giả
    And upstream giả đã dừng
    When tôi gửi "GET" tới "/health/ready"
    Then mã trạng thái là 200
