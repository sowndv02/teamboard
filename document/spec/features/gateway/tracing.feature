@TB-022 @gateway
Feature: Trace nối xuyên gateway và board-service
  Là người vận hành
  Tôi muốn thấy một request đi qua gateway và service trong cùng một trace
  Để định vị chậm hoặc lỗi ở đúng service

  @S06 @NFR-002 @NFR-004 @docker
  Scenario: Một trace trong Jaeger có span của cả gateway và board-service
    Given Docker khả dụng
    And image của gateway và board-service đã được build bằng compose chính và observability
    When tôi chạy gateway, board-service và jaeger bằng compose với tên dự án riêng
    And tôi gọi "GET /api/v1/boards" qua cổng host 8080 với traceparent do test sinh
    Then Jaeger có trace theo trace id đó chứa span của service "gateway" và "board-service"
